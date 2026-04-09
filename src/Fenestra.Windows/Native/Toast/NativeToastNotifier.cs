using System.Runtime.InteropServices;
using Fenestra.Windows.Models;
using static Fenestra.Windows.Native.Toast.ToastInteropConstants;

namespace Fenestra.Windows.Native.Toast;

/// <summary>
/// Facade for the Windows toast notification system.
/// Handles show/hide, property get/set, event subscription, and data updates
/// directly through WinRT COM interfaces.
/// </summary>
internal sealed class NativeToastNotifier : INativeToastNotifier
{
    private readonly IWinRtInterop _interop;
    private readonly IComRef<IToastNotifier> _notifier;
    private readonly IComRef<IToastNotificationHistory>? _history;
    // Lazy-cached factories — WinRT activation factories are singletons, safe to reuse
    private IComRef<IPropertyValueStatics>? _propertyValueFactory;
    private IComRef<IScheduledToastNotificationFactory>? _scheduledFactory;
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

    // ExpirationTime requires boxing DateTimeOffset into IReference<DateTime> via IPropertyValueStatics.
    // The boxed value is an IntPtr (IInspectable) because IReference<T> has no fixed GUID for [ComImport].
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
        _scheduledFactory ??= _interop.GetActivationFactory<IScheduledToastNotificationFactory>(
            "Windows.UI.Notifications.ScheduledToastNotification", IID_IScheduledToastNotificationFactory)
            ?? throw new InvalidOperationException("Failed to get ScheduledToastNotification factory.");

        var hr = _scheduledFactory.Value.CreateScheduledToastNotification(xmlDoc, deliveryTime.UtcDateTime.ToFileTimeUtc(), out var pResult);
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

    private IComRef<INotificationData>? CreateNotificationData(Dictionary<string, string> data, uint sequenceNumber)
    {
        var notifData = _interop.ActivateInstance<INotificationData>("Windows.UI.Notifications.NotificationData");
        if (notifData == null) return null;

        notifData.Value.put_SequenceNumber(sequenceNumber);

        // get_Values returns a separate COM identity (IMap) that must be released independently
        if (notifData.Value.get_Values(out var rawMap) == 0 && rawMap != null)
        {
            using var map = new ComRef<IMapStringString>(rawMap);
            foreach (var kv in data)
                map.Value.Insert(kv.Key, kv.Value, out _);
        }

        return notifData;
    }

    // Returns raw IntPtr (IReference<DateTime>) — caller must Marshal.Release.
    // Cannot return ComRef because IReference<T> is a parameterized WinRT type with no fixed GUID.
    private IntPtr BoxDateTime(DateTimeOffset value)
    {
        _propertyValueFactory ??= _interop.GetActivationFactory<IPropertyValueStatics>(
            "Windows.Foundation.PropertyValue", IID_IPropertyValueStatics);
        if (_propertyValueFactory == null) return IntPtr.Zero;

        var hr = _propertyValueFactory.Value.CreateDateTime(value.UtcDateTime.ToFileTimeUtc(), out var result);
        return hr == 0 ? result : IntPtr.Zero;
    }

    // --- Dispose ---
    // Release order: factories first (they depend on nothing), then history, then notifier (root)

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _scheduledFactory?.Dispose();
        _propertyValueFactory?.Dispose();
        _history?.Dispose();
        _notifier.Dispose();
    }
}
