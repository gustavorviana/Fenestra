using Fenestra.Core;
using Fenestra.Windows;
using Fenestra.Windows.Native;
using Fenestra.Windows.Services;
using NSubstitute;

namespace Fenestra.Windows.Tests.Services;

public class IdleDetectionServiceTests
{
    private readonly IIdleInputProbe _probe = Substitute.For<IIdleInputProbe>();

    /// <summary>
    /// Default test SUT uses a 1-hour PollInterval so the background
    /// <see cref="System.Threading.Timer"/> never fires during a test run. All assertions
    /// are driven by explicit calls to the internal <c>Poll()</c> method.
    /// </summary>
    private IdleDetectionService CreateSut(
        TimeSpan? threshold = null,
        TimeSpan? pollInterval = null,
        IThreadContext? threadContext = null)
    {
        var opts = new IdleDetectionOptions
        {
            Threshold = threshold ?? TimeSpan.FromMinutes(5),
            PollInterval = pollInterval ?? TimeSpan.FromHours(1),
        };
        return new IdleDetectionService(opts, threadContext, _probe);
    }

    // =====================================================================
    // Constructor — validation
    // =====================================================================

    [Fact]
    public void Ctor_NullOptions_Throws()
    {
        Assert.Throws<ArgumentNullException>(
            () => new IdleDetectionService(null!, null, _probe));
    }

    [Fact]
    public void Ctor_ThresholdBelowMinimum_Throws()
    {
        var opts = new IdleDetectionOptions { Threshold = TimeSpan.FromMilliseconds(500) };
        Assert.Throws<ArgumentException>(() => new IdleDetectionService(opts, null, _probe));
    }

    [Fact]
    public void Ctor_ThresholdAtMinimum_DoesNotThrow()
    {
        var opts = new IdleDetectionOptions
        {
            Threshold = TimeSpan.FromSeconds(1),
            PollInterval = TimeSpan.FromHours(1),
        };
        var ex = Record.Exception(() =>
        {
            using var sut = new IdleDetectionService(opts, null, _probe);
        });
        Assert.Null(ex);
    }

    [Fact]
    public void Ctor_PollIntervalBelowMinimum_Throws()
    {
        var opts = new IdleDetectionOptions { PollInterval = TimeSpan.FromMilliseconds(50) };
        Assert.Throws<ArgumentException>(() => new IdleDetectionService(opts, null, _probe));
    }

    [Fact]
    public void Ctor_PollIntervalAtMinimum_DoesNotThrow()
    {
        var opts = new IdleDetectionOptions { PollInterval = TimeSpan.FromMilliseconds(100) };
        var ex = Record.Exception(() =>
        {
            using var sut = new IdleDetectionService(opts, null, _probe);
        });
        Assert.Null(ex);
    }

    [Fact]
    public void Ctor_AcceptsNullProbe_DoesNotThrow()
    {
        var opts = new IdleDetectionOptions { PollInterval = TimeSpan.FromHours(1) };
        var ex = Record.Exception(() =>
        {
            using var sut = new IdleDetectionService(opts);
        });
        Assert.Null(ex);
    }

    [Fact]
    public void Ctor_AcceptsNullThreadContext_DoesNotThrow()
    {
        var opts = new IdleDetectionOptions { PollInterval = TimeSpan.FromHours(1) };
        var ex = Record.Exception(() =>
        {
            using var sut = new IdleDetectionService(opts, threadContext: null, probe: _probe);
        });
        Assert.Null(ex);
    }

    // =====================================================================
    // Constructor — initial state
    // =====================================================================

    [Fact]
    public void Ctor_InitiallyNotIdle()
    {
        using var sut = CreateSut();
        Assert.False(sut.IsIdle);
    }

    [Fact]
    public void Ctor_InitialIdleTimeIsZero()
    {
        using var sut = CreateSut();
        Assert.Equal(TimeSpan.Zero, sut.IdleTime);
    }

    [Fact]
    public void Ctor_StoresThreshold()
    {
        using var sut = CreateSut(threshold: TimeSpan.FromMinutes(3));
        Assert.Equal(TimeSpan.FromMinutes(3), sut.Threshold);
    }

    // =====================================================================
    // Threshold setter
    // =====================================================================

    [Fact]
    public void Threshold_Setter_BelowMinimum_Throws()
    {
        using var sut = CreateSut();
        Assert.Throws<ArgumentException>(() => sut.Threshold = TimeSpan.FromMilliseconds(500));
    }

    [Fact]
    public void Threshold_Setter_UpdatesProperty()
    {
        using var sut = CreateSut();
        sut.Threshold = TimeSpan.FromMinutes(10);
        Assert.Equal(TimeSpan.FromMinutes(10), sut.Threshold);
    }

    [Fact]
    public void Threshold_Setter_DoesNotInvokePoll()
    {
        using var sut = CreateSut();
        _probe.ClearReceivedCalls();

        sut.Threshold = TimeSpan.FromMinutes(10);

        _probe.DidNotReceive().GetIdleTime();
    }

    [Fact]
    public void Threshold_Setter_DoesNotRaiseEventsImmediately()
    {
        using var sut = CreateSut(threshold: TimeSpan.FromMinutes(5));
        _probe.GetIdleTime().Returns(TimeSpan.FromMinutes(10)); // user already idle
        var raised = false;
        sut.BecameIdle += (_, _) => raised = true;

        sut.Threshold = TimeSpan.FromMinutes(1); // lowering threshold would make user idle

        Assert.False(raised); // event fires only on next Poll
    }

    // =====================================================================
    // Poll — state transitions (CORE LOGIC)
    // =====================================================================

    [Fact]
    public void Poll_WhenNotIdleAndStillNotIdle_NoEvent()
    {
        using var sut = CreateSut(threshold: TimeSpan.FromMinutes(5));
        _probe.GetIdleTime().Returns(TimeSpan.FromSeconds(30));
        var idleRaised = 0;
        var activeRaised = 0;
        sut.BecameIdle += (_, _) => idleRaised++;
        sut.BecameActive += (_, _) => activeRaised++;

        sut.Poll();

        Assert.Equal(0, idleRaised);
        Assert.Equal(0, activeRaised);
    }

    [Fact]
    public void Poll_WhenNotIdleAndCrossesThreshold_RaisesBecameIdle()
    {
        using var sut = CreateSut(threshold: TimeSpan.FromMinutes(5));
        _probe.GetIdleTime().Returns(TimeSpan.FromMinutes(5) + TimeSpan.FromSeconds(1));
        var raised = 0;
        sut.BecameIdle += (_, _) => raised++;

        sut.Poll();

        Assert.Equal(1, raised);
    }

    [Fact]
    public void Poll_AfterBecameIdle_IsIdleIsTrue()
    {
        using var sut = CreateSut(threshold: TimeSpan.FromMinutes(5));
        _probe.GetIdleTime().Returns(TimeSpan.FromMinutes(6));

        sut.Poll();

        Assert.True(sut.IsIdle);
    }

    [Fact]
    public void Poll_WhenIdleAndStillIdle_NoEvent()
    {
        using var sut = CreateSut(threshold: TimeSpan.FromMinutes(5));
        _probe.GetIdleTime().Returns(TimeSpan.FromMinutes(6));
        sut.Poll(); // first: becomes idle
        var raised = 0;
        sut.BecameIdle += (_, _) => raised++;
        sut.BecameActive += (_, _) => raised++;

        sut.Poll(); // second: still idle

        Assert.Equal(0, raised);
    }

    [Fact]
    public void Poll_WhenIdleAndCrossesBackBelowThreshold_RaisesBecameActive()
    {
        using var sut = CreateSut(threshold: TimeSpan.FromMinutes(5));
        _probe.GetIdleTime().Returns(TimeSpan.FromMinutes(6));
        sut.Poll(); // becomes idle

        _probe.GetIdleTime().Returns(TimeSpan.FromSeconds(10));
        var raised = 0;
        sut.BecameActive += (_, _) => raised++;

        sut.Poll(); // user just moved

        Assert.Equal(1, raised);
    }

    [Fact]
    public void Poll_AfterBecameActive_IsIdleIsFalse()
    {
        using var sut = CreateSut(threshold: TimeSpan.FromMinutes(5));
        _probe.GetIdleTime().Returns(TimeSpan.FromMinutes(6));
        sut.Poll();
        _probe.GetIdleTime().Returns(TimeSpan.FromSeconds(5));

        sut.Poll();

        Assert.False(sut.IsIdle);
    }

    [Fact]
    public void Poll_MultipleTransitions_RaisesEventsInOrder()
    {
        using var sut = CreateSut(threshold: TimeSpan.FromMinutes(5));
        var events = new List<string>();
        sut.BecameIdle += (_, _) => events.Add("idle");
        sut.BecameActive += (_, _) => events.Add("active");

        // idle
        _probe.GetIdleTime().Returns(TimeSpan.FromMinutes(6));
        sut.Poll();
        // active
        _probe.GetIdleTime().Returns(TimeSpan.FromSeconds(5));
        sut.Poll();
        // idle again
        _probe.GetIdleTime().Returns(TimeSpan.FromMinutes(7));
        sut.Poll();
        // active again
        _probe.GetIdleTime().Returns(TimeSpan.FromSeconds(1));
        sut.Poll();

        Assert.Equal(new[] { "idle", "active", "idle", "active" }, events);
    }

    [Fact]
    public void Poll_ExactlyAtThreshold_ConsideredIdle()
    {
        using var sut = CreateSut(threshold: TimeSpan.FromMinutes(5));
        _probe.GetIdleTime().Returns(TimeSpan.FromMinutes(5)); // exactly at threshold

        sut.Poll();

        Assert.True(sut.IsIdle);
    }

    [Fact]
    public void Poll_OneMsBelowThreshold_ConsideredActive()
    {
        using var sut = CreateSut(threshold: TimeSpan.FromMinutes(5));
        _probe.GetIdleTime().Returns(TimeSpan.FromMinutes(5) - TimeSpan.FromMilliseconds(1));

        sut.Poll();

        Assert.False(sut.IsIdle);
    }

    [Fact]
    public void Poll_UpdatesIdleTimeProperty()
    {
        using var sut = CreateSut();
        _probe.GetIdleTime().Returns(TimeSpan.FromSeconds(42));

        sut.Poll();

        Assert.Equal(TimeSpan.FromSeconds(42), sut.IdleTime);
    }

    // =====================================================================
    // Poll — probe failures
    // =====================================================================

    [Fact]
    public void Poll_WhenProbeReturnsZero_TreatsAsActive()
    {
        using var sut = CreateSut(threshold: TimeSpan.FromMinutes(5));
        _probe.GetIdleTime().Returns(TimeSpan.Zero);

        sut.Poll();

        Assert.False(sut.IsIdle);
        Assert.Equal(TimeSpan.Zero, sut.IdleTime);
    }

    // =====================================================================
    // Events
    // =====================================================================

    [Fact]
    public void BecameIdle_MultipleSubscribers_AllInvoked()
    {
        using var sut = CreateSut(threshold: TimeSpan.FromMinutes(5));
        _probe.GetIdleTime().Returns(TimeSpan.FromMinutes(6));
        var a = 0;
        var b = 0;
        var c = 0;
        sut.BecameIdle += (_, _) => a++;
        sut.BecameIdle += (_, _) => b++;
        sut.BecameIdle += (_, _) => c++;

        sut.Poll();

        Assert.Equal(1, a);
        Assert.Equal(1, b);
        Assert.Equal(1, c);
    }

    [Fact]
    public void BecameActive_MultipleSubscribers_AllInvoked()
    {
        using var sut = CreateSut(threshold: TimeSpan.FromMinutes(5));
        _probe.GetIdleTime().Returns(TimeSpan.FromMinutes(6));
        sut.Poll(); // idle

        _probe.GetIdleTime().Returns(TimeSpan.FromSeconds(1));
        var a = 0;
        var b = 0;
        sut.BecameActive += (_, _) => a++;
        sut.BecameActive += (_, _) => b++;

        sut.Poll(); // active

        Assert.Equal(1, a);
        Assert.Equal(1, b);
    }

    [Fact]
    public void BecameIdle_UnsubscribedHandler_NotInvoked()
    {
        using var sut = CreateSut(threshold: TimeSpan.FromMinutes(5));
        _probe.GetIdleTime().Returns(TimeSpan.FromMinutes(6));
        var raised = 0;
        EventHandler handler = (_, _) => raised++;
        sut.BecameIdle += handler;
        sut.BecameIdle -= handler;

        sut.Poll();

        Assert.Equal(0, raised);
    }

    [Fact]
    public void BecameIdle_RaisedExactlyOncePerTransition()
    {
        using var sut = CreateSut(threshold: TimeSpan.FromMinutes(5));
        _probe.GetIdleTime().Returns(TimeSpan.FromMinutes(6));
        var raised = 0;
        sut.BecameIdle += (_, _) => raised++;

        sut.Poll(); // idle
        sut.Poll(); // still idle
        sut.Poll(); // still idle

        Assert.Equal(1, raised);
    }

    // =====================================================================
    // Threshold change → re-evaluation on next Poll
    // =====================================================================

    [Fact]
    public void ThresholdChange_CanTriggerIdleOnNextPoll()
    {
        using var sut = CreateSut(threshold: TimeSpan.FromMinutes(5));
        _probe.GetIdleTime().Returns(TimeSpan.FromMinutes(3)); // active (3min < 5min)
        sut.Poll();
        Assert.False(sut.IsIdle);

        var raised = 0;
        sut.BecameIdle += (_, _) => raised++;
        sut.Threshold = TimeSpan.FromMinutes(2); // now 3min > 2min → idle

        sut.Poll();

        Assert.Equal(1, raised);
        Assert.True(sut.IsIdle);
    }

    [Fact]
    public void ThresholdChange_CanTriggerActiveOnNextPoll()
    {
        using var sut = CreateSut(threshold: TimeSpan.FromMinutes(4));
        _probe.GetIdleTime().Returns(TimeSpan.FromMinutes(5)); // idle
        sut.Poll();
        Assert.True(sut.IsIdle);

        var raised = 0;
        sut.BecameActive += (_, _) => raised++;
        sut.Threshold = TimeSpan.FromMinutes(10); // now 5min < 10min → active

        sut.Poll();

        Assert.Equal(1, raised);
        Assert.False(sut.IsIdle);
    }

    // =====================================================================
    // IThreadContext marshaling
    // =====================================================================

    [Fact]
    public void RaiseEvent_WithThreadContext_MarshalsViaInvokeAsync()
    {
        var ctx = Substitute.For<IThreadContext>();
        // Configure the mock to run the action synchronously so we can assert the handler ran
        ctx.InvokeAsync(Arg.Any<Action>()).Returns(ci =>
        {
            ci.Arg<Action>().Invoke();
            return Task.CompletedTask;
        });

        using var sut = CreateSut(threshold: TimeSpan.FromMinutes(5), threadContext: ctx);
        _probe.GetIdleTime().Returns(TimeSpan.FromMinutes(6));
        var raised = 0;
        sut.BecameIdle += (_, _) => raised++;

        sut.Poll();

        // Event reached the handler (via the marshaled Action)
        Assert.Equal(1, raised);
        // And the thread context was actually used
        ctx.Received(1).InvokeAsync(Arg.Any<Action>());
    }

    [Fact]
    public void RaiseEvent_WithoutThreadContext_InvokesHandlerDirectly()
    {
        using var sut = CreateSut(threshold: TimeSpan.FromMinutes(5), threadContext: null);
        _probe.GetIdleTime().Returns(TimeSpan.FromMinutes(6));
        var raised = 0;
        sut.BecameIdle += (_, _) => raised++;

        sut.Poll();

        Assert.Equal(1, raised);
    }

    // =====================================================================
    // Dispose
    // =====================================================================

    [Fact]
    public void Dispose_IsIdempotent()
    {
        var sut = CreateSut();

        sut.Dispose();
        var ex = Record.Exception((Action)(() => sut.Dispose()));

        Assert.Null(ex);
    }

    [Fact]
    public void Dispose_SetsDisposedFlag()
    {
        var sut = CreateSut();

        sut.Dispose();

        Assert.True(sut.Disposed);
    }

    [Fact]
    public void Poll_AfterDispose_IsNoOp()
    {
        var sut = CreateSut();
        sut.Dispose();

        sut.Poll();

        // Disposed Poll() returns early before calling the probe
        _probe.DidNotReceive().GetIdleTime();
    }
}

public class IdleDetectionOptionsTests
{
    [Fact]
    public void IdleDetectionOptions_DefaultThresholdIs5Minutes()
    {
        var opts = new IdleDetectionOptions();
        Assert.Equal(TimeSpan.FromMinutes(5), opts.Threshold);
    }

    [Fact]
    public void IdleDetectionOptions_DefaultPollIntervalIs5Seconds()
    {
        var opts = new IdleDetectionOptions();
        Assert.Equal(TimeSpan.FromSeconds(5), opts.PollInterval);
    }
}
