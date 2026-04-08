# App Info

## Summary

- [Overview](#overview)
- [Default Resolution](#default-resolution)
- [UseAppName](#useappname)
- [UseAppInfo](#useappinfo)
- [AppInfo Properties](#appinfo-properties)
- [AppGuid Resolution](#appguid-resolution)
- [Packaged Apps (MSIX/AppX)](#packaged-apps-msixappx)
- [Accessing AppInfo at Runtime](#accessing-appinfo-at-runtime)
- [Full Example](#full-example)

## Overview

`AppInfo` holds application metadata -- name, identifier, version, and a stable GUID. Fenestra resolves this information automatically from assembly attributes and the runtime environment, but you can override individual values through the builder.

`AppInfo` is registered as a singleton in the DI container and is available via constructor injection.

## Default Resolution

When no explicit configuration is provided, Fenestra resolves metadata from the entry assembly:

1. **AppName**: `[assembly: AssemblyProduct("...")]` attribute, falling back to the assembly name, then `"FenestraApp"`.
2. **AppId**: Derived from `AppName` by stripping non-alphanumeric characters.
3. **Version**: `AssemblyName.Version`, falling back to `1.0.0`.
4. **AppGuid**: See [AppGuid Resolution](#appguid-resolution).

```csharp
using Fenestra.Wpf;
using Microsoft.Extensions.DependencyInjection;
using System.Windows;

// Assembly attributes in your .csproj or AssemblyInfo.cs:
// <Product>My Dashboard</Product>
// <Version>2.1.0</Version>

public partial class App : FenestraApp
{
    protected override void Configure(WpfFenestraBuilder builder)
    {
        // No explicit app info configuration.
        // AppName => "My Dashboard" (from Product attribute)
        // AppId => "MyDashboard"
        // Version => 2.1.0
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
        Title = "Main Window";
        Width = 800;
        Height = 600;
    }
}
```

## UseAppName

Sets only the application name. Version is still resolved from the entry assembly.

```csharp
using Fenestra.Wpf;
using Microsoft.Extensions.DependencyInjection;
using System.Windows;

public partial class App : FenestraApp
{
    protected override void Configure(WpfFenestraBuilder builder)
    {
        builder.UseAppName("My Custom App");
        // AppName => "My Custom App"
        // AppId => "MyCustomApp"
        // Version => resolved from assembly
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
        Title = "Main Window";
        Width = 800;
        Height = 600;
    }
}
```

## UseAppInfo

Sets the application name and version explicitly. An overload also accepts a custom `AppId`.

```csharp
using Fenestra.Wpf;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Windows;

public partial class App : FenestraApp
{
    protected override void Configure(WpfFenestraBuilder builder)
    {
        // Name + version
        builder.UseAppInfo("My App", new Version(3, 0, 0));
        // AppName => "My App"
        // AppId => "MyApp"
        // Version => 3.0.0

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
        Title = "Main Window";
        Width = 800;
        Height = 600;
    }
}
```

With a custom `AppId`:

```csharp
using Fenestra.Wpf;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Windows;

public partial class App : FenestraApp
{
    protected override void Configure(WpfFenestraBuilder builder)
    {
        builder.UseAppInfo("My App", "com.company.myapp", new Version(3, 0, 0));
        // AppName => "My App"
        // AppId => "com.company.myapp"
        // Version => 3.0.0

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
        Title = "Main Window";
        Width = 800;
        Height = 600;
    }
}
```

> **Tip:** The `AppId` is used by single-instance mode (named pipe/mutex), auto-start (registry key), and other features that need a stable identifier. Keep it consistent across versions.

## AppInfo Properties

| Property | Type | Description |
|---|---|---|
| `AppName` | `string` | Display name of the application |
| `AppId` | `string` | Unique application identifier. For classic apps: user-defined or derived from `AppName`. For packaged apps: the AUMID |
| `Version` | `Version` | Application version |
| `AppGuid` | `Guid` | Stable GUID for this installation (see [AppGuid Resolution](#appguid-resolution)) |
| `IsPackagedApp` | `bool` | `true` when running as MSIX/AppX |
| `PackageFamilyName` | `string?` | Package family name for packaged apps; `null` otherwise |

## AppGuid Resolution

The `AppGuid` is resolved in this order:

1. **Assembly `[Guid]` attribute**: If the entry assembly has `[assembly: Guid("...")]`, that value is used.
2. **Registry**: If no assembly GUID exists, the framework checks `HKCU\SOFTWARE\{AppName}\AppGuid`. If found, it is used.
3. **Auto-generate**: If neither source provides a GUID, a new one is generated via `Guid.NewGuid()` and persisted to the registry for future runs.

This ensures that the GUID remains stable across application restarts without requiring explicit configuration.

```csharp
using System.Runtime.InteropServices;

// Option 1: Set via assembly attribute (recommended for libraries/stable apps)
[assembly: Guid("a1b2c3d4-e5f6-7890-abcd-ef1234567890")]
```

```csharp
using Fenestra.Core.Models;
using System;
using System.Windows;

public class MainWindow : Window
{
    public MainWindow(AppInfo appInfo)
    {
        Title = "GUID Demo";
        Width = 800;
        Height = 600;

        Guid guid = appInfo.AppGuid;
        // guid => stable GUID for this application installation
    }
}
```

## Packaged Apps (MSIX/AppX)

When the application runs as a packaged app, Fenestra automatically detects the package identity and resolves `AppInfo` from it. In this case:

- `UseAppName()` and `UseAppInfo()` are ignored.
- `AppId` is set to the AUMID (e.g., `PackageFamilyName!App`).
- `IsPackagedApp` is `true`.
- `PackageFamilyName` is populated.

No additional configuration is needed for packaged apps.

## Accessing AppInfo at Runtime

`AppInfo` is a singleton in the DI container. Inject it wherever you need application metadata.

```csharp
using Fenestra.Core.Models;
using System.Windows;

public class AboutWindow : Window
{
    public AboutWindow(AppInfo appInfo)
    {
        Title = $"About {appInfo.AppName}";
        Width = 350;
        Height = 250;

        var text = $"{appInfo.AppName}\n" +
                   $"Version: {appInfo.Version}\n" +
                   $"App ID: {appInfo.AppId}\n" +
                   $"GUID: {appInfo.AppGuid}\n" +
                   $"Packaged: {appInfo.IsPackagedApp}";

        Content = new System.Windows.Controls.TextBlock
        {
            Text = text,
            Margin = new Thickness(20),
            FontSize = 14
        };
    }
}
```

## Full Example

```csharp
using Fenestra.Core;
using Fenestra.Core.Models;
using Fenestra.Wpf;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Windows;
using System.Windows.Controls;

public partial class App : FenestraApp
{
    protected override void Configure(WpfFenestraBuilder builder)
    {
        builder.UseAppInfo("My Dashboard", "com.company.dashboard", new Version(2, 5, 0));
        builder.Services.AddWpfSingleInstance();
        builder.Services.AddWindowsAutoStart();
        builder.RegisterWindows();
    }

    protected override Window CreateMainWindow(IServiceProvider services)
    {
        return services.GetRequiredService<MainWindow>();
    }
}

public class MainWindow : Window, IRememberWindowState
{
    private readonly IWindowManager _windowManager;
    private readonly AppInfo _appInfo;

    public MainWindow(IWindowManager windowManager, AppInfo appInfo)
    {
        _windowManager = windowManager;
        _appInfo = appInfo;
        Title = $"{_appInfo.AppName} v{_appInfo.Version}";
        Width = 800;
        Height = 600;

        var panel = new StackPanel { Margin = new Thickness(20) };

        panel.Children.Add(new TextBlock
        {
            Text = $"App Name: {_appInfo.AppName}",
            FontSize = 14
        });
        panel.Children.Add(new TextBlock
        {
            Text = $"App ID: {_appInfo.AppId}",
            FontSize = 14
        });
        panel.Children.Add(new TextBlock
        {
            Text = $"Version: {_appInfo.Version}",
            FontSize = 14
        });
        panel.Children.Add(new TextBlock
        {
            Text = $"GUID: {_appInfo.AppGuid}",
            FontSize = 14
        });
        panel.Children.Add(new TextBlock
        {
            Text = $"Packaged: {_appInfo.IsPackagedApp}",
            FontSize = 14
        });

        if (_appInfo.IsPackagedApp)
        {
            panel.Children.Add(new TextBlock
            {
                Text = $"Package Family: {_appInfo.PackageFamilyName}",
                FontSize = 14
            });
        }

        var aboutButton = new Button
        {
            Content = "About",
            Margin = new Thickness(0, 20, 0, 0)
        };
        aboutButton.Click += (_, _) => _windowManager.Show<AboutWindow>();

        panel.Children.Add(aboutButton);
        Content = panel;
    }
}

public class AboutWindow : Window, ISingleWindow
{
    public AboutWindow(AppInfo appInfo)
    {
        Title = $"About {appInfo.AppName}";
        Width = 350;
        Height = 200;

        Content = new TextBlock
        {
            Text = $"{appInfo.AppName}\nVersion {appInfo.Version}\n\nGUID: {appInfo.AppGuid}",
            Margin = new Thickness(20),
            FontSize = 14,
            TextWrapping = TextWrapping.Wrap
        };
    }
}
```

## References

- [AppInfo](../src/Fenestra.Core/Models/AppInfo.cs)
- [AppInfoBuilder](../src/Fenestra.Core/Models/AppInfoBuilder.cs)
