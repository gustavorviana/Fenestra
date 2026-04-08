using Fenestra.Core;
using Fenestra.Windows.Models;
using Fenestra.Windows.Services;
using System.Runtime.InteropServices;

namespace Fenestra.Windows.Native.Toast;

internal class XmlToast : FenestraComponent
{
    private readonly ComRef<IXmlDocumentIO> _xmlDoc;
    internal IXmlDocumentIO XmlDocument => _xmlDoc.Value;
    public ToastContent Content { get; }

    public XmlToast(ToastContent toast)
    {
        _xmlDoc = WinRtToastInterop.ActivateInstance<IXmlDocumentIO>("Windows.Data.Xml.Dom.XmlDocument")
            ?? throw new InvalidOperationException("RoActivateInstance failed for XmlDocument.");

        Content = toast;

        var xml = ToastXmlBuilder.Build(toast, toast.ProgressTracker != null);
        var hr = _xmlDoc.Value.LoadXml(xml);
        if (hr < 0) throw new COMException($"LoadXml failed. HRESULT=0x{hr:X8}", hr);
    }

    public InternalNotificationHandle CreateNotification(NativeToastNotifier notifier)
    {
        return new InternalNotificationHandle(notifier, Content, CreateNotificationRcw());
    }

    public IToastNotification CreateNotificationRcw()
    {
        using var factory = WinRtToastInterop.GetActivationFactory<IToastNotificationFactory>(
            "Windows.UI.Notifications.ToastNotification", ToastInteropConstants.IID_IToastNotificationFactory)
            ?? throw new InvalidOperationException("Failed to get ToastNotification factory.");

        var hr = factory.Value.CreateToastNotification(_xmlDoc.Value, out var pNotif);
        if (hr < 0) throw new COMException($"CreateToastNotification failed. HRESULT=0x{hr:X8}", hr);
        if (pNotif == IntPtr.Zero) throw new InvalidOperationException("CreateToastNotification returned null.");

        return WinRtToastInterop.CastPointer<IToastNotification>(pNotif)?.Value
            ?? throw new InvalidOperationException("Failed to wrap IToastNotification.");
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
            _xmlDoc.Dispose();
    }
}
