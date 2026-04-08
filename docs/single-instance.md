# Single Instance

## Summary

- [Overview](#overview)
- [Enabling Single Instance](#enabling-single-instance)
- [Receiving Forwarded Arguments](#receiving-forwarded-arguments)
- [How It Works](#how-it-works)
- [Full Example](#full-example)

## Overview

Fenestra supports single-instance mode to prevent multiple copies of the application from running simultaneously. When a second instance launches, its command-line arguments are forwarded to the already-running instance via a named pipe, and the second instance exits immediately.

## Enabling Single Instance

Call `UseWindowsSingleInstance()` on the builder during configuration.

```csharp
using Fenestra.Wpf;
using Microsoft.Extensions.DependencyInjection;
using System.Windows;

public partial class App : FenestraApp
{
    protected override void Configure(FenestraBuilder builder)
    {
        builder.UseWindowsSingleInstance();
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

With this configuration, launching the application a second time sends the new instance's arguments to the running instance and shuts down the duplicate process.

## Receiving Forwarded Arguments

To handle arguments forwarded from subsequent launches, implement `ISingleInstanceApp` on a class and register it in the DI container.

```csharp
using Fenestra.Core;
using Fenestra.Wpf;
using Microsoft.Extensions.DependencyInjection;
using System.Windows;

public partial class App : FenestraApp
{
    protected override void Configure(FenestraBuilder builder)
    {
        builder.UseWindowsSingleInstance();
        builder.RegisterWindows();
        builder.Services.AddSingleton<ISingleInstanceApp, ArgumentHandler>();
    }

    protected override Window CreateMainWindow(IServiceProvider services)
    {
        return services.GetRequiredService<MainWindow>();
    }
}

public class ArgumentHandler : ISingleInstanceApp
{
    public void OnArgumentsReceived(string[] args)
    {
        // Called on the UI thread when another instance launches.
        // args contains the command-line arguments from the second instance.
        if (args.Length > 0)
        {
            // Example: open a file passed as argument
            string filePath = args[0];
            // Process the file...
        }
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

> **Tip:** `OnArgumentsReceived` is dispatched on the UI thread, so you can safely interact with UI elements directly.

## How It Works

1. On startup, a system `Mutex` named `Fenestra_{AppId}` is created.
2. If the mutex is acquired (first instance), a named pipe server starts listening for incoming arguments.
3. If the mutex is already held (second instance), the new process connects to the named pipe, sends its arguments as a tab-separated UTF-8 string, and exits.
4. The first instance deserializes the arguments and dispatches `ISingleInstanceApp.OnArgumentsReceived` on the UI thread via `Dispatcher.BeginInvoke`.

The pipe name is derived from `AppInfo.AppId`, ensuring that different applications using Fenestra do not conflict.

## Full Example

A file-opening application that handles arguments from duplicate launches:

```csharp
using Fenestra.Core;
using Fenestra.Wpf;
using Microsoft.Extensions.DependencyInjection;
using System.IO;
using System.Windows;
using System.Windows.Controls;

public partial class App : FenestraApp
{
    protected override void Configure(FenestraBuilder builder)
    {
        builder.UseAppName("FileViewer");
        builder.UseWindowsSingleInstance();
        builder.RegisterWindows();
        builder.Services.AddSingleton<ISingleInstanceApp, FileOpenHandler>();
    }

    protected override Window CreateMainWindow(IServiceProvider services)
    {
        return services.GetRequiredService<MainWindow>();
    }
}

public class FileOpenHandler : ISingleInstanceApp
{
    private readonly IWindowManager _windowManager;

    public FileOpenHandler(IWindowManager windowManager)
    {
        _windowManager = windowManager;
    }

    public void OnArgumentsReceived(string[] args)
    {
        foreach (string arg in args)
        {
            if (File.Exists(arg))
            {
                MainWindow? mainWindow = _windowManager.GetOpenWindow<MainWindow>();
                if (mainWindow != null)
                {
                    mainWindow.OpenFile(arg);
                }
            }
        }
    }
}

public class MainWindow : Window
{
    private readonly TextBlock _content;

    public MainWindow()
    {
        Title = "File Viewer";
        Width = 800;
        Height = 600;

        _content = new TextBlock();
        Content = _content;
    }

    public void OpenFile(string path)
    {
        Title = $"File Viewer - {Path.GetFileName(path)}";
        _content.Text = File.ReadAllText(path);
    }
}
```

Running this application with `FileViewer.exe document.txt` opens the file. Launching a second instance with `FileViewer.exe notes.txt` sends `notes.txt` to the running instance, which opens it without spawning a new window.

## References

- [ISingleInstanceApp](../src/Fenestra.Core/ISingleInstanceApp.cs)
- [SingleInstanceGuard](../src/Fenestra.Windows.Wpf/Services/SingleInstanceGuard.cs)
