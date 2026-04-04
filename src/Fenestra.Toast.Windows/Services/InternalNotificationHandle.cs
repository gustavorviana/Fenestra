using Fenestra.Core;
using Fenestra.Core.Models;
using Fenestra.Toast.Windows.Native;
using Fenestra.Toast.Windows.Native.Toast;

namespace Fenestra.Toast.Windows.Services;

internal class InternalNotificationHandle : FenestraComponent
{
    private ComPointerHandle _handle;

    public NativeToastNotifier Notifier { get; }
    public string? Group { get; private set; }
    public string? Tag { get; private set; }

    public InternalNotificationHandle(NativeToastNotifier notifier, ToastContent content, ComPointerHandle handle)
    {
        Notifier = notifier;
        _handle = handle;

        Group = content.Group;
        Tag = content.Tag;

        if (!string.IsNullOrEmpty(content.Tag))
            Notifier.SetTag(_handle, content.Tag!);

        if (!string.IsNullOrEmpty(content.Group))
            Notifier.SetGroup(_handle, content.Group!);

        if (content.SuppressPopup)
            Notifier.SetSuppressPopup(_handle, true);

        if (content.Priority != ToastPriority.Default)
            Notifier.SetPriority(_handle, (int)content.Priority);

        if (content.ExpiresOnReboot)
            Notifier.SetExpiresOnReboot(_handle, true);
    }

    public void Show(ToastProgressTracker? tracker)
    {
        Notifier.Show(_handle);

        if (tracker == null)
            return;

        tracker.Bind(Update);

        var initial = new Dictionary<string, string>
        {
            ["progressStatus"] = " ",
            ["progressValue"] = "0"
        };
        if (tracker.Title != null)
            initial["progressTitle"] = tracker.Title;

        if (tracker.UseValueOverride)
            initial["progressValueOverride"] = "0%";

        Update(initial);
    }

    public void Update(Dictionary<string, string> data)
    {
        if (Notifier == null || Tag == null) return;
        try { Notifier.Update(Tag!, Group, data, 0); }
        catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"[Fenestra.Toast] {ex.Message}"); }
    }


    public void ReplaceInternal(ToastContent toast)
    {
        _handle.Dispose();

        try
        {
            using var pXmlDoc = new XmlToast(toast);
            _handle = pXmlDoc.CreateNotificationSafeHandle();

            Tag = toast.Tag;
            Group = toast.Group;

            Show(toast.ProgressTracker);
        }
        catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"[Fenestra.Toast] {ex.Message}"); }
    }

    public void RemoveInternal(string tag, string? group)
    {
        try
        {
            if (group != null) Notifier.HistoryRemoveGrouped(tag, group);
            else Notifier.HistoryRemove(tag);
        }
        catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"[Fenestra.Toast] {ex.Message}"); }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
            _handle.Dispose();
    }
}
