using Fenestra.Core.Models;

namespace Fenestra.Tests.Core.Models;

public class FenestraExceptionContextTests
{
    [Fact]
    public void Constructor_SetsExceptionAndIsCritical()
    {
        var ex = new InvalidOperationException("test");
        var context = new FenestraExceptionContext(ex, isCritical: true);

        Assert.Same(ex, context.Exception);
        Assert.True(context.IsCritical);
    }

    [Fact]
    public void Handled_DefaultsFalse()
    {
        var context = new FenestraExceptionContext(new Exception(), isCritical: false);

        Assert.False(context.Handled);
    }

    [Fact]
    public void Handled_CanBeSetToTrue()
    {
        var context = new FenestraExceptionContext(new Exception(), isCritical: false);

        context.Handled = true;

        Assert.True(context.Handled);
    }
}
