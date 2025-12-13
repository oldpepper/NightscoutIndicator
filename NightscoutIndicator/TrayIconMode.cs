using NightscoutIndicator.Localization;
using CommunityToolkit.Mvvm.ComponentModel;

namespace NightscoutIndicator;

/// <summary>
/// Tray icon display mode.
/// </summary>
public enum TrayIconMode
{
    TwoIcons,
    SingleIcon
}

/// <summary>
/// Tray icon mode with its localized title.
/// </summary>
public class TrayIconModeOption : ObservableObject
{
    private readonly string _titleKey;

    /// <summary>
    /// Initializes a new instance of the <see cref="TrayIconModeOption"/> class.
    /// </summary>
    public TrayIconModeOption(TrayIconMode mode, string titleKey)
    {
        Mode = mode;
        _titleKey = titleKey;

        // Options live as long as the application, so the subscription is never released
        LocalizationService.Instance.PropertyChanged += (_, _) => OnPropertyChanged(nameof(Title));
    }

    /// <summary>
    /// Tray icon mode.
    /// </summary>
    public TrayIconMode Mode { get; }

    /// <summary>
    /// Localized title.
    /// </summary>
    public string Title => LocalizationService.Instance[_titleKey];
}
