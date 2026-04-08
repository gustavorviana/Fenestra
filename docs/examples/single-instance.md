# Single Instance

> **Windows only.** Uses a named mutex to ensure only one instance of the application runs at a time. Subsequent launches forward their arguments to the first instance.

## Setup

```csharp
var builder = FenestraApplication.CreateBuilder();
builder.UseAppInfo("My App", new Version(1, 0, 0));
builder.Services.AddWpfSingleInstance();
var app = builder.Build();
```

## Receive Forwarded Arguments

Implement `ISingleInstanceApp` on your main window or a service:

```csharp
using Fenestra.Core;

public partial class MainWindow : Window, ISingleInstanceApp
{
    public void OnArgumentsReceived(string[] args)
    {
        // Called when another instance tries to launch
        // args = command-line arguments from the second instance

        if (args.Length > 0)
            OpenFile(args[0]);

        // Bring this window to the foreground
        Activate();
    }
}
```

## How It Works

1. First launch: application starts normally
2. Second launch: detects existing instance, forwards its command-line arguments via IPC, then exits
3. First instance: receives arguments via `OnArgumentsReceived` callback
