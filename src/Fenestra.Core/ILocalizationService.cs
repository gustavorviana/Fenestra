using Fenestra.Core.Models;
using System.Globalization;
using System.Reflection;

namespace Fenestra.Core;

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
/// Configuration for <see cref="ILocalizationService"/>. Declares the supported cultures,
/// the default culture, and the resource bundles used for UI string lookups.
/// </summary>
public sealed class LocalizationOptions
{
    /// <summary>
    /// IETF culture names the app supports (e.g. <c>"en-US"</c>, <c>"pt-BR"</c>).
    /// Must contain at least one entry and must include <see cref="Default"/>.
    /// </summary>
    public string[] Supported { get; set; } = Array.Empty<string>();

    /// <summary>
    /// Fallback culture used when neither a persisted value nor the OS culture matches
    /// an entry in <see cref="Supported"/>. Must be a member of <see cref="Supported"/>.
    /// </summary>
    public string Default { get; set; } = "";

    /// <summary>
    /// How the resource registry should react when two resources resolve to the same alias.
    /// Defaults to <see cref="DuplicateResourceBehavior.Throw"/> so misconfiguration is
    /// caught at startup rather than producing silently-wrong translations.
    /// </summary>
    public DuplicateResourceBehavior DuplicateBehavior { get; set; }
        = DuplicateResourceBehavior.Throw;

    /// <summary>
    /// Manually-declared resources, each mapping an alias to a <c>.resources</c> base name
    /// inside a specific assembly. Populated via <see cref="AddResource"/>.
    /// </summary>
    public List<ResourceDefinition> Resources { get; } = new();

    /// <summary>
    /// Assemblies the loader should scan at startup to find <c>.resources</c> bundles
    /// automatically. Populated via <see cref="AutoDiscoverFrom"/>.
    /// </summary>
    public List<AutoDiscoverDefinition> AutoDiscoverAssemblies { get; } = new();

    /// <summary>
    /// Registers a single resource bundle by alias. Use when you need explicit control
    /// over the alias a <c>.resources</c> file gets (e.g. to decouple UI bindings from
    /// the physical namespace layout).
    /// </summary>
    /// <param name="alias">Short name used by bindings (e.g. <c>"messages"</c>).</param>
    /// <param name="baseName">
    /// Fully-qualified base name of the <c>.resources</c> bundle
    /// (e.g. <c>"MyApp.Resources.Messages"</c>, without the <c>.resources</c> suffix).
    /// </param>
    /// <param name="assembly">The assembly that embeds the bundle.</param>
    public void AddResource(string alias, string baseName, Assembly assembly)
    {
        Resources.Add(new ResourceDefinition(alias, baseName, assembly));
    }

    /// <summary>
    /// Enables auto-discovery of every <c>.resources</c> bundle embedded in
    /// <paramref name="assembly"/>. Each discovered bundle is registered under its
    /// short name (the last segment of its base name) — optionally prefixed by
    /// <paramref name="prefix"/> and optionally scoped to a sub-namespace by
    /// <paramref name="namespaceFilter"/>.
    ///
    /// <para>
    /// WPF's compiled-XAML bundle (<c>.g.resources</c>) and the standard
    /// <c>Properties.Resources.resources</c> are skipped automatically because they are
    /// not string <see cref="ResourceManager"/> compatible in the auto-discovery sense.
    /// </para>
    /// </summary>
    /// <param name="assembly">The assembly whose manifest is scanned.</param>
    /// <param name="prefix">
    /// Optional alias prefix prepended to every discovered resource. When set, an
    /// alias becomes <c>{prefix}.{shortName}</c> — useful to disambiguate bundles from
    /// multiple assemblies that happen to share short names.
    /// </param>
    /// <param name="namespaceFilter">
    /// Optional namespace prefix used to include only bundles whose base name starts
    /// with the given value (case-insensitive). For example,
    /// <c>namespaceFilter: "MyApp.Resources"</c> restricts discovery to bundles directly
    /// under that namespace.
    /// </param>
    public void AutoDiscoverFrom(Assembly assembly, string? prefix = null, string? namespaceFilter = null)
    {
        AutoDiscoverAssemblies.Add(new AutoDiscoverDefinition(assembly, prefix, namespaceFilter));
    }
}

/// <summary>
/// Fully-qualified reference to a single resource bundle: a base name inside an assembly,
/// exposed under a developer-chosen <see cref="Alias"/>.
/// </summary>
/// <param name="Alias">Short name used by bindings at runtime.</param>
/// <param name="BaseName">
/// Fully-qualified base name of the bundle (without the <c>.resources</c> suffix).
/// </param>
/// <param name="Assembly">The assembly that embeds the bundle.</param>
public sealed record ResourceDefinition(
    string Alias,
    string BaseName,
    Assembly Assembly);

/// <summary>
/// Describes an assembly to be scanned at startup by the localization loader.
/// </summary>
/// <param name="Assembly">The assembly whose manifest is scanned.</param>
/// <param name="Prefix">
/// Optional alias prefix prepended to every discovered resource.
/// </param>
/// <param name="NamespaceFilter">
/// Optional namespace prefix that restricts discovery to bundles whose base name starts
/// with the given value (case-insensitive). <c>null</c> disables filtering.
/// </param>
public sealed record AutoDiscoverDefinition(
    Assembly Assembly,
    string? Prefix,
    string? NamespaceFilter = null);