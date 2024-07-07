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
using PropertyChanged;

namespace NightscoutIndicator;

/// <summary>
/// Settings window view model.
/// </summary>
[DoNotNotify]
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
    private string _alarmHiFileName;

    [ObservableProperty]
    private string _alarmLowFileName;

    [ObservableProperty]
    private string _flyingIndicatorSize;

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
    /// Available glucose units.
    /// </summary>
    public NightscoutUnit[] Units { get; } = Enum.GetValues<NightscoutUnit>();

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
        AlarmHiFileName = _settings.AlarmHiFileName;
        AlarmLowFileName = _settings.AlarmLowFileName;
        FlyingIndicatorSize = ToText(_settings.FlyingIndicatorSize);
        ErrorText = null;
    }

    [RelayCommand]
    private void Save()
    {
        var errors = new List<string>();
        var newSettings = _settings.Clone();

        newSettings.NightscoutUrl = NightscoutUrl?.Trim() ?? string.Empty;
        if (newSettings.NightscoutUrl.Length > 0 && !IsHttpUrl(newSettings.NightscoutUrl))
        {
            errors.Add("Nightscout URL must be an absolute http(s) address.");
        }

        newSettings.ApiSecret = ApiSecret ?? string.Empty;
        newSettings.Unit = Unit;

        newSettings.UpdateInterval = ParsePositiveInt(UpdateInterval, "Update interval", errors);
        newSettings.DataExpirationTimeout = ParsePositiveInt(DataExpirationTimeout, "Data expiration timeout", errors);

        newSettings.ExtraLow = ParsePositiveDecimal(ExtraLow, "Extra low", errors);
        newSettings.Low = ParsePositiveDecimal(Low, "Low", errors);
        newSettings.High = ParsePositiveDecimal(High, "High", errors);
        newSettings.ExtraHigh = ParsePositiveDecimal(ExtraHigh, "Extra high", errors);
        var areThresholdsParsed = newSettings.ExtraLow > 0 && newSettings.Low > 0 && newSettings.High > 0 && newSettings.ExtraHigh > 0;
        var areThresholdsOrdered = newSettings.ExtraLow < newSettings.Low &&
                                   newSettings.Low < newSettings.High &&
                                   newSettings.High < newSettings.ExtraHigh;
        if (areThresholdsParsed && !areThresholdsOrdered)
        {
            errors.Add("Thresholds must grow: Extra low < Low < High < Extra high.");
        }

        newSettings.TimeRange = ParsePositiveInt(TimeRange, "Graph time range", errors);
        newSettings.Width = ParsePositiveInt(Width, "Graph width", errors);
        newSettings.Height = ParsePositiveInt(Height, "Graph height", errors);
        newSettings.FontSize = ParsePositiveInt(FontSize, "Graph font size", errors);

        newSettings.UseAlarm = UseAlarm;
        newSettings.AlarmInterval = (double)ParsePositiveDecimal(AlarmInterval, "Alarm interval", errors);
        newSettings.AlarmHiFileName = AlarmHiFileName?.Trim() ?? string.Empty;
        newSettings.AlarmLowFileName = AlarmLowFileName?.Trim() ?? string.Empty;
        if (newSettings.UseAlarm)
        {
            CheckAlarmFile(newSettings.AlarmHiFileName, "High alarm sound", errors);
            CheckAlarmFile(newSettings.AlarmLowFileName, "Low alarm sound", errors);
        }

        newSettings.FlyingIndicatorSize = ParsePositiveInt(FlyingIndicatorSize, "Indicator size", errors);

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
            ErrorText = $"Failed to save {UserSettingsFile.FilePath}:{Environment.NewLine}{ex.Message}";
            return;
        }

        _settings.CopyFrom(newSettings);
        ErrorText = null;

        SettingsSavedEvent?.Invoke();
        CloseRequestedEvent?.Invoke();
    }

    [RelayCommand]
    private void Cancel()
    {
        CloseRequestedEvent?.Invoke();
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

    private static int ParsePositiveInt(string text, string name, List<string> errors)
    {
        var isParsed = int.TryParse(text?.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var value);
        if (!isParsed || value <= 0)
        {
            errors.Add($"{name} must be a positive integer.");
            return 0;
        }

        return value;
    }

    private static decimal ParsePositiveDecimal(string text, string name, List<string> errors)
    {
        // Both '.' and ',' are accepted as the decimal separator
        var normalized = text?.Trim().Replace(',', '.');
        var isParsed = decimal.TryParse(normalized, NumberStyles.Number, CultureInfo.InvariantCulture, out var value);
        if (!isParsed || value <= 0)
        {
            errors.Add($"{name} must be a positive number.");
            return 0;
        }

        return value;
    }

    private static void CheckAlarmFile(string fileName, string name, List<string> errors)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            errors.Add($"{name} file is not set.");
            return;
        }

        // Relative names are resolved against the application folder, as in MainViewModel.PlayAlarm
        if (!File.Exists(Path.Combine(AppContext.BaseDirectory, fileName)))
        {
            errors.Add($"{name} file not found: {fileName}.");
        }
    }
}
