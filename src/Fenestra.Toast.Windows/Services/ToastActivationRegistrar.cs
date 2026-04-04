using System.IO;
using Fenestra.Core;
using Fenestra.Core.Models;
using Fenestra.Toast.Windows.Native.Toast;

namespace Fenestra.Toast.Windows.Services;

/// <summary>
/// Registers COM server + Start Menu shortcut so toast activations relaunch the app when closed.
/// When the app IS running, the WinRT Activated event on <see cref="IToastService"/> handles activation.
/// When the app is NOT running, Windows relaunches the EXE. Use <see cref="ISingleInstanceApp"/>
/// to receive the activation arguments on relaunch.
/// </summary>
internal class ToastActivationRegistrar : IToastActivationRegistrar
{
    private readonly string _shortcutPath;
    private readonly string _exePath;
    private readonly string _appId;
    private readonly Guid _activatorClsid;
    private readonly bool _supported;
    private bool _registered;

    /// <inheritdoc />
    public bool IsRegistered => _registered;

    public ToastActivationRegistrar(AppInfo appInfo, ToastActivationOptions options)
    {
        _appId = appInfo.AppId;
        _activatorClsid = options.ActivatorClsid;
        _supported = Environment.OSVersion.Version.Major >= 10;

        _exePath = System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName
            ?? System.Reflection.Assembly.GetEntryAssembly()?.Location
            ?? string.Empty;

        _shortcutPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            @"Microsoft\Windows\Start Menu\Programs",
            $"{appInfo.AppName}.lnk");
    }

    /// <inheritdoc />
    public void Register()
    {
        if (!_supported || string.IsNullOrEmpty(_exePath)) return;

        try
        {
            ToastActivationInterop.RegisterComServer(_exePath, _activatorClsid);

            if (!File.Exists(_shortcutPath))
                ToastActivationInterop.CreateShortcut(_shortcutPath, _exePath, _appId, _activatorClsid);

            _registered = true;
        }
        catch { }
    }

    /// <inheritdoc />
    public void Unregister()
    {
        try
        {
            ToastActivationInterop.UnregisterComServer(_activatorClsid);
            ToastActivationInterop.RemoveShortcut(_shortcutPath);
            _registered = false;
        }
        catch { }
    }
}

/// <summary>
/// Options for configuring toast background activation.
/// </summary>
public class ToastActivationOptions
{
    /// <summary>
    /// A stable GUID for the toast activator COM class. Must never change once deployed.
    /// Generate one with <c>Guid.NewGuid()</c> and hardcode it in your app.
    /// </summary>
    public Guid ActivatorClsid { get; set; }
}
