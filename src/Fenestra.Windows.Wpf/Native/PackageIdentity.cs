using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Xml.Linq;
using Fenestra.Core.Models;

namespace Fenestra.Wpf.Native;

/// <summary>
/// Detects MSIX/AppX package identity and resolves application metadata from the package manifest.
/// </summary>
internal static class PackageIdentity
{
    private const int APPMODEL_ERROR_NO_PACKAGE = 15700;
    private const int ERROR_INSUFFICIENT_BUFFER = 122;

    /// <summary>
    /// Returns <c>true</c> when the current process is running inside a packaged app (MSIX/AppX).
    /// </summary>
    public static bool HasPackageIdentity()
    {
        var length = 0;
        return GetCurrentPackageFullName(ref length, null) != APPMODEL_ERROR_NO_PACKAGE;
    }

    /// <summary>
    /// Builds an <see cref="AppInfo"/> from the current package identity, or returns <c>null</c>
    /// when the process is not packaged.
    /// </summary>
    public static AppInfo? TryCreateAppInfo()
    {
        if (!HasPackageIdentity())
            return null;

        var fullName = GetPackageFullName();
        var familyName = GetPackageFamilyName();

        if (fullName == null || familyName == null)
            return null;

        // Full name format: {Name}_{Version}_{Arch}_{ResourceId}_{PublisherId}
        var parts = fullName.Split('_');
        var identityName = parts[0];
        var version = parts.Length > 1 && Version.TryParse(parts[1], out var v)
            ? v
            : new Version(1, 0);

        var displayName = identityName;
        var applicationId = "App";

        var packagePath = GetPackagePath();
        if (packagePath != null)
        {
            var manifestPath = Path.Combine(packagePath, "AppxManifest.xml");
            if (File.Exists(manifestPath))
                TryParseManifest(manifestPath, ref displayName, ref applicationId);
        }

        var aumid = $"{familyName}!{applicationId}";
        return new AppInfo(displayName, aumid, version, familyName);
    }

    private static void TryParseManifest(string manifestPath, ref string displayName, ref string applicationId)
    {
        try
        {
            var doc = XDocument.Load(manifestPath);
            if (doc.Root == null) return;

            var ns = doc.Root.GetDefaultNamespace();

            // Application Id (used to build the AUMID)
            var appElement = doc.Root
                .Element(ns + "Applications")?
                .Element(ns + "Application");

            var appId = appElement?.Attribute("Id")?.Value;
            if (!string.IsNullOrEmpty(appId))
                applicationId = appId!;

            // Prefer the VisualElements display name (what the user sees in Start Menu)
            if (appElement != null)
            {
                var uapNs = doc.Root.GetNamespaceOfPrefix("uap") ?? ns;
                var visualName = appElement
                    .Element(uapNs + "VisualElements")?
                    .Attribute("DisplayName")?
                    .Value;

                if (IsResolvableName(visualName))
                {
                    displayName = visualName!;
                    return;
                }
            }

            // Fall back to the package-level DisplayName
            var packageName = doc.Root
                .Element(ns + "Properties")?
                .Element(ns + "DisplayName")?
                .Value;

            if (IsResolvableName(packageName))
                displayName = packageName!;
        }
        catch
        {
            // Manifest parsing is best-effort; identity name is used as fallback.
        }
    }

    /// <summary>
    /// Returns <c>true</c> when the name is a concrete string (not an <c>ms-resource:</c> reference).
    /// </summary>
    private static bool IsResolvableName(string? name)
        => !string.IsNullOrEmpty(name) && !name!.StartsWith("ms-resource:", StringComparison.OrdinalIgnoreCase);

    // --- P/Invoke helpers --------------------------------------------------------

    private static string? GetPackageFullName()
    {
        var length = 0;
        if (GetCurrentPackageFullName(ref length, null) != ERROR_INSUFFICIENT_BUFFER || length <= 0)
            return null;

        var sb = new StringBuilder(length);
        return GetCurrentPackageFullName(ref length, sb) == 0 ? sb.ToString() : null;
    }

    private static string? GetPackageFamilyName()
    {
        var length = 0;
        if (GetCurrentPackageFamilyName(ref length, null) != ERROR_INSUFFICIENT_BUFFER || length <= 0)
            return null;

        var sb = new StringBuilder(length);
        return GetCurrentPackageFamilyName(ref length, sb) == 0 ? sb.ToString() : null;
    }

    private static string? GetPackagePath()
    {
        var length = 0;
        if (GetCurrentPackagePath(ref length, null) != ERROR_INSUFFICIENT_BUFFER || length <= 0)
            return null;

        var sb = new StringBuilder(length);
        return GetCurrentPackagePath(ref length, sb) == 0 ? sb.ToString() : null;
    }

    // --- P/Invoke declarations ---------------------------------------------------

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
    private static extern int GetCurrentPackageFullName(ref int length, StringBuilder? fullName);

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
    private static extern int GetCurrentPackageFamilyName(ref int length, StringBuilder? familyName);

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
    private static extern int GetCurrentPackagePath(ref int length, StringBuilder? path);
}
