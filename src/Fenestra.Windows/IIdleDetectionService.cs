namespace Fenestra.Windows;

/// <summary>
/// Detects user inactivity (no mouse or keyboard input) across the entire Windows session.
///
/// <para>
/// <b>Scope note:</b> this is <i>global</i> idle detection via Win32 <c>GetLastInputInfo</c> —
/// it tracks ANY input on the system, not input targeted at your application's windows.
/// If the user is interacting with another application, they are NOT considered idle.
/// </para>
/// </summary>
public interface IIdleDetectionService
{
    /// <summary>
    /// Time elapsed since the last global user input, as observed on the last poll tick.
    /// </summary>
    TimeSpan IdleTime { get; }

    /// <summary>
    /// Whether the user is currently considered idle (<see cref="IdleTime"/> ≥ <see cref="Threshold"/>).
    /// Cached from the last poll — consistent with <see cref="BecameIdle"/>/<see cref="BecameActive"/> events.
    /// </summary>
    bool IsIdle { get; }

    /// <summary>
    /// Idle threshold. Re-evaluation happens on the next poll tick, not immediately on assignment.
    /// Minimum: 1 second.
    /// </summary>
    TimeSpan Threshold { get; set; }

    /// <summary>Raised on the dispatcher thread when the user transitions from active to idle.</summary>
    event EventHandler? BecameIdle;

    /// <summary>Raised on the dispatcher thread when the user transitions from idle to active.</summary>
    event EventHandler? BecameActive;
}

/// <summary>
/// Configuration for <see cref="IIdleDetectionService"/>.
/// </summary>
public sealed class IdleDetectionOptions
{
    /// <summary>
    /// How long the user must be inactive before being considered idle. Default: 5 minutes.
    /// Minimum: 1 second.
    /// </summary>
    public TimeSpan Threshold { get; set; } = TimeSpan.FromMinutes(5);

    /// <summary>
    /// How often the service polls the underlying input probe. Default: 5 seconds.
    /// Minimum: 100ms. Events can only fire at poll granularity, so this is also the maximum
    /// latency for <see cref="IIdleDetectionService.BecameIdle"/>/<c>BecameActive</c>.
    /// </summary>
    public TimeSpan PollInterval { get; set; } = TimeSpan.FromSeconds(5);
}
