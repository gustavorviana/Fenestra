# Global Hotkeys

## Summary

- [Overview](#overview)
- [Enabling Global Hotkeys](#enabling-global-hotkeys)
- [Registering a Hotkey](#registering-a-hotkey)
- [Unregistering a Hotkey](#unregistering-a-hotkey)
- [Modifier Combinations](#modifier-combinations)
- [Available Keys](#available-keys)
- [Full Example](#full-example)

## Overview

`IGlobalHotkeyService` registers system-wide keyboard shortcuts that work even when the application is not in the foreground. It uses the Win32 `RegisterHotKey` API through a hidden message-only window.

The service implements `IDisposable` and automatically unregisters all hotkeys when disposed.

## Enabling Global Hotkeys

Call `UseGlobalHotkeys()` on the builder.

```csharp
using Fenestra.Wpf;
using Microsoft.Extensions.DependencyInjection;
using System.Windows;

public partial class App : FenestraApp
{
    protected override void Configure(FenestraBuilder builder)
    {
        builder.UseGlobalHotkeys();
        builder.RegisterWindows();
    }

    protected override Window CreateMainWindow(IServiceProvider services)
    {
        return services.GetRequiredService<MainWindow>();
    }
}

public class MainWindow : Window
{
    public MainWindow()
    {
        Title = "My App";
        Width = 800;
        Height = 600;
    }
}
```

## Registering a Hotkey

`Register()` takes a modifier combination, a key, and a callback. It returns an integer ID that can be used to unregister the hotkey later.

```csharp
using Fenestra.Windows;
using Fenestra.Windows.Models;
using System.Windows;

public class MainWindow : Window
{
    private readonly IGlobalHotkeyService _hotkeys;

    public MainWindow(IGlobalHotkeyService hotkeys)
    {
        _hotkeys = hotkeys;
        Title = "Hotkey Demo";
        Width = 800;
        Height = 600;

        int id = _hotkeys.Register(
            HotkeyModifiers.Ctrl | HotkeyModifiers.Shift,
            HotkeyKey.F1,
            OnHotkeyPressed);
        // id => unique identifier for this registration
    }

    private void OnHotkeyPressed()
    {
        // Called when Ctrl+Shift+F1 is pressed anywhere in the system.
        Activate();
    }
}
```

> **Attention:** If the hotkey combination is already registered by another application, `Register()` throws `InvalidOperationException`.

## Unregistering a Hotkey

Pass the ID returned by `Register()` to `Unregister()`.

```csharp
using Fenestra.Windows;
using Fenestra.Windows.Models;
using System.Windows;

public class MainWindow : Window
{
    private readonly IGlobalHotkeyService _hotkeys;
    private int _hotkeyId;

    public MainWindow(IGlobalHotkeyService hotkeys)
    {
        _hotkeys = hotkeys;
        Title = "Hotkey Demo";
        Width = 800;
        Height = 600;

        _hotkeyId = _hotkeys.Register(
            HotkeyModifiers.Alt,
            HotkeyKey.Space,
            () => Activate());
    }

    public void DisableHotkey()
    {
        _hotkeys.Unregister(_hotkeyId);
    }
}
```

## Modifier Combinations

`HotkeyModifiers` is a `[Flags]` enum. Combine modifiers with the `|` operator.

| Value | Description |
|---|---|
| `None` | No modifier |
| `Alt` | Alt key |
| `Ctrl` | Ctrl key |
| `Shift` | Shift key |
| `Win` | Windows key |

Examples of valid combinations:

```csharp
using Fenestra.Windows.Models;

// Single modifier
HotkeyModifiers mods1 = HotkeyModifiers.Ctrl;

// Two modifiers
HotkeyModifiers mods2 = HotkeyModifiers.Ctrl | HotkeyModifiers.Shift;

// Three modifiers
HotkeyModifiers mods3 = HotkeyModifiers.Ctrl | HotkeyModifiers.Alt | HotkeyModifiers.Shift;

// All four modifiers
HotkeyModifiers mods4 = HotkeyModifiers.Ctrl | HotkeyModifiers.Alt | HotkeyModifiers.Shift | HotkeyModifiers.Win;
```

## Available Keys

`HotkeyKey` covers standard virtual key codes:

| Category | Keys |
|---|---|
| Letters | `A` through `Z` |
| Digits | `D0` through `D9` |
| Function keys | `F1` through `F12` |
| Navigation | `Home`, `End`, `PageUp`, `PageDown`, `Left`, `Up`, `Right`, `Down` |
| Editing | `Space`, `Enter`, `Escape`, `Tab`, `Backspace`, `Delete`, `Insert` |
| Numpad | `NumPad0` through `NumPad9` |
| Special | `PrintScreen`, `Pause` |

## Full Example

An application that registers multiple global hotkeys to show/hide windows and toggle features:

```csharp
using Fenestra.Core;
using Fenestra.Windows;
using Fenestra.Windows.Models;
using Fenestra.Wpf;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Generic;
using System.Windows;

public partial class App : FenestraApp
{
    protected override void Configure(FenestraBuilder builder)
    {
        builder.UseAppName("HotkeyApp");
        builder.UseGlobalHotkeys();
        builder.RegisterWindows();
    }

    protected override Window CreateMainWindow(IServiceProvider services)
    {
        return services.GetRequiredService<MainWindow>();
    }

    protected override void OnReady(IServiceProvider services, Window mainWindow)
    {
        var hotkeys = services.GetRequiredService<IGlobalHotkeyService>();

        hotkeys.Register(
            HotkeyModifiers.Ctrl | HotkeyModifiers.Alt,
            HotkeyKey.M,
            () => ActivateMainWindow(mainWindow));

        hotkeys.Register(
            HotkeyModifiers.Ctrl | HotkeyModifiers.Alt,
            HotkeyKey.N,
            () => ShowNotes(services));
    }

    private static void ActivateMainWindow(Window mainWindow)
    {
        if (mainWindow.WindowState == WindowState.Minimized)
        {
            mainWindow.WindowState = WindowState.Normal;
        }
        mainWindow.Activate();
    }

    private static void ShowNotes(IServiceProvider services)
    {
        var windowManager = services.GetRequiredService<IWindowManager>();
        windowManager.Show<NotesWindow>();
    }
}

public class MainWindow : Window
{
    private readonly IGlobalHotkeyService _hotkeys;
    private readonly List<int> _hotkeyIds = new();

    public MainWindow(IGlobalHotkeyService hotkeys)
    {
        _hotkeys = hotkeys;
        Title = "Hotkey App";
        Width = 800;
        Height = 600;

        int clipboardHotkey = _hotkeys.Register(
            HotkeyModifiers.Ctrl | HotkeyModifiers.Shift,
            HotkeyKey.V,
            OnClipboardHotkey);
        _hotkeyIds.Add(clipboardHotkey);

        int screenshotHotkey = _hotkeys.Register(
            HotkeyModifiers.Win | HotkeyModifiers.Shift,
            HotkeyKey.S,
            OnScreenshotHotkey);
        _hotkeyIds.Add(screenshotHotkey);
    }

    private void OnClipboardHotkey()
    {
        Title = "Clipboard hotkey pressed!";
    }

    private void OnScreenshotHotkey()
    {
        Title = "Screenshot hotkey pressed!";
    }

    public void UnregisterAll()
    {
        foreach (int id in _hotkeyIds)
        {
            _hotkeys.Unregister(id);
        }
        _hotkeyIds.Clear();
    }
}

public class NotesWindow : Window, ISingleWindow
{
    public NotesWindow()
    {
        Title = "Quick Notes";
        Width = 400;
        Height = 300;
    }
}
```

## References

- [IGlobalHotkeyService](../src/Fenestra.Windows/IGlobalHotkeyService.cs)
- [HotkeyModifiers](../src/Fenestra.Windows/Models/HotkeyModifiers.cs)
- [HotkeyKey](../src/Fenestra.Windows/Models/HotkeyKey.cs)
