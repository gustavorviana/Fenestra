# Registry Configuration

> **Windows only.** Stores application settings in the Windows Registry under `HKCU\Software\{AppName}`. Registered automatically — no builder call needed.

## Basic Usage

```csharp
using Fenestra.Windows;

public class SettingsViewModel
{
    private readonly IRegistryConfig _config;

    public SettingsViewModel(IRegistryConfig config)
    {
        _config = config;
    }

    public void SaveSettings()
    {
        _config.Set("Theme", "Dark");
        _config.Set("FontSize", 14);
        _config.Set("ShowWelcome", true);
    }

    public void LoadSettings()
    {
        var theme = _config.Get<string>("Theme", "Light");         // Default: "Light"
        var fontSize = _config.Get<int>("FontSize", 12);           // Default: 12
        var showWelcome = _config.Get<bool>("ShowWelcome", true);  // Default: true
    }
}
```

## Sections (Nested Keys)

```csharp
// Stored under HKCU\Software\{AppName}\Window
_config.Set("Window", "Width", 800);
_config.Set("Window", "Height", 600);

var width = _config.Get<int>("Window", "Width", 1024);
```

## Delete Values

```csharp
_config.Delete("Theme");
_config.DeleteSection("Window");
```

## Enumerate

```csharp
var keys = _config.GetValueNames();           // All value names in root
var sections = _config.GetSectionNames();     // All section names
```
