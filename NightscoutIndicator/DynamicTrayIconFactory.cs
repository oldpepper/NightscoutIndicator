using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Windows.Media.Imaging;
using NightscoutIndicator.Nightscout;
using FontStyle = System.Drawing.FontStyle;

namespace NightscoutIndicator;

public class DynamicTrayIconFactory
{
    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    public static extern bool DestroyIcon(IntPtr handle);

    [System.Runtime.InteropServices.DllImport("gdi32.dll")]
    public static extern bool DeleteObject(IntPtr hObject);
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
        using var bmp = new Bitmap(32, 32, PixelFormat.Format32bppArgb);
        using var g = Graphics.FromImage(bmp);

        g.Clear(Color.Transparent);
        using var font = new Font("League Gothic", 18, FontStyle.Bold);

        g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;
        var scaleFactor = GetScaleFactorForMainDisplay();
        g.ScaleTransform(0.8f/ scaleFactor, 1.7f/ scaleFactor); // Shrink width to 80%, stretch height by 70%
        var textSize = g.MeasureString(text, font);
        var x = (bmp.Width - textSize.Width) / 2f;
        var y = (bmp.Height - textSize.Height) / 2f;
        g.DrawString(text, font, isOld ? Brushes.Orchid : Brushes.White, 0, -6);
        g.ResetTransform(); // Important!

        return CreateOwnedIcon(bmp);
    }

    public Icon CreateTrendIcon(NightscoutTrend trend, Color color)
    {
        var icoFileName = trend switch
        {
            NightscoutTrend.DoubleUp => "Icon\\arrow-up2.ico",
            NightscoutTrend.FortyFiveUp => "Icon\\arrow-up-right2.ico",
            NightscoutTrend.Flat => "Icon\\arrow-right2.ico",
            NightscoutTrend.FortyFiveDown => "Icon\\arrow-down-right2.ico",
            NightscoutTrend.DoubleDown => "Icon\\arrow-down2.ico",
            _ => "Icon\\icon-red.ico"
        };

        using var icon = new Icon(icoFileName, 32, 32);
        using var bmp = new Bitmap(32, 32, PixelFormat.Format32bppArgb);
        using var g = Graphics.FromImage(bmp);

        g.Clear(color);

        // 1. Draw the icon in the top-left corner or center it if needed
        g.DrawIcon(icon, new Rectangle(0, 0, 32, 32));

        // 3. bmp2 contains your image with the icon and text!
        // It can be saved or used as an ImageSource for WPF


        return CreateOwnedIcon(bmp);
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