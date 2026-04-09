using Fenestra.Core;

namespace Fenestra.Tests.Core;

public class EventBusTests
{
    private record TestMessage(string Text);
    private record OtherMessage(int Value);

    [Fact]
    public async Task PublishAsync_NoSubscribers_DoesNotThrow()
    {
        var bus = new EventBus();

        await bus.PublishAsync(new TestMessage("hi"));
    }

    [Fact]
    public async Task PublishAsync_InvokesSubscribedHandler()
    {
        var bus = new EventBus();
        TestMessage? received = null;
        bus.On<TestMessage>(msg => { received = msg; return Task.CompletedTask; });

        await bus.PublishAsync(new TestMessage("hello"));

        Assert.NotNull(received);
        Assert.Equal("hello", received!.Text);
    }

    [Fact]
    public async Task PublishAsync_InvokesMultipleHandlersInOrder()
    {
        var bus = new EventBus();
        var calls = new List<int>();
        bus.On<TestMessage>(_ => { calls.Add(1); return Task.CompletedTask; });
        bus.On<TestMessage>(_ => { calls.Add(2); return Task.CompletedTask; });
        bus.On<TestMessage>(_ => { calls.Add(3); return Task.CompletedTask; });

        await bus.PublishAsync(new TestMessage("x"));

        Assert.Equal(new[] { 1, 2, 3 }, calls);
    }

    [Fact]
    public async Task PublishAsync_OnlyInvokesHandlersOfMatchingType()
    {
        var bus = new EventBus();
        var testReceived = 0;
        var otherReceived = 0;
        bus.On<TestMessage>(_ => { testReceived++; return Task.CompletedTask; });
        bus.On<OtherMessage>(_ => { otherReceived++; return Task.CompletedTask; });

        await bus.PublishAsync(new TestMessage("x"));

        Assert.Equal(1, testReceived);
        Assert.Equal(0, otherReceived);
    }

    [Fact]
    public async Task PublishAsync_AwaitsHandlersSequentially()
    {
        var bus = new EventBus();
        var sequence = new List<string>();
        bus.On<TestMessage>(async _ =>
        {
            sequence.Add("h1-start");
            await Task.Delay(10);
            sequence.Add("h1-end");
        });
        bus.On<TestMessage>(_ =>
        {
            sequence.Add("h2");
            return Task.CompletedTask;
        });

        await bus.PublishAsync(new TestMessage("x"));

        Assert.Equal(new[] { "h1-start", "h1-end", "h2" }, sequence);
    }

    [Fact]
    public void On_ReturnsNonNullSubscription()
    {
        var bus = new EventBus();

        var subscription = bus.On<TestMessage>(_ => Task.CompletedTask);

        Assert.NotNull(subscription);
    }

    [Fact]
    public async Task Dispose_Subscription_UnregistersHandler()
    {
        var bus = new EventBus();
        var received = 0;
        var subscription = bus.On<TestMessage>(_ => { received++; return Task.CompletedTask; });

        subscription.Dispose();
        await bus.PublishAsync(new TestMessage("x"));

        Assert.Equal(0, received);
    }

    [Fact]
    public async Task Dispose_OneSubscription_DoesNotAffectOthers()
    {
        var bus = new EventBus();
        var first = 0;
        var second = 0;
        var sub1 = bus.On<TestMessage>(_ => { first++; return Task.CompletedTask; });
        bus.On<TestMessage>(_ => { second++; return Task.CompletedTask; });

        sub1.Dispose();
        await bus.PublishAsync(new TestMessage("x"));

        Assert.Equal(0, first);
        Assert.Equal(1, second);
    }

    [Fact]
    public void Dispose_Subscription_IsIdempotent()
    {
        var bus = new EventBus();
        var sub = bus.On<TestMessage>(_ => Task.CompletedTask);

        sub.Dispose();
        var ex = Record.Exception(() => sub.Dispose());

        Assert.Null(ex);
    }

    [Fact]
    public async Task PublishAsync_HandlerException_PropagatesToCaller()
    {
        var bus = new EventBus();
        bus.On<TestMessage>(_ => throw new InvalidOperationException("boom"));

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            bus.PublishAsync(new TestMessage("x")));
    }

    [Fact]
    public async Task PublishAsync_DispatchesSnapshotOfHandlers_SafeForSubscribeDuringPublish()
    {
        var bus = new EventBus();
        var lateSubscriberCalled = false;
        bus.On<TestMessage>(_ =>
        {
            bus.On<TestMessage>(__ => { lateSubscriberCalled = true; return Task.CompletedTask; });
            return Task.CompletedTask;
        });

        await bus.PublishAsync(new TestMessage("x"));

        // Late subscriber must not receive the current publish (snapshot was taken up front)
        Assert.False(lateSubscriberCalled);
    }

    [Fact]
    public async Task On_SameHandlerTwice_InvokedTwice()
    {
        var bus = new EventBus();
        var count = 0;
        BusHandler<TestMessage> handler = _ => { count++; return Task.CompletedTask; };
        bus.On(handler);
        bus.On(handler);

        await bus.PublishAsync(new TestMessage("x"));

        Assert.Equal(2, count);
    }
}
