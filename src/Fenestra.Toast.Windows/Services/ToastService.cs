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
    private readonly List<ToastHandle> _active = new();
    private object? _notifier;
    private bool _disposed;

    public IReadOnlyList<IToastHandle> Active
    {
        get
        {
            lock (_active) return _active.ToArray();
        }
    }

    public ToastService(AppInfo appInfo, IThreadContext threadContext)
    {
        _appId = appInfo.AppId;
        _supported = IsSupported();
        _threadContext = threadContext;

        if (_supported)
        {
            WinRtToastInterop.SetCurrentProcessExplicitAppUserModelID(_appId);
            _notifier = WinRtToastInterop.CreateNotifier(_appId);
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

        try
        {
            ShowInternal(toast, handle);
        }
        catch { }

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
        if (!_supported) return;
        try
        {
            var history = WinRtToastInterop.GetHistory();
            if (history != null)
                WinRtToastInterop.HistoryClearWithId(history, _appId);
        }
        catch { }

        lock (_active) _active.Clear();
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _notifier = null;
        lock (_active) _active.Clear();
    }

    // --- Internal methods called by ToastHandle ---

    internal void UpdateInternal(string tag, Dictionary<string, string> data, uint sequenceNumber, string? group)
    {
        if (!_supported || _notifier == null) return;
        try
        {
            var notificationData = WinRtToastInterop.CreateNotificationData(sequenceNumber);
            if (notificationData == null) return;

            foreach (var kv in data)
                WinRtToastInterop.SetNotificationDataValue(notificationData, kv.Key, kv.Value);

            WinRtToastInterop.UpdateNotification(_notifier, notificationData, tag, group);
        }
        catch { }
    }

    internal void ReplaceInternal(ToastContent toast, ToastHandle handle)
    {
        if (!_supported || _notifier == null) return;
        try
        {
            ShowInternal(toast, handle);
        }
        catch { }
    }

    internal void RemoveInternal(string tag, string? group)
    {
        if (!_supported) return;
        try
        {
            var history = WinRtToastInterop.GetHistory();
            if (history == null) return;

            if (group != null)
                WinRtToastInterop.HistoryRemoveGrouped(history, tag, group);
            else
                WinRtToastInterop.HistoryRemove(history, tag);
        }
        catch { }
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
        var xmlDoc = WinRtToastInterop.CreateXmlDocument(xml);
        if (xmlDoc == null) return;

        var notification = WinRtToastInterop.CreateNotification(xmlDoc);
        if (notification == null) return;

        if (!string.IsNullOrEmpty(toast.Tag))
            WinRtToastInterop.SetTag(notification, toast.Tag!);
        if (!string.IsNullOrEmpty(toast.Group))
            WinRtToastInterop.SetGroup(notification, toast.Group!);
        if (toast.SuppressPopup)
            WinRtToastInterop.SetSuppressPopup(notification, true);
        if (toast.Priority != ToastPriority.Default)
            WinRtToastInterop.SetPriority(notification, (int)toast.Priority);
        if (toast.ExpiresOnReboot)
            WinRtToastInterop.SetExpiresOnReboot(notification, true);
        if (toast.ExpirationTime.HasValue)
            WinRtToastInterop.SetExpirationTime(notification, toast.ExpirationTime.Value);

        WinRtToastInterop.SubscribeActivated(notification, (args, input) =>
            Post(() => handle.RaiseActivated(new ToastActivatedArgs(args, input))));

        WinRtToastInterop.SubscribeDismissed(notification, reason =>
            Post(() => handle.RaiseDismissed((ToastDismissalReason)reason)));

        WinRtToastInterop.SubscribeFailed(notification, errorCode =>
            Post(() => handle.RaiseFailed(errorCode)));

        WinRtToastInterop.ShowNotification(_notifier!, notification);

        // Bind progress tracker after show
        if (toast.ProgressTracker != null)
        {
            var tag = toast.Tag!;
            var group = toast.Group;
            toast.ProgressTracker.Bind(data => UpdateInternal(tag, data, 0, group));
        }
    }

    private void Post(Action action) => _threadContext.InvokeAsync(action);

    private static bool IsSupported()
    {
#if NET6_0_OR_GREATER
        return RuntimeInformation.IsOSPlatform(OSPlatform.Windows) && Environment.OSVersion.Version.Major >= 10;
#else
        return Environment.OSVersion.Version.Major >= 10;
#endif
    }
}
