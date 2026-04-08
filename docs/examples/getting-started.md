# Getting Started

> **Windows only.** Fenestra currently provides Windows-specific implementations. The libraries target `net6.0` / `net472` (not `net6.0-windows`) to allow future cross-platform abstraction, but calling Windows-specific APIs on other platforms will throw `PlatformNotSupportedException` at runtime.

## Installation

Add the WPF integration package to your project:

```xml
<PackageReference Include="Fenestra.Windows.Wpf" />
```

## Minimal Setup

Replace the default WPF `App.xaml.cs` startup with `FenestraBuilder`:

```csharp
using Fenestra.Windows.Wpf;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var app = FenestraBuilder.CreateDefault()
            .UseAppInfo("My App", new Version(1, 0, 0))
            .Build();

        app.Run<MainWindow>();
    }
}
```

## Enabling Features

Each feature is opt-in via the builder:

```csharp
var app = FenestraBuilder.CreateDefault()
    .UseAppInfo("My App", new Version(1, 0, 0))
    .UseToastNotifications()    // Toast notifications (Windows 10+)
    .UseTrayIcon()              // System tray icon
    .UseMinimizeToTray()        // Minimize-to-tray behavior
    .UseGlobalHotkeys()         // Global keyboard shortcuts
    .UseThemeDetection()        // Dark/light mode detection
    .UseAutoStart()             // Run on Windows startup
    .UseSingleInstance()        // Prevent multiple instances
    .Build();
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
| [Toast Notifications](toast-notifications.md) | `UseToastNotifications()` | `IToastService` | Windows 10+ |
| [Tray Icon](tray-icon.md) | `UseTrayIcon()` | `ITrayIconService` | All Windows |
| [Taskbar Progress](taskbar-progress.md) | *(auto)* | `ITaskbarProgress` | Windows 7+ |
| [Global Hotkeys](global-hotkeys.md) | `UseGlobalHotkeys()` | `IGlobalHotkeyService` | All Windows |
| [Theme Detection](theme-detection.md) | `UseThemeDetection()` | `IThemeService` | Windows 10+ |
| [Auto-Start](auto-start.md) | `UseAutoStart()` | `IAutoStartService` | All Windows |
| [Single Instance](single-instance.md) | `UseSingleInstance()` | `ISingleInstanceApp` | All Windows |
| [Window Management](window-management.md) | *(auto)* | `IWindowManager` | All |
| [Dialogs](dialogs.md) | *(auto)* | `IDialogService` | All |
| [Registry Config](registry-config.md) | *(auto)* | `IRegistryConfig` | Windows only |
| [Event Bus](event-bus.md) | *(auto)* | `IEventBus` | All |
| [Window Persistence](window-persistence.md) | `UseWindowPositionStorage<T>()` | `IRememberWindowState` | All Windows |

Features marked *(auto)* are registered automatically and don't require a builder call.
