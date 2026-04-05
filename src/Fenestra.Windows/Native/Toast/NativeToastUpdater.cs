using System.Runtime.InteropServices;
using static Fenestra.Windows.Native.Toast.ToastInteropConstants;

namespace Fenestra.Windows.Native.Toast;

/// <summary>
/// Responsible for updating toast notification data bindings (progress bars).
/// Does not own the IToastNotifier2 RCW — the parent <see cref="NativeToastNotifier"/> manages its lifetime.
/// </summary>
internal sealed class NativeToastUpdater
{
    private readonly IToastNotifier2 _notifier2;

    private NativeToastUpdater(IToastNotifier2 notifier2)
    {
        _notifier2 = notifier2;
    }

    /// <summary>
    /// Creates an updater if the notifier supports IToastNotifier2. Returns null otherwise.
    /// </summary>
    public static NativeToastUpdater? TryCreate(IToastNotifier notifier)
    {
        try
        {
            var notifier2 = (IToastNotifier2)notifier;
            return new NativeToastUpdater(notifier2);
        }
        catch (InvalidCastException) { return null; }
    }

    public int Update(string tag, string? group, Dictionary<string, string> data, uint sequenceNumber)
    {
        using var pData = CreateNotificationData(data, sequenceNumber);
        if (pData == null) return NotificationUpdateResult_Failed;

        using var hTag = HStringHandle.Create(tag);

        if (group != null)
        {
            using var hGroup = HStringHandle.Create(group);
            _notifier2.UpdateWithTagAndGroup(
                pData.DangerousGetHandle(), hTag.DangerousGetHandle(), hGroup.DangerousGetHandle(), out var result);
            return result;
        }

        _notifier2.UpdateWithTag(pData.DangerousGetHandle(), hTag.DangerousGetHandle(), out var tagResult);
        return tagResult;
    }

    private static ComPointerHandle? CreateNotificationData(Dictionary<string, string> data, uint sequenceNumber)
    {
        var notifData = WinRtToastInterop.ActivateInstanceAs<INotificationData>("Windows.UI.Notifications.NotificationData");
        if (notifData == null) return null;

        try
        {
            notifData.put_SequenceNumber(sequenceNumber);

            notifData.get_Values(out var pMap);
            if (pMap != IntPtr.Zero)
            {
                var map = WinRtToastInterop.CastComPointer<IMapStringString>(pMap);
                if (map != null)
                {
                    try
                    {
                        foreach (var kv in data)
                        {
                            using var hKey = HStringHandle.Create(kv.Key);
                            using var hVal = HStringHandle.Create(kv.Value);
                            map.Insert(hKey.DangerousGetHandle(), hVal.DangerousGetHandle(), out _);
                        }
                    }
                    finally { Marshal.ReleaseComObject(map); }
                }
            }

            var ptr = Marshal.GetIUnknownForObject(notifData);
            return new ComPointerHandle(ptr);
        }
        finally { Marshal.ReleaseComObject(notifData); }
    }
}
