# Event Bus

> Available on all platforms. `IEventBus` is a core service registered automatically — no builder call needed.

Typed async pub/sub messaging for decoupled communication between components.

## Define an Event

```csharp
public record FileDownloaded(string FileName, long Size);
```

## Subscribe

```csharp
using Fenestra.Core;

public class StatusBarViewModel
{
    public StatusBarViewModel(IEventBus events)
    {
        events.Subscribe<FileDownloaded>(OnFileDownloaded);
    }

    private Task OnFileDownloaded(FileDownloaded e)
    {
        StatusText = $"Downloaded: {e.FileName} ({e.Size} bytes)";
        return Task.CompletedTask;
    }
}
```

## Publish

```csharp
public class DownloadService
{
    private readonly IEventBus _events;

    public DownloadService(IEventBus events) => _events = events;

    public async Task Download(string url)
    {
        var file = await DownloadFile(url);
        await _events.Publish(new FileDownloaded(file.Name, file.Size));
    }
}
```

## Unsubscribe

`Subscribe` returns a disposable. Dispose it to unsubscribe:

```csharp
var subscription = _events.Subscribe<FileDownloaded>(OnFileDownloaded);

// Later
subscription.Dispose();
```
