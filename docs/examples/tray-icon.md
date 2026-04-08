# Tray Icon

> **Windows only.** System tray (notification area) icons use Win32 Shell_NotifyIcon APIs. These are available on all supported Windows versions.

## Setup

```csharp
var app = FenestraBuilder.CreateDefault()
    .UseAppInfo("My App", new Version(1, 0, 0))
    .UseTrayIcon()
    .Build();
```

## Basic Tray Icon

```csharp
using Fenestra.Windows;

public class MainViewModel
{
    private readonly ITrayIconService _tray;

    public MainViewModel(ITrayIconService tray)
    {
        _tray = tray;

        _tray.SetTooltip("My App is running");
        _tray.Show();
    }
}
```

## Click Events

```csharp
_tray.Click += (_, _) =>
{
    // Single click — e.g., toggle a popup
};

_tray.DoubleClick += (_, _) =>
{
    // Double click — e.g., show/restore main window
};
```

## Context Menu

```csharp
_tray.SetContextMenu(new[]
{
    new TrayMenuItem("Open", () => ShowMainWindow()),
    new TrayMenuItem("Settings", () => OpenSettings()),
    TrayMenuItem.Separator,
    new TrayMenuItem("Exit", () => Application.Current.Shutdown())
});
```

## Balloon Notifications (Classic)

> For modern toast notifications, use [IToastService](toast-notifications.md) instead. Balloon tips are legacy Windows notifications.

```csharp
_tray.ShowBalloonTip("Update Available", "Version 2.0 is ready to install.", BalloonTipIcon.Info);

_tray.BalloonTipClicked += (_, _) =>
{
    // User clicked the balloon
};
```

## Change Icon Dynamically

```csharp
// Set from a file path
_tray.SetIcon("Assets/app-icon.ico");

// Hide when not needed
_tray.Hide();

// Show again
_tray.Show();
```

## Badge Overlay

Show a notification count badge on the tray icon:

```csharp
_tray.SetBadge(5);      // Shows "5" overlay
_tray.SetBadge(0);      // Clears the badge
```

## Minimize to Tray

Enable minimize-to-tray behavior for windows:

```csharp
var app = FenestraBuilder.CreateDefault()
    .UseAppInfo("My App", new Version(1, 0, 0))
    .UseTrayIcon()
    .UseMinimizeToTray()
    .Build();
```

Mark your window to use this behavior:

```csharp
public partial class MainWindow : Window, IMinimizeToTray
{
    // When the user clicks the close button, the window hides to tray
    // instead of closing. Double-click the tray icon to restore.
}
```
