namespace Fenestra.Core.Models;

public class AppInfo
{
    public string AppName { get; }
    public Version Version { get; }
    public string HostName { get; }

    public AppInfo(string appName, Version version, string hostName)
    {
        AppName = appName;
        Version = version;
        HostName = hostName;
    }
}
