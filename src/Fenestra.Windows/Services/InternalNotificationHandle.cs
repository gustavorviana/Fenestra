using Fenestra.Core;
using Fenestra.Windows.Models;
using Fenestra.Windows.Native;
using Fenestra.Windows.Native.Toast;
using System.Runtime.InteropServices;

namespace Fenestra.Windows.Services;

internal class InternalNotificationHandle : FenestraComponent
{
    internal NativeToastHistory ToastHistory { get; }
    private IToastNotification _notification;
    private IntPtr _activatedHandler;
    private IntPtr _dismissedHandler;
    private IntPtr _failedHandler;

    public NativeToastNotifier Notifier { get; }
    public string? Group { get; private set; }
    public string? Tag { get; private set; }
    public bool SuppressPopup { get; private set; }
    public ToastPriority Priority { get; private set; }
    public bool ExpiresOnReboot { get; private set; }
    public DateTimeOffset? ExpirationTime { get; private set; }

    public Action<ToastActivatedArgs>? OnActivated { get; set; }
    public Action<ToastDismissalReason>? OnDismissed { get; set; }
    public Action<int>? OnFailed { get; set; }

    public InternalNotificationHandle(NativeToastNotifier notifier, ToastContent content, IToastNotification notification)
    {
        Notifier = notifier;
        _notification = notification;
        ToastHistory = new NativeToastHistory();

        CaptureAndApplyProperties(content);
    }

    public void Show(ToastProgressTracker? tracker)
    {
        Notifier.Show(_notification);
        RegisterEvents();

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

    public void HideNotification()
    {
        try { Notifier.Hide(_notification); }
        catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"[Fenestra.Toast] {ex.Message}"); }
    }

    public void ReplaceInternal(ToastContent toast)
    {
        ReleaseEventHandlers();
        Marshal.ReleaseComObject(_notification);

        try
        {
            using var pXmlDoc = new XmlToast(toast);
            _notification = pXmlDoc.CreateNotificationRcw();

            CaptureAndApplyProperties(toast);
            Show(toast.ProgressTracker);
        }
        catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"[Fenestra.Toast] {ex.Message}"); }
    }

    public void RemoveInternal(string tag, string? group)
    {
        try
        {
            if (group != null) ToastHistory.RemoveGrouped(tag, group);
            else ToastHistory.Remove(tag);
        }
        catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"[Fenestra.Toast] {ex.Message}"); }
    }

    public void RemoveGroupInternal(string group)
    {
        try { ToastHistory.RemoveGroup(group); }
        catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"[Fenestra.Toast] {ex.Message}"); }
    }

    // --- COM event registration ---

    private void RegisterEvents()
    {
        ReleaseEventHandlers();

        _activatedHandler = TypedEventHandlerFactory.Create(
            ToastInteropConstants.IID_TypedEventHandler_Activated,
            (_, args) => HandleActivated(args));

        _dismissedHandler = TypedEventHandlerFactory.Create(
            ToastInteropConstants.IID_TypedEventHandler_Dismissed,
            (_, args) => HandleDismissed(args));

        _failedHandler = TypedEventHandlerFactory.Create(
            ToastInteropConstants.IID_TypedEventHandler_Failed,
            (_, args) => HandleFailed(args));

        try
        {
            _notification.add_Activated(_activatedHandler, out _);
            _notification.add_Dismissed(_dismissedHandler, out _);
            _notification.add_Failed(_failedHandler, out _);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[Fenestra.Toast] Event registration failed: {ex.Message}");
            ReleaseEventHandlers();
        }
    }

    private void ReleaseEventHandlers()
    {
        ReleaseHandler(ref _activatedHandler);
        ReleaseHandler(ref _dismissedHandler);
        ReleaseHandler(ref _failedHandler);
    }

    private static void ReleaseHandler(ref IntPtr handler)
    {
        if (handler == IntPtr.Zero) return;
        TypedEventHandlerFactory.Release(handler);
        handler = IntPtr.Zero;
    }

    // --- Event arg parsing ---

    private void HandleActivated(IntPtr pArgs)
    {
        var arguments = "";
        var userInput = new Dictionary<string, string>();

        if (pArgs != IntPtr.Zero)
        {
            using var args = WinRtToastInterop.BorrowPointer<IToastActivatedEventArgs>(pArgs);
            if (args != null && args.Value.get_Arguments(out var value) == 0)
                arguments = value ?? "";

            ReadUserInput(pArgs, userInput);
        }

        OnActivated?.Invoke(new ToastActivatedArgs(arguments, userInput));
    }

    private void HandleDismissed(IntPtr pArgs)
    {
        var reason = 0;
        if (pArgs != IntPtr.Zero)
        {
            using var args = WinRtToastInterop.BorrowPointer<IToastDismissedEventArgs>(pArgs);
            if (args != null) args.Value.get_Reason(out reason);
        }

        OnDismissed?.Invoke((ToastDismissalReason)reason);
    }

    private void HandleFailed(IntPtr pArgs)
    {
        var errorCode = 0;
        if (pArgs != IntPtr.Zero)
        {
            using var args = WinRtToastInterop.BorrowPointer<IToastFailedEventArgs>(pArgs);
            if (args != null) args.Value.get_ErrorCode(out errorCode);
        }

        OnFailed?.Invoke(errorCode);
    }

    // --- UserInput parsing (IToastActivatedEventArgs2 → IPropertySet → iterate) ---

    private static void ReadUserInput(IntPtr pArgs, Dictionary<string, string> result)
    {
        using var args2 = WinRtToastInterop.BorrowPointer<IToastActivatedEventArgs2>(pArgs);
        if (args2 == null) return;

        if (args2.Value.get_UserInput(out var pPropSet) != 0 || pPropSet == IntPtr.Zero) return;

        using var iterable = WinRtToastInterop.CastPointer<IIterableKvpStringObject>(pPropSet);
        if (iterable == null) return;

        if (iterable.Value.First(out var pIter) != 0 || pIter == IntPtr.Zero) return;

        using var iterator = WinRtToastInterop.CastPointer<IIteratorKvpStringObject>(pIter);
        if (iterator == null) return;

        IterateUserInputPairs(iterator.Value, result);
    }

    private static void IterateUserInputPairs(IIteratorKvpStringObject iterator, Dictionary<string, string> result)
    {
        while (true)
        {
            iterator.get_HasCurrent(out var hasCurrent);
            if (hasCurrent == 0) break;

            if (iterator.get_Current(out var pKvp) == 0 && pKvp != IntPtr.Zero)
            {
                using var kvp = WinRtToastInterop.CastPointer<IKeyValuePairStringObject>(pKvp);
                if (kvp != null)
                {
                    kvp.Value.get_Key(out var key);
                    var value = ReadObjectValueAsString(kvp.Value);
                    if (!string.IsNullOrEmpty(key))
                        result[key!] = value;
                }
            }

            iterator.MoveNext(out var moved);
            if (moved == 0) break;
        }
    }

    private static string ReadObjectValueAsString(IKeyValuePairStringObject kvp)
    {
        if (kvp.get_Value(out var pValue) != 0 || pValue == IntPtr.Zero)
            return "";

        using var propVal = WinRtToastInterop.CastPointer<IPropertyValue>(pValue);
        if (propVal == null) return "";

        return propVal.Value.GetString(out var value) == 0 ? value ?? "" : "";
    }

    // --- Properties ---

    private void CaptureAndApplyProperties(ToastContent content)
    {
        Tag = content.Tag;
        Group = content.Group;
        SuppressPopup = content.SuppressPopup;
        Priority = content.Priority;
        ExpiresOnReboot = content.ExpiresOnReboot;
        ExpirationTime = content.ExpirationTime;

        if (_notification is IToastNotification2 notif2)
        {
            if (!string.IsNullOrEmpty(content.Tag))
                notif2.put_Tag(content.Tag!);

            if (!string.IsNullOrEmpty(content.Group))
                notif2.put_Group(content.Group!);

            if (content.SuppressPopup)
                notif2.put_SuppressPopup(1);
        }

        if (content.Priority != ToastPriority.Default && _notification is IToastNotification4 notif4)
            notif4.put_Priority((int)content.Priority);

        if (content.ExpiresOnReboot && _notification is IToastNotification6 notif6)
            notif6.put_ExpiresOnReboot(1);

        if (content.ExpirationTime.HasValue)
            Notifier.SetExpirationTime(_notification, content.ExpirationTime.Value);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            ReleaseEventHandlers();
            Marshal.ReleaseComObject(_notification);
        }
    }
}
