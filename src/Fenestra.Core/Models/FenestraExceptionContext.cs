namespace Fenestra.Core.Models;

/// <summary>
/// Provides context about an unhandled exception for the exception handler.
/// </summary>
public class FenestraExceptionContext
{
    /// <summary>
    /// Gets the exception that was thrown.
    /// </summary>
    public Exception Exception { get; }

    /// <summary>
    /// Gets whether the exception is considered critical (application cannot continue).
    /// </summary>
    public bool IsCritical { get; }

    /// <summary>
    /// Gets or sets whether the exception has been handled by the handler.
    /// </summary>
    public bool Handled { get; set; }

    /// <summary>
    /// Initializes a new instance with the specified exception and criticality flag.
    /// </summary>
    public FenestraExceptionContext(Exception exception, bool isCritical)
    {
        Exception = exception;
        IsCritical = isCritical;
    }
}
