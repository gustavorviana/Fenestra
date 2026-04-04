using System.Runtime.InteropServices;
using Fenestra.Core;
using Fenestra.Core.Models;
using Fenestra.Toast.Windows.Native;

namespace Fenestra.Toast.Windows.Services;

/// <summary>
/// Displays Windows 10+ toast notifications. Falls back silently on unsupported OS versions.
/// </summary>
internal class ToastService : IToastService, IDisposable
{
    private readonly string _appId;
    private readonly bool _supported;
    private readonly IThreadContext _threadContext;
    private readonly IWindowsNotificationRegistrationManager? _registrationManager;
    private readonly List<ToastHandle> _active = new();
    private NativeToastNotifier? _notifier;
    private bool _disposed;

    public IReadOnlyList<IToastHandle> Active
    {
        get { lock (_active) return _active.ToArray(); }
    }

    public ToastService(AppInfo appInfo, IThreadContext threadContext, IWindowsNotificationRegistrationManager? registrationManager = null)
    {
        _appId = appInfo.AppId;
        _supported = IsSupported();
        _threadContext = threadContext;
        _registrationManager = registrationManager;

        if (_supported)
        {
            _registrationManager?.EnsureRegistered();
            WinRtToastInterop.SetCurrentProcessExplicitAppUserModelID(_appId);
            _notifier = new NativeToastNotifier(_appId);
            if (!_notifier.IsValid) { _notifier.Dispose(); _notifier = null; }
        }
    }

    public IToastHandle Show(ToastContent toast)
    {
        if (string.IsNullOrEmpty(toast.Tag))
            toast.Tag = $"toast-{Guid.NewGuid():N}";

        var handle = new ToastHandle(toast.Tag!, toast.Group, this);
        lock (_active) _active.Add(handle);

        if (!_supported || _notifier == null)
            return handle;

        ShowInternal(toast, handle);

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
        if (!_supported || _notifier == null) return;
        try { _notifier.HistoryClearWithId(_appId); }
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

    internal void UpdateInternal(string tag, Dictionary<string, string> data, uint sequenceNumber, string? group)
    {
        if (_notifier == null) return;
        try { _notifier.Update(tag, group, data, sequenceNumber); }
        catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"[Fenestra.Toast] {ex.Message}"); }
    }

    internal void ReplaceInternal(ToastContent toast, ToastHandle handle)
    {
        if (_notifier == null) return;
        try { ShowInternal(toast, handle); }
        catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"[Fenestra.Toast] {ex.Message}"); }
    }

    internal void RemoveInternal(string tag, string? group)
    {
        if (_notifier == null) return;
        try
        {
            if (group != null)
                _notifier.HistoryRemoveGrouped(tag, group);
            else
                _notifier.HistoryRemove(tag);
        }
        catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"[Fenestra.Toast] {ex.Message}"); }
    }

    internal void OnHandleDisposed(ToastHandle handle)
    {
        lock (_active) _active.Remove(handle);
    }

    // --- Private ---

    private void ShowInternal(ToastContent toast, ToastHandle handle)
    {
        var useBindings = toast.ProgressTracker != null;
        var xml = ToastXmlBuilder.Build(toast, useBindings);

        var pXmlDoc = _notifier!.CreateXmlDocument(xml);
        if (pXmlDoc == IntPtr.Zero) return;

        IntPtr pNotif;
        try
        {
            pNotif = _notifier.CreateNotification(pXmlDoc);
            if (pNotif == IntPtr.Zero) return;
        }
        catch
        {
            Marshal.Release(pXmlDoc);
            throw;
        }

        try
        {
            if (!string.IsNullOrEmpty(toast.Tag))
                _notifier.SetTag(pNotif, toast.Tag!);
            if (!string.IsNullOrEmpty(toast.Group))
                _notifier.SetGroup(pNotif, toast.Group!);
            if (toast.SuppressPopup)
                _notifier.SetSuppressPopup(pNotif, true);
            if (toast.Priority != ToastPriority.Default)
                _notifier.SetPriority(pNotif, (int)toast.Priority);
            if (toast.ExpiresOnReboot)
                _notifier.SetExpiresOnReboot(pNotif, true);

            _notifier.Show(pNotif);

            if (toast.ProgressTracker != null)
            {
                var tag = toast.Tag!;
                var group = toast.Group;
                var tracker = toast.ProgressTracker;
                tracker.Bind(data => UpdateInternal(tag, data, 0, group));

                // Send initial data so bindings have values immediately
                var initial = new Dictionary<string, string>
                {
                    ["progressStatus"] = " ",
                    ["progressValue"] = toast.ProgressTracker.Value.ToString()
                };
                if (tracker.Title != null)
                    initial["progressTitle"] = tracker.Title;
                if (tracker.UseValueOverride)
                    initial["progressValueOverride"] = "0%";
                UpdateInternal(tag, initial, 0, group);
            }
        }
        finally
        {
            Marshal.Release(pNotif);
            Marshal.Release(pXmlDoc);
        }
    }

    private static bool IsSupported()
    {
#if NET6_0_OR_GREATER
        return RuntimeInformation.IsOSPlatform(OSPlatform.Windows) && Environment.OSVersion.Version.Major >= 10;
#else
        return Environment.OSVersion.Version.Major >= 10;
#endif
    }
}
