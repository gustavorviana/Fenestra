namespace Fenestra.Windows.Services;

/// <summary>
/// Configures the process-wide AppUserModelID (AUMID) and the matching Start Menu
/// shortcut required by Windows for taskbar-grouped features such as toast notifications,
/// Jump Lists, taskbar overlay icons, and pinned-shortcut identity.
/// </summary>
/// <remarks>
/// Centralized here because multiple Fenestra features (toasts, jump list, taskbar
/// overlay) share the same prerequisites. Calling <see cref="EnsureRegistered"/> is
/// idempotent — it's a no-op once the AUMID is set and the shortcut is up to date.
/// </remarks>
public interface IAumidRegistrationManager
{
    /// <summary>
    /// Sets the explicit AUMID for the current process and creates/updates the Start
    /// Menu shortcut carrying the same AUMID. Skips entirely for packaged (MSIX) apps
    /// whose identity is provided by the AppX manifest.
    /// </summary>
    void EnsureRegistered();

    /// <summary>
    /// Returns <c>true</c> when either the AUMID has not yet been applied or the Start
    /// Menu shortcut is missing/stale and would need rewriting.
    /// </summary>
    bool NeedsRegistration();
}
