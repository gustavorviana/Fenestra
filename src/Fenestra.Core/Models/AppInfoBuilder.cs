using System.Reflection;
using System.Runtime.InteropServices;

namespace Fenestra.Core.Models;

/// <summary>
/// Internal builder for constructing <see cref="AppInfo"/> instances.
/// Collects application metadata from multiple sources (user config, assembly, package identity)
/// before producing the final immutable <see cref="AppInfo"/>.
/// </summary>
internal class AppInfoBuilder
{
    public string? AppName { get; set; }
    public string? AppId { get; set; }
    public Version? Version { get; set; }
    public Guid AppGuid { get; set; }
    public string? PackageFamilyName { get; set; }

    /// <summary>
    /// Populates name, version, and GUID from the entry assembly attributes.
    /// </summary>
    public AppInfoBuilder FromEntryAssembly()
    {
        var assembly = Assembly.GetEntryAssembly() ?? Assembly.GetCallingAssembly();
        var name = assembly.GetName();

        AppName ??= assembly.GetCustomAttribute<AssemblyProductAttribute>()?.Product
                    ?? name.Name
                    ?? "FenestraApp";

        Version ??= name.Version ?? new Version(1, 0, 0);

        if (AppGuid == Guid.Empty)
        {
            var guidAttr = assembly.GetCustomAttribute<GuidAttribute>();
            if (guidAttr != null && Guid.TryParse(guidAttr.Value, out var appGuid))
                AppGuid = appGuid;
        }

        return this;
    }

    /// <summary>
    /// Builds the final <see cref="AppInfo"/> from the collected metadata.
    /// </summary>
    public AppInfo Build()
    {
        var appName = AppName ?? "FenestraApp";
        var version = Version ?? new Version(1, 0, 0);

        AppInfo info;

        if (PackageFamilyName != null)
        {
            var aumid = AppId ?? $"{PackageFamilyName}!App";
            info = new AppInfo(appName, aumid, version, PackageFamilyName);
        }
        else if (AppId != null)
        {
            info = new AppInfo(appName, AppId, version);
        }
        else
        {
            info = new AppInfo(appName, version);
        }

        info.AppGuid = AppGuid;
        return info;
    }
}
