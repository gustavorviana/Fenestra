# Taskbar Progress

> **Windows 7+ only.** Uses the ITaskbarList3 COM interface to show progress on the taskbar button. No builder call needed — registered automatically.

## Basic Usage

```csharp
using Fenestra.Windows;

public class DownloadViewModel
{
    private readonly ITaskbarProgress _taskbar;

    public DownloadViewModel(ITaskbarProvider taskbarProvider)
    {
        _taskbar = taskbarProvider.GetProgress();
    }

    public async Task DownloadFile()
    {
        _taskbar.SetState(TaskbarProgressState.Normal);

        for (int i = 0; i <= 100; i += 10)
        {
            _taskbar.SetValue(i / 100.0);
            await Task.Delay(200);
        }

        _taskbar.SetState(TaskbarProgressState.None);  // Clear progress
    }
}
```

## Progress States

```csharp
// TaskbarProgressState: None, Normal, Paused, Error, Indeterminate

_taskbar.SetState(TaskbarProgressState.Normal);         // Green progress bar
_taskbar.SetState(TaskbarProgressState.Paused);         // Yellow (paused)
_taskbar.SetState(TaskbarProgressState.Error);          // Red (error)
_taskbar.SetState(TaskbarProgressState.Indeterminate);  // Pulsing animation
_taskbar.SetState(TaskbarProgressState.None);           // Clear/hide
```

## Error State Example

```csharp
try
{
    _taskbar.SetState(TaskbarProgressState.Normal);
    _taskbar.SetValue(0.5);
    await DoWork();
    _taskbar.SetState(TaskbarProgressState.None);
}
catch
{
    _taskbar.SetState(TaskbarProgressState.Error);
    _taskbar.SetValue(1.0);  // Show full red bar
}
```
