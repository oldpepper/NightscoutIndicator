using Microsoft.Extensions.Options;
using NightscoutIndicator.Nightscout;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Forms;
using MessageBox = System.Windows.MessageBox;

namespace NightscoutIndicator;

/// <summary>
/// Glucose diagram view model.
/// </summary>
public partial class DiagramViewModel : INotifyPropertyChanged, IDisposable
{
    private readonly NightscoutIndicatorSettings _settings;
    private readonly NightscoutClient _nightscoutClient;
    private bool _isDiagramOpened;
    private GlucoseDiagram _diagram;

    /// <summary>
    /// Initializes a new instance of the <see cref="DiagramViewModel"/> class.
    /// </summary>
    public DiagramViewModel(
            IOptions<NightscoutIndicatorSettings> settings,
            NightscoutClient nightscoutClient
            )
    {
        _settings = settings.Value;
        _nightscoutClient = nightscoutClient;
    }

    private void CloseDiagram(object sender, EventArgs e)
    {
        _diagram?.Close();
        _diagram?.Dispose();
        _diagram = null;
        _isDiagramOpened = false;
    }
    
    public async void ShowDiagramAsync(object sender, MouseEventArgs mouseEventArgs)
    {
        if (mouseEventArgs?.Button == MouseButtons.Right)
        {
            return;
        }

        try
        {
            var timer = new Stopwatch();

            timer.Start();
            if (!_isDiagramOpened)
            {
                _isDiagramOpened = true;
                List<NightscoutEntry> entries;
                try
                {
                    entries = await _nightscoutClient.GetLatestEntriesAsync(_settings.TimeRange * 60);
                }
                catch (Exception ex)
                {
                    while (ex.InnerException != null)
                        ex = ex.InnerException;
                    MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
                var maxTimestamp = entries.Max(x => x.Timestamp);
                var minTimestamp = maxTimestamp.AddHours(-(_settings.TimeRange + 5));
                entries = entries.Where(x => x.Timestamp >= minTimestamp).ToList();
                CloseDiagram(this, null);
                _diagram = new GlucoseDiagram(_settings, entries);
                _diagram.LostFocus += CloseDiagram;
                _diagram.Show();
                _diagram.Activate();
            }
            timer.Stop();
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
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
        _diagram?.Dispose();
    }
}