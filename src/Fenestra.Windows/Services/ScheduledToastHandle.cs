using Fenestra.Windows.Native;
using Fenestra.Windows.Native.Toast;

namespace Fenestra.Windows.Services;

internal class ScheduledToastHandle : IScheduledToastHandle
{
    private readonly IComRef<IScheduledToastNotification> _scheduled;
    private readonly INativeToastNotifier _notifier;
    private readonly ToastService _service;
    private bool _disposed;

    public string? Id { get; }
    public string? Tag { get; }
    public string? Group { get; }
    public DateTimeOffset DeliveryTime { get; }
    public bool SuppressPopup { get; }

    internal ScheduledToastHandle(
        ToastService service,
        INativeToastNotifier notifier,
        IScheduledToastNotification scheduled,
        string? tag,
        string? group,
        DateTimeOffset deliveryTime,
        bool suppressPopup)
    {
        _service = service;
        _notifier = notifier;
        _scheduled = new ComRef<IScheduledToastNotification>(scheduled);
        Tag = tag;
        Group = group;
        DeliveryTime = deliveryTime;
        SuppressPopup = suppressPopup;

        if (scheduled.get_Id(out var id) == 0)
            Id = id;
    }

    public void Cancel()
    {
        if (_disposed) return;
        _disposed = true;
        try { _notifier.RemoveFromSchedule(_scheduled.Value); }
        catch { /* already removed or expired */ }
        _service.OnScheduledHandleDisposed(this);
        _scheduled.Dispose();
    }

    public void Dispose() => Cancel();
}
