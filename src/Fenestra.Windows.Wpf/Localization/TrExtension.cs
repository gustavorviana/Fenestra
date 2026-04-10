using Fenestra.Windows.Localization;
using System.Windows.Data;
using System.Windows.Markup;

namespace Fenestra.Wpf.Localization;

/// <summary>
/// XAML markup extension for resource-based localization bindings.
///
/// <para>
/// Usage: <c>{fenestra:Tr messages, Greeting}</c>, where <c>messages</c> is a
/// <see cref="System.Resources.ResourceManager"/> name previously registered on
/// <see cref="TranslationSource.Instance"/> and <c>Greeting</c> is the resource key
/// inside that manager.
/// </para>
///
/// <para>
/// Returns a one-way <see cref="Binding"/> to <c>TranslationSource.Instance[Resource, Key]</c>.
/// The binding re-evaluates whenever the source raises <c>PropertyChanged("Item[]")</c>,
/// which <see cref="TranslationSource.Invalidate"/> does. Wire
/// <c>ILocalizationService.CultureChanged</c> to <c>Invalidate</c> at startup and
/// every <c>{fenestra:Tr ...}</c> in the UI updates automatically when the user picks
/// a new language.
/// </para>
/// </summary>
[MarkupExtensionReturnType(typeof(object))]
public class TrExtension : MarkupExtension
{
    /// <summary>
    /// Name of the <see cref="System.Resources.ResourceManager"/> to use (as registered
    /// via <see cref="TranslationSource.AddResourceManager"/>).
    /// </summary>
    public string Resource { get; set; } = string.Empty;

    /// <summary>
    /// The resource key to look up inside <see cref="Resource"/>.
    /// </summary>
    public string Key { get; set; } = string.Empty;

    public TrExtension() { }

    /// <summary>
    /// Positional constructor: <c>{fenestra:Tr messages, Greeting}</c>.
    /// </summary>
    public TrExtension(string resource, string key)
    {
        Resource = resource;
        Key = key;
    }

    public override object ProvideValue(IServiceProvider serviceProvider)
    {
        var binding = new Binding($"[{Resource},{Key}]")
        {
            Source = TranslationSource.Instance,
            Mode = BindingMode.OneWay,
        };
        return binding.ProvideValue(serviceProvider);
    }
}
