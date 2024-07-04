using System.ComponentModel;

namespace NightscoutIndicator.SettingsManagement;

public class SettingsManager : IDisposable
{
    private readonly IWritableOptions<FlyingIndicatorWindowSettings> _writableOptions;
    private readonly FlyingIndicatorWindowSettings _flyingIndicatorWindowSettings;
    private Timer _saveTimer;

    /// <summary>Initializes a new instance of the <see cref="T:System.Object" /> class.</summary>
    public SettingsManager(IWritableOptions<FlyingIndicatorWindowSettings> writableOptions)
    {
        _writableOptions = writableOptions;
        _flyingIndicatorWindowSettings = writableOptions.Value;
        _flyingIndicatorWindowSettings.PropertyChanged += FlyingIndicatorWindowSettings_OnPropertyChanged;
    }

    public void Start()
    {
        _saveTimer = new Timer(SaveWindowPosition, null, Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
    }

    private async void SaveWindowPosition(object state)
    {
        try
        {
            await _writableOptions.UpdateAsync(_ => {});
        }
        catch (Exception e)
        {
            // ignored
        }
    }

    private void FlyingIndicatorWindowSettings_OnPropertyChanged(object sender, PropertyChangedEventArgs e)
    {
        _saveTimer.Change(TimeSpan.FromSeconds(3), Timeout.InfiniteTimeSpan);
    }

    /// <summary>Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.</summary>
    public void Dispose()
    {
        _saveTimer.Change(Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
        _saveTimer?.Dispose();
    }
}