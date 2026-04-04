using System.Runtime.InteropServices;
using static Fenestra.Toast.Windows.Native.ToastInteropConstants;

namespace Fenestra.Toast.Windows.Native;

/// <summary>
/// Manages the lifecycle of a Windows toast notifier and provides high-level operations.
/// All COM pointers are wrapped in <see cref="ComPointerHandle"/> for automatic release.
/// </summary>
internal class NativeToastNotifier : IDisposable
{
    private ComPointerHandle? _pNotifier;
    private ComPointerHandle? _pToastNotifier;
    private ComPointerHandle? _pNotifier2;
    private bool _disposed;

    public bool IsValid => _pNotifier != null && !_pNotifier.IsInvalid;

    public NativeToastNotifier(string appId)
    {
        using var pManager = WinRtToastInterop.GetActivationFactory(
            "Windows.UI.Notifications.ToastNotificationManager", IID_IToastNotificationManagerStatics);
        if (pManager == null)
            throw new InvalidOperationException("Failed to get ToastNotificationManager activation factory.");

        _pNotifier = WinRtToastInterop.CallGetPtr(pManager.DangerousGetHandle(), Slot_Manager_CreateToastNotifier);

        if (_pNotifier == null)
            _pNotifier = WinRtToastInterop.CallWithHString(pManager, Slot_Manager_CreateToastNotifierWithId, appId);

        if (_pNotifier == null)
            throw new InvalidOperationException($"CreateToastNotifier failed for appId '{appId}'.");

        _pToastNotifier = _pNotifier.QueryInterface(IID_IToastNotifier);
        _pNotifier2 = _pNotifier.QueryInterface(IID_IToastNotifier2);
    }

    // --- Show / Hide ---

    public void Show(ComPointerHandle pNotification)
    {
        if (_pToastNotifier == null) throw new InvalidOperationException("IToastNotifier not available.");

        using var pNotif = pNotification.QueryInterface(IID_IToastNotification);
        if (pNotif == null)
            throw new InvalidOperationException("QI for IToastNotification failed.");

        var hr = WinRtToastInterop.CallWithPtr(_pToastNotifier.DangerousGetHandle(), Slot_Notifier_Show, pNotif.DangerousGetHandle());
        if (hr < 0) throw new COMException($"IToastNotifier.Show failed. HRESULT=0x{hr:X8}", hr);
    }

    public void Hide(ComPointerHandle pNotification)
    {
        if (_pToastNotifier == null) throw new InvalidOperationException("IToastNotifier not available.");

        using var pNotif = pNotification.QueryInterface(IID_IToastNotification);
        if (pNotif == null)
            throw new InvalidOperationException("QI for IToastNotification failed.");

        var hr = WinRtToastInterop.CallWithPtr(_pToastNotifier.DangerousGetHandle(), Slot_Notifier_Hide, pNotif.DangerousGetHandle());
        if (hr < 0) throw new COMException($"IToastNotifier.Hide failed. HRESULT=0x{hr:X8}", hr);
    }

    // --- Create ---

    public ComPointerHandle CreateXmlDocument(string xml)
    {
        var pXmlDoc = WinRtToastInterop.ActivateInstance("Windows.Data.Xml.Dom.XmlDocument")
            ?? throw new InvalidOperationException("RoActivateInstance failed for XmlDocument.");

        using var pIO = pXmlDoc.QueryInterface(IID_IXmlDocumentIO)
            ?? throw new InvalidOperationException("QI for IXmlDocumentIO failed.");

        var hr = WinRtToastInterop.CallSetHString(pIO.DangerousGetHandle(), Slot_XmlDocIO_LoadXml, xml);
        if (hr < 0) throw new COMException($"LoadXml failed. HRESULT=0x{hr:X8}", hr);

        return pXmlDoc;
    }

    public ComPointerHandle CreateNotification(ComPointerHandle pXmlDoc)
    {
        using var pFactory = WinRtToastInterop.GetActivationFactory(
            "Windows.UI.Notifications.ToastNotification", IID_IToastNotificationFactory)
            ?? throw new InvalidOperationException("Failed to get ToastNotification factory.");

        var fn = ComFactory.GetDelegate<CreateNotifDelegate>(
            WinRtToastInterop.GetVtableSlot(pFactory.DangerousGetHandle(), Slot_Factory_CreateToastNotification));
        var hr = fn(pFactory.DangerousGetHandle(), pXmlDoc.DangerousGetHandle(), out var pNotif);
        if (hr < 0) throw new COMException($"CreateToastNotification failed. HRESULT=0x{hr:X8}", hr);
        if (pNotif == IntPtr.Zero) throw new InvalidOperationException("CreateToastNotification returned null.");

        return ComPointerHandle.Wrap(pNotif);
    }

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    private delegate int CreateNotifDelegate(IntPtr @this, IntPtr content, out IntPtr notification);

    // --- Properties ---

    public void SetTag(ComPointerHandle pNotif, string tag)
    {
        using var p = pNotif.QueryInterface(IID_IToastNotification2);
        if (p == null) return;
        WinRtToastInterop.CallSetHString(p.DangerousGetHandle(), Slot_Notification2_put_Tag, tag);
    }

    public void SetGroup(ComPointerHandle pNotif, string group)
    {
        using var p = pNotif.QueryInterface(IID_IToastNotification2);
        if (p == null) return;
        WinRtToastInterop.CallSetHString(p.DangerousGetHandle(), Slot_Notification2_put_Group, group);
    }

    public void SetSuppressPopup(ComPointerHandle pNotif, bool value)
    {
        using var p = pNotif.QueryInterface(IID_IToastNotification2);
        if (p == null) return;
        WinRtToastInterop.CallSetBool(p.DangerousGetHandle(), Slot_Notification2_put_SuppressPopup, value);
    }

    public void SetPriority(ComPointerHandle pNotif, int priority)
    {
        using var p = pNotif.QueryInterface(IID_IToastNotification4);
        if (p == null) return;
        WinRtToastInterop.CallSetInt(p.DangerousGetHandle(), Slot_Notification4_put_Priority, priority);
    }

    public void SetExpiresOnReboot(ComPointerHandle pNotif, bool value)
    {
        using var p = pNotif.QueryInterface(IID_IToastNotification6);
        if (p == null) return;
        WinRtToastInterop.CallSetBool(p.DangerousGetHandle(), Slot_Notification6_put_ExpiresOnReboot, value);
    }

    // --- Events ---

    public long AddActivatedHandler(IntPtr pNotif, IntPtr pHandler)
        => WinRtToastInterop.CallAddEvent(pNotif, Slot_Notification_add_Activated, pHandler);

    public long AddDismissedHandler(IntPtr pNotif, IntPtr pHandler)
        => WinRtToastInterop.CallAddEvent(pNotif, Slot_Notification_add_Dismissed, pHandler);

    public long AddFailedHandler(IntPtr pNotif, IntPtr pHandler)
        => WinRtToastInterop.CallAddEvent(pNotif, Slot_Notification_add_Failed, pHandler);

    // --- Update ---

    public int Update(string tag, string? group, Dictionary<string, string> data, uint sequenceNumber)
    {
        if (_pNotifier2 == null) return NotificationUpdateResult_Failed;

        using var pData = CreateNotificationData(data, sequenceNumber);
        if (pData == null) return NotificationUpdateResult_Failed;

        if (group != null)
            return WinRtToastInterop.CallUpdateTagGroup(_pNotifier2.DangerousGetHandle(), Slot_Notifier2_UpdateWithTagAndGroup, pData.DangerousGetHandle(), tag, group);
        return WinRtToastInterop.CallUpdateTag(_pNotifier2.DangerousGetHandle(), Slot_Notifier2_UpdateWithTag, pData.DangerousGetHandle(), tag);
    }

    private ComPointerHandle? CreateNotificationData(Dictionary<string, string> data, uint sequenceNumber)
    {
        var pData = WinRtToastInterop.ActivateInstance("Windows.UI.Notifications.NotificationData");
        if (pData == null) return null;

        WinRtToastInterop.CallSetUInt(pData.DangerousGetHandle(), Slot_NotifData_put_SequenceNumber, sequenceNumber);

        using var pMap = WinRtToastInterop.CallGetPtr(pData.DangerousGetHandle(), Slot_NotifData_get_Values);
        if (pMap != null)
        {
            foreach (var kv in data)
            {
                using var hKey = HStringHandle.Create(kv.Key);
                using var hVal = HStringHandle.Create(kv.Value);
                var fn = ComFactory.GetDelegate<MapInsertDelegate>(
                    WinRtToastInterop.GetVtableSlot(pMap.DangerousGetHandle(), Slot_Map_Insert));
                fn(pMap.DangerousGetHandle(), hKey.DangerousGetHandle(), hVal.DangerousGetHandle(), out _);
            }
        }

        return pData;
    }

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    private delegate int MapInsertDelegate(IntPtr @this, IntPtr key, IntPtr value, out int replaced);

    // --- History ---

    public void HistoryRemove(string tag)
    {
        using var pHistory = GetHistory();
        if (pHistory == null) return;
        WinRtToastInterop.CallSetHString(pHistory.DangerousGetHandle(), Slot_History_Remove, tag);
    }

    public void HistoryRemoveGrouped(string tag, string group)
    {
        using var pHistory = GetHistory();
        if (pHistory == null) return;
        WinRtToastInterop.CallHStringHString(pHistory.DangerousGetHandle(), Slot_History_RemoveGroupedTag, tag, group);
    }

    public void HistoryRemoveGroup(string group)
    {
        using var pHistory = GetHistory();
        if (pHistory == null) return;
        WinRtToastInterop.CallSetHString(pHistory.DangerousGetHandle(), Slot_History_RemoveGroup, group);
    }

    public void HistoryClear()
    {
        using var pHistory = GetHistory();
        if (pHistory == null) return;
        WinRtToastInterop.CallVoid(pHistory.DangerousGetHandle(), Slot_History_Clear);
    }

    public void HistoryClearWithId(string appId)
    {
        using var pHistory = GetHistory();
        if (pHistory == null) return;
        WinRtToastInterop.CallSetHString(pHistory.DangerousGetHandle(), Slot_History_ClearWithId, appId);
    }

    private ComPointerHandle? GetHistory()
    {
        using var pManager2 = WinRtToastInterop.GetActivationFactory(
            "Windows.UI.Notifications.ToastNotificationManager", IID_IToastNotificationManagerStatics2);
        if (pManager2 == null) return null;
        return WinRtToastInterop.CallGetPtr(pManager2.DangerousGetHandle(), Slot_Manager2_get_History);
    }

    // --- Dispose ---

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _pToastNotifier?.Dispose();
        _pNotifier2?.Dispose();
        _pNotifier?.Dispose();
    }
}
