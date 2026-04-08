namespace Fenestra.Windows;

/// <summary>
/// Handle to a scheduled toast notification. Allows querying properties and canceling.
/// </summary>
public interface IScheduledToastHandle : IDisposable
{
    /// <summary>Gets the unique identifier for this scheduled notification.</summary>
    string? Id { get; }

    /// <summary>Gets the tag for this scheduled notification.</summary>
    string? Tag { get; }

    /// <summary>Gets the group for this scheduled notification.</summary>
    string? Group { get; }

    /// <summary>Gets the scheduled delivery time.</summary>
    DateTimeOffset DeliveryTime { get; }

    /// <summary>Gets whether the popup will be suppressed when delivered.</summary>
    bool SuppressPopup { get; }

    /// <summary>Cancels this scheduled notification (removes it from the schedule).</summary>
    void Cancel();
}
