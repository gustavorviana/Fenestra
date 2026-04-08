# Getting Started

> **Windows only.** Fenestra currently provides Windows-specific implementations. The libraries target `net6.0` / `net472` (not `net6.0-windows`) to allow future cross-platform abstraction, but calling Windows-specific APIs on other platforms will throw `PlatformNotSupportedException` at runtime.

## Installation

Add the WPF integration package to your project:

```xml
<PackageReference Include="Fenestra.Windows.Wpf" />
```

## Minimal Setup

Replace the default WPF `App.xaml.cs` startup with `WpfFenestraBuilder`:

```csharp
using Fenestra.Windows.Wpf;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var app = FenestraApplication.CreateBuilder()
            .UseAppInfo("My App", new Version(1, 0, 0))
            .Build();

        app.Run<MainWindow>();
    }
}
```

## Enabling Features

Each feature is opt-in via the builder:

```csharp
var builder = FenestraApplication.CreateBuilder();
builder.UseAppInfo("My App", new Version(1, 0, 0));

// Windows services (no WPF dependency)
builder.Services.AddWindowsToastNotifications();  // Toast notifications (Windows 10+)
builder.Services.AddWindowsThemeDetection();      // Dark/light mode detection
builder.Services.AddWindowsAutoStart();           // Run on Windows startup

// WPF services (require WPF runtime)
builder.Services.AddWpfTrayIcon();                // System tray icon
builder.Services.AddWpfMinimizeToTray();          // Minimize-to-tray behavior
builder.Services.AddWpfGlobalHotkeys();           // Global keyboard shortcuts
builder.Services.AddWpfSingleInstance();          // Prevent multiple instances

var app = builder.Build();
```

## Dependency Injection

All services are injected via constructor. Request the interface you need:

```csharp
using Fenestra.Windows;

public class MainViewModel
{
    public MainViewModel(
        IToastService toast,
        ITrayIconService tray,
        IGlobalHotkeyService hotkeys,
        IThemeService theme,
        IAutoStartService autoStart,
        IDialogService dialogs,
        IWindowManager windows,
        IEventBus events)
    {
        // All services are ready to use
    }
}
```

## Available Features

| Feature | Builder Method | Interface | Doc |
|---------|---------------|-----------|-----|
| [Toast Notifications](toast-notifications.md) | `AddWindowsToastNotifications()` | `IToastService` | Windows 10+ |
| [Tray Icon](tray-icon.md) | `AddWpfTrayIcon()` | `ITrayIconService` | WPF + Windows |
| [Taskbar Progress](taskbar-progress.md) | *(auto)* | `ITaskbarProgress` | Windows 7+ |
| [Global Hotkeys](global-hotkeys.md) | `AddWpfGlobalHotkeys()` | `IGlobalHotkeyService` | WPF + Windows |
| [Theme Detection](theme-detection.md) | `AddWindowsThemeDetection()` | `IThemeService` | Windows 10+ |
| [Auto-Start](auto-start.md) | `AddWindowsAutoStart()` | `IAutoStartService` | Windows |
| [Single Instance](single-instance.md) | `AddWpfSingleInstance()` | `ISingleInstanceApp` | WPF + Windows |
| [Window Management](window-management.md) | *(auto)* | `IWindowManager` | WPF |
| [Dialogs](dialogs.md) | *(auto)* | `IDialogService` | WPF |
| [Registry Config](registry-config.md) | *(auto)* | `IRegistryConfig` | Windows |
| [Event Bus](event-bus.md) | *(auto)* | `IEventBus` | All |
| [Window Persistence](window-persistence.md) | `UseWindowsPositionStorage<T>()` | `IRememberWindowState` | Windows |

Features marked *(auto)* are registered automatically and don't require a builder call.
