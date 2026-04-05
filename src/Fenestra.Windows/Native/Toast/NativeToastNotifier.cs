using System.Runtime.InteropServices;
using static Fenestra.Windows.Native.Toast.ToastInteropConstants;

namespace Fenestra.Windows.Native.Toast;

/// <summary>
/// Facade for the Windows toast notification system.
/// Delegates to <see cref="NativeToastDisplay"/> (show/hide/create),
/// <see cref="NativeToastUpdater"/> (data binding updates), and
/// <see cref="NativeToastHistory"/> (action center management).
/// </summary>
internal sealed class NativeToastNotifier : IDisposable
{
    private readonly object _notifierRcw;
    private readonly IToastNotifier _notifier;
    private readonly NativeToastDisplay _display;
    private readonly NativeToastUpdater? _updater;
    private bool _disposed;

    public bool IsValid => true;

    public NativeToastNotifier(string appId)
    {
        var manager = WinRtToastInterop.GetActivationFactoryAs<IToastNotificationManagerStatics>(
            "Windows.UI.Notifications.ToastNotificationManager", IID_IToastNotificationManagerStatics)
            ?? throw new InvalidOperationException("Failed to get ToastNotificationManager activation factory.");

        try
        {
            IntPtr pNotifier;

            // Try parameterless CreateToastNotifier first, fall back to CreateToastNotifierWithId
            var hr = manager.CreateToastNotifier(out pNotifier);
            if (hr != 0 || pNotifier == IntPtr.Zero)
            {
                using var hAppId = HStringHandle.Create(appId);
                hr = manager.CreateToastNotifierWithId(hAppId.DangerousGetHandle(), out pNotifier);
                if (hr != 0 || pNotifier == IntPtr.Zero)
                    throw new InvalidOperationException($"CreateToastNotifier failed for appId '{appId}'.");
            }

            _notifierRcw = WinRtToastInterop.CastComPointer<IToastNotifier>(pNotifier)
                ?? throw new InvalidOperationException("Failed to wrap IToastNotifier.");
            _notifier = (IToastNotifier)_notifierRcw;
        }
        finally { Marshal.ReleaseComObject(manager); }

        _display = new NativeToastDisplay(_notifier);
        _updater = NativeToastUpdater.TryCreate(_notifier);
    }

    public void Show(ComPointerHandle pNotification) => _display.Show(pNotification);
    public void Hide(ComPointerHandle pNotification) => _display.Hide(pNotification);

    // --- Properties (set on notification before Show) ---

    public void SetTag(ComPointerHandle pNotif, string tag)
    {
        var notif2 = WinRtToastInterop.BorrowComPointer<IToastNotification2>(pNotif.DangerousGetHandle());
        if (notif2 == null) return;
        try
        {
            using var h = HStringHandle.Create(tag);
            notif2.put_Tag(h.DangerousGetHandle());
        }
        finally { Marshal.ReleaseComObject(notif2); }
    }

    public void SetGroup(ComPointerHandle pNotif, string group)
    {
        var notif2 = WinRtToastInterop.BorrowComPointer<IToastNotification2>(pNotif.DangerousGetHandle());
        if (notif2 == null) return;
        try
        {
            using var h = HStringHandle.Create(group);
            notif2.put_Group(h.DangerousGetHandle());
        }
        finally { Marshal.ReleaseComObject(notif2); }
    }

    public void SetSuppressPopup(ComPointerHandle pNotif, bool value)
    {
        var notif2 = WinRtToastInterop.BorrowComPointer<IToastNotification2>(pNotif.DangerousGetHandle());
        if (notif2 == null) return;
        try { notif2.put_SuppressPopup(value ? 1 : 0); }
        finally { Marshal.ReleaseComObject(notif2); }
    }

    public void SetPriority(ComPointerHandle pNotif, int priority)
    {
        var notif4 = WinRtToastInterop.BorrowComPointer<IToastNotification4>(pNotif.DangerousGetHandle());
        if (notif4 == null) return;
        try { notif4.put_Priority(priority); }
        finally { Marshal.ReleaseComObject(notif4); }
    }

    public void SetExpiresOnReboot(ComPointerHandle pNotif, bool value)
    {
        var notif6 = WinRtToastInterop.BorrowComPointer<IToastNotification6>(pNotif.DangerousGetHandle());
        if (notif6 == null) return;
        try { notif6.put_ExpiresOnReboot(value ? 1 : 0); }
        finally { Marshal.ReleaseComObject(notif6); }
    }

    public void SetExpirationTime(ComPointerHandle pNotif, DateTimeOffset expirationTime)
    {
        var notif = WinRtToastInterop.BorrowComPointer<IToastNotification>(pNotif.DangerousGetHandle());
        if (notif == null) return;

        try
        {
            using var pBoxed = BoxDateTime(expirationTime);
            if (pBoxed == null) return;

            notif.put_ExpirationTime(pBoxed.DangerousGetHandle());
        }
        finally { Marshal.ReleaseComObject(notif); }
    }

    // --- Events ---

    public long AddActivatedHandler(ComPointerHandle pNotif, IntPtr pHandler)
    {
        var notif = WinRtToastInterop.BorrowComPointer<IToastNotification>(pNotif.DangerousGetHandle());
        if (notif == null) return 0;
        try
        {
            notif.add_Activated(pHandler, out var token);
            return token;
        }
        finally { Marshal.ReleaseComObject(notif); }
    }

    public long AddDismissedHandler(ComPointerHandle pNotif, IntPtr pHandler)
    {
        var notif = WinRtToastInterop.BorrowComPointer<IToastNotification>(pNotif.DangerousGetHandle());
        if (notif == null) return 0;
        try
        {
            notif.add_Dismissed(pHandler, out var token);
            return token;
        }
        finally { Marshal.ReleaseComObject(notif); }
    }

    public long AddFailedHandler(ComPointerHandle pNotif, IntPtr pHandler)
    {
        var notif = WinRtToastInterop.BorrowComPointer<IToastNotification>(pNotif.DangerousGetHandle());
        if (notif == null) return 0;
        try
        {
            notif.add_Failed(pHandler, out var token);
            return token;
        }
        finally { Marshal.ReleaseComObject(notif); }
    }

    // --- Update ---

    public int Update(string tag, string? group, Dictionary<string, string> data, uint sequenceNumber)
        => _updater?.Update(tag, group, data, sequenceNumber) ?? NotificationUpdateResult_Failed;

    // --- History ---

    public void HistoryRemove(string tag) => NativeToastHistory.Remove(tag);
    public void HistoryRemoveGrouped(string tag, string group) => NativeToastHistory.RemoveGrouped(tag, group);
    public void HistoryRemoveGroup(string group) => NativeToastHistory.RemoveGroup(group);
    public void HistoryClear() => NativeToastHistory.Clear();
    public void HistoryClearWithId(string appId) => NativeToastHistory.ClearWithId(appId);

    // --- Private ---

    private static ComPointerHandle? BoxDateTime(DateTimeOffset value)
    {
        var factory = WinRtToastInterop.GetActivationFactoryAs<IPropertyValueStatics>(
            "Windows.Foundation.PropertyValue", IID_IPropertyValueStatics);
        if (factory == null) return null;

        try
        {
            long winrtDateTime = value.UtcDateTime.ToFileTimeUtc();
            var hr = factory.CreateDateTime(winrtDateTime, out var result);
            return hr == 0 && result != IntPtr.Zero ? new ComPointerHandle(result) : null;
        }
        finally { Marshal.ReleaseComObject(factory); }
    }

    // --- Dispose ---

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        Marshal.ReleaseComObject(_notifierRcw);
    }
}
