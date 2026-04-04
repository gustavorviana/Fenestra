namespace Fenestra.Core.Models;

/// <summary>
/// Contains application metadata including name, version, and host information.
/// </summary>
public class AppInfo
{
    /// <summary>
    /// Gets the display name of the application.
    /// </summary>
    public string AppName { get; }

    /// <summary>
    /// Gets the application identifier derived from the app name (alphanumeric characters only).
    /// </summary>
    public string AppId { get; }

    /// <summary>
    /// Gets the application version.
    /// </summary>
    public Version Version { get; }

    /// <summary>
    /// Initializes a new instance of <see cref="AppInfo"/> with the specified name, version, and host.
    /// </summary>
    public AppInfo(string appName, Version version)
    {
        if (string.IsNullOrEmpty(appName))
            throw new ArgumentException("App name cannot be null or empty.", nameof(appName));

        if (version == null)
            throw new ArgumentException("App id cannot be null or empty.", nameof(AppId));

        AppName = appName;
        AppId = new string([.. appName.Where(char.IsLetterOrDigit)]);
        Version = version;
    }
}
