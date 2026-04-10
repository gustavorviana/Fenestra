namespace Fenestra.Core;

/// <summary>
/// Represents a single progress update reported by a splash screen during application startup.
/// </summary>
public readonly struct SplashStatus
{
    /// <summary>Human-readable status text (e.g. "Loading settings...").</summary>
    public string Message { get; }

    /// <summary>
    /// Optional progress value in the range [0, 1]. <c>null</c> means indeterminate — the splash
    /// should display a non-deterministic indicator (spinner, marquee, etc.).
    /// </summary>
    public double? Percent { get; }

    /// <summary>Creates a new splash status update.</summary>
    public SplashStatus(string message, double? percent = null)
    {
        Message = message;
        Percent = percent;
    }
}
