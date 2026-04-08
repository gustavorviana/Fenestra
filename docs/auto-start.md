# Auto Start

## Summary

- [Overview](#overview)
- [Enabling Auto Start](#enabling-auto-start)
- [Enable and Disable](#enable-and-disable)
- [Checking Status](#checking-status)
- [Passing Arguments](#passing-arguments)
- [StartupType Reference](#startuptype-reference)
- [Full Example](#full-example)

## Overview

`IAutoStartService` manages Windows startup registration for the application. It writes to the `HKCU\SOFTWARE\Microsoft\Windows\CurrentVersion\Run` registry key and the corresponding `StartupApproved\Run` key used by Windows to track user/policy overrides.

## Enabling Auto Start

Call `UseWindowsAutoStart()` on the builder to register the service.

```csharp
using Fenestra.Wpf;
using Microsoft.Extensions.DependencyInjection;
using System.Windows;

public partial class App : FenestraApp
{
    protected override void Configure(WpfFenestraBuilder builder)
    {
        builder.Services.AddWindowsAutoStart();
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

## Enable and Disable

Use `Enable()` to register the application for startup and `Disable()` to remove it.

```csharp
using Fenestra.Windows;
using System.Windows;

public class SettingsWindow : Window
{
    private readonly IAutoStartService _autoStart;

    public SettingsWindow(IAutoStartService autoStart)
    {
        _autoStart = autoStart;
        Title = "Settings";
        Width = 400;
        Height = 300;
    }

    public void ToggleAutoStart()
    {
        if (_autoStart.IsEnabled)
        {
            _autoStart.Disable();
            // Removes the registry entry from Run key
        }
        else
        {
            _autoStart.Enable();
            // Registers: "C:\path\to\app.exe" in Run key
        }
    }
}
```

## Checking Status

`IsEnabled` returns whether the startup entry exists and is approved. `GetStatus()` returns a `StartupStatus` struct with detailed information, or `null` if no entry exists.

```csharp
using Fenestra.Windows;
using Fenestra.Windows.Models;
using System.Windows;

public class SettingsWindow : Window
{
    private readonly IAutoStartService _autoStart;

    public SettingsWindow(IAutoStartService autoStart)
    {
        _autoStart = autoStart;
        Title = "Settings";
        Width = 400;
        Height = 300;
    }

    public void ShowStartupInfo()
    {
        bool enabled = _autoStart.IsEnabled;
        // enabled => true if startup entry is active

        StartupStatus? status = _autoStart.GetStatus();
        if (status.HasValue)
        {
            StartupType type = status.Value.Status;
            // type => Enabled, Disabled, DisabledByUser, or DisabledByPolicy

            System.DateTime modified = status.Value.ModifiedDate;
            // modified => when the entry was last changed

            bool isActive = status.Value.Enabled;
            // isActive => true only when Status == StartupType.Enabled
        }
    }
}
```

## Passing Arguments

`Enable()` accepts optional arguments that are appended to the command line when the application starts.

```csharp
using Fenestra.Windows;
using System.Windows;

public class SettingsWindow : Window
{
    private readonly IAutoStartService _autoStart;

    public SettingsWindow(IAutoStartService autoStart)
    {
        _autoStart = autoStart;
        Title = "Settings";
        Width = 400;
        Height = 300;
    }

    public void EnableWithMinimized()
    {
        _autoStart.Enable("--minimized", "--silent");
        // Registers: "C:\path\to\app.exe" --minimized --silent
    }

    public void CheckInitialized()
    {
        bool ready = _autoStart.IsInitialized("--minimized");
        // ready => true if the current startup entry contains "--minimized"
    }
}
```

`IsInitialized()` checks whether the startup entry exists and contains the specified arguments. Pass no arguments to simply check for the entry's existence.

## StartupType Reference

| Value | Description |
|---|---|
| `Enabled` | Startup entry is active |
| `Disabled` | Startup entry is disabled |
| `DisabledByUser` | User disabled the entry via Task Manager or Settings |
| `DisabledByPolicy` | Group Policy prevents the application from starting |

> **Attention:** When a user disables startup via Task Manager, the `StartupType` changes to `DisabledByUser`. Calling `Enable()` again re-enables it. However, `DisabledByPolicy` cannot be overridden programmatically.

## Full Example

```csharp
using Fenestra.Core;
using Fenestra.Windows;
using Fenestra.Windows.Models;
using Fenestra.Wpf;
using Microsoft.Extensions.DependencyInjection;
using System.Windows;
using System.Windows.Controls;

public partial class App : FenestraApp
{
    protected override void Configure(WpfFenestraBuilder builder)
    {
        builder.UseAppName("MyDashboard");
        builder.Services.AddWindowsAutoStart();
        builder.RegisterWindows();
    }

    protected override Window CreateMainWindow(IServiceProvider services)
    {
        return services.GetRequiredService<MainWindow>();
    }
}

public class MainWindow : Window
{
    private readonly IAutoStartService _autoStart;
    private readonly CheckBox _autoStartCheckBox;
    private readonly TextBlock _statusText;

    public MainWindow(IAutoStartService autoStart)
    {
        _autoStart = autoStart;
        Title = "My Dashboard";
        Width = 800;
        Height = 600;

        var panel = new StackPanel { Margin = new Thickness(20) };

        _autoStartCheckBox = new CheckBox
        {
            Content = "Start with Windows",
            IsChecked = _autoStart.IsEnabled
        };
        _autoStartCheckBox.Checked += (_, _) => EnableAutoStart();
        _autoStartCheckBox.Unchecked += (_, _) => DisableAutoStart();

        _statusText = new TextBlock { Margin = new Thickness(0, 10, 0, 0) };
        UpdateStatusText();

        panel.Children.Add(_autoStartCheckBox);
        panel.Children.Add(_statusText);
        Content = panel;
    }

    private void EnableAutoStart()
    {
        _autoStart.Enable("--minimized");
        UpdateStatusText();
    }

    private void DisableAutoStart()
    {
        _autoStart.Disable();
        UpdateStatusText();
    }

    private void UpdateStatusText()
    {
        StartupStatus? status = _autoStart.GetStatus();
        if (status.HasValue)
        {
            _statusText.Text = $"Status: {status.Value.Status}, Modified: {status.Value.ModifiedDate:g}";
        }
        else
        {
            _statusText.Text = "Status: Not registered";
        }
    }
}
```

## References

- [IAutoStartService](../src/Fenestra.Windows/IAutoStartService.cs)
- [StartupStatus](../src/Fenestra.Windows/Models/StartupStatus.cs)
- [StartupType](../src/Fenestra.Windows/Models/StartupType.cs)
