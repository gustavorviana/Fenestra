using Fenestra.Core;
using Fenestra.Windows.Models;
using Fenestra.Windows.Native;
using Fenestra.Windows.Native.Toast;
using System.Runtime.InteropServices;

namespace Fenestra.Windows.Services;

internal class InternalNotificationHandle : FenestraComponent
{
    private ComPointerHandle _handle;
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

    public InternalNotificationHandle(NativeToastNotifier notifier, ToastContent content, ComPointerHandle handle)
    {
        Notifier = notifier;
        _handle = handle;
        CaptureAndApplyProperties(content);
    }

    public void Show(ToastProgressTracker? tracker)
    {
        Notifier.Show(_handle);
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
        try { Notifier.Hide(_handle); }
        catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"[Fenestra.Toast] {ex.Message}"); }
    }

    public void ReplaceInternal(ToastContent toast)
    {
        ReleaseEventHandlers();
        _handle.Dispose();

        try
        {
            using var pXmlDoc = new XmlToast(toast);
            _handle = pXmlDoc.CreateNotificationSafeHandle();

            CaptureAndApplyProperties(toast);
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

    public void RemoveGroupInternal(string group)
    {
        try { Notifier.HistoryRemoveGroup(group); }
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
            Notifier.AddActivatedHandler(_handle, _activatedHandler);
            Notifier.AddDismissedHandler(_handle, _dismissedHandler);
            Notifier.AddFailedHandler(_handle, _failedHandler);
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
            var args = WinRtToastInterop.BorrowComPointer<IToastActivatedEventArgs>(pArgs);
            if (args != null)
            {
                try
                {
                    if (args.get_Arguments(out var hString) == 0 && hString != IntPtr.Zero)
                    {
                        using var h = new HStringHandle(hString);
                        arguments = h.ToString();
                    }
                }
                finally { Marshal.ReleaseComObject(args); }
            }

            ReadUserInput(pArgs, userInput);
        }

        OnActivated?.Invoke(new ToastActivatedArgs(arguments, userInput));
    }

    private void HandleDismissed(IntPtr pArgs)
    {
        var reason = 0;
        if (pArgs != IntPtr.Zero)
        {
            var args = WinRtToastInterop.BorrowComPointer<IToastDismissedEventArgs>(pArgs);
            if (args != null)
            {
                try { args.get_Reason(out reason); }
                finally { Marshal.ReleaseComObject(args); }
            }
        }

        OnDismissed?.Invoke((ToastDismissalReason)reason);
    }

    private void HandleFailed(IntPtr pArgs)
    {
        var errorCode = 0;
        if (pArgs != IntPtr.Zero)
        {
            var args = WinRtToastInterop.BorrowComPointer<IToastFailedEventArgs>(pArgs);
            if (args != null)
            {
                try { args.get_ErrorCode(out errorCode); }
                finally { Marshal.ReleaseComObject(args); }
            }
        }

        OnFailed?.Invoke(errorCode);
    }

    // --- UserInput parsing (IToastActivatedEventArgs2 → IPropertySet → iterate) ---

    private static void ReadUserInput(IntPtr pArgs, Dictionary<string, string> result)
    {
        var args2 = WinRtToastInterop.BorrowComPointer<IToastActivatedEventArgs2>(pArgs);
        if (args2 == null) return;

        try
        {
            if (args2.get_UserInput(out var pPropSet) != 0 || pPropSet == IntPtr.Zero)
                return;

            var iterable = WinRtToastInterop.CastComPointer<IIterableKvpStringObject>(pPropSet);
            if (iterable == null) return;

            try
            {
                if (iterable.First(out var pIter) != 0 || pIter == IntPtr.Zero) return;

                var iterator = WinRtToastInterop.CastComPointer<IIteratorKvpStringObject>(pIter);
                if (iterator == null) return;

                try { IterateUserInputPairs(iterator, result); }
                finally { Marshal.ReleaseComObject(iterator); }
            }
            finally { Marshal.ReleaseComObject(iterable); }
        }
        finally { Marshal.ReleaseComObject(args2); }
    }

    private static void IterateUserInputPairs(IIteratorKvpStringObject iterator, Dictionary<string, string> result)
    {
        while (true)
        {
            iterator.get_HasCurrent(out var hasCurrent);
            if (hasCurrent == 0) break;

            if (iterator.get_Current(out var pKvp) == 0 && pKvp != IntPtr.Zero)
            {
                var kvp = WinRtToastInterop.CastComPointer<IKeyValuePairStringObject>(pKvp);
                if (kvp != null)
                {
                    try
                    {
                        var key = ReadHString(kvp.get_Key);
                        var value = ReadObjectValueAsString(kvp);
                        if (!string.IsNullOrEmpty(key))
                            result[key] = value;
                    }
                    finally { Marshal.ReleaseComObject(kvp); }
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

        var propVal = WinRtToastInterop.CastComPointer<IPropertyValue>(pValue);
        if (propVal == null) return "";

        try { return ReadHString(propVal.GetString); }
        finally { Marshal.ReleaseComObject(propVal); }
    }

    private delegate int HStringGetter(out IntPtr result);

    private static string ReadHString(HStringGetter getter)
    {
        var hr = getter(out var hstringPtr);
        if (hr != 0 || hstringPtr == IntPtr.Zero) return "";
        using var hstring = new HStringHandle(hstringPtr);
        return hstring.ToString();
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

        if (content.ExpirationTime.HasValue)
            Notifier.SetExpirationTime(_handle, content.ExpirationTime.Value);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            ReleaseEventHandlers();
            _handle.Dispose();
        }
    }
}
