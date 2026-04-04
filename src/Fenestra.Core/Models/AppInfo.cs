using System.Reflection;
using System.Runtime.InteropServices;

namespace Fenestra.Core.Models;

/// <summary>
/// Contains application metadata including name, version, and host information.
/// For packaged apps (MSIX/AppX), the metadata is resolved from the package identity.
/// </summary>
public class AppInfo
{
    /// <summary>
    /// Gets the display name of the application.
    /// </summary>
    public string AppName { get; }

    /// <summary>
    /// Gets the application identifier.
    /// For classic (non-packaged) apps, this is either user-defined or derived from the app name (alphanumeric only).
    /// For packaged apps, this is the AUMID (e.g. <c>PackageFamilyName!ApplicationId</c>).
    /// </summary>
    public string AppId { get; }

    /// <summary>
    /// Gets the application version.
    /// </summary>
    public Version Version { get; }

    /// <summary>
    /// Gets whether the application is running as a packaged app (MSIX/AppX).
    /// </summary>
    public bool IsPackagedApp { get; }

    /// <summary>
    /// Gets the package family name when running as a packaged app; otherwise <c>null</c>.
    /// </summary>
    public string? PackageFamilyName { get; }

    /// <summary>
    /// Gets the stable GUID that uniquely identifies this application installation.
    /// Resolved from the assembly <see cref="GuidAttribute"/>, or persisted in the registry on first run.
    /// </summary>
    public Guid AppGuid { get; internal set; }

    /// <summary>
    /// Initializes a new instance of <see cref="AppInfo"/> for a classic (non-packaged) application.
    /// The <see cref="AppId"/> is derived from <paramref name="appName"/> (alphanumeric characters only).
    /// </summary>
    public AppInfo(string appName, Version version)
    {
        if (string.IsNullOrEmpty(appName))
            throw new ArgumentException("App name cannot be null or empty.", nameof(appName));

        if (version == null)
            throw new ArgumentException("Version cannot be null.", nameof(version));

        AppName = appName;
        AppId = new string([.. appName.Where(char.IsLetterOrDigit)]);
        Version = version;
    }

    /// <summary>
    /// Initializes a new instance of <see cref="AppInfo"/> for a classic (non-packaged) application
    /// with an explicit <paramref name="appId"/>.
    /// </summary>
    public AppInfo(string appName, string appId, Version version)
    {
        if (string.IsNullOrEmpty(appName))
            throw new ArgumentException("App name cannot be null or empty.", nameof(appName));

        if (string.IsNullOrEmpty(appId))
            throw new ArgumentException("App id cannot be null or empty.", nameof(appId));

        if (version == null)
            throw new ArgumentException("Version cannot be null.", nameof(version));

        AppName = appName;
        AppId = appId;
        Version = version;
    }

    /// <summary>
    /// Initializes a new instance of <see cref="AppInfo"/> for a packaged (MSIX/AppX) application.
    /// </summary>
    public AppInfo(string appName, string aumid, Version version, string packageFamilyName)
    {
        if (string.IsNullOrEmpty(appName))
            throw new ArgumentException("App name cannot be null or empty.", nameof(appName));

        if (string.IsNullOrEmpty(aumid))
            throw new ArgumentException("AUMID cannot be null or empty.", nameof(aumid));

        if (version == null)
            throw new ArgumentException("Version cannot be null.", nameof(version));

        if (string.IsNullOrEmpty(packageFamilyName))
            throw new ArgumentException("Package family name cannot be null or empty.", nameof(packageFamilyName));

        AppName = appName;
        AppId = aumid;
        Version = version;
        IsPackagedApp = true;
        PackageFamilyName = packageFamilyName;
    }
}
