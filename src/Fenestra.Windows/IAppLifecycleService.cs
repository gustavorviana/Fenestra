namespace Fenestra.Windows;

/// <summary>
/// Tracks first-run and version-upgrade state for a Fenestra application, persisted in
/// the Windows Registry under <c>HKCU\SOFTWARE\{AppName}\Lifecycle</c>.
///
/// <para>
/// The values are a <b>snapshot captured when the service is constructed</b>. Resolve the
/// service at startup (typically in <c>OnReady</c> or the constructor of a long-lived
/// service) so that <see cref="LaunchCount"/> increments exactly once per launch.
/// </para>
///
/// <para>
/// Typical usage:
/// </para>
/// <code>
/// if (lifecycle.IsFirstRun) ShowOnboarding();
/// else if (lifecycle.IsFirstRunOfVersion) ShowChangelog(from: lifecycle.PreviousVersion!);
/// </code>
/// </summary>
public interface IAppLifecycleService
{
    /// <summary>
    /// <c>true</c> when this is the first time the app has ever run for the current Windows user.
    /// </summary>
    bool IsFirstRun { get; }

    /// <summary>
    /// <c>true</c> when this is the first time the current <see cref="Version"/> has run.
    /// Also <c>true</c> on the very first run (<see cref="IsFirstRun"/>).
    /// Use the idiom <c>if (IsFirstRun) ... else if (IsFirstRunOfVersion) ...</c> to avoid
    /// showing onboarding and changelog simultaneously on the very first launch.
    /// </summary>
    bool IsFirstRunOfVersion { get; }

    /// <summary>
    /// The version that was stored before this launch. Non-null only when <see cref="IsFirstRun"/>
    /// is <c>false</c> AND <see cref="IsFirstRunOfVersion"/> is <c>true</c> — i.e., a real upgrade.
    /// Useful for showing per-version changelogs.
    /// </summary>
    Version? PreviousVersion { get; }

    /// <summary>
    /// UTC timestamp of the first time the app was ever run for this user.
    /// Persisted across launches; never updated after the initial write.
    /// </summary>
    DateTimeOffset FirstInstallDate { get; }

    /// <summary>
    /// Number of times the app has been launched, incremented on every construction of the service.
    /// <c>1</c> on the first run, <c>2</c> on the second, etc.
    /// </summary>
    int LaunchCount { get; }
}
