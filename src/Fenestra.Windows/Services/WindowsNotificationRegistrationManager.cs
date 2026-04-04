using Fenestra.Core.Models;
using Fenestra.Windows.Native;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace Fenestra.Windows.Services;

public sealed class WindowsNotificationRegistrationManager : IWindowsNotificationRegistrationManager
{
    private readonly AppInfo _info;

    public WindowsNotificationRegistrationManager(AppInfo info)
    {
        _info = info ?? throw new ArgumentNullException(nameof(info));
    }

    public void EnsureRegistered()
    {
        if (HasPackageIdentity())
            return;

        SetCurrentProcessAppUserModelId(_info.AppId);

        if (!NeedsRegistration())
            return;

        CreateOrUpdateShortcut();
    }

    public bool NeedsRegistration()
    {
        if (HasPackageIdentity())
            return false;

        try
        {
            using var link = ShellLink.Open(GetShortcutPath());
            if (link == null) return true;

            var currentPath = GetCurrentExecutablePath();
            if (!PathsEqual(link.TargetPath, currentPath))
                return true;

            var shortcutAppId = link.AppUserModelId;
            if (!string.Equals(shortcutAppId, _info.AppId, StringComparison.Ordinal))
                return true;

            return false;
        }
        catch
        {
            return true;
        }
    }

    private void CreateOrUpdateShortcut()
    {
        var executablePath = GetCurrentExecutablePath();
        var workingDirectory = Path.GetDirectoryName(executablePath) ?? AppDomain.CurrentDomain.BaseDirectory;

        using var link = ShellLink.Create(GetShortcutPath());
        link.TargetPath = executablePath;
        link.WorkingDirectory = workingDirectory;
        link.Arguments = "";
        link.Description = _info.AppName;
        link.SetIconLocation(executablePath);
        link.AppUserModelId = _info.AppId;
        link.Save();
    }

    private string GetShortcutPath()
    {
        return Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "Microsoft", "Windows", "Start Menu", "Programs",
            $"{_info.AppName}.lnk");
    }

    private static bool HasPackageIdentity()
    {
        var length = 0;
        var result = GetCurrentPackageFullName(ref length, null);
        return result != APPMODEL_ERROR_NO_PACKAGE;
    }

    private static void SetCurrentProcessAppUserModelId(string appUserModelId)
    {
        var hr = SetCurrentProcessExplicitAppUserModelID(appUserModelId);
        if (hr < 0) Marshal.ThrowExceptionForHR(hr);
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

    private const int APPMODEL_ERROR_NO_PACKAGE = 15700;

    [DllImport("shell32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern int SetCurrentProcessExplicitAppUserModelID(string appID);

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
    private static extern int GetCurrentPackageFullName(ref int packageFullNameLength, StringBuilder? packageFullName);
}
