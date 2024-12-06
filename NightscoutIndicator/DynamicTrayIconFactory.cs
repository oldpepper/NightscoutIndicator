using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Windows.Media.Imaging;
using NightscoutIndicator.Nightscout;
using FontStyle = System.Drawing.FontStyle;

namespace NightscoutIndicator;

public class DynamicTrayIconFactory
{
    private const string GLUCOSE_FONT_NAME = "League Gothic";
    private const string GLUCOSE_FONT_FILE_NAME = @"Fonts\LeagueGothic-Regular.ttf";

    // The collection must stay alive while its font family is used
    private static readonly PrivateFontCollection _privateFonts = new();
    private static readonly FontFamily _glucoseFontFamily = LoadGlucoseFontFamily();

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    public static extern bool DestroyIcon(IntPtr handle);

    [DllImport("Shcore.dll")]
    public static extern int GetDpiForMonitor(IntPtr hmonitor, MonitorDpiType dpiType, out uint dpiX, out uint dpiY);

    [DllImport("User32.dll")]
    public static extern IntPtr MonitorFromWindow(IntPtr hwnd, uint dwFlags);

    public enum MonitorDpiType
    {
        EffectiveDpi = 0, // Effective DPI (takes user settings into account)
        AngularDpi = 1,   // Angular DPI
        RawDpi = 2,       // Raw DPI
    }

    public static float GetScaleFactorForMainDisplay()
    {
        // Get the primary monitor handle
        IntPtr mainMonitor = MonitorFromWindow(IntPtr.Zero, 1); // MONITOR_DEFAULTTOPRIMARY

        // Get DPI
        GetDpiForMonitor(mainMonitor, MonitorDpiType.EffectiveDpi, out uint dpiX, out uint dpiY);

        return dpiX / 96.0f; // Return the scale factor
    }
    
    public Icon CreateGlucoseBitmapSource(string text, bool isOld = false)
    {
        // The icon is rendered at the tray icon size of the primary monitor, so the tray does not rescale it
        var size = Math.Max(16, (int)Math.Round(16 * GetScaleFactorForMainDisplay()));
        using var bmp = new Bitmap(size, size, PixelFormat.Format32bppArgb);
        using var g = Graphics.FromImage(bmp);

        g.Clear(Color.Transparent);
        g.SmoothingMode = SmoothingMode.AntiAlias;

        if (!string.IsNullOrEmpty(text))
        {
            DrawFittedText(g, text, new RectangleF(0, 0, size, size), isOld ? Brushes.Orchid : Brushes.White);
        }

        return CreateOwnedIcon(bmp);
    }

    public Icon CreateSingleIcon(string text, GlucoseLevel level, Color backgroundColor, NightscoutTrend trend, bool isOld = false)
    {
        var scale = GetScaleFactorForMainDisplay();
        var size = Math.Max(16, (int)Math.Round(16 * scale));
        // 2 px at 100% scale, 3 px at 125%, then proportionally
        var stripeHeight = Math.Max(2, (int)Math.Round(2 * scale, MidpointRounding.AwayFromZero));

        using var bmp = new Bitmap(size, size, PixelFormat.Format32bppArgb);
        using var g = Graphics.FromImage(bmp);

        g.Clear(backgroundColor);
        g.SmoothingMode = SmoothingMode.AntiAlias;

        // The stripe is drawn first, so the digits fill the whole height and overlap it
        var stripeMultiplier = GetStripeMultiplier(trend);
        if (stripeMultiplier > 0)
        {
            var isRising = trend is NightscoutTrend.FortyFiveUp or NightscoutTrend.SingleUp;
            var height = stripeHeight * stripeMultiplier;
            var top = isRising ? 0 : size - height;

            // A red stripe is poorly visible on the red extra high and the purple extra low backgrounds, so it is gold there
            var stripeBrush = level is GlucoseLevel.ExtraHigh or GlucoseLevel.ExtraLow ? Brushes.Gold : Brushes.Red;

            g.SmoothingMode = SmoothingMode.None;
            g.FillRectangle(stripeBrush, 0, top, size, height);
            g.SmoothingMode = SmoothingMode.AntiAlias;
        }

        if (!string.IsNullOrEmpty(text))
        {
            DrawFittedText(g, text, new RectangleF(0, 0, size, size), isOld ? Brushes.Orchid : Brushes.Black);
        }

        return CreateOwnedIcon(bmp);
    }

    /// <summary>
    /// Gets the trend stripe thickness in base stripe heights, or 0 if the trend has no stripe.
    /// </summary>
    private static int GetStripeMultiplier(NightscoutTrend trend)
    {
        return trend switch
        {
            NightscoutTrend.FortyFiveUp or NightscoutTrend.FortyFiveDown => 1,
            NightscoutTrend.SingleUp or NightscoutTrend.SingleDown => 2,
            _ => 0
        };
    }

    public Icon CreateTrendIcon(string iconFileName, Color color)
    {
        using var bmp = new Bitmap(32, 32, PixelFormat.Format32bppArgb);
        using var g = Graphics.FromImage(bmp);

        g.Clear(color);

        // A missing or broken icon file leaves only the zone color instead of breaking the tray update
        try
        {
            using var icon = new Icon(NightscoutIndicatorSettings.GetFullPath(iconFileName), 32, 32);
            g.DrawIcon(icon, new Rectangle(0, 0, 32, 32));
        }
        catch (Exception)
        {
            // ignored
        }

        // 3. bmp2 contains your image with the icon and text!
        // It can be saved or used as an ImageSource for WPF


        return CreateOwnedIcon(bmp);
    }

    /// <summary>
    /// Draws the text stretched to the full height of the area.
    /// </summary>
    private static void DrawFittedText(Graphics g, string text, RectangleF area, Brush brush)
    {
        // The text outline gives the exact glyph bounds without the empty space above and below the digits
        using var path = new GraphicsPath();
        path.AddString(text, _glucoseFontFamily, (int)FontStyle.Regular, 100f, PointF.Empty, StringFormat.GenericTypographic);

        var bounds = path.GetBounds();
        if (bounds.Width <= 0 || bounds.Height <= 0)
        {
            return;
        }

        // The digits fill the whole height without a margin, as the trend icon does; long values are narrowed to fit the width, short ones keep their proportions
        var scaleY = area.Height / bounds.Height;
        var scaleX = Math.Min(scaleY, area.Width / bounds.Width);

        // Matrix operations are prepended, so they apply to the points in reverse order
        using var matrix = new Matrix();
        matrix.Translate(area.X + area.Width / 2f, area.Y + area.Height / 2f);
        matrix.Scale(scaleX, scaleY);
        matrix.Translate(-(bounds.X + bounds.Width / 2f), -(bounds.Y + bounds.Height / 2f));
        path.Transform(matrix);

        g.FillPath(brush, path);
    }

    /// <summary>
    /// Loads the glucose font family.
    /// </summary>
    private static FontFamily LoadGlucoseFontFamily()
    {
        // The font shipped with the application is preferred, then the installed one, then the default sans serif font
        var fontPath = NightscoutIndicatorSettings.GetFullPath(GLUCOSE_FONT_FILE_NAME);
        if (File.Exists(fontPath))
        {
            try
            {
                _privateFonts.AddFontFile(fontPath);
                return _privateFonts.Families[0];
            }
            catch (Exception)
            {
                // ignored
            }
        }

        try
        {
            return new FontFamily(GLUCOSE_FONT_NAME);
        }
        catch (ArgumentException)
        {
            return FontFamily.GenericSansSerif;
        }
    }

    /// <summary>
    /// Creates an icon that owns its handle from the bitmap.
    /// </summary>
    private static Icon CreateOwnedIcon(Bitmap bmp)
    {
        var handle = bmp.GetHicon();
        try
        {
            // Icon.FromHandle does not own the handle, so clone it and destroy the original
            using var icon = Icon.FromHandle(handle);
            return (Icon)icon.Clone();
        }
        finally
        {
            DestroyIcon(handle);
        }
    }
}