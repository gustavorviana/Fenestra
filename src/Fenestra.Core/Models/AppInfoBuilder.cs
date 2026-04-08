using System.Reflection;

namespace Fenestra.Core.Models;

/// <summary>
/// Internal builder for constructing <see cref="AppInfo"/> instances.
/// Collects application metadata from user config and assembly attributes.
/// </summary>
internal class AppInfoBuilder
{
    public string? AppName { get; set; }
    public string? AppId { get; set; }
    public Version? Version { get; set; }

    /// <summary>
    /// Populates name and version from the entry assembly attributes.
    /// </summary>
    public AppInfoBuilder FromEntryAssembly()
    {
        var assembly = Assembly.GetEntryAssembly() ?? Assembly.GetCallingAssembly();
        var name = assembly.GetName();

        AppName ??= assembly.GetCustomAttribute<AssemblyProductAttribute>()?.Product
                    ?? name.Name
                    ?? "FenestraApp";

        Version ??= name.Version ?? new Version(1, 0, 0);

        return this;
    }

    /// <summary>
    /// Builds the final <see cref="AppInfo"/> from the collected metadata.
    /// </summary>
    public virtual AppInfo Build()
    {
        var appName = AppName ?? "FenestraApp";
        var version = Version ?? new Version(1, 0, 0);

        if (AppId != null)
            return new AppInfo(appName, AppId, version);

        return new AppInfo(appName, version);
    }
}
