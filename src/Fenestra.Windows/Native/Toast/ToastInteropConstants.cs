namespace Fenestra.Windows.Native.Toast;

/// <summary>
/// COM GUIDs, enum values, and HRESULT codes for Windows Toast Notifications.
/// Interface definitions are in <see cref="ToastInterfaces"/>.
/// </summary>
internal static class ToastInteropConstants
{
    // =========================================================================
    // Toast Notification Interfaces — GUIDs
    // =========================================================================

    public static readonly Guid IID_IToastNotification = new("997E2675-059E-4E60-8B06-1760917C8B80");
    public static readonly Guid IID_IToastNotification2 = new("9DFB9FD1-143A-490E-90BF-B9FBA7132DE7");
    public static readonly Guid IID_IToastNotification3 = new("31E8AED8-8141-4F99-BC0A-C4ED21297D77");
    public static readonly Guid IID_IToastNotification4 = new("15154935-28EA-4727-88E9-C58680E2D118");
    public static readonly Guid IID_IToastNotification6 = new("43EBFE53-89AE-5C1E-A279-3AECFE9B6F54");

    // ── Factory ──
    public static readonly Guid IID_IToastNotificationFactory = new("04124B20-82C6-4229-B109-FD9ED4662B53");

    // ── Notifier ──
    public static readonly Guid IID_IToastNotifier = new("75927B93-03F3-41EC-91D3-6E5BAC1B38E7");
    public static readonly Guid IID_IToastNotifier2 = new("354389C6-7C01-4BD5-9C20-604340CD2B74");
    public static readonly Guid IID_IToastNotifier3 = new("AE75A04A-3B0C-51AD-B7E8-B08AB6052549");

    // ── Manager Statics ──
    public static readonly Guid IID_IToastNotificationManagerStatics = new("50AC103F-D235-4598-BBEF-98FE4D1A3AD4");
    public static readonly Guid IID_IToastNotificationManagerStatics2 = new("7AB93C52-0E48-4750-BA9D-1A4113981847");
    public static readonly Guid IID_IToastNotificationManagerStatics4 = new("8F993FD3-E516-45FB-8130-398E93FA52C3");
    public static readonly Guid IID_IToastNotificationManagerStatics5 = new("D6F5F569-D40D-407C-8989-88CAB42CFD14");

    // ── History ──
    public static readonly Guid IID_IToastNotificationHistory = new("5CADDC63-01D3-4C97-986F-0533483FEE14");
    public static readonly Guid IID_IToastNotificationHistory2 = new("3BC3D253-2F31-4092-9129-8AD5ABF067DA");

    // ── Event Args ──
    public static readonly Guid IID_IToastActivatedEventArgs = new("E3BF92F3-C197-436F-8265-0625824F8DAC");
    public static readonly Guid IID_IToastActivatedEventArgs2 = new("AB7DA512-CC61-568E-81BE-304AC31038FA");
    public static readonly Guid IID_IToastDismissedEventArgs = new("3F89D935-D9CB-4538-A0F0-FFE7659938F8");
    public static readonly Guid IID_IToastFailedEventArgs = new("35176862-CFD4-44F8-AD64-F500FD896C3B");

    // ── Typed Event Handlers ──
    public static readonly Guid IID_TypedEventHandler_Activated = new("AB54DE2D-97D9-5528-B6AD-105AFE156530");
    public static readonly Guid IID_TypedEventHandler_Dismissed = new("61C2402F-0ED0-5A18-AB69-59F4AA99A368");
    public static readonly Guid IID_TypedEventHandler_Failed = new("95E3E803-C969-5E3A-9753-EA2AD22A9A33");

    // ── Notification Data ──
    public static readonly Guid IID_INotificationData = new("9FFD2312-9D6A-4AAF-B6AC-FF17F0C1F280");
    public static readonly Guid IID_INotificationDataFactory = new("23C1E33A-1C10-46FB-8040-DEC384621CF8");

    // ── XML Document ──
    public static readonly Guid IID_IXmlDocument = new("F7F3A506-1E87-42D6-BCFB-B8C809FA5494");
    public static readonly Guid IID_IXmlDocumentIO = new("6CD0E74E-EE65-4489-9EBF-CA43E87BA637");
    public static readonly Guid IID_IXmlNode = new("1C741D59-2122-47D5-A856-83F3D4214875");
    public static readonly Guid IID_IXmlNodeSerializer = new("5CC5B382-E6DD-4991-ABEF-06D8D2E7BD0C");

    // ── Foundation Collections ──
    public static readonly Guid IID_IMap_String_String = new("F6D1F700-49C2-52AE-8154-826F9908773C");
    public static readonly Guid IID_IMapView_String_String = new("AC7F26F2-FEB7-5B2A-8AC4-345BC62CAEDE");
    public static readonly Guid IID_IIterable_KVP_String_String = new("E9BDAAF0-CBF6-5272-B751-F5AD7B256D78");
    public static readonly Guid IID_IIterator_KVP_String_String = new("05EB86F1-7140-5517-B88D-CBAEBE57E6B1");
    public static readonly Guid IID_IKeyValuePair_String_String = new("60310303-49C5-52E6-ABC6-A9B36ECACA98");
    public static readonly Guid IID_IIterable_KVP_String_Object = new("FE2F3D47-5D47-5499-8374-430C7CDA0204");
    public static readonly Guid IID_IIterator_KVP_String_Object = new("5DB5FA32-707C-5849-A06B-91C8EB9D10E8");
    public static readonly Guid IID_IKeyValuePair_String_Object = new("09335560-6C6B-5A26-9348-97B781132B20");

    // ── Property Value ──
    public static readonly Guid IID_IPropertyValueStatics = new("629BDBC8-D932-4FF4-96B9-8D96C5C1E858");
    public static readonly Guid IID_IPropertyValue = new("4BD682DD-7554-40E9-9A9B-82654EDE7E62");
    public static readonly Guid IID_IPropertySet = new("8A43ED9F-F4E6-4421-ACF9-1DAB2986820C");
    public static readonly Guid IID_IValueSet = new("8A43ED9F-F4E6-4421-ACF9-1DAB2986820C");

    // =========================================================================
    // Shell Link / Shortcut Interfaces (for AUMID + Toast Activation)
    // =========================================================================

    public static readonly Guid IID_IShellLinkW = new("000214F9-0000-0000-C000-000000000046");
    public static readonly Guid IID_IPersistFile = new("0000010B-0000-0000-C000-000000000046");
    public static readonly Guid IID_IPropertyStore = new("886D8EEB-8CF2-4446-8D02-CDBA1DBDCF99");
    public static readonly Guid CLSID_ShellLink = new("00021401-0000-0000-C000-000000000046");

    // ── Property Keys ──
    public static readonly Guid PKEY_AppUserModel_ID_fmtid = new("9F4C2855-9F79-4B39-A8D0-E1D42DE1D5F3");
    public const uint PKEY_AppUserModel_ID_pid = 5;
    public static readonly Guid PKEY_AppUserModel_ToastActivatorCLSID_fmtid = new("9F4C2855-9F79-4B39-A8D0-E1D42DE1D5F3");
    public const uint PKEY_AppUserModel_ToastActivatorCLSID_pid = 26;

    // ── Toast Activation Callback ──
    public static readonly Guid IID_INotificationActivationCallback = new("53E31837-6600-4A81-9395-75CFFE746F94");
    public static readonly Guid IID_IClassFactory = new("00000001-0000-0000-C000-000000000046");

    // =========================================================================
    // Enums
    // =========================================================================

    public const int ToastDismissalReason_UserCanceled = 0;
    public const int ToastDismissalReason_ApplicationHidden = 1;
    public const int ToastDismissalReason_TimedOut = 2;

    public const int ToastNotificationPriority_Default = 0;
    public const int ToastNotificationPriority_High = 1;

    public const int NotificationMirroring_Allowed = 0;
    public const int NotificationMirroring_Disabled = 1;

    public const int NotificationSetting_Enabled = 0;
    public const int NotificationSetting_DisabledForApplication = 1;
    public const int NotificationSetting_DisabledForUser = 2;
    public const int NotificationSetting_DisabledByGroupPolicy = 3;
    public const int NotificationSetting_DisabledByManifest = 4;

    public const int NotificationUpdateResult_Succeeded = 0;
    public const int NotificationUpdateResult_Failed = 1;
    public const int NotificationUpdateResult_NotificationNotFound = 2;

    public const int ToastTemplateType_ToastImageAndText01 = 0;
    public const int ToastTemplateType_ToastImageAndText02 = 1;
    public const int ToastTemplateType_ToastImageAndText03 = 2;
    public const int ToastTemplateType_ToastImageAndText04 = 3;
    public const int ToastTemplateType_ToastText01 = 4;
    public const int ToastTemplateType_ToastText02 = 5;
    public const int ToastTemplateType_ToastText03 = 6;
    public const int ToastTemplateType_ToastText04 = 7;

    // =========================================================================
    // HRESULT codes
    // =========================================================================

    public const int S_OK = 0;
    public const int E_INVALIDARG = unchecked((int)0x80070057);
    public const int E_NOINTERFACE = unchecked((int)0x80004002);
    public const int E_ACCESSDENIED = unchecked((int)0x80070005);
    public const int E_NOTFOUND = unchecked((int)0x80070490);
    public const int RPC_E_DISCONNECTED = unchecked((int)0x80010108);
    public const int WPN_E_TOAST_NOTIFICATION_DROPPED = unchecked((int)0x803E0207);
    public const int WPN_E_NOTIFICATION_DISABLED = unchecked((int)0x803E0202);
    public const int WPN_E_NOTIFICATION_SIZE_TOO_LARGE = unchecked((int)0x803E0208);
    public const int WPN_E_INVALID_APP = unchecked((int)0x803E0209);
}
