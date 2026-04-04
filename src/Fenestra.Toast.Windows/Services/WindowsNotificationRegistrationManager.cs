using Fenestra.Core.Models;
using Fenestra.Toast.Windows.Native;
using Fenestra.Toast.Windows.Native.Interfaces;
using Fenestra.Toast.Windows.Native.Structs;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace Fenestra.Toast.Windows.Services;

public sealed class WindowsNotificationRegistrationManager : IWindowsNotificationRegistrationManager
{
    private static readonly Guid ShellLinkId = Guid.Parse("00021401-0000-0000-C000-000000000046");
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

        var shortcutPath = GetShortcutPath();
        if (!File.Exists(shortcutPath))
            return true;

        try
        {
            var currentExecutablePath = GetCurrentExecutablePath();

            var shellLink = ComFactory.Create<IShellLinkW>(ShellLinkId);
            var persistFile = (IPersistFile)shellLink;
            persistFile.Load(shortcutPath, NativeMethods.STGM_READ);

            var shortcutTargetPath = GetShortcutTargetPath(shellLink);
            if (!PathsEqual(shortcutTargetPath, currentExecutablePath))
                return true;

            var propertyStore = (IPropertyStore)shellLink;
            var propertyKey = PropertyKeys.AppUserModelId;

            ThrowIfFailed(propertyStore.GetValue(ref propertyKey, out var appIdValue));

            var shortcutAppId = PropVariantHelper.GetStringAndClear(ref appIdValue);

            Marshal.ReleaseComObject(shellLink);
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
        var shortcutPath = GetShortcutPath();
        var executablePath = GetCurrentExecutablePath();
        var workingDirectory = Path.GetDirectoryName(executablePath) ?? AppDomain.CurrentDomain.BaseDirectory;

        var shortcutDirectory = Path.GetDirectoryName(shortcutPath)
            ?? throw new InvalidOperationException("Shortcut directory could not be determined.");

        Directory.CreateDirectory(shortcutDirectory);

        var shellLink = ComFactory.Create<IShellLinkW>(ShellLinkId);
        shellLink.SetPath(executablePath);
        shellLink.SetWorkingDirectory(workingDirectory);
        shellLink.SetArguments(string.Empty);
        shellLink.SetDescription(_info.AppName);
        shellLink.SetIconLocation(executablePath, 0);

        var propertyStore = (IPropertyStore)shellLink;
        var propertyKey = PropertyKeys.AppUserModelId;

        using (var appId = new PropVariant(_info.AppId))
        {
            ThrowIfFailed(propertyStore.SetValue(ref propertyKey, appId));
        }

        ThrowIfFailed(propertyStore.Commit());

        var persistFile = (IPersistFile)shellLink;
        persistFile.Save(shortcutPath, true);
        Marshal.ReleaseComObject(shellLink);
    }

    private string GetShortcutPath()
    {
        return Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "Microsoft",
            "Windows",
            "Start Menu",
            "Programs",
            $"{_info.AppName}.lnk");
    }

    private static bool HasPackageIdentity()
    {
        var length = 0;
        var result = NativeMethods.GetCurrentPackageFullName(ref length, null);

        return result != NativeMethods.APPMODEL_ERROR_NO_PACKAGE;
    }

    private static void SetCurrentProcessAppUserModelId(string appUserModelId)
    {
        var hResult = NativeMethods.SetCurrentProcessExplicitAppUserModelID(appUserModelId);
        if (hResult < 0)
            Marshal.ThrowExceptionForHR(hResult);
    }

    private static string GetCurrentExecutablePath()
    {
        using var process = Process.GetCurrentProcess();

        var path = process.MainModule?.FileName;
        if (string.IsNullOrWhiteSpace(path))
            throw new InvalidOperationException("Could not determine the current executable path.");

        return Path.GetFullPath(path);
    }

    private static string GetShortcutTargetPath(IShellLinkW shellLink)
    {
        var builder = new StringBuilder(1024);
        shellLink.GetPath(builder, builder.Capacity, IntPtr.Zero, 0);

        return builder.ToString();
    }

    private static bool PathsEqual(string leftPath, string rightPath)
    {
        if (string.IsNullOrWhiteSpace(leftPath) || string.IsNullOrWhiteSpace(rightPath))
            return false;

        var normalizedLeft = Path.GetFullPath(leftPath)
            .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

        var normalizedRight = Path.GetFullPath(rightPath)
            .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

        return string.Equals(normalizedLeft, normalizedRight, StringComparison.OrdinalIgnoreCase);
    }

    private static void ThrowIfFailed(uint hResult)
    {
        if (hResult != 0)
            Marshal.ThrowExceptionForHR(unchecked((int)hResult));
    }


    private static class PropertyKeys
    {
        internal static readonly PROPERTYKEY AppUserModelId = new()
        {
            fmtid = new Guid("9F4C2855-9F79-4B39-A8D0-E1D42DE1D5F3"),
            pid = 5
        };
    }

    private static class PropVariantHelper
    {
        private const ushort VT_LPWSTR = 31;

        public static string? GetStringAndClear(ref PROPVARIANT value)
        {
            try
            {
                if (value.vt != VT_LPWSTR || value.p == IntPtr.Zero)
                    return null;

                return Marshal.PtrToStringUni(value.p);
            }
            finally
            {
                PropVariantClear(ref value);
            }
        }

        [DllImport("ole32.dll")]
        private static extern int PropVariantClear(ref PROPVARIANT pvar);
    }
}