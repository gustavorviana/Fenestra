using Fenestra.Core.Models;

namespace Fenestra.Windows.Services;

/// <summary>
/// <see cref="IAppLifecycleService"/> implementation backed by <see cref="IRegistryConfig"/>.
///
/// <para>
/// The constructor reads the existing state from <c>HKCU\SOFTWARE\{AppName}\Lifecycle</c>,
/// computes the first-run / version-upgrade booleans, increments <c>LaunchCount</c>, and
/// writes the updated state back. All public properties are a snapshot of the moment the
/// service was constructed and are immutable for the lifetime of the instance.
/// </para>
///
/// <para>
/// Corrupted registry values (e.g., manual edits, schema mismatch) are handled defensively
/// by <see cref="IRegistryConfig.TryGet{T}"/>: unparseable dates or versions make
/// <c>TryGet</c> return <c>false</c>, so the affected field behaves as if it were missing,
/// never throwing out of the constructor. Conversion (e.g., <see cref="DateTimeOffset"/>
/// ↔ ISO 8601 round-trip string, <see cref="Version"/> ↔ string) is handled natively by
/// <see cref="RegistryConfigService"/>.
/// </para>
/// </summary>
internal sealed class AppLifecycleService : IAppLifecycleService
{
    private const string SectionName = "Lifecycle";
    private const string KeyFirstInstallDate = "FirstInstallDate";
    private const string KeyLastVersion = "LastVersion";
    private const string KeyLaunchCount = "LaunchCount";

    public bool IsFirstRun { get; }
    public bool IsFirstRunOfVersion { get; }
    public Version? PreviousVersion { get; }
    public DateTimeOffset FirstInstallDate { get; }
    public int LaunchCount { get; }

    public AppLifecycleService(AppInfo appInfo, IRegistryConfig config)
    {
        Platform.EnsureWindows();
        if (appInfo is null) throw new ArgumentNullException(nameof(appInfo));
        if (config is null) throw new ArgumentNullException(nameof(config));

        using var section = config.GetSection(SectionName, createIfNotExists: true)!;

        // TryGet<T> returns false for missing or un-parseable values — no manual
        // parsing or format handling needed here.
        var hasInstallDate = section.TryGet<DateTimeOffset>(KeyFirstInstallDate, out var storedInstallDate);
        var hasLastVersion = section.TryGet<Version>(KeyLastVersion, out var storedLastVersion);
        var storedLaunchCount = section.Get<int>(KeyLaunchCount);

        IsFirstRun = !hasInstallDate;
        IsFirstRunOfVersion = IsFirstRun || !hasLastVersion || storedLastVersion != appInfo.Version;

        // PreviousVersion is non-null ONLY on real upgrades (not on first run ever).
        PreviousVersion = (IsFirstRun || !IsFirstRunOfVersion) ? null : storedLastVersion;
        FirstInstallDate = hasInstallDate ? storedInstallDate : DateTimeOffset.UtcNow;
        LaunchCount = storedLaunchCount + 1;

        // Persist updated state using native types — RegistryConfigService handles the
        // DateTimeOffset → ISO 8601 string and Version → string conversion internally.
        if (IsFirstRun)
            section.Set(KeyFirstInstallDate, FirstInstallDate);

        section.Set(KeyLastVersion, appInfo.Version);
        section.Set(KeyLaunchCount, LaunchCount);
    }
}
