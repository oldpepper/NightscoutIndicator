using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Threading;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Json;
using Microsoft.Extensions.Options;
using NAudio.Wave;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using NightscoutIndicator.Nightscout;
using NightscoutIndicator.SettingsManagement;
using Application = System.Windows.Application;
using MessageBox = System.Windows.MessageBox;
using MouseEventArgs = System.Windows.Forms.MouseEventArgs;

namespace NightscoutIndicator;

public class MainViewModel : INotifyPropertyChanged, IDisposable
{
    private SettingsWindow _settingsWindow;
    private readonly SettingsViewModel _settingsViewModel;
    private readonly DynamicTrayIconFactory _dynamicTrayIconFactory = new();
    private readonly NightscoutClient _nightscoutClient;
    private DateTimeOffset _lastAlarmDate;
    
    private NotifyIcon _notifyIconGlucose;
    private NotifyIcon _notifyIconTrend;
    private NightscoutEntry _latestEntry;
    private WaveOutEvent _alarmPlayer;
    private MediaFoundationReader _alarmReader;
    private string _lastErrorText;
    private readonly DataReceiver _dataReceiver;
    private readonly DiagramViewModel _diagramViewModel;
    private readonly FlyingIndicatorViewModel _flyingIndicatorViewModel;
    private readonly DiaCalculations _diaCalculations;
    private readonly NightscoutIndicatorSettings _settings;

    /// <summary>
    /// Initializes a new instance of the <see cref="MainViewModel"/> class.
    /// </summary>
    public MainViewModel(IOptions<NightscoutIndicatorSettings> settings, 
                         FlyingIndicatorViewModel flyingIndicatorViewModel, 
                         DiaCalculations diaCalculations,
                         DataReceiver dataReceiver,
                         DiagramViewModel diagramViewModel,
                         NightscoutClient nightscoutClient,
                         SettingsViewModel settingsViewModel)
    {
        _flyingIndicatorViewModel = flyingIndicatorViewModel;
        _diaCalculations = diaCalculations;
        _dataReceiver = dataReceiver;
        _diagramViewModel = diagramViewModel;
        _nightscoutClient = nightscoutClient;
        _settingsViewModel = settingsViewModel;
        _settings = settings.Value;

        ExitApplicationCommand = new DelegateCommand
        {
            CommandAction = () => Application.Current.Shutdown()
        };

        CreateIcons();

        _settingsViewModel.SettingsSavedEvent += SettingsViewModel_OnSettingsSavedEvent;
        _dataReceiver.DataReceivedEvent += DataReceiver_OnDataReceivedEvent;
        _dataReceiver.DataExpiredEvent += DataReceiver_OnDataExpiredEvent;
        _dataReceiver.DataErrorEvent += DataReceiver_OnDataErrorEvent;
    }

    private void ShowSettingsWindow()
    {
        if (_settingsWindow != null)
        {
            _settingsWindow.Activate();
            return;
        }

        // Every time the window is opened it shows the current settings
        _settingsViewModel.Load();
        _settingsWindow = new SettingsWindow(_settingsViewModel);
        _settingsWindow.Closed += (_, _) => _settingsWindow = null;
        _settingsWindow.Show();
        _settingsWindow.Activate();
    }

    public ICommand ExitApplicationCommand { get; }

    /// <summary>
    /// Redraws the tray icons and the indicator with the new settings.
    /// </summary>
    private void SettingsViewModel_OnSettingsSavedEvent()
    {
        _flyingIndicatorViewModel.ApplySettings();
        UpdateLastData(_latestEntry);
    }

    private void DataReceiver_OnDataExpiredEvent()
    {
        RunOnUiThread(() => UpdateLastData(_latestEntry));
    }

    private void DataReceiver_OnDataReceivedEvent(List<NightscoutEntry> entries)
    {
        var latestEntry = entries.FirstOrDefault();
        RunOnUiThread(() =>
        {
            // A successful response clears the last error even if it has no entries
            _lastErrorText = null;
            if (latestEntry != null)
            {
                _latestEntry = latestEntry;
            }

            UpdateLastData(_latestEntry);
        });
    }

    private void DataReceiver_OnDataErrorEvent(string errorText)
    {
        RunOnUiThread(() =>
        {
            _lastErrorText = errorText;
            UpdateLastData(_latestEntry);
        });
    }

    /// <summary>
    /// Gets the tray icon tooltip text.
    /// </summary>
    private string GetTooltipText(NightscoutEntry latestEntry)
    {
        // NotifyIcon.Text accepts at most 127 characters
        const int MAX_LENGTH = 127;

        var text = latestEntry is null ? "null" : latestEntry.Value.ToString("F1");
        if (!string.IsNullOrEmpty(_lastErrorText))
        {
            text += $"{Environment.NewLine}No data: {_lastErrorText}";
        }

        return text.Length <= MAX_LENGTH ? text : text.Substring(0, MAX_LENGTH);
    }

    /// <summary>
    /// Runs the action on the UI thread.
    /// </summary>
    private static void RunOnUiThread(Action action)
    {
        // DataReceiver raises its events on a thread pool thread, while tray icons, sounds and message boxes belong to the UI thread
        var dispatcher = Application.Current?.Dispatcher;
        if (dispatcher == null || dispatcher.HasShutdownStarted)
        {
            return;
        }

        if (dispatcher.CheckAccess())
        {
            action();
        }
        else
        {
            dispatcher.BeginInvoke(action);
        }
    }

    private void CreateIcons()
    {
        var trayMenu = new ContextMenuStrip();
        trayMenu.Items.Add("Settings", null, (_, _) => ShowSettingsWindow());
        trayMenu.Items.Add(new ToolStripSeparator());
        trayMenu.Items.Add("Exit", null, (_, _) => Application.Current.Shutdown());

        var trayMenu2 = new ContextMenuStrip();
        trayMenu2.Items.Add("Settings", null, (_, _) => ShowSettingsWindow());
        trayMenu2.Items.Add(new ToolStripSeparator());
        trayMenu2.Items.Add("Exit", null, (_, _) => Application.Current.Shutdown());

        
        _notifyIconGlucose = new NotifyIcon();
//        _notifyIconGlucose.Icon = new Icon(@"Icon\\arrow-up2.ico", 32, 32);
        _notifyIconGlucose.ContextMenuStrip = trayMenu;
        _notifyIconGlucose.MouseClick += _diagramViewModel.ShowDiagramAsync;
        _notifyIconGlucose.MouseDoubleClick += (_, _) => ShowSettingsWindow();
        _notifyIconGlucose.Visible = true;


        _notifyIconTrend = new NotifyIcon();
        //        _notifyIconTrend.Icon = new Icon(@"Icon\\arrow-up2.ico", 32, 32);
        _notifyIconTrend.ContextMenuStrip = trayMenu2;
        _notifyIconTrend.MouseClick += _diagramViewModel.ShowDiagramAsync;
        _notifyIconTrend.MouseDoubleClick += (_, _) => ShowSettingsWindow();
        _notifyIconTrend.Visible = true;
    }

    private void UpdateLastData(NightscoutEntry latestEntry)
    {
        if (_notifyIconGlucose == null || _notifyIconTrend == null)
        {
            return;
        }

        var isSingleIcon = _settings.TrayIconMode == TrayIconMode.SingleIcon;

//        if (latestEntry is null)
//        {
//            return;
//        }
        
        var level = _diaCalculations.CalculateLevel(latestEntry?.Value);

        var oldGlucoseIcon = _notifyIconGlucose.Icon;
        var isExpired = latestEntry is null || DateTimeOffset.Now - latestEntry.Timestamp > TimeSpan.FromMinutes(_settings.DataExpirationTimeout);
        var glucoseText = latestEntry?.Value.ToString("0.#", CultureInfo.InvariantCulture);
        var iconGlucose = isSingleIcon
            ? _dynamicTrayIconFactory.CreateSingleIcon(glucoseText, level, _diaCalculations.CalculateLevelDrawingColor(level), latestEntry?.Trend ?? NightscoutTrend.None, isExpired)
            : _dynamicTrayIconFactory.CreateGlucoseBitmapSource(glucoseText, isExpired);
        _notifyIconGlucose.Icon = iconGlucose;
        oldGlucoseIcon?.Dispose();
        _notifyIconGlucose.Text = GetTooltipText(latestEntry);
        _notifyIconGlucose.Visible = true;

        if (isSingleIcon)
        {
            // The second icon is not needed: the trend is shown by the stripe on the first one
            _notifyIconTrend.Visible = false;
            var unusedTrendIcon = _notifyIconTrend.Icon;
            _notifyIconTrend.Icon = null;
            unusedTrendIcon?.Dispose();
        }
        else
        {
            var oldTrendIcon = _notifyIconTrend.Icon;
            var iconTrend = _dynamicTrayIconFactory.CreateTrendIcon(_settings.GetTrendIconFileName(latestEntry?.Trend ?? NightscoutTrend.None), _diaCalculations.CalculateLevelDrawingColor(level));
            _notifyIconTrend.Icon = iconTrend;
            oldTrendIcon?.Dispose();
            _notifyIconTrend.Text = GetTooltipText(latestEntry);
            _notifyIconTrend.Visible = true;
        }

        CheckRaiseAlarm(latestEntry);
    }

    private void CheckRaiseAlarm(NightscoutEntry latestEntry)
    {
        if(latestEntry is null)
            return;
        
        if (_settings.UseAlarm &&
            latestEntry.Value > _settings.High &&
            latestEntry.Trend is NightscoutTrend.DoubleUp or NightscoutTrend.Flat or NightscoutTrend.FortyFiveUp or NightscoutTrend.SingleUp &&
            (latestEntry.Value == 0 || DateTimeOffset.Now > _lastAlarmDate.AddMinutes(_settings.AlarmInterval)))
        {
            PlayAlarm(_settings.AlarmHiFileName);
            _notifyIconGlucose.ShowBalloonTip(3000, "HI", latestEntry.Value.ToString("F1"), ToolTipIcon.Error);

            _lastAlarmDate = DateTimeOffset.Now;
            return;
        }

        if (_settings.UseAlarm &&
            latestEntry.Value <= _settings.Low &&
            latestEntry.Trend is NightscoutTrend.DoubleDown or NightscoutTrend.Flat or NightscoutTrend.FortyFiveDown or NightscoutTrend.SingleDown &&
            (latestEntry.Value == 0 || DateTimeOffset.Now > _lastAlarmDate.AddMinutes(_settings.AlarmInterval)))
        {
            PlayAlarm(_settings.AlarmLowFileName);
            _notifyIconGlucose.ShowBalloonTip(3000, "LOW", latestEntry.Value.ToString("F1"), ToolTipIcon.Error);

            _lastAlarmDate = DateTimeOffset.Now;
            return;
        }
    }

    private void PlayAlarm(string fileName)
    {
        try
        {
            var dir = AppContext.BaseDirectory;
            var alarm = Path.Combine(dir, fileName);

            if (File.Exists(alarm))
            {
                StopAlarm();

                _alarmReader = new MediaFoundationReader(alarm);
                _alarmPlayer = new WaveOutEvent();
                _alarmPlayer.PlaybackStopped += AlarmPlayer_OnPlaybackStopped;
                _alarmPlayer.Init(_alarmReader);
                _alarmPlayer.Play();
                _lastAlarmDate = DateTimeOffset.Now;
            }
        }
        catch (Exception ex)
        {
            StopAlarm();
            MessageBox.Show($"Error playing sound {fileName}:\n{ex.Message}");
        }
    }

    private void AlarmPlayer_OnPlaybackStopped(object sender, StoppedEventArgs e)
    {
        StopAlarm();
    }

    /// <summary>
    /// Stops the alarm sound and releases the player.
    /// </summary>
    private void StopAlarm()
    {
        if (_alarmPlayer != null)
        {
            _alarmPlayer.PlaybackStopped -= AlarmPlayer_OnPlaybackStopped;
            _alarmPlayer.Dispose();
            _alarmPlayer = null;
        }

        if (_alarmReader != null)
        {
            _alarmReader.Dispose();
            _alarmReader = null;
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
        _settingsViewModel.SettingsSavedEvent -= SettingsViewModel_OnSettingsSavedEvent;
        _dataReceiver.DataReceivedEvent -= DataReceiver_OnDataReceivedEvent;
        _dataReceiver.DataExpiredEvent -= DataReceiver_OnDataExpiredEvent;
        _dataReceiver.DataErrorEvent -= DataReceiver_OnDataErrorEvent;
        StopAlarm();
        _nightscoutClient?.Dispose();
        
        
        if (_notifyIconGlucose != null)
        {
            _notifyIconGlucose.MouseClick -= _diagramViewModel.ShowDiagramAsync;
        }
        var iconGlucose = _notifyIconGlucose?.Icon;
        _notifyIconGlucose?.Dispose();
        _notifyIconGlucose = null;
        iconGlucose?.Dispose();


        if (_notifyIconTrend != null)
        {
            _notifyIconTrend.MouseClick -= _diagramViewModel.ShowDiagramAsync;
        }
        var iconTrend = _notifyIconTrend?.Icon;
        _notifyIconTrend?.Dispose();
        _notifyIconTrend = null;
        iconTrend?.Dispose();
    }
}