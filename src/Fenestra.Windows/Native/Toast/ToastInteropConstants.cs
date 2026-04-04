namespace Fenestra.Windows.Native.Toast;

/// <summary>
/// All COM GUIDs, vtable slot numbers, enum values, and HRESULT codes for Windows Toast Notifications.
/// Sources:
/// - https://github.com/tpn/winsdk-10/blob/master/Include/10.0.16299.0/winrt/windows.ui.notifications.idl
/// - https://github.com/microsoft/windows-rs (generated from Windows metadata)
/// - https://learn.microsoft.com/en-us/uwp/api/windows.ui.notifications
/// - https://github.com/microsoft/Windows-classic-samples/tree/main/Samples/DesktopToasts/CS
/// </summary>
internal static class ToastInteropConstants
{
    // =========================================================================
    // IUnknown / IInspectable base slots (apply to ALL WinRT interfaces)
    // =========================================================================
    // Slot 0: IUnknown.QueryInterface
    // Slot 1: IUnknown.AddRef
    // Slot 2: IUnknown.Release
    // Slot 3: IInspectable.GetIids
    // Slot 4: IInspectable.GetRuntimeClassName
    // Slot 5: IInspectable.GetTrustLevel
    // Slot 6+: Interface-specific methods

    // =========================================================================
    // Toast Notification Interfaces
    // https://learn.microsoft.com/en-us/uwp/api/windows.ui.notifications.toastnotification
    // =========================================================================

    /// <summary>IToastNotification — base interface for toast notifications.</summary>
    public static readonly Guid IID_IToastNotification = new("997E2675-059E-4E60-8B06-1760917C8B80");
    // Slot 6:  get_Content(out IXmlDocument*)
    // Slot 7:  put_ExpirationTime(IReference<DateTime>*)
    // Slot 8:  get_ExpirationTime(out IReference<DateTime>*)
    // Slot 9:  add_Dismissed(TypedEventHandler*, out EventRegistrationToken)
    // Slot 10: remove_Dismissed(EventRegistrationToken)
    // Slot 11: add_Activated(TypedEventHandler*, out EventRegistrationToken)
    // Slot 12: remove_Activated(EventRegistrationToken)
    // Slot 13: add_Failed(TypedEventHandler*, out EventRegistrationToken)
    // Slot 14: remove_Failed(EventRegistrationToken)
    public const int Slot_Notification_get_Content = 6;
    public const int Slot_Notification_put_ExpirationTime = 7;
    public const int Slot_Notification_get_ExpirationTime = 8;
    public const int Slot_Notification_add_Dismissed = 9;
    public const int Slot_Notification_remove_Dismissed = 10;
    public const int Slot_Notification_add_Activated = 11;
    public const int Slot_Notification_remove_Activated = 12;
    public const int Slot_Notification_add_Failed = 13;
    public const int Slot_Notification_remove_Failed = 14;

    /// <summary>IToastNotification2 — Tag, Group, SuppressPopup.</summary>
    public static readonly Guid IID_IToastNotification2 = new("9DFB9FD1-143A-490E-90BF-B9FBA7132DE7");
    // Slot 6:  put_Tag(HSTRING)
    // Slot 7:  get_Tag(out HSTRING)
    // Slot 8:  put_Group(HSTRING)
    // Slot 9:  get_Group(out HSTRING)
    // Slot 10: put_SuppressPopup(boolean)
    // Slot 11: get_SuppressPopup(out boolean)
    public const int Slot_Notification2_put_Tag = 6;
    public const int Slot_Notification2_get_Tag = 7;
    public const int Slot_Notification2_put_Group = 8;
    public const int Slot_Notification2_get_Group = 9;
    public const int Slot_Notification2_put_SuppressPopup = 10;
    public const int Slot_Notification2_get_SuppressPopup = 11;

    /// <summary>IToastNotification3 — NotificationMirroring, RemoteId.</summary>
    public static readonly Guid IID_IToastNotification3 = new("31E8AED8-8141-4F99-BC0A-C4ED21297D77");
    // Slot 6: get_NotificationMirroring(out NotificationMirroring)
    // Slot 7: put_NotificationMirroring(NotificationMirroring)
    // Slot 8: get_RemoteId(out HSTRING)
    // Slot 9: put_RemoteId(HSTRING)
    public const int Slot_Notification3_get_NotificationMirroring = 6;
    public const int Slot_Notification3_put_NotificationMirroring = 7;
    public const int Slot_Notification3_get_RemoteId = 8;
    public const int Slot_Notification3_put_RemoteId = 9;

    /// <summary>IToastNotification4 — Data (NotificationData), Priority.</summary>
    public static readonly Guid IID_IToastNotification4 = new("15154935-28EA-4727-88E9-C58680E2D118");
    // Slot 6: get_Data(out NotificationData*)
    // Slot 7: put_Data(NotificationData*)
    // Slot 8: get_Priority(out ToastNotificationPriority)
    // Slot 9: put_Priority(ToastNotificationPriority)
    public const int Slot_Notification4_get_Data = 6;
    public const int Slot_Notification4_put_Data = 7;
    public const int Slot_Notification4_get_Priority = 8;
    public const int Slot_Notification4_put_Priority = 9;

    /// <summary>IToastNotification6 — ExpiresOnReboot. (Windows 10 1903+)</summary>
    public static readonly Guid IID_IToastNotification6 = new("43EBFE53-89AE-5C1E-A279-3AECFE9B6F54");
    // Slot 6: get_ExpiresOnReboot(out boolean)
    // Slot 7: put_ExpiresOnReboot(boolean)
    public const int Slot_Notification6_get_ExpiresOnReboot = 6;
    public const int Slot_Notification6_put_ExpiresOnReboot = 7;

    // =========================================================================
    // Toast Notification Factory
    // https://learn.microsoft.com/en-us/uwp/api/windows.ui.notifications.toastnotification.-ctor
    // =========================================================================

    /// <summary>IToastNotificationFactory — creates ToastNotification from XmlDocument.</summary>
    public static readonly Guid IID_IToastNotificationFactory = new("04124B20-82C6-4229-B109-FD9ED4662B53");
    // Slot 6: CreateToastNotification(IXmlDocument* content, out IToastNotification* result)
    public const int Slot_Factory_CreateToastNotification = 6;

    // =========================================================================
    // Toast Notifier
    // https://learn.microsoft.com/en-us/uwp/api/windows.ui.notifications.toastnotifier
    // =========================================================================

    /// <summary>IToastNotifier — Show, Hide, Setting, Schedule.</summary>
    public static readonly Guid IID_IToastNotifier = new("75927B93-03F3-41EC-91D3-6E5BAC1B38E7");
    // Slot 6:  Show(IToastNotification*)
    // Slot 7:  Hide(IToastNotification*)
    // Slot 8:  get_Setting(out NotificationSetting)
    // Slot 9:  AddToSchedule(IScheduledToastNotification*)
    // Slot 10: RemoveFromSchedule(IScheduledToastNotification*)
    // Slot 11: GetScheduledToastNotifications(out IVectorView*)
    public const int Slot_Notifier_Show = 6;
    public const int Slot_Notifier_Hide = 7;
    public const int Slot_Notifier_get_Setting = 8;
    public const int Slot_Notifier_AddToSchedule = 9;
    public const int Slot_Notifier_RemoveFromSchedule = 10;
    public const int Slot_Notifier_GetScheduledToastNotifications = 11;

    /// <summary>IToastNotifier2 — Update notification data.</summary>
    public static readonly Guid IID_IToastNotifier2 = new("354389C6-7C01-4BD5-9C20-604340CD2B74");
    // Slot 6: UpdateWithTagAndGroup(NotificationData*, HSTRING tag, HSTRING group, out NotificationUpdateResult)
    // Slot 7: UpdateWithTag(NotificationData*, HSTRING tag, out NotificationUpdateResult)
    public const int Slot_Notifier2_UpdateWithTagAndGroup = 6;
    public const int Slot_Notifier2_UpdateWithTag = 7;

    /// <summary>IToastNotifier3 — (Windows 10 2004+)</summary>
    public static readonly Guid IID_IToastNotifier3 = new("AE75A04A-3B0C-51AD-B7E8-B08AB6052549");
    // Slot 6: ShowWithOptions(IToastNotification*, ToastNotificationOptions*)
    // Slot 7: UpdateWithTagAndGroupAndData(NotificationData*, HSTRING, HSTRING, out result)
    public const int Slot_Notifier3_ShowWithOptions = 6;

    // =========================================================================
    // Toast Notification Manager (Statics)
    // https://learn.microsoft.com/en-us/uwp/api/windows.ui.notifications.toastnotificationmanager
    // =========================================================================

    /// <summary>IToastNotificationManagerStatics — CreateToastNotifier, GetTemplateContent.</summary>
    public static readonly Guid IID_IToastNotificationManagerStatics = new("50AC103F-D235-4598-BBEF-98FE4D1A3AD4");
    // Slot 6: CreateToastNotifier(out IToastNotifier*)
    // Slot 7: CreateToastNotifierWithId(HSTRING appId, out IToastNotifier*)
    // Slot 8: GetTemplateContent(ToastTemplateType, out IXmlDocument*)
    public const int Slot_Manager_CreateToastNotifier = 6;
    public const int Slot_Manager_CreateToastNotifierWithId = 7;
    public const int Slot_Manager_GetTemplateContent = 8;

    /// <summary>IToastNotificationManagerStatics2 — History property.</summary>
    public static readonly Guid IID_IToastNotificationManagerStatics2 = new("7AB93C52-0E48-4750-BA9D-1A4113981847");
    // Slot 6: get_History(out IToastNotificationHistory*)
    public const int Slot_Manager2_get_History = 6;

    /// <summary>IToastNotificationManagerStatics4 — (Windows 10 Anniversary+)</summary>
    public static readonly Guid IID_IToastNotificationManagerStatics4 = new("8F993FD3-E516-45FB-8130-398E93FA52C3");
    // Slot 6: GetForUser(User*, out IToastNotificationManagerForUser*)

    /// <summary>IToastNotificationManagerStatics5 — (Windows 10 Fall Creators+)</summary>
    public static readonly Guid IID_IToastNotificationManagerStatics5 = new("D6F5F569-D40D-407C-8989-88CAB42CFD14");
    // Slot 6: GetDefault(out IToastNotificationManagerForUser*)

    // =========================================================================
    // Toast Notification History
    // https://learn.microsoft.com/en-us/uwp/api/windows.ui.notifications.toastnotificationhistory
    // =========================================================================

    /// <summary>IToastNotificationHistory — Remove, Clear operations.</summary>
    public static readonly Guid IID_IToastNotificationHistory = new("5CADDC63-01D3-4C97-986F-0533483FEE14");
    // Slot 6:  RemoveGroup(HSTRING group)
    // Slot 7:  RemoveGroupWithId(HSTRING group, HSTRING appId)
    // Slot 8:  RemoveGroupedTagWithId(HSTRING tag, HSTRING group, HSTRING appId)
    // Slot 9:  RemoveGroupedTag(HSTRING tag, HSTRING group)
    // Slot 10: Remove(HSTRING tag)
    // Slot 11: Clear()
    // Slot 12: ClearWithId(HSTRING appId)
    public const int Slot_History_RemoveGroup = 6;
    public const int Slot_History_RemoveGroupWithId = 7;
    public const int Slot_History_RemoveGroupedTagWithId = 8;
    public const int Slot_History_RemoveGroupedTag = 9;
    public const int Slot_History_Remove = 10;
    public const int Slot_History_Clear = 11;
    public const int Slot_History_ClearWithId = 12;

    /// <summary>IToastNotificationHistory2 — GetHistory returns list.</summary>
    public static readonly Guid IID_IToastNotificationHistory2 = new("3BC3D253-2F31-4092-9129-8AD5ABF067DA");
    // Slot 6: GetHistory(out IVectorView<ToastNotification>*)
    // Slot 7: GetHistoryWithId(HSTRING appId, out IVectorView*)
    public const int Slot_History2_GetHistory = 6;
    public const int Slot_History2_GetHistoryWithId = 7;

    // =========================================================================
    // Toast Activated / Dismissed / Failed Event Args
    // https://learn.microsoft.com/en-us/uwp/api/windows.ui.notifications.toastactivatedeventargs
    // =========================================================================

    /// <summary>IToastActivatedEventArgs — Arguments string.</summary>
    public static readonly Guid IID_IToastActivatedEventArgs = new("E3BF92F3-C197-436F-8265-0625824F8DAC");
    // Slot 6: get_Arguments(out HSTRING)
    public const int Slot_ActivatedArgs_get_Arguments = 6;

    /// <summary>IToastActivatedEventArgs2 — UserInput (ValueSet).</summary>
    public static readonly Guid IID_IToastActivatedEventArgs2 = new("AB7DA512-CC61-568E-81BE-304AC31038FA");
    // Slot 6: get_UserInput(out IPropertySet*)
    public const int Slot_ActivatedArgs2_get_UserInput = 6;

    /// <summary>IToastDismissedEventArgs — Reason.</summary>
    public static readonly Guid IID_IToastDismissedEventArgs = new("3F89D935-D9CB-4538-A0F0-FFE7659938F8");
    // Slot 6: get_Reason(out ToastDismissalReason)
    public const int Slot_DismissedArgs_get_Reason = 6;

    /// <summary>IToastFailedEventArgs — ErrorCode.</summary>
    public static readonly Guid IID_IToastFailedEventArgs = new("35176862-CFD4-44F8-AD64-F500FD896C3B");
    // Slot 6: get_ErrorCode(out HRESULT)
    public const int Slot_FailedArgs_get_ErrorCode = 6;

    // =========================================================================
    // Typed Event Handlers
    // https://learn.microsoft.com/en-us/uwp/api/windows.foundation.typedeventhandler-2
    // =========================================================================

    /// <summary>TypedEventHandler&lt;ToastNotification, Object&gt; — Activated event.</summary>
    public static readonly Guid IID_TypedEventHandler_Activated = new("AB54DE2D-97D9-5528-B6AD-105AFE156530");

    /// <summary>TypedEventHandler&lt;ToastNotification, ToastDismissedEventArgs&gt; — Dismissed event.</summary>
    public static readonly Guid IID_TypedEventHandler_Dismissed = new("61C2402F-0ED0-5A18-AB69-59F4AA99A368");

    /// <summary>TypedEventHandler&lt;ToastNotification, ToastFailedEventArgs&gt; — Failed event.</summary>
    public static readonly Guid IID_TypedEventHandler_Failed = new("95E3E803-C969-5E3A-9753-EA2AD22A9A33");

    // =========================================================================
    // Notification Data
    // https://learn.microsoft.com/en-us/uwp/api/windows.ui.notifications.notificationdata
    // =========================================================================

    /// <summary>INotificationData — Values map + SequenceNumber.</summary>
    public static readonly Guid IID_INotificationData = new("9FFD2312-9D6A-4AAF-B6AC-FF17F0C1F280");
    // Slot 6: get_Values(out IMap<String,String>*)
    // Slot 7: get_SequenceNumber(out UINT32)
    // Slot 8: put_SequenceNumber(UINT32)
    public const int Slot_NotifData_get_Values = 6;
    public const int Slot_NotifData_get_SequenceNumber = 7;
    public const int Slot_NotifData_put_SequenceNumber = 8;

    /// <summary>INotificationDataFactory — Create with initial values.</summary>
    public static readonly Guid IID_INotificationDataFactory = new("23C1E33A-1C10-46FB-8040-DEC384621CF8");
    // Slot 6: CreateNotificationDataWithValuesAndSequenceNumber(IIterable*, UINT32, out NotificationData*)
    // Slot 7: CreateNotificationDataWithValues(IIterable*, out NotificationData*)
    public const int Slot_NotifDataFactory_CreateWithValuesAndSeq = 6;
    public const int Slot_NotifDataFactory_CreateWithValues = 7;

    // =========================================================================
    // XML Document Interfaces
    // https://learn.microsoft.com/en-us/uwp/api/windows.data.xml.dom.xmldocument
    // =========================================================================

    /// <summary>IXmlDocument — DOM document.</summary>
    public static readonly Guid IID_IXmlDocument = new("F7F3A506-1E87-42D6-BCFB-B8C809FA5494");
    // Note: This is actually IXmlDocumentIO. The real IXmlDocument GUID is below.

    /// <summary>IXmlDocumentIO — LoadXml, SaveToFileAsync.</summary>
    public static readonly Guid IID_IXmlDocumentIO = new("6CD0E74E-EE65-4489-9EBF-CA43E87BA637");
    // Slot 6: LoadXml(HSTRING xml)
    // Slot 7: LoadXmlWithSettings(HSTRING xml, IXmlLoadSettings* settings)
    // Slot 8: SaveToFileAsync(IStorageFile*, out IAsyncAction*)
    public const int Slot_XmlDocIO_LoadXml = 6;
    public const int Slot_XmlDocIO_LoadXmlWithSettings = 7;

    /// <summary>IXmlNode — base interface for XML nodes.</summary>
    public static readonly Guid IID_IXmlNode = new("1C741D59-2122-47D5-A856-83F3D4214875");

    /// <summary>IXmlNodeSerializer — GetXml, InnerText.</summary>
    public static readonly Guid IID_IXmlNodeSerializer = new("5CC5B382-E6DD-4991-ABEF-06D8D2E7BD0C");
    // Slot 6: GetXml(out HSTRING)
    // Slot 7: get_InnerText(out HSTRING)
    // Slot 8: put_InnerText(HSTRING)
    public const int Slot_XmlSerializer_GetXml = 6;

    // =========================================================================
    // Foundation Collection Interfaces (parameterized for String,String)
    // https://learn.microsoft.com/en-us/uwp/api/windows.foundation.collections.imap-2
    // =========================================================================

    /// <summary>IMap&lt;String, String&gt; — mutable string-to-string map.</summary>
    public static readonly Guid IID_IMap_String_String = new("F6D1F700-49C2-52AE-8154-826F9908773C");
    // Slot 6:  Lookup(HSTRING key, out HSTRING value)
    // Slot 7:  get_Size(out UINT32)
    // Slot 8:  HasKey(HSTRING key, out boolean)
    // Slot 9:  GetView(out IMapView*)
    // Slot 10: Insert(HSTRING key, HSTRING value, out boolean replaced)
    // Slot 11: Remove(HSTRING key)
    // Slot 12: Clear()
    public const int Slot_Map_Lookup = 6;
    public const int Slot_Map_get_Size = 7;
    public const int Slot_Map_HasKey = 8;
    public const int Slot_Map_GetView = 9;
    public const int Slot_Map_Insert = 10;
    public const int Slot_Map_Remove = 11;
    public const int Slot_Map_Clear = 12;

    /// <summary>IMapView&lt;String, String&gt; — read-only view.</summary>
    public static readonly Guid IID_IMapView_String_String = new("AC7F26F2-FEB7-5B2A-8AC4-345BC62CAEDE");

    /// <summary>IIterable&lt;IKeyValuePair&lt;String, String&gt;&gt;</summary>
    public static readonly Guid IID_IIterable_KVP_String_String = new("E9BDAAF0-CBF6-5272-B751-F5AD7B256D78");
    // Slot 6: First(out IIterator*)
    public const int Slot_Iterable_First = 6;

    /// <summary>IIterator&lt;IKeyValuePair&lt;String, String&gt;&gt;</summary>
    public static readonly Guid IID_IIterator_KVP_String_String = new("05EB86F1-7140-5517-B88D-CBAEBE57E6B1");
    // Slot 6: get_Current(out IKeyValuePair*)
    // Slot 7: get_HasCurrent(out boolean)
    // Slot 8: MoveNext(out boolean)
    // Slot 9: GetMany(UINT32 capacity, IKeyValuePair*[] items, out UINT32 actual)
    public const int Slot_Iterator_get_Current = 6;
    public const int Slot_Iterator_get_HasCurrent = 7;
    public const int Slot_Iterator_MoveNext = 8;

    /// <summary>IKeyValuePair&lt;String, String&gt;</summary>
    public static readonly Guid IID_IKeyValuePair_String_String = new("60310303-49C5-52E6-ABC6-A9B36ECACA98");
    // Slot 6: get_Key(out HSTRING)
    // Slot 7: get_Value(out HSTRING)
    public const int Slot_KVP_get_Key = 6;
    public const int Slot_KVP_get_Value = 7;

    // =========================================================================
    // Foundation Property Interfaces
    // https://learn.microsoft.com/en-us/uwp/api/windows.foundation.propertyvalue
    // =========================================================================

    /// <summary>IPropertyValueStatics — boxing factory for primitives and DateTime.</summary>
    public static readonly Guid IID_IPropertyValueStatics = new("629BDBC8-D932-4FF4-96B9-8D96C5C1E858");
    // Slot 6:  CreateEmpty(out IInspectable*)
    // Slot 7:  CreateUInt8(BYTE, out IInspectable*)
    // Slot 8:  CreateInt16(INT16, out IInspectable*)
    // Slot 9:  CreateUInt16(UINT16, out IInspectable*)
    // Slot 10: CreateInt32(INT32, out IInspectable*)
    // Slot 11: CreateUInt32(UINT32, out IInspectable*)
    // Slot 12: CreateInt64(INT64, out IInspectable*)
    // Slot 13: CreateUInt64(UINT64, out IInspectable*)
    // Slot 14: CreateSingle(FLOAT, out IInspectable*)
    // Slot 15: CreateDouble(DOUBLE, out IInspectable*)
    // Slot 16: CreateChar16(WCHAR, out IInspectable*)
    // Slot 17: CreateBoolean(boolean, out IInspectable*)
    // Slot 18: CreateString(HSTRING, out IInspectable*)
    // Slot 19: CreateInspectable(IInspectable*, out IInspectable*)
    // Slot 20: CreateGuid(GUID, out IInspectable*)
    // Slot 21: CreateDateTime(DateTime, out IInspectable*)
    // Slot 22: CreateTimeSpan(TimeSpan, out IInspectable*)
    public const int Slot_PV_CreateDateTime = 21;

    /// <summary>IPropertyValue — unboxing for primitives.</summary>
    public static readonly Guid IID_IPropertyValue = new("4BD682DD-7554-40E9-9A9B-82654EDE7E62");
    // Slot 6:  get_Type(out PropertyType)
    // Slot 7:  get_IsNumericScalar(out boolean)
    // Slot 8–18: GetUInt8..GetBoolean
    // Slot 19: GetString(out HSTRING)
    public const int Slot_PV_GetString = 19;

    /// <summary>IPropertySet — string-to-object map (extends IObservableMap).</summary>
    public static readonly Guid IID_IPropertySet = new("8A43ED9F-F4E6-4421-ACF9-1DAB2986820C");

    /// <summary>IValueSet — used by ToastActivatedEventArgs2.UserInput.</summary>
    public static readonly Guid IID_IValueSet = new("8A43ED9F-F4E6-4421-ACF9-1DAB2986820C"); // same as IPropertySet

    // =========================================================================
    // Foundation Collection Interfaces (parameterized for String, Object)
    // Used for iterating IPropertySet / ValueSet from toast UserInput.
    // GUIDs computed from WinRT parameterized type signatures (UUID v5).
    // =========================================================================

    /// <summary>IIterable&lt;IKeyValuePair&lt;String, Object&gt;&gt;</summary>
    public static readonly Guid IID_IIterable_KVP_String_Object = new("FE2F3D47-5D47-5499-8374-430C7CDA0204");

    /// <summary>IIterator&lt;IKeyValuePair&lt;String, Object&gt;&gt;</summary>
    public static readonly Guid IID_IIterator_KVP_String_Object = new("5DB5FA32-707C-5849-A06B-91C8EB9D10E8");

    /// <summary>IKeyValuePair&lt;String, Object&gt;</summary>
    public static readonly Guid IID_IKeyValuePair_String_Object = new("09335560-6C6B-5A26-9348-97B781132B20");
    // Slot 6: get_Key(out HSTRING)
    // Slot 7: get_Value(out IInspectable*)
    public const int Slot_KVP_Object_get_Key = 6;
    public const int Slot_KVP_Object_get_Value = 7;

    // =========================================================================
    // Shell Link / Shortcut Interfaces (for AUMID + Toast Activation)
    // https://learn.microsoft.com/en-us/windows/win32/api/shobjidl_core/nn-shobjidl_core-ishelllinkw
    // =========================================================================

    /// <summary>IShellLinkW — shell shortcut interface.</summary>
    public static readonly Guid IID_IShellLinkW = new("000214F9-0000-0000-C000-000000000046");

    /// <summary>IPersistFile — save/load for shell links.</summary>
    public static readonly Guid IID_IPersistFile = new("0000010B-0000-0000-C000-000000000046");

    /// <summary>IPropertyStore — set properties on shell objects.</summary>
    public static readonly Guid IID_IPropertyStore = new("886D8EEB-8CF2-4446-8D02-CDBA1DBDCF99");

    /// <summary>CShellLink coclass CLSID.</summary>
    public static readonly Guid CLSID_ShellLink = new("00021401-0000-0000-C000-000000000046");

    // =========================================================================
    // Property Keys (for Start Menu shortcut AUMID registration)
    // https://learn.microsoft.com/en-us/windows/win32/properties/props-system-appusermodel-id
    // =========================================================================

    /// <summary>PKEY_AppUserModel_ID — {9F4C2855-9F79-4B39-A8D0-E1D42DE1D5F3}, pid=5</summary>
    public static readonly Guid PKEY_AppUserModel_ID_fmtid = new("9F4C2855-9F79-4B39-A8D0-E1D42DE1D5F3");
    public const uint PKEY_AppUserModel_ID_pid = 5;

    /// <summary>PKEY_AppUserModel_ToastActivatorCLSID — {9F4C2855-9F79-4B39-A8D0-E1D42DE1D5F3}, pid=26</summary>
    public static readonly Guid PKEY_AppUserModel_ToastActivatorCLSID_fmtid = new("9F4C2855-9F79-4B39-A8D0-E1D42DE1D5F3");
    public const uint PKEY_AppUserModel_ToastActivatorCLSID_pid = 26;

    // =========================================================================
    // Toast Activation Callback
    // https://learn.microsoft.com/en-us/windows/win32/api/notificationactivationcallback/nn-notificationactivationcallback-inotificationactivationcallback
    // =========================================================================

    /// <summary>INotificationActivationCallback — COM callback for background activation.</summary>
    public static readonly Guid IID_INotificationActivationCallback = new("53E31837-6600-4A81-9395-75CFFE746F94");

    /// <summary>IClassFactory — standard COM class factory.</summary>
    public static readonly Guid IID_IClassFactory = new("00000001-0000-0000-C000-000000000046");

    // =========================================================================
    // Enums — Toast Dismissal Reason
    // https://learn.microsoft.com/en-us/uwp/api/windows.ui.notifications.toastdismissalreason
    // =========================================================================
    public const int ToastDismissalReason_UserCanceled = 0;
    public const int ToastDismissalReason_ApplicationHidden = 1;
    public const int ToastDismissalReason_TimedOut = 2;

    // =========================================================================
    // Enums — Toast Notification Priority
    // https://learn.microsoft.com/en-us/uwp/api/windows.ui.notifications.toastnotificationpriority
    // =========================================================================
    public const int ToastNotificationPriority_Default = 0;
    public const int ToastNotificationPriority_High = 1;

    // =========================================================================
    // Enums — Notification Mirroring
    // https://learn.microsoft.com/en-us/uwp/api/windows.ui.notifications.notificationmirroring
    // =========================================================================
    public const int NotificationMirroring_Allowed = 0;
    public const int NotificationMirroring_Disabled = 1;

    // =========================================================================
    // Enums — Notification Setting
    // https://learn.microsoft.com/en-us/uwp/api/windows.ui.notifications.notificationsetting
    // =========================================================================
    public const int NotificationSetting_Enabled = 0;
    public const int NotificationSetting_DisabledForApplication = 1;
    public const int NotificationSetting_DisabledForUser = 2;
    public const int NotificationSetting_DisabledByGroupPolicy = 3;
    public const int NotificationSetting_DisabledByManifest = 4;

    // =========================================================================
    // Enums — Notification Update Result
    // https://learn.microsoft.com/en-us/uwp/api/windows.ui.notifications.notificationupdateresult
    // =========================================================================
    public const int NotificationUpdateResult_Succeeded = 0;
    public const int NotificationUpdateResult_Failed = 1;
    public const int NotificationUpdateResult_NotificationNotFound = 2;

    // =========================================================================
    // Enums — Toast Template Type
    // https://learn.microsoft.com/en-us/uwp/api/windows.ui.notifications.toasttemplatetype
    // =========================================================================
    public const int ToastTemplateType_ToastImageAndText01 = 0;
    public const int ToastTemplateType_ToastImageAndText02 = 1;
    public const int ToastTemplateType_ToastImageAndText03 = 2;
    public const int ToastTemplateType_ToastImageAndText04 = 3;
    public const int ToastTemplateType_ToastText01 = 4;
    public const int ToastTemplateType_ToastText02 = 5;
    public const int ToastTemplateType_ToastText03 = 6;
    public const int ToastTemplateType_ToastText04 = 7;

    // =========================================================================
    // Common HRESULT codes from Toast APIs
    // =========================================================================

    /// <summary>S_OK — Operation succeeded.</summary>
    public const int S_OK = 0;

    /// <summary>E_INVALIDARG — One or more arguments are invalid.</summary>
    public const int E_INVALIDARG = unchecked((int)0x80070057);

    /// <summary>E_NOINTERFACE — No such interface supported (QI failure).</summary>
    public const int E_NOINTERFACE = unchecked((int)0x80004002);

    /// <summary>E_ACCESSDENIED — General access denied error.</summary>
    public const int E_ACCESSDENIED = unchecked((int)0x80070005);

    /// <summary>E_NOTFOUND — Element not found (toast not in history).</summary>
    public const int E_NOTFOUND = unchecked((int)0x80070490);

    /// <summary>RPC_E_DISCONNECTED — The object invoked has disconnected from its clients.</summary>
    public const int RPC_E_DISCONNECTED = unchecked((int)0x80010108);

    /// <summary>WPN_E_TOAST_NOTIFICATION_DROPPED — Toast was not shown because app was in the background.</summary>
    public const int WPN_E_TOAST_NOTIFICATION_DROPPED = unchecked((int)0x803E0207);

    /// <summary>WPN_E_NOTIFICATION_DISABLED — Notifications are disabled for this app.</summary>
    public const int WPN_E_NOTIFICATION_DISABLED = unchecked((int)0x803E0202);

    /// <summary>WPN_E_NOTIFICATION_SIZE_TOO_LARGE — Notification payload exceeds 5KB.</summary>
    public const int WPN_E_NOTIFICATION_SIZE_TOO_LARGE = unchecked((int)0x803E0208);

    /// <summary>WPN_E_INVALID_APP — App not registered for notifications (no AUMID/shortcut).</summary>
    public const int WPN_E_INVALID_APP = unchecked((int)0x803E0209);
}
