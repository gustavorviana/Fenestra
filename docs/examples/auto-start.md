# Auto-Start

> **Windows only.** Registers the application to run automatically on Windows startup via the registry (HKCU or HKLM Run keys).

## Setup

```csharp
var app = FenestraBuilder.CreateDefault()
    .UseAppInfo("My App", new Version(1, 0, 0))
    .UseWindowsAutoStart()
    .Build();
```

## Enable Auto-Start

```csharp
using Fenestra.Windows;

public class SettingsViewModel
{
    private readonly IAutoStartService _autoStart;

    public SettingsViewModel(IAutoStartService autoStart)
    {
        _autoStart = autoStart;
    }

    public void EnableStartup()
    {
        _autoStart.Register();
    }

    public void DisableStartup()
    {
        _autoStart.Unregister();
    }

    public bool IsEnabled => _autoStart.IsRegistered;
}
```

## With Custom Arguments

```csharp
_autoStart.Register("--minimized --silent");
```

## Check Status

```csharp
// StartupStatus: Approved, Denied, ChangedByUser, etc.

var status = _autoStart.GetStatus();
if (status == StartupStatus.Denied)
    Console.WriteLine("User disabled auto-start in Task Manager.");
```
