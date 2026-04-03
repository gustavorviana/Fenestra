using Fenestra.Core.Exceptions;

namespace Fenestra.Tests.Core.Exceptions;

public class LaunchWindowExceptionTests
{
    [Fact]
    public void Constructor_SetsWindowType()
    {
        var ex = new LaunchWindowException(typeof(string));

        Assert.Equal(typeof(string), ex.WindowType);
        Assert.Contains("System.String", ex.Message);
    }

    [Fact]
    public void Constructor_WithInnerException_PreservesIt()
    {
        var inner = new InvalidOperationException("inner");
        var ex = new LaunchWindowException(typeof(string), inner);

        Assert.Same(inner, ex.InnerException);
        Assert.Equal(typeof(string), ex.WindowType);
    }
}
