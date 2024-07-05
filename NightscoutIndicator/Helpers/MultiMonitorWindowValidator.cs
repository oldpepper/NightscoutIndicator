using System;
using System.Drawing;
using System.Linq;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Interop;
using System.Windows.Media;

public static class MultiMonitorWindowValidator
{
    /// <summary>
    /// Validates and corrects the window position for a multi-monitor configuration.
    /// </summary>
    /// <param name="windowLeft">Window left position</param>
    /// <param name="windowTop">Window top position</param>
    /// <param name="windowWidth">Window width</param>
    /// <param name="windowHeight">Window height</param>
    /// <param name="correctedBounds">Corrected window bounds</param>
    /// <returns>True if the position was corrected</returns>
    public static bool ValidateAndCorrectWindowPosition(
        ref double windowLeft,
        ref double windowTop,
        double windowWidth,
        double windowHeight,
        out Rect correctedBounds)
    {
        correctedBounds = new Rect(windowLeft, windowTop, windowWidth, windowHeight);

        // Get information about all screens
        var screens = Screen.AllScreens;
        if (!screens.Any())
        {
            // Fallback to the primary screen parameters
            windowLeft = 0;
            windowTop = 0;
            correctedBounds = new Rect(0, 0, windowWidth, windowHeight);
            return true;
        }

        // Check whether the window is at least partially on any screen
        bool isVisible = IsWindowVisibleOnAnyScreen(windowLeft, windowTop, windowWidth, windowHeight, screens);

        if (!isVisible)
        {
            // The window is completely outside all screens - move it to the primary screen
            var primaryScreen = Screen.PrimaryScreen;
            var correctedPosition = CenterWindowOnScreen(primaryScreen, windowWidth, windowHeight);

            windowLeft = correctedPosition.X;
            windowTop = correctedPosition.Y;
            correctedBounds = new Rect(windowLeft, windowTop, windowWidth, windowHeight);
            return true;
        }

        // The window is visible but may be partially out of bounds - correct it
        var targetScreen = FindBestScreenForWindow(windowLeft, windowTop, windowWidth, windowHeight, screens);
        var adjustedPosition = EnsureWindowFitsOnScreen(
            windowLeft, windowTop, windowWidth, windowHeight, targetScreen);

        if (Math.Abs(adjustedPosition.X - windowLeft) > 1 || Math.Abs(adjustedPosition.Y - windowTop) > 1)
        {
            windowLeft = adjustedPosition.X;
            windowTop = adjustedPosition.Y;
            correctedBounds = new Rect(windowLeft, windowTop, windowWidth, windowHeight);
            return true;
        }

        return false; // No correction needed
    }

    /// <summary>
    /// Checks whether the window is visible on any screen.
    /// </summary>
    private static bool IsWindowVisibleOnAnyScreen(
        double left, double top, double width, double height, Screen[] screens)
    {
        var windowRect = new Rectangle((int)left, (int)top, (int)width, (int)height);

        foreach (var screen in screens)
        {
            // Check the intersection with the screen working area
            var intersection = Rectangle.Intersect(windowRect, screen.WorkingArea);
            if (!intersection.IsEmpty)
            {
                // The window is considered visible if an area of at least 100x50 pixels is visible
                if (width/intersection.Width >= 0.3 && height/intersection.Height >= 0.3)
                    return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Finds the best screen to place the window on.
    /// </summary>
    private static Screen FindBestScreenForWindow(
        double left, double top, double width, double height, Screen[] screens)
    {
        var windowRect = new Rectangle((int)left, (int)top, (int)width, (int)height);
        var windowCenter = new System.Drawing.Point(
            (int)(left + width / 2),
            (int)(top + height / 2));

        Screen bestScreen = null;
        int maxIntersectionArea = 0;

        foreach (var screen in screens)
        {
            // If the window center is on this screen, it is the preferred choice
            if (screen.Bounds.Contains(windowCenter))
                return screen;

            // Otherwise find the screen with the largest intersection area
            var intersection = Rectangle.Intersect(windowRect, screen.WorkingArea);
            int intersectionArea = intersection.Width * intersection.Height;

            if (intersectionArea > maxIntersectionArea)
            {
                maxIntersectionArea = intersectionArea;
                bestScreen = screen;
            }
        }

        return bestScreen ?? Screen.PrimaryScreen;
    }

    /// <summary>
    /// Centers the window on the specified screen taking DPI into account.
    /// </summary>
    private static System.Drawing.Point CenterWindowOnScreen(
        Screen screen, double windowWidth, double windowHeight)
    {
        // Get the screen working area
        var workingArea = screen.WorkingArea;

        // Calculate the centered position
        int left = workingArea.Left + (workingArea.Width - (int)windowWidth) / 2;
        int top = workingArea.Top + (workingArea.Height - (int)windowHeight) / 2;

        // Make sure the window does not go out of bounds
        left = Math.Max(workingArea.Left, Math.Min(left, workingArea.Right - (int)windowWidth));
        top = Math.Max(workingArea.Top, Math.Min(top, workingArea.Bottom - (int)windowHeight));

        return new System.Drawing.Point(left, top);
    }

    /// <summary>
    /// Corrects the window position so that it fits on the screen.
    /// </summary>
    private static System.Drawing.Point EnsureWindowFitsOnScreen(
        double left, double top, double width, double height, Screen screen)
    {
        var workingArea = screen.WorkingArea;

        // Correct the left position
        double correctedLeft = left;
        if (left < workingArea.Left)
            correctedLeft = workingArea.Left;
        else if (left + width > workingArea.Right)
            correctedLeft = workingArea.Right - width;

        // Correct the top position
        double correctedTop = top;
        if (top < workingArea.Top)
            correctedTop = workingArea.Top;
        else if (top + height > workingArea.Bottom)
            correctedTop = workingArea.Bottom - height;

        // If the window is larger than the screen, place it in the top-left corner of the working area
        if (width > workingArea.Width)
            correctedLeft = workingArea.Left;
        if (height > workingArea.Height)
            correctedTop = workingArea.Top;

        return new System.Drawing.Point((int)correctedLeft, (int)correctedTop);
    }

    /// <summary>
    /// Gets the DPI scale of the screen for WPF.
    /// </summary>
    public static double GetDpiScaleForScreen(Screen screen, Window window = null)
    {
        try
        {
            if (window != null)
            {
                var source = PresentationSource.FromVisual(window);
                if (source?.CompositionTarget != null)
                {
                    var dpiScale = VisualTreeHelper.GetDpi(window);
                    return Math.Max(dpiScale.DpiScaleX, dpiScale.DpiScaleY);
                }
            }

            // Fallback: use the primary system DPI
            using (var graphics = Graphics.FromHwnd(IntPtr.Zero))
            {
                return graphics.DpiX / 96.0; // 96 DPI is the standard value
            }
        }
        catch
        {
            return 1.0; // Fallback to 100% scaling
        }
    }

    /// <summary>
    /// Checks whether the window's screen is disconnected.
    /// </summary>
    public static bool IsTargetScreenAvailable(string deviceName)
    {
        if (string.IsNullOrEmpty(deviceName))
            return false;

        return Screen.AllScreens.Any(s => s.DeviceName.Equals(deviceName, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Gets the virtual bounds of all screens.
    /// </summary>
    public static Rectangle GetVirtualScreenBounds()
    {
        int left = Screen.AllScreens.Min(s => s.Bounds.Left);
        int top = Screen.AllScreens.Min(s => s.Bounds.Top);
        int right = Screen.AllScreens.Max(s => s.Bounds.Right);
        int bottom = Screen.AllScreens.Max(s => s.Bounds.Bottom);

        return new Rectangle(left, top, right - left, bottom - top);
    }

    /// <summary>
    /// Converts WPF coordinates to screen coordinates taking DPI into account.
    /// </summary>
    public static System.Drawing.Point ConvertWpfToScreenCoordinates(
        double wpfX, double wpfY, Window window)
    {
        try
        {
            var point = new System.Windows.Point(wpfX, wpfY);
            var screenPoint = window.PointToScreen(point);
            return new System.Drawing.Point((int)screenPoint.X, (int)screenPoint.Y);
        }
        catch
        {
            return new System.Drawing.Point((int)wpfX, (int)wpfY);
        }
    }

    /// <summary>
    /// Gets the monitor information by the window handle.
    /// </summary>
    public static Screen GetScreenFromWindowHandle(Window window)
    {
        try
        {
            var handle = new WindowInteropHelper(window).Handle;
            if (handle != IntPtr.Zero)
            {
                return Screen.FromHandle(handle);
            }
        }
        catch { }

        return Screen.PrimaryScreen;
    }
}
