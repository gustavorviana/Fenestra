namespace Fenestra.Core.Models;

/// <summary>
/// Base application metadata. Platform-independent.
/// For Windows-specific metadata (AppGuid, MSIX), see <c>WindowsAppInfo</c>.
/// </summary>
public class AppInfo : IAppInfo
{
    /// <inheritdoc />
    public string AppName { get; }

    /// <inheritdoc />
    public string AppId { get; }

    /// <inheritdoc />
    public Version Version { get; }

    /// <summary>
    /// Initializes a new <see cref="AppInfo"/> with AppId derived from <paramref name="appName"/> (alphanumeric only).
    /// </summary>
    public AppInfo(string appName, Version version)
    {
        if (string.IsNullOrEmpty(appName))
            throw new ArgumentException("App name cannot be null or empty.", nameof(appName));

        AppName = appName;
        AppId = new string(appName.Where(char.IsLetterOrDigit).ToArray());
        Version = version ?? throw new ArgumentException("Version cannot be null.", nameof(version));
    }

    /// <summary>
    /// Initializes a new <see cref="AppInfo"/> with an explicit <paramref name="appId"/>.
    /// </summary>
    public AppInfo(string appName, string appId, Version version)
    {
        if (string.IsNullOrEmpty(appName))
            throw new ArgumentException("App name cannot be null or empty.", nameof(appName));
        if (string.IsNullOrEmpty(appId))
            throw new ArgumentException("App id cannot be null or empty.", nameof(appId));

        AppName = appName;
        AppId = appId;
        Version = version ?? throw new ArgumentException("Version cannot be null.", nameof(version));
    }
}
