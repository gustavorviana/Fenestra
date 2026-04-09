using System.Resources;

namespace Fenestra.Sample.BuilderStyle.Resources;

/// <summary>
/// Typed accessors for strings in <c>Strings.resx</c> and its locale variants
/// (<c>Strings.pt-BR.resx</c>, <c>Strings.es-ES.resx</c>).
///
/// <para>
/// The underlying <see cref="ResourceManager"/> picks the right satellite assembly based on
/// <see cref="System.Globalization.CultureInfo.CurrentUICulture"/> automatically. When
/// Fenestra's <c>ILocalizationService.SetCulture</c> changes the UI culture, the very next
/// call to any accessor here returns the new language.
/// </para>
/// </summary>
internal static class Strings
{
    private static readonly ResourceManager _rm = new(
        baseName: "Fenestra.Sample.BuilderStyle.Resources.Strings",
        assembly: typeof(Strings).Assembly);

    public static string Greeting
        => _rm.GetString(nameof(Greeting)) ?? nameof(Greeting);

    public static string LanguageLabel
        => _rm.GetString(nameof(LanguageLabel)) ?? nameof(LanguageLabel);

    public static string ChangeCulturePrompt
        => _rm.GetString(nameof(ChangeCulturePrompt)) ?? nameof(ChangeCulturePrompt);

    public static string WelcomeMessage
        => _rm.GetString(nameof(WelcomeMessage)) ?? nameof(WelcomeMessage);
}
