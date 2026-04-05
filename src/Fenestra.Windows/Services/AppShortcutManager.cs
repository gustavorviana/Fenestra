using Fenestra.Core.Models;
using Fenestra.Windows.Native;
using System.Diagnostics;

namespace Fenestra.Windows.Services;

/// <summary>
/// Manages the Start Menu shortcut for the application.
/// Provides shared shortcut creation logic for notification registration and toast activation.
/// </summary>
internal class AppShortcutManager
{
    private readonly AppInfo _appInfo;

    public string ExePath { get; }
    public string ShortcutPath { get; }

    public AppShortcutManager(AppInfo appInfo)
    {
        _appInfo = appInfo;

        ExePath = GetCurrentExecutablePath();
        ShortcutPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "Microsoft", "Windows", "Start Menu", "Programs",
            $"{appInfo.AppName}.lnk");
    }

    /// <summary>
    /// Creates or updates the Start Menu shortcut with target, AUMID, and optional CLSID.
    /// </summary>
    public void CreateOrUpdateShortcut(Guid? toastActivatorClsid = null)
    {
        var workingDir = Path.GetDirectoryName(ExePath) ?? AppDomain.CurrentDomain.BaseDirectory;

        using var link = ShellLink.Create(ShortcutPath);
        link.TargetPath = ExePath;
        link.WorkingDirectory = workingDir;
        link.Arguments = "";
        link.Description = _appInfo.AppName;
        link.SetIconLocation(ExePath);
        link.AppUserModelId = _appInfo.AppId;

        if (toastActivatorClsid.HasValue)
            link.ToastActivatorClsid = toastActivatorClsid.Value;

        link.Save();
    }

    /// <summary>
    /// Checks whether the shortcut needs to be created or updated.
    /// </summary>
    public bool NeedsUpdate()
    {
        try
        {
            using var link = ShellLink.Open(ShortcutPath);
            if (link == null) return true;

            if (!PathsEqual(link.TargetPath, ExePath))
                return true;

            if (!string.Equals(link.AppUserModelId, _appInfo.AppId, StringComparison.Ordinal))
                return true;

            return false;
        }
        catch
        {
            return true;
        }
    }

    internal static string GetCurrentExecutablePath()
    {
        using var process = Process.GetCurrentProcess();
        var path = process.MainModule?.FileName;
        if (string.IsNullOrWhiteSpace(path))
            throw new InvalidOperationException("Could not determine the current executable path.");
        return Path.GetFullPath(path);
    }

    private static bool PathsEqual(string left, string right)
    {
        if (string.IsNullOrWhiteSpace(left) || string.IsNullOrWhiteSpace(right))
            return false;

        return string.Equals(
            Path.GetFullPath(left).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar),
            Path.GetFullPath(right).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar),
            StringComparison.OrdinalIgnoreCase);
    }
}
