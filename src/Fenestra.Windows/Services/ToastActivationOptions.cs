namespace Fenestra.Windows.Services;

/// <summary>
/// Options for configuring toast background activation with an explicit COM activator CLSID.
/// </summary>
public class ToastActivationOptions
{
    /// <summary>
    /// A stable GUID for the toast activator COM class.
    /// Must never change once deployed. Generate one with <c>Guid.NewGuid()</c> and hardcode it.
    /// </summary>
    public Guid ActivatorClsid { get; set; }
}
