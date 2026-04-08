# Taskbar Progress

## Summary

- [Overview](#overview)
- [Creating a Progress Indicator](#creating-a-progress-indicator)
- [Setting Progress Value](#setting-progress-value)
- [Setting Progress State](#setting-progress-state)
- [TaskbarProgressState Reference](#taskbarprogressstate-reference)
- [Full Example](#full-example)

## Overview

`ITaskbarProvider` creates taskbar progress indicators for WPF windows. The progress is displayed on the window's taskbar button, providing visual feedback to the user even when the application is minimized.

Each window can have only one active `ITaskbarProgress` instance at a time. Dispose the current instance before creating a new one.

`ITaskbarProvider` is registered automatically and requires no builder configuration.

## Creating a Progress Indicator

Call `Create()` with the target window to get an `ITaskbarProgress` instance. Dispose it when the operation is complete to clear the progress display.

```csharp
using Fenestra.Windows;
using System.Windows;

public class MainWindow : Window
{
    private readonly ITaskbarProvider _taskbarProvider;

    public MainWindow(ITaskbarProvider taskbarProvider)
    {
        _taskbarProvider = taskbarProvider;
        Title = "Taskbar Demo";
        Width = 800;
        Height = 600;
    }

    public async Task RunOperationAsync()
    {
        using ITaskbarProgress progress = _taskbarProvider.Create(this);

        for (int i = 0; i <= 100; i += 10)
        {
            progress.SetProgress(i / 100.0);
            await Task.Delay(200);
        }
        // Disposing clears the progress bar from the taskbar
    }
}
```

## Setting Progress Value

`SetProgress()` accepts a `double` value between `0.0` (empty) and `1.0` (full).

```csharp
using Fenestra.Windows;
using System.Windows;

public class MainWindow : Window
{
    private readonly ITaskbarProvider _taskbarProvider;

    public MainWindow(ITaskbarProvider taskbarProvider)
    {
        _taskbarProvider = taskbarProvider;
        Title = "Progress Demo";
        Width = 800;
        Height = 600;
    }

    public async Task DownloadFileAsync()
    {
        using ITaskbarProgress progress = _taskbarProvider.Create(this);

        progress.SetProgress(0.0);   // 0% - empty bar
        await Task.Delay(500);

        progress.SetProgress(0.25);  // 25%
        await Task.Delay(500);

        progress.SetProgress(0.5);   // 50%
        await Task.Delay(500);

        progress.SetProgress(0.75);  // 75%
        await Task.Delay(500);

        progress.SetProgress(1.0);   // 100% - full bar
    }
}
```

## Setting Progress State

`SetState()` changes the visual style of the progress bar.

```csharp
using Fenestra.Windows;
using Fenestra.Windows.Models;
using System.Windows;

public class MainWindow : Window
{
    private readonly ITaskbarProvider _taskbarProvider;

    public MainWindow(ITaskbarProvider taskbarProvider)
    {
        _taskbarProvider = taskbarProvider;
        Title = "State Demo";
        Width = 800;
        Height = 600;
    }

    public void ShowIndeterminate()
    {
        ITaskbarProgress progress = _taskbarProvider.Create(this);
        progress.SetState(TaskbarProgressState.Indeterminate);
        // Shows a pulsing/marquee animation on the taskbar button
    }

    public void ShowError()
    {
        ITaskbarProgress progress = _taskbarProvider.Create(this);
        progress.SetProgress(0.7);
        progress.SetState(TaskbarProgressState.Error);
        // Shows a red progress bar at 70%
    }

    public void ShowPaused()
    {
        ITaskbarProgress progress = _taskbarProvider.Create(this);
        progress.SetProgress(0.5);
        progress.SetState(TaskbarProgressState.Paused);
        // Shows a yellow/paused progress bar at 50%
    }
}
```

## TaskbarProgressState Reference

| Value | Description |
|---|---|
| `None` | No progress indicator displayed |
| `Indeterminate` | Pulsing/marquee animation (no specific value) |
| `Normal` | Green progress bar |
| `Paused` | Yellow progress bar (operation paused) |
| `Error` | Red progress bar (operation failed) |

## Full Example

A file processing application that shows progress on the taskbar with error handling:

```csharp
using Fenestra.Core;
using Fenestra.Windows;
using Fenestra.Windows.Models;
using Fenestra.Wpf;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

public partial class App : FenestraApp
{
    protected override void Configure(WpfFenestraBuilder builder)
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
    private readonly ITaskbarProvider _taskbarProvider;
    private readonly TextBlock _statusText;

    public MainWindow(ITaskbarProvider taskbarProvider)
    {
        _taskbarProvider = taskbarProvider;
        Title = "File Processor";
        Width = 600;
        Height = 400;

        var panel = new StackPanel { Margin = new Thickness(20) };

        _statusText = new TextBlock { FontSize = 14, Text = "Ready" };

        var processButton = new Button { Content = "Process Files", Margin = new Thickness(0, 10, 0, 0) };
        processButton.Click += async (_, _) => await ProcessFilesAsync();

        var errorButton = new Button { Content = "Simulate Error", Margin = new Thickness(0, 5, 0, 0) };
        errorButton.Click += async (_, _) => await SimulateErrorAsync();

        var indeterminateButton = new Button { Content = "Indeterminate Task", Margin = new Thickness(0, 5, 0, 0) };
        indeterminateButton.Click += async (_, _) => await RunIndeterminateAsync();

        panel.Children.Add(_statusText);
        panel.Children.Add(processButton);
        panel.Children.Add(errorButton);
        panel.Children.Add(indeterminateButton);
        Content = panel;
    }

    private async Task ProcessFilesAsync()
    {
        using ITaskbarProgress progress = _taskbarProvider.Create(this);
        progress.SetState(TaskbarProgressState.Normal);

        int totalFiles = 50;
        for (int i = 0; i < totalFiles; i++)
        {
            double percent = (i + 1) / (double)totalFiles;
            progress.SetProgress(percent);
            _statusText.Text = $"Processing file {i + 1} of {totalFiles}...";
            await Task.Delay(100);
        }

        _statusText.Text = "Processing complete!";
    }

    private async Task SimulateErrorAsync()
    {
        using ITaskbarProgress progress = _taskbarProvider.Create(this);
        progress.SetState(TaskbarProgressState.Normal);

        for (int i = 0; i <= 6; i++)
        {
            progress.SetProgress(i / 10.0);
            _statusText.Text = $"Processing... {i * 10}%";
            await Task.Delay(300);
        }

        progress.SetState(TaskbarProgressState.Error);
        _statusText.Text = "Error: Processing failed at 60%";

        await Task.Delay(3000);
        _statusText.Text = "Ready";
    }

    private async Task RunIndeterminateAsync()
    {
        using ITaskbarProgress progress = _taskbarProvider.Create(this);
        progress.SetState(TaskbarProgressState.Indeterminate);
        _statusText.Text = "Running background task...";

        await Task.Delay(5000);

        progress.SetState(TaskbarProgressState.Normal);
        progress.SetProgress(1.0);
        _statusText.Text = "Background task complete!";

        await Task.Delay(1000);
        _statusText.Text = "Ready";
    }
}
```

> **Attention:** Calling `Create()` on a window that already has an active (non-disposed) `ITaskbarProgress` throws `InvalidOperationException`. Always dispose the previous instance first.

## References

- [ITaskbarProvider](../src/Fenestra.Windows/ITaskbarProvider.cs)
- [ITaskbarProgress](../src/Fenestra.Windows/ITaskbarProgress.cs)
- [TaskbarProgressState](../src/Fenestra.Windows/Models/TaskbarProgressState.cs)
