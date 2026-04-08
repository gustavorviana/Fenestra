using System.Runtime.InteropServices;
using Fenestra.Windows.Models;
using static Fenestra.Windows.Native.Toast.ToastInteropConstants;

namespace Fenestra.Windows.Native.Toast;

/// <summary>
/// Facade for the Windows toast notification system.
/// Handles show/hide, property get/set, event subscription, and data updates
/// directly through WinRT COM interfaces.
/// </summary>
internal sealed class NativeToastNotifier : IDisposable
{
    private readonly ComRef<IToastNotifier> _notifier;
    private readonly ComRef<IToastNotificationHistory>? _history;
    private bool _disposed;

    /// <summary>Direct access to the toast notification history interface. Null if unsupported.</summary>
    public IToastNotificationHistory? History => _history?.Value;

    public NativeToastNotifier(string appId)
    {
        using var manager = WinRtToastInterop.GetActivationFactory<IToastNotificationManagerStatics>(
            "Windows.UI.Notifications.ToastNotificationManager", IID_IToastNotificationManagerStatics)
            ?? throw new InvalidOperationException("Failed to get ToastNotificationManager activation factory.");

        var hr = manager.Value.CreateToastNotifier(out var pNotifier);
        if (hr != 0 || pNotifier == IntPtr.Zero)
        {
            hr = manager.Value.CreateToastNotifierWithId(appId, out pNotifier);
            if (hr != 0 || pNotifier == IntPtr.Zero)
                throw new InvalidOperationException($"CreateToastNotifier failed for appId '{appId}'.");
        }

        _notifier = WinRtToastInterop.CastPointer<IToastNotifier>(pNotifier)
            ?? throw new InvalidOperationException("Failed to wrap IToastNotifier.");

        // Obtain toast history (optional — may not be supported on all OS versions)
        using var manager2 = WinRtToastInterop.GetActivationFactory<IToastNotificationManagerStatics2>(
            "Windows.UI.Notifications.ToastNotificationManager", IID_IToastNotificationManagerStatics2);
        if (manager2 != null && manager2.Value.get_History(out var pHistory) == 0 && pHistory != IntPtr.Zero)
            _history = WinRtToastInterop.CastPointer<IToastNotificationHistory>(pHistory);
    }

    // --- Show / Hide ---

    public void Show(IToastNotification notification)
    {
        var hr = _notifier.Value.Show(notification);
        if (hr < 0) throw new COMException($"IToastNotifier.Show failed. HRESULT=0x{hr:X8}", hr);
    }

    public void Hide(IToastNotification notification)
    {
        var hr = _notifier.Value.Hide(notification);
        if (hr < 0) throw new COMException($"IToastNotifier.Hide failed. HRESULT=0x{hr:X8}", hr);
    }

    // --- Notifier queries ---

    public NotificationSetting GetSetting()
    {
        _notifier.Value.get_Setting(out var setting);
        return (NotificationSetting)setting;
    }

    // --- Notification property setters ---

    public void SetTag(IToastNotification notification, string tag)
    {
        if (notification is IToastNotification2 n) n.put_Tag(tag);
    }

    public void SetGroup(IToastNotification notification, string group)
    {
        if (notification is IToastNotification2 n) n.put_Group(group);
    }

    public void SetSuppressPopup(IToastNotification notification, bool value)
    {
        if (notification is IToastNotification2 n) n.put_SuppressPopup(value ? 1 : 0);
    }

    public void SetPriority(IToastNotification notification, ToastPriority priority)
    {
        if (notification is IToastNotification4 n) n.put_Priority((int)priority);
    }

    public void SetExpiresOnReboot(IToastNotification notification, bool value)
    {
        if (notification is IToastNotification6 n) n.put_ExpiresOnReboot(value ? 1 : 0);
    }

    public void SetExpirationTime(IToastNotification notification, DateTimeOffset expirationTime)
    {
        var pBoxed = BoxDateTime(expirationTime);
        if (pBoxed == IntPtr.Zero) return;
        try { notification.put_ExpirationTime(pBoxed); }
        finally { Marshal.Release(pBoxed); }
    }

    public void SetNotificationMirroring(IToastNotification notification, NotificationMirroring value)
    {
        if (notification is IToastNotification3 n) n.put_NotificationMirroring((int)value);
    }

    public void SetRemoteId(IToastNotification notification, string remoteId)
    {
        if (notification is IToastNotification3 n) n.put_RemoteId(remoteId);
    }

    // --- Notification property getters ---

    public string? GetTag(IToastNotification notification)
        => notification is IToastNotification2 n && n.get_Tag(out var r) == 0 ? r : null;

    public string? GetGroup(IToastNotification notification)
        => notification is IToastNotification2 n && n.get_Group(out var r) == 0 ? r : null;

    public bool GetSuppressPopup(IToastNotification notification)
        => notification is IToastNotification2 n && n.get_SuppressPopup(out var r) == 0 && r != 0;

    public ToastPriority GetPriority(IToastNotification notification)
        => notification is IToastNotification4 n && n.get_Priority(out var r) == 0 ? (ToastPriority)r : ToastPriority.Default;

    public bool GetExpiresOnReboot(IToastNotification notification)
        => notification is IToastNotification6 n && n.get_ExpiresOnReboot(out var r) == 0 && r != 0;

    public NotificationMirroring GetNotificationMirroring(IToastNotification notification)
        => notification is IToastNotification3 n && n.get_NotificationMirroring(out var r) == 0 ? (NotificationMirroring)r : NotificationMirroring.Allowed;

    public string? GetRemoteId(IToastNotification notification)
        => notification is IToastNotification3 n && n.get_RemoteId(out var r) == 0 ? r : null;

    // --- Events ---

    public long AddActivatedHandler(IToastNotification notification, IntPtr pHandler)
    {
        notification.add_Activated(pHandler, out var token);
        return token;
    }

    public long AddDismissedHandler(IToastNotification notification, IntPtr pHandler)
    {
        notification.add_Dismissed(pHandler, out var token);
        return token;
    }

    public long AddFailedHandler(IToastNotification notification, IntPtr pHandler)
    {
        notification.add_Failed(pHandler, out var token);
        return token;
    }

    // --- Scheduling ---

    public IScheduledToastNotification CreateScheduledToast(object xmlDoc, DateTimeOffset deliveryTime)
    {
        using var factory = WinRtToastInterop.GetActivationFactory<IScheduledToastNotificationFactory>(
            "Windows.UI.Notifications.ScheduledToastNotification", IID_IScheduledToastNotificationFactory)
            ?? throw new InvalidOperationException("Failed to get ScheduledToastNotification factory.");

        var hr = factory.Value.CreateScheduledToastNotification(xmlDoc, deliveryTime.UtcDateTime.ToFileTimeUtc(), out var pResult);
        if (hr < 0) throw new COMException($"CreateScheduledToastNotification failed. HRESULT=0x{hr:X8}", hr);

        return WinRtToastInterop.CastPointer<IScheduledToastNotification>(pResult)?.Value
            ?? throw new InvalidOperationException("Failed to wrap IScheduledToastNotification.");
    }

    public IScheduledToastNotification CreateScheduledToastRecurring(object xmlDoc, DateTimeOffset deliveryTime, TimeSpan snoozeInterval, uint maxSnoozeCount)
    {
        using var factory = WinRtToastInterop.GetActivationFactory<IScheduledToastNotificationFactory>(
            "Windows.UI.Notifications.ScheduledToastNotification", IID_IScheduledToastNotificationFactory)
            ?? throw new InvalidOperationException("Failed to get ScheduledToastNotification factory.");

        var hr = factory.Value.CreateScheduledToastNotificationRecurring(
            xmlDoc, deliveryTime.UtcDateTime.ToFileTimeUtc(), snoozeInterval.Ticks, maxSnoozeCount, out var pResult);
        if (hr < 0) throw new COMException($"CreateScheduledToastNotificationRecurring failed. HRESULT=0x{hr:X8}", hr);

        return WinRtToastInterop.CastPointer<IScheduledToastNotification>(pResult)?.Value
            ?? throw new InvalidOperationException("Failed to wrap IScheduledToastNotification.");
    }

    public void AddToSchedule(IScheduledToastNotification scheduled)
    {
        var hr = _notifier.Value.AddToSchedule(scheduled);
        if (hr < 0) throw new COMException($"AddToSchedule failed. HRESULT=0x{hr:X8}", hr);
    }

    public void RemoveFromSchedule(IScheduledToastNotification scheduled)
    {
        var hr = _notifier.Value.RemoveFromSchedule(scheduled);
        if (hr < 0) throw new COMException($"RemoveFromSchedule failed. HRESULT=0x{hr:X8}", hr);
    }

    // --- Update ---

    public NotificationUpdateResult Update(string tag, string? group, Dictionary<string, string> data, uint sequenceNumber)
    {
        if (_notifier.Value is not IToastNotifier2 notifier2)
            return NotificationUpdateResult.Failed;

        using var notifData = CreateNotificationData(data, sequenceNumber);
        if (notifData == null) return NotificationUpdateResult.Failed;

        int result;
        if (group != null)
            notifier2.UpdateWithTagAndGroup(notifData.Value, tag, group, out result);
        else
            notifier2.UpdateWithTag(notifData.Value, tag, out result);

        return (NotificationUpdateResult)result;
    }

    // --- Private ---

    private static ComRef<INotificationData>? CreateNotificationData(Dictionary<string, string> data, uint sequenceNumber)
    {
        var notifData = WinRtToastInterop.ActivateInstance<INotificationData>("Windows.UI.Notifications.NotificationData");
        if (notifData == null) return null;

        notifData.Value.put_SequenceNumber(sequenceNumber);

        if (notifData.Value.get_Values(out var rawMap) == 0 && rawMap != null)
        {
            using var map = new ComRef<IMapStringString>(rawMap);
            foreach (var kv in data)
                map.Value.Insert(kv.Key, kv.Value, out _);
        }

        return notifData;
    }

    private static IntPtr BoxDateTime(DateTimeOffset value)
    {
        using var factory = WinRtToastInterop.GetActivationFactory<IPropertyValueStatics>(
            "Windows.Foundation.PropertyValue", IID_IPropertyValueStatics);
        if (factory == null) return IntPtr.Zero;

        var hr = factory.Value.CreateDateTime(value.UtcDateTime.ToFileTimeUtc(), out var result);
        return hr == 0 ? result : IntPtr.Zero;
    }

    // --- Dispose ---

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _history?.Dispose();
        _notifier.Dispose();
    }
}
