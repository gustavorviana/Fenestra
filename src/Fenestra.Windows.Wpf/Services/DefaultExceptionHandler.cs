using Fenestra.Core;
using Fenestra.Core.Models;
using Microsoft.Extensions.Logging;

namespace Fenestra.Wpf.Services;

internal class DefaultExceptionHandler : IExceptionHandler
{
    private readonly ILogger<DefaultExceptionHandler> _logger;
    private readonly IDialogService _dialogService;

    public DefaultExceptionHandler(ILogger<DefaultExceptionHandler> logger, IDialogService dialogService)
    {
        _logger = logger;
        _dialogService = dialogService;
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
            _dialogService.ShowMessage(
                message: "An unexpected error occurred. The application may need to restart.",
                title: "Error",
                buttons: FenestraMessageButton.OK,
                icon: FenestraMessageIcon.Error);
        }
        catch (Exception dialogException)
        {
            _logger.LogError(dialogException, "Failed to display exception dialog to the user.");
        }

        context.Handled = true;
    }
}
