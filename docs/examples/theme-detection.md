# Theme Detection

> **Windows 10+ only.** Reads the Windows dark/light mode setting from the registry and watches for changes via registry notification.

## Setup

```csharp
var app = FenestraBuilder.CreateDefault()
    .UseAppInfo("My App", new Version(1, 0, 0))
    .UseThemeDetection()
    .Build();
```

## Check Current Theme

```csharp
using Fenestra.Windows;

public class MainViewModel
{
    private readonly IThemeService _theme;

    public MainViewModel(IThemeService theme)
    {
        _theme = theme;

        if (_theme.IsDarkMode)
            ApplyDarkTheme();
        else
            ApplyLightTheme();
    }
}
```

## React to Theme Changes

```csharp
_theme.ThemeChanged += (_, _) =>
{
    if (_theme.IsDarkMode)
        ApplyDarkTheme();
    else
        ApplyLightTheme();
};
```

## Set Theme Mode

```csharp
// AppThemeMode: System, Dark, Light

_theme.SetMode(AppThemeMode.System);  // Follow Windows setting (default)
_theme.SetMode(AppThemeMode.Dark);    // Force dark mode
_theme.SetMode(AppThemeMode.Light);   // Force light mode
```
