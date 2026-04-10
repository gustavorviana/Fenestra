using Fenestra.Core;
using Fenestra.Core.Models;
using Fenestra.Windows;
using Fenestra.Windows.Models;
using Fenestra.Windows.Native;
using Fenestra.Windows.Native.Toast;
using Fenestra.Windows.Services;
using NSubstitute;

namespace Fenestra.Windows.Tests.Services;

public class ToastServiceTests
{
    private const string AppId = "TestApp";
    private readonly AppInfo _appInfo = new("Test App", AppId, new Version(1, 0));
    private readonly IThreadContext _threadContext = Substitute.For<IThreadContext>();
    private readonly IWinRtInterop _interop = Substitute.For<IWinRtInterop>();
    private readonly IApplicationActivator _activator = Substitute.For<IApplicationActivator>();
    private readonly IAumidRegistrationManager _regManager = Substitute.For<IAumidRegistrationManager>();
    private readonly INativeToastNotifier _notifier = Substitute.For<INativeToastNotifier>();
    private readonly INativeToastNotifierFactory _notifierFactory = Substitute.For<INativeToastNotifierFactory>();
    private readonly IXmlToastFactory _xmlToastFactory = Substitute.For<IXmlToastFactory>();

    public ToastServiceTests()
    {
        _notifierFactory.Create(Arg.Any<string>(), Arg.Any<IWinRtInterop>()).Returns(_notifier);
        // By default, thread context runs actions synchronously (drop async-ness for testing observable outcome)
        _threadContext.InvokeAsync(Arg.Any<Action>())
            .Returns(ci => { ci.Arg<Action>()(); return Task.CompletedTask; });
    }

    private ToastService CreateSut()
    {
        return new ToastService(
            _appInfo, _threadContext, _interop, _activator, _regManager, _notifierFactory, _xmlToastFactory);
    }

    private (IXmlToast xmlToast, InternalNotificationHandle internalHandle, IToastNotification notif) SetupXmlToastForShow(ToastContent expectedContent)
    {
        var notif = (IToastNotification)Substitute.For(new[]
        {
            typeof(IToastNotification),
            typeof(IToastNotification2),
            typeof(IToastNotification3),
            typeof(IToastNotification4),
            typeof(IToastNotification6),
        }, Array.Empty<object>());
        var xmlToast = Substitute.For<IXmlToast>();

        // Build InternalNotificationHandle with no-op ComRef wrap so dispose doesn't blow up
        Func<IToastNotification, IComRef<IToastNotification>> wrap = n =>
        {
            var comRef = Substitute.For<IComRef<IToastNotification>>();
            comRef.Value.Returns(n);
            return comRef;
        };
        var internalHandle = new InternalNotificationHandle(
            _notifier, expectedContent, notif, _interop,
            _xmlToastFactory, Substitute.For<ITypedEventHandlerFactory>(), wrap);

        xmlToast.CreateNotification(_notifier).Returns(internalHandle);
        _xmlToastFactory.Create(expectedContent, _interop).Returns(xmlToast);
        return (xmlToast, internalHandle, notif);
    }

    // --- Constructor ---

    [Fact]
    public void Ctor_CallsSetCurrentProcessExplicitAppUserModelIDWithAppId()
    {
        using var sut = CreateSut();

        _interop.Received(1).SetCurrentProcessExplicitAppUserModelID(AppId);
    }

    [Fact]
    public void Ctor_CallsRegistrationManagerEnsureRegistered()
    {
        using var sut = CreateSut();

        _regManager.Received(1).EnsureRegistered();
    }

    [Fact]
    public void Ctor_WithNullRegistrationManager_DoesNotThrow()
    {
        var ex = Record.Exception(() => new ToastService(
            _appInfo, _threadContext, _interop, _activator,
            registrationManager: null,
            notifierFactory: _notifierFactory,
            xmlToastFactory: _xmlToastFactory));

        Assert.Null(ex);
    }

    [Fact]
    public void Ctor_CreatesNotifierViaFactory()
    {
        using var sut = CreateSut();

        _notifierFactory.Received(1).Create(AppId, _interop);
    }

    // --- Show(ToastContent) ---

    [Fact]
    public void Show_GeneratesTagAutomaticallyIfEmpty()
    {
        var content = new ToastContent { Title = "Hi" };
        SetupXmlToastForShow(content);
        using var sut = CreateSut();

        sut.Show(content);

        Assert.NotNull(content.Tag);
        Assert.StartsWith("toast-", content.Tag);
    }

    [Fact]
    public void Show_PreservesExistingTag()
    {
        var content = new ToastContent { Tag = "existing" };
        SetupXmlToastForShow(content);
        using var sut = CreateSut();

        sut.Show(content);

        Assert.Equal("existing", content.Tag);
    }

    [Fact]
    public void Show_AddsHandleToActiveList()
    {
        var content = new ToastContent { Tag = "t" };
        SetupXmlToastForShow(content);
        using var sut = CreateSut();

        var handle = sut.Show(content);

        Assert.Single(sut.Active);
        Assert.Contains(handle, sut.Active);
    }

    [Fact]
    public void Show_CreatesXmlToastViaFactory()
    {
        var content = new ToastContent { Tag = "t" };
        SetupXmlToastForShow(content);
        using var sut = CreateSut();

        sut.Show(content);

        _xmlToastFactory.Received(1).Create(content, _interop);
    }

    [Fact]
    public void Show_WithBuilderOverload_BuildsAndDelegates()
    {
        ToastContent? captured = null;
        _xmlToastFactory.Create(Arg.Do<ToastContent>(c => captured = c), _interop).Returns(ci =>
        {
            var xmlToast = Substitute.For<IXmlToast>();
            var notif = Substitute.For<IToastNotification>();
            Func<IToastNotification, IComRef<IToastNotification>> wrap = n =>
            {
                var cr = Substitute.For<IComRef<IToastNotification>>();
                cr.Value.Returns(n);
                return cr;
            };
            var handle = new InternalNotificationHandle(
                _notifier, (ToastContent)ci[0], notif, _interop,
                _xmlToastFactory, Substitute.For<ITypedEventHandlerFactory>(), wrap);
            xmlToast.CreateNotification(_notifier).Returns(handle);
            return xmlToast;
        });
        using var sut = CreateSut();

        sut.Show(b => b.Title("Title from builder"));

        Assert.NotNull(captured);
        Assert.Equal("Title from builder", captured!.Title);
    }

    // --- ClearHistory overloads ---

    [Fact]
    public void ClearHistory_NoArgs_CallsHistoryClearWithId()
    {
        var history = Substitute.For<IToastNotificationHistory>();
        _notifier.History.Returns(history);
        using var sut = CreateSut();

        sut.ClearHistory();

        history.Received(1).ClearWithId(AppId);
    }

    [Fact]
    public void ClearHistory_NoArgs_EmptiesActiveList()
    {
        var content = new ToastContent { Tag = "t" };
        SetupXmlToastForShow(content);
        var history = Substitute.For<IToastNotificationHistory>();
        _notifier.History.Returns(history);
        using var sut = CreateSut();
        sut.Show(content);

        sut.ClearHistory();

        Assert.Empty(sut.Active);
    }

    [Fact]
    public void ClearHistory_WithGroup_CallsHistoryRemoveGroup()
    {
        var history = Substitute.For<IToastNotificationHistory>();
        _notifier.History.Returns(history);
        using var sut = CreateSut();

        sut.ClearHistory("my-group");

        history.Received(1).RemoveGroup("my-group");
    }

    [Fact]
    public void ClearHistory_WithTagAndGroup_CallsHistoryRemoveGroupedTag()
    {
        var history = Substitute.For<IToastNotificationHistory>();
        _notifier.History.Returns(history);
        using var sut = CreateSut();

        sut.ClearHistory("my-tag", "my-group");

        history.Received(1).RemoveGroupedTag("my-tag", "my-group");
    }

    [Fact]
    public void ClearHistory_WhenHistoryNull_DoesNotThrow()
    {
        _notifier.History.Returns((IToastNotificationHistory?)null);
        using var sut = CreateSut();

        var ex = Record.Exception(() => sut.ClearHistory());

        Assert.Null(ex);
    }

    // --- GetSetting ---

    [Fact]
    public void GetSetting_DelegatesToNotifier()
    {
        _notifier.GetSetting().Returns(NotificationSetting.DisabledForUser);
        using var sut = CreateSut();

        var result = sut.GetSetting();

        Assert.Equal(NotificationSetting.DisabledForUser, result);
    }

    // --- FindByTag / FindByGroup ---

    [Fact]
    public void FindByTag_ReturnsMatchingHandle()
    {
        var content1 = new ToastContent { Tag = "a" };
        var content2 = new ToastContent { Tag = "b" };
        SetupXmlToastForShow(content1);
        SetupXmlToastForShow(content2);
        using var sut = CreateSut();
        var handleA = sut.Show(content1);
        var handleB = sut.Show(content2);

        Assert.Same(handleA, sut.FindByTag("a"));
        Assert.Same(handleB, sut.FindByTag("b"));
    }

    [Fact]
    public void FindByTag_NonExistent_ReturnsNull()
    {
        using var sut = CreateSut();

        Assert.Null(sut.FindByTag("missing"));
    }

    [Fact]
    public void FindByGroup_ReturnsHandlesOfGroup()
    {
        var content1 = new ToastContent { Tag = "a", Group = "g1" };
        var content2 = new ToastContent { Tag = "b", Group = "g1" };
        var content3 = new ToastContent { Tag = "c", Group = "g2" };
        SetupXmlToastForShow(content1);
        SetupXmlToastForShow(content2);
        SetupXmlToastForShow(content3);
        using var sut = CreateSut();
        sut.Show(content1);
        sut.Show(content2);
        sut.Show(content3);

        var g1Handles = sut.FindByGroup("g1");

        Assert.Equal(2, g1Handles.Count);
    }

    // --- Dispose ---

    [Fact]
    public void Dispose_IsIdempotent()
    {
        var sut = CreateSut();

        sut.Dispose();
        var ex = Record.Exception(() => sut.Dispose());

        Assert.Null(ex);
    }

    [Fact]
    public void Dispose_DisposesNotifier()
    {
        var sut = CreateSut();

        sut.Dispose();

        _notifier.Received(1).Dispose();
    }

    [Fact]
    public void Dispose_ClearsActiveList()
    {
        var content = new ToastContent { Tag = "t" };
        SetupXmlToastForShow(content);
        var sut = CreateSut();
        sut.Show(content);

        sut.Dispose();

        Assert.Empty(sut.Active);
    }
}
