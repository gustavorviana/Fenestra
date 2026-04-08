# Tray Icon

System tray (notification area) icon with context menus, balloon tips, overlays, and minimize-to-tray behavior.

## Table of Contents

- [Quick Start](#quick-start)
- [Show and Hide](#show-and-hide)
- [Tooltip](#tooltip)
- [Context Menu](#context-menu)
  - [Items and Actions](#items-and-actions)
  - [Separators](#separators)
  - [Submenus](#submenus)
  - [Colors and Icons](#colors-and-icons)
- [Balloon Tips](#balloon-tips)
- [Minimize to Tray](#minimize-to-tray)
  - [MinimizeToTrayOptions](#minimizetotrayoptions)
- [TrayMenuStyle and Themes](#traymenustyle-and-themes)
- [Click and DoubleClick Events](#click-and-doubleclick-events)
- [API Reference: ITrayIconService](#api-reference-itrayiconservice)
- [API Reference: TrayMenuItem](#api-reference-traymenuitem)
- [API Reference: TrayMenuStyle](#api-reference-traymenustyle)

## Quick Start

```csharp
using Fenestra.Wpf;
using Fenestra.Windows;
using Fenestra.Windows.Models;
using Microsoft.Extensions.DependencyInjection;
using System.Windows;

public partial class App : FenestraApp
{
    protected override void Configure(FenestraBuilder builder)
    {
        builder.UseWindowsTrayIcon();
        builder.RegisterWindows();
    }

    protected override Window CreateMainWindow(IServiceProvider services)
    {
        return services.GetRequiredService<MainWindow>();
    }

    protected override void OnReady(IServiceProvider services, Window mainWindow)
    {
        var tray = services.GetRequiredService<ITrayIconService>();
        tray.SetTooltip("My Application");
        tray.SetContextMenu(new[]
        {
            new TrayMenuItem("Show", () => { mainWindow.Show(); mainWindow.Activate(); }),
            TrayMenuItem.Separator(),
            new TrayMenuItem("Exit", () => Current.Shutdown())
        });
        tray.Show();
    }
}

public class MainWindow : Window
{
    public MainWindow()
    {
        Title = "Tray Icon Demo";
        Width = 400;
        Height = 200;
    }
}
```

## Show and Hide

The tray icon starts hidden. Call `Show()` to display it and `Hide()` to remove it from the notification area.

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
        builder.UseWindowsTrayIcon();
        builder.RegisterWindows();
    }

    protected override Window CreateMainWindow(IServiceProvider services)
    {
        return services.GetRequiredService<MainWindow>();
    }
}

public class MainWindow : Window
{
    private readonly ITrayIconService _tray;

    public MainWindow(ITrayIconService tray)
    {
        _tray = tray;
        Title = "Show/Hide Demo";
        Width = 400;
        Height = 200;

        var panel = new StackPanel();

        var showBtn = new Button { Content = "Show Tray Icon" };
        showBtn.Click += (_, _) => _tray.Show();
        panel.Children.Add(showBtn);

        var hideBtn = new Button { Content = "Hide Tray Icon" };
        hideBtn.Click += (_, _) => _tray.Hide();
        panel.Children.Add(hideBtn);

        Content = panel;
    }
}
```

## Tooltip

`SetTooltip()` sets the text shown when the user hovers over the tray icon.

```csharp
tray.SetTooltip("MyApp v2.1 - Connected");
```

## Context Menu

### Items and Actions

Each `TrayMenuItem` has a display text and a click action.

```csharp
tray.SetContextMenu(new[]
{
    new TrayMenuItem("Open Dashboard", () => OpenDashboard()),
    new TrayMenuItem("Settings", () => OpenSettings()),
    new TrayMenuItem("Exit", () => Application.Current.Shutdown())
});
```

### Separators

Use `TrayMenuItem.Separator()` to insert a visual divider.

```csharp
tray.SetContextMenu(new[]
{
    new TrayMenuItem("Open", () => Open()),
    TrayMenuItem.Separator(),
    new TrayMenuItem("Exit", () => Exit())
});
```

### Submenus

Set the `Children` property to create nested menus.

```csharp
tray.SetContextMenu(new[]
{
    new TrayMenuItem("Theme", () => { })
    {
        Children = new[]
        {
            new TrayMenuItem("Dark", () => SetTheme("dark")),
            new TrayMenuItem("Light", () => SetTheme("light")),
            new TrayMenuItem("System", () => SetTheme("system"))
        }
    },
    TrayMenuItem.Separator(),
    new TrayMenuItem("Exit", () => Application.Current.Shutdown())
});
```

### Colors and Icons

Items support custom foreground/background colors and icons. Colors use `FenestralColor` which accepts hex strings.

```csharp
using Fenestra.Wpf;
using Fenestra.Windows;
using Fenestra.Windows.Models;
using Microsoft.Extensions.DependencyInjection;
using System.Windows;

public partial class App : FenestraApp
{
    protected override void Configure(FenestraBuilder builder)
    {
        builder.UseWindowsTrayIcon();
        builder.RegisterWindows();
    }

    protected override Window CreateMainWindow(IServiceProvider services)
    {
        return services.GetRequiredService<MainWindow>();
    }

    protected override void OnReady(IServiceProvider services, Window mainWindow)
    {
        var tray = services.GetRequiredService<ITrayIconService>();

        tray.SetContextMenu(new[]
        {
            new TrayMenuItem("Show Window", () => { mainWindow.Show(); mainWindow.Activate(); })
            {
                Foreground = "#2196F3",
                Icon = @"C:\MyApp\Resources\app.ico"
            },
            TrayMenuItem.Separator(),
            new TrayMenuItem("Disabled Item", () => { }) { IsEnabled = false },
            new TrayMenuItem("Exit", () => Current.Shutdown())
            {
                Foreground = "#F44336"
            }
        });

        tray.Show();
    }
}

public class MainWindow : Window
{
    public MainWindow()
    {
        Title = "Colors Demo";
        Width = 400;
        Height = 200;
    }
}
```

The `Icon` property accepts a file path (`string`), `Stream`, or a platform-specific image object (e.g., WPF `BitmapSource`).

## Balloon Tips

Balloon notifications appear above the tray icon. They use the native Windows balloon tip API.

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
        builder.UseWindowsTrayIcon();
        builder.RegisterWindows();
    }

    protected override Window CreateMainWindow(IServiceProvider services)
    {
        return services.GetRequiredService<MainWindow>();
    }

    protected override void OnReady(IServiceProvider services, Window mainWindow)
    {
        var tray = services.GetRequiredService<ITrayIconService>();
        tray.Show();
    }
}

public class MainWindow : Window
{
    private readonly ITrayIconService _tray;

    public MainWindow(ITrayIconService tray)
    {
        _tray = tray;
        Title = "Balloon Demo";
        Width = 400;
        Height = 200;

        var panel = new StackPanel();

        var infoBtn = new Button { Content = "Info Balloon" };
        infoBtn.Click += (_, _) => _tray.ShowBalloonTip("Info", "Sync completed.", TrayBalloonIcon.Info);
        panel.Children.Add(infoBtn);

        var warnBtn = new Button { Content = "Warning Balloon" };
        warnBtn.Click += (_, _) => _tray.ShowBalloonTip("Warning", "Disk space low.", TrayBalloonIcon.Warning);
        panel.Children.Add(warnBtn);

        var errorBtn = new Button { Content = "Error Balloon" };
        errorBtn.Click += (_, _) => _tray.ShowBalloonTip("Error", "Connection lost!", TrayBalloonIcon.Error, timeoutMs: 10000);
        panel.Children.Add(errorBtn);

        Content = panel;
    }
}
```

The `BalloonTipClicked` event on `ITrayIconService` fires when the user clicks the balloon notification.

| Icon | Description |
|---|---|
| `TrayBalloonIcon.None` | No icon |
| `TrayBalloonIcon.Info` | Information icon |
| `TrayBalloonIcon.Warning` | Warning icon |
| `TrayBalloonIcon.Error` | Error icon |

## Minimize to Tray

`UseWindowsMinimizeToTray()` intercepts the window close event on windows that implement `IMinimizeToTray`. Instead of closing, the window is hidden and the tray icon is shown. The user can restore the window by double-clicking the tray icon.

`UseWindowsMinimizeToTray()` implies `UseWindowsTrayIcon()` -- you do not need to call both.

```csharp
using Fenestra.Core;
using Fenestra.Wpf;
using Fenestra.Windows;
using Fenestra.Windows.Models;
using Microsoft.Extensions.DependencyInjection;
using System.Windows;

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
        tray.SetTooltip("Still running in background");
        tray.SetContextMenu(new[]
        {
            new TrayMenuItem("Show", () => { mainWindow.Show(); mainWindow.Activate(); }),
            TrayMenuItem.Separator(),
            new TrayMenuItem("Exit", () => Current.Shutdown())
        });
    }
}

public class MainWindow : Window, IMinimizeToTray
{
    public MainWindow()
    {
        Title = "Minimize to Tray Demo";
        Width = 400;
        Height = 200;
        Content = new System.Windows.Controls.TextBlock
        {
            Text = "Close this window -- it will minimize to the tray instead.",
            Margin = new Thickness(10)
        };
    }
}
```

When the user clicks the close button:

1. The `Closing` event is cancelled.
2. The window is hidden via `window.Hide()`.
3. The tray icon is shown (if `AutoShowTrayIcon` is true).
4. Double-clicking the tray icon restores and activates the window.

The window only truly closes when `Application.Current.Shutdown()` is called (e.g., from an "Exit" menu item).

### MinimizeToTrayOptions

Configure the behavior by passing an options delegate to `UseWindowsMinimizeToTray()`.

```csharp
builder.UseWindowsMinimizeToTray(options =>
{
    options.AutoShowTrayIcon = true;       // Show tray icon when window hides (default: true)
    options.RestoreOnDoubleClick = true;    // Restore window on tray double-click (default: true)
});
```

| Option | Type | Default | Description |
|---|---|---|---|
| `AutoShowTrayIcon` | `bool` | `true` | Automatically show the tray icon when a window is hidden |
| `RestoreOnDoubleClick` | `bool` | `true` | Restore the first hidden `IMinimizeToTray` window on tray icon double-click |

## TrayMenuStyle and Themes

The context menu supports theming via the `MenuStyle` property on `ITrayIconService`.

```csharp
using Fenestra.Wpf;
using Fenestra.Windows;
using Fenestra.Windows.Models;
using Microsoft.Extensions.DependencyInjection;
using System.Windows;

public partial class App : FenestraApp
{
    protected override void Configure(FenestraBuilder builder)
    {
        builder.UseWindowsTrayIcon();
        builder.RegisterWindows();
    }

    protected override Window CreateMainWindow(IServiceProvider services)
    {
        return services.GetRequiredService<MainWindow>();
    }

    protected override void OnReady(IServiceProvider services, Window mainWindow)
    {
        var tray = services.GetRequiredService<ITrayIconService>();

        tray.MenuStyle!.Theme = TrayMenuTheme.System;
        tray.MenuStyle!.CornerRadius = 8;

        tray.SetContextMenu(new[]
        {
            new TrayMenuItem("Dark Mode", () => tray.MenuStyle!.Theme = TrayMenuTheme.Dark),
            new TrayMenuItem("Light Mode", () => tray.MenuStyle!.Theme = TrayMenuTheme.Light),
            new TrayMenuItem("Follow System", () => tray.MenuStyle!.Theme = TrayMenuTheme.System),
            new TrayMenuItem("Custom Blue", () => tray.MenuStyle!.Background = "#1E3A5F"),
            new TrayMenuItem("Reset", () =>
            {
                tray.MenuStyle!.Background = null;
                tray.MenuStyle!.Theme = TrayMenuTheme.Default;
            }),
            TrayMenuItem.Separator(),
            new TrayMenuItem("Exit", () => Current.Shutdown())
        });

        tray.Show();
    }
}

public class MainWindow : Window
{
    public MainWindow()
    {
        Title = "Theme Demo";
        Width = 400;
        Height = 200;
    }
}
```

| Theme | Description |
|---|---|
| `TrayMenuTheme.Default` | Standard WPF system appearance (no custom template) |
| `TrayMenuTheme.System` | Follows the current Windows app theme (dark or light) |
| `TrayMenuTheme.Dark` | Forces dark theme |
| `TrayMenuTheme.Light` | Forces light theme |

When `Background` is set to a custom color, theme auto-detection is bypassed and the specified color is used directly.

## Click and DoubleClick Events

`ITrayIconService` exposes `Click` and `DoubleClick` events for the tray icon itself (not the context menu).

```csharp
using Fenestra.Wpf;
using Fenestra.Windows;
using Microsoft.Extensions.DependencyInjection;
using System.Windows;

public partial class App : FenestraApp
{
    protected override void Configure(FenestraBuilder builder)
    {
        builder.UseWindowsTrayIcon();
        builder.RegisterWindows();
    }

    protected override Window CreateMainWindow(IServiceProvider services)
    {
        return services.GetRequiredService<MainWindow>();
    }

    protected override void OnReady(IServiceProvider services, Window mainWindow)
    {
        var tray = services.GetRequiredService<ITrayIconService>();
        tray.SetTooltip("Click or double-click me");

        tray.Click += (_, _) =>
        {
            mainWindow.Title = "Tray icon clicked!";
        };

        tray.DoubleClick += (_, _) =>
        {
            mainWindow.Show();
            mainWindow.WindowState = WindowState.Normal;
            mainWindow.Activate();
        };

        tray.Show();
    }
}

public class MainWindow : Window
{
    public MainWindow()
    {
        Title = "Events Demo";
        Width = 400;
        Height = 200;
    }
}
```

## API Reference: ITrayIconService

| Member | Type | Description |
|---|---|---|
| `Show()` | method | Shows the tray icon in the notification area |
| `Hide()` | method | Hides the tray icon |
| `SetIcon(ITrayIcon)` | method | Sets a custom icon |
| `SetTooltip(string)` | method | Sets the hover tooltip text |
| `ShowBalloonTip(title, text, icon?, timeoutMs?)` | method | Displays a balloon notification. Default icon: `None`, default timeout: 5000ms |
| `SetContextMenu(IEnumerable<TrayMenuItem>)` | method | Sets the right-click context menu items |
| `SetOverlay(ITrayIconOverlay)` | method | Attaches an overlay that composites on top of the icon |
| `MenuStyle` | `TrayMenuStyle?` | Style settings for the context menu |
| `Click` | event | Raised on single-click |
| `DoubleClick` | event | Raised on double-click |
| `BalloonTipClicked` | event | Raised when the user clicks a balloon notification |

## API Reference: TrayMenuItem

| Property | Type | Default | Description |
|---|---|---|---|
| `Text` | `string?` | `null` | Display text |
| `Action` | `Action?` | `null` | Click callback |
| `IsSeparator` | `bool` | `false` | Renders as a separator line |
| `IsEnabled` | `bool` | `true` | Whether the item is clickable |
| `Children` | `IReadOnlyList<TrayMenuItem>?` | `null` | Child items (submenu) |
| `Icon` | `object?` | `null` | Icon source: file path, `Stream`, or platform image |
| `Foreground` | `FenestralColor?` | `null` | Text color (hex string or named color) |
| `Background` | `FenestralColor?` | `null` | Background color |

Static factory: `TrayMenuItem.Separator()` creates a separator item.

Constructor: `new TrayMenuItem(string text, Action action)` creates an item with text and click action.

## API Reference: TrayMenuStyle

| Property | Type | Default | Description |
|---|---|---|---|
| `Background` | `FenestralColor?` | `null` | Custom background color (bypasses theme when set) |
| `Foreground` | `FenestralColor?` | `null` | Custom text color |
| `CornerRadius` | `double` | `0` | Corner radius for the context menu popup |
| `Theme` | `TrayMenuTheme` | `Default` | Theme mode: `Default`, `System`, `Dark`, `Light` |

### FenestraBuilder Methods

| Method | Description |
|---|---|
| `UseWindowsTrayIcon()` | Enables the system tray icon service |
| `UseWindowsMinimizeToTray(Action<MinimizeToTrayOptions>?)` | Enables minimize-to-tray behavior. Implies `UseWindowsTrayIcon()`. |
