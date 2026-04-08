using System.Runtime.InteropServices;
using Fenestra.Windows.Native;

namespace Fenestra.Windows.Native.Toast;

// ═══════════════════════════════════════════════════════════════════
// [ComImport] interface definitions for Windows Toast Notification APIs.
// Uses InterfaceIsIUnknown + 3 IInspectable stubs for net472/net6.0 compat.
// HSTRING parameters use HStringMarshaler for automatic string marshaling.
//
// IntPtr is used ONLY when:
//   - The type is IReference<T> (boxed nullable value — no [ComImport] possible)
//   - The type is IVectorView<T> (parameterized — no fixed GUID)
//   - The type is a raw COM callback pointer from TypedEventHandlerFactory
//   - The type is IXmlDocument (not defined here — only IXmlDocumentIO is used)
//   - COM plumbing (IClassFactory.CreateInstance, IInspectable stubs)
// ═══════════════════════════════════════════════════════════════════

// ── Toast Notification Manager ─────────────────────────────────────
// https://learn.microsoft.com/en-us/uwp/api/windows.ui.notifications.toastnotificationmanager

/// <summary>IToastNotificationManagerStatics — factory for <see cref="IToastNotifier"/> instances.</summary>
[ComImport, InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
[Guid("50AC103F-D235-4598-BBEF-98FE4D1A3AD4")]
internal interface IToastNotificationManagerStatics
{
    void GetIids(out int iidCount, out IntPtr iids);
    void GetRuntimeClassName(out IntPtr className);
    void GetTrustLevel(out int trustLevel);

    [PreserveSig] int CreateToastNotifier(out IntPtr result); // → IToastNotifier (wrapped via CastPointer)
    [PreserveSig] int CreateToastNotifierWithId([MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(HStringMarshaler))] string appId, out IntPtr result);
    [PreserveSig] int GetTemplateContent(int type, out IntPtr result); // → IXmlDocument
}

/// <summary>IToastNotificationManagerStatics2 — provides access to <see cref="IToastNotificationHistory"/>.</summary>
[ComImport, InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
[Guid("7AB93C52-0E48-4750-BA9D-1A4113981847")]
internal interface IToastNotificationManagerStatics2
{
    void GetIids(out int iidCount, out IntPtr iids);
    void GetRuntimeClassName(out IntPtr className);
    void GetTrustLevel(out int trustLevel);

    [PreserveSig] int get_History(out IntPtr result); // → IToastNotificationHistory (wrapped via CastPointer)
}

// ── Toast Notifier ─────────────────────────────────────────────────
// https://learn.microsoft.com/en-us/uwp/api/windows.ui.notifications.toastnotifier

/// <summary>IToastNotifier — displays and hides toast notifications.</summary>
[ComImport, InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
[Guid("75927B93-03F3-41EC-91D3-6E5BAC1B38E7")]
internal interface IToastNotifier
{
    void GetIids(out int iidCount, out IntPtr iids);
    void GetRuntimeClassName(out IntPtr className);
    void GetTrustLevel(out int trustLevel);

    [PreserveSig] int Show(IToastNotification notification);
    [PreserveSig] int Hide(IToastNotification notification);
    [PreserveSig] int get_Setting(out int setting);
    [PreserveSig] int AddToSchedule(IScheduledToastNotification scheduled);
    [PreserveSig] int RemoveFromSchedule(IScheduledToastNotification scheduled);
    [PreserveSig] int GetScheduledToastNotifications(out IntPtr result); // → IVectorView<IScheduledToastNotification>
}

/// <summary>IToastNotifier2 — updates toast notification data bindings.</summary>
[ComImport, InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
[Guid("354389C6-7C01-4BD5-9C20-604340CD2B74")]
internal interface IToastNotifier2
{
    void GetIids(out int iidCount, out IntPtr iids);
    void GetRuntimeClassName(out IntPtr className);
    void GetTrustLevel(out int trustLevel);

    [PreserveSig] int UpdateWithTagAndGroup(
        INotificationData data,
        [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(HStringMarshaler))] string tag,
        [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(HStringMarshaler))] string group,
        out int result);
    [PreserveSig] int UpdateWithTag(
        INotificationData data,
        [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(HStringMarshaler))] string tag,
        out int result);
}

// ── Toast Notification ─────────────────────────────────────────────
// https://learn.microsoft.com/en-us/uwp/api/windows.ui.notifications.toastnotification

/// <summary>IToastNotification — represents a toast with content, expiration, and event handlers.</summary>
[ComImport, InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
[Guid("997E2675-059E-4E60-8B06-1760917C8B80")]
internal interface IToastNotification
{
    void GetIids(out int iidCount, out IntPtr iids);
    void GetRuntimeClassName(out IntPtr className);
    void GetTrustLevel(out int trustLevel);

    [PreserveSig] int get_Content(out IntPtr result); // → IXmlDocument
    [PreserveSig] int put_ExpirationTime(IntPtr value); // IReference<DateTime> (boxed)
    [PreserveSig] int get_ExpirationTime(out IntPtr result); // IReference<DateTime> (boxed)
    [PreserveSig] int add_Dismissed(IntPtr handler, out long token); // TypedEventHandler (raw COM ptr)
    [PreserveSig] int remove_Dismissed(long token);
    [PreserveSig] int add_Activated(IntPtr handler, out long token); // TypedEventHandler (raw COM ptr)
    [PreserveSig] int remove_Activated(long token);
    [PreserveSig] int add_Failed(IntPtr handler, out long token); // TypedEventHandler (raw COM ptr)
    [PreserveSig] int remove_Failed(long token);
}

/// <summary>IToastNotification2 — adds Tag, Group, and SuppressPopup properties.</summary>
[ComImport, InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
[Guid("9DFB9FD1-143A-490E-90BF-B9FBA7132DE7")]
internal interface IToastNotification2
{
    void GetIids(out int iidCount, out IntPtr iids);
    void GetRuntimeClassName(out IntPtr className);
    void GetTrustLevel(out int trustLevel);

    [PreserveSig] int put_Tag([MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(HStringMarshaler))] string value);
    [PreserveSig] int get_Tag([MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(HStringMarshaler))] out string result);
    [PreserveSig] int put_Group([MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(HStringMarshaler))] string value);
    [PreserveSig] int get_Group([MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(HStringMarshaler))] out string result);
    [PreserveSig] int put_SuppressPopup(int value);
    [PreserveSig] int get_SuppressPopup(out int result);
}

/// <summary>IToastNotification3 — adds NotificationMirroring and RemoteId.</summary>
// https://learn.microsoft.com/en-us/uwp/api/windows.ui.notifications.toastnotification.notificationmirroring
[ComImport, InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
[Guid("31E8AED8-8141-4F99-BC0A-C4ED21297D77")]
internal interface IToastNotification3
{
    void GetIids(out int iidCount, out IntPtr iids);
    void GetRuntimeClassName(out IntPtr className);
    void GetTrustLevel(out int trustLevel);

    [PreserveSig] int get_NotificationMirroring(out int result);
    [PreserveSig] int put_NotificationMirroring(int value);
    [PreserveSig] int get_RemoteId([MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(HStringMarshaler))] out string result);
    [PreserveSig] int put_RemoteId([MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(HStringMarshaler))] string value);
}

/// <summary>IToastNotification4 — adds Data (NotificationData) and Priority.</summary>
[ComImport, InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
[Guid("15154935-28EA-4727-88E9-C58680E2D118")]
internal interface IToastNotification4
{
    void GetIids(out int iidCount, out IntPtr iids);
    void GetRuntimeClassName(out IntPtr className);
    void GetTrustLevel(out int trustLevel);

    [PreserveSig] int get_Data(out INotificationData result);
    [PreserveSig] int put_Data(INotificationData value);
    [PreserveSig] int get_Priority(out int result);
    [PreserveSig] int put_Priority(int value);
}

/// <summary>IToastNotification6 — adds ExpiresOnReboot (Windows 10 1903+).</summary>
[ComImport, InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
[Guid("43EBFE53-89AE-5C1E-A279-3AECFE9B6F54")]
internal interface IToastNotification6
{
    void GetIids(out int iidCount, out IntPtr iids);
    void GetRuntimeClassName(out IntPtr className);
    void GetTrustLevel(out int trustLevel);

    [PreserveSig] int get_ExpiresOnReboot(out int result);
    [PreserveSig] int put_ExpiresOnReboot(int value);
}

// ── Scheduled Toast Notification ────────────────────────────────────
// https://learn.microsoft.com/en-us/uwp/api/windows.ui.notifications.scheduledtoastnotification

/// <summary>IScheduledToastNotification — a toast notification scheduled for future delivery.</summary>
[ComImport, InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
[Guid("79F577F8-0DE7-48CD-9740-9B370490C838")]
internal interface IScheduledToastNotification
{
    void GetIids(out int iidCount, out IntPtr iids);
    void GetRuntimeClassName(out IntPtr className);
    void GetTrustLevel(out int trustLevel);

    [PreserveSig] int get_Content(out IntPtr result); // → IXmlDocument
    [PreserveSig] int get_DeliveryTime(out long result);
    [PreserveSig] int get_SnoozeInterval(out IntPtr result); // → IReference<TimeSpan> (boxed nullable)
    [PreserveSig] int get_MaximumSnoozeCount(out uint result);
    [PreserveSig] int put_Id([MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(HStringMarshaler))] string value);
    [PreserveSig] int get_Id([MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(HStringMarshaler))] out string result);
}

/// <summary>IScheduledToastNotification2 — adds Tag, Group, and SuppressPopup.</summary>
[ComImport, InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
[Guid("A66EA09C-31B4-43B0-B5DD-7A40E85363B1")]
internal interface IScheduledToastNotification2
{
    void GetIids(out int iidCount, out IntPtr iids);
    void GetRuntimeClassName(out IntPtr className);
    void GetTrustLevel(out int trustLevel);

    [PreserveSig] int put_Tag([MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(HStringMarshaler))] string value);
    [PreserveSig] int get_Tag([MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(HStringMarshaler))] out string result);
    [PreserveSig] int put_Group([MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(HStringMarshaler))] string value);
    [PreserveSig] int get_Group([MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(HStringMarshaler))] out string result);
    [PreserveSig] int put_SuppressPopup(int value);
    [PreserveSig] int get_SuppressPopup(out int result);
}

/// <summary>IScheduledToastNotification3 — adds NotificationMirroring and RemoteId.</summary>
[ComImport, InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
[Guid("98429E8B-BD32-4A3B-9D15-22AEA49462A1")]
internal interface IScheduledToastNotification3
{
    void GetIids(out int iidCount, out IntPtr iids);
    void GetRuntimeClassName(out IntPtr className);
    void GetTrustLevel(out int trustLevel);

    [PreserveSig] int get_NotificationMirroring(out int result);
    [PreserveSig] int put_NotificationMirroring(int value);
    [PreserveSig] int get_RemoteId([MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(HStringMarshaler))] out string result);
    [PreserveSig] int put_RemoteId([MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(HStringMarshaler))] string value);
}

/// <summary>IScheduledToastNotificationFactory — creates scheduled notifications with delivery time.</summary>
// https://learn.microsoft.com/en-us/uwp/api/windows.ui.notifications.scheduledtoastnotification.-ctor
[ComImport, InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
[Guid("E7BED191-0BB9-4571-8C0E-7F0AB7A7BD09")]
internal interface IScheduledToastNotificationFactory
{
    void GetIids(out int iidCount, out IntPtr iids);
    void GetRuntimeClassName(out IntPtr className);
    void GetTrustLevel(out int trustLevel);

    [PreserveSig] int CreateScheduledToastNotification([MarshalAs(UnmanagedType.IUnknown)] object content, long deliveryTime, out IntPtr result);
    [PreserveSig] int CreateScheduledToastNotificationRecurring([MarshalAs(UnmanagedType.IUnknown)] object content, long deliveryTime, long snoozeInterval, uint maximumSnoozeCount, out IntPtr result);
}

// ── Toast Notification Factory ─────────────────────────────────────
// https://learn.microsoft.com/en-us/uwp/api/windows.ui.notifications.toastnotification.-ctor

/// <summary>IToastNotificationFactory — creates a ToastNotification from an XmlDocument.</summary>
[ComImport, InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
[Guid("04124B20-82C6-4229-B109-FD9ED4662B53")]
internal interface IToastNotificationFactory
{
    void GetIids(out int iidCount, out IntPtr iids);
    void GetRuntimeClassName(out IntPtr className);
    void GetTrustLevel(out int trustLevel);

    [PreserveSig] int CreateToastNotification([MarshalAs(UnmanagedType.IUnknown)] object content, out IntPtr result);
}

// ── Toast Notification History ─────────────────────────────────────
// https://learn.microsoft.com/en-us/uwp/api/windows.ui.notifications.toastnotificationhistory

/// <summary>IToastNotificationHistory — manages toast history in Action Center (remove, clear).</summary>
[ComImport, InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
[Guid("5CADDC63-01D3-4C97-986F-0533483FEE14")]
internal interface IToastNotificationHistory
{
    void GetIids(out int iidCount, out IntPtr iids);
    void GetRuntimeClassName(out IntPtr className);
    void GetTrustLevel(out int trustLevel);

    [PreserveSig] int RemoveGroup([MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(HStringMarshaler))] string group);
    [PreserveSig] int RemoveGroupWithId(
        [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(HStringMarshaler))] string group,
        [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(HStringMarshaler))] string appId);
    [PreserveSig] int RemoveGroupedTagWithId(
        [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(HStringMarshaler))] string tag,
        [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(HStringMarshaler))] string group,
        [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(HStringMarshaler))] string appId);
    [PreserveSig] int RemoveGroupedTag(
        [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(HStringMarshaler))] string tag,
        [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(HStringMarshaler))] string group);
    [PreserveSig] int Remove([MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(HStringMarshaler))] string tag);
    [PreserveSig] int Clear();
    [PreserveSig] int ClearWithId([MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(HStringMarshaler))] string appId);
}

/// <summary>IToastNotificationHistory2 — retrieves toast notification history list.</summary>
// https://learn.microsoft.com/en-us/uwp/api/windows.ui.notifications.toastnotificationhistory.gethistory
[ComImport, InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
[Guid("3BC3D253-2F31-4092-9129-8AD5ABF067DA")]
internal interface IToastNotificationHistory2
{
    void GetIids(out int iidCount, out IntPtr iids);
    void GetRuntimeClassName(out IntPtr className);
    void GetTrustLevel(out int trustLevel);

    [PreserveSig] int GetHistory(out IntPtr result); // → IVectorView<ToastNotification>
    [PreserveSig] int GetHistoryWithId([MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(HStringMarshaler))] string appId, out IntPtr result);
}

// ── Toast Event Args ───────────────────────────────────────────────
// https://learn.microsoft.com/en-us/uwp/api/windows.ui.notifications.toastactivatedeventargs

/// <summary>IToastActivatedEventArgs — provides the activation arguments string.</summary>
[ComImport, InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
[Guid("E3BF92F3-C197-436F-8265-0625824F8DAC")]
internal interface IToastActivatedEventArgs
{
    void GetIids(out int iidCount, out IntPtr iids);
    void GetRuntimeClassName(out IntPtr className);
    void GetTrustLevel(out int trustLevel);

    [PreserveSig] int get_Arguments([MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(HStringMarshaler))] out string result);
}

/// <summary>IToastActivatedEventArgs2 — provides user input from interactive toasts.</summary>
// https://learn.microsoft.com/en-us/uwp/api/windows.ui.notifications.toastactivatedeventargs.userinput
[ComImport, InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
[Guid("AB7DA512-CC61-568E-81BE-304AC31038FA")]
internal interface IToastActivatedEventArgs2
{
    void GetIids(out int iidCount, out IntPtr iids);
    void GetRuntimeClassName(out IntPtr className);
    void GetTrustLevel(out int trustLevel);

    [PreserveSig] int get_UserInput(out IntPtr result); // → IPropertySet (iterated via IIterableKvpStringObject)
}

/// <summary>IToastDismissedEventArgs — provides the dismissal reason.</summary>
// https://learn.microsoft.com/en-us/uwp/api/windows.ui.notifications.toastdismissedeventargs
[ComImport, InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
[Guid("3F89D935-D9CB-4538-A0F0-FFE7659938F8")]
internal interface IToastDismissedEventArgs
{
    void GetIids(out int iidCount, out IntPtr iids);
    void GetRuntimeClassName(out IntPtr className);
    void GetTrustLevel(out int trustLevel);

    [PreserveSig] int get_Reason(out int reason);
}

/// <summary>IToastFailedEventArgs — provides the error code on failure.</summary>
// https://learn.microsoft.com/en-us/uwp/api/windows.ui.notifications.toastfailedeventargs
[ComImport, InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
[Guid("35176862-CFD4-44F8-AD64-F500FD896C3B")]
internal interface IToastFailedEventArgs
{
    void GetIids(out int iidCount, out IntPtr iids);
    void GetRuntimeClassName(out IntPtr className);
    void GetTrustLevel(out int trustLevel);

    [PreserveSig] int get_ErrorCode(out int errorCode);
}

// ── XML Document ───────────────────────────────────────────────────
// https://learn.microsoft.com/en-us/uwp/api/windows.data.xml.dom.xmldocument

/// <summary>IXmlDocumentIO — loads and saves XML content.</summary>
[ComImport, InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
[Guid("6CD0E74E-EE65-4489-9EBF-CA43E87BA637")]
internal interface IXmlDocumentIO
{
    void GetIids(out int iidCount, out IntPtr iids);
    void GetRuntimeClassName(out IntPtr className);
    void GetTrustLevel(out int trustLevel);

    [PreserveSig] int LoadXml([MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(HStringMarshaler))] string xml);
    [PreserveSig] int LoadXmlWithSettings([MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(HStringMarshaler))] string xml, IntPtr settings); // IXmlLoadSettings
    [PreserveSig] int SaveToFileAsync(IntPtr file, out IntPtr result); // IStorageFile → IAsyncAction
}

// ── Notification Data ──────────────────────────────────────────────
// https://learn.microsoft.com/en-us/uwp/api/windows.ui.notifications.notificationdata

/// <summary>INotificationData — provides key/value data bindings and sequence number for toast updates.</summary>
[ComImport, InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
[Guid("9FFD2312-9D6A-4AAF-B6AC-FF17F0C1F280")]
internal interface INotificationData
{
    void GetIids(out int iidCount, out IntPtr iids);
    void GetRuntimeClassName(out IntPtr className);
    void GetTrustLevel(out int trustLevel);

    [PreserveSig] int get_Values(out IMapStringString result);
    [PreserveSig] int get_SequenceNumber(out uint result);
    [PreserveSig] int put_SequenceNumber(uint value);
}

// ── IMap<String, String> ───────────────────────────────────────────
// https://learn.microsoft.com/en-us/uwp/api/windows.foundation.collections.imap-2

/// <summary>IMap&lt;String, String&gt; — mutable string-to-string map used by <see cref="INotificationData"/>.</summary>
[ComImport, InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
[Guid("F6D1F700-49C2-52AE-8154-826F9908773C")]
internal interface IMapStringString
{
    void GetIids(out int iidCount, out IntPtr iids);
    void GetRuntimeClassName(out IntPtr className);
    void GetTrustLevel(out int trustLevel);

    [PreserveSig] int Lookup(
        [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(HStringMarshaler))] string key,
        [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(HStringMarshaler))] out string value);
    [PreserveSig] int get_Size(out uint size);
    [PreserveSig] int HasKey([MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(HStringMarshaler))] string key, out int found);
    [PreserveSig] int GetView(out IntPtr result); // → IMapView<String, String>
    [PreserveSig] int Insert(
        [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(HStringMarshaler))] string key,
        [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(HStringMarshaler))] string value,
        out int replaced);
    [PreserveSig] int MapRemove([MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(HStringMarshaler))] string key);
    [PreserveSig] int MapClear();
}

// ── Property Value ─────────────────────────────────────────────────
// https://learn.microsoft.com/en-us/uwp/api/windows.foundation.propertyvalue

/// <summary>IPropertyValueStatics — boxing factory for primitives and DateTime.</summary>
[ComImport, InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
[Guid("629BDBC8-D932-4FF4-96B9-8D96C5C1E858")]
internal interface IPropertyValueStatics
{
    void GetIids(out int iidCount, out IntPtr iids);
    void GetRuntimeClassName(out IntPtr className);
    void GetTrustLevel(out int trustLevel);

    // All return IInspectable (boxed value) — must be IntPtr
    [PreserveSig] int CreateEmpty(out IntPtr result);
    [PreserveSig] int CreateUInt8(byte value, out IntPtr result);
    [PreserveSig] int CreateInt16(short value, out IntPtr result);
    [PreserveSig] int CreateUInt16(ushort value, out IntPtr result);
    [PreserveSig] int CreateInt32(int value, out IntPtr result);
    [PreserveSig] int CreateUInt32(uint value, out IntPtr result);
    [PreserveSig] int CreateInt64(long value, out IntPtr result);
    [PreserveSig] int CreateUInt64(ulong value, out IntPtr result);
    [PreserveSig] int CreateSingle(float value, out IntPtr result);
    [PreserveSig] int CreateDouble(double value, out IntPtr result);
    [PreserveSig] int CreateChar16(ushort value, out IntPtr result);
    [PreserveSig] int CreateBoolean(int value, out IntPtr result);
    [PreserveSig] int CreateString([MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(HStringMarshaler))] string value, out IntPtr result);
    [PreserveSig] int CreateInspectable(IntPtr value, out IntPtr result);
    [PreserveSig] int CreateGuid(Guid value, out IntPtr result);
    [PreserveSig] int CreateDateTime(long value, out IntPtr result);
}

/// <summary>IPropertyValue — unboxing for primitives.</summary>
[ComImport, InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
[Guid("4BD682DD-7554-40E9-9A9B-82654EDE7E62")]
internal interface IPropertyValue
{
    void GetIids(out int iidCount, out IntPtr iids);
    void GetRuntimeClassName(out IntPtr className);
    void GetTrustLevel(out int trustLevel);

    [PreserveSig] int get_Type(out int type);
    [PreserveSig] int get_IsNumericScalar(out int result);
    [PreserveSig] int GetUInt8(out byte result);
    [PreserveSig] int GetInt16(out short result);
    [PreserveSig] int GetUInt16(out ushort result);
    [PreserveSig] int GetInt32(out int result);
    [PreserveSig] int GetUInt32(out uint result);
    [PreserveSig] int GetInt64(out long result);
    [PreserveSig] int GetUInt64(out ulong result);
    [PreserveSig] int GetSingle(out float result);
    [PreserveSig] int GetDouble(out double result);
    [PreserveSig] int GetChar16(out ushort result);
    [PreserveSig] int GetBoolean(out int result);
    [PreserveSig] int GetString([MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(HStringMarshaler))] out string result);
}

// ── Collection Interfaces (IKeyValuePair<String, Object>) ──────────
// https://learn.microsoft.com/en-us/uwp/api/windows.foundation.collections.ikeyvaluepair-2

/// <summary>IIterable&lt;IKeyValuePair&lt;String, Object&gt;&gt; — used to iterate user input from toast activation.</summary>
[ComImport, InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
[Guid("FE2F3D47-5D47-5499-8374-430C7CDA0204")]
internal interface IIterableKvpStringObject
{
    void GetIids(out int iidCount, out IntPtr iids);
    void GetRuntimeClassName(out IntPtr className);
    void GetTrustLevel(out int trustLevel);

    [PreserveSig] int First(out IntPtr result); // → IIteratorKvpStringObject (wrapped via CastPointer)
}

/// <summary>IIterator&lt;IKeyValuePair&lt;String, Object&gt;&gt;</summary>
[ComImport, InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
[Guid("5DB5FA32-707C-5849-A06B-91C8EB9D10E8")]
internal interface IIteratorKvpStringObject
{
    void GetIids(out int iidCount, out IntPtr iids);
    void GetRuntimeClassName(out IntPtr className);
    void GetTrustLevel(out int trustLevel);

    [PreserveSig] int get_Current(out IntPtr result); // → IKeyValuePairStringObject (wrapped via CastPointer)
    [PreserveSig] int get_HasCurrent(out int hasCurrent);
    [PreserveSig] int MoveNext(out int hasCurrent);
}

/// <summary>IKeyValuePair&lt;String, Object&gt;</summary>
[ComImport, InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
[Guid("09335560-6C6B-5A26-9348-97B781132B20")]
internal interface IKeyValuePairStringObject
{
    void GetIids(out int iidCount, out IntPtr iids);
    void GetRuntimeClassName(out IntPtr className);
    void GetTrustLevel(out int trustLevel);

    [PreserveSig] int get_Key([MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(HStringMarshaler))] out string result);
    [PreserveSig] int get_Value(out IntPtr result); // → IInspectable (any boxed type)
}

// ── COM Callback Interfaces (classic COM, no IInspectable) ─────────

/// <summary>IClassFactory — standard COM class factory for toast activation server.</summary>
// https://learn.microsoft.com/en-us/windows/win32/api/unknwn/nn-unknwn-iclassfactory
[ComImport, InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
[Guid("00000001-0000-0000-C000-000000000046")]
internal interface IClassFactory
{
    [PreserveSig]
    int CreateInstance(IntPtr pUnkOuter, ref Guid riid, out IntPtr ppvObject);

    [PreserveSig]
    int LockServer([MarshalAs(UnmanagedType.Bool)] bool fLock);
}

/// <summary>INotificationActivationCallback — COM callback invoked when a toast is clicked.</summary>
// https://learn.microsoft.com/en-us/windows/win32/api/notificationactivationcallback/nn-notificationactivationcallback-inotificationactivationcallback
[ComImport, InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
[Guid("53E31837-6600-4A81-9395-75CFFE746F94")]
internal interface INotificationActivationCallback
{
    [PreserveSig]
    int Activate(
        [MarshalAs(UnmanagedType.LPWStr)] string appUserModelId,
        [MarshalAs(UnmanagedType.LPWStr)] string invokedArgs,
        IntPtr data, // NOTIFICATION_USER_INPUT_DATA*
        uint count);
}
