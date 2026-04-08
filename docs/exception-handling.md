# Exception Handling

## Summary

- [Overview](#overview)
- [Default Behavior](#default-behavior)
- [FenestraExceptionContext](#fenestraexceptioncontext)
- [Custom Exception Handler](#custom-exception-handler)
- [Exception Sources](#exception-sources)
- [Full Example](#full-example)

## Overview

Fenestra automatically catches unhandled exceptions from three sources: the WPF dispatcher, the `AppDomain`, and unobserved `Task` exceptions. All exceptions are routed through an `IExceptionHandler` implementation resolved from the DI container.

A default handler is registered via `TryAddSingleton`, which means registering your own `IExceptionHandler` before building the app replaces the default.

## Default Behavior

The built-in `DefaultExceptionHandler` logs the exception and shows a `MessageBox` to the user. Critical exceptions are logged at `Critical` level; non-critical ones at `Error` level. The handler always marks the exception as handled.

```
An unexpected error occurred. The application may need to restart.
```

## FenestraExceptionContext

Every exception is wrapped in a `FenestraExceptionContext` that provides metadata about the error.

| Property | Type | Description |
|---|---|---|
| `Exception` | `Exception` | The exception that was thrown |
| `IsCritical` | `bool` | `true` for `AppDomain.UnhandledException` (application may not be recoverable) |
| `Handled` | `bool` | Set to `true` to prevent the exception from crashing the application |

- **Dispatcher exceptions** (`IsCritical = false`): When `Handled` is set to `true`, the WPF dispatcher marks the event as handled and the application continues.
- **AppDomain exceptions** (`IsCritical = true`): The application is in an unstable state. Setting `Handled` prevents the default crash behavior but recovery is not guaranteed.
- **Task exceptions** (`IsCritical = false`): When `Handled` is set to `true`, the unobserved exception is marked as observed.

## Custom Exception Handler

Implement `IExceptionHandler` and register it in the DI container. Your registration takes precedence over the default handler.

```csharp
using Fenestra.Core;
using Fenestra.Core.Models;
using Fenestra.Wpf;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Windows;

public partial class App : FenestraApp
{
    protected override void Configure(WpfFenestraBuilder builder)
    {
        builder.RegisterWindows();
        builder.Services.AddSingleton<IExceptionHandler, CustomExceptionHandler>();
    }

    protected override Window CreateMainWindow(IServiceProvider services)
    {
        return services.GetRequiredService<MainWindow>();
    }
}

public class CustomExceptionHandler : IExceptionHandler
{
    private readonly ILogger<CustomExceptionHandler> _logger;

    public CustomExceptionHandler(ILogger<CustomExceptionHandler> logger)
    {
        _logger = logger;
    }

    public void Handle(FenestraExceptionContext context)
    {
        _logger.LogError(context.Exception, "Unhandled exception (Critical: {IsCritical})", context.IsCritical);

        if (context.IsCritical)
        {
            WriteCrashLog(context.Exception);
            context.Handled = true;
        }
        else
        {
            context.Handled = true;
        }
    }

    private static void WriteCrashLog(Exception exception)
    {
        string path = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "MyApp",
            "crash.log");

        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.AppendAllText(path, $"[{DateTime.Now:O}] {exception}\n\n");
    }
}

public class MainWindow : Window
{
    public MainWindow()
    {
        Title = "Custom Handler Demo";
        Width = 800;
        Height = 600;
    }
}
```

## Exception Sources

| Source | `IsCritical` | When |
|---|---|---|
| `DispatcherUnhandledException` | `false` | Exception on the WPF UI thread |
| `AppDomain.UnhandledException` | `true` | Exception on any thread that is not caught |
| `TaskScheduler.UnobservedTaskException` | `false` | Faulted `Task` that was garbage collected without observation |

## Full Example

A handler that shows a user-friendly dialog for non-critical errors and writes crash dumps for critical ones:

```csharp
using Fenestra.Core;
using Fenestra.Core.Models;
using Fenestra.Wpf;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;

public partial class App : FenestraApp
{
    protected override void Configure(WpfFenestraBuilder builder)
    {
        builder.UseAppName("RobustApp");
        builder.RegisterWindows();
        builder.Services.AddSingleton<IExceptionHandler, AppExceptionHandler>();
    }

    protected override Window CreateMainWindow(IServiceProvider services)
    {
        return services.GetRequiredService<MainWindow>();
    }
}

public class AppExceptionHandler : IExceptionHandler
{
    private readonly ILogger<AppExceptionHandler> _logger;
    private readonly IDialogService _dialogService;

    public AppExceptionHandler(ILogger<AppExceptionHandler> logger, IDialogService dialogService)
    {
        _logger = logger;
        _dialogService = dialogService;
    }

    public void Handle(FenestraExceptionContext context)
    {
        if (context.IsCritical)
        {
            _logger.LogCritical(context.Exception, "Critical unhandled exception.");

            string crashPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "RobustApp",
                "crashes",
                $"crash_{DateTime.Now:yyyyMMdd_HHmmss}.log");

            Directory.CreateDirectory(Path.GetDirectoryName(crashPath)!);
            File.WriteAllText(crashPath, context.Exception.ToString());

            context.Handled = true;
        }
        else
        {
            _logger.LogError(context.Exception, "Non-critical unhandled exception.");

            try
            {
                _dialogService.ShowMessage(
                    $"An error occurred: {context.Exception.Message}",
                    title: "Error",
                    buttons: FenestraMessageButton.OK,
                    icon: FenestraMessageIcon.Error);
            }
            catch
            {
                // UI thread may not be available
            }

            context.Handled = true;
        }
    }
}

public class MainWindow : Window
{
    public MainWindow()
    {
        Title = "Robust App";
        Width = 600;
        Height = 400;

        var panel = new StackPanel { Margin = new Thickness(20) };

        var throwButton = new Button { Content = "Throw UI Exception" };
        throwButton.Click += (_, _) => throw new InvalidOperationException("Test UI exception");

        var throwTaskButton = new Button { Content = "Throw Task Exception", Margin = new Thickness(0, 5, 0, 0) };
        throwTaskButton.Click += (_, _) =>
        {
            Task.Run(() => throw new InvalidOperationException("Test background exception"));
        };

        panel.Children.Add(throwButton);
        panel.Children.Add(throwTaskButton);
        Content = panel;
    }
}
```

## References

- [IExceptionHandler](../src/Fenestra.Core/IExceptionHandler.cs)
- [FenestraExceptionContext](../src/Fenestra.Core/Models/FenestraExceptionContext.cs)
- [DefaultExceptionHandler](../src/Fenestra.Windows.Wpf/Services/DefaultExceptionHandler.cs)
