using System.Globalization;

namespace Fenestra.Windows;

/// <summary>
/// Tracks the current application culture, persists it across launches, and exposes a
/// runtime <see cref="SetCulture"/> method with change notifications.
///
/// <para>
/// <b>Scope</b>: this service handles <i>which</i> culture the app should use. It does
/// <b>not</b> implement XAML string binding — that's the job of libraries like
/// <c>WPFLocalizeExtension</c>. Subscribe to <see cref="CultureChanged"/> to re-render
/// your views or hook into your preferred XAML localization library.
/// </para>
///
/// <para>
/// Must be resolved at startup (typically in <c>OnReady</c> or the constructor of a
/// long-lived service) so the persisted culture is applied before any UI materializes.
/// </para>
/// </summary>
public interface ILocalizationService
{
    /// <summary>
    /// Current application culture. Reflects the last <see cref="SetCulture"/> call, the
    /// persisted value from a previous launch, or the initial culture resolved at startup.
    /// </summary>
    CultureInfo CurrentCulture { get; }

    /// <summary>
    /// The set of cultures this app supports, as configured via
    /// <see cref="LocalizationOptions.Supported"/>.
    /// </summary>
    IReadOnlyList<CultureInfo> SupportedCultures { get; }

    /// <summary>
    /// Changes the current culture. Persists the selection to the registry, updates
    /// <see cref="CultureInfo.CurrentCulture"/>, <see cref="CultureInfo.CurrentUICulture"/>,
    /// and the <c>DefaultThreadCurrent*</c> variants, and raises <see cref="CultureChanged"/>.
    /// </summary>
    /// <exception cref="ArgumentNullException">When <paramref name="culture"/> is null.</exception>
    /// <exception cref="ArgumentException">
    /// When <paramref name="culture"/> is not in <see cref="SupportedCultures"/>.
    /// </exception>
    void SetCulture(CultureInfo culture);

    /// <summary>
    /// Raised after <see cref="CurrentCulture"/> changes. Fired synchronously on the thread
    /// that called <see cref="SetCulture"/>. Consumers that need to update UI should marshal
    /// via <c>IThreadContext.InvokeAsync</c> or their framework's dispatcher.
    /// </summary>
    event EventHandler<CultureChangedEventArgs>? CultureChanged;
}

/// <summary>
/// Payload for <see cref="ILocalizationService.CultureChanged"/>.
/// </summary>
public sealed class CultureChangedEventArgs : EventArgs
{
    public CultureInfo OldCulture { get; }
    public CultureInfo NewCulture { get; }

    public CultureChangedEventArgs(CultureInfo oldCulture, CultureInfo newCulture)
    {
        OldCulture = oldCulture ?? throw new ArgumentNullException(nameof(oldCulture));
        NewCulture = newCulture ?? throw new ArgumentNullException(nameof(newCulture));
    }
}

/// <summary>
/// Configuration for <see cref="ILocalizationService"/>.
/// </summary>
public sealed class LocalizationOptions
{
    /// <summary>
    /// Culture names this app supports (e.g., <c>"en-US"</c>, <c>"pt-BR"</c>).
    /// Must contain at least one entry and each name must be a valid
    /// <see cref="CultureInfo"/> identifier.
    /// </summary>
    public string[] Supported { get; set; } = Array.Empty<string>();

    /// <summary>
    /// Fallback culture when nothing is persisted and the OS culture is not in
    /// <see cref="Supported"/>. Must be one of the entries in <see cref="Supported"/>.
    /// </summary>
    public string Default { get; set; } = "";
}
