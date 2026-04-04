# Registry Configuration

Typed read/write access to Windows Registry keys under `HKEY_CURRENT_USER\SOFTWARE\{AppName}`. Supports primitive values, common .NET types, structured sections, and nested objects via subkeys.

## Table of Contents

- [Quick Start](#quick-start)
- [Supported Types](#supported-types)
- [Reading Values](#reading-values)
  - [Get](#get)
  - [TryGet](#tryget)
  - [GetString and GetValue](#getstring-and-getvalue)
- [Writing Values](#writing-values)
- [Sections](#sections)
  - [SetSection and GetSection (Typed)](#setsection-and-getsection-typed)
  - [RegistrySection Attribute](#registrysection-attribute)
  - [GetSection (Child IRegistryConfig)](#getsection-child-iregistryconfig)
- [Utility Methods](#utility-methods)
- [Full Example](#full-example)
- [API Reference](#api-reference)

## Quick Start

`IRegistryConfig` is automatically registered at `HKCU\SOFTWARE\{AppName}`. No explicit setup is needed -- just inject it.

```csharp
using Fenestra.Wpf;
using Fenestra.Windows;
using Microsoft.Extensions.DependencyInjection;
using System.Windows;
using System.Windows.Controls;

public partial class App : FenestraApp
{
    protected override void Configure(FenestraBuilder builder)
    {
        builder.RegisterWindows();
    }

    protected override Window CreateMainWindow(IServiceProvider services)
    {
        return services.GetRequiredService<MainWindow>();
    }
}

public class MainWindow : Window
{
    private readonly IRegistryConfig _config;

    public MainWindow(IRegistryConfig config)
    {
        _config = config;
        Title = "Registry Demo";
        Width = 400;
        Height = 200;

        _config.Set("LastOpened", DateTime.UtcNow);
        _config.Set("LaunchCount", (_config.Get<int>("LaunchCount")) + 1);

        var count = _config.Get<int>("LaunchCount");
        var lastOpened = _config.Get<DateTime>("LastOpened");

        Content = new TextBlock
        {
            Text = $"Launch #{count}, last opened: {lastOpened:g}",
            Margin = new Thickness(10)
        };
    }
}
```

This writes to `HKCU\SOFTWARE\{AppName}\LaunchCount` and `HKCU\SOFTWARE\{AppName}\LastOpened`.

## Supported Types

All types below are transparently converted to and from registry values.

| .NET Type | Registry Kind | Storage Format |
|---|---|---|
| `int` | `DWORD` | 32-bit integer |
| `uint` | `DWORD` | Unsigned, stored as unchecked int |
| `short` | `DWORD` | Cast to int |
| `ushort` | `DWORD` | Cast to int |
| `byte` | `DWORD` | Cast to int |
| `sbyte` | `DWORD` | Cast to int |
| `bool` | `DWORD` | `1` for true, `0` for false |
| `long` | `QWORD` | 64-bit integer |
| `ulong` | `QWORD` | Unsigned, stored as unchecked long |
| `string` | `String` | Direct string value |
| `byte[]` | `Binary` | Raw binary data |
| `Enum` | `DWORD` | Underlying int value |
| `Guid` | `String` | Format `"D"` (e.g., `"a1b2c3d4-..."`) |
| `DateTime` | `String` | ISO 8601 round-trip (`"O"` format) |
| `DateTimeOffset` | `String` | ISO 8601 round-trip (`"O"` format) |
| `TimeSpan` | `String` | Constant format (`"c"`, e.g., `"01:30:00"`) |
| `float` | `String` | Round-trip format (`"R"`) |
| `double` | `String` | Round-trip format (`"R"`) |
| `decimal` | `String` | Invariant culture string |
| `Version` | `String` | e.g., `"1.2.3.4"` |
| `Uri` | `String` | Absolute URI string |

## Reading Values

### Get

Returns the typed value, or `default` if the key does not exist. Throws on conversion failure.

```csharp
int count = _config.Get<int>("LaunchCount");
// count => 0 if "LaunchCount" does not exist

string? name = _config.Get<string>("UserName");
// name => null if "UserName" does not exist

bool darkMode = _config.Get<bool>("DarkMode");
// darkMode => false if "DarkMode" does not exist
```

### TryGet

Returns `false` if the value does not exist or conversion fails. Never throws.

```csharp
if (_config.TryGet<Guid>("SessionId", out var sessionId))
{
    // sessionId => the stored Guid
}
else
{
    // key does not exist or is not a valid Guid
}
```

### GetString and GetValue

Convenience methods for raw access.

```csharp
string? theme = _config.GetString("Theme", defaultValue: "System");
// theme => "System" if "Theme" does not exist

object? raw = _config.GetValue("SomeKey", defaultValue: 42);
// raw => the registry value as-is, or 42 if not found
```

## Writing Values

`Set()` writes a value. Pass `null` to delete the value.

```csharp
_config.Set("UserName", "Alice");
_config.Set("MaxRetries", 3);
_config.Set("EnableFeatureX", true);
_config.Set("SessionId", Guid.NewGuid());
_config.Set("Timeout", TimeSpan.FromMinutes(5));
_config.Set("AppVersion", new Version(2, 1, 0));

// Delete a value
_config.Set("UserName", null);
```

## Sections

Sections map to registry subkeys. They let you organize settings into logical groups.

### SetSection and GetSection (Typed)

Write and read all public properties of an object as values in a subkey.

```csharp
using Fenestra.Wpf;
using Fenestra.Windows;
using Microsoft.Extensions.DependencyInjection;
using System.Windows;
using System.Windows.Controls;

public partial class App : FenestraApp
{
    protected override void Configure(FenestraBuilder builder)
    {
        builder.RegisterWindows();
    }

    protected override Window CreateMainWindow(IServiceProvider services)
    {
        return services.GetRequiredService<MainWindow>();
    }
}

public class DisplaySettings
{
    public int Width { get; set; }
    public int Height { get; set; }
    public bool Fullscreen { get; set; }
    public double Zoom { get; set; }
}

public class MainWindow : Window
{
    private readonly IRegistryConfig _config;

    public MainWindow(IRegistryConfig config)
    {
        _config = config;
        Title = "Sections Demo";
        Width = 400;
        Height = 200;

        var settings = new DisplaySettings
        {
            Width = 1920,
            Height = 1080,
            Fullscreen = true,
            Zoom = 1.25
        };
        _config.SetSection("Display", settings);
        // Writes to HKCU\SOFTWARE\{AppName}\Display\Width, Height, Fullscreen, Zoom

        var loaded = _config.GetSection<DisplaySettings>("Display");
        // loaded.Width => 1920
        // loaded.Height => 1080
        // loaded.Fullscreen => true
        // loaded.Zoom => 1.25

        Content = new TextBlock
        {
            Text = $"{loaded.Width}x{loaded.Height}, Fullscreen={loaded.Fullscreen}, Zoom={loaded.Zoom}",
            Margin = new Thickness(10)
        };
    }
}
```

Use `TryGetSection` for safe reading:

```csharp
if (_config.TryGetSection<DisplaySettings>("Display", out var display))
{
    // display is populated
}
else
{
    // subkey does not exist
}
```

### RegistrySection Attribute

Mark a class with `[RegistrySection]` to have it stored as a nested subkey when used as a property inside another section object. This enables hierarchical configuration.

```csharp
using Fenestra.Wpf;
using Fenestra.Windows;
using Microsoft.Extensions.DependencyInjection;
using System.Windows;
using System.Windows.Controls;

public partial class App : FenestraApp
{
    protected override void Configure(FenestraBuilder builder)
    {
        builder.RegisterWindows();
    }

    protected override Window CreateMainWindow(IServiceProvider services)
    {
        return services.GetRequiredService<MainWindow>();
    }
}

[RegistrySection]
public class ProxySettings
{
    public string Host { get; set; } = string.Empty;
    public int Port { get; set; }
    public bool Enabled { get; set; }
}

[RegistrySection]
public class CacheSettings
{
    public int MaxSizeMb { get; set; }
    public TimeSpan Ttl { get; set; }
}

public class NetworkConfig
{
    public string BaseUrl { get; set; } = string.Empty;
    public int TimeoutSeconds { get; set; }
    public ProxySettings Proxy { get; set; } = new();
    public CacheSettings Cache { get; set; } = new();
}

public class MainWindow : Window
{
    private readonly IRegistryConfig _config;

    public MainWindow(IRegistryConfig config)
    {
        _config = config;
        Title = "RegistrySection Demo";
        Width = 500;
        Height = 200;

        var networkConfig = new NetworkConfig
        {
            BaseUrl = "https://api.example.com",
            TimeoutSeconds = 30,
            Proxy = new ProxySettings
            {
                Host = "proxy.corp.local",
                Port = 8080,
                Enabled = true
            },
            Cache = new CacheSettings
            {
                MaxSizeMb = 512,
                Ttl = TimeSpan.FromHours(2)
            }
        };

        _config.SetSection("Network", networkConfig);
        // Registry layout:
        //   HKCU\SOFTWARE\{AppName}\Network\BaseUrl = "https://api.example.com"
        //   HKCU\SOFTWARE\{AppName}\Network\TimeoutSeconds = 30
        //   HKCU\SOFTWARE\{AppName}\Network\Proxy\Host = "proxy.corp.local"
        //   HKCU\SOFTWARE\{AppName}\Network\Proxy\Port = 8080
        //   HKCU\SOFTWARE\{AppName}\Network\Proxy\Enabled = 1
        //   HKCU\SOFTWARE\{AppName}\Network\Cache\MaxSizeMb = 512
        //   HKCU\SOFTWARE\{AppName}\Network\Cache\Ttl = "02:00:00"

        var loaded = _config.GetSection<NetworkConfig>("Network");

        Content = new TextBlock
        {
            Text = $"URL: {loaded.BaseUrl}\n"
                 + $"Timeout: {loaded.TimeoutSeconds}s\n"
                 + $"Proxy: {loaded.Proxy.Host}:{loaded.Proxy.Port} (enabled={loaded.Proxy.Enabled})\n"
                 + $"Cache: {loaded.Cache.MaxSizeMb}MB, TTL={loaded.Cache.Ttl}",
            Margin = new Thickness(10)
        };
    }
}
```

Without the `[RegistrySection]` attribute, the property is treated as a single value and the framework attempts to convert it, which will fail for complex types.

### GetSection (Child IRegistryConfig)

Get a child `IRegistryConfig` wrapping a subkey for direct key-value operations.

```csharp
using (var pluginsConfig = _config.GetSection("Plugins", createIfNotExists: true))
{
    if (pluginsConfig != null)
    {
        pluginsConfig.Set("EditorPlugin", true);
        pluginsConfig.Set("ThemePlugin", false);
        pluginsConfig.Set("Version", new Version(1, 0, 0));

        var editorEnabled = pluginsConfig.Get<bool>("EditorPlugin");
        // editorEnabled => true
    }
}
```

When `createIfNotExists` is `false` (the default), the method returns `null` if the subkey does not exist.

## Utility Methods

```csharp
// Check if a value exists
bool hasUser = _config.Exists("UserName");

// List all value names in the current key
string[] names = _config.GetValueNames();
// names => ["LaunchCount", "LastOpened", "UserName", ...]

// List all subkey names
string[] sections = _config.GetSections();
// sections => ["Display", "Network", "Plugins", ...]

// Delete a subkey and all its values
bool deleted = _config.DeleteSection("Plugins");
// deleted => true if "Plugins" existed and was removed
```

## Full Example

A settings class with nested sections, demonstrating the complete lifecycle: write, read, check existence, and clean up.

```csharp
using Fenestra.Wpf;
using Fenestra.Windows;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Windows;
using System.Windows.Controls;

public partial class App : FenestraApp
{
    protected override void Configure(FenestraBuilder builder)
    {
        builder.RegisterWindows();
    }

    protected override Window CreateMainWindow(IServiceProvider services)
    {
        return services.GetRequiredService<MainWindow>();
    }
}

public enum AppTheme { Light, Dark, System }

[RegistrySection]
public class WindowPreferences
{
    public int Left { get; set; }
    public int Top { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
    public bool Maximized { get; set; }
}

public class AppSettings
{
    public string UserName { get; set; } = string.Empty;
    public AppTheme Theme { get; set; }
    public bool ShowNotifications { get; set; }
    public DateTime LastLogin { get; set; }
    public Version AppVersion { get; set; } = new(1, 0, 0);
    public WindowPreferences MainWindow { get; set; } = new();
}

public class MainWindow : Window
{
    private readonly IRegistryConfig _config;

    public MainWindow(IRegistryConfig config)
    {
        _config = config;
        Title = "Full Registry Example";
        Width = 500;
        Height = 300;

        var panel = new StackPanel { Margin = new Thickness(10) };

        var saveBtn = new Button { Content = "Save Settings" };
        saveBtn.Click += (_, _) => SaveSettings();
        panel.Children.Add(saveBtn);

        var loadBtn = new Button { Content = "Load Settings", Margin = new Thickness(0, 5, 0, 0) };
        loadBtn.Click += (_, _) => LoadSettings(panel);
        panel.Children.Add(loadBtn);

        var deleteBtn = new Button { Content = "Delete Settings", Margin = new Thickness(0, 5, 0, 0) };
        deleteBtn.Click += (_, _) =>
        {
            _config.DeleteSection("Settings");
            Title = "Settings deleted.";
        };
        panel.Children.Add(deleteBtn);

        Content = panel;
    }

    private void SaveSettings()
    {
        var settings = new AppSettings
        {
            UserName = "Alice",
            Theme = AppTheme.Dark,
            ShowNotifications = true,
            LastLogin = DateTime.UtcNow,
            AppVersion = new Version(2, 5, 1),
            MainWindow = new WindowPreferences
            {
                Left = 100,
                Top = 50,
                Width = 1280,
                Height = 720,
                Maximized = false
            }
        };

        _config.SetSection("Settings", settings);
        // Registry layout:
        //   HKCU\SOFTWARE\{AppName}\Settings\UserName = "Alice"
        //   HKCU\SOFTWARE\{AppName}\Settings\Theme = 1  (Dark as DWORD)
        //   HKCU\SOFTWARE\{AppName}\Settings\ShowNotifications = 1
        //   HKCU\SOFTWARE\{AppName}\Settings\LastLogin = "2026-04-04T..."
        //   HKCU\SOFTWARE\{AppName}\Settings\AppVersion = "2.5.1"
        //   HKCU\SOFTWARE\{AppName}\Settings\MainWindow\Left = 100
        //   HKCU\SOFTWARE\{AppName}\Settings\MainWindow\Top = 50
        //   HKCU\SOFTWARE\{AppName}\Settings\MainWindow\Width = 1280
        //   HKCU\SOFTWARE\{AppName}\Settings\MainWindow\Height = 720
        //   HKCU\SOFTWARE\{AppName}\Settings\MainWindow\Maximized = 0

        Title = "Settings saved.";
    }

    private void LoadSettings(StackPanel panel)
    {
        if (!_config.TryGetSection<AppSettings>("Settings", out var settings) || settings == null)
        {
            Title = "No settings found.";
            return;
        }

        var output = new TextBlock
        {
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 10, 0, 0),
            Text = $"User: {settings.UserName}\n"
                 + $"Theme: {settings.Theme}\n"
                 + $"Notifications: {settings.ShowNotifications}\n"
                 + $"Last Login: {settings.LastLogin:g}\n"
                 + $"Version: {settings.AppVersion}\n"
                 + $"Window: {settings.MainWindow.Width}x{settings.MainWindow.Height} "
                 + $"at ({settings.MainWindow.Left},{settings.MainWindow.Top}), "
                 + $"Maximized={settings.MainWindow.Maximized}"
        };

        if (panel.Children.Count > 3)
            panel.Children.RemoveAt(3);
        panel.Children.Add(output);

        Title = "Settings loaded.";
    }
}
```

## API Reference

### IRegistryConfig

| Method | Description |
|---|---|
| `Set(string name, object? value)` | Writes a value. Pass `null` to delete. |
| `Get<T>(string name)` | Returns typed value or `default` if not found. Throws on conversion failure. |
| `TryGet<T>(string name, out T? value)` | Safe read. Returns `false` if not found or conversion fails. |
| `GetString(string name, string? defaultValue?)` | Returns string value or default. |
| `GetValue(string name, object? defaultValue?)` | Returns raw registry value or default. |
| `GetSection<T>(string sectionName)` | Reads all properties of `T` from a subkey. `[RegistrySection]` properties read recursively. |
| `TryGetSection<T>(string sectionName, out T? value)` | Safe section read. Returns `false` if subkey does not exist. |
| `SetSection(string sectionName, object section)` | Writes all readable properties into a subkey. `[RegistrySection]` properties written recursively. |
| `GetSection(string sectionName, bool createIfNotExists?)` | Returns a child `IRegistryConfig` wrapping the subkey, or `null`. |
| `Exists(string name)` | Returns `true` if the value exists. |
| `GetValueNames()` | Returns all value names in the current key. |
| `GetSections()` | Returns all subkey names. |
| `DeleteSection(string sectionName)` | Deletes a subkey tree. Returns `false` if it did not exist. |
| `Dispose()` | Releases the underlying registry key handle. |

> **Note:** `IRegistryConfig` is automatically registered in the DI container at `HKCU\SOFTWARE\{AppName}`. The `{AppName}` is resolved from the assembly's `Product` attribute or the `UseAppName()` / `UseAppInfo()` builder configuration.
