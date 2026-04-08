# Platform Detection

## Summary

- [Overview](#overview)
- [Properties](#properties)
- [Guard Methods](#guard-methods)
- [Full Example](#full-example)

## Overview

The `Platform` static class provides OS detection utilities for Fenestra.Windows services. It identifies the current operating system and version, with guard methods that throw `PlatformNotSupportedException` when requirements are not met.

On .NET Framework 4.7.2, `IsWindows` always returns `true`. On .NET 6+, it uses `RuntimeInformation.IsOSPlatform`.

## Properties

| Property | Type | Description |
|---|---|---|
| `IsWindows` | `bool` | `true` if the current OS is Windows |
| `OsVersion` | `Version` | The Windows version. Only meaningful when `IsWindows` is `true` |
| `IsWindows10OrLater` | `bool` | `true` if running on Windows 10 (build 10240) or later |

```csharp
using Fenestra.Windows;
using System;
using System.Windows;

public class MainWindow : Window
{
    public MainWindow()
    {
        Title = "Platform Demo";
        Width = 800;
        Height = 600;

        bool isWindows = Platform.IsWindows;
        // isWindows => true on any Windows version

        Version version = Platform.OsVersion;
        // version => e.g. 10.0.22631.0

        bool isWin10Plus = Platform.IsWindows10OrLater;
        // isWin10Plus => true on Windows 10, 11, and later
    }
}
```

## Guard Methods

`EnsureWindows()` and `EnsureWindows10()` throw `PlatformNotSupportedException` if the OS requirements are not met. Use these to protect platform-specific code paths.

```csharp
using Fenestra.Windows;
using System;
using System.Windows;

public class MainWindow : Window
{
    public MainWindow()
    {
        Title = "Platform Guard Demo";
        Width = 800;
        Height = 600;
    }

    public void UseWindowsFeature()
    {
        Platform.EnsureWindows();
        // Throws PlatformNotSupportedException with message
        // "This feature requires Windows." if not on Windows.

        // Safe to use Windows-specific APIs here
    }

    public void UseWindows10Feature()
    {
        Platform.EnsureWindows10();
        // Throws PlatformNotSupportedException with message
        // "This feature requires Windows 10 or later." if OS < Windows 10.

        // Safe to use Windows 10+ APIs (toast notifications, etc.)
    }

    public void ConditionalFeature()
    {
        if (Platform.IsWindows10OrLater)
        {
            // Use modern APIs
        }
        else if (Platform.IsWindows)
        {
            // Fall back to legacy Windows APIs
        }
        else
        {
            // Non-Windows path
        }
    }
}
```

## Full Example

```csharp
using Fenestra.Windows;
using Fenestra.Wpf;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Windows;
using System.Windows.Controls;

public partial class App : FenestraApp
{
    protected override void Configure(WpfFenestraBuilder builder)
    {
        builder.RegisterWindows();

        if (Platform.IsWindows10OrLater)
        {
            builder.Services.AddWindowsThemeDetection();
        }
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
        Title = "Platform Demo";
        Width = 600;
        Height = 400;

        var panel = new StackPanel { Margin = new Thickness(20) };

        panel.Children.Add(new TextBlock
        {
            Text = $"Is Windows: {Platform.IsWindows}"
        });

        panel.Children.Add(new TextBlock
        {
            Text = $"OS Version: {Platform.OsVersion}"
        });

        panel.Children.Add(new TextBlock
        {
            Text = $"Is Windows 10+: {Platform.IsWindows10OrLater}"
        });

        Content = panel;
    }

    public void RunPlatformSpecificCode()
    {
        try
        {
            Platform.EnsureWindows10();
            // Windows 10+ code path
        }
        catch (PlatformNotSupportedException ex)
        {
            // ex.Message => "This feature requires Windows 10 or later."
        }
    }
}
```

## References

- [Platform](../src/Fenestra.Windows/Platform.cs)
