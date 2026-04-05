using System.Runtime.InteropServices;

namespace Fenestra.Windows.Native.Toast;

// ═══════════════════════════════════════════════════════════════════
// [ComImport] interface definitions for Windows Toast Notification APIs.
// Uses InterfaceIsIUnknown + 3 IInspectable stubs for net472/net6.0 compatibility.
// HSTRING parameters are IntPtr — callers use HStringHandle for lifecycle.
// ═══════════════════════════════════════════════════════════════════

// ── Toast Notification Manager ─────────────────────────────────────

[ComImport, InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
[Guid("50AC103F-D235-4598-BBEF-98FE4D1A3AD4")]
internal interface IToastNotificationManagerStatics
{
    // IInspectable
    void GetIids(out int iidCount, out IntPtr iids);
    void GetRuntimeClassName(out IntPtr className);
    void GetTrustLevel(out int trustLevel);

    [PreserveSig] int CreateToastNotifier(out IntPtr result);
    [PreserveSig] int CreateToastNotifierWithId(IntPtr appId, out IntPtr result);
    [PreserveSig] int GetTemplateContent(int type, out IntPtr result);
}

[ComImport, InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
[Guid("7AB93C52-0E48-4750-BA9D-1A4113981847")]
internal interface IToastNotificationManagerStatics2
{
    // IInspectable
    void GetIids(out int iidCount, out IntPtr iids);
    void GetRuntimeClassName(out IntPtr className);
    void GetTrustLevel(out int trustLevel);

    [PreserveSig] int get_History(out IntPtr result);
}

// ── Toast Notifier ─────────────────────────────────────────────────

[ComImport, InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
[Guid("75927B93-03F3-41EC-91D3-6E5BAC1B38E7")]
internal interface IToastNotifier
{
    // IInspectable
    void GetIids(out int iidCount, out IntPtr iids);
    void GetRuntimeClassName(out IntPtr className);
    void GetTrustLevel(out int trustLevel);

    [PreserveSig] int Show(IntPtr notification);
    [PreserveSig] int Hide(IntPtr notification);
    [PreserveSig] int get_Setting(out int setting);
    [PreserveSig] int AddToSchedule(IntPtr scheduled);
    [PreserveSig] int RemoveFromSchedule(IntPtr scheduled);
    [PreserveSig] int GetScheduledToastNotifications(out IntPtr result);
}

[ComImport, InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
[Guid("354389C6-7C01-4BD5-9C20-604340CD2B74")]
internal interface IToastNotifier2
{
    // IInspectable
    void GetIids(out int iidCount, out IntPtr iids);
    void GetRuntimeClassName(out IntPtr className);
    void GetTrustLevel(out int trustLevel);

    [PreserveSig] int UpdateWithTagAndGroup(IntPtr data, IntPtr tag, IntPtr group, out int result);
    [PreserveSig] int UpdateWithTag(IntPtr data, IntPtr tag, out int result);
}

// ── Toast Notification ─────────────────────────────────────────────

[ComImport, InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
[Guid("997E2675-059E-4E60-8B06-1760917C8B80")]
internal interface IToastNotification
{
    // IInspectable
    void GetIids(out int iidCount, out IntPtr iids);
    void GetRuntimeClassName(out IntPtr className);
    void GetTrustLevel(out int trustLevel);

    [PreserveSig] int get_Content(out IntPtr result);
    [PreserveSig] int put_ExpirationTime(IntPtr value);
    [PreserveSig] int get_ExpirationTime(out IntPtr result);
    [PreserveSig] int add_Dismissed(IntPtr handler, out long token);
    [PreserveSig] int remove_Dismissed(long token);
    [PreserveSig] int add_Activated(IntPtr handler, out long token);
    [PreserveSig] int remove_Activated(long token);
    [PreserveSig] int add_Failed(IntPtr handler, out long token);
    [PreserveSig] int remove_Failed(long token);
}

[ComImport, InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
[Guid("9DFB9FD1-143A-490E-90BF-B9FBA7132DE7")]
internal interface IToastNotification2
{
    // IInspectable
    void GetIids(out int iidCount, out IntPtr iids);
    void GetRuntimeClassName(out IntPtr className);
    void GetTrustLevel(out int trustLevel);

    [PreserveSig] int put_Tag(IntPtr value);
    [PreserveSig] int get_Tag(out IntPtr result);
    [PreserveSig] int put_Group(IntPtr value);
    [PreserveSig] int get_Group(out IntPtr result);
    [PreserveSig] int put_SuppressPopup(int value);
    [PreserveSig] int get_SuppressPopup(out int result);
}

[ComImport, InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
[Guid("15154935-28EA-4727-88E9-C58680E2D118")]
internal interface IToastNotification4
{
    // IInspectable
    void GetIids(out int iidCount, out IntPtr iids);
    void GetRuntimeClassName(out IntPtr className);
    void GetTrustLevel(out int trustLevel);

    [PreserveSig] int get_Data(out IntPtr result);
    [PreserveSig] int put_Data(IntPtr value);
    [PreserveSig] int get_Priority(out int result);
    [PreserveSig] int put_Priority(int value);
}

[ComImport, InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
[Guid("43EBFE53-89AE-5C1E-A279-3AECFE9B6F54")]
internal interface IToastNotification6
{
    // IInspectable
    void GetIids(out int iidCount, out IntPtr iids);
    void GetRuntimeClassName(out IntPtr className);
    void GetTrustLevel(out int trustLevel);

    [PreserveSig] int get_ExpiresOnReboot(out int result);
    [PreserveSig] int put_ExpiresOnReboot(int value);
}

// ── Toast Notification Factory ─────────────────────────────────────

[ComImport, InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
[Guid("04124B20-82C6-4229-B109-FD9ED4662B53")]
internal interface IToastNotificationFactory
{
    // IInspectable
    void GetIids(out int iidCount, out IntPtr iids);
    void GetRuntimeClassName(out IntPtr className);
    void GetTrustLevel(out int trustLevel);

    [PreserveSig] int CreateToastNotification(IntPtr content, out IntPtr result);
}

// ── Toast Notification History ─────────────────────────────────────

[ComImport, InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
[Guid("5CADDC63-01D3-4C97-986F-0533483FEE14")]
internal interface IToastNotificationHistory
{
    // IInspectable
    void GetIids(out int iidCount, out IntPtr iids);
    void GetRuntimeClassName(out IntPtr className);
    void GetTrustLevel(out int trustLevel);

    [PreserveSig] int RemoveGroup(IntPtr group);
    [PreserveSig] int RemoveGroupWithId(IntPtr group, IntPtr appId);
    [PreserveSig] int RemoveGroupedTagWithId(IntPtr tag, IntPtr group, IntPtr appId);
    [PreserveSig] int RemoveGroupedTag(IntPtr tag, IntPtr group);
    [PreserveSig] int Remove(IntPtr tag);
    [PreserveSig] int Clear();
    [PreserveSig] int ClearWithId(IntPtr appId);
}

// ── XML Document ───────────────────────────────────────────────────

[ComImport, InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
[Guid("6CD0E74E-EE65-4489-9EBF-CA43E87BA637")]
internal interface IXmlDocumentIO
{
    // IInspectable
    void GetIids(out int iidCount, out IntPtr iids);
    void GetRuntimeClassName(out IntPtr className);
    void GetTrustLevel(out int trustLevel);

    [PreserveSig] int LoadXml(IntPtr xml);
    [PreserveSig] int LoadXmlWithSettings(IntPtr xml, IntPtr settings);
    [PreserveSig] int SaveToFileAsync(IntPtr file, out IntPtr result);
}

// ── Notification Data ──────────────────────────────────────────────

[ComImport, InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
[Guid("9FFD2312-9D6A-4AAF-B6AC-FF17F0C1F280")]
internal interface INotificationData
{
    // IInspectable
    void GetIids(out int iidCount, out IntPtr iids);
    void GetRuntimeClassName(out IntPtr className);
    void GetTrustLevel(out int trustLevel);

    [PreserveSig] int get_Values(out IntPtr result);
    [PreserveSig] int get_SequenceNumber(out uint result);
    [PreserveSig] int put_SequenceNumber(uint value);
}

// ── IMap<String, String> ───────────────────────────────────────────

[ComImport, InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
[Guid("F6D1F700-49C2-52AE-8154-826F9908773C")]
internal interface IMapStringString
{
    // IInspectable
    void GetIids(out int iidCount, out IntPtr iids);
    void GetRuntimeClassName(out IntPtr className);
    void GetTrustLevel(out int trustLevel);

    [PreserveSig] int Lookup(IntPtr key, out IntPtr value);
    [PreserveSig] int get_Size(out uint size);
    [PreserveSig] int HasKey(IntPtr key, out int found);
    [PreserveSig] int GetView(out IntPtr result);
    [PreserveSig] int Insert(IntPtr key, IntPtr value, out int replaced);
    [PreserveSig] int MapRemove(IntPtr key);
    [PreserveSig] int MapClear();
}

// ── Property Value ─────────────────────────────────────────────────

[ComImport, InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
[Guid("629BDBC8-D932-4FF4-96B9-8D96C5C1E858")]
internal interface IPropertyValueStatics
{
    // IInspectable
    void GetIids(out int iidCount, out IntPtr iids);
    void GetRuntimeClassName(out IntPtr className);
    void GetTrustLevel(out int trustLevel);

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
    [PreserveSig] int CreateString(IntPtr value, out IntPtr result);
    [PreserveSig] int CreateInspectable(IntPtr value, out IntPtr result);
    [PreserveSig] int CreateGuid(Guid value, out IntPtr result);
    [PreserveSig] int CreateDateTime(long value, out IntPtr result);
}

[ComImport, InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
[Guid("4BD682DD-7554-40E9-9A9B-82654EDE7E62")]
internal interface IPropertyValue
{
    // IInspectable
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
    [PreserveSig] int GetString(out IntPtr result);
}

// ── Toast Event Args ───────────────────────────────────────────────

[ComImport, InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
[Guid("E3BF92F3-C197-436F-8265-0625824F8DAC")]
internal interface IToastActivatedEventArgs
{
    // IInspectable
    void GetIids(out int iidCount, out IntPtr iids);
    void GetRuntimeClassName(out IntPtr className);
    void GetTrustLevel(out int trustLevel);

    [PreserveSig] int get_Arguments(out IntPtr result);
}

[ComImport, InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
[Guid("AB7DA512-CC61-568E-81BE-304AC31038FA")]
internal interface IToastActivatedEventArgs2
{
    // IInspectable
    void GetIids(out int iidCount, out IntPtr iids);
    void GetRuntimeClassName(out IntPtr className);
    void GetTrustLevel(out int trustLevel);

    [PreserveSig] int get_UserInput(out IntPtr result);
}

[ComImport, InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
[Guid("3F89D935-D9CB-4538-A0F0-FFE7659938F8")]
internal interface IToastDismissedEventArgs
{
    // IInspectable
    void GetIids(out int iidCount, out IntPtr iids);
    void GetRuntimeClassName(out IntPtr className);
    void GetTrustLevel(out int trustLevel);

    [PreserveSig] int get_Reason(out int reason);
}

[ComImport, InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
[Guid("35176862-CFD4-44F8-AD64-F500FD896C3B")]
internal interface IToastFailedEventArgs
{
    // IInspectable
    void GetIids(out int iidCount, out IntPtr iids);
    void GetRuntimeClassName(out IntPtr className);
    void GetTrustLevel(out int trustLevel);

    [PreserveSig] int get_ErrorCode(out int errorCode);
}

// ── Collection Interfaces (IKeyValuePair<String, Object>) ──────────

[ComImport, InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
[Guid("FE2F3D47-5D47-5499-8374-430C7CDA0204")]
internal interface IIterableKvpStringObject
{
    // IInspectable
    void GetIids(out int iidCount, out IntPtr iids);
    void GetRuntimeClassName(out IntPtr className);
    void GetTrustLevel(out int trustLevel);

    [PreserveSig] int First(out IntPtr result);
}

[ComImport, InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
[Guid("5DB5FA32-707C-5849-A06B-91C8EB9D10E8")]
internal interface IIteratorKvpStringObject
{
    // IInspectable
    void GetIids(out int iidCount, out IntPtr iids);
    void GetRuntimeClassName(out IntPtr className);
    void GetTrustLevel(out int trustLevel);

    [PreserveSig] int get_Current(out IntPtr result);
    [PreserveSig] int get_HasCurrent(out int hasCurrent);
    [PreserveSig] int MoveNext(out int hasCurrent);
}

[ComImport, InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
[Guid("09335560-6C6B-5A26-9348-97B781132B20")]
internal interface IKeyValuePairStringObject
{
    // IInspectable
    void GetIids(out int iidCount, out IntPtr iids);
    void GetRuntimeClassName(out IntPtr className);
    void GetTrustLevel(out int trustLevel);

    [PreserveSig] int get_Key(out IntPtr result);
    [PreserveSig] int get_Value(out IntPtr result);
}

// ── COM Callback Interfaces (classic COM, no IInspectable) ─────────

[ComImport, InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
[Guid("00000001-0000-0000-C000-000000000046")]
internal interface IClassFactory
{
    [PreserveSig]
    int CreateInstance(IntPtr pUnkOuter, ref Guid riid, out IntPtr ppvObject);

    [PreserveSig]
    int LockServer([MarshalAs(UnmanagedType.Bool)] bool fLock);
}

[ComImport, InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
[Guid("53E31837-6600-4A81-9395-75CFFE746F94")]
internal interface INotificationActivationCallback
{
    [PreserveSig]
    int Activate(
        [MarshalAs(UnmanagedType.LPWStr)] string appUserModelId,
        [MarshalAs(UnmanagedType.LPWStr)] string invokedArgs,
        IntPtr data,
        uint count);
}
