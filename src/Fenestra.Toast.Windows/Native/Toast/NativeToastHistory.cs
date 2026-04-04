using static Fenestra.Toast.Windows.Native.Toast.ToastInteropConstants;

namespace Fenestra.Toast.Windows.Native.Toast;

/// <summary>
/// Responsible for managing toast notification history in Action Center (remove, clear).
/// Stateless — obtains the IToastNotificationHistory pointer on each call.
/// </summary>
internal static class NativeToastHistory
{
    public static void Remove(string tag)
    {
        using var pHistory = GetHistory();
        if (pHistory == null) return;
        WinRtToastInterop.CallSetHString(pHistory, Slot_History_Remove, tag);
    }

    public static void RemoveGrouped(string tag, string group)
    {
        using var pHistory = GetHistory();
        if (pHistory == null) return;
        WinRtToastInterop.CallHStringHString(pHistory, Slot_History_RemoveGroupedTag, tag, group);
    }

    public static void RemoveGroup(string group)
    {
        using var pHistory = GetHistory();
        if (pHistory == null) return;
        WinRtToastInterop.CallSetHString(pHistory, Slot_History_RemoveGroup, group);
    }

    public static void Clear()
    {
        using var pHistory = GetHistory();
        if (pHistory == null) return;
        WinRtToastInterop.CallVoid(pHistory, Slot_History_Clear);
    }

    public static void ClearWithId(string appId)
    {
        using var pHistory = GetHistory();
        if (pHistory == null) return;
        WinRtToastInterop.CallSetHString(pHistory, Slot_History_ClearWithId, appId);
    }

    private static ComPointerHandle? GetHistory()
    {
        using var pManager2 = WinRtToastInterop.GetActivationFactory(
            "Windows.UI.Notifications.ToastNotificationManager", IID_IToastNotificationManagerStatics2);
        return pManager2 != null ? WinRtToastInterop.CallGetPtr(pManager2, Slot_Manager2_get_History) : null;
    }
}
