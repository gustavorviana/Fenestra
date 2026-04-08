using Fenestra.Core;
using System.Runtime.InteropServices;

namespace Fenestra.Windows.Native.Toast;

/// <summary>
/// Responsible for managing toast notification history in Action Center (remove, clear).
/// </summary>
internal class NativeToastHistory : FenestraComponent
{
    private readonly IToastNotificationHistory? _history;

    public NativeToastHistory()
    {
        using var manager2 = WinRtToastInterop.GetActivationFactory<IToastNotificationManagerStatics2>(
            "Windows.UI.Notifications.ToastNotificationManager", ToastInteropConstants.IID_IToastNotificationManagerStatics2);
        if (manager2 == null) return;

        var hr = manager2.Value.get_History(out var pHistory);
        if (hr != 0 || pHistory == IntPtr.Zero) return;
        _history = WinRtToastInterop.CastPointer<IToastNotificationHistory>(pHistory)?.Value;
    }

    public void Remove(string tag)
    {
        if (_history == null) return;
        _history.Remove(tag);
    }

    public void RemoveGrouped(string tag, string group)
    {
        if (_history == null) return;
        _history.RemoveGroupedTag(tag, group);
    }

    public void RemoveGroup(string group)
    {
        if (_history == null) return;
        _history.RemoveGroup(group);
    }

    public void Clear()
    {
        if (_history == null) return;
        _history.Clear();
    }

    public void ClearWithId(string appId)
    {
        if (_history == null) return;
        _history.ClearWithId(appId);
    }

    public void RemoveGroupWithId(string group, string appId)
    {
        if (_history == null) return;
        _history.RemoveGroupWithId(group, appId);
    }

    public void RemoveGroupedTagWithId(string tag, string group, string appId)
    {
        if (_history == null) return;
        _history.RemoveGroupedTagWithId(tag, group, appId);
    }

    /// <summary>
    /// Retrieves the list of toast notifications in Action Center.
    /// Returns the raw IVectorView COM pointer. Returns <see cref="IntPtr.Zero"/> if unsupported.
    /// </summary>
    public IntPtr GetHistory()
    {
        if (_history is not IToastNotificationHistory2 history2) return IntPtr.Zero;
        return history2.GetHistory(out var result) == 0 ? result : IntPtr.Zero;
    }

    /// <summary>
    /// Retrieves the list of toast notifications in Action Center for a specific app.
    /// Returns the raw IVectorView COM pointer. Returns <see cref="IntPtr.Zero"/> if unsupported.
    /// </summary>
    public IntPtr GetHistoryWithId(string appId)
    {
        if (_history is not IToastNotificationHistory2 history2) return IntPtr.Zero;
        return history2.GetHistoryWithId(appId, out var result) == 0 ? result : IntPtr.Zero;
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (_history != null)
            Marshal.ReleaseComObject(_history);
    }
}
