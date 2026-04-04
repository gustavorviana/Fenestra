using System.Runtime.InteropServices;
using static Fenestra.Toast.Windows.Native.ToastInteropConstants;

namespace Fenestra.Toast.Windows.Native;

/// <summary>
/// Manages the lifecycle of a Windows toast notifier and provides high-level operations.
/// All COM interactions use raw IntPtr vtable calls via <see cref="WinRtToastInterop"/>.
/// </summary>
internal class NativeToastNotifier : IDisposable
{
    private IntPtr _pNotifier;
    private IntPtr _pNotifier2;
    private bool _disposed;

    public bool IsValid => _pNotifier != IntPtr.Zero;

    public NativeToastNotifier(string appId)
    {
        var pManager = WinRtToastInterop.GetActivationFactory(
            "Windows.UI.Notifications.ToastNotificationManager", IID_IToastNotificationManagerStatics);
        if (pManager == IntPtr.Zero)
            throw new InvalidOperationException("Failed to get ToastNotificationManager activation factory.");

        try
        {
            _pNotifier = WinRtToastInterop.CallGetPtr(pManager, Slot_Manager_CreateToastNotifier);

            if (_pNotifier == IntPtr.Zero)
                _pNotifier = WinRtToastInterop.CallWithHString(pManager, Slot_Manager_CreateToastNotifierWithId, appId);

            if (_pNotifier == IntPtr.Zero)
                throw new InvalidOperationException($"CreateToastNotifier failed for appId '{appId}'.");
        }
        finally { Marshal.Release(pManager); }

        TryQI(_pNotifier, IID_IToastNotifier2, out _pNotifier2);
    }

    // --- Show / Hide ---

    public void Show(IntPtr pNotification)
    {
        if (_pNotifier == IntPtr.Zero) throw new InvalidOperationException("Notifier not initialized.");
        if (pNotification == IntPtr.Zero) throw new ArgumentException("Notification pointer is null.");

        var pNotifier = QI(_pNotifier, IID_IToastNotifier);
        if (pNotifier == IntPtr.Zero) throw new InvalidOperationException("QI for IToastNotifier failed.");

        var pNotif = QI(pNotification, IID_IToastNotification);
        if (pNotif == IntPtr.Zero)
        {
            Marshal.Release(pNotifier);
            throw new InvalidOperationException("QI for IToastNotification failed on notification object.");
        }

        try
        {
            var hr = WinRtToastInterop.CallWithPtr(pNotifier, Slot_Notifier_Show, pNotif);
            if (hr < 0) throw new COMException($"IToastNotifier.Show failed. HRESULT=0x{hr:X8}", hr);
        }
        finally
        {
            Marshal.Release(pNotif);
            Marshal.Release(pNotifier);
        }
    }

    public void Hide(IntPtr pNotification)
    {
        if (_pNotifier == IntPtr.Zero) throw new InvalidOperationException("Notifier not initialized.");
        if (pNotification == IntPtr.Zero) throw new ArgumentException("Notification pointer is null.");

        var pNotifier = QI(_pNotifier, IID_IToastNotifier);
        if (pNotifier == IntPtr.Zero) throw new InvalidOperationException("QI for IToastNotifier failed.");

        var pNotif = QI(pNotification, IID_IToastNotification);
        if (pNotif == IntPtr.Zero)
        {
            Marshal.Release(pNotifier);
            throw new InvalidOperationException("QI for IToastNotification failed.");
        }

        try
        {
            var hr = WinRtToastInterop.CallWithPtr(pNotifier, Slot_Notifier_Hide, pNotif);
            if (hr < 0) throw new COMException($"IToastNotifier.Hide failed. HRESULT=0x{hr:X8}", hr);
        }
        finally
        {
            Marshal.Release(pNotif);
            Marshal.Release(pNotifier);
        }
    }

    // --- Create notification ---

    public IntPtr CreateXmlDocument(string xml)
    {
        var pXmlDoc = WinRtToastInterop.ActivateInstance("Windows.Data.Xml.Dom.XmlDocument");
        if (pXmlDoc == IntPtr.Zero)
            throw new InvalidOperationException("RoActivateInstance failed for Windows.Data.Xml.Dom.XmlDocument.");

        var pIO = QI(pXmlDoc, IID_IXmlDocumentIO);
        if (pIO == IntPtr.Zero)
        {
            Marshal.Release(pXmlDoc);
            throw new InvalidOperationException($"QI for IXmlDocumentIO failed. Tried GUID: {IID_IXmlDocumentIO}");
        }

        try
        {
            var hr = WinRtToastInterop.CallSetHString(pIO, Slot_XmlDocIO_LoadXml, xml);
            if (hr < 0) throw new COMException($"IXmlDocumentIO.LoadXml failed. HRESULT=0x{hr:X8}", hr);
        }
        finally { Marshal.Release(pIO); }

        return pXmlDoc;
    }

    public IntPtr CreateNotification(IntPtr pXmlDoc)
    {
        if (pXmlDoc == IntPtr.Zero) throw new ArgumentException("XmlDocument pointer is null.");

        var pFactory = WinRtToastInterop.GetActivationFactory(
            "Windows.UI.Notifications.ToastNotification", IID_IToastNotificationFactory);
        if (pFactory == IntPtr.Zero)
            throw new InvalidOperationException("Failed to get ToastNotification activation factory.");

        try
        {
            var fn = Marshal.GetDelegateForFunctionPointer<CreateNotifDelegate>(
                WinRtToastInterop.GetVtableSlot(pFactory, Slot_Factory_CreateToastNotification));
            var hr = fn(pFactory, pXmlDoc, out var pNotif);
            if (hr < 0) throw new COMException($"CreateToastNotification failed. HRESULT=0x{hr:X8}", hr);
            if (pNotif == IntPtr.Zero) throw new InvalidOperationException("CreateToastNotification returned null.");
            return pNotif;
        }
        finally { Marshal.Release(pFactory); }
    }

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    private delegate int CreateNotifDelegate(IntPtr @this, IntPtr content, out IntPtr notification);

    // --- Tag / Group / SuppressPopup / Priority / ExpiresOnReboot ---

    public void SetTag(IntPtr pNotif, string tag)
    {
        var p = QI(pNotif, IID_IToastNotification2);
        if (p == IntPtr.Zero) return;
        try { WinRtToastInterop.CallSetHString(p, Slot_Notification2_put_Tag, tag); }
        finally { Marshal.Release(p); }
    }

    public void SetGroup(IntPtr pNotif, string group)
    {
        var p = QI(pNotif, IID_IToastNotification2);
        if (p == IntPtr.Zero) return;
        try { WinRtToastInterop.CallSetHString(p, Slot_Notification2_put_Group, group); }
        finally { Marshal.Release(p); }
    }

    public void SetSuppressPopup(IntPtr pNotif, bool value)
    {
        var p = QI(pNotif, IID_IToastNotification2);
        if (p == IntPtr.Zero) return;
        try { WinRtToastInterop.CallSetBool(p, Slot_Notification2_put_SuppressPopup, value); }
        finally { Marshal.Release(p); }
    }

    public void SetPriority(IntPtr pNotif, int priority)
    {
        var p = QI(pNotif, IID_IToastNotification4);
        if (p == IntPtr.Zero) return;
        try { WinRtToastInterop.CallSetInt(p, Slot_Notification4_put_Priority, priority); }
        finally { Marshal.Release(p); }
    }

    public void SetExpiresOnReboot(IntPtr pNotif, bool value)
    {
        var p = QI(pNotif, IID_IToastNotification6);
        if (p == IntPtr.Zero) return;
        try { WinRtToastInterop.CallSetBool(p, Slot_Notification6_put_ExpiresOnReboot, value); }
        finally { Marshal.Release(p); }
    }

    // --- Events ---

    public long AddActivatedHandler(IntPtr pNotif, IntPtr pHandler)
        => WinRtToastInterop.CallAddEvent(pNotif, Slot_Notification_add_Activated, pHandler);

    public long AddDismissedHandler(IntPtr pNotif, IntPtr pHandler)
        => WinRtToastInterop.CallAddEvent(pNotif, Slot_Notification_add_Dismissed, pHandler);

    public long AddFailedHandler(IntPtr pNotif, IntPtr pHandler)
        => WinRtToastInterop.CallAddEvent(pNotif, Slot_Notification_add_Failed, pHandler);

    // --- Update (NotificationData) ---

    public int Update(string tag, string? group, Dictionary<string, string> data, uint sequenceNumber)
    {
        if (_pNotifier2 == IntPtr.Zero) return NotificationUpdateResult_Failed;

        var pData = CreateNotificationData(data, sequenceNumber);
        if (pData == IntPtr.Zero) return NotificationUpdateResult_Failed;

        try
        {
            if (group != null)
                return WinRtToastInterop.CallUpdateTagGroup(_pNotifier2, Slot_Notifier2_UpdateWithTagAndGroup, pData, tag, group);
            return WinRtToastInterop.CallUpdateTag(_pNotifier2, Slot_Notifier2_UpdateWithTag, pData, tag);
        }
        finally { Marshal.Release(pData); }
    }

    private IntPtr CreateNotificationData(Dictionary<string, string> data, uint sequenceNumber)
    {
        var pData = WinRtToastInterop.ActivateInstance("Windows.UI.Notifications.NotificationData");
        if (pData == IntPtr.Zero) return IntPtr.Zero;

        WinRtToastInterop.CallSetUInt(pData, Slot_NotifData_put_SequenceNumber, sequenceNumber);

        var pMap = WinRtToastInterop.CallGetPtr(pData, Slot_NotifData_get_Values);
        if (pMap != IntPtr.Zero)
        {
            foreach (var kv in data)
            {
                var hKey = WinRtToastInterop.CreateHString(kv.Key);
                var hVal = WinRtToastInterop.CreateHString(kv.Value);
                try
                {
                    var fn = Marshal.GetDelegateForFunctionPointer<MapInsertDelegate>(
                        WinRtToastInterop.GetVtableSlot(pMap, Slot_Map_Insert));
                    fn(pMap, hKey, hVal, out _);
                }
                finally
                {
                    WinRtToastInterop.FreeHString(hKey);
                    WinRtToastInterop.FreeHString(hVal);
                }
            }
            Marshal.Release(pMap);
        }

        return pData;
    }

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    private delegate int MapInsertDelegate(IntPtr @this, IntPtr key, IntPtr value, out int replaced);

    // --- History ---

    public void HistoryRemove(string tag)
    {
        var pHistory = GetHistory();
        if (pHistory == IntPtr.Zero) return;
        try { WinRtToastInterop.CallSetHString(pHistory, Slot_History_Remove, tag); }
        finally { Marshal.Release(pHistory); }
    }

    public void HistoryRemoveGrouped(string tag, string group)
    {
        var pHistory = GetHistory();
        if (pHistory == IntPtr.Zero) return;
        try { WinRtToastInterop.CallHStringHString(pHistory, Slot_History_RemoveGroupedTag, tag, group); }
        finally { Marshal.Release(pHistory); }
    }

    public void HistoryRemoveGroup(string group)
    {
        var pHistory = GetHistory();
        if (pHistory == IntPtr.Zero) return;
        try { WinRtToastInterop.CallSetHString(pHistory, Slot_History_RemoveGroup, group); }
        finally { Marshal.Release(pHistory); }
    }

    public void HistoryClear()
    {
        var pHistory = GetHistory();
        if (pHistory == IntPtr.Zero) return;
        try { WinRtToastInterop.CallVoid(pHistory, Slot_History_Clear); }
        finally { Marshal.Release(pHistory); }
    }

    public void HistoryClearWithId(string appId)
    {
        var pHistory = GetHistory();
        if (pHistory == IntPtr.Zero) return;
        try { WinRtToastInterop.CallSetHString(pHistory, Slot_History_ClearWithId, appId); }
        finally { Marshal.Release(pHistory); }
    }

    private IntPtr GetHistory()
    {
        var pManager2 = WinRtToastInterop.GetActivationFactory(
            "Windows.UI.Notifications.ToastNotificationManager", IID_IToastNotificationManagerStatics2);
        if (pManager2 == IntPtr.Zero) return IntPtr.Zero;

        try { return WinRtToastInterop.CallGetPtr(pManager2, Slot_Manager2_get_History); }
        finally { Marshal.Release(pManager2); }
    }

    // --- QI helpers ---

    private static IntPtr QI(IntPtr pObj, Guid iid)
    {
        var hr = Marshal.QueryInterface(pObj, ref iid, out var pResult);
        return hr == 0 ? pResult : IntPtr.Zero;
    }

    private static bool TryQI(IntPtr pObj, Guid iid, out IntPtr result)
    {
        result = QI(pObj, iid);
        return result != IntPtr.Zero;
    }

    // --- Dispose ---

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        WinRtToastInterop.SafeRelease(ref _pNotifier2);
        WinRtToastInterop.SafeRelease(ref _pNotifier);
    }
}
