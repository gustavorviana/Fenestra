# Window Management

## Summary

- [Overview](#overview)
- [IWindowManager](#iwindowmanager)
  - [Showing Windows](#showing-windows)
  - [Showing Dialogs](#showing-dialogs)
  - [Typed Dialog Results](#typed-dialog-results)
  - [Querying and Closing](#querying-and-closing)
- [ISingleWindow](#isinglewindow)
- [IRememberWindowState](#irememberwindowstate)
- [Custom Window Position Storage](#custom-window-position-storage)
- [Full Example](#full-example)

## Overview

Fenestra manages window lifecycle through dependency injection. All windows are resolved from the DI container and tracked automatically. The framework provides marker interfaces to opt into single-instance enforcement and position persistence.

## IWindowManager

`IWindowManager` is registered automatically and available via constructor injection. It handles creation, display, tracking, and closing of windows.

### Showing Windows

`Show<T>()` resolves a window from the container, displays it, and returns the instance. An optional `dataContext` parameter sets the `DataContext` before the window is shown.

```csharp
using Fenestra.Core;
using Fenestra.Wpf;
using Microsoft.Extensions.DependencyInjection;
using System.Windows;

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
    private readonly IWindowManager _windowManager;

    public MainWindow(IWindowManager windowManager)
    {
        _windowManager = windowManager;
        Title = "Main Window";
        Width = 800;
        Height = 600;
    }

    public void OpenSettings()
    {
        _windowManager.Show<SettingsWindow>();
    }

    public void OpenSettingsWithContext()
    {
        var viewModel = new SettingsViewModel { Theme = "Dark" };
        _windowManager.Show<SettingsWindow>(viewModel);
    }
}

public class SettingsWindow : Window
{
    public SettingsWindow()
    {
        Title = "Settings";
        Width = 400;
        Height = 300;
    }
}

public class SettingsViewModel
{
    public string Theme { get; set; } = "Light";
}
```

### Showing Dialogs

Dialog windows must implement `IDialog`. `ShowDialog<T>()` opens a modal dialog and returns `true` when the dialog result is affirmative.

```csharp
using Fenestra.Core;
using System.Windows;

public class ConfirmDialog : Window, IDialog
{
    public ConfirmDialog()
    {
        Title = "Confirm";
        Width = 300;
        Height = 150;
    }
}

public class MainWindow : Window
{
    private readonly IWindowManager _windowManager;

    public MainWindow(IWindowManager windowManager)
    {
        _windowManager = windowManager;
        Title = "Main";
        Width = 800;
        Height = 600;
    }

    public void AskForConfirmation()
    {
        bool confirmed = _windowManager.ShowDialog<ConfirmDialog>();
        // confirmed => true if DialogResult was set to true
    }
}
```

### Typed Dialog Results

For dialogs that return a specific value, implement `IDialog<TResult>`. The `Result` property carries the typed return value.

```csharp
using Fenestra.Core;
using System.Windows;

public class ColorPickerDialog : Window, IDialog<string>
{
    public string? Result { get; set; }

    public ColorPickerDialog()
    {
        Title = "Pick a Color";
        Width = 300;
        Height = 200;
    }

    public void SelectRed()
    {
        Result = "#FF0000";
        DialogResult = true;
        Close();
    }
}

public class MainWindow : Window
{
    private readonly IWindowManager _windowManager;

    public MainWindow(IWindowManager windowManager)
    {
        _windowManager = windowManager;
        Title = "Main";
        Width = 800;
        Height = 600;
    }

    public void PickColor()
    {
        string color = _windowManager.ShowDialog<ColorPickerDialog, string>();
        // color => "#FF0000" if user selected red
    }
}
```

### Querying and Closing

```csharp
using Fenestra.Core;
using System.Windows;

public class MainWindow : Window
{
    private readonly IWindowManager _windowManager;

    public MainWindow(IWindowManager windowManager)
    {
        _windowManager = windowManager;
        Title = "Main";
        Width = 800;
        Height = 600;
    }

    public void CheckIfSettingsIsOpen()
    {
        SettingsWindow? settings = _windowManager.GetOpenWindow<SettingsWindow>();
        // settings => null if the window is not open
    }

    public void CloseSettings()
    {
        _windowManager.Close<SettingsWindow>();
    }

    public void CloseAllOpenDialogs()
    {
        _windowManager.CloseAllDialogs();
    }
}

public class SettingsWindow : Window
{
    public SettingsWindow()
    {
        Title = "Settings";
        Width = 400;
        Height = 300;
    }
}
```

## ISingleWindow

Mark a window with `ISingleWindow` to enforce that only one instance can be open at a time. If the window is already open, `Show<T>()` brings the existing instance to the front instead of creating a new one.

> **Attention:** A window cannot implement both `ISingleWindow` and `IDialog`. Attempting to show such a type as a dialog throws `InvalidOperationException`.

```csharp
using Fenestra.Core;
using System.Windows;

public class LogViewerWindow : Window, ISingleWindow
{
    public LogViewerWindow()
    {
        Title = "Log Viewer";
        Width = 600;
        Height = 400;
    }
}

public class MainWindow : Window
{
    private readonly IWindowManager _windowManager;

    public MainWindow(IWindowManager windowManager)
    {
        _windowManager = windowManager;
        Title = "Main";
        Width = 800;
        Height = 600;
    }

    public void ShowLogs()
    {
        // First call creates and shows the window.
        _windowManager.Show<LogViewerWindow>();

        // Second call brings the existing window to the front.
        _windowManager.Show<LogViewerWindow>();
    }
}
```

## IRememberWindowState

Mark a window with `IRememberWindowState` to automatically persist and restore its position, size, and state (normal/maximized) across sessions.

By default, window positions are stored in the Windows Registry under `HKCU\SOFTWARE\{AppName}`. The key used for each window is its full type name.

If the saved position falls outside all connected monitors (e.g., after a display was disconnected), the saved data is discarded and the window opens at its default position.

```csharp
using Fenestra.Core;
using System.Windows;

public class MainWindow : Window, IRememberWindowState
{
    public MainWindow()
    {
        Title = "My App";
        Width = 800;
        Height = 600;
    }
}
```

Both interfaces can be combined:

```csharp
using Fenestra.Core;
using System.Windows;

public class ToolboxWindow : Window, ISingleWindow, IRememberWindowState
{
    public ToolboxWindow()
    {
        Title = "Toolbox";
        Width = 300;
        Height = 500;
    }
}
```

## Custom Window Position Storage

The default storage uses the Windows Registry. To use a custom backend (file, database, etc.), implement `IWindowPositionStorage` and register it on the builder.

```csharp
using Fenestra.Core;
using Fenestra.Core.Models;
using Fenestra.Wpf;
using Microsoft.Extensions.DependencyInjection;
using System.IO;
using System.Text.Json;
using System.Windows;

public class JsonWindowPositionStorage : IWindowPositionStorage
{
    private readonly string _directory;

    public JsonWindowPositionStorage()
    {
        _directory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "MyApp",
            "WindowPositions");

        Directory.CreateDirectory(_directory);
    }

    public WindowPositionData? Load(string key)
    {
        var path = GetPath(key);
        if (!File.Exists(path)) return null;

        var json = File.ReadAllText(path);
        return JsonSerializer.Deserialize<WindowPositionData>(json);
    }

    public void Save(string key, WindowPositionData data)
    {
        var json = JsonSerializer.Serialize(data);
        File.WriteAllText(GetPath(key), json);
    }

    public void Delete(string key)
    {
        var path = GetPath(key);
        if (File.Exists(path))
            File.Delete(path);
    }

    private string GetPath(string key)
    {
        var safe = string.Concat(key.Where(char.IsLetterOrDigit));
        return Path.Combine(_directory, $"{safe}.json");
    }
}

public partial class App : FenestraApp
{
    protected override void Configure(FenestraBuilder builder)
    {
        builder.UseWindowsPositionStorage<JsonWindowPositionStorage>();
        builder.RegisterWindows();
    }

    protected override Window CreateMainWindow(IServiceProvider services)
    {
        return services.GetRequiredService<MainWindow>();
    }
}

public class MainWindow : Window, IRememberWindowState
{
    public MainWindow()
    {
        Title = "My App";
        Width = 800;
        Height = 600;
    }
}
```

## Full Example

A complete application using window management, single-window enforcement, and position persistence:

```csharp
using Fenestra.Core;
using Fenestra.Wpf;
using Microsoft.Extensions.DependencyInjection;
using System.Windows;

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

public class MainWindow : Window, IRememberWindowState
{
    private readonly IWindowManager _windowManager;

    public MainWindow(IWindowManager windowManager)
    {
        _windowManager = windowManager;
        Title = "My Application";
        Width = 1024;
        Height = 768;
    }

    public void OpenPreferences()
    {
        _windowManager.Show<PreferencesWindow>();
    }

    public bool ConfirmDelete()
    {
        return _windowManager.ShowDialog<DeleteConfirmDialog>();
    }

    public void ShowAbout()
    {
        _windowManager.Show<AboutWindow>();
    }
}

public class PreferencesWindow : Window, ISingleWindow, IRememberWindowState
{
    public PreferencesWindow()
    {
        Title = "Preferences";
        Width = 500;
        Height = 400;
    }
}

public class AboutWindow : Window, ISingleWindow
{
    public AboutWindow()
    {
        Title = "About";
        Width = 350;
        Height = 250;
    }
}

public class DeleteConfirmDialog : Window, IDialog
{
    public DeleteConfirmDialog()
    {
        Title = "Confirm Delete";
        Width = 300;
        Height = 150;
    }
}
```

## References

- [IWindowManager](../src/Fenestra.Core/IWindowManager.cs)
- [IDialog / IDialog&lt;TResult&gt;](../src/Fenestra.Core/IDialog.cs)
- [ISingleWindow](../src/Fenestra.Core/ISingleWindow.cs)
- [IRememberWindowState](../src/Fenestra.Core/IRememberWindowState.cs)
- [IWindowPositionStorage](../src/Fenestra.Core/IWindowPositionStorage.cs)
- [WindowPositionData](../src/Fenestra.Core/Models/WindowPositionData.cs)
