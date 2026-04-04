namespace Fenestra.Windows;

/// <summary>
/// Registers the application for toast notification background activation.
/// This enables toast actions to work even when the app is closed — Windows will relaunch it.
/// Call <see cref="Register"/> once at app startup and <see cref="Unregister"/> to clean up.
/// </summary>
public interface IToastActivationRegistrar
{
    /// <summary>
    /// Registers the COM server and Start Menu shortcut for toast background activation.
    /// </summary>
    void Register();

    /// <summary>
    /// Removes the COM server registration and Start Menu shortcut.
    /// </summary>
    void Unregister();

    /// <summary>
    /// Gets whether the activation infrastructure is currently registered.
    /// </summary>
    bool IsRegistered { get; }
}
