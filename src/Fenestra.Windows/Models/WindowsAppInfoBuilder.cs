using Fenestra.Core.Models;
using System.Reflection;
using System.Runtime.InteropServices;

namespace Fenestra.Windows.Models;

/// <summary>
/// Extends <see cref="AppInfoBuilder"/> with Windows-specific metadata
/// (AppGuid from GuidAttribute, package identity).
/// </summary>
internal class WindowsAppInfoBuilder : AppInfoBuilder
{
    public Guid AppGuid { get; set; }
    public string? PackageFamilyName { get; set; }

    public new WindowsAppInfoBuilder FromEntryAssembly()
    {
        base.FromEntryAssembly();

        if (AppGuid == Guid.Empty)
        {
            var assembly = Assembly.GetEntryAssembly() ?? Assembly.GetCallingAssembly();
            var guidAttr = assembly.GetCustomAttribute<GuidAttribute>();
            if (guidAttr != null && Guid.TryParse(guidAttr.Value, out var appGuid))
                AppGuid = appGuid;
        }

        return this;
    }

    public override AppInfo Build()
    {
        var appName = AppName ?? "FenestraApp";
        var version = Version ?? new Version(1, 0, 0);

        WindowsAppInfo info;

        if (PackageFamilyName != null)
        {
            var aumid = AppId ?? $"{PackageFamilyName}!App";
            info = new WindowsAppInfo(appName, aumid, version, PackageFamilyName);
        }
        else if (AppId != null)
        {
            info = new WindowsAppInfo(appName, AppId, version);
        }
        else
        {
            info = new WindowsAppInfo(appName, version);
        }

        info.AppGuid = AppGuid;
        return info;
    }
}
