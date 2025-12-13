using NightscoutIndicator.Localization;
using System.Globalization;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Json;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using NightscoutIndicator.Nightscout;
using NightscoutIndicator.SettingsManagement;

namespace NightscoutIndicator;

/// <summary>
/// Settings window view model.
/// </summary>
public partial class SettingsViewModel : ObservableObject
{
    private readonly NightscoutIndicatorSettings _settings;
    private readonly IConfigurationRoot _configuration;

    [ObservableProperty]
    private string _nightscoutUrl;

    [ObservableProperty]
    private string _apiSecret;

    [ObservableProperty]
    private string _updateInterval;

    [ObservableProperty]
    private string _dataExpirationTimeout;

    [ObservableProperty]
    private NightscoutUnit _unit;

    [ObservableProperty]
    private string _extraLow;

    [ObservableProperty]
    private string _low;

    [ObservableProperty]
    private string _high;

    [ObservableProperty]
    private string _extraHigh;

    [ObservableProperty]
    private string _timeRange;

    [ObservableProperty]
    private string _width;

    [ObservableProperty]
    private string _height;

    [ObservableProperty]
    private string _fontSize;

    [ObservableProperty]
    private bool _useAlarm;

    [ObservableProperty]
    private string _alarmInterval;

    [ObservableProperty]
    private string _flyingIndicatorSize;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsTwoIconsMode))]
    private TrayIconMode _trayIconMode;

    [ObservableProperty]
    private string _language;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasError))]
    private string _errorText;

    /// <summary>
    /// Initializes a new instance of the <see cref="SettingsViewModel"/> class.
    /// </summary>
    public SettingsViewModel(IOptions<NightscoutIndicatorSettings> settings, IConfigurationRoot configuration)
    {
        _settings = settings.Value;
        _configuration = configuration;
    }

    /// <summary>
    /// Settings saved event.
    /// </summary>
    public event Action SettingsSavedEvent;

    /// <summary>
    /// Window close request event.
    /// </summary>
    public event Action CloseRequestedEvent;

    /// <summary>
    /// Available interface languages.
    /// </summary>
    public List<LanguageOption> Languages { get; } = LocalizationService.Instance.GetLanguages();

    /// <summary>
    /// Available glucose units.
    /// </summary>
    public NightscoutUnit[] Units { get; } = Enum.GetValues<NightscoutUnit>();

    /// <summary>
    /// Available tray icon modes.
    /// </summary>
    public TrayIconModeOption[] TrayIconModes { get; } =
    [
        new(TrayIconMode.TwoIcons, "Settings.TrayModeTwo"),
        new(TrayIconMode.SingleIcon, "Settings.TrayModeOne")
    ];

    /// <summary>
    /// Whether the tray shows two icons.
    /// </summary>
    public bool IsTwoIconsMode => TrayIconMode == TrayIconMode.TwoIcons;

    /// <summary>
    /// High alarm sound file.
    /// </summary>
    public SoundFileViewModel AlarmHiSound { get; } = new("Settings.AlarmHiSound", x => x.AlarmHiFileName, (x, v) => x.AlarmHiFileName = v);

    /// <summary>
    /// Low alarm sound file.
    /// </summary>
    public SoundFileViewModel AlarmLowSound { get; } = new("Settings.AlarmLowSound", x => x.AlarmLowFileName, (x, v) => x.AlarmLowFileName = v);

    /// <summary>
    /// Trend icon files.
    /// </summary>
    public IconFileViewModel[] TrendIcons { get; } =
    [
        new("Settings.TrendRisingFast", x => x.TrendIconUp, (x, v) => x.TrendIconUp = v),
        new("Settings.TrendRising", x => x.TrendIconUpRight, (x, v) => x.TrendIconUpRight = v),
        new("Settings.TrendStable", x => x.TrendIconFlat, (x, v) => x.TrendIconFlat = v),
        new("Settings.TrendFalling", x => x.TrendIconDownRight, (x, v) => x.TrendIconDownRight = v),
        new("Settings.TrendFallingFast", x => x.TrendIconDown, (x, v) => x.TrendIconDown = v),
        new("Settings.TrendError", x => x.TrendIconUnknown, (x, v) => x.TrendIconUnknown = v)
    ];

    /// <summary>
    /// Whether there is a validation error.
    /// </summary>
    public bool HasError => !string.IsNullOrEmpty(ErrorText);

    /// <summary>
    /// Loads the editable fields from the current settings.
    /// </summary>
    public void Load()
    {
        NightscoutUrl = _settings.NightscoutUrl;
        ApiSecret = _settings.ApiSecret;
        UpdateInterval = ToText(_settings.UpdateInterval);
        DataExpirationTimeout = ToText(_settings.DataExpirationTimeout);
        Unit = _settings.Unit;
        ExtraLow = ToText(_settings.ExtraLow);
        Low = ToText(_settings.Low);
        High = ToText(_settings.High);
        ExtraHigh = ToText(_settings.ExtraHigh);
        TimeRange = ToText(_settings.TimeRange);
        Width = ToText(_settings.Width);
        Height = ToText(_settings.Height);
        FontSize = ToText(_settings.FontSize);
        UseAlarm = _settings.UseAlarm;
        AlarmInterval = ToText(_settings.AlarmInterval);
        FlyingIndicatorSize = ToText(_settings.FlyingIndicatorSize);
        TrayIconMode = _settings.TrayIconMode;
        Language = LocalizationService.Instance.ResolveLanguage(_settings.Language);

        var defaults = NightscoutIndicatorSettings.LoadDefaults();
        AlarmHiSound.Load(_settings, defaults);
        AlarmLowSound.Load(_settings, defaults);
        foreach (var trendIcon in TrendIcons)
        {
            trendIcon.Load(_settings, defaults);
        }

        ErrorText = null;
    }

    /// <summary>
    /// Restores the language of the saved settings.
    /// </summary>
    public void RevertLanguage()
    {
        // The language is switched while the window is open to show the result at once, so unsaved changes are rolled back
        LocalizationService.Instance.SetLanguage(_settings.Language);
    }

    partial void OnLanguageChanged(string value)
    {
        LocalizationService.Instance.SetLanguage(value);
    }

    [RelayCommand]
    private void Save()
    {
        var errors = new List<string>();
        var newSettings = _settings.Clone();

        newSettings.NightscoutUrl = NightscoutUrl?.Trim() ?? string.Empty;
        if (newSettings.NightscoutUrl.Length > 0 && !IsHttpUrl(newSettings.NightscoutUrl))
        {
            errors.Add(Localization["Validation.UrlInvalid"]);
        }

        newSettings.ApiSecret = ApiSecret ?? string.Empty;
        newSettings.Unit = Unit;

        newSettings.UpdateInterval = ParsePositiveInt(UpdateInterval, "Validation.NameUpdateInterval", errors);
        newSettings.DataExpirationTimeout = ParsePositiveInt(DataExpirationTimeout, "Validation.NameDataExpiration", errors);

        newSettings.ExtraLow = ParsePositiveDecimal(ExtraLow, "Settings.ExtraLow", errors);
        newSettings.Low = ParsePositiveDecimal(Low, "Settings.Low", errors);
        newSettings.High = ParsePositiveDecimal(High, "Settings.High", errors);
        newSettings.ExtraHigh = ParsePositiveDecimal(ExtraHigh, "Settings.ExtraHigh", errors);
        var areThresholdsParsed = newSettings.ExtraLow > 0 && newSettings.Low > 0 && newSettings.High > 0 && newSettings.ExtraHigh > 0;
        var areThresholdsOrdered = newSettings.ExtraLow < newSettings.Low &&
                                   newSettings.Low < newSettings.High &&
                                   newSettings.High < newSettings.ExtraHigh;
        if (areThresholdsParsed && !areThresholdsOrdered)
        {
            errors.Add(Localization["Validation.ThresholdsOrder"]);
        }

        newSettings.TimeRange = ParsePositiveInt(TimeRange, "Validation.NameTimeRange", errors);
        newSettings.Width = ParsePositiveInt(Width, "Validation.NameWidth", errors);
        newSettings.Height = ParsePositiveInt(Height, "Validation.NameHeight", errors);
        newSettings.FontSize = ParsePositiveInt(FontSize, "Validation.NameFontSize", errors);

        newSettings.UseAlarm = UseAlarm;
        newSettings.AlarmInterval = (double)ParsePositiveDecimal(AlarmInterval, "Validation.NameAlarmInterval", errors);
        AlarmHiSound.Store(newSettings);
        AlarmLowSound.Store(newSettings);
        if (newSettings.UseAlarm)
        {
            CheckAlarmFile(newSettings.AlarmHiFileName, AlarmHiSound.Title, errors);
            CheckAlarmFile(newSettings.AlarmLowFileName, AlarmLowSound.Title, errors);
        }

        newSettings.FlyingIndicatorSize = ParsePositiveInt(FlyingIndicatorSize, "Validation.NameIndicatorSize", errors);
        newSettings.TrayIconMode = TrayIconMode;
        newSettings.Language = Language;

        foreach (var trendIcon in TrendIcons)
        {
            trendIcon.Store(newSettings);
            CheckIconFile(trendIcon.FileName, Localization.Format("Validation.IconName", trendIcon.Title), errors);
        }

        if (errors.Count > 0)
        {
            ErrorText = string.Join(Environment.NewLine, errors);
            return;
        }

        try
        {
            UserSettingsFile.WriteSection(NightscoutIndicatorSettings.SECTION_NAME, BuildUserSection(newSettings));
        }
        catch (Exception ex)
        {
            ErrorText = Localization.Format("Validation.SaveFailed", UserSettingsFile.FilePath, ex.Message);
            return;
        }

        _settings.CopyFrom(newSettings);
        ErrorText = null;

        StopSounds();
        SettingsSavedEvent?.Invoke();
        CloseRequestedEvent?.Invoke();
    }

    [RelayCommand]
    private void Cancel()
    {
        StopSounds();
        CloseRequestedEvent?.Invoke();
    }

    /// <summary>
    /// Stops the sound playback.
    /// </summary>
    public void StopSounds()
    {
        AlarmHiSound.Stop();
        AlarmLowSound.Stop();
    }

    /// <summary>
    /// Builds the settings section for the user settings file.
    /// </summary>
    private JObject BuildUserSection(NightscoutIndicatorSettings newSettings)
    {
        // Unchanged values from the developer settings file are replaced with the previous user values,
        // so developer secrets do not leak into the user settings file
        var serializer = JsonSerializer.Create(new JsonSerializerSettings { Converters = { new StringEnumConverter() } });
        var section = JObject.FromObject(newSettings, serializer);

        var overrides = GetDevelopmentOverrides();
        if (overrides.Count == 0)
        {
            return section;
        }

        var previousUserSection = UserSettingsFile.ReadSection(NightscoutIndicatorSettings.SECTION_NAME);

        foreach (var (key, developmentValue) in overrides)
        {
            var property = section.Property(key, StringComparison.OrdinalIgnoreCase);
            if (property == null || !string.Equals(ToInvariantString(property.Value), developmentValue, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var previousValue = previousUserSection?.GetValue(key, StringComparison.OrdinalIgnoreCase);
            if (previousValue != null)
            {
                property.Value = previousValue.DeepClone();
            }
            else
            {
                property.Remove();
            }
        }

        return section;
    }

    /// <summary>
    /// Gets the settings defined in the developer settings file.
    /// </summary>
    private Dictionary<string, string> GetDevelopmentOverrides()
    {
        // The developer file is loaded in Debug builds only, so in Release the result is always empty
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        var provider = _configuration.Providers
                                     .OfType<JsonConfigurationProvider>()
                                     .FirstOrDefault(x => string.Equals(x.Source.Path, UserSettingsFile.DEVELOPMENT_FILE_NAME, StringComparison.OrdinalIgnoreCase));
        if (provider == null)
        {
            return result;
        }

        var sectionName = NightscoutIndicatorSettings.SECTION_NAME;
        foreach (var key in provider.GetChildKeys(Enumerable.Empty<string>(), sectionName).Distinct(StringComparer.OrdinalIgnoreCase))
        {
            if (provider.TryGet(ConfigurationPath.Combine(sectionName, key), out var value))
            {
                result[key] = value;
            }
        }

        return result;
    }

    private static LocalizationService Localization => LocalizationService.Instance;

    private static string ToInvariantString(JToken token)
    {
        if (token is JValue value)
        {
            return Convert.ToString(value.Value, CultureInfo.InvariantCulture);
        }

        return token.ToString(Formatting.None);
    }

    private static string ToText(IFormattable value)
    {
        return value.ToString(null, CultureInfo.InvariantCulture);
    }

    private static bool IsHttpUrl(string text)
    {
        var isAbsolute = Uri.TryCreate(text, UriKind.Absolute, out var uri);

        return isAbsolute && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
    }

    private static int ParsePositiveInt(string text, string nameKey, List<string> errors)
    {
        var isParsed = int.TryParse(text?.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var value);
        if (!isParsed || value <= 0)
        {
            errors.Add(Localization.Format("Validation.PositiveInt", Localization[nameKey]));
            return 0;
        }

        return value;
    }

    private static decimal ParsePositiveDecimal(string text, string nameKey, List<string> errors)
    {
        // Both '.' and ',' are accepted as the decimal separator
        var normalized = text?.Trim().Replace(',', '.');
        var isParsed = decimal.TryParse(normalized, NumberStyles.Number, CultureInfo.InvariantCulture, out var value);
        if (!isParsed || value <= 0)
        {
            errors.Add(Localization.Format("Validation.PositiveNumber", Localization[nameKey]));
            return 0;
        }

        return value;
    }

    private static void CheckIconFile(string fileName, string name, List<string> errors)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            errors.Add(Localization.Format("Validation.FileNotSet", name));
            return;
        }

        var fullPath = NightscoutIndicatorSettings.GetFullPath(fileName.Trim());
        if (!File.Exists(fullPath))
        {
            errors.Add(Localization.Format("Validation.FileNotFound", name, fileName));
            return;
        }

        try
        {
            using var icon = new System.Drawing.Icon(fullPath, 32, 32);
        }
        catch (Exception)
        {
            errors.Add(Localization.Format("Validation.IconInvalid", name, fileName));
        }
    }

    private static void CheckAlarmFile(string fileName, string name, List<string> errors)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            errors.Add(Localization.Format("Validation.FileNotSet", name));
            return;
        }

        // Relative names are resolved against the application folder, as in MainViewModel.PlayAlarm
        if (!File.Exists(Path.Combine(AppContext.BaseDirectory, fileName)))
        {
            errors.Add(Localization.Format("Validation.FileNotFound", name, fileName));
        }
    }
}
