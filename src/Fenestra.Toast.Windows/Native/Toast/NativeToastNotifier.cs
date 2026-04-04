using static Fenestra.Toast.Windows.Native.Toast.ToastInteropConstants;

namespace Fenestra.Toast.Windows.Native.Toast;

/// <summary>
/// Facade for the Windows toast notification system.
/// Delegates to <see cref="NativeToastDisplay"/> (show/hide/create),
/// <see cref="NativeToastUpdater"/> (data binding updates), and
/// <see cref="NativeToastHistory"/> (action center management).
/// </summary>
internal sealed class NativeToastNotifier : IDisposable
{
    private readonly ComPointerHandle _pNotifier;
    private readonly NativeToastDisplay _display;
    private readonly NativeToastUpdater? _updater;
    private bool _disposed;

    public bool IsValid => !_pNotifier.IsInvalid;

    public NativeToastNotifier(string appId)
    {
        using var pManager = WinRtToastInterop.GetActivationFactory(
            "Windows.UI.Notifications.ToastNotificationManager", IID_IToastNotificationManagerStatics)
            ?? throw new InvalidOperationException("Failed to get ToastNotificationManager activation factory.");

        _pNotifier = WinRtToastInterop.CallGetPtr(pManager, Slot_Manager_CreateToastNotifier)
            ?? WinRtToastInterop.CallWithHString(pManager, Slot_Manager_CreateToastNotifierWithId, appId)
            ?? throw new InvalidOperationException($"CreateToastNotifier failed for appId '{appId}'.");

        _display = new NativeToastDisplay(_pNotifier);
        _updater = NativeToastUpdater.TryCreate(_pNotifier);
    }

    public void Show(ComPointerHandle pNotification) => _display.Show(pNotification);
    public void Hide(ComPointerHandle pNotification) => _display.Hide(pNotification);

    // --- Properties (set on notification before Show) ---

    public void SetTag(ComPointerHandle pNotif, string tag)
    {
        using var p = pNotif.QueryInterface(IID_IToastNotification2);
        if (p != null) WinRtToastInterop.CallSetHString(p, Slot_Notification2_put_Tag, tag);
    }

    public void SetGroup(ComPointerHandle pNotif, string group)
    {
        using var p = pNotif.QueryInterface(IID_IToastNotification2);
        if (p != null) WinRtToastInterop.CallSetHString(p, Slot_Notification2_put_Group, group);
    }

    public void SetSuppressPopup(ComPointerHandle pNotif, bool value)
    {
        using var p = pNotif.QueryInterface(IID_IToastNotification2);
        if (p != null) WinRtToastInterop.CallSetBool(p, Slot_Notification2_put_SuppressPopup, value);
    }

    public void SetPriority(ComPointerHandle pNotif, int priority)
    {
        using var p = pNotif.QueryInterface(IID_IToastNotification4);
        if (p != null) WinRtToastInterop.CallSetInt(p, Slot_Notification4_put_Priority, priority);
    }

    public void SetExpiresOnReboot(ComPointerHandle pNotif, bool value)
    {
        using var p = pNotif.QueryInterface(IID_IToastNotification6);
        if (p != null) WinRtToastInterop.CallSetBool(p, Slot_Notification6_put_ExpiresOnReboot, value);
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

    // --- Dispose ---

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _updater?.Dispose();
        _display.Dispose();
        _pNotifier.Dispose();
    }
}
