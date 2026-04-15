using Fenestra.Core;
using Fenestra.Core.Models;
using Fenestra.Windows.Models;
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
/// Opt in via <c>builder.UseWindowsToastActivation()</c>.
/// </summary>
internal class ToastActivationRegistrar : IToastActivationRegistrar, IFenestraModule, IDisposable
{
    private readonly IThreadContext _threadContext;
    private readonly IApplicationActivator? _activator;
    private readonly AppShortcutManager _shortcut;
    private readonly Guid _clsid;
    private bool _registered;

    /// <inheritdoc />
    public bool IsRegistered => _registered;

    public ToastActivationRegistrar(WindowsAppInfo appInfo, IThreadContext threadContext, IApplicationActivator? activator = null)
    {
        _threadContext = threadContext;
        _activator = activator;
        _shortcut = new AppShortcutManager(appInfo);
        _clsid = appInfo.AppGuid;
    }

    /// <inheritdoc />
    public void Register()
    {
        if (_registered) return;
        Platform.EnsureWindows10();

        RegisterComServerInRegistry();
        _shortcut.CreateOrUpdateShortcut(_clsid);

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

    public Task InitAsync(CancellationToken cancellationToken)
    {
        Register();
        return Task.CompletedTask;
    }

    public void Dispose()
    {
        NotificationActivatorServer.Unregister();
    }

    private void RegisterComServerInRegistry()
    {
        var regPath = $@"SOFTWARE\Classes\CLSID\{{{_clsid}}}\LocalServer32";
        using var key = Microsoft.Win32.Registry.CurrentUser.CreateSubKey(regPath);
        key.SetValue(null, $"\"{_shortcut.ExePath}\"");
    }
}
