# Localization

Tracks the current application culture, persists it across launches, and exposes a runtime `SetCulture` method with change notifications. Useful for apps that support multiple languages via `.resx` resource files and need a standard way to let the user pick their preferred language.

## Overview

`CultureInfo.CurrentCulture` / `CultureInfo.CurrentUICulture` is the BCL way to tell .NET which language to use for formatting (dates, numbers) and resource lookup. But the plumbing around it is always the same:

- Define which languages the app supports
- Persist the user's choice across runs
- Pick a sensible default on first run (ideally the OS language, if supported)
- Apply the culture to the current thread and new threads
- Notify consumers when the culture changes so they can re-render

`ILocalizationService` handles all of this in a small, opinionated API, persisting the choice to `HKCU\SOFTWARE\{AppName}\Localization`.

**Important scope note**: this service decides *which* culture the app should use. It does **not** implement XAML string binding — use a library like [`WPFLocalizeExtension`](https://github.com/XAMLMarkupExtensions/WPFLocalizeExtension) for that. Subscribe to `CultureChanged` to re-render your views or hook the event into your XAML localization library of choice.

## Registration

```csharp
var builder = FenestraApplication.CreateBuilder(args);
builder.UseMainWindow<MainWindow>();
builder.Services.AddWindowsLocalization(opts =>
{
    opts.Supported = new[] { "en-US", "pt-BR", "es-ES" };
    opts.Default = "en-US";
});
builder.RegisterWindows();
```

`Supported` and `Default` are both **required**. `Default` must be one of the entries in `Supported`. Culture names must be valid `CultureInfo` identifiers (e.g., `"en-US"`, `"pt-BR"`, `"en"` for neutral).

**Must be resolved at startup** — the service applies the culture to the process in its constructor. If nobody resolves `ILocalizationService`, the culture never gets applied. Resolve it in `OnReady`:

```csharp
protected override void OnReady(IServiceProvider services, Window mainWindow)
{
    _ = services.GetRequiredService<ILocalizationService>();
}
```

Or inject it into any long-lived service's constructor and DI will resolve it transitively.

## Initial culture resolution

On every launch, the service picks the initial culture in this priority order:

1. **Persisted value** — if a culture was previously stored via `SetCulture` and is still in `Supported`, use it
2. **OS culture** — if `CultureInfo.CurrentUICulture.Name` matches a supported culture, use it; if not, try the 2-letter language code as a fallback (e.g., OS `en-GB` matches a supported neutral `en`)
3. **`Default`** — as configured in options

This gives a "just works" experience for users whose Windows language is supported, with a safe fallback when it isn't.

## Usage

### Subscribe to changes

```csharp
public class MyViewModel
{
    public MyViewModel(ILocalizationService loc)
    {
        loc.CultureChanged += OnCultureChanged;
        var currentLang = loc.CurrentCulture.Name; // e.g., "pt-BR"
    }

    private void OnCultureChanged(object? sender, CultureChangedEventArgs e)
    {
        // e.OldCulture, e.NewCulture
        // Re-render your views, refresh string resources, etc.
    }
}
```

### Change culture at runtime

```csharp
using System.Globalization;

_localization.SetCulture(CultureInfo.GetCultureInfo("pt-BR"));
```

What happens:
1. Input validation: `null` → `ArgumentNullException`; unsupported culture → `ArgumentException`
2. If the culture is already the current one → no-op (no event, no registry write)
3. Otherwise:
   - Update `Thread.CurrentThread.CurrentCulture` and `CurrentUICulture`
   - Update `CultureInfo.DefaultThreadCurrentCulture` and `DefaultThreadCurrentUICulture` (affects new threads)
   - Persist the choice to the registry
   - Raise `CultureChanged` synchronously on the calling thread

### List supported cultures

Useful for building a language picker UI:

```csharp
foreach (var culture in _localization.SupportedCultures)
{
    comboBox.Items.Add(new LanguageItem
    {
        Tag = culture.Name,
        DisplayName = culture.NativeName, // e.g., "português (Brasil)"
    });
}
```

## Critical caveats

- **Does NOT re-render open WPF windows** — WPF's binding system caches computed values and doesn't automatically re-evaluate when `CurrentUICulture` changes. To refresh your UI after a culture change you need to either:
  - Re-create the views after `CultureChanged` (close/reopen the main window), or
  - Use a XAML localization library (`WPFLocalizeExtension`, `ReswPlus`) that listens to culture changes and updates bindings, or
  - Manually refresh each control's text from a resource manager in the event handler

- **Running threads don't auto-update** — `CultureInfo.DefaultThreadCurrent*` only affects **new** threads created after the change. Threads already running at the time of `SetCulture` keep their cached culture until they next read `CurrentCulture`. For UI threads this is usually fine because WPF reads the culture on the next render pass.

- **Event fires on caller thread** — if you call `SetCulture` from a non-UI thread and update UI in the handler, marshal manually via `Dispatcher.Invoke` or Fenestra's `IThreadContext.InvokeAsync`.

- **Not a translation engine** — this service does not provide `.resx` helpers, fallback chains, or translation tables. It only manages *which* culture is active. Use .NET's `ResourceManager` for actual resource lookup.

- **No hot-reload from registry edits** — editing `HKCU\SOFTWARE\{AppName}\Localization\SelectedCulture` directly in `regedit` does NOT fire `CultureChanged`. The only way to change culture is via `SetCulture`. Manual registry edits will be picked up on the next launch.

## When to use this

**Use this if:**
- Your app has a small set of supported languages (typically < 20)
- You want persistence across launches without writing the plumbing yourself
- You want the "OS language if supported, else default" pattern for first launch
- You're using `.resx` + `ResourceManager` and just need culture management

**Don't use this if:**
- Your app is mono-lingual — you don't need this feature at all
- You need server-side translations or dynamic language packs that change the set of supported languages at runtime
- You want full automatic XAML binding refresh — combine this service with `WPFLocalizeExtension` or a similar library (they coexist fine; Fenestra handles culture state, the library handles XAML bindings)

## Registry layout

```
HKEY_CURRENT_USER\
  SOFTWARE\
    {AppName}\
      Localization\
        SelectedCulture   (REG_SZ)   "pt-BR"
```

Single `REG_SZ` value. Human-readable in `regedit`, easy to debug or manually reset.

## Troubleshooting

### Reset to "use OS language or default"

```powershell
reg delete "HKCU\SOFTWARE\YourAppName\Localization" /f
```

On the next launch, the initial culture resolution restarts from scratch (no persisted value → OS culture if supported → default).

### Force a specific culture on next launch without running the app

```powershell
reg add "HKCU\SOFTWARE\YourAppName\Localization" /v SelectedCulture /t REG_SZ /d "es-ES" /f
```

The next launch reads this value and applies it immediately.

## Related

- Fenestra's [Registry Config](./registry-config.md) — the underlying abstraction this service is built on.
- Microsoft's [CultureInfo docs](https://learn.microsoft.com/en-us/dotnet/api/system.globalization.cultureinfo) — reference for valid culture names and behavior.
- [`WPFLocalizeExtension`](https://github.com/XAMLMarkupExtensions/WPFLocalizeExtension) — complementary XAML binding library. Use it together with this service: Fenestra manages state, the library handles view refresh.
