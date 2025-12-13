using System.ComponentModel;
using System.IO;
using Newtonsoft.Json;

namespace NightscoutIndicator.Localization;

/// <summary>
/// Localization service.
/// </summary>
/// <remarks>
/// Texts are read from the JSON files in the Localization folder next to the exe: the file name is the language code,
/// the "Language.Name" key is the language title. A missing key falls back to the default language, then to the key itself.
/// </remarks>
public class LocalizationService : INotifyPropertyChanged
{
    /// <summary>
    /// Default language code.
    /// </summary>
    public const string DEFAULT_LANGUAGE = "en-US";

    private const string FOLDER_NAME = "Localization";
    private const string LANGUAGE_NAME_KEY = "Language.Name";

    private Dictionary<string, string> _texts = new();
    private Dictionary<string, string> _defaultTexts = new();

    /// <summary>
    /// Shared instance.
    /// </summary>
    public static LocalizationService Instance { get; } = new();

    /// <inheritdoc />
    public event PropertyChangedEventHandler PropertyChanged;

    /// <summary>
    /// Current language code.
    /// </summary>
    public string CurrentLanguage { get; private set; } = DEFAULT_LANGUAGE;

    /// <summary>
    /// Gets the text by the key.
    /// </summary>
    public string this[string key]
    {
        get
        {
            if (key != null && _texts.TryGetValue(key, out var text))
            {
                return text;
            }

            if (key != null && _defaultTexts.TryGetValue(key, out var defaultText))
            {
                return defaultText;
            }

            return key;
        }
    }

    /// <summary>
    /// Gets the text by the key and inserts the arguments.
    /// </summary>
    public string Format(string key, params object[] args)
    {
        try
        {
            return string.Format(this[key], args);
        }
        catch (FormatException)
        {
            return this[key];
        }
    }

    /// <summary>
    /// Gets the languages available in the Localization folder.
    /// </summary>
    public List<LanguageOption> GetLanguages()
    {
        var result = new List<LanguageOption>();

        var folder = Path.Combine(AppContext.BaseDirectory, FOLDER_NAME);
        if (!Directory.Exists(folder))
        {
            return result;
        }

        foreach (var file in Directory.GetFiles(folder, "*.json").OrderBy(x => x, StringComparer.OrdinalIgnoreCase))
        {
            var code = Path.GetFileNameWithoutExtension(file);
            var texts = ReadFile(file);
            var name = texts.GetValueOrDefault(LANGUAGE_NAME_KEY) ?? code;
            result.Add(new LanguageOption(code, name));
        }

        return result;
    }

    /// <summary>
    /// Gets the code of an available language that matches the given code, e.g. "ru" gives "ru-RU".
    /// </summary>
    public string ResolveLanguage(string code)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            return DEFAULT_LANGUAGE;
        }

        var languages = GetLanguages();
        var trimmed = code.Trim();
        var exact = languages.FirstOrDefault(x => string.Equals(x.Code, trimmed, StringComparison.OrdinalIgnoreCase));
        var byPrefix = languages.FirstOrDefault(x => x.Code.StartsWith(trimmed + "-", StringComparison.OrdinalIgnoreCase));

        return (exact ?? byPrefix)?.Code ?? DEFAULT_LANGUAGE;
    }

    /// <summary>
    /// Switches the language and notifies the bound texts.
    /// </summary>
    public void SetLanguage(string code)
    {
        var languageCode = ResolveLanguage(code);

        _defaultTexts = ReadFile(GetFilePath(DEFAULT_LANGUAGE));
        _texts = string.Equals(languageCode, DEFAULT_LANGUAGE, StringComparison.OrdinalIgnoreCase)
            ? _defaultTexts
            : ReadFile(GetFilePath(languageCode));
        CurrentLanguage = languageCode;

        // "Item[]" refreshes every binding to the indexer
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Item[]"));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CurrentLanguage)));
    }

    private static string GetFilePath(string code)
    {
        return Path.Combine(AppContext.BaseDirectory, FOLDER_NAME, code + ".json");
    }

    private static Dictionary<string, string> ReadFile(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                var texts = JsonConvert.DeserializeObject<Dictionary<string, string>>(File.ReadAllText(path));
                if (texts != null)
                {
                    return texts;
                }
            }
        }
        catch (Exception)
        {
            // A broken file is treated as an empty one: the texts fall back to the default language
        }

        return new Dictionary<string, string>();
    }
}

/// <summary>
/// Language code with its title.
/// </summary>
public record LanguageOption(string Code, string Name);
