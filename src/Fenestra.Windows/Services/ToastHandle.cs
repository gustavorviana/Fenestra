using Fenestra.Windows.Models;

namespace Fenestra.Windows.Services;

/// <summary>
/// Windows implementation of <see cref="IToastHandle"/>.
/// </summary>
internal class ToastHandle : IToastHandle
{
    private readonly InternalNotificationHandle _internalHandle;
    private readonly ToastService _toastService;

    public string Tag => _internalHandle.Tag ?? string.Empty;
    public string? Group => _internalHandle.Group;
    public ToastHandleState State { get; private set; } = ToastHandleState.Active;
    public bool SuppressPopup => _internalHandle.SuppressPopup;
    public ToastPriority Priority => _internalHandle.Priority;
    public bool ExpiresOnReboot => _internalHandle.ExpiresOnReboot;
    public DateTimeOffset? ExpirationTime => _internalHandle.ExpirationTime;
    public NotificationMirroring NotificationMirroring => _internalHandle.NotificationMirroring;
    public string? RemoteId => _internalHandle.RemoteId;

    public event EventHandler<ToastActivatedArgs>? Activated;
    public event EventHandler<ToastDismissalReason>? Dismissed;
    public event EventHandler<int>? Failed;

    internal ToastHandle(ToastService toastService, InternalNotificationHandle internalHandle)
    {
        _toastService = toastService;
        _internalHandle = internalHandle;
    }

    public NotificationUpdateResult Update(Dictionary<string, string> data)
    {
        if (State != ToastHandleState.Active) return NotificationUpdateResult.Failed;
        return _internalHandle.Update(data);
    }

    public NotificationUpdateResult Update(Action<ToastBuilder> configure)
    {
        if (State != ToastHandleState.Active) return NotificationUpdateResult.Failed;

        var builder = new ToastBuilder();
        configure(builder);
        var content = builder.Build();
        content.Tag = Tag;
        content.Group = Group;

        _internalHandle.ReplaceInternal(content);
        return NotificationUpdateResult.Succeeded;
    }

    public void Hide()
    {
        if (State != ToastHandleState.Active) return;
        State = ToastHandleState.Removed;
        _internalHandle.HideNotification();
        _toastService.OnHandleDisposed(this);
    }

    public void Remove()
    {
        if (State != ToastHandleState.Active) return;
        State = ToastHandleState.Removed;
        _internalHandle.RemoveInternal(Tag, Group);
        _toastService.OnHandleDisposed(this);
    }

    public void RemoveGroup()
    {
        if (State != ToastHandleState.Active || Group == null) return;
        _internalHandle.RemoveGroupInternal(Group);
    }

    public void Dispose()
    {
        if (State != ToastHandleState.Active) return;
        Remove();
    }

    internal void RaiseActivated(ToastActivatedArgs args) => Activated?.Invoke(this, args);

    internal void RaiseDismissed(ToastDismissalReason reason)
    {
        State = ToastHandleState.Dismissed;
        Dismissed?.Invoke(this, reason);
        _toastService.OnHandleDisposed(this);
    }

    internal void RaiseFailed(int errorCode)
    {
        State = ToastHandleState.Failed;
        Failed?.Invoke(this, errorCode);
        _toastService.OnHandleDisposed(this);
    }
}
