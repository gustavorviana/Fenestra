using Fenestra.Core;
using Fenestra.Core.Models;

namespace Fenestra.Toast.Windows.Services;

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

    public event EventHandler<ToastActivatedArgs>? Activated;
    public event EventHandler<ToastDismissalReason>? Dismissed;
    public event EventHandler<int>? Failed;

    internal ToastHandle(ToastService toastService, InternalNotificationHandle internalHandle)
    {
        _toastService = toastService;
        _internalHandle = internalHandle;
    }

    public void Update(Dictionary<string, string> data)
    {
        if (State != ToastHandleState.Active) return;
        _internalHandle.Update(data);
    }

    public void Update(Action<ToastBuilder> configure)
    {
        if (State != ToastHandleState.Active) return;

        var builder = new ToastBuilder();
        configure(builder);
        var content = builder.Build();
        content.Tag = Tag;
        content.Group = Group;

        _internalHandle.ReplaceInternal(content);
    }

    public void Hide()
    {
        if (State != ToastHandleState.Active) return;
        State = ToastHandleState.Removed;
        _internalHandle.RemoveInternal(Tag, Group);
        _toastService.OnHandleDisposed(this);
    }

    public void Remove()
    {
        if (State != ToastHandleState.Active) return;
        State = ToastHandleState.Removed;
        _internalHandle.RemoveInternal(Tag, Group);
        _toastService.OnHandleDisposed(this);
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
