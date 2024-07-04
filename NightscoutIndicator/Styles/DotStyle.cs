using System.Drawing;
using System.Drawing.Drawing2D;
using NightscoutIndicator.Nightscout;

namespace NightscoutIndicator.Styles
{
    public abstract class DotStyle : BaseStyle
    {
        protected override void DrawEntries(Graphics g, int width, int height, List<NightscoutEntry> entries, decimal low, decimal high, DateTimeOffset minX, DateTimeOffset maxX, int minY, int maxY)
        {
            g.SmoothingMode = SmoothingMode.HighQuality;

            for (var i = 0; i < entries.Count; i++)
            {
                var x = (int)IntervalScale(minX.Ticks, maxX.Ticks, entries[i].Timestamp.Ticks, 0, width - 1);
                var y = (int)IntervalScale(minY, maxY, entries[i].Value, 0, height - 1);

                var color = GetGraphColor(low, high, entries[i].Value);
                g.FillEllipse(new SolidBrush(color), x - Radius, y - Radius, Radius * 2, Radius * 2);
            }

            g.SmoothingMode = SmoothingMode.None;
        }
    }
}
