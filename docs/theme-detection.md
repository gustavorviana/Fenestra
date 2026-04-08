# Theme Detection

## Summary

- [Overview](#overview)
- [Enabling Theme Detection](#enabling-theme-detection)
- [Reading the Current Theme](#reading-the-current-theme)
- [Setting the Theme Mode](#setting-the-theme-mode)
- [Reacting to Theme Changes](#reacting-to-theme-changes)
- [AppThemeMode Reference](#appthememode-reference)
- [Full Example](#full-example)

## Overview

`IThemeService` detects the Windows dark/light mode setting and notifies the application when it changes. In `System` mode, the service monitors the registry key `HKCU\Software\Microsoft\Windows\CurrentVersion\Themes\Personalize\AppsUseLightTheme` for changes. In `Dark` or `Light` mode, the value is fixed and monitoring is disabled.

## Enabling Theme Detection

Call `UseWindowsThemeDetection()` on the builder.

```csharp
using Fenestra.Wpf;
using Microsoft.Extensions.DependencyInjection;
using System.Windows;

public partial class App : FenestraApp
{
    protected override void Configure(WpfFenestraBuilder builder)
    {
        builder.Services.AddWindowsThemeDetection();
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

## Reading the Current Theme

`IsDarkMode` returns the current effective theme. `Mode` returns the configured `AppThemeMode`.

```csharp
using Fenestra.Windows;
using Fenestra.Windows.Models;
using System.Windows;

public class MainWindow : Window
{
    private readonly IThemeService _themeService;

    public MainWindow(IThemeService themeService)
    {
        _themeService = themeService;
        Title = "Theme Demo";
        Width = 800;
        Height = 600;

        bool isDark = _themeService.IsDarkMode;
        // isDark => true if the current effective theme is dark

        AppThemeMode mode = _themeService.Mode;
        // mode => System, Dark, or Light
    }
}
```

## Setting the Theme Mode

`SetMode()` switches between system-tracked, forced dark, and forced light modes.

```csharp
using Fenestra.Windows;
using Fenestra.Windows.Models;
using System.Windows;

public class SettingsWindow : Window
{
    private readonly IThemeService _themeService;

    public SettingsWindow(IThemeService themeService)
    {
        _themeService = themeService;
        Title = "Settings";
        Width = 400;
        Height = 300;
    }

    public void FollowSystem()
    {
        _themeService.SetMode(AppThemeMode.System);
        // Reads current Windows setting and starts monitoring for changes
    }

    public void ForceDark()
    {
        _themeService.SetMode(AppThemeMode.Dark);
        // IsDarkMode => true, registry monitoring is disabled
    }

    public void ForceLight()
    {
        _themeService.SetMode(AppThemeMode.Light);
        // IsDarkMode => false, registry monitoring is disabled
    }
}
```

## Reacting to Theme Changes

Subscribe to the `ThemeChanged` event to update your UI when the theme switches. The event parameter is `true` for dark mode, `false` for light mode.

> **Tip:** `ThemeChanged` is dispatched on the UI thread, so you can update UI elements directly in the handler.

```csharp
using Fenestra.Core;
using Fenestra.Windows;
using System.Windows;
using System.Windows.Media;

public class MainWindow : Window
{
    private readonly IThemeService _themeService;

    public MainWindow(IThemeService themeService)
    {
        _themeService = themeService;
        Title = "Theme Demo";
        Width = 800;
        Height = 600;

        _themeService.ThemeChanged += OnThemeChanged;
        ApplyTheme(_themeService.IsDarkMode);
    }

    private Task OnThemeChanged(bool isDarkMode)
    {
        ApplyTheme(isDarkMode);
        return Task.CompletedTask;
    }

    private void ApplyTheme(bool isDarkMode)
    {
        if (isDarkMode)
        {
            Background = new SolidColorBrush(Color.FromRgb(30, 30, 30));
        }
        else
        {
            Background = new SolidColorBrush(Colors.White);
        }
    }
}
```

> **Attention:** The `ThemeChanged` event uses the `BusHandler<bool>` delegate signature, which returns a `Task`. Return `Task.CompletedTask` for synchronous handlers.

## AppThemeMode Reference

| Value | Description |
|---|---|
| `System` | Follow Windows system setting. Registry monitoring is active. |
| `Dark` | Force dark mode. Registry monitoring is disabled. |
| `Light` | Force light mode. Registry monitoring is disabled. |

## Full Example

```csharp
using Fenestra.Core;
using Fenestra.Windows;
using Fenestra.Windows.Models;
using Fenestra.Wpf;
using Microsoft.Extensions.DependencyInjection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

public partial class App : FenestraApp
{
    protected override void Configure(WpfFenestraBuilder builder)
    {
        builder.UseAppName("ThemeApp");
        builder.Services.AddWindowsThemeDetection();
        builder.RegisterWindows();
    }

    protected override Window CreateMainWindow(IServiceProvider services)
    {
        return services.GetRequiredService<MainWindow>();
    }
}

public class MainWindow : Window
{
    private readonly IThemeService _themeService;
    private readonly TextBlock _statusText;
    private readonly StackPanel _panel;

    public MainWindow(IThemeService themeService)
    {
        _themeService = themeService;
        Title = "Theme App";
        Width = 600;
        Height = 400;

        _panel = new StackPanel { Margin = new Thickness(20) };

        _statusText = new TextBlock
        {
            FontSize = 16,
            Margin = new Thickness(0, 0, 0, 20)
        };

        var systemButton = new Button { Content = "System", Margin = new Thickness(0, 0, 0, 5) };
        systemButton.Click += (_, _) => _themeService.SetMode(AppThemeMode.System);

        var darkButton = new Button { Content = "Dark", Margin = new Thickness(0, 0, 0, 5) };
        darkButton.Click += (_, _) => _themeService.SetMode(AppThemeMode.Dark);

        var lightButton = new Button { Content = "Light", Margin = new Thickness(0, 0, 0, 5) };
        lightButton.Click += (_, _) => _themeService.SetMode(AppThemeMode.Light);

        _panel.Children.Add(_statusText);
        _panel.Children.Add(systemButton);
        _panel.Children.Add(darkButton);
        _panel.Children.Add(lightButton);
        Content = _panel;

        _themeService.ThemeChanged += OnThemeChanged;
        ApplyTheme(_themeService.IsDarkMode);
    }

    private Task OnThemeChanged(bool isDarkMode)
    {
        ApplyTheme(isDarkMode);
        return Task.CompletedTask;
    }

    private void ApplyTheme(bool isDarkMode)
    {
        if (isDarkMode)
        {
            _panel.Background = new SolidColorBrush(Color.FromRgb(30, 30, 30));
            _statusText.Foreground = new SolidColorBrush(Colors.White);
            _statusText.Text = $"Mode: {_themeService.Mode} | Dark Mode: ON";
        }
        else
        {
            _panel.Background = new SolidColorBrush(Colors.White);
            _statusText.Foreground = new SolidColorBrush(Colors.Black);
            _statusText.Text = $"Mode: {_themeService.Mode} | Dark Mode: OFF";
        }
    }
}
```

## References

- [IThemeService](../src/Fenestra.Windows/IThemeService.cs)
- [AppThemeMode](../src/Fenestra.Windows/Models/AppThemeMode.cs)
