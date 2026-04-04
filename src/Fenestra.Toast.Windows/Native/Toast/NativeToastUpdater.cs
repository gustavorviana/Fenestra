using System.Runtime.InteropServices;
using static Fenestra.Toast.Windows.Native.Toast.ToastInteropConstants;

namespace Fenestra.Toast.Windows.Native.Toast;

/// <summary>
/// Responsible for updating toast notification data bindings (progress bars).
/// Owns the IToastNotifier2 COM pointer. May be null if the OS doesn't support it.
/// </summary>
internal sealed class NativeToastUpdater : IDisposable
{
    private readonly ComPointerHandle _pNotifier2;

    private NativeToastUpdater(ComPointerHandle pNotifier2)
    {
        _pNotifier2 = pNotifier2;
    }

    /// <summary>
    /// Creates an updater if the notifier supports IToastNotifier2. Returns null otherwise.
    /// </summary>
    public static NativeToastUpdater? TryCreate(ComPointerHandle pNotifier)
    {
        var p = pNotifier.QueryInterface(IID_IToastNotifier2);
        return p != null ? new NativeToastUpdater(p) : null;
    }

    public int Update(string tag, string? group, Dictionary<string, string> data, uint sequenceNumber)
    {
        using var pData = CreateNotificationData(data, sequenceNumber);
        if (pData == null) return NotificationUpdateResult_Failed;

        if (group != null)
            return WinRtToastInterop.CallUpdateTagGroup(_pNotifier2, Slot_Notifier2_UpdateWithTagAndGroup, pData, tag, group);

        return WinRtToastInterop.CallUpdateTag(_pNotifier2, Slot_Notifier2_UpdateWithTag, pData, tag);
    }

    private static ComPointerHandle? CreateNotificationData(Dictionary<string, string> data, uint sequenceNumber)
    {
        var pData = WinRtToastInterop.ActivateInstance("Windows.UI.Notifications.NotificationData");
        if (pData == null) return null;

        WinRtToastInterop.CallSetUInt(pData, Slot_NotifData_put_SequenceNumber, sequenceNumber);

        using var pMap = WinRtToastInterop.CallGetPtr(pData, Slot_NotifData_get_Values);
        if (pMap != null)
        {
            foreach (var kv in data)
            {
                using var hKey = HStringHandle.Create(kv.Key);
                using var hVal = HStringHandle.Create(kv.Value);
                var fn = ComFactory.GetDelegate<MapInsertDelegate>(
                    WinRtToastInterop.GetVtableSlot(pMap, Slot_Map_Insert));
                fn(pMap.DangerousGetHandle(), hKey.DangerousGetHandle(), hVal.DangerousGetHandle(), out _);
            }
        }

        return pData;
    }

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    private delegate int MapInsertDelegate(IntPtr @this, IntPtr key, IntPtr value, out int replaced);

    public void Dispose() => _pNotifier2.Dispose();
}
