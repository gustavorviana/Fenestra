namespace Fenestra.Windows.Models;

/// <summary>
/// Startup approval status as stored in the Windows StartupApproved registry.
/// </summary>
public enum StartupType : uint
{
    Enabled = 2,
    Disabled = 3,
    DisabledByUser = 6,
    DisabledByPolicy = 7
}
