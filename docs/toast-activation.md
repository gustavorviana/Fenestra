# Toast Activation

Registers the application as a COM server so that clicking a toast notification brings the app to the foreground, or relaunches it if it was closed.

## Table of Contents

- [Overview](#overview)
- [Quick Start (Auto-Generated CLSID)](#quick-start-auto-generated-clsid)
- [Explicit CLSID for Production](#explicit-clsid-for-production)
- [How It Works](#how-it-works)
- [Full Example](#full-example)
- [API Reference](#api-reference)

## Overview

By default, Windows toast notifications are fire-and-forget. If the user clicks a toast after the app has been closed, nothing happens. `UseToastActivation()` solves this by:

1. Registering a COM server in the registry so Windows knows which EXE to launch.
2. Creating a Start Menu shortcut with a `ToastActivatorCLSID` so Windows links toasts to the COM server.
3. Registering a COM class factory at runtime (`CoRegisterClassObject`) so Windows can deliver the activation callback while the app is running.

When the app is running, a toast click invokes `IApplicationActivator.BringToForeground()` on the UI thread. When the app is closed, Windows relaunches the EXE via the COM server registration and delivers the activation.

## Quick Start (Auto-Generated CLSID)

The simplest setup. The CLSID is derived deterministically from the application's AppId using a SHA-256 hash, so it stays stable across restarts as long as the AppId does not change.

```csharp
using Fenestra.Wpf;
using Fenestra.Windows;
using Microsoft.Extensions.DependencyInjection;
using System.Windows;

public partial class App : FenestraApp
{
    protected override void Configure(FenestraBuilder builder)
    {
        builder.UseToastNotifications();
        builder.UseToastActivation();
        builder.RegisterWindows();
    }

    protected override Window CreateMainWindow(IServiceProvider services)
    {
        return services.GetRequiredService<MainWindow>();
    }
}

public class MainWindow : Window
{
    private readonly IToastService _toast;

    public MainWindow(IToastService toast)
    {
        _toast = toast;
        Title = "Toast Activation Demo";
        Width = 400;
        Height = 200;

        var button = new System.Windows.Controls.Button { Content = "Send Toast & Close" };
        button.Click += (_, _) =>
        {
            _toast.Show(t => t
                .Title("Reminder")
                .Body("Click me to relaunch the app."));
        };
        Content = button;
    }
}
```

> **Note:** `UseToastActivation()` must be called after `UseToastNotifications()`.

## Explicit CLSID for Production

For production deployments, use a hardcoded GUID. This avoids any risk of the CLSID changing if the AppId derivation logic is updated in a future Fenestra version. Generate one with `Guid.NewGuid()` and never change it once deployed.

```csharp
using Fenestra.Wpf;
using Fenestra.Windows;
using Microsoft.Extensions.DependencyInjection;
using System.Windows;

public partial class App : FenestraApp
{
    protected override void Configure(FenestraBuilder builder)
    {
        builder.UseToastNotifications();
        builder.UseToastActivation(Guid.Parse("A1B2C3D4-E5F6-7890-ABCD-EF1234567890"));
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
        Title = "Production App";
        Width = 400;
        Height = 200;
    }
}
```

## How It Works

### Registration Flow (on startup)

1. **Registry COM server** -- writes `HKCU\SOFTWARE\Classes\CLSID\{clsid}\LocalServer32` pointing to the current EXE path. This tells Windows which executable to launch when the COM class is activated.

2. **Start Menu shortcut** -- creates or updates `%APPDATA%\Microsoft\Windows\Start Menu\Programs\{AppName}.lnk` with the `ToastActivatorCLSID` property. Windows requires this link between the shortcut and the CLSID to deliver toast activations.

3. **Runtime class factory** -- calls `CoRegisterClassObject` to register a COM class factory for the CLSID. While the app is running, Windows delivers activation callbacks through this factory instead of launching a new process.

### Activation Flow (on toast click)

- **App is running**: Windows calls the registered class factory. Fenestra invokes `IApplicationActivator.BringToForeground()` on the UI thread, which restores and focuses the main window.
- **App is closed**: Windows launches the EXE via the `LocalServer32` registry entry. On startup, the COM class factory is registered again, and the activation callback fires.

### Cleanup

`Unregister()` removes the registry COM server entry and stops the runtime class factory. This happens automatically when the `ToastActivationRegistrar` is disposed (on app shutdown).

## Full Example

A complete app that sends a toast with a button, then handles both the button click and the toast body click -- whether the app was open or relaunched.

```csharp
using Fenestra.Wpf;
using Fenestra.Windows;
using Fenestra.Windows.Models;
using Microsoft.Extensions.DependencyInjection;
using System.Windows;
using System.Windows.Controls;

public partial class App : FenestraApp
{
    protected override void Configure(FenestraBuilder builder)
    {
        builder.UseToastNotifications();
        builder.UseToastActivation(Guid.Parse("64BD1DB5-C8C7-41D1-958F-30B13D3F18ED"));
        builder.RegisterWindows();
    }

    protected override Window CreateMainWindow(IServiceProvider services)
    {
        return services.GetRequiredService<MainWindow>();
    }
}

public class MainWindow : Window
{
    private readonly IToastService _toast;
    private readonly TextBlock _log;

    public MainWindow(IToastService toast)
    {
        _toast = toast;
        Title = "Activation Demo";
        Width = 500;
        Height = 300;

        var panel = new StackPanel { Margin = new Thickness(10) };

        var sendBtn = new Button { Content = "Send Notification" };
        sendBtn.Click += (_, _) => SendNotification();
        panel.Children.Add(sendBtn);

        _log = new TextBlock { TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 10, 0, 0) };
        panel.Children.Add(_log);

        Content = panel;
    }

    private void SendNotification()
    {
        var handle = _toast.Show(t => t
            .Title("Task Complete")
            .Body("Your export finished. Click to view.")
            .Launch("action=view")
            .AddButton("Open Folder", "action=folder", ToastButtonStyle.Success)
            .AddDismissButton());

        handle.Activated += (_, args) =>
        {
            Dispatcher.Invoke(() =>
            {
                switch (args.Arguments)
                {
                    case "action=view":
                        _log.Text = "Toast body clicked -- opening viewer.";
                        break;
                    case "action=folder":
                        _log.Text = "Open Folder button clicked.";
                        break;
                    default:
                        _log.Text = $"Activated with: {args.Arguments}";
                        break;
                }
            });
        };
    }
}
```

> **Note:** The `Activated` event fires on the UI thread when the app is already running. When the app is relaunched from a closed state, `IApplicationActivator.BringToForeground()` is called automatically -- the `Activated` event on the original handle is not replayed.

## API Reference

### FenestraBuilder Extensions

| Method | Description |
|---|---|
| `UseToastActivation()` | Enables toast activation with a CLSID derived from the AppId |
| `UseToastActivation(Guid)` | Enables toast activation with an explicit CLSID |

### IToastActivationRegistrar

| Member | Description |
|---|---|
| `Register()` | Registers COM server, shortcut, and class factory |
| `Unregister()` | Removes COM server registration and shortcut |
| `IsRegistered` | Whether activation infrastructure is currently registered |

### IApplicationActivator

| Member | Description |
|---|---|
| `BringToForeground()` | Activates and brings the main window to the foreground |

> **Note:** `IApplicationActivator` is automatically registered by Fenestra. You do not need to implement it unless you want custom foreground-activation behavior.
