# Idle Detection

Detects user inactivity across the entire Windows session via the Win32 `GetLastInputInfo` API. Raises events when the user crosses an idle threshold, useful for apps that want to pause animations, dim the UI, save drafts, or update presence status when the user steps away.

## Overview

Windows tracks the tick count of the last global mouse/keyboard input via `GetLastInputInfo` (user32). Fenestra's `IIdleDetectionService` polls this value on a pool-based `System.Threading.Timer` and raises `BecameIdle` / `BecameActive` events when the user crosses a configurable threshold.

**Framework-agnostic**: this service lives in `Fenestra.Windows` and uses `System.Threading.Timer` from the BCL. It works in **WPF**, **WinForms**, **console**, and **background Windows hosts** — no dispatcher or UI thread required.

**Key concept — "global" idle**: the API tracks ANY input anywhere on the system, not input targeted at your application's windows. If the user is typing in Notepad while your app is minimized, they are **not** idle. This is a characteristic of the underlying Windows API, not of Fenestra.

## Registration

```csharp
var builder = FenestraApplication.CreateBuilder(args);
builder.UseMainWindow<MainWindow>();
builder.Services.AddWindowsIdleDetection(opts =>
{
    opts.Threshold = TimeSpan.FromMinutes(5);     // default: 5 minutes
    opts.PollInterval = TimeSpan.FromSeconds(5);  // default: 5 seconds
});
builder.RegisterWindows();
```

Calling `AddWindowsIdleDetection` without a configure callback uses the defaults.

## Thread model for events

- If an `IThreadContext` is registered in DI (e.g., WPF apps where `WpfFenestraBuilder` registers `WpfThreadContext`), `BecameIdle`/`BecameActive` events are **marshalled to the dispatcher thread** automatically — you can update UI directly in the handler.
- If no `IThreadContext` is registered (console apps, background services), events fire on the thread pool thread that ran the timer callback. The consumer is responsible for marshalling to a UI thread if needed.

This means WPF apps get UI-safe events for free, and non-UI apps don't pay for marshalling they don't need.

## Usage

```csharp
public class MyViewModel
{
    private readonly IIdleDetectionService _idle;

    public MyViewModel(IIdleDetectionService idle)
    {
        _idle = idle;
        _idle.BecameIdle += OnUserWentIdle;
        _idle.BecameActive += OnUserCameBack;
    }

    private void OnUserWentIdle(object? sender, EventArgs e)
    {
        // e.g., pause background animations, update presence status
        PauseAnimations();
        _presence.SetStatus("Away");
    }

    private void OnUserCameBack(object? sender, EventArgs e)
    {
        ResumeAnimations();
        _presence.SetStatus("Online");
    }
}
```

**Properties:**
- `IdleTime` — how long since the last user input (updated on every poll)
- `IsIdle` — cached; consistent with the last event fired
- `Threshold` — mutable at runtime; re-evaluation happens on the next poll tick

**Events fire on the WPF dispatcher thread**, so you can update UI directly in the handler without marshalling.

## Scope limitations

### Global, not per-app

```
User activity timeline:
[0:00]  typing in your Fenestra app         → active
[2:00]  switches to Notepad, keeps typing   → still active (global input)
[7:00]  closes Notepad, stops touching PC   → still active (for now)
[12:00] 5 min after the last global input   → BecameIdle fires
```

If you need "idle in my window", you'd have to implement global keyboard/mouse hooks (invasive, antivirus-unfriendly). Not supported by Fenestra.

### Video playback / games don't count as input

`GetLastInputInfo` only tracks mouse and keyboard events. If the user is watching a full-screen video with no input, they'll be flagged as idle. The user's **actual** attention is not observable by the OS.

### Polling latency

Events fire at `PollInterval` granularity. If you set `PollInterval = 5s` and threshold = 5 min, the `BecameIdle` event can fire up to 5 seconds **after** the user actually crossed the threshold. Lower `PollInterval` = faster detection but slightly more CPU. Higher = opposite.

The default of 5 seconds is a good balance: imperceptible latency for user-facing cases, negligible CPU cost (17280 polls/day = a tiny fraction of a core).

### Non-persistent state

Restarting the app resets the idle state. The service starts as "active" and will fire `BecameIdle` on the first poll where the threshold is crossed.

## Performance notes

- `GetLastInputInfo` is a cheap syscall (~microseconds). The `DispatcherTimer` tick is the dominant cost, and it's still negligible.
- Default `PollInterval = 5s` → ~17,280 calls/day. Even at 1s intervals (86,400 calls/day) the cost is unmeasurable in practice.
- Avoid `PollInterval` below 500ms unless you have a specific reason — the `DispatcherTimer` queueing overhead becomes nontrivial at that rate.

## Threshold changes at runtime

Changing `Threshold` mid-flight is fully supported but **does not re-evaluate immediately** — the new threshold is used on the next poll tick. This is intentional: it keeps event dispatch in a single predictable place (the timer callback) and avoids race conditions between setter and event handlers.

```csharp
// User is currently at 3min idle, threshold = 5min → not idle
idleService.Threshold = TimeSpan.FromMinutes(2);
// Not idle yet — wait for the next Poll tick (≤ PollInterval delay)
// Then BecameIdle fires because 3min > 2min
```

## Limitations

- **Minimum `Threshold`**: 1 second. Below this, events would fire too frequently to be useful.
- **Minimum `PollInterval`**: 100ms. Below this, you're wasting CPU and introducing DispatcherTimer overhead.
- **Gamepad input (XInput/DirectInput) is NOT tracked** — only mouse and keyboard via the standard input pipeline.
- **Held keys don't refresh the idle timer continuously** — Windows only registers the initial keydown, not the hold.
- **Raw input devices** (pen, touch) count as input in most cases, but the exact behavior depends on the device and driver.

## When NOT to use this

- **Games or apps that need gamepad input awareness** — use XInput/DirectInput directly, not this.
- **Apps that need sub-second reaction** to user activity — the polling-based design has inherent latency.
- **Web apps embedded in a WebView** — use the browser's visibility/idle APIs (`document.visibilityState`, `requestIdleCallback`, etc.) instead.
- **Cross-session or cross-user activity tracking** — this service is per-process and resets on restart.

## Exceptions

| Exception | When |
|---|---|
| `ArgumentNullException` | `options` parameter passed to the constructor is `null` |
| `ArgumentException` | `Threshold` < 1 second or `PollInterval` < 100ms, at construction or via `Threshold` setter |
| `PlatformNotSupportedException` | Running on a non-Windows platform |

## Manual verification

1. Run a sample app with `AddWpfIdleDetection(opts => opts.Threshold = TimeSpan.FromSeconds(10))` (low threshold makes testing faster).
2. Stop touching mouse/keyboard for 10 seconds.
3. Within `PollInterval` (default 5s) after the threshold, `BecameIdle` should fire.
4. Move the mouse.
5. Within another `PollInterval`, `BecameActive` should fire.
