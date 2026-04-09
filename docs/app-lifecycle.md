# App Lifecycle (First Run & Upgrade Detection)

Tracks first-run state, version upgrades, install date, and launch count for a Fenestra application. Useful for showing onboarding wizards on first run, changelogs on upgrade, migrating settings between versions, or powering retention analytics.

## Overview

Every Fenestra app eventually needs to answer:

- *Is this the first time the user has ever opened this app?* — show onboarding
- *Did the user upgrade from a previous version?* — show changelog, migrate config
- *When did the user first install the app?* — retention analytics
- *How many times has the user launched the app?* — engagement metrics

Without a standard service, developers end up scribbling their own flags into the registry or `IRegistryConfig` manually, each with a different convention. `IAppLifecycleService` provides the canonical implementation, backed by `HKCU\SOFTWARE\{AppName}\Lifecycle`.

## Registration

```csharp
var builder = FenestraApplication.CreateBuilder(args);
builder.UseMainWindow<MainWindow>();
builder.Services.AddWindowsAppLifecycle();
builder.RegisterWindows();
```

## Usage patterns

### Show onboarding on first run

```csharp
protected override void OnReady(IServiceProvider services, Window mainWindow)
{
    var lifecycle = services.GetRequiredService<IAppLifecycleService>();

    if (lifecycle.IsFirstRun)
    {
        ShowOnboardingWizard();
    }
}
```

### Show changelog on upgrade

```csharp
if (!lifecycle.IsFirstRun && lifecycle.IsFirstRunOfVersion)
{
    ShowChangelog(from: lifecycle.PreviousVersion!, to: appInfo.Version);
}
```

### The `if / else if` idiom (avoid showing both)

```csharp
if (lifecycle.IsFirstRun)
{
    ShowOnboardingWizard();
}
else if (lifecycle.IsFirstRunOfVersion)
{
    ShowChangelog(lifecycle.PreviousVersion!);
}
```

On the very first launch both `IsFirstRun` and `IsFirstRunOfVersion` are `true`. The `else if` ensures the user sees onboarding once, not onboarding + an empty changelog dialog.

### Retention analytics

```csharp
var daysSinceInstall = (DateTimeOffset.UtcNow - lifecycle.FirstInstallDate).TotalDays;
Analytics.Track("session_start", new
{
    launch_count = lifecycle.LaunchCount,
    days_since_install = daysSinceInstall,
});
```

## State machine

The service captures a snapshot at construction time. Here's how the properties evolve across launches:

| Scenario | IsFirstRun | IsFirstRunOfVersion | PreviousVersion | LaunchCount | FirstInstallDate |
|---|---|---|---|---|---|
| First launch ever | `true` | `true` | `null` | 1 | now |
| Second launch (same version) | `false` | `false` | `null` | 2 | (preserved) |
| First launch of a new version (upgrade) | `false` | `true` | old version | N+1 | (preserved) |
| Second launch of the new version | `false` | `false` | `null` | N+2 | (preserved) |

**Rule**: `PreviousVersion` is non-null **only** when `IsFirstRun == false` AND `IsFirstRunOfVersion == true` — in other words, only on a real upgrade. On the very first run (`IsFirstRun == true`), there is no "previous" version, so the property returns `null`.

## How it works

The constructor of `AppLifecycleService` does the following in one atomic sequence:

1. Opens the `Lifecycle` subkey under `HKCU\SOFTWARE\{AppName}` (creating it if needed).
2. Reads the stored `FirstInstallDate`, `LastVersion`, and `LaunchCount`.
3. Computes `IsFirstRun`, `IsFirstRunOfVersion`, `PreviousVersion` based on the stored state vs. the current `AppInfo.Version`.
4. Increments `LaunchCount` by 1.
5. Writes the updated `LastVersion` and `LaunchCount` back to the registry. If this is the first run, also writes `FirstInstallDate`.
6. Closes the registry subkey.

All public properties are snapshots of the moment of construction. They do **not** change during the lifetime of the instance — the next launch (next construction) sees new values.

### Defensive parsing

If the registry contains corrupted values (user edited manually, schema mismatch, another app with the same name wrote garbage), the service treats them defensively:

- Unparseable `FirstInstallDate` → treated as "never set" → `IsFirstRun = true`
- Unparseable `LastVersion` → treated as "never set" → `IsFirstRunOfVersion = true`
- Out-of-range `LaunchCount` → read as `0` by default, becomes `1` after increment

The service **never throws** from the constructor due to registry content. Worst case, you get a "fresh install" snapshot, which is safe.

## Registry layout

```
HKEY_CURRENT_USER\
  SOFTWARE\
    {AppName}\
      Lifecycle\
        FirstInstallDate  (REG_SZ)  "2024-03-15T10:30:00.0000000+00:00"
        LastVersion       (REG_SZ)  "2.0"
        LaunchCount       (REG_DWORD)  42
```

All three values are plain and human-readable in `regedit`, which is useful for debugging.

## Critical caveats

- **Must be resolved at startup** — the service is a lazy singleton. If nobody resolves `IAppLifecycleService`, `LaunchCount` never increments and the registry state never updates. Resolve it in `OnReady`, or inject it into the constructor of any long-lived service.

- **Snapshot semantics** — the properties are captured at construction and are immutable for the lifetime of the instance. If you resolve the singleton early, mutate the state somehow, and then check the properties later, you'll see the original values. This is by design: the instance represents "the state at the moment the app launched".

- **Reinstalling the app does NOT reset the state** — the registry values live in `HKCU\SOFTWARE\{AppName}`, which persists across reinstalls. A reinstall is indistinguishable from "another launch". If you need to reset, delete the registry key manually (see Troubleshooting below) or ship an uninstaller that cleans it.

- **Downgrades are treated as version changes** — if the user has `LastVersion = 2.0` and runs your version `1.0`, `IsFirstRunOfVersion` is `true` and `PreviousVersion` is `2.0`. The service does not categorize upgrade vs. downgrade — you can infer it yourself from `PreviousVersion.CompareTo(currentVersion)`.

- **Multi-instance of the same app** — if two processes of the same app launch simultaneously, both will read + write the registry, and `LaunchCount` may be off by one. This is an edge case; Fenestra's `SingleInstanceGuard` prevents it in the normal flow.

- **Does not track per-feature first run** — this service tracks the application as a whole. If you need "is this the first time the user opened the Settings dialog?", use your own `IRegistryConfig` key — that's a different problem.

## Troubleshooting

### Reset the state (simulate a fresh install)

```powershell
reg delete "HKCU\SOFTWARE\YourAppName\Lifecycle" /f
```

On the next launch, `IsFirstRun` will be `true` again.

### Inspect the current state

```powershell
reg query "HKCU\SOFTWARE\YourAppName\Lifecycle"
```

Or open `regedit.exe` and navigate to `HKEY_CURRENT_USER\SOFTWARE\YourAppName\Lifecycle`.

## Related

- Fenestra's [Registry Config](./registry-config.md) — the underlying abstraction this service is built on.
- Fenestra's [App Info](./app-info.md) — where `AppInfo.Version` comes from (read from the entry assembly by default).
