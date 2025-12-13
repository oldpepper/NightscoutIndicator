using System.Windows.Data;
using System.Windows.Markup;

namespace NightscoutIndicator.Localization;

/// <summary>
/// Markup extension that binds a XAML property to a localized text.
/// </summary>
[MarkupExtensionReturnType(typeof(string))]
public class LocExtension : MarkupExtension
{
    /// <summary>
    /// Initializes a new instance of the <see cref="LocExtension"/> class.
    /// </summary>
    public LocExtension(string key)
    {
        Key = key;
    }

    /// <summary>
    /// Text key.
    /// </summary>
    public string Key { get; set; }

    /// <inheritdoc />
    public override object ProvideValue(IServiceProvider serviceProvider)
    {
        var binding = new Binding($"[{Key}]")
        {
            Source = LocalizationService.Instance,
            Mode = BindingMode.OneWay
        };

        return binding.ProvideValue(serviceProvider);
    }
}
