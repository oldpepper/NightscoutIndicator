using System.IO;

namespace NightscoutIndicator.SettingsManagement;

/// <summary>
/// Location and initialization of the per-user settings file
/// (%LOCALAPPDATA%\NightscoutIndicator\usersettings.json).
/// </summary>
public static class UserSettingsFile
{
    /// <summary>
    /// Name of the default settings file shipped next to the executable.
    /// </summary>
    public const string DEFAULT_FILE_NAME = "appsettings.json";

    /// <summary>
    /// Name of the developer settings file (Debug builds only, not stored in git).
    /// </summary>
    public const string DEVELOPMENT_FILE_NAME = "appsettings.Development.json";

    private const string APP_FOLDER_NAME = "NightscoutIndicator";
    private const string USER_FILE_NAME = "usersettings.json";

    /// <summary>
    /// Full path to the per-user settings file.
    /// </summary>
    public static string FilePath { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        APP_FOLDER_NAME,
        USER_FILE_NAME);

    /// <summary>
    /// Creates the user settings file from the default settings on the first run.
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
}
