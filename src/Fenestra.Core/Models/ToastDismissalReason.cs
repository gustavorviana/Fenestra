namespace Fenestra.Core.Models;

/// <summary>
/// Specifies the reason a toast notification was dismissed.
/// </summary>
public enum ToastDismissalReason
{
    UserCanceled = 0,
    ApplicationHidden = 1,
    TimedOut = 2
}
