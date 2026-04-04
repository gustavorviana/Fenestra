namespace Fenestra.Windows;

/// <summary>
/// Registers the application for toast notification background activation.
/// This enables the app to receive activation callbacks when a toast is clicked,
/// by registering a COM server in the registry and a runtime class factory.
/// Call <see cref="Register"/> once at app startup and <see cref="Unregister"/> to clean up.
/// </summary>
public interface IToastActivationRegistrar
{
    /// <summary>
    /// Registers the COM server in the registry and the runtime class factory.
    /// </summary>
    void Register();

    /// <summary>
    /// Removes the COM server registration and revokes the runtime class factory.
    /// </summary>
    void Unregister();

    /// <summary>
    /// Gets whether the activation infrastructure is currently registered.
    /// </summary>
    bool IsRegistered { get; }
}
