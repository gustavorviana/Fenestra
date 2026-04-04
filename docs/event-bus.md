# Event Bus

## Summary

- [Overview](#overview)
- [Publishing Events](#publishing-events)
- [Subscribing to Events](#subscribing-to-events)
- [Unsubscribing](#unsubscribing)
- [Custom Event Types](#custom-event-types)
- [Full Example](#full-example)

## Overview

`IEventBus` is a typed async pub/sub event bus. It decouples publishers from subscribers using message types as keys. Any class can be used as a message type. The bus is registered as a singleton automatically and is thread-safe.

The subscription model uses `BusHandler<T>`, an async delegate:

```csharp
public delegate Task BusHandler<in T>(T message);
```

## Publishing Events

`PublishAsync<T>()` sends a message to all subscribers of that type. Handlers are invoked sequentially in registration order.

```csharp
using Fenestra.Core;
using System.Windows;

public class UserUpdatedEvent
{
    public string UserId { get; }
    public string NewName { get; }

    public UserUpdatedEvent(string userId, string newName)
    {
        UserId = userId;
        NewName = newName;
    }
}

public class SettingsWindow : Window
{
    private readonly IEventBus _eventBus;

    public SettingsWindow(IEventBus eventBus)
    {
        _eventBus = eventBus;
        Title = "Settings";
        Width = 400;
        Height = 300;
    }

    public async Task UpdateUserName(string userId, string newName)
    {
        // Perform the update...

        await _eventBus.PublishAsync(new UserUpdatedEvent(userId, newName));
    }
}
```

## Subscribing to Events

`On<T>()` registers a handler for a message type and returns an `IDisposable` subscription.

```csharp
using Fenestra.Core;
using System;
using System.Windows;

public class UserUpdatedEvent
{
    public string UserId { get; }
    public string NewName { get; }

    public UserUpdatedEvent(string userId, string newName)
    {
        UserId = userId;
        NewName = newName;
    }
}

public class MainWindow : Window
{
    private readonly IEventBus _eventBus;
    private readonly IDisposable _subscription;

    public MainWindow(IEventBus eventBus)
    {
        _eventBus = eventBus;
        Title = "Main";
        Width = 800;
        Height = 600;

        _subscription = _eventBus.On<UserUpdatedEvent>(OnUserUpdated);
    }

    private Task OnUserUpdated(UserUpdatedEvent e)
    {
        Title = $"User {e.UserId} is now {e.NewName}";
        return Task.CompletedTask;
    }
}
```

## Unsubscribing

Dispose the `IDisposable` returned by `On<T>()` to unsubscribe.

```csharp
using Fenestra.Core;
using System;
using System.Windows;

public class StatusChangedEvent
{
    public string Status { get; }

    public StatusChangedEvent(string status)
    {
        Status = status;
    }
}

public class MonitorWindow : Window
{
    private readonly IEventBus _eventBus;
    private IDisposable? _subscription;

    public MonitorWindow(IEventBus eventBus)
    {
        _eventBus = eventBus;
        Title = "Monitor";
        Width = 600;
        Height = 400;
    }

    public void StartListening()
    {
        _subscription = _eventBus.On<StatusChangedEvent>(e =>
        {
            Title = $"Status: {e.Status}";
            return Task.CompletedTask;
        });
    }

    public void StopListening()
    {
        _subscription?.Dispose();
        _subscription = null;
        // No further StatusChangedEvent messages will be received
    }
}
```

Disposing the subscription multiple times is safe -- subsequent calls are no-ops.

## Custom Event Types

Any `notnull` type can be used as an event. Use dedicated classes for clarity, or use simple types for lightweight messaging.

```csharp
using Fenestra.Core;
using System;
using System.Threading.Tasks;

// Simple record event
public record FileImportedEvent(string FilePath, int RecordCount);

// Event with nested data
public class BatchProcessCompleteEvent
{
    public string BatchId { get; }
    public int SuccessCount { get; }
    public int FailureCount { get; }
    public TimeSpan Duration { get; }

    public BatchProcessCompleteEvent(string batchId, int successCount, int failureCount, TimeSpan duration)
    {
        BatchId = batchId;
        SuccessCount = successCount;
        FailureCount = failureCount;
        Duration = duration;
    }
}

// Using primitive types (works but not recommended for complex scenarios)
public class NotificationService
{
    private readonly IEventBus _eventBus;

    public NotificationService(IEventBus eventBus)
    {
        _eventBus = eventBus;
    }

    public async Task NotifyAsync()
    {
        await _eventBus.PublishAsync("Simple string event");
        await _eventBus.PublishAsync(42);
    }

    public void Subscribe()
    {
        _eventBus.On<string>(msg =>
        {
            // msg => "Simple string event"
            return Task.CompletedTask;
        });
    }
}
```

## Full Example

A complete application with multiple components communicating through the event bus:

```csharp
using Fenestra.Core;
using Fenestra.Wpf;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

public record ItemAddedEvent(string ItemName, DateTime AddedAt);
public record ItemRemovedEvent(string ItemName);
public record ItemCountChangedEvent(int Count);

public partial class App : FenestraApp
{
    protected override void Configure(FenestraBuilder builder)
    {
        builder.RegisterWindows();
        builder.Services.AddSingleton<InventoryService>();
    }

    protected override Window CreateMainWindow(IServiceProvider services)
    {
        return services.GetRequiredService<MainWindow>();
    }
}

public class InventoryService
{
    private readonly IEventBus _eventBus;
    private int _count;

    public InventoryService(IEventBus eventBus)
    {
        _eventBus = eventBus;
    }

    public async Task AddItemAsync(string name)
    {
        _count++;
        await _eventBus.PublishAsync(new ItemAddedEvent(name, DateTime.Now));
        await _eventBus.PublishAsync(new ItemCountChangedEvent(_count));
    }

    public async Task RemoveItemAsync(string name)
    {
        _count--;
        await _eventBus.PublishAsync(new ItemRemovedEvent(name));
        await _eventBus.PublishAsync(new ItemCountChangedEvent(_count));
    }
}

public class MainWindow : Window
{
    private readonly IEventBus _eventBus;
    private readonly InventoryService _inventory;
    private readonly TextBlock _logText;
    private readonly TextBlock _countText;
    private readonly IDisposable _addedSub;
    private readonly IDisposable _removedSub;
    private readonly IDisposable _countSub;

    public MainWindow(IEventBus eventBus, InventoryService inventory)
    {
        _eventBus = eventBus;
        _inventory = inventory;
        Title = "Inventory Manager";
        Width = 600;
        Height = 400;

        var panel = new StackPanel { Margin = new Thickness(20) };

        _countText = new TextBlock { FontSize = 18, Text = "Items: 0" };
        _logText = new TextBlock { Margin = new Thickness(0, 10, 0, 0) };

        var addButton = new Button { Content = "Add Widget" };
        addButton.Click += async (_, _) => await _inventory.AddItemAsync("Widget");

        var removeButton = new Button { Content = "Remove Widget", Margin = new Thickness(0, 5, 0, 0) };
        removeButton.Click += async (_, _) => await _inventory.RemoveItemAsync("Widget");

        panel.Children.Add(_countText);
        panel.Children.Add(addButton);
        panel.Children.Add(removeButton);
        panel.Children.Add(_logText);
        Content = panel;

        _addedSub = _eventBus.On<ItemAddedEvent>(e =>
        {
            _logText.Text = $"Added: {e.ItemName} at {e.AddedAt:T}";
            return Task.CompletedTask;
        });

        _removedSub = _eventBus.On<ItemRemovedEvent>(e =>
        {
            _logText.Text = $"Removed: {e.ItemName}";
            return Task.CompletedTask;
        });

        _countSub = _eventBus.On<ItemCountChangedEvent>(e =>
        {
            _countText.Text = $"Items: {e.Count}";
            return Task.CompletedTask;
        });

        Closed += (_, _) =>
        {
            _addedSub.Dispose();
            _removedSub.Dispose();
            _countSub.Dispose();
        };
    }
}
```

## References

- [IEventBus](../src/Fenestra.Core/IEventBus.cs)
- [EventBus (implementation)](../src/Fenestra.Core/EventBus.cs)
