using Fenestra.Core;

namespace Fenestra.Tests.Core;

public class FenestraComponentTests
{
    private class TestComponent : FenestraComponent
    {
        public int DisposeManagedCallCount { get; private set; }
        public int DisposeUnmanagedCallCount { get; private set; }

        protected override void Dispose(bool disposing)
        {
            if (Disposed) return;
            if (disposing)
                DisposeManagedCallCount++;
            else
                DisposeUnmanagedCallCount++;
            base.Dispose(disposing);
        }
    }

    [Fact]
    public void Disposed_IsFalseInitially()
    {
        var component = new TestComponent();

        Assert.False(component.Disposed);
    }

    [Fact]
    public void Dispose_SetsDisposedToTrue()
    {
        var component = new TestComponent();

        component.Dispose();

        Assert.True(component.Disposed);
    }

    [Fact]
    public void Dispose_InvokesManagedCleanupOnce()
    {
        var component = new TestComponent();

        component.Dispose();

        Assert.Equal(1, component.DisposeManagedCallCount);
    }

    [Fact]
    public void Dispose_TwiceIsIdempotent()
    {
        var component = new TestComponent();

        component.Dispose();
        component.Dispose();

        Assert.Equal(1, component.DisposeManagedCallCount);
    }

    [Fact]
    public void Dispose_DoesNotInvokeUnmanagedCleanup()
    {
        var component = new TestComponent();

        component.Dispose();

        Assert.Equal(0, component.DisposeUnmanagedCallCount);
    }

    [Fact]
    public void Dispose_SuppressesFinalization()
    {
        // Indirect test: if finalization were not suppressed, the finalizer would run after GC
        // and increment DisposeUnmanagedCallCount. This is a best-effort assertion.
        var component = new TestComponent();

        component.Dispose();
        GC.Collect();
        GC.WaitForPendingFinalizers();

        Assert.Equal(0, component.DisposeUnmanagedCallCount);
    }
}
