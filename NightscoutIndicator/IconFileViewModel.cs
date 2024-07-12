using System.Drawing;
using System.IO;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using PropertyChanged;

namespace NightscoutIndicator;

/// <summary>
/// Icon file setting view model.
/// </summary>
[DoNotNotify]
public partial class IconFileViewModel : FileSettingViewModel
{
    [ObservableProperty]
    private ImageSource _preview;

    /// <summary>
    /// Initializes a new instance of the <see cref="IconFileViewModel"/> class.
    /// </summary>
    public IconFileViewModel(string title,
                             Func<NightscoutIndicatorSettings, string> getValue,
                             Action<NightscoutIndicatorSettings, string> setValue)
        : base(title, getValue, setValue)
    {
    }

    /// <inheritdoc />
    protected override string FileFilter => "Icon files (*.ico)|*.ico";

    /// <summary>
    /// Updates the preview of the selected icon.
    /// </summary>
    protected override void OnFileChanged()
    {
        Preview = LoadPreview(FullPath);
    }

    private static ImageSource LoadPreview(string fullPath)
    {
        if (!File.Exists(fullPath))
        {
            return null;
        }

        try
        {
            // The icon is loaded the same way as in the tray, so the preview shows what the tray will show
            using var icon = new Icon(fullPath, 32, 32);
            var source = Imaging.CreateBitmapSourceFromHIcon(icon.Handle, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
            source.Freeze();

            return source;
        }
        catch (Exception)
        {
            return null;
        }
    }
}