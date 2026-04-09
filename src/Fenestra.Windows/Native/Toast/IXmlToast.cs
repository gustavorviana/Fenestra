using Fenestra.Windows.Models;
using Fenestra.Windows.Services;

namespace Fenestra.Windows.Native.Toast;

/// <summary>
/// Abstraction over <see cref="XmlToast"/> to enable mocking in tests.
/// </summary>
internal interface IXmlToast : IDisposable
{
    IXmlDocumentIO XmlDocument { get; }
    ToastContent Content { get; }
    InternalNotificationHandle CreateNotification(INativeToastNotifier notifier);
    IToastNotification CreateNotificationRcw();
}

/// <summary>
/// Factory indirection so consumers (ToastService, InternalNotificationHandle)
/// can inject a mock in tests instead of constructing <see cref="XmlToast"/> directly
/// (which triggers real WinRT activation in the ctor).
/// </summary>
internal interface IXmlToastFactory
{
    IXmlToast Create(ToastContent content, IWinRtInterop interop);
}

/// <summary>
/// Default factory — instantiates the real <see cref="XmlToast"/>.
/// </summary>
internal sealed class DefaultXmlToastFactory : IXmlToastFactory
{
    public static readonly DefaultXmlToastFactory Instance = new();

    public IXmlToast Create(ToastContent content, IWinRtInterop interop)
        => new XmlToast(content, interop);
}
