using System.Windows;
using Fenestra.Core;
using Fenestra.Core.Models;
using Microsoft.Extensions.Logging;

namespace Fenestra.Wpf.Services;

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
        {
            _logger.LogCritical(context.Exception, "An unhandled critical exception occurred.");
        }
        else
        {
            _logger.LogError(context.Exception, "An unhandled exception occurred.");
        }

        try
        {
            MessageBox.Show(
                "An unexpected error occurred. The application may need to restart.",
                "Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
        catch
        {
            // If MessageBox fails (e.g., no UI thread), silently continue
        }

        context.Handled = true;
    }
}
