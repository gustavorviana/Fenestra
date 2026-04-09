using System.Globalization;

namespace Fenestra.Windows.Services;

/// <summary>
/// <see cref="ILocalizationService"/> implementation backed by <see cref="IRegistryConfig"/>.
///
/// <para>
/// The constructor validates <see cref="LocalizationOptions"/>, resolves the initial culture
/// (persisted → OS → default, in priority order), and applies it to the process. Runtime
/// changes via <see cref="SetCulture"/> are persisted and broadcast to subscribers.
/// </para>
/// </summary>
internal sealed class LocalizationService : ILocalizationService
{
    private const string SectionName = "Localization";
    private const string KeySelectedCulture = "SelectedCulture";

    private readonly IRegistryConfig _config;
    private readonly CultureInfo[] _supported;
    private readonly CultureInfo _default;
    private CultureInfo _current;

    public event EventHandler<CultureChangedEventArgs>? CultureChanged;

    public CultureInfo CurrentCulture => _current;
    public IReadOnlyList<CultureInfo> SupportedCultures => _supported;

    public LocalizationService(LocalizationOptions options, IRegistryConfig config)
    {
        Platform.EnsureWindows();
        if (options is null) throw new ArgumentNullException(nameof(options));
        if (config is null) throw new ArgumentNullException(nameof(config));

        _config = config;

        // Validate and materialize Supported
        if (options.Supported is null || options.Supported.Length == 0)
            throw new ArgumentException(
                "At least one supported culture is required.", nameof(options));

        _supported = new CultureInfo[options.Supported.Length];
        for (int i = 0; i < options.Supported.Length; i++)
        {
            try
            {
                _supported[i] = CultureInfo.GetCultureInfo(options.Supported[i]);
            }
            catch (CultureNotFoundException ex)
            {
                throw new ArgumentException(
                    $"'{options.Supported[i]}' is not a valid CultureInfo name.",
                    nameof(options),
                    ex);
            }
        }

        // Validate Default is present in Supported
        if (string.IsNullOrWhiteSpace(options.Default))
            throw new ArgumentException("Default culture is required.", nameof(options));

        _default = FindSupported(options.Default)
            ?? throw new ArgumentException(
                $"Default culture '{options.Default}' is not in the Supported list.",
                nameof(options));

        // Resolve initial culture in priority order: persisted → OS → default.
        _current = ResolveInitialCulture();

        // Apply without raising the event — this is the initial state, not a change.
        ApplyCulture(_current);
    }

    private CultureInfo ResolveInitialCulture()
    {
        // Priority 1: persisted value from a previous launch.
        using (var section = _config.GetSection(SectionName, createIfNotExists: true)!)
        {
            if (section.TryGet<string>(KeySelectedCulture, out var persisted)
                && !string.IsNullOrWhiteSpace(persisted))
            {
                var match = FindSupported(persisted!);
                if (match is not null) return match;
            }
        }

        // Priority 2: OS culture (exact match, then 2-letter language fallback).
        var osMatch = FindSupported(CultureInfo.CurrentUICulture.Name)
                   ?? FindSupported(CultureInfo.CurrentUICulture.TwoLetterISOLanguageName);
        if (osMatch is not null) return osMatch;

        // Priority 3: dev-configured default.
        return _default;
    }

    private CultureInfo? FindSupported(string name)
    {
        if (string.IsNullOrEmpty(name)) return null;
        foreach (var c in _supported)
        {
            if (c.Name.Equals(name, StringComparison.OrdinalIgnoreCase)) return c;
        }
        return null;
    }

    public void SetCulture(CultureInfo culture)
    {
        if (culture is null) throw new ArgumentNullException(nameof(culture));

        var match = FindSupported(culture.Name);
        if (match is null)
        {
            throw new ArgumentException(
                $"Culture '{culture.Name}' is not in the supported list. " +
                $"Supported: {string.Join(", ", _supported.Select(c => c.Name))}.",
                nameof(culture));
        }

        if (_current.Equals(match)) return; // no-op — don't persist or fire the event

        var old = _current;
        _current = match;

        ApplyCulture(match);
        Persist(match);

        CultureChanged?.Invoke(this, new CultureChangedEventArgs(old, match));
    }

    private static void ApplyCulture(CultureInfo culture)
    {
        // Affects the current thread immediately.
        Thread.CurrentThread.CurrentCulture = culture;
        Thread.CurrentThread.CurrentUICulture = culture;

        // Affects newly created threads — running threads that already cached their
        // culture won't see the change until they next read it.
        CultureInfo.DefaultThreadCurrentCulture = culture;
        CultureInfo.DefaultThreadCurrentUICulture = culture;
    }

    private void Persist(CultureInfo culture)
    {
        using var section = _config.GetSection(SectionName, createIfNotExists: true)!;
        section.Set(KeySelectedCulture, culture.Name);
    }
}
