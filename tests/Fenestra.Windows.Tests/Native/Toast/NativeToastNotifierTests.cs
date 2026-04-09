using Fenestra.Windows.Models;
using Fenestra.Windows.Native;
using Fenestra.Windows.Native.Toast;
using NSubstitute;
using System.Runtime.InteropServices;

namespace Fenestra.Windows.Tests.Native.Toast;

public class NativeToastNotifierTests
{
    private const string AppId = "TestApp";
    private const int S_OK = 0;
    private const int E_FAIL = unchecked((int)0x80004005);

    private readonly IWinRtInterop _interop = Substitute.For<IWinRtInterop>();
    private readonly IToastNotificationManagerStatics _managerStatics = Substitute.For<IToastNotificationManagerStatics>();
    private readonly IToastNotifier _notifier = Substitute.For<IToastNotifier>();
    private readonly IntPtr _notifierPtr = new(0x1000);

    public NativeToastNotifierTests()
    {
        // Factory lookup succeeds
        var managerRef = Substitute.For<IComRef<IToastNotificationManagerStatics>>();
        managerRef.Value.Returns(_managerStatics);
        _interop.GetActivationFactory<IToastNotificationManagerStatics>(
            "Windows.UI.Notifications.ToastNotificationManager", Arg.Any<Guid>())
            .Returns(managerRef);

        // CreateToastNotifier returns S_OK with a fake pointer
        _managerStatics.CreateToastNotifier(out Arg.Any<IntPtr>())
            .Returns(ci => { ci[0] = _notifierPtr; return S_OK; });

        // CastPointer returns the mocked notifier
        var notifierRef = Substitute.For<IComRef<IToastNotifier>>();
        notifierRef.Value.Returns(_notifier);
        _interop.CastPointer<IToastNotifier>(_notifierPtr).Returns(notifierRef);

        // Statics2 lookup returns null by default (no history) — individual tests can override
        _interop.GetActivationFactory<IToastNotificationManagerStatics2>(
            Arg.Any<string>(), Arg.Any<Guid>())
            .Returns((IComRef<IToastNotificationManagerStatics2>?)null);
    }

    // --- Constructor ---

    [Fact]
    public void Ctor_FetchesToastNotificationManagerStatics()
    {
        using var sut = new NativeToastNotifier(AppId, _interop);

        _interop.Received(1).GetActivationFactory<IToastNotificationManagerStatics>(
            "Windows.UI.Notifications.ToastNotificationManager",
            ToastInteropConstants.IID_IToastNotificationManagerStatics);
    }

    [Fact]
    public void Ctor_WhenFactoryReturnsNull_Throws()
    {
        _interop.GetActivationFactory<IToastNotificationManagerStatics>(Arg.Any<string>(), Arg.Any<Guid>())
            .Returns((IComRef<IToastNotificationManagerStatics>?)null);

        Assert.Throws<InvalidOperationException>(() => new NativeToastNotifier(AppId, _interop));
    }

    [Fact]
    public void Ctor_CallsCreateToastNotifierFirst()
    {
        using var sut = new NativeToastNotifier(AppId, _interop);

        _managerStatics.Received(1).CreateToastNotifier(out Arg.Any<IntPtr>());
    }

    [Fact]
    public void Ctor_WhenCreateToastNotifierFails_FallsBackToWithId()
    {
        _managerStatics.CreateToastNotifier(out Arg.Any<IntPtr>()).Returns(E_FAIL);
        _managerStatics.CreateToastNotifierWithId(AppId, out Arg.Any<IntPtr>())
            .Returns(ci => { ci[1] = _notifierPtr; return S_OK; });

        using var sut = new NativeToastNotifier(AppId, _interop);

        _managerStatics.Received(1).CreateToastNotifierWithId(AppId, out Arg.Any<IntPtr>());
    }

    [Fact]
    public void Ctor_WhenBothCreateMethodsFail_Throws()
    {
        _managerStatics.CreateToastNotifier(out Arg.Any<IntPtr>()).Returns(E_FAIL);
        _managerStatics.CreateToastNotifierWithId(Arg.Any<string>(), out Arg.Any<IntPtr>()).Returns(E_FAIL);

        Assert.Throws<InvalidOperationException>(() => new NativeToastNotifier(AppId, _interop));
    }

    [Fact]
    public void Ctor_WhenCreateToastNotifierReturnsZeroPtr_FallsBackToWithId()
    {
        _managerStatics.CreateToastNotifier(out Arg.Any<IntPtr>())
            .Returns(ci => { ci[0] = IntPtr.Zero; return S_OK; });
        _managerStatics.CreateToastNotifierWithId(AppId, out Arg.Any<IntPtr>())
            .Returns(ci => { ci[1] = _notifierPtr; return S_OK; });

        using var sut = new NativeToastNotifier(AppId, _interop);

        _managerStatics.Received(1).CreateToastNotifierWithId(AppId, out Arg.Any<IntPtr>());
    }

    [Fact]
    public void Ctor_WrapsNotifierPointerViaCastPointer()
    {
        using var sut = new NativeToastNotifier(AppId, _interop);

        _interop.Received(1).CastPointer<IToastNotifier>(_notifierPtr);
    }

    [Fact]
    public void Ctor_WhenCastPointerReturnsNull_Throws()
    {
        _interop.CastPointer<IToastNotifier>(Arg.Any<IntPtr>()).Returns((IComRef<IToastNotifier>?)null);

        Assert.Throws<InvalidOperationException>(() => new NativeToastNotifier(AppId, _interop));
    }

    [Fact]
    public void Ctor_TriesToFetchStatics2ForHistory()
    {
        using var sut = new NativeToastNotifier(AppId, _interop);

        _interop.Received(1).GetActivationFactory<IToastNotificationManagerStatics2>(
            "Windows.UI.Notifications.ToastNotificationManager",
            ToastInteropConstants.IID_IToastNotificationManagerStatics2);
    }

    [Fact]
    public void Ctor_WhenStatics2Null_HistoryIsNull()
    {
        using var sut = new NativeToastNotifier(AppId, _interop);

        Assert.Null(sut.History);
    }

    [Fact]
    public void Ctor_WhenStatics2Returns_HistoryIsWrappedFromGetHistory()
    {
        var statics2 = Substitute.For<IToastNotificationManagerStatics2>();
        var historyPtr = new IntPtr(0x2000);
        statics2.get_History(out Arg.Any<IntPtr>())
            .Returns(ci => { ci[0] = historyPtr; return S_OK; });

        var statics2Ref = Substitute.For<IComRef<IToastNotificationManagerStatics2>>();
        statics2Ref.Value.Returns(statics2);
        _interop.GetActivationFactory<IToastNotificationManagerStatics2>(Arg.Any<string>(), Arg.Any<Guid>())
            .Returns(statics2Ref);

        var history = Substitute.For<IToastNotificationHistory>();
        var historyRef = Substitute.For<IComRef<IToastNotificationHistory>>();
        historyRef.Value.Returns(history);
        _interop.CastPointer<IToastNotificationHistory>(historyPtr).Returns(historyRef);

        using var sut = new NativeToastNotifier(AppId, _interop);

        Assert.Same(history, sut.History);
    }

    // --- Show / Hide ---

    [Fact]
    public void Show_DelegatesToNotifier()
    {
        using var sut = new NativeToastNotifier(AppId, _interop);
        var notification = Substitute.For<IToastNotification>();
        _notifier.Show(notification).Returns(S_OK);

        sut.Show(notification);

        _notifier.Received(1).Show(notification);
    }

    [Fact]
    public void Show_WhenHResultNegative_ThrowsComException()
    {
        using var sut = new NativeToastNotifier(AppId, _interop);
        var notification = Substitute.For<IToastNotification>();
        _notifier.Show(notification).Returns(E_FAIL);

        Assert.Throws<COMException>(() => sut.Show(notification));
    }

    [Fact]
    public void Hide_DelegatesToNotifier()
    {
        using var sut = new NativeToastNotifier(AppId, _interop);
        var notification = Substitute.For<IToastNotification>();
        _notifier.Hide(notification).Returns(S_OK);

        sut.Hide(notification);

        _notifier.Received(1).Hide(notification);
    }

    [Fact]
    public void Hide_WhenHResultNegative_ThrowsComException()
    {
        using var sut = new NativeToastNotifier(AppId, _interop);
        var notification = Substitute.For<IToastNotification>();
        _notifier.Hide(notification).Returns(E_FAIL);

        Assert.Throws<COMException>(() => sut.Hide(notification));
    }

    // --- GetSetting ---

    [Fact]
    public void GetSetting_ReturnsNotifierSettingValue()
    {
        using var sut = new NativeToastNotifier(AppId, _interop);
        _notifier.get_Setting(out Arg.Any<int>())
            .Returns(ci => { ci[0] = (int)NotificationSetting.DisabledForUser; return S_OK; });

        var result = sut.GetSetting();

        Assert.Equal(NotificationSetting.DisabledForUser, result);
    }

    // --- Scheduling ---

    [Fact]
    public void CreateScheduledToast_LazyFetchesFactory()
    {
        using var sut = new NativeToastNotifier(AppId, _interop);
        var (_, _) = SetupScheduledFactory();
        var xml = new object();

        sut.CreateScheduledToast(xml, DateTimeOffset.UtcNow.AddMinutes(5));

        _interop.Received(1).GetActivationFactory<IScheduledToastNotificationFactory>(
            "Windows.UI.Notifications.ScheduledToastNotification",
            ToastInteropConstants.IID_IScheduledToastNotificationFactory);
    }

    [Fact]
    public void CreateScheduledToast_SecondCall_ReusesCachedFactory()
    {
        using var sut = new NativeToastNotifier(AppId, _interop);
        var (_, _) = SetupScheduledFactory();

        sut.CreateScheduledToast(new object(), DateTimeOffset.UtcNow);
        sut.CreateScheduledToast(new object(), DateTimeOffset.UtcNow);

        _interop.Received(1).GetActivationFactory<IScheduledToastNotificationFactory>(
            Arg.Any<string>(), Arg.Any<Guid>());
    }

    [Fact]
    public void CreateScheduledToast_WhenFactoryReturnsNull_Throws()
    {
        using var sut = new NativeToastNotifier(AppId, _interop);
        _interop.GetActivationFactory<IScheduledToastNotificationFactory>(Arg.Any<string>(), Arg.Any<Guid>())
            .Returns((IComRef<IScheduledToastNotificationFactory>?)null);

        Assert.Throws<InvalidOperationException>(() =>
            sut.CreateScheduledToast(new object(), DateTimeOffset.UtcNow));
    }

    [Fact]
    public void CreateScheduledToast_WhenHResultNegative_ThrowsComException()
    {
        using var sut = new NativeToastNotifier(AppId, _interop);
        var factory = Substitute.For<IScheduledToastNotificationFactory>();
        factory.CreateScheduledToastNotification(Arg.Any<object>(), Arg.Any<long>(), out Arg.Any<IntPtr>())
            .Returns(E_FAIL);
        var factoryRef = Substitute.For<IComRef<IScheduledToastNotificationFactory>>();
        factoryRef.Value.Returns(factory);
        _interop.GetActivationFactory<IScheduledToastNotificationFactory>(Arg.Any<string>(), Arg.Any<Guid>())
            .Returns(factoryRef);

        Assert.Throws<COMException>(() => sut.CreateScheduledToast(new object(), DateTimeOffset.UtcNow));
    }

    [Fact]
    public void CreateScheduledToast_ReturnsCastedScheduled()
    {
        using var sut = new NativeToastNotifier(AppId, _interop);
        var (scheduled, _) = SetupScheduledFactory();

        var result = sut.CreateScheduledToast(new object(), DateTimeOffset.UtcNow);

        Assert.Same(scheduled, result);
    }

    [Fact]
    public void AddToSchedule_DelegatesToNotifier()
    {
        using var sut = new NativeToastNotifier(AppId, _interop);
        var scheduled = Substitute.For<IScheduledToastNotification>();
        _notifier.AddToSchedule(scheduled).Returns(S_OK);

        sut.AddToSchedule(scheduled);

        _notifier.Received(1).AddToSchedule(scheduled);
    }

    [Fact]
    public void AddToSchedule_WhenHResultNegative_ThrowsComException()
    {
        using var sut = new NativeToastNotifier(AppId, _interop);
        var scheduled = Substitute.For<IScheduledToastNotification>();
        _notifier.AddToSchedule(scheduled).Returns(E_FAIL);

        Assert.Throws<COMException>(() => sut.AddToSchedule(scheduled));
    }

    [Fact]
    public void RemoveFromSchedule_WhenHResultNegative_ThrowsComException()
    {
        using var sut = new NativeToastNotifier(AppId, _interop);
        var scheduled = Substitute.For<IScheduledToastNotification>();
        _notifier.RemoveFromSchedule(scheduled).Returns(E_FAIL);

        Assert.Throws<COMException>(() => sut.RemoveFromSchedule(scheduled));
    }

    // --- Update ---

    [Fact]
    public void Update_WhenNotifierNotIToastNotifier2_ReturnsFailed()
    {
        using var sut = new NativeToastNotifier(AppId, _interop);
        // Default _notifier mock doesn't implement IToastNotifier2

        var result = sut.Update("tag", "group", new Dictionary<string, string>(), 0);

        Assert.Equal(NotificationUpdateResult.Failed, result);
    }

    [Fact]
    public void Update_WithGroup_CallsUpdateWithTagAndGroup()
    {
        // Create a mock that implements both IToastNotifier and IToastNotifier2
        var notifier2 = Substitute.For<IToastNotifier, IToastNotifier2>();
        var notifierRef = Substitute.For<IComRef<IToastNotifier>>();
        notifierRef.Value.Returns(notifier2);
        _interop.CastPointer<IToastNotifier>(_notifierPtr).Returns(notifierRef);
        SetupNotificationDataActivation();

        using var sut = new NativeToastNotifier(AppId, _interop);
        var data = new Dictionary<string, string> { ["k"] = "v" };
        ((IToastNotifier2)notifier2).UpdateWithTagAndGroup(
            Arg.Any<INotificationData>(), "tag", "group", out Arg.Any<int>())
            .Returns(ci => { ci[3] = (int)NotificationUpdateResult.Succeeded; return S_OK; });

        var result = sut.Update("tag", "group", data, 1);

        Assert.Equal(NotificationUpdateResult.Succeeded, result);
        ((IToastNotifier2)notifier2).Received(1).UpdateWithTagAndGroup(
            Arg.Any<INotificationData>(), "tag", "group", out Arg.Any<int>());
    }

    [Fact]
    public void Update_WithoutGroup_CallsUpdateWithTagOnly()
    {
        var notifier2 = Substitute.For<IToastNotifier, IToastNotifier2>();
        var notifierRef = Substitute.For<IComRef<IToastNotifier>>();
        notifierRef.Value.Returns(notifier2);
        _interop.CastPointer<IToastNotifier>(_notifierPtr).Returns(notifierRef);
        SetupNotificationDataActivation();

        using var sut = new NativeToastNotifier(AppId, _interop);
        ((IToastNotifier2)notifier2).UpdateWithTag(Arg.Any<INotificationData>(), "tag", out Arg.Any<int>())
            .Returns(ci => { ci[2] = (int)NotificationUpdateResult.Succeeded; return S_OK; });

        sut.Update("tag", null, new Dictionary<string, string>(), 1);

        ((IToastNotifier2)notifier2).Received(1).UpdateWithTag(
            Arg.Any<INotificationData>(), "tag", out Arg.Any<int>());
    }

    // --- Dispose ---

    [Fact]
    public void Dispose_IsIdempotent()
    {
        var sut = new NativeToastNotifier(AppId, _interop);

        sut.Dispose();
        var ex = Record.Exception(() => sut.Dispose());

        Assert.Null(ex);
    }

    // --- Helpers ---

    private (IScheduledToastNotification scheduled, IntPtr ptr) SetupScheduledFactory()
    {
        var factory = Substitute.For<IScheduledToastNotificationFactory>();
        var scheduledPtr = new IntPtr(0x3000);
        factory.CreateScheduledToastNotification(Arg.Any<object>(), Arg.Any<long>(), out Arg.Any<IntPtr>())
            .Returns(ci => { ci[2] = scheduledPtr; return S_OK; });

        var factoryRef = Substitute.For<IComRef<IScheduledToastNotificationFactory>>();
        factoryRef.Value.Returns(factory);
        _interop.GetActivationFactory<IScheduledToastNotificationFactory>(Arg.Any<string>(), Arg.Any<Guid>())
            .Returns(factoryRef);

        var scheduled = Substitute.For<IScheduledToastNotification>();
        var scheduledRef = Substitute.For<IComRef<IScheduledToastNotification>>();
        scheduledRef.Value.Returns(scheduled);
        _interop.CastPointer<IScheduledToastNotification>(scheduledPtr).Returns(scheduledRef);

        return (scheduled, scheduledPtr);
    }

    private void SetupNotificationDataActivation()
    {
        var data = Substitute.For<INotificationData>();
        data.get_Values(out Arg.Any<IMapStringString>())
            .Returns(ci => { ci[0] = null!; return S_OK; });
        var dataRef = Substitute.For<IComRef<INotificationData>>();
        dataRef.Value.Returns(data);
        _interop.ActivateInstance<INotificationData>("Windows.UI.Notifications.NotificationData").Returns(dataRef);
    }
}
