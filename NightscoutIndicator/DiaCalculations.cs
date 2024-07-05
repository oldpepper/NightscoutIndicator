using Microsoft.Extensions.Options;
using System.Drawing;

namespace NightscoutIndicator;

public class DiaCalculations
{
    private readonly NightscoutIndicatorSettings _settings;

    /// <summary>
    /// Initializes a new instance of the <see cref="DiaCalculations"/> class.
    /// </summary>
    public DiaCalculations(IOptions<NightscoutIndicatorSettings> settings)
    {
        _settings = settings.Value;
    }

    public Color CalculateLevelDrawingColor(GlucoseLevel level)
    {
        return level switch
        {
            GlucoseLevel.ExtraHigh => Color.Red,
            GlucoseLevel.ExtraLow => Color.DarkOrchid,
            GlucoseLevel.Low => Color.DodgerBlue,
            GlucoseLevel.Normal => Color.ForestGreen,
            GlucoseLevel.High => Color.DarkOrange,
            _ => Color.White
        };
    }

    public System.Windows.Media.Color CalculateLevelMediaColor(GlucoseLevel level)
    {
        return level switch
        {
            GlucoseLevel.ExtraHigh => System.Windows.Media.Colors.Red,
            GlucoseLevel.ExtraLow => System.Windows.Media.Colors.DarkOrchid,
            GlucoseLevel.Low => System.Windows.Media.Colors.DodgerBlue,
            GlucoseLevel.Normal => System.Windows.Media.Colors.ForestGreen,
            GlucoseLevel.High => System.Windows.Media.Colors.DarkOrange,
            _ => System.Windows.Media.Colors.White
        };
    }

    public GlucoseLevel CalculateLevel(decimal? glucoseValue)
    {
        return glucoseValue switch
        {
            null => GlucoseLevel.Unknown,
            _ when glucoseValue < _settings.ExtraLow => GlucoseLevel.ExtraLow,
            _ when glucoseValue < _settings.Low => GlucoseLevel.Low,
            _ when glucoseValue < _settings.High => GlucoseLevel.Normal,
            _ when glucoseValue < _settings.ExtraHigh => GlucoseLevel.High,
            _ when glucoseValue >= _settings.ExtraHigh => GlucoseLevel.ExtraHigh,
            _ => GlucoseLevel.Unknown
        };
    }
}