# Fenestra

Fenestra is a WPF application framework that provides a structured hosting model with dependency injection, system tray integration, toast notifications, registry configuration, window state persistence, global hotkeys, theme detection, auto-start, single-instance enforcement, and more.

Built on top of `Microsoft.Extensions.Hosting`, Fenestra eliminates the boilerplate of wiring up a modern WPF application while keeping full control over the DI container, logging, and configuration.

## Table of Contents

- [Prerequisites](#prerequisites)
- [Getting Started](#getting-started)
  - [Pattern 1: App Style (App.xaml.cs Inheritance)](#pattern-1-app-style-appxamlcs-inheritance)
  - [Pattern 2: Builder Style (Program.cs)](#pattern-2-builder-style-programcs)
  - [Pattern 3: Builder with Custom App](#pattern-3-builder-with-custom-app)
- [Features](#features)
- [Architecture](#architecture)
- [References](#references)

## Prerequisites

- .NET 8.0+ (also supports .NET 6.0 and .NET Framework 4.7.2)
- Windows 10 or later (required for toast notifications and theme detection)
- Visual Studio 2022 or later (recommended)

## Getting Started

Fenestra supports two main startup patterns. Choose the one that best fits your application.

### Pattern 1: App Style (App.xaml.cs Inheritance)

This pattern uses standard WPF `App.xaml` + `App.xaml.cs` with inheritance from `FenestraApp`. Best for applications that need XAML resources, merged dictionaries, or the traditional WPF startup flow.

**Step 1 -- Create the project**

```xml
<!-- MyApp.csproj -->
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>WinExe</OutputType>
    <TargetFramework>net8.0-windows</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <UseWPF>true</UseWPF>
    <Product>My App</Product>
  </PropertyGroup>

  <ItemGroup>
    <ProjectReference Include="..\Fenestra.Windows.Wpf\Fenestra.Windows.Wpf.csproj" />
    <ProjectReference Include="..\Fenestra.Windows\Fenestra.Windows.csproj" />
  </ItemGroup>
</Project>
```

**Step 2 -- Define the MainWindow**

```xml
<!-- MainWindow.xaml -->
<Window x:Class="MyApp.MainWindow"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        Title="My App" Height="450" Width="800">
    <Grid>
        <TextBlock Text="Hello, Fenestra!" 
                   HorizontalAlignment="Center" 
                   VerticalAlignment="Center" 
                   FontSize="24" />
    </Grid>
</Window>
```

```csharp
// MainWindow.xaml.cs
using Fenestra.Core;
using System.Windows;

namespace MyApp;

public partial class MainWindow : Window, IRememberWindowState, IMinimizeToTray
{
    public MainWindow()
    {
        InitializeComponent();
    }
}
```

`IRememberWindowState` persists the window's position and size across sessions. `IMinimizeToTray` makes the window minimize to the system tray instead of closing (requires `UseWindowsMinimizeToTray()` in the builder).

**Step 3 -- Configure App.xaml**

Replace the default `Application` root element with `FenestraApp`:

```xml
<!-- App.xaml -->
<fenestra:FenestraApp x:Class="MyApp.App"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:fenestra="clr-namespace:Fenestra.Wpf;assembly=Fenestra.Windows.Wpf">
    <Application.Resources>
    </Application.Resources>
</fenestra:FenestraApp>
```

**Step 4 -- Implement App.xaml.cs**

```csharp
// App.xaml.cs
using Fenestra.Core;
using Fenestra.Windows;
using Fenestra.Wpf;
using Microsoft.Extensions.DependencyInjection;
using System.Windows;

namespace MyApp;

public partial class App : FenestraApp
{
    protected override void Configure(FenestraBuilder builder)
    {
        builder.UseWindowsMinimizeToTray();
        builder.RegisterWindows();
    }

    protected override Window CreateMainWindow(IServiceProvider services)
    {
        return services.GetRequiredService<MainWindow>();
    }

    protected override void OnReady(IServiceProvider services, Window mainWindow)
    {
        var tray = services.GetRequiredService<ITrayIconService>();
        tray.SetTooltip("My App");
        tray.Show();
        tray.Click += (_, _) =>
        {
            mainWindow.Show();
            mainWindow.WindowState = WindowState.Normal;
            mainWindow.Activate();
        };
    }
}
```

| Override | Purpose |
|---|---|
| `Configure(FenestraBuilder)` | Register services, enable features (tray, hotkeys, toasts, etc.) |
| `CreateMainWindow(IServiceProvider)` | Resolve and return the main window from the DI container |
| `OnReady(IServiceProvider, Window)` | Post-initialization setup (tray menus, hotkeys, event subscriptions) |

### Pattern 2: Builder Style (Program.cs)

This pattern uses a top-level `Program.cs` with no `App.xaml`. Best for console-style entry points or when you want maximum control over the startup sequence.

> **Note:** The `.csproj` must disable the default application definition so WPF does not look for `App.xaml`:
> ```xml
> <EnableDefaultApplicationDefinition>false</EnableDefaultApplicationDefinition>
> ```

**Step 1 -- Create the project**

```xml
<!-- MyApp.csproj -->
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>WinExe</OutputType>
    <TargetFramework>net8.0-windows</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <UseWPF>true</UseWPF>
    <EnableDefaultApplicationDefinition>false</EnableDefaultApplicationDefinition>
  </PropertyGroup>

  <ItemGroup>
    <ProjectReference Include="..\Fenestra.Windows.Wpf\Fenestra.Windows.Wpf.csproj" />
    <ProjectReference Include="..\Fenestra.Windows\Fenestra.Windows.csproj" />
  </ItemGroup>
</Project>
```

**Step 2 -- Define the MainWindow**

```xml
<!-- MainWindow.xaml -->
<Window x:Class="MyApp.MainWindow"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        Title="My App" Height="450" Width="800">
    <Grid>
        <TextBlock Text="Hello, Fenestra!" 
                   HorizontalAlignment="Center" 
                   VerticalAlignment="Center" 
                   FontSize="24" />
    </Grid>
</Window>
```

```csharp
// MainWindow.xaml.cs
using Fenestra.Core;
using System.Windows;

namespace MyApp;

public partial class MainWindow : Window, IMinimizeToTray
{
    public MainWindow()
    {
        InitializeComponent();
    }
}
```

**Step 3 -- Write Program.cs**

```csharp
// Program.cs
using MyApp;
using Fenestra.Wpf;

var builder = FenestraApplication.CreateBuilder(args);
builder.UseMainWindow<MainWindow>();
builder.UseWindowsMinimizeToTray();
builder.RegisterWindows();

var app = builder.Build();
app.Run();
```

That is the entire startup. Fenestra creates the WPF `Application` instance internally, sets up the DI container, and runs the message loop.

### Pattern 3: Builder with Custom App

When you need XAML resources (styles, merged dictionaries) but still want a `Program.cs` entry point, you can combine both approaches:

```csharp
// Program.cs
using MyApp;
using Fenestra.Wpf;

var builder = FenestraApplication.CreateBuilder<App, MainWindow>(args);
builder.RegisterWindows();

var app = builder.Build();
app.Run();
```

In this case, `App` is a plain `Application` subclass (not `FenestraApp`) whose `App.xaml` carries your resources. Fenestra instantiates it and calls `InitializeComponent()` automatically.

## Features

Fenestra is modular. Each feature is opt-in via the `FenestraBuilder`:

| Feature | Builder Method | Documentation |
|---|---|---|
| App Info (name, version, GUID) | Automatic | [App Info](./app-info.md) |
| Registry Configuration | Automatic | [Registry Config](./registry-config.md) |
| Toast Notifications | `UseWindowsToastNotifications()` | [Toast Notifications](./toast-notifications.md) |
| Toast Background Activation | `UseWindowsToastActivation()` | [Toast Activation](./toast-activation.md) |
| Window State Persistence | Implement `IRememberWindowState` | [Window Management](./window-management.md) |
| Dialog Service | Automatic | [Dialog Service](./dialog-service.md) |
| System Tray Icon | `UseWindowsTrayIcon()` | [Tray Icon](./tray-icon.md) |
| Minimize to Tray | `UseWindowsMinimizeToTray()` | [Tray Icon](./tray-icon.md) |
| Single Instance | `UseWindowsSingleInstance()` | [Single Instance](./single-instance.md) |
| Auto-Start (Windows startup) | `UseWindowsAutoStart()` | [Auto-Start](./auto-start.md) |
| Global Hotkeys | `UseWindowsGlobalHotkeys()` | [Global Hotkeys](./global-hotkeys.md) |
| Theme Detection (dark/light) | `UseWindowsThemeDetection()` | [Theme Detection](./theme-detection.md) |
| Event Bus | Automatic | [Event Bus](./event-bus.md) |
| Taskbar Progress | Automatic | [Taskbar Progress](./taskbar-progress.md) |
| Platform Detection | Static class | [Platform](./platform.md) |
| Exception Handling | Automatic (customizable) | [Exception Handling](./exception-handling.md) |
| Window Manager | Automatic | [Window Management](./window-management.md) |
| Logging | `ConfigureLogging()` | Standard `Microsoft.Extensions.Logging` |
| Configuration | `Configuration` property | Standard `Microsoft.Extensions.Configuration` |

### Services Available by Default

These services are always registered in the DI container, with no explicit opt-in:

- `AppInfo` -- application name, version, GUID, and package identity
- `IRegistryConfig` -- read/write values in `HKCU\SOFTWARE\{AppName}`
- `IWindowManager` -- open, close, and manage windows via DI
- `IDialogService` -- file/folder dialogs and message boxes
- `IEventBus` -- publish/subscribe in-process event bus
- `ITaskbarProvider` -- taskbar progress bar overlay
- `IExceptionHandler` -- global exception handling (replaceable)

### Services Requiring Opt-In

These require a `Use*()` call on the builder:

- `ITrayIconService` -- via `UseWindowsTrayIcon()` or `UseWindowsMinimizeToTray()`
- `IToastService` -- via `UseWindowsToastNotifications()`
- `IGlobalHotkeyService` -- via `UseWindowsGlobalHotkeys()`
- `IAutoStartService` -- via `UseWindowsAutoStart()`
- `IThemeService` -- via `UseWindowsThemeDetection()`

## Architecture

Fenestra is split into three assemblies:

```
Fenestra.Core            (net6.0 / net472)     Interfaces, models, abstractions
    |
Fenestra.Windows         (net6.0 / net472)     Windows-specific implementations (registry, tray, toast, hotkeys)
    |
Fenestra.Windows.Wpf     (net6.0-windows / net472)    WPF hosting (FenestraApp, FenestraApplication, FenestraBuilder)
```

| Assembly | Namespace | Contains |
|---|---|---|
| Fenestra.Core | `Fenestra.Core` | `IRememberWindowState`, `IMinimizeToTray`, `ISingleInstanceApp`, `IWindowManager`, `IEventBus`, `IDialogService`, `IExceptionHandler` |
| Fenestra.Windows | `Fenestra.Windows` | `ITrayIconService`, `IToastService`, `IRegistryConfig`, `IGlobalHotkeyService`, `IAutoStartService`, `IThemeService`, `ITaskbarProvider`, `Platform` |
| Fenestra.Windows.Wpf | `Fenestra.Wpf` | `FenestraApp`, `FenestraApplication`, `FenestraBuilder`, `IMainWindowFactory` |

## References

- [Microsoft.Extensions.Hosting](https://learn.microsoft.com/en-us/dotnet/core/extensions/generic-host)
- [Microsoft.Extensions.DependencyInjection](https://learn.microsoft.com/en-us/dotnet/core/extensions/dependency-injection)
- [WPF Documentation](https://learn.microsoft.com/en-us/dotnet/desktop/wpf/)
