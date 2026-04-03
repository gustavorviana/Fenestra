using Fenestra.Core.Models;

namespace Fenestra.Core;

public interface IExceptionHandler
{
    void Handle(FenestraExceptionContext context);
}
