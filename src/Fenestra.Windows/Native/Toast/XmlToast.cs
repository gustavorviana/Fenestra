using Fenestra.Core;
using Fenestra.Windows.Models;
using Fenestra.Windows.Services;
using System.Runtime.InteropServices;

namespace Fenestra.Windows.Native.Toast;

internal class XmlToast : FenestraComponent
{
    private readonly object _xmlDoc;
    public ToastContent Content { get; }

    public XmlToast(ToastContent toast)
    {
        _xmlDoc = WinRtToastInterop.ActivateInstanceAs<IXmlDocumentIO>("Windows.Data.Xml.Dom.XmlDocument")
            ?? throw new InvalidOperationException("RoActivateInstance failed for XmlDocument.");

        Content = toast;

        var xml = ToastXmlBuilder.Build(toast, toast.ProgressTracker != null);
        using var hXml = HStringHandle.Create(xml);
        var hr = ((IXmlDocumentIO)_xmlDoc).LoadXml(hXml.DangerousGetHandle());
        if (hr < 0) throw new COMException($"LoadXml failed. HRESULT=0x{hr:X8}", hr);
    }

    public InternalNotificationHandle CreateNotification(NativeToastNotifier notifier)
    {
        return new InternalNotificationHandle(notifier, Content, CreateNotificationSafeHandle());
    }

    public ComPointerHandle CreateNotificationSafeHandle()
    {
        var factory = WinRtToastInterop.GetActivationFactoryAs<IToastNotificationFactory>(
            "Windows.UI.Notifications.ToastNotification", ToastInteropConstants.IID_IToastNotificationFactory)
            ?? throw new InvalidOperationException("Failed to get ToastNotification factory.");

        try
        {
            var pContent = Marshal.GetIUnknownForObject(_xmlDoc);
            try
            {
                var hr = factory.CreateToastNotification(pContent, out var pNotif);
                if (hr < 0) throw new COMException($"CreateToastNotification failed. HRESULT=0x{hr:X8}", hr);
                if (pNotif == IntPtr.Zero) throw new InvalidOperationException("CreateToastNotification returned null.");
                return new ComPointerHandle(pNotif);
            }
            finally { Marshal.Release(pContent); }
        }
        finally { Marshal.ReleaseComObject(factory); }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
            Marshal.ReleaseComObject(_xmlDoc);
    }
}
