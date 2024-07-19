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
            DrawFittedText(g, text, size, isOld ? Brushes.Orchid : Brushes.White);
        }

        return CreateOwnedIcon(bmp);
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
    /// Draws the text stretched to the full height of the icon.
    /// </summary>
    private static void DrawFittedText(Graphics g, string text, int size, Brush brush)
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
        float available = size;
        var scaleY = available / bounds.Height;
        var scaleX = Math.Min(scaleY, available / bounds.Width);

        // Matrix operations are prepended, so they apply to the points in reverse order
        using var matrix = new Matrix();
        matrix.Translate(size / 2f, size / 2f);
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