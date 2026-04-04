using Fenestra.Core;
using Fenestra.Core.Models;

namespace Fenestra.Toast.Windows.Services;

/// <summary>
/// Windows implementation of <see cref="IToastHandle"/>.
/// </summary>
internal class ToastHandle : IToastHandle
{
    private readonly ToastService _service;

    public string Tag { get; }
    public string? Group { get; }
    public ToastHandleState State { get; private set; } = ToastHandleState.Active;

    public event EventHandler<ToastActivatedArgs>? Activated;
    public event EventHandler<ToastDismissalReason>? Dismissed;
    public event EventHandler<int>? Failed;

    internal ToastHandle(string tag, string? group, ToastService service)
    {
        Tag = tag;
        Group = group;
        _service = service;
    }

    public void Update(Dictionary<string, string> data, uint sequenceNumber = 0)
    {
        if (State != ToastHandleState.Active) return;
        _service.UpdateInternal(Tag, data, sequenceNumber, Group);
    }

    public void Update(Action<ToastBuilder> configure)
    {
        if (State != ToastHandleState.Active) return;

        var builder = new ToastBuilder();
        configure(builder);
        var content = builder.Build();
        content.Tag = Tag;
        content.Group = Group;
        _service.ReplaceInternal(content, this);
    }

    public void Hide()
    {
        if (State != ToastHandleState.Active) return;
        State = ToastHandleState.Removed;
        _service.RemoveInternal(Tag, Group);
        _service.OnHandleDisposed(this);
    }

    public void Remove()
    {
        if (State != ToastHandleState.Active) return;
        State = ToastHandleState.Removed;
        _service.RemoveInternal(Tag, Group);
        _service.OnHandleDisposed(this);
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
        _service.OnHandleDisposed(this);
    }

    internal void RaiseFailed(int errorCode)
    {
        State = ToastHandleState.Failed;
        Failed?.Invoke(this, errorCode);
        _service.OnHandleDisposed(this);
    }
}
