namespace Fenestra.Core.Models;

public class FenestraExceptionContext
{
    public Exception Exception { get; }
    public bool IsCritical { get; }
    public bool Handled { get; set; }

    public FenestraExceptionContext(Exception exception, bool isCritical)
    {
        Exception = exception;
        IsCritical = isCritical;
    }
}
