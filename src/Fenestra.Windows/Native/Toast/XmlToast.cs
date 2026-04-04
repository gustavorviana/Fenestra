using Fenestra.Core;
using Fenestra.Windows.Models;
using Fenestra.Windows.Services;
using System.Runtime.InteropServices;

namespace Fenestra.Windows.Native.Toast;

internal class XmlToast : FenestraComponent
{
    private readonly ComPointerHandle _handle;
    public ToastContent Content { get; }

    public XmlToast(ToastContent toast)
    {
        _handle = WinRtToastInterop.ActivateInstance("Windows.Data.Xml.Dom.XmlDocument")
            ?? throw new InvalidOperationException("RoActivateInstance failed for XmlDocument.");

        using var pIO = _handle.QueryInterface(ToastInteropConstants.IID_IXmlDocumentIO)
            ?? throw new InvalidOperationException("QI for IXmlDocumentIO failed.");

        Content = toast;

        var xml = ToastXmlBuilder.Build(toast, toast.ProgressTracker != null);
        var hr = WinRtToastInterop.CallSetHString(pIO, ToastInteropConstants.Slot_XmlDocIO_LoadXml, xml);
        if (hr < 0) throw new COMException($"LoadXml failed. HRESULT=0x{hr:X8}", hr);
    }

    public InternalNotificationHandle CreateNotification(NativeToastNotifier notifier)
    {
        return new InternalNotificationHandle(notifier, Content, CreateNotificationSafeHandle());
    }

    public ComPointerHandle CreateNotificationSafeHandle()
    {
        using var pFactory = WinRtToastInterop.GetActivationFactory(
            "Windows.UI.Notifications.ToastNotification", ToastInteropConstants.IID_IToastNotificationFactory)
            ?? throw new InvalidOperationException("Failed to get ToastNotification factory.");

        var fn = ComFactory.GetDelegate<CreateNotifDelegate>(
            WinRtToastInterop.GetVtableSlot(pFactory, ToastInteropConstants.Slot_Factory_CreateToastNotification));
        var hr = fn(pFactory, _handle, out var pNotif);
        if (hr < 0) throw new COMException($"CreateToastNotification failed. HRESULT=0x{hr:X8}", hr);
        if (pNotif == null || pNotif.IsInvalid) throw new InvalidOperationException("CreateToastNotification returned null.");

        return pNotif;
    }

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    private delegate int CreateNotifDelegate(ComPointerHandle @this, ComPointerHandle content, out ComPointerHandle notification);
}
