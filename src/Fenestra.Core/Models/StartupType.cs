namespace Fenestra.Core.Models;

public enum StartupType : uint
{
    Enabled = 2,
    Disabled = 3,
    DisabledByUser = 6,
    DisabledByPolicy = 7
}
