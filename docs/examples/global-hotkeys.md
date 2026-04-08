# Global Hotkeys

> **Windows only.** Uses RegisterHotKey/UnregisterHotKey Win32 APIs. Hotkeys work system-wide, even when the application is not focused.

## Setup

```csharp
var app = FenestraBuilder.CreateDefault()
    .UseAppInfo("My App", new Version(1, 0, 0))
    .UseGlobalHotkeys()
    .Build();
```

## Register a Hotkey

```csharp
using Fenestra.Windows;

public class MainViewModel
{
    private readonly IGlobalHotkeyService _hotkeys;

    public MainViewModel(IGlobalHotkeyService hotkeys)
    {
        _hotkeys = hotkeys;

        // Ctrl+Shift+S — global shortcut
        _hotkeys.Register(HotkeyModifiers.Ctrl | HotkeyModifiers.Shift, HotkeyKey.S, () =>
        {
            Console.WriteLine("Global hotkey pressed!");
        });
    }
}
```

## Multiple Hotkeys

```csharp
_hotkeys.Register(HotkeyModifiers.Ctrl | HotkeyModifiers.Alt, HotkeyKey.P, () =>
    TogglePause());

_hotkeys.Register(HotkeyModifiers.Ctrl | HotkeyModifiers.Alt, HotkeyKey.Q, () =>
    Application.Current.Shutdown());

_hotkeys.Register(HotkeyModifiers.Win, HotkeyKey.F1, () =>
    ShowQuickLauncher());
```

## Unregister

```csharp
var id = _hotkeys.Register(HotkeyModifiers.Ctrl, HotkeyKey.Space, () => DoSomething());

// Later, unregister by ID
_hotkeys.Unregister(id);
```

## Available Modifiers

```csharp
// HotkeyModifiers (flags, combinable with |):
// Ctrl, Shift, Alt, Win
```

## Available Keys

```csharp
// HotkeyKey includes:
// A-Z, D0-D9 (number row), F1-F24
// Space, Enter, Escape, Tab
// Left, Right, Up, Down
// Home, End, PageUp, PageDown
// Insert, Delete, Backspace
// And more...
```
