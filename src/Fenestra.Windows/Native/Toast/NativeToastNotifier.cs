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
    private readonly IWinRtInterop _interop;
    private readonly ComRef<IToastNotifier> _notifier;
    private readonly ComRef<IToastNotificationHistory>? _history;
    private bool _disposed;

    /// <summary>Direct access to the toast notification history interface. Null if unsupported.</summary>
    public IToastNotificationHistory? History => _history?.Value;

    public NativeToastNotifier(string appId, IWinRtInterop interop)
    {
        _interop = interop;
        using var manager = interop.GetActivationFactory<IToastNotificationManagerStatics>(
            "Windows.UI.Notifications.ToastNotificationManager", IID_IToastNotificationManagerStatics)
            ?? throw new InvalidOperationException("Failed to get ToastNotificationManager activation factory.");

        var hr = manager.Value.CreateToastNotifier(out var pNotifier);
        if (hr != 0 || pNotifier == IntPtr.Zero)
        {
            hr = manager.Value.CreateToastNotifierWithId(appId, out pNotifier);
            if (hr != 0 || pNotifier == IntPtr.Zero)
                throw new InvalidOperationException($"CreateToastNotifier failed for appId '{appId}'.");
        }

        _notifier = _interop.CastPointer<IToastNotifier>(pNotifier)
            ?? throw new InvalidOperationException("Failed to wrap IToastNotifier.");

        // Obtain toast history (optional — may not be supported on all OS versions)
        using var manager2 = _interop.GetActivationFactory<IToastNotificationManagerStatics2>(
            "Windows.UI.Notifications.ToastNotificationManager", IID_IToastNotificationManagerStatics2);
        if (manager2 != null && manager2.Value.get_History(out var pHistory) == 0 && pHistory != IntPtr.Zero)
            _history = _interop.CastPointer<IToastNotificationHistory>(pHistory);
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

    public void SetExpirationTime(IToastNotification notification, DateTimeOffset expirationTime)
    {
        var pBoxed = BoxDateTime(expirationTime);
        if (pBoxed == IntPtr.Zero) return;
        try { notification.put_ExpirationTime(pBoxed); }
        finally { Marshal.Release(pBoxed); }
    }

    // --- Scheduling ---

    public IScheduledToastNotification CreateScheduledToast(object xmlDoc, DateTimeOffset deliveryTime)
    {
        using var factory = _interop.GetActivationFactory<IScheduledToastNotificationFactory>(
            "Windows.UI.Notifications.ScheduledToastNotification", IID_IScheduledToastNotificationFactory)
            ?? throw new InvalidOperationException("Failed to get ScheduledToastNotification factory.");

        var hr = factory.Value.CreateScheduledToastNotification(xmlDoc, deliveryTime.UtcDateTime.ToFileTimeUtc(), out var pResult);
        if (hr < 0) throw new COMException($"CreateScheduledToastNotification failed. HRESULT=0x{hr:X8}", hr);

        return _interop.CastPointer<IScheduledToastNotification>(pResult)?.Value
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

    private ComRef<INotificationData>? CreateNotificationData(Dictionary<string, string> data, uint sequenceNumber)
    {
        var notifData = _interop.ActivateInstance<INotificationData>("Windows.UI.Notifications.NotificationData");
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

    private IntPtr BoxDateTime(DateTimeOffset value)
    {
        using var factory = _interop.GetActivationFactory<IPropertyValueStatics>(
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
