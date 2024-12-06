using System.Net.Http;
using Microsoft.Extensions.Options;
using NightscoutIndicator.Nightscout;

namespace NightscoutIndicator;

public class DataReceiver : IDisposable
{
    // Debug switch: fake data cycling through every glucose level and every trend instead of the server
    private const bool IS_FAKE_DATA = false;
    private int _fakeStep;

    private readonly NightscoutIndicatorSettings _settings;
    private readonly NightscoutClient _nightscoutClient;
    private readonly Timer _nightscoutCallTimer;
    private DateTimeOffset? _lastDataDate = DateTimeOffset.MinValue;

    public event Action<List<NightscoutEntry>> DataReceivedEvent;
    public event Action DataExpiredEvent;

    /// <summary>
    /// Server request error event.
    /// </summary>
    public event Action<string> DataErrorEvent;

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

            var entries = IS_FAKE_DATA ? GetFakeEntries() : await _nightscoutClient.GetLatestEntriesAsync(1);

            _lastDataDate = entries.LastOrDefault()?.Timestamp;

            FireDataReceivedEvent(entries);
        }
        catch (Exception ex)
        {
            FireDataErrorEvent(GetErrorText(ex));
        }
        finally
        {
            try
            {
                _nightscoutCallTimer.Change(IS_FAKE_DATA ? TimeSpan.FromSeconds(2) : TimeSpan.FromSeconds(_settings.UpdateInterval), Timeout.InfiniteTimeSpan);

                if (_lastDataDate is null || DateTimeOffset.Now - _lastDataDate > TimeSpan.FromMinutes(_settings.DataExpirationTimeout))
                {
                    FireDataExpiredEvent();
                }
            }
            catch (Exception)
            {
                // ignored
            }
        }
    }

    /// <summary>
    /// Gets the next fake entry: every glucose level with every trend.
    /// </summary>
    private List<NightscoutEntry> GetFakeEntries()
    {
        decimal[] values =
        [
            _settings.ExtraLow - 0.5m,
            (_settings.ExtraLow + _settings.Low) / 2,
            (_settings.Low + _settings.High) / 2,
            (_settings.High + _settings.ExtraHigh) / 2,
            _settings.ExtraHigh + 1m
        ];
        NightscoutTrend[] trends =
        [
            NightscoutTrend.None,
            NightscoutTrend.Flat,
            NightscoutTrend.FortyFiveUp,
            NightscoutTrend.SingleUp,
            NightscoutTrend.FortyFiveDown,
            NightscoutTrend.SingleDown
        ];

        var step = _fakeStep++ % (values.Length * trends.Length);
        var value = Math.Round(values[step / trends.Length], 1);
        var entry = new NightscoutEntry(DateTimeOffset.Now, value, _settings.Unit, trends[step % trends.Length]);

        return [entry];
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

    public void FireDataErrorEvent(string errorText)
    {
        try
        {
            DataErrorEvent?.Invoke(errorText);
        }
        catch (Exception)
        {
            // ignored
        }
    }

    /// <summary>
    /// Gets a short description of the server request error.
    /// </summary>
    private static string GetErrorText(Exception exception)
    {
        return exception switch
        {
            NightscoutException => exception.Message,
            TaskCanceledException => "Server timeout",
            HttpRequestException => $"Server unavailable: {exception.Message}",
            _ => exception.Message
        };
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