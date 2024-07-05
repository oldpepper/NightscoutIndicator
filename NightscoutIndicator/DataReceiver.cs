using Microsoft.Extensions.Options;
using NightscoutIndicator.Nightscout;

namespace NightscoutIndicator;

public class DataReceiver : IDisposable
{
    private readonly NightscoutIndicatorSettings _settings;
    private readonly NightscoutClient _nightscoutClient;
    private readonly Timer _nightscoutCallTimer;
    private DateTimeOffset? _lastDataDate = DateTimeOffset.MinValue;

    public event Action<List<NightscoutEntry>> DataReceivedEvent;
    public event Action DataExpiredEvent;

    /// <summary>
    /// Initializes a new instance of the <see cref="DataReceiver"/> class.
    /// </summary>
    public DataReceiver(IOptions<NightscoutIndicatorSettings> settings, NightscoutClient nightscoutClient)
    {
        _settings = settings.Value;
        _nightscoutClient = nightscoutClient;
        _nightscoutCallTimer = new Timer(CallServerByTimer, null, TimeSpan.FromMilliseconds(300), Timeout.InfiniteTimeSpan);
    }

    private async void CallServerByTimer(object _)
    {
        try
        {
            _nightscoutCallTimer.Change(Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);

            var entries = await _nightscoutClient.GetLatestEntriesAsync(1);

            _lastDataDate = entries.LastOrDefault()?.Timestamp;

            FireDataReceivedEvent(entries);
        }
        catch (Exception ex)
        {
            //            MessageBox.Show($"Error calling GetLatestEntriesAsync:\n{ex.Message}");
        }
        finally
        {
            try
            {
                _nightscoutCallTimer.Change(TimeSpan.FromSeconds(_settings.UpdateInterval), Timeout.InfiniteTimeSpan);

                if (_lastDataDate is null || DateTimeOffset.Now - _lastDataDate > TimeSpan.FromMinutes(_settings.DataExpirationTimeout))
                {
                    FireDataExpiredEvent();
                }
            }
            catch (Exception ex)
            {
                //                MessageBox.Show($"Failed tu setup timer to {_mainViewModel?.Settings?.UpdateInterval} update interval:\n{ex.Message}");
            }
        }
    }

    public void FireDataReceivedEvent(List<NightscoutEntry> entries)
    {
        try
        {
            DataReceivedEvent?.Invoke(entries);
        }
        catch (Exception)
        {
            // ignored
        }
    }

    public void FireDataExpiredEvent()
    {
        try
        {
            DataExpiredEvent?.Invoke();
        }
        catch (Exception)
        {
            // ignored
        }
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            _nightscoutCallTimer.Change(Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
            _nightscoutCallTimer?.Dispose();
        }
    }

    /// <summary>
    /// Releases the resources.
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}