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
/// Tray icon mode with its display title.
/// </summary>
public record TrayIconModeOption(TrayIconMode Mode, string Title);
