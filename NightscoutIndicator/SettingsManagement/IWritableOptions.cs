using System.IO;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
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
    private readonly string _file;

    public WritableOptions(
        IOptions<T> options,
        IConfigurationRoot configuration,
        string section,
        string file = "appsettings.json")
    {
        _options = options;
        _configuration = configuration;
        _section = section;
        _file = file;
    }

    public T Value => _options.Value;

    public async Task UpdateAsync(Action<T> applyChanges)
    {
        await UpdateAsync(_section, applyChanges);
    }

    public async Task UpdateAsync(string sectionName, Action<T> applyChanges)
    {
        var jObject = JsonConvert.DeserializeObject<JObject>(await File.ReadAllTextAsync(_file));
        var sectionObject = jObject.TryGetValue(sectionName, out JToken section) ?
                                JsonConvert.DeserializeObject<T>(section.ToString()) : (Value ?? new T());

        applyChanges(sectionObject);

        jObject[sectionName] = JObject.FromObject(_options.Value);

        await File.WriteAllTextAsync(_file, JsonConvert.SerializeObject(jObject, Formatting.Indented));

        // Force configuration reload
        _configuration.Reload();
    }
}
