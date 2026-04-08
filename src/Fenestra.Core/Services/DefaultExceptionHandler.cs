using Fenestra.Core.Models;
using Microsoft.Extensions.Logging;

namespace Fenestra.Core.Services;

/// <summary>
/// Default exception handler that logs the exception.
/// Platform-specific builders can replace this with a UI-aware handler.
/// </summary>
internal class DefaultExceptionHandler : IExceptionHandler
{
    private readonly ILogger<DefaultExceptionHandler> _logger;

    public DefaultExceptionHandler(ILogger<DefaultExceptionHandler> logger)
    {
        _logger = logger;
    }

    public void Handle(FenestraExceptionContext context)
    {
        if (context.IsCritical)
            _logger.LogCritical(context.Exception, "An unhandled critical exception occurred.");
        else
            _logger.LogError(context.Exception, "An unhandled exception occurred.");

        context.Handled = true;
    }
}
