using System.Globalization;

namespace NightscoutIndicator.Nightscout;

public class NightscoutEntry : IEquatable<NightscoutEntry>
{
    public DateTimeOffset Timestamp { get; }
    public decimal Value { get; }
    public IList<short> Digits { get; }
    public NightscoutTrend Trend { get; set; }

    public NightscoutEntry(string line, NightscoutUnit unit)
    {
        try
        {
            IList<string> split = line.Split('\t').Select(x => x.Trim('"')).ToList();
            try
            {
                var unixTimestamp = long.Parse(split[1]);
                Timestamp = DateTimeOffset.FromUnixTimeMilliseconds(unixTimestamp).ToLocalTime();

//                Timestamp = DateTimeOffset.Parse(split[0]);
            }
            catch
            {
                var unixTimestamp = long.Parse(split[1]) / 1000;
                var epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
                Timestamp = epoch.AddSeconds(unixTimestamp);
            }
            var value = int.Parse(split[2]);
            // Nightscout always uses mg/dl even if DISPLAY_UNITS is set to mmol/L
            if (unit == NightscoutUnit.MgDl)
                Value = value;
            if (unit == NightscoutUnit.MmolL)
                Value = value * 0.0555m;
            Digits = GetDigits(Value, unit);
            if (!Enum.TryParse(split[3], out NightscoutTrend trend))
                trend = NightscoutTrend.None;
            Trend = trend;
        }
        catch (Exception ex)
        {
            throw new Exception("Parsing entry '" + line + "' failed.", ex);
        }
    }

    public NightscoutEntry(DateTimeOffset timestamp, decimal value, NightscoutUnit unit, NightscoutTrend trend)
    {
        Timestamp = timestamp;
        Value = value;
        Digits = GetDigits(Value, unit);
        Trend = trend;
    }

    private IList<short> GetDigits(decimal value, NightscoutUnit unit)
    {
        var stringValue = value.ToString(unit == NightscoutUnit.MmolL ? "F1" : "F0", CultureInfo.InvariantCulture);
        IList<short> digits = stringValue.Select(x => x == '.' ? (short)10 : (short)(x - 48)).ToList();
        return digits;
    }

    public bool Equals(NightscoutEntry other)
    {
        if (other == null)
            return false;
        else
            return other.Timestamp == Timestamp && other.Value == Value;
    }

    public override int GetHashCode()
    {
        return Timestamp.GetHashCode();
    }
}