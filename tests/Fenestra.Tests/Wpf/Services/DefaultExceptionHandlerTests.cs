using Fenestra.Core.Models;
using Fenestra.Wpf.Services;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace Fenestra.Tests.Wpf.Services;

public class DefaultExceptionHandlerTests
{
    private readonly ILogger<DefaultExceptionHandler> _logger = Substitute.For<ILogger<DefaultExceptionHandler>>();
    private readonly DefaultExceptionHandler _handler;

    public DefaultExceptionHandlerTests()
    {
        _handler = new DefaultExceptionHandler(_logger);
    }

    [Fact]
    public void Handle_NonCritical_SetsHandledTrue()
    {
        var context = new FenestraExceptionContext(new Exception("test"), isCritical: false);

        _handler.Handle(context);

        Assert.True(context.Handled);
    }

    [Fact]
    public void Handle_Critical_SetsHandledTrue()
    {
        var context = new FenestraExceptionContext(new Exception("test"), isCritical: true);

        _handler.Handle(context);

        Assert.True(context.Handled);
    }

    [Fact]
    public void Handle_NonCritical_LogsError()
    {
        var exception = new Exception("test error");
        var context = new FenestraExceptionContext(exception, isCritical: false);

        _handler.Handle(context);

        _logger.Received().Log(
            LogLevel.Error,
            Arg.Any<EventId>(),
            Arg.Any<object>(),
            exception,
            Arg.Any<Func<object, Exception?, string>>());
    }

    [Fact]
    public void Handle_Critical_LogsCritical()
    {
        var exception = new Exception("critical error");
        var context = new FenestraExceptionContext(exception, isCritical: true);

        _handler.Handle(context);

        _logger.Received().Log(
            LogLevel.Critical,
            Arg.Any<EventId>(),
            Arg.Any<object>(),
            exception,
            Arg.Any<Func<object, Exception?, string>>());
    }
}
