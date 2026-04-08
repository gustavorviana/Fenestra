using Fenestra.Core.Models;

namespace Fenestra.Windows.Models;

/// <summary>
/// Windows-specific application metadata extending <see cref="AppInfo"/>.
/// Includes AppGuid (persisted in registry) and MSIX package identity.
/// </summary>
public class WindowsAppInfo : AppInfo
{
    /// <summary>
    /// Gets the stable GUID that uniquely identifies this application installation.
    /// Persisted in the Windows Registry on first run.
    /// </summary>
    public Guid AppGuid { get; internal set; }

    /// <summary>
    /// Gets whether the application is running as a packaged app (MSIX/AppX).
    /// </summary>
    public bool IsPackagedApp { get; }

    /// <summary>
    /// Gets the package family name when running as a packaged app; otherwise <c>null</c>.
    /// </summary>
    public string? PackageFamilyName { get; }

    /// <summary>
    /// Initializes a new <see cref="WindowsAppInfo"/> for a classic (non-packaged) application.
    /// </summary>
    public WindowsAppInfo(string appName, Version version)
        : base(appName, version) { }

    /// <summary>
    /// Initializes a new <see cref="WindowsAppInfo"/> for a classic (non-packaged) application with explicit AppId.
    /// </summary>
    public WindowsAppInfo(string appName, string appId, Version version)
        : base(appName, appId, version) { }

    /// <summary>
    /// Initializes a new <see cref="WindowsAppInfo"/> for a packaged (MSIX/AppX) application.
    /// </summary>
    public WindowsAppInfo(string appName, string aumid, Version version, string packageFamilyName)
        : base(appName, aumid, version)
    {
        if (string.IsNullOrEmpty(packageFamilyName))
            throw new ArgumentException("Package family name cannot be null or empty.", nameof(packageFamilyName));

        IsPackagedApp = true;
        PackageFamilyName = packageFamilyName;
    }
}
