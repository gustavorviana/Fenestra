namespace Fenestra.Windows.Services;

public interface IWindowsNotificationRegistrationManager
{
    void EnsureRegistered();
    bool NeedsRegistration();
}