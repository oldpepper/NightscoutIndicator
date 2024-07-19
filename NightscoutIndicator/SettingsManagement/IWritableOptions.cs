using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Newtonsoft.Json.Linq;

namespace NightscoutIndicator.SettingsManagement;

public interface IWritableOptions<out TOptions> : IOptions<TOptions> where TOptions : class, new()
{
    Task UpdateAsync(Action<TOptions> applyChanges);
    Task UpdateAsync(string sectionName, Action<TOptions> applyChanges);
}

public class WritableOptions<T> : IWritableOptions<T> where T : class, new()
{
//    private readonly IHostEnvironment _environment;
    private readonly IOptions<T> _options;
    private readonly IConfigurationRoot _configuration;
    private readonly string _section;

    public WritableOptions(
        IOptions<T> options,
        IConfigurationRoot configuration,
        string section)
    {
        _options = options;
        _configuration = configuration;
        _section = section;
    }

    public T Value => _options.Value;

    public Task UpdateAsync(Action<T> applyChanges)
    {
        return UpdateAsync(_section, applyChanges);
    }

    /// <summary>
    /// Applies the changes to the current options and writes them to the user settings file.
    /// </summary>
    public Task UpdateAsync(string sectionName, Action<T> applyChanges)
    {
        // The changes are applied to the live options object, so the application sees exactly what is written
        var value = Value ?? new T();
        applyChanges(value);

        UserSettingsFile.WriteSection(sectionName, JObject.FromObject(value));

        // Force configuration reload
        _configuration.Reload();

        return Task.CompletedTask;
    }
}
