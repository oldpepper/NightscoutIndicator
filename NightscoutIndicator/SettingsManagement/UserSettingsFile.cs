using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace NightscoutIndicator.SettingsManagement;

/// <summary>
/// User settings file.
/// </summary>
public static class UserSettingsFile
{
    /// <summary>
    /// Name of the default settings file shipped next to the executable.
    /// </summary>
    public const string DEFAULT_FILE_NAME = "appsettings.json";

    /// <summary>
    /// Name of the developer settings file.
    /// </summary>
    public const string DEVELOPMENT_FILE_NAME = "appsettings.Development.json";

    private const string APP_FOLDER_NAME = "NightscoutIndicator";
    private const string USER_FILE_NAME = "usersettings.json";

    // The file is written from the UI thread (Save) and from timer threads (indicator position)
    private static readonly object _syncRoot = new();

    /// <summary>
    /// Full path to the per-user settings file.
    /// </summary>
    public static string FilePath { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        APP_FOLDER_NAME,
        USER_FILE_NAME);

    /// <summary>
    /// Creates the user settings file from the default settings.
    /// </summary>
    /// <param name="defaultFilePath">Full path to the default settings file.</param>
    public static void EnsureCreated(string defaultFilePath)
    {
        if (File.Exists(FilePath))
        {
            return;
        }

        Directory.CreateDirectory(Path.GetDirectoryName(FilePath));

        if (File.Exists(defaultFilePath))
        {
            File.Copy(defaultFilePath, FilePath);
        }
        else
        {
            File.WriteAllText(FilePath, "{}");
        }
    }

    /// <summary>
    /// Reads a section of the user settings file.
    /// </summary>
    /// <param name="sectionName">Name of the section.</param>
    /// <returns>The section or null if the file or the section does not exist.</returns>
    public static JObject ReadSection(string sectionName)
    {
        lock (_syncRoot)
        {
            var root = ReadRoot();

            return root[sectionName] as JObject;
        }
    }

    /// <summary>
    /// Writes a section of the user settings file.
    /// </summary>
    /// <param name="sectionName">Name of the section.</param>
    /// <param name="section">New content of the section.</param>
    public static void WriteSection(string sectionName, JObject section)
    {
        // Reading and writing under one lock keeps the other sections written by concurrent callers
        lock (_syncRoot)
        {
            var root = ReadRoot();
            root[sectionName] = section;

            Directory.CreateDirectory(Path.GetDirectoryName(FilePath));
            File.WriteAllText(FilePath, root.ToString(Formatting.Indented));
        }
    }

    private static JObject ReadRoot()
    {
        if (!File.Exists(FilePath))
        {
            return new JObject();
        }

        var text = File.ReadAllText(FilePath);
        if (string.IsNullOrWhiteSpace(text))
        {
            return new JObject();
        }

        return JObject.Parse(text);
    }
}
