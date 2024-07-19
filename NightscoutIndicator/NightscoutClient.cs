using System;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Windows;
using Microsoft.Extensions.Options;
using NightscoutIndicator.Nightscout;

namespace NightscoutIndicator;

public class NightscoutClient : IDisposable
{
    private readonly HttpClient _client;
    public NightscoutIndicatorSettings _settings { get; set; }


    /// <summary>
    /// Initializes a new instance of the <see cref="NightscoutClient"/> class.
    /// </summary>
    public NightscoutClient(IOptions<NightscoutIndicatorSettings> settings)
    {
        _settings = settings.Value;

        _client = new HttpClient();
    }

    public async Task<List<NightscoutEntry>> GetLatestEntriesAsync(int count)
    {
        // URL and secret are read on every call: they may be empty on the first run and changed later in Settings
        if (string.IsNullOrWhiteSpace(_settings.NightscoutUrl))
        {
            throw new NightscoutException("Nightscout URL is not configured");
        }

        var requestUri = $"{_settings.NightscoutUrl.Trim().TrimEnd('/')}/api/v1/entries?count={count}";
        using var request = new HttpRequestMessage(HttpMethod.Get, requestUri);
        request.Headers.Add("API-Secret", GetApiSecretHash(_settings.ApiSecret));

        using var requestResult = await _client.SendAsync(request);
        if (!requestResult.IsSuccessStatusCode)
        {
            throw new NightscoutException("Nightscout API: HTTP " + (int)requestResult.StatusCode + " " + requestResult.StatusCode);
        }

        var content = await requestResult.Content.ReadAsStringAsync();
        // An empty response or a trailing line break must not be parsed as an entry
        var lines = content.Replace("\r\n", "\n").Split('\n').Where(x => !string.IsNullOrWhiteSpace(x)).ToList();
        var entries = lines.Select(x => new NightscoutEntry(x, _settings.Unit)).Distinct().OrderByDescending(x => x.Timestamp).ToList();
        return entries;
    }

    /// <summary>
    /// Computes the SHA1 hash of the API secret in lowercase hex, as Nightscout expects.
    /// </summary>
    private static string GetApiSecretHash(string apiSecret)
    {
        var hash = SHA1.HashData(Encoding.UTF8.GetBytes(apiSecret ?? string.Empty));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    /// <summary>
    /// Releases the resources.
    /// </summary>
    public void Dispose()
    {
        _client?.Dispose();
    }
}
