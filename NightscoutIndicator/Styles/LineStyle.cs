using System.Drawing;
using NightscoutIndicator.Nightscout;

namespace NightscoutIndicator.Styles
{
    public abstract class LineStyle : BaseStyle
    {
        protected override void DrawEntries(Graphics g, int width, int height, List<NightscoutEntry> entries, decimal low, decimal high, DateTimeOffset minX, DateTimeOffset maxX, int minY, int maxY)
        {
            int lastX = 0, lastY = 0;

            for (var i = 0; i < entries.Count; i++)
            {
                var x = (int)IntervalScale(minX.Ticks, maxX.Ticks, entries[i].Timestamp.Ticks, 0, width - 1);
                var y = (int)IntervalScale(minY, maxY, entries[i].Value, 0, height - 1);
                var color = GetGraphColor(low, high, entries[i].Value);

                if (i != 0)
                    g.DrawLine(new Pen(color), lastX, lastY, x, y);
                g.FillRectangle(new SolidBrush(color), x - Radius, y - Radius, Radius * 2, Radius * 2);

                lastX = x; lastY = y;
            }
        }
    }
}
