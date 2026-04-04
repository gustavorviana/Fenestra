using Fenestra.Core;
using Fenestra.Core.Models;
using Fenestra.Windows.Native;
using Fenestra.Windows.Native.Toast;

namespace Fenestra.Windows.Services;

/// <summary>
/// Registers the application for toast background activation so clicking a toast
/// when the app is closed relaunches it. Handles three things:
/// <list type="number">
///   <item>Registry COM server (LocalServer32) so Windows knows which EXE to launch.</item>
///   <item>Start Menu shortcut with <c>ToastActivatorCLSID</c> so Windows links toasts to the COM server.</item>
///   <item>Runtime COM class factory (<c>CoRegisterClassObject</c>) so Windows can deliver the activation callback.</item>
/// </list>
/// Opt in via <c>builder.UseToastActivation()</c>.
/// </summary>
internal class ToastActivationRegistrar : IToastActivationRegistrar, IDisposable
{
    private readonly AppInfo _appInfo;
    private readonly IThreadContext _threadContext;
    private readonly IApplicationActivator? _activator;
    private readonly Guid _clsid;
    private readonly string _exePath;
    private readonly string _shortcutPath;
    private bool _registered;

    /// <inheritdoc />
    public bool IsRegistered => _registered;

    public ToastActivationRegistrar(AppInfo appInfo, IThreadContext threadContext, IApplicationActivator? activator = null)
    {
        _appInfo = appInfo;
        _threadContext = threadContext;
        _activator = activator;
        _clsid = appInfo.AppGuid;
        _exePath = WindowsNotificationRegistrationManager.GetCurrentExecutablePath();
        _shortcutPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            @"Microsoft\Windows\Start Menu\Programs",
            $"{appInfo.AppName}.lnk");
    }

    /// <inheritdoc />
    public void Register()
    {
        if (_registered) return;
        Platform.EnsureWindows10();

        RegisterComServerInRegistry();
        EnsureShortcutHasClsid();

        NotificationActivatorServer.Register(_clsid, (_, _) =>
        {
            try { _ = _threadContext.InvokeAsync(() => _activator?.BringToForeground()); }
            catch { }
        });

        _registered = true;
    }

    /// <inheritdoc />
    public void Unregister()
    {
        NotificationActivatorServer.Unregister();

        try
        {
            var regPath = $@"SOFTWARE\Classes\CLSID\{{{_clsid}}}";
            Microsoft.Win32.Registry.CurrentUser.DeleteSubKeyTree(regPath, false);
        }
        catch { }

        _registered = false;
    }

    public void Dispose()
    {
        NotificationActivatorServer.Unregister();
    }

    private void RegisterComServerInRegistry()
    {
        var regPath = $@"SOFTWARE\Classes\CLSID\{{{_clsid}}}\LocalServer32";
        using var key = Microsoft.Win32.Registry.CurrentUser.CreateSubKey(regPath);
        key.SetValue(null, $"\"{_exePath}\"");
    }

    private void EnsureShortcutHasClsid()
    {
        var workingDir = Path.GetDirectoryName(_exePath) ?? AppDomain.CurrentDomain.BaseDirectory;

        using var link = ShellLink.Create(_shortcutPath);
        link.TargetPath = _exePath;
        link.WorkingDirectory = workingDir;
        link.Description = _appInfo.AppName;
        link.SetIconLocation(_exePath);
        link.AppUserModelId = _appInfo.AppId;
        link.ToastActivatorClsid = _clsid;
        link.Save();
    }
}
