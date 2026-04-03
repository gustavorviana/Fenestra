using Fenestra.Core.Models;

namespace Fenestra.Core;

/// <summary>
/// Handles unhandled exceptions in the application.
/// </summary>
public interface IExceptionHandler
{
    /// <summary>
    /// Processes an unhandled exception described by the given context.
    /// </summary>
    void Handle(FenestraExceptionContext context);
}
