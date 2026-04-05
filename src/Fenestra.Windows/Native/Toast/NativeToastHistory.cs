using System.Runtime.InteropServices;

namespace Fenestra.Windows.Native.Toast;

/// <summary>
/// Responsible for managing toast notification history in Action Center (remove, clear).
/// Stateless — obtains the IToastNotificationHistory pointer on each call.
/// </summary>
internal static class NativeToastHistory
{
    public static void Remove(string tag)
    {
        var history = GetHistory();
        if (history == null) return;
        try
        {
            using var hTag = HStringHandle.Create(tag);
            history.Remove(hTag.DangerousGetHandle());
        }
        finally { Marshal.ReleaseComObject(history); }
    }

    public static void RemoveGrouped(string tag, string group)
    {
        var history = GetHistory();
        if (history == null) return;
        try
        {
            using var hTag = HStringHandle.Create(tag);
            using var hGroup = HStringHandle.Create(group);
            history.RemoveGroupedTag(hTag.DangerousGetHandle(), hGroup.DangerousGetHandle());
        }
        finally { Marshal.ReleaseComObject(history); }
    }

    public static void RemoveGroup(string group)
    {
        var history = GetHistory();
        if (history == null) return;
        try
        {
            using var hGroup = HStringHandle.Create(group);
            history.RemoveGroup(hGroup.DangerousGetHandle());
        }
        finally { Marshal.ReleaseComObject(history); }
    }

    public static void Clear()
    {
        var history = GetHistory();
        if (history == null) return;
        try { history.Clear(); }
        finally { Marshal.ReleaseComObject(history); }
    }

    public static void ClearWithId(string appId)
    {
        var history = GetHistory();
        if (history == null) return;
        try
        {
            using var hAppId = HStringHandle.Create(appId);
            history.ClearWithId(hAppId.DangerousGetHandle());
        }
        finally { Marshal.ReleaseComObject(history); }
    }

    private static IToastNotificationHistory? GetHistory()
    {
        var manager2 = WinRtToastInterop.GetActivationFactoryAs<IToastNotificationManagerStatics2>(
            "Windows.UI.Notifications.ToastNotificationManager", ToastInteropConstants.IID_IToastNotificationManagerStatics2);
        if (manager2 == null) return null;

        try
        {
            var hr = manager2.get_History(out var pHistory);
            if (hr != 0 || pHistory == IntPtr.Zero) return null;
            return WinRtToastInterop.CastComPointer<IToastNotificationHistory>(pHistory);
        }
        finally { Marshal.ReleaseComObject(manager2); }
    }
}
