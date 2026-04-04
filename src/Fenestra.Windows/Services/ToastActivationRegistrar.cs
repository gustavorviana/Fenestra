using Fenestra.Core;
using Fenestra.Core.Models;
using Fenestra.Windows.Native;
using Fenestra.Windows.Native.Toast;
using System.Security.Cryptography;
using System.Text;

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
    private readonly string _shortcutPath;
    private readonly string _exePath;
    private bool _registered;

    /// <inheritdoc />
    public bool IsRegistered => _registered;

    public ToastActivationRegistrar(AppInfo appInfo, IThreadContext threadContext, IApplicationActivator? activator = null, ToastActivationOptions? options = null)
    {
        _appInfo = appInfo;
        _threadContext = threadContext;
        _activator = activator;
        _clsid = options?.ActivatorClsid is { } guid && guid != Guid.Empty
            ? guid
            : GenerateActivatorClsid(appInfo.AppId);
        _exePath = WindowsNotificationRegistrationManager.GetCurrentExecutablePath();
        _shortcutPath = System.IO.Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            @"Microsoft\Windows\Start Menu\Programs",
            $"{appInfo.AppName}.lnk");
    }

    /// <inheritdoc />
    public void Register()
    {
        if (_registered) return;

        try
        {
            RegisterComServerInRegistry();
            EnsureShortcutHasClsid();

            NotificationActivatorServer.Register(_clsid, (_, _) =>
            {
                try { _ = _threadContext.InvokeAsync(() => _activator?.BringToForeground()); }
                catch { }
            });

            _registered = true;
        }
        catch { }
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
        using var link = ShellLink.Create(_shortcutPath);
        link.ToastActivatorClsid = _clsid;
        link.Save();
    }

    /// <summary>
    /// Generates a deterministic GUID from the AppId so the CLSID is stable across restarts.
    /// </summary>
    private static Guid GenerateActivatorClsid(string appId)
    {
        using var sha = SHA256.Create();
        var hash = sha.ComputeHash(Encoding.UTF8.GetBytes("Fenestra.ToastActivator:" + appId));
        var bytes = new byte[16];
        Array.Copy(hash, bytes, 16);
        bytes[6] = (byte)((bytes[6] & 0x0F) | 0x50); // UUID version 5
        bytes[8] = (byte)((bytes[8] & 0x3F) | 0x80); // UUID variant 1
        return new Guid(bytes);
    }
}
