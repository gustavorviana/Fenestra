using Fenestra.Windows.Models;
using Fenestra.Windows.Native;
using Fenestra.Windows.Native.Toast;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using System.Runtime.InteropServices;

namespace Fenestra.Windows.Tests.Native.Toast;

public class XmlToastTests
{
    private readonly IWinRtInterop _interop = Substitute.For<IWinRtInterop>();
    private readonly IXmlDocumentIO _xmlDoc = Substitute.For<IXmlDocumentIO>();

    public XmlToastTests()
    {
        // Default: ActivateInstance returns our XmlDocument mock wrapped in a no-op ComRef.
        var xmlDocRef = Substitute.For<IComRef<IXmlDocumentIO>>();
        xmlDocRef.Value.Returns(_xmlDoc);
        _interop.ActivateInstance<IXmlDocumentIO>("Windows.Data.Xml.Dom.XmlDocument").Returns(xmlDocRef);

        // Default: LoadXml succeeds
        _xmlDoc.LoadXml(Arg.Any<string>()).Returns(0);
    }

    // --- Constructor ---

    [Fact]
    public void Ctor_CallsActivateInstanceWithXmlDocumentClassName()
    {
        var content = new ToastContent { Title = "Hello" };

        using var sut = new XmlToast(content, _interop);

        _interop.Received(1).ActivateInstance<IXmlDocumentIO>("Windows.Data.Xml.Dom.XmlDocument");
    }

    [Fact]
    public void Ctor_WhenActivateInstanceReturnsNull_ThrowsInvalidOperationException()
    {
        _interop.ActivateInstance<IXmlDocumentIO>(Arg.Any<string>()).Returns((IComRef<IXmlDocumentIO>?)null);
        var content = new ToastContent();

        Assert.Throws<InvalidOperationException>(() => new XmlToast(content, _interop));
    }

    [Fact]
    public void Ctor_LoadsXmlBuiltFromContent()
    {
        var content = new ToastContent { Title = "Hello", Body = "World" };

        using var sut = new XmlToast(content, _interop);

        _xmlDoc.Received(1).LoadXml(Arg.Is<string>(xml =>
            xml.Contains("<text>Hello</text>") && xml.Contains("<text>World</text>")));
    }

    [Fact]
    public void Ctor_WhenLoadXmlReturnsFailureHResult_ThrowsComException()
    {
        _xmlDoc.LoadXml(Arg.Any<string>()).Returns(unchecked((int)0x80004005));
        var content = new ToastContent { Title = "Hello" };

        var ex = Assert.Throws<COMException>(() => new XmlToast(content, _interop));
        Assert.Contains("LoadXml failed", ex.Message);
    }

    [Fact]
    public void Ctor_StoresContentReference()
    {
        var content = new ToastContent { Title = "Hello" };

        using var sut = new XmlToast(content, _interop);

        Assert.Same(content, sut.Content);
    }

    [Fact]
    public void XmlDocument_ExposesUnderlyingComInterface()
    {
        var content = new ToastContent();

        using var sut = new XmlToast(content, _interop);

        Assert.Same(_xmlDoc, sut.XmlDocument);
    }

    // --- CreateNotificationRcw ---

    [Fact]
    public void CreateNotificationRcw_GetsToastNotificationFactory()
    {
        SetupToastNotificationFactory(out _, out _);
        using var sut = new XmlToast(new ToastContent(), _interop);

        sut.CreateNotificationRcw();

        _interop.Received(1).GetActivationFactory<IToastNotificationFactory>(
            "Windows.UI.Notifications.ToastNotification",
            ToastInteropConstants.IID_IToastNotificationFactory);
    }

    [Fact]
    public void CreateNotificationRcw_WhenFactoryReturnsNull_Throws()
    {
        _interop.GetActivationFactory<IToastNotificationFactory>(Arg.Any<string>(), Arg.Any<Guid>())
            .Returns((IComRef<IToastNotificationFactory>?)null);
        using var sut = new XmlToast(new ToastContent(), _interop);

        Assert.Throws<InvalidOperationException>(() => sut.CreateNotificationRcw());
    }

    [Fact]
    public void CreateNotificationRcw_WhenFactoryReturnsFailureHResult_ThrowsComException()
    {
        var factory = Substitute.For<IToastNotificationFactory>();
        factory.CreateToastNotification(Arg.Any<object>(), out Arg.Any<IntPtr>())
            .Returns(unchecked((int)0x80004005));
        var factoryRef = Substitute.For<IComRef<IToastNotificationFactory>>();
        factoryRef.Value.Returns(factory);
        _interop.GetActivationFactory<IToastNotificationFactory>(Arg.Any<string>(), Arg.Any<Guid>()).Returns(factoryRef);

        using var sut = new XmlToast(new ToastContent(), _interop);

        Assert.Throws<COMException>(() => sut.CreateNotificationRcw());
    }

    [Fact]
    public void CreateNotificationRcw_WhenPointerIsZero_ThrowsInvalidOperation()
    {
        var factory = Substitute.For<IToastNotificationFactory>();
        factory.CreateToastNotification(Arg.Any<object>(), out Arg.Any<IntPtr>())
            .Returns(ci =>
            {
                ci[1] = IntPtr.Zero;
                return 0;
            });
        var factoryRef = Substitute.For<IComRef<IToastNotificationFactory>>();
        factoryRef.Value.Returns(factory);
        _interop.GetActivationFactory<IToastNotificationFactory>(Arg.Any<string>(), Arg.Any<Guid>()).Returns(factoryRef);

        using var sut = new XmlToast(new ToastContent(), _interop);

        Assert.Throws<InvalidOperationException>(() => sut.CreateNotificationRcw());
    }

    [Fact]
    public void CreateNotificationRcw_WhenCastPointerReturnsNull_Throws()
    {
        SetupToastNotificationFactory(out var factory, out var fakePtr);
        _interop.CastPointer<IToastNotification>(fakePtr).Returns((IComRef<IToastNotification>?)null);

        using var sut = new XmlToast(new ToastContent(), _interop);

        Assert.Throws<InvalidOperationException>(() => sut.CreateNotificationRcw());
    }

    [Fact]
    public void CreateNotificationRcw_ReturnsCastedToastNotification()
    {
        SetupToastNotificationFactory(out _, out var fakePtr);
        var notification = Substitute.For<IToastNotification>();
        var notificationRef = Substitute.For<IComRef<IToastNotification>>();
        notificationRef.Value.Returns(notification);
        _interop.CastPointer<IToastNotification>(fakePtr).Returns(notificationRef);

        using var sut = new XmlToast(new ToastContent(), _interop);
        var result = sut.CreateNotificationRcw();

        Assert.Same(notification, result);
    }

    // --- CreateNotification ---

    [Fact]
    public void CreateNotification_BuildsInternalHandleWithNotifierAndContent()
    {
        var content = new ToastContent { Tag = "my-tag" };
        SetupToastNotificationFactory(out _, out var fakePtr);
        var notification = Substitute.For<IToastNotification>();
        var notificationRef = Substitute.For<IComRef<IToastNotification>>();
        notificationRef.Value.Returns(notification);
        _interop.CastPointer<IToastNotification>(fakePtr).Returns(notificationRef);
        var notifier = Substitute.For<INativeToastNotifier>();

        using var sut = new XmlToast(content, _interop);
        var handle = sut.CreateNotification(notifier);

        Assert.NotNull(handle);
        Assert.Same(notifier, handle.Notifier);
        Assert.Equal("my-tag", handle.Tag);
    }

    // --- Helpers ---

    private void SetupToastNotificationFactory(out IToastNotificationFactory factory, out IntPtr fakePtr)
    {
        factory = Substitute.For<IToastNotificationFactory>();
        var ptr = new IntPtr(0x1234);
        var captured = ptr;
        factory.CreateToastNotification(Arg.Any<object>(), out Arg.Any<IntPtr>())
            .Returns(ci =>
            {
                ci[1] = captured;
                return 0;
            });
        fakePtr = ptr;

        var factoryRef = Substitute.For<IComRef<IToastNotificationFactory>>();
        factoryRef.Value.Returns(factory);
        _interop.GetActivationFactory<IToastNotificationFactory>(Arg.Any<string>(), Arg.Any<Guid>()).Returns(factoryRef);
    }
}
