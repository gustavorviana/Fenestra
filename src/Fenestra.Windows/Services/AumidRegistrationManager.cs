using Fenestra.Core.Models;
using System.Runtime.InteropServices;
using System.Text;

namespace Fenestra.Windows.Services;

/// <inheritdoc cref="IAumidRegistrationManager"/>
public sealed class AumidRegistrationManager : IAumidRegistrationManager
{
    private readonly AppInfo _info;
    private readonly AppShortcutManager _shortcut;

    public AumidRegistrationManager(AppInfo info)
    {
        _info = info ?? throw new ArgumentNullException(nameof(info));
        _shortcut = new AppShortcutManager(info);
    }

    public void EnsureRegistered()
    {
        Platform.EnsureWindows();

        if (HasPackageIdentity())
            return;

        SetCurrentProcessAppUserModelId(_info.AppId);

        if (!_shortcut.NeedsUpdate())
            return;

        _shortcut.CreateOrUpdateShortcut();
    }

    public bool NeedsRegistration()
    {
        if (HasPackageIdentity())
            return false;

        return _shortcut.NeedsUpdate();
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

    private const int APPMODEL_ERROR_NO_PACKAGE = 15700;

    [DllImport("shell32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern int SetCurrentProcessExplicitAppUserModelID(string appID);

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
    private static extern int GetCurrentPackageFullName(ref int packageFullNameLength, StringBuilder? packageFullName);
}
