using Fenestra.Core;
using Fenestra.Core.Models;
using Fenestra.Windows.Models;
using Fenestra.Windows.Native;
using Fenestra.Windows.Native.Toast;

namespace Fenestra.Windows.Services;

/// <summary>
/// Displays Windows 10+ toast notifications.
/// </summary>
internal class ToastService : IToastService, IDisposable
{
    private readonly string _appId;
    private readonly IWinRtInterop _interop;
    private readonly IThreadContext _threadContext;
    private readonly IApplicationActivator? _activator;
    private readonly IXmlToastFactory _xmlToastFactory;
    private readonly List<ToastHandle> _active = new();
    private readonly List<ScheduledToastHandle> _scheduled = new();
    private INativeToastNotifier? _notifier = null!;
    private bool _disposed;

    public IReadOnlyList<IToastHandle> Active
    {
        get { lock (_active) return _active.ToArray(); }
    }

    public IReadOnlyList<IScheduledToastHandle> Scheduled
    {
        get { lock (_scheduled) return _scheduled.ToArray(); }
    }

    public ToastService(
        AppInfo appInfo,
        IThreadContext threadContext,
        IWinRtInterop? interop = null,
        IApplicationActivator? activator = null,
        IAumidRegistrationManager? registrationManager = null,
        INativeToastNotifierFactory? notifierFactory = null,
        IXmlToastFactory? xmlToastFactory = null)
    {
        Platform.EnsureWindows10();

        _appId = appInfo.AppId;
        _interop = interop ?? new WinRtInterop();
        _threadContext = threadContext;
        _activator = activator;
        _xmlToastFactory = xmlToastFactory ?? DefaultXmlToastFactory.Instance;

        registrationManager?.EnsureRegistered();
        _interop.SetCurrentProcessExplicitAppUserModelID(_appId);
        _notifier = (notifierFactory ?? DefaultNativeToastNotifierFactory.Instance).Create(_appId, _interop);
    }

    public IToastHandle Show(ToastContent toast)
    {
        if (string.IsNullOrEmpty(toast.Tag))
            toast.Tag = $"toast-{Guid.NewGuid():N}";

        if (_notifier == null)
            return null!;

        using var pXmlDoc = _xmlToastFactory.Create(toast, _interop);
        var internalHandle = pXmlDoc.CreateNotification(_notifier!);

        var handle = new ToastHandle(this, internalHandle);
        lock (_active) _active.Add(handle);

        // Wire COM event callbacks → marshal to UI thread → raise on ToastHandle
        internalHandle.OnActivated = args =>
        {
            try
            {
                _ = _threadContext.InvokeAsync(() =>
                {
                    _activator?.BringToForeground();
                    handle.RaiseActivated(args);
                });
            }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"[Fenestra.Toast] {ex.Message}"); }
        };
        internalHandle.OnDismissed = reason =>
        {
            try { _ = _threadContext.InvokeAsync(() => handle.RaiseDismissed(reason)); }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"[Fenestra.Toast] {ex.Message}"); }
        };
        internalHandle.OnFailed = errorCode =>
        {
            try { _ = _threadContext.InvokeAsync(() => handle.RaiseFailed(errorCode)); }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"[Fenestra.Toast] {ex.Message}"); }
        };

        internalHandle.Show(toast.ProgressTracker);

        return handle;
    }

    public IToastHandle Show(Action<ToastBuilder> configure)
    {
        var builder = new ToastBuilder();
        configure(builder);
        return Show(builder.Build());
    }

    public void ClearHistory()
    {
        if (_notifier == null) return;
        try { _notifier.History?.ClearWithId(_appId); }
        catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"[Fenestra.Toast] {ex.Message}"); }
        lock (_active) _active.Clear();
    }

    public void ClearHistory(string group)
    {
        if (_notifier == null) return;
        try { _notifier.History?.RemoveGroup(group); }
        catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"[Fenestra.Toast] {ex.Message}"); }
        lock (_active) _active.RemoveAll(h => h.Group == group);
    }

    public void ClearHistory(string tag, string group)
    {
        if (_notifier == null) return;
        try { _notifier.History?.RemoveGroupedTag(tag, group); }
        catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"[Fenestra.Toast] {ex.Message}"); }
        lock (_active) _active.RemoveAll(h => h.Tag == tag && h.Group == group);
    }

    public NotificationSetting GetSetting()
        => _notifier?.GetSetting() ?? NotificationSetting.DisabledForApplication;

    public IToastHandle? FindByTag(string tag)
    {
        lock (_active) return _active.FirstOrDefault(h => h.Tag == tag);
    }

    public IReadOnlyList<IToastHandle> FindByGroup(string group)
    {
        lock (_active) return _active.Where(h => h.Group == group).ToArray<IToastHandle>();
    }

    public IScheduledToastHandle Schedule(ToastContent toast, DateTimeOffset deliveryTime)
    {
        if (string.IsNullOrEmpty(toast.Tag))
            toast.Tag = $"scheduled-{Guid.NewGuid():N}";

        if (_notifier == null)
            throw new InvalidOperationException("ToastService is not initialized.");

        using var xmlToast = _xmlToastFactory.Create(toast, _interop);
        var scheduled = _notifier.CreateScheduledToast(xmlToast.XmlDocument, deliveryTime);

        // Apply tag/group if supported
        if (scheduled is IScheduledToastNotification2 s2)
        {
            if (!string.IsNullOrEmpty(toast.Tag)) s2.put_Tag(toast.Tag!);
            if (!string.IsNullOrEmpty(toast.Group)) s2.put_Group(toast.Group!);
            if (toast.SuppressPopup) s2.put_SuppressPopup(1);
        }

        _notifier.AddToSchedule(scheduled);

        var handle = new ScheduledToastHandle(this, _notifier, scheduled, toast.Tag, toast.Group, deliveryTime, toast.SuppressPopup);
        lock (_scheduled) _scheduled.Add(handle);
        return handle;
    }

    public IScheduledToastHandle Schedule(Action<ToastBuilder> configure, DateTimeOffset deliveryTime)
    {
        var builder = new ToastBuilder();
        configure(builder);
        return Schedule(builder.Build(), deliveryTime);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _notifier?.Dispose();
        _notifier = null;
        lock (_active) _active.Clear();
    }

    // --- Internal (called by ToastHandle) ---

    internal void OnHandleDisposed(ToastHandle handle)
    {
        lock (_active) _active.Remove(handle);
    }

    internal void OnScheduledHandleDisposed(ScheduledToastHandle handle)
    {
        lock (_scheduled) _scheduled.Remove(handle);
    }

}
