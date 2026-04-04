namespace Fenestra.Toast.Windows.Services;

public interface IWindowsNotificationRegistrationManager
{
    void EnsureRegistered();
    bool NeedsRegistration();
}