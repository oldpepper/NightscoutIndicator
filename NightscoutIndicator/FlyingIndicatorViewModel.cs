using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Options;
using NightscoutIndicator.Nightscout;
using NightscoutIndicator.SettingsManagement;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Media;

namespace NightscoutIndicator;

public partial class FlyingIndicatorViewModel : INotifyPropertyChanged, IDisposable
{
    private readonly NightscoutClient _nightscoutClient;
    private bool _isDiagramOpened;
    private GlucoseDiagram _diagram;
    private readonly NightscoutIndicatorSettings _settings;
    private readonly DataReceiver _dataReceiver;
    private readonly IWritableOptions<FlyingIndicatorWindowSettings> _windowSettingsWritableOptions;
    private readonly FlyingIndicatorWindowSettings _windowSettings;
    private readonly DiaCalculations _diaCalculations;
    private readonly DiagramViewModel _diagramViewModel;

    private string _glucoseValueText = string.Empty;
    private int _angleByTrend = 0;
    private Brush _brushByGlucose = Brushes.CadetBlue;
    private Brush _brushByIsExpired = Brushes.Orchid;
    private decimal _top = 50;
    private decimal _left = 50;
    private NightscoutEntry _lastNightscoutEntry;
    private readonly FlyingIndicatorWindow _flyingIndicatorView;
    private readonly Timer _saveWindowSettingsDelayTimer;

    public event Action<FlyingIndicatorWindowSettings> IndicatorWindowSettingsChangedEvent;

    public RelayCommand ShowWindowCommand { get; }
//    public RelayCommand ShowDiagramCommand { get; }

    public Visibility Visibility
    {
        get => Visibility.Visible;
        set => FirePropertyChanged();
    }

    public int FlyingIndicatorSize => _settings.FlyingIndicatorSize;
    public string GlucoseValueText
    {
        get => _glucoseValueText;
        set => SetField(ref _glucoseValueText, value);
    }

    public int AngleByTrend
    {
        get => _angleByTrend;
        set => SetField(ref _angleByTrend, value);
    }

    public Brush BrushByGlucose
    {
        get => _brushByGlucose;
        set => SetField(ref _brushByGlucose, value);
    }

    public Brush BrushByIsExpired
    {
        get => _brushByIsExpired;
        set => SetField(ref _brushByIsExpired, value);
    }

    public decimal Top
    {
        get => _windowSettings.Top;
        set
        {
            if (_top != value)
            {
                SetField(ref _top, value);
                _windowSettings.Top = value;
            }
        }
    }

    public decimal Left
    {
        get => _windowSettings.Left;
        set
        {
            if (_left != value)
            {
                SetField(ref _left, value);
                _windowSettings.Left = value;
            }
        }
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="FlyingIndicatorViewModel"/> class.
    /// </summary>
    public FlyingIndicatorViewModel(IOptions<NightscoutIndicatorSettings> settings, 
                                    DataReceiver dataReceiver, 
                                    IWritableOptions<FlyingIndicatorWindowSettings> windowSettingsWritableOptions, 
                                    DiaCalculations diaCalculations,
                                    DiagramViewModel diagramViewModel
            )
    {
        _settings = settings.Value;
        _windowSettings = windowSettingsWritableOptions.Value;
        _dataReceiver = dataReceiver;
        _windowSettingsWritableOptions = windowSettingsWritableOptions;
        _diaCalculations = diaCalculations;
        _diagramViewModel = diagramViewModel;
        _dataReceiver.DataReceivedEvent += DataReceiver_OnDataReceivedEvent;
        _dataReceiver.DataExpiredEvent += DataReceiver_OnDataExpiredEvent;

        ShowWindowCommand = new RelayCommand(() => Visibility = Visibility.Visible, () => Visibility != Visibility.Visible);
//        ShowDiagramCommand = new RelayCommand(ShowDiagram);

        var top = (double)Top;
        var left = (double)Left;

//        if (MultiMonitorWindowValidator.ValidateAndCorrectWindowPosition(ref left, ref top, FlyingIndicatorSize, FlyingIndicatorSize, out Rect rect))
//        {
//            Top = (decimal)rect.Top;
//            Left = (decimal)rect.Left;
//        }

        _flyingIndicatorView = new FlyingIndicatorWindow
        {
            DataContext = this
        };
        
        _saveWindowSettingsDelayTimer = new Timer(SaveIndicatorPosition, null, Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
        _windowSettings.PropertyChanged += WindowSettings_OnPropertyChanged;
    }
    
    [RelayCommand]
    private async Task ShowDiagram()
    {
        _diagramViewModel.ShowDiagramAsync(_diagram, null);
    }

    private void WindowSettings_OnPropertyChanged(object sender, PropertyChangedEventArgs e)
    {
        _saveWindowSettingsDelayTimer.Change(TimeSpan.FromSeconds(2), Timeout.InfiniteTimeSpan);
    }

    private void SaveIndicatorPosition(object _)
    {
        try
        {
            _windowSettingsWritableOptions.UpdateAsync(_ => {}).GetAwaiter().GetResult();
        }
        catch (Exception)
        {
            // The position is saved again on the next move
        }
    }

    private void DataReceiver_OnDataExpiredEvent()
    {
        if (Application.Current.Dispatcher.CheckAccess())
        {
            BrushByIsExpired = Brushes.Orchid;
        }
        else
        {
            Application.Current.Dispatcher.Invoke(() => BrushByIsExpired = Brushes.Orchid);
        }
    }

    private void DataReceiver_OnDataReceivedEvent(List<NightscoutEntry> nightscoutEntries)
    {
        if (nightscoutEntries == null || !nightscoutEntries.Any())
        {
            return;
        }

        _lastNightscoutEntry = nightscoutEntries.FirstOrDefault();
        
        if (Application.Current.Dispatcher.CheckAccess())
        {
            UpdateIndicator(_lastNightscoutEntry);
        }
        else
        {
            Application.Current.Dispatcher.Invoke(() => UpdateIndicator(_lastNightscoutEntry));
        }
    }

    private void UpdateIndicator(NightscoutEntry nightscoutEntry)
    {
        BrushByIsExpired = Brushes.Black;
        GlucoseValueText = nightscoutEntry?.Value.ToString("F1");
        AngleByTrend = CalcAngleByTrend(nightscoutEntry?.Trend);
        var glucoseLevel = _diaCalculations.CalculateLevel(nightscoutEntry?.Value);
        BrushByGlucose = new SolidColorBrush(_diaCalculations.CalculateLevelMediaColor(glucoseLevel));
        BrushByGlucose = new SolidColorBrush(_diaCalculations.CalculateLevelMediaColor(glucoseLevel));
    }

    private int CalcAngleByTrend(NightscoutTrend? nightscoutTrend)
    {
        return nightscoutTrend switch
        {
            NightscoutTrend.SingleUp or NightscoutTrend.DoubleUp => 0,
            NightscoutTrend.FortyFiveUp => 45,
            NightscoutTrend.Flat => 90,
            NightscoutTrend.FortyFiveDown => 135,
            NightscoutTrend.SingleDown or NightscoutTrend.DoubleDown => 180,
            _ => 270
        };
    }

    public void Show()
    {
        Visibility = Visibility.Visible;
    }

    /// <summary>
    /// Applies the changed settings to the indicator.
    /// </summary>
    public void ApplySettings()
    {
        FirePropertyChanged(nameof(FlyingIndicatorSize));

        if (_lastNightscoutEntry != null)
        {
            UpdateIndicator(_lastNightscoutEntry);
        }
    }

#region PropertyChanged

    public event PropertyChangedEventHandler PropertyChanged;

    protected virtual void FirePropertyChanged([CallerMemberName] string propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    protected bool SetField<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
            return false;

        field = value;
        FirePropertyChanged(propertyName);

        return true;
    }

#endregion

    /// <summary>
    /// Releases the resources.
    /// </summary>
    public void Dispose()
    {
        _saveWindowSettingsDelayTimer.Change(Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
        _saveWindowSettingsDelayTimer?.Dispose();
    }
}