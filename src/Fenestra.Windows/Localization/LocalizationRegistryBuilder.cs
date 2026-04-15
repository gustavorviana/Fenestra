using Fenestra.Core;
using Fenestra.Core.Models;
using System.Resources;

namespace Fenestra.Windows.Localization;

/// <summary>
/// Pure helpers used by <c>AddWindowsLocalization</c> to register resource bundles into
/// <see cref="TranslationSource"/>. Split from the DI extension so the filtering,
/// alias-building, and duplicate-resolution logic can be unit-tested without assembling
/// a full DI container or relying on reflection against real embedded <c>.resources</c>
/// files.
/// </summary>
internal static class LocalizationRegistryBuilder
{
    private const string ResourcesSuffix = ".resources";
    private const string GeneratedXamlSuffix = ".g.resources";

    /// <summary>
    /// Enumerates the <c>(baseName, alias)</c> pairs that should be registered for a
    /// single auto-discovered assembly. Takes the raw list returned by
    /// <see cref="System.Reflection.Assembly.GetManifestResourceNames"/> (or any
    /// equivalent sequence in tests) and applies filtering, base-name normalization,
    /// and alias construction.
    /// </summary>
    /// <param name="manifestResourceNames">
    /// Raw manifest resource names from the target assembly.
    /// </param>
    /// <param name="prefix">
    /// Optional alias prefix (see <see cref="AutoDiscoverDefinition.Prefix"/>).
    /// </param>
    /// <param name="namespaceFilter">
    /// Optional namespace prefix used to include only bundles whose base name starts
    /// with it (see <see cref="AutoDiscoverDefinition.NamespaceFilter"/>).
    /// </param>
    /// <remarks>
    /// <para>Skipped:</para>
    /// <list type="bullet">
    ///   <item><description>Names that don't end with <c>.resources</c>.</description></item>
    ///   <item><description><c>.g.resources</c> (WPF compiled-XAML bundle — not a string resource).</description></item>
    ///   <item><description>Bundles that would produce an empty base name or an empty short name.</description></item>
    ///   <item><description>Bundles whose base name does not start with <paramref name="namespaceFilter"/>.</description></item>
    /// </list>
    /// </remarks>
    public static IEnumerable<(string BaseName, string Alias)> EnumerateAutoDiscovered(
        IEnumerable<string> manifestResourceNames,
        string? prefix,
        string? namespaceFilter = null)
    {
        if (manifestResourceNames is null)
            yield break;

        var lowerPrefix = string.IsNullOrWhiteSpace(prefix) ? null : prefix!.ToLowerInvariant();
        var filter = string.IsNullOrWhiteSpace(namespaceFilter) ? null : namespaceFilter;

        foreach (var name in manifestResourceNames)
        {
            if (string.IsNullOrEmpty(name))
                continue;

            // Must end with ".resources" — anchor to the end, don't substring-match
            // (older implementation used Replace which would strip any occurrence).
            if (!name.EndsWith(ResourcesSuffix, StringComparison.OrdinalIgnoreCase))
                continue;

            // Skip WPF compiled XAML bundle. It's always named "{AssemblyName}.g.resources"
            // and contains BAML, not string resources — picking it up would register a
            // bogus alias like "g" that silently fails at lookup.
            if (name.EndsWith(GeneratedXamlSuffix, StringComparison.OrdinalIgnoreCase))
                continue;

            var baseName = name.Substring(0, name.Length - ResourcesSuffix.Length);
            if (baseName.Length == 0)
                continue;

            if (filter is not null && !baseName.StartsWith(filter, StringComparison.OrdinalIgnoreCase))
                continue;

            var lastDot = baseName.LastIndexOf('.');
            var shortName = (lastDot >= 0 ? baseName.Substring(lastDot + 1) : baseName)
                .ToLowerInvariant();

            if (shortName.Length == 0)
                continue;

            var alias = lowerPrefix is not null
                ? $"{lowerPrefix}.{shortName}"
                : shortName;

            yield return (baseName, alias);
        }
    }

    /// <summary>
    /// Registers a single <see cref="ResourceManager"/> into <paramref name="target"/>
    /// under <paramref name="alias"/>, applying the configured
    /// <see cref="DuplicateResourceBehavior"/> when the alias is already taken.
    /// </summary>
    /// <param name="target">Destination registry (typically <see cref="TranslationSource.Instance"/>).</param>
    /// <param name="alias">The alias to try first. Matched case-insensitively.</param>
    /// <param name="manager">The resource manager being registered.</param>
    /// <param name="baseName">
    /// Fully-qualified base name of the bundle. Used as a fallback alias when the
    /// <see cref="DuplicateResourceBehavior.UseFullName"/> branch fires.
    /// </param>
    /// <param name="behavior">Conflict-resolution strategy.</param>
    /// <exception cref="InvalidOperationException">
    /// Thrown when <paramref name="behavior"/> is <see cref="DuplicateResourceBehavior.Throw"/>
    /// and the alias is already registered.
    /// </exception>
    public static void Register(
        TranslationSource target,
        string alias,
        ResourceManager manager,
        string baseName,
        DuplicateResourceBehavior behavior)
    {
        if (target is null) throw new ArgumentNullException(nameof(target));
        if (manager is null) throw new ArgumentNullException(nameof(manager));
        if (string.IsNullOrWhiteSpace(alias)) throw new ArgumentException("Alias is required.", nameof(alias));
        if (string.IsNullOrWhiteSpace(baseName)) throw new ArgumentException("Base name is required.", nameof(baseName));

        if (!target.HasResourceManager(alias))
        {
            target.AddResourceManager(alias, manager);
            return;
        }

        switch (behavior)
        {
            case DuplicateResourceBehavior.Replace:
                target.AddResourceManager(alias, manager);
                return;

            case DuplicateResourceBehavior.Throw:
                throw new InvalidOperationException(
                    $"Duplicate resource alias '{alias}' detected while registering '{baseName}'. " +
                    $"Use DuplicateResourceBehavior.Replace or DuplicateResourceBehavior.UseFullName " +
                    $"to avoid this, or register with explicit unique aliases via LocalizationOptions.AddResource.");

            case DuplicateResourceBehavior.UseFullName:
                // Register the new manager under its fully-qualified base name so both
                // bundles remain reachable. The previously-registered manager keeps the
                // short alias (winner-takes-short) — callers who need symmetric access
                // should register explicit aliases via AddResource instead.
                var fullAlias = baseName.ToLowerInvariant();
                target.AddResourceManager(fullAlias, manager);
                return;

            default:
                throw new ArgumentOutOfRangeException(nameof(behavior), behavior, "Unknown DuplicateResourceBehavior.");
        }
    }
}
