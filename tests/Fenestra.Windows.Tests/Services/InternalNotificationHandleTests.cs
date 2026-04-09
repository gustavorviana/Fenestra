using Fenestra.Windows;
using Fenestra.Windows.Models;
using Fenestra.Windows.Native;
using Fenestra.Windows.Native.Toast;
using Fenestra.Windows.Services;
using NSubstitute;

namespace Fenestra.Windows.Tests.Services;

public class InternalNotificationHandleTests
{
    private const int S_OK = 0;

    private readonly INativeToastNotifier _notifier = Substitute.For<INativeToastNotifier>();
    private readonly IWinRtInterop _interop = Substitute.For<IWinRtInterop>();
    private readonly IXmlToastFactory _xmlToastFactory = Substitute.For<IXmlToastFactory>();
    private readonly ITypedEventHandlerFactory _eventHandlerFactory = Substitute.For<ITypedEventHandlerFactory>();

    private IToastNotification CreateMultiVersionNotification()
    {
        // A notification that supports ALL IToastNotificationN interfaces
        return (IToastNotification)Substitute.For(new[]
        {
            typeof(IToastNotification),
            typeof(IToastNotification2),
            typeof(IToastNotification3),
            typeof(IToastNotification4),
            typeof(IToastNotification6),
        }, Array.Empty<object>());
    }

    private InternalNotificationHandle CreateSut(ToastContent content, IToastNotification notification)
    {
        // Wrap every IToastNotification in a no-op IComRef mock so Dispose doesn't hit Marshal.ReleaseComObject.
        Func<IToastNotification, IComRef<IToastNotification>> wrap = n =>
        {
            var comRef = Substitute.For<IComRef<IToastNotification>>();
            comRef.Value.Returns(n);
            return comRef;
        };
        return new InternalNotificationHandle(
            _notifier, content, notification, _interop, _xmlToastFactory, _eventHandlerFactory, wrap);
    }

    // --- Property capture ---

    [Fact]
    public void Ctor_CapturesContentPropertiesOnHandle()
    {
        var notif = CreateMultiVersionNotification();
        var expTime = DateTimeOffset.UtcNow.AddMinutes(5);
        var content = new ToastContent
        {
            Tag = "t",
            Group = "g",
            SuppressPopup = true,
            Priority = ToastPriority.High,
            ExpiresOnReboot = true,
            ExpirationTime = expTime,
            NotificationMirroring = NotificationMirroring.Disabled,
            RemoteId = "remote-1"
        };

        var sut = CreateSut(content, notif);

        Assert.Equal("t", sut.Tag);
        Assert.Equal("g", sut.Group);
        Assert.True(sut.SuppressPopup);
        Assert.Equal(ToastPriority.High, sut.Priority);
        Assert.True(sut.ExpiresOnReboot);
        Assert.Equal(expTime, sut.ExpirationTime);
        Assert.Equal(NotificationMirroring.Disabled, sut.NotificationMirroring);
        Assert.Equal("remote-1", sut.RemoteId);
    }

    // --- IToastNotification2 application (Tag, Group, SuppressPopup) ---

    [Fact]
    public void Ctor_WithTag_CallsPutTagOnNotification2()
    {
        var notif = CreateMultiVersionNotification();
        var content = new ToastContent { Tag = "my-tag" };

        CreateSut(content, notif);

        ((IToastNotification2)notif).Received(1).put_Tag("my-tag");
    }

    [Fact]
    public void Ctor_WithoutTag_DoesNotCallPutTag()
    {
        var notif = CreateMultiVersionNotification();
        var content = new ToastContent();

        CreateSut(content, notif);

        ((IToastNotification2)notif).DidNotReceive().put_Tag(Arg.Any<string>());
    }

    [Fact]
    public void Ctor_WithGroup_CallsPutGroupOnNotification2()
    {
        var notif = CreateMultiVersionNotification();
        var content = new ToastContent { Group = "g" };

        CreateSut(content, notif);

        ((IToastNotification2)notif).Received(1).put_Group("g");
    }

    [Fact]
    public void Ctor_WithSuppressPopup_CallsPutSuppressPopup()
    {
        var notif = CreateMultiVersionNotification();
        var content = new ToastContent { SuppressPopup = true };

        CreateSut(content, notif);

        ((IToastNotification2)notif).Received(1).put_SuppressPopup(1);
    }

    [Fact]
    public void Ctor_WithoutSuppressPopup_DoesNotCallPutSuppressPopup()
    {
        var notif = CreateMultiVersionNotification();
        var content = new ToastContent { SuppressPopup = false };

        CreateSut(content, notif);

        ((IToastNotification2)notif).DidNotReceive().put_SuppressPopup(Arg.Any<int>());
    }

    // --- IToastNotification4: Priority ---

    [Fact]
    public void Ctor_WithNonDefaultPriority_CallsPutPriority()
    {
        var notif = CreateMultiVersionNotification();
        var content = new ToastContent { Priority = ToastPriority.High };

        CreateSut(content, notif);

        ((IToastNotification4)notif).Received(1).put_Priority((int)ToastPriority.High);
    }

    [Fact]
    public void Ctor_WithDefaultPriority_DoesNotCallPutPriority()
    {
        var notif = CreateMultiVersionNotification();
        var content = new ToastContent { Priority = ToastPriority.Default };

        CreateSut(content, notif);

        ((IToastNotification4)notif).DidNotReceive().put_Priority(Arg.Any<int>());
    }

    // --- IToastNotification6: ExpiresOnReboot ---

    [Fact]
    public void Ctor_WithExpiresOnReboot_CallsPutExpiresOnReboot()
    {
        var notif = CreateMultiVersionNotification();
        var content = new ToastContent { ExpiresOnReboot = true };

        CreateSut(content, notif);

        ((IToastNotification6)notif).Received(1).put_ExpiresOnReboot(1);
    }

    [Fact]
    public void Ctor_WithoutExpiresOnReboot_DoesNotCallPutExpiresOnReboot()
    {
        var notif = CreateMultiVersionNotification();
        var content = new ToastContent { ExpiresOnReboot = false };

        CreateSut(content, notif);

        ((IToastNotification6)notif).DidNotReceive().put_ExpiresOnReboot(Arg.Any<int>());
    }

    // --- ExpirationTime ---

    [Fact]
    public void Ctor_WithExpirationTime_CallsNotifierSetExpirationTime()
    {
        var notif = CreateMultiVersionNotification();
        var expTime = DateTimeOffset.UtcNow.AddMinutes(10);
        var content = new ToastContent { ExpirationTime = expTime };

        CreateSut(content, notif);

        _notifier.Received(1).SetExpirationTime(notif, expTime);
    }

    [Fact]
    public void Ctor_WithoutExpirationTime_DoesNotCallNotifierSetExpirationTime()
    {
        var notif = CreateMultiVersionNotification();
        var content = new ToastContent();

        CreateSut(content, notif);

        _notifier.DidNotReceive().SetExpirationTime(Arg.Any<IToastNotification>(), Arg.Any<DateTimeOffset>());
    }

    // --- IToastNotification3: Mirroring + RemoteId ---

    [Fact]
    public void Ctor_WithNonAllowedMirroring_CallsPutNotificationMirroring()
    {
        var notif = CreateMultiVersionNotification();
        var content = new ToastContent { NotificationMirroring = NotificationMirroring.Disabled };

        CreateSut(content, notif);

        ((IToastNotification3)notif).Received(1).put_NotificationMirroring((int)NotificationMirroring.Disabled);
    }

    [Fact]
    public void Ctor_WithAllowedMirroring_DoesNotCallPutNotificationMirroring()
    {
        var notif = CreateMultiVersionNotification();
        var content = new ToastContent { NotificationMirroring = NotificationMirroring.Allowed };

        CreateSut(content, notif);

        ((IToastNotification3)notif).DidNotReceive().put_NotificationMirroring(Arg.Any<int>());
    }

    [Fact]
    public void Ctor_WithRemoteId_CallsPutRemoteId()
    {
        var notif = CreateMultiVersionNotification();
        var content = new ToastContent { RemoteId = "r1" };

        CreateSut(content, notif);

        ((IToastNotification3)notif).Received(1).put_RemoteId("r1");
    }

    [Fact]
    public void Ctor_WithoutRemoteId_DoesNotCallPutRemoteId()
    {
        var notif = CreateMultiVersionNotification();
        var content = new ToastContent();

        CreateSut(content, notif);

        ((IToastNotification3)notif).DidNotReceive().put_RemoteId(Arg.Any<string>());
    }

    // --- Show ---

    [Fact]
    public void Show_NullTracker_DelegatesToNotifierShow()
    {
        var notif = CreateMultiVersionNotification();
        var sut = CreateSut(new ToastContent { Tag = "t" }, notif);

        sut.Show(null);

        _notifier.Received(1).Show(notif);
    }

    [Fact]
    public void Show_NullTracker_RegistersThreeEventHandlers()
    {
        var notif = CreateMultiVersionNotification();
        var sut = CreateSut(new ToastContent { Tag = "t" }, notif);

        sut.Show(null);

        _eventHandlerFactory.Received(1).Create(ToastInteropConstants.IID_TypedEventHandler_Activated, Arg.Any<Action<IntPtr, IntPtr>>());
        _eventHandlerFactory.Received(1).Create(ToastInteropConstants.IID_TypedEventHandler_Dismissed, Arg.Any<Action<IntPtr, IntPtr>>());
        _eventHandlerFactory.Received(1).Create(ToastInteropConstants.IID_TypedEventHandler_Failed, Arg.Any<Action<IntPtr, IntPtr>>());
    }

    [Fact]
    public void Show_WithTracker_BindsTrackerCallback()
    {
        var notif = CreateMultiVersionNotification();
        var tracker = new ToastProgressTracker();
        var sut = CreateSut(new ToastContent { Tag = "t" }, notif);
        _notifier.Update("t", null, Arg.Any<Dictionary<string, string>>(), 0).Returns(NotificationUpdateResult.Succeeded);

        sut.Show(tracker);

        // Reporting should now push updates through the notifier
        tracker.Report(0.5, "Working");
        _notifier.ReceivedWithAnyArgs().Update(default!, default, default!, default);
    }

    [Fact]
    public void Show_WithTracker_InitialUpdateIncludesProgressStatusAndValue()
    {
        var notif = CreateMultiVersionNotification();
        var sut = CreateSut(new ToastContent { Tag = "t" }, notif);

        sut.Show(new ToastProgressTracker());

        _notifier.Received(1).Update(
            "t",
            null,
            Arg.Is<Dictionary<string, string>>(d =>
                d.ContainsKey("progressStatus") && d.ContainsKey("progressValue")),
            0);
    }

    [Fact]
    public void Show_WithTrackerTitle_InitialUpdateIncludesProgressTitle()
    {
        var notif = CreateMultiVersionNotification();
        var sut = CreateSut(new ToastContent { Tag = "t" }, notif);

        sut.Show(new ToastProgressTracker(title: "My Upload"));

        _notifier.Received(1).Update(
            Arg.Any<string>(), Arg.Any<string?>(),
            Arg.Is<Dictionary<string, string>>(d =>
                d.ContainsKey("progressTitle") && d["progressTitle"] == "My Upload"),
            Arg.Any<uint>());
    }

    [Fact]
    public void Show_WithTrackerValueOverride_InitialUpdateIncludesProgressValueOverride()
    {
        var notif = CreateMultiVersionNotification();
        var sut = CreateSut(new ToastContent { Tag = "t" }, notif);

        sut.Show(new ToastProgressTracker(useValueOverride: true));

        _notifier.Received(1).Update(
            Arg.Any<string>(), Arg.Any<string?>(),
            Arg.Is<Dictionary<string, string>>(d => d.ContainsKey("progressValueOverride")),
            Arg.Any<uint>());
    }

    // --- Update ---

    [Fact]
    public void Update_WithoutTag_ReturnsFailed()
    {
        var notif = CreateMultiVersionNotification();
        var sut = CreateSut(new ToastContent(), notif);

        var result = sut.Update(new Dictionary<string, string>());

        Assert.Equal(NotificationUpdateResult.Failed, result);
        _notifier.DidNotReceive().Update(Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<Dictionary<string, string>>(), Arg.Any<uint>());
    }

    [Fact]
    public void Update_WithTag_DelegatesToNotifier()
    {
        var notif = CreateMultiVersionNotification();
        var sut = CreateSut(new ToastContent { Tag = "t", Group = "g" }, notif);
        var data = new Dictionary<string, string> { ["k"] = "v" };
        _notifier.Update("t", "g", data, 0).Returns(NotificationUpdateResult.Succeeded);

        var result = sut.Update(data);

        Assert.Equal(NotificationUpdateResult.Succeeded, result);
        _notifier.Received(1).Update("t", "g", data, 0);
    }

    [Fact]
    public void Update_WhenNotifierThrows_ReturnsFailed()
    {
        var notif = CreateMultiVersionNotification();
        var sut = CreateSut(new ToastContent { Tag = "t" }, notif);
        _notifier.Update("t", null, Arg.Any<Dictionary<string, string>>(), 0)
            .Returns<NotificationUpdateResult>(_ => throw new InvalidOperationException());

        var result = sut.Update(new Dictionary<string, string>());

        Assert.Equal(NotificationUpdateResult.Failed, result);
    }

    // --- HideNotification ---

    [Fact]
    public void HideNotification_DelegatesToNotifierHide()
    {
        var notif = CreateMultiVersionNotification();
        var sut = CreateSut(new ToastContent(), notif);

        sut.HideNotification();

        _notifier.Received(1).Hide(notif);
    }

    [Fact]
    public void HideNotification_WhenNotifierThrows_Swallows()
    {
        var notif = CreateMultiVersionNotification();
        var sut = CreateSut(new ToastContent(), notif);
        _notifier.When(n => n.Hide(Arg.Any<IToastNotification>()))
            .Do(_ => throw new InvalidOperationException());

        var ex = Record.Exception(() => sut.HideNotification());

        Assert.Null(ex);
    }

    // --- RemoveInternal / RemoveGroupInternal ---

    [Fact]
    public void RemoveInternal_WithGroup_CallsRemoveGroupedTag()
    {
        var history = Substitute.For<IToastNotificationHistory>();
        _notifier.History.Returns(history);
        var sut = CreateSut(new ToastContent(), CreateMultiVersionNotification());

        sut.RemoveInternal("my-tag", "my-group");

        history.Received(1).RemoveGroupedTag("my-tag", "my-group");
    }

    [Fact]
    public void RemoveInternal_WithoutGroup_CallsRemove()
    {
        var history = Substitute.For<IToastNotificationHistory>();
        _notifier.History.Returns(history);
        var sut = CreateSut(new ToastContent(), CreateMultiVersionNotification());

        sut.RemoveInternal("my-tag", null);

        history.Received(1).Remove("my-tag");
    }

    [Fact]
    public void RemoveInternal_WhenHistoryNull_DoesNotThrow()
    {
        _notifier.History.Returns((IToastNotificationHistory?)null);
        var sut = CreateSut(new ToastContent(), CreateMultiVersionNotification());

        var ex = Record.Exception(() => sut.RemoveInternal("tag", "group"));

        Assert.Null(ex);
    }

    [Fact]
    public void RemoveGroupInternal_CallsHistoryRemoveGroup()
    {
        var history = Substitute.For<IToastNotificationHistory>();
        _notifier.History.Returns(history);
        var sut = CreateSut(new ToastContent(), CreateMultiVersionNotification());

        sut.RemoveGroupInternal("my-group");

        history.Received(1).RemoveGroup("my-group");
    }

    // --- ReplaceInternal ---

    [Fact]
    public void ReplaceInternal_CreatesNewXmlToastFromFactory()
    {
        var oldNotif = CreateMultiVersionNotification();
        var sut = CreateSut(new ToastContent { Tag = "old" }, oldNotif);

        var newNotif = CreateMultiVersionNotification();
        var newXml = Substitute.For<IXmlToast>();
        newXml.CreateNotificationRcw().Returns(newNotif);
        _xmlToastFactory.Create(Arg.Any<ToastContent>(), _interop).Returns(newXml);

        sut.ReplaceInternal(new ToastContent { Tag = "new" });

        _xmlToastFactory.Received(1).Create(Arg.Is<ToastContent>(c => c.Tag == "new"), _interop);
    }

    [Fact]
    public void ReplaceInternal_UpdatesTagFromNewContent()
    {
        var oldNotif = CreateMultiVersionNotification();
        var sut = CreateSut(new ToastContent { Tag = "old" }, oldNotif);

        var newNotif = CreateMultiVersionNotification();
        var newXml = Substitute.For<IXmlToast>();
        newXml.CreateNotificationRcw().Returns(newNotif);
        _xmlToastFactory.Create(Arg.Any<ToastContent>(), _interop).Returns(newXml);

        sut.ReplaceInternal(new ToastContent { Tag = "new" });

        Assert.Equal("new", sut.Tag);
    }
}
