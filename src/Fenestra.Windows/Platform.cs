using System.Runtime.InteropServices;

namespace Fenestra.Windows;

/// <summary>
/// Provides platform detection for Fenestra.Windows services.
/// </summary>
public static class Platform
{
    private static readonly bool _isWindows;
    private static readonly Version _osVersion;

    static Platform()
    {
#if NET6_0_OR_GREATER
        _isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
#else
        _isWindows = true;
#endif
        _osVersion = Environment.OSVersion.Version;
    }

    /// <summary>
    /// Gets whether the current OS is Windows.
    /// </summary>
    public static bool IsWindows => _isWindows;

    /// <summary>
    /// Gets the Windows version. Only meaningful when <see cref="IsWindows"/> is true.
    /// </summary>
    public static Version OsVersion => _osVersion;

    /// <summary>
    /// Gets whether the current OS is Windows 10 or later (build 10240+).
    /// </summary>
    public static bool IsWindows10OrLater => _isWindows && _osVersion.Major >= 10;

    /// <summary>
    /// Throws <see cref="PlatformNotSupportedException"/> if the current OS is not Windows.
    /// </summary>
    public static void EnsureWindows()
    {
        if (!_isWindows)
            throw new PlatformNotSupportedException("This feature requires Windows.");
    }

    /// <summary>
    /// Throws <see cref="PlatformNotSupportedException"/> if the current OS is not Windows 10 or later.
    /// </summary>
    public static void EnsureWindows10()
    {
        EnsureWindows();

        if (!IsWindows10OrLater)
            throw new PlatformNotSupportedException("This feature requires Windows 10 or later.");
    }
}
