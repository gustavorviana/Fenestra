using System.Text;
using Fenestra.Core.Models;
using Fenestra.Windows;
using Fenestra.Windows.Models;
using Microsoft.Win32;

namespace Fenestra.Wpf.Services;

internal class AutoStartService : IAutoStartService
{
    private const string RunKey = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";
    private const string ApprovedKey = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\StartupApproved\Run";

    private readonly string _appName;
    private readonly string _executablePath;

    public AutoStartService(AppInfo appInfo)
    {
        _appName = appInfo.AppId;
        _executablePath = System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName
            ?? System.Reflection.Assembly.GetEntryAssembly()?.Location
            ?? string.Empty;
    }

    public bool IsEnabled
    {
        get
        {
            var status = GetStatus();
            return status?.Enabled ?? false;
        }
    }

    public bool IsInitialized(params string[] args)
    {
        var startup = GetStartupArgs();
        if (string.IsNullOrEmpty(startup)) return false;

        return args.Length == 0 || args.All(a => startup!.Contains(a));
    }

    public void Enable(params string[] args)
    {
        using var key = Registry.CurrentUser.CreateSubKey(RunKey);
        key.SetValue(_appName, BuildCommandLine(args));

        using var approved = Registry.CurrentUser.CreateSubKey(ApprovedKey);
        approved.SetValue(_appName, StartupStatus.CreateEnabled().ToBytes());
    }

    public void Disable()
    {
        using var key = Registry.CurrentUser.CreateSubKey(RunKey);
        key.DeleteValue(_appName, false);
    }

    public StartupStatus? GetStatus()
    {
        using var key = Registry.CurrentUser.OpenSubKey(ApprovedKey, false);
        if (key == null) return null;

        var data = key.GetValue(_appName) as byte[];
        if (data != null && data.Length >= 12)
            return new StartupStatus(data);

        return null;
    }

    private string BuildCommandLine(string[] args)
    {
        var sb = new StringBuilder();
        sb.AppendFormat("\"{0}\"", _executablePath);

        if (args.Length > 0)
            sb.AppendFormat(" {0}", string.Join(" ", args));

        return sb.ToString();
    }

    private string? GetStartupArgs()
    {
        using var key = Registry.CurrentUser.OpenSubKey(RunKey, false);
        return key?.GetValue(_appName) as string;
    }
}
