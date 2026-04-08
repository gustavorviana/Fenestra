# Window Position Persistence

> **Windows only (default).** The default implementation stores window position in the Windows Registry. Custom implementations can target any platform.

Saves and restores window position, size, and state (maximized, normal) across application restarts.

## Setup

```csharp
var app = FenestraBuilder.CreateDefault()
    .UseAppInfo("My App", new Version(1, 0, 0))
    .UseWindowsPositionStorage<RegistryWindowPositionStorage>()
    .Build();
```

## Mark Windows to Remember

Implement the `IRememberWindowState` marker interface on any window that should persist its position:

```csharp
using Fenestra.Core;

public partial class MainWindow : Window, IRememberWindowState
{
    // Position, size, and window state are automatically saved on close
    // and restored on next launch.
}
```

That's it. No additional code needed — the framework handles save/restore automatically.

## Custom Storage Backend

Implement `IWindowPositionStorage` to store positions anywhere (e.g., JSON file, database):

```csharp
using Fenestra.Core;

public class JsonWindowPositionStorage : IWindowPositionStorage
{
    public WindowPositionData? Load(string windowId) { /* ... */ }
    public void Save(string windowId, WindowPositionData data) { /* ... */ }
}
```

Then register it:

```csharp
.UseWindowsPositionStorage<JsonWindowPositionStorage>()
```
