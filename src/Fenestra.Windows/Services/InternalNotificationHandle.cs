using Fenestra.Core;
using Fenestra.Windows.Models;
using Fenestra.Windows.Native;
using Fenestra.Windows.Native.Toast;
using System.Runtime.InteropServices;

namespace Fenestra.Windows.Services;

internal class InternalNotificationHandle : FenestraComponent
{
    private readonly IWinRtInterop _interop;
    private readonly IXmlToastFactory _xmlToastFactory;
    private readonly ITypedEventHandlerFactory _eventHandlerFactory;
    private readonly Func<IToastNotification, IComRef<IToastNotification>> _wrapNotification;
    private IComRef<IToastNotification> _notification;
    private IntPtr _activatedHandler;
    private IntPtr _dismissedHandler;
    private IntPtr _failedHandler;

    public INativeToastNotifier Notifier { get; }
    public string? Group { get; private set; }
    public string? Tag { get; private set; }
    public bool SuppressPopup { get; private set; }
    public ToastPriority Priority { get; private set; }
    public bool ExpiresOnReboot { get; private set; }
    public DateTimeOffset? ExpirationTime { get; private set; }
    public NotificationMirroring NotificationMirroring { get; private set; }
    public string? RemoteId { get; private set; }

    public Action<ToastActivatedArgs>? OnActivated { get; set; }
    public Action<ToastDismissalReason>? OnDismissed { get; set; }
    public Action<int>? OnFailed { get; set; }

    public InternalNotificationHandle(
        INativeToastNotifier notifier,
        ToastContent content,
        IToastNotification notification,
        IWinRtInterop interop,
        IXmlToastFactory? xmlToastFactory = null,
        ITypedEventHandlerFactory? eventHandlerFactory = null,
        Func<IToastNotification, IComRef<IToastNotification>>? wrapNotification = null)
    {
        _interop = interop;
        _xmlToastFactory = xmlToastFactory ?? DefaultXmlToastFactory.Instance;
        _eventHandlerFactory = eventHandlerFactory ?? DefaultTypedEventHandlerFactory.Instance;
        _wrapNotification = wrapNotification ?? (n => new ComRef<IToastNotification>(n));
        Notifier = notifier;
        _notification = _wrapNotification(notification);
        CaptureAndApplyProperties(content);
    }

    public void Show(ToastProgressTracker? tracker)
    {
        Notifier.Show(_notification.Value);
        RegisterEvents();

        if (tracker == null)
            return;

        tracker.Bind(data => Update(data));

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

    public NotificationUpdateResult Update(Dictionary<string, string> data)
    {
        if (Notifier == null || Tag == null) return NotificationUpdateResult.Failed;
        try { return Notifier.Update(Tag!, Group, data, 0); }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[Fenestra.Toast] {ex.Message}");
            return NotificationUpdateResult.Failed;
        }
    }

    public void HideNotification()
    {
        try { Notifier.Hide(_notification.Value); }
        catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"[Fenestra.Toast] {ex.Message}"); }
    }

    public void ReplaceInternal(ToastContent toast)
    {
        ReleaseEventHandlers();
        _notification.Dispose();

        try
        {
            using var pXmlDoc = _xmlToastFactory.Create(toast, _interop);
            _notification = _wrapNotification(pXmlDoc.CreateNotificationRcw());

            CaptureAndApplyProperties(toast);
            Show(toast.ProgressTracker);
        }
        catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"[Fenestra.Toast] {ex.Message}"); }
    }

    public void RemoveInternal(string tag, string? group)
    {
        try
        {
            if (group != null) Notifier.History?.RemoveGroupedTag(tag, group);
            else Notifier.History?.Remove(tag);
        }
        catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"[Fenestra.Toast] {ex.Message}"); }
    }

    public void RemoveGroupInternal(string group)
    {
        try { Notifier.History?.RemoveGroup(group); }
        catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"[Fenestra.Toast] {ex.Message}"); }
    }

    // --- COM event registration ---

    private void RegisterEvents()
    {
        ReleaseEventHandlers();

        _activatedHandler = _eventHandlerFactory.Create(
            ToastInteropConstants.IID_TypedEventHandler_Activated,
            (_, args) => HandleActivated(args));

        _dismissedHandler = _eventHandlerFactory.Create(
            ToastInteropConstants.IID_TypedEventHandler_Dismissed,
            (_, args) => HandleDismissed(args));

        _failedHandler = _eventHandlerFactory.Create(
            ToastInteropConstants.IID_TypedEventHandler_Failed,
            (_, args) => HandleFailed(args));

        long activatedToken = 0, dismissedToken = 0, failedToken = 0;
        try
        {
            _notification.Value.add_Activated(_activatedHandler, out activatedToken);
            _notification.Value.add_Dismissed(_dismissedHandler, out dismissedToken);
            _notification.Value.add_Failed(_failedHandler, out failedToken);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[Fenestra.Toast] Event registration failed: {ex.Message}");

            // Unregister any handlers that were already registered before releasing them
            if (activatedToken != 0) _notification.Value.remove_Activated(activatedToken);
            if (dismissedToken != 0) _notification.Value.remove_Dismissed(dismissedToken);
            if (failedToken != 0) _notification.Value.remove_Failed(failedToken);

            ReleaseEventHandlers();
        }
    }

    private void ReleaseEventHandlers()
    {
        ReleaseHandler(ref _activatedHandler);
        ReleaseHandler(ref _dismissedHandler);
        ReleaseHandler(ref _failedHandler);
    }

    private void ReleaseHandler(ref IntPtr handler)
    {
        if (handler == IntPtr.Zero) return;
        _eventHandlerFactory.Release(handler);
        handler = IntPtr.Zero;
    }

    // --- Event arg parsing ---

    private void HandleActivated(IntPtr pArgs)
    {
        var arguments = "";
        var userInput = new Dictionary<string, string>();

        if (pArgs != IntPtr.Zero)
        {
            using var args = _interop.BorrowPointer<IToastActivatedEventArgs>(pArgs);
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
            using var args = _interop.BorrowPointer<IToastDismissedEventArgs>(pArgs);
            if (args != null) args.Value.get_Reason(out reason);
        }

        OnDismissed?.Invoke((ToastDismissalReason)reason);
    }

    private void HandleFailed(IntPtr pArgs)
    {
        var errorCode = 0;
        if (pArgs != IntPtr.Zero)
        {
            using var args = _interop.BorrowPointer<IToastFailedEventArgs>(pArgs);
            if (args != null) args.Value.get_ErrorCode(out errorCode);
        }

        OnFailed?.Invoke(errorCode);
    }

    // --- UserInput parsing (IToastActivatedEventArgs2 → IPropertySet → iterate) ---

    private void ReadUserInput(IntPtr pArgs, Dictionary<string, string> result)
    {
        using var args2 = _interop.BorrowPointer<IToastActivatedEventArgs2>(pArgs);
        if (args2 == null) return;

        if (args2.Value.get_UserInput(out var pPropSet) != 0 || pPropSet == IntPtr.Zero) return;

        using var iterable = _interop.CastPointer<IIterableKvpStringObject>(pPropSet);
        if (iterable == null) return;

        if (iterable.Value.First(out var pIter) != 0 || pIter == IntPtr.Zero) return;

        using var iterator = _interop.CastPointer<IIteratorKvpStringObject>(pIter);
        if (iterator == null) return;

        IterateUserInputPairs(iterator.Value, result);
    }

    private void IterateUserInputPairs(IIteratorKvpStringObject iterator, Dictionary<string, string> result)
    {
        while (true)
        {
            iterator.get_HasCurrent(out var hasCurrent);
            if (hasCurrent == 0) break;

            if (iterator.get_Current(out var pKvp) == 0 && pKvp != IntPtr.Zero)
            {
                using var kvp = _interop.CastPointer<IKeyValuePairStringObject>(pKvp);
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

    private string ReadObjectValueAsString(IKeyValuePairStringObject kvp)
    {
        if (kvp.get_Value(out var pValue) != 0 || pValue == IntPtr.Zero)
            return "";

        using var propVal = _interop.CastPointer<IPropertyValue>(pValue);
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
        NotificationMirroring = content.NotificationMirroring;
        RemoteId = content.RemoteId;

        if (_notification.Value is IToastNotification2 notif2)
        {
            if (!string.IsNullOrEmpty(content.Tag))
                notif2.put_Tag(content.Tag!);

            if (!string.IsNullOrEmpty(content.Group))
                notif2.put_Group(content.Group!);

            if (content.SuppressPopup)
                notif2.put_SuppressPopup(1);
        }

        if (content.Priority != ToastPriority.Default && _notification.Value is IToastNotification4 notif4)
            notif4.put_Priority((int)content.Priority);

        if (content.ExpiresOnReboot && _notification.Value is IToastNotification6 notif6)
            notif6.put_ExpiresOnReboot(1);

        if (content.ExpirationTime.HasValue)
            Notifier.SetExpirationTime(_notification.Value, content.ExpirationTime.Value);

        if (_notification.Value is IToastNotification3 notif3)
        {
            if (content.NotificationMirroring != NotificationMirroring.Allowed)
                notif3.put_NotificationMirroring((int)content.NotificationMirroring);

            if (!string.IsNullOrEmpty(content.RemoteId))
                notif3.put_RemoteId(content.RemoteId!);
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            ReleaseEventHandlers();
            _notification.Dispose();
        }
    }
}
