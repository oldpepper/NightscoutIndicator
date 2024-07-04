using NightscoutIndicator.Nightscout;
using System.Configuration;

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
}