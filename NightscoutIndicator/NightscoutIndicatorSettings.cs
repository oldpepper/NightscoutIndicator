using System.IO;
using Microsoft.Extensions.Configuration;
using NightscoutIndicator.Nightscout;
using NightscoutIndicator.SettingsManagement;

namespace NightscoutIndicator;

public class NightscoutIndicatorSettings
{
    public const string SECTION_NAME = "AppSettings";

    public string NightscoutUrl { get; set; }
    public string ApiSecret { get; set; }
    public int UpdateInterval { get; set; }
    public NightscoutUnit Unit { get; set; }
    public decimal ExtraHigh { get; set; }
    public decimal High { get; set; }
    public decimal Low { get; set; }
    public decimal ExtraLow { get; set; }
    public int TimeRange { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
    public int FontSize { get; set; }
    public double AlarmInterval { get; set; }
    public bool UseAlarm { get; set; }
    public string AlarmHiFileName { get; set; }
    public string AlarmLowFileName { get; set; }
    public int DataExpirationTimeout { get; set; } = 30; // minutes
    public int FlyingIndicatorSize { get; set; } = 50;
    public string TrendIconUp { get; set; }
    public string TrendIconUpRight { get; set; }
    public string TrendIconFlat { get; set; }
    public string TrendIconDownRight { get; set; }
    public string TrendIconDown { get; set; }
    public string TrendIconUnknown { get; set; }

    /// <summary>
    /// Reads the default settings from the default settings file.
    /// </summary>
    public static NightscoutIndicatorSettings LoadDefaults()
    {
        var configuration = new ConfigurationBuilder()
                            .SetBasePath(AppContext.BaseDirectory)
                            .AddJsonFile(UserSettingsFile.DEFAULT_FILE_NAME, optional: true, reloadOnChange: false)
                            .Build();

        return configuration.GetSection(SECTION_NAME).Get<NightscoutIndicatorSettings>() ?? new NightscoutIndicatorSettings();
    }

    /// <summary>
    /// Gets the icon file name for the trend.
    /// </summary>
    public string GetTrendIconFileName(NightscoutTrend trend)
    {
        return trend switch
        {
            NightscoutTrend.DoubleUp => TrendIconUp,
            NightscoutTrend.FortyFiveUp => TrendIconUpRight,
            NightscoutTrend.Flat => TrendIconFlat,
            NightscoutTrend.FortyFiveDown => TrendIconDownRight,
            NightscoutTrend.DoubleDown => TrendIconDown,
            _ => TrendIconUnknown
        };
    }

    /// <summary>
    /// Gets the full path of a file whose name is relative to the application folder.
    /// </summary>
    public static string GetFullPath(string fileName)
    {
        // Absolute paths are returned unchanged by Path.Combine
        return Path.Combine(AppContext.BaseDirectory, fileName ?? string.Empty);
    }

    /// <summary>
    /// Creates a shallow copy of the settings.
    /// </summary>
    public NightscoutIndicatorSettings Clone()
    {
        return (NightscoutIndicatorSettings)MemberwiseClone();
    }

    /// <summary>
    /// Copies the settings values from another instance.
    /// </summary>
    /// <param name="source">Settings to copy from.</param>
    public void CopyFrom(NightscoutIndicatorSettings source)
    {
        foreach (var property in typeof(NightscoutIndicatorSettings).GetProperties())
        {
            if (property.CanRead && property.CanWrite)
            {
                property.SetValue(this, property.GetValue(source));
            }
        }
    }
}