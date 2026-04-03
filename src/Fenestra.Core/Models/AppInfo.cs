namespace Fenestra.Core.Models;

public class AppInfo
{
    public string AppName { get; }
    public string AppId { get; }
    public Version Version { get; }
    public string HostName { get; }

    public AppInfo(string appName, Version version, string hostName)
    {
        AppName = appName;
        AppId = new string([.. appName.Where(char.IsLetterOrDigit)]);
        Version = version;
        HostName = hostName;
    }
}
