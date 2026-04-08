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
    private readonly IThreadContext _threadContext;
    private readonly IApplicationActivator? _activator;
    private readonly List<ToastHandle> _active = new();
    private NativeToastNotifier? _notifier = null!;
    private bool _disposed;

    public IReadOnlyList<IToastHandle> Active
    {
        get { lock (_active) return _active.ToArray(); }
    }

    public ToastService(AppInfo appInfo, IThreadContext threadContext, IApplicationActivator? activator = null, IWindowsNotificationRegistrationManager? registrationManager = null)
    {
        Platform.EnsureWindows10();

        _appId = appInfo.AppId;
        _threadContext = threadContext;
        _activator = activator;

        registrationManager?.EnsureRegistered();
        WinRtToastInterop.SetCurrentProcessExplicitAppUserModelID(_appId);
        _notifier = new NativeToastNotifier(_appId);
    }

    public IToastHandle Show(ToastContent toast)
    {
        if (string.IsNullOrEmpty(toast.Tag))
            toast.Tag = $"toast-{Guid.NewGuid():N}";

        if (_notifier == null)
            return null!;

        using var pXmlDoc = new XmlToast(toast);
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
        using (var history = new NativeToastHistory())
            try { history.ClearWithId(_appId); }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"[Fenestra.Toast] {ex.Message}"); }
        lock (_active) _active.Clear();
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

}
