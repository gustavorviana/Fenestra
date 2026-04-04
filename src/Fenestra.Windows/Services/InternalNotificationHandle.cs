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
            arguments = ReadHStringFromQI(pArgs,
                ToastInteropConstants.IID_IToastActivatedEventArgs,
                ToastInteropConstants.Slot_ActivatedArgs_get_Arguments);

            ReadUserInput(pArgs, userInput);
        }

        OnActivated?.Invoke(new ToastActivatedArgs(arguments, userInput));
    }

    private void HandleDismissed(IntPtr pArgs)
    {
        var reason = 0;
        if (pArgs != IntPtr.Zero)
        {
            reason = ReadIntFromQI(pArgs,
                ToastInteropConstants.IID_IToastDismissedEventArgs,
                ToastInteropConstants.Slot_DismissedArgs_get_Reason);
        }

        OnDismissed?.Invoke((ToastDismissalReason)reason);
    }

    private void HandleFailed(IntPtr pArgs)
    {
        var errorCode = 0;
        if (pArgs != IntPtr.Zero)
        {
            errorCode = ReadIntFromQI(pArgs,
                ToastInteropConstants.IID_IToastFailedEventArgs,
                ToastInteropConstants.Slot_FailedArgs_get_ErrorCode);
        }

        OnFailed?.Invoke(errorCode);
    }

    // --- UserInput parsing (IToastActivatedEventArgs2 → IPropertySet → iterate) ---

    private static void ReadUserInput(IntPtr pArgs, Dictionary<string, string> result)
    {
        // QI for IToastActivatedEventArgs2
        var iid2 = ToastInteropConstants.IID_IToastActivatedEventArgs2;
        if (Marshal.QueryInterface(pArgs, ref iid2, out var pArgs2) != 0 || pArgs2 == IntPtr.Zero)
            return;

        try
        {
            // get_UserInput(out IPropertySet*) — slot 6
            var hr = SlotCall<OutPtrFn>(pArgs2, ToastInteropConstants.Slot_ActivatedArgs2_get_UserInput)(pArgs2, out var pPropSet);
            if (hr != 0 || pPropSet == IntPtr.Zero) return;

            try
            {
                // QI for IIterable<IKeyValuePair<String, Object>>
                var iidIterable = ToastInteropConstants.IID_IIterable_KVP_String_Object;
                if (Marshal.QueryInterface(pPropSet, ref iidIterable, out var pIterable) != 0 || pIterable == IntPtr.Zero)
                    return;

                try
                {
                    // First() → IIterator*
                    hr = SlotCall<OutPtrFn>(pIterable, ToastInteropConstants.Slot_Iterable_First)(pIterable, out var pIter);
                    if (hr != 0 || pIter == IntPtr.Zero) return;

                    try { IterateUserInputPairs(pIter, result); }
                    finally { Marshal.Release(pIter); }
                }
                finally { Marshal.Release(pIterable); }
            }
            finally { Marshal.Release(pPropSet); }
        }
        finally { Marshal.Release(pArgs2); }
    }

    private static void IterateUserInputPairs(IntPtr pIter, Dictionary<string, string> result)
    {
        while (true)
        {
            // get_HasCurrent(out boolean)
            SlotCall<OutIntFn>(pIter, ToastInteropConstants.Slot_Iterator_get_HasCurrent)(pIter, out var hasCurrent);
            if (hasCurrent == 0) break;

            // get_Current(out IKeyValuePair<String, Object>*)
            if (SlotCall<OutPtrFn>(pIter, ToastInteropConstants.Slot_Iterator_get_Current)(pIter, out var pKvp) == 0 && pKvp != IntPtr.Zero)
            {
                try
                {
                    var key = ReadHStringFromSlot(pKvp, ToastInteropConstants.Slot_KVP_Object_get_Key);
                    var value = ReadObjectValueAsString(pKvp);
                    if (!string.IsNullOrEmpty(key))
                        result[key] = value;
                }
                finally { Marshal.Release(pKvp); }
            }

            // MoveNext(out boolean)
            SlotCall<OutIntFn>(pIter, ToastInteropConstants.Slot_Iterator_MoveNext)(pIter, out var moved);
            if (moved == 0) break;
        }
    }

    private static string ReadObjectValueAsString(IntPtr pKvp)
    {
        // get_Value(out IInspectable*) — slot 7
        if (SlotCall<OutPtrFn>(pKvp, ToastInteropConstants.Slot_KVP_Object_get_Value)(pKvp, out var pValue) != 0 || pValue == IntPtr.Zero)
            return "";

        try
        {
            // QI for IPropertyValue, then GetString (slot 19)
            var iidPv = ToastInteropConstants.IID_IPropertyValue;
            if (Marshal.QueryInterface(pValue, ref iidPv, out var pPropVal) != 0 || pPropVal == IntPtr.Zero)
                return "";

            try { return ReadHStringFromSlot(pPropVal, ToastInteropConstants.Slot_PV_GetString); }
            finally { Marshal.Release(pPropVal); }
        }
        finally { Marshal.Release(pValue); }
    }

    // --- Low-level COM helpers ---

    private static string ReadHStringFromQI(IntPtr pObj, Guid iid, int slot)
    {
        if (Marshal.QueryInterface(pObj, ref iid, out var p) != 0 || p == IntPtr.Zero)
            return "";
        try { return ReadHStringFromSlot(p, slot); }
        finally { Marshal.Release(p); }
    }

    private static int ReadIntFromQI(IntPtr pObj, Guid iid, int slot)
    {
        if (Marshal.QueryInterface(pObj, ref iid, out var p) != 0 || p == IntPtr.Zero)
            return 0;
        try
        {
            SlotCall<OutIntFn>(p, slot)(p, out var result);
            return result;
        }
        finally { Marshal.Release(p); }
    }

    private static string ReadHStringFromSlot(IntPtr pObj, int slot)
    {
        var hr = SlotCall<OutPtrFn>(pObj, slot)(pObj, out var hstringPtr);
        if (hr != 0 || hstringPtr == IntPtr.Zero) return "";
        using var hstring = new HStringHandle(hstringPtr);
        return hstring.ToString();
    }

    private static T SlotCall<T>(IntPtr pObj, int slot) where T : Delegate
    {
        var vtable = Marshal.ReadIntPtr(pObj);
        var fnPtr = Marshal.ReadIntPtr(vtable, slot * IntPtr.Size);
        return Marshal.GetDelegateForFunctionPointer<T>(fnPtr);
    }

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    private delegate int OutPtrFn(IntPtr @this, out IntPtr result);

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    private delegate int OutIntFn(IntPtr @this, out int result);

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
