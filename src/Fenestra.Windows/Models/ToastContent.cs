namespace Fenestra.Windows.Models;

/// <summary>Represents the complete content of a toast notification.</summary>
public class ToastContent
{
    public string? Title { get; set; }
    public string? Body { get; set; }
    public string? Attribution { get; set; }
    public string? LaunchArgs { get; set; }
    public ToastActivationType ActivationType { get; set; }
    public ToastDuration Duration { get; set; }
    public ToastScenario Scenario { get; set; }
    public DateTimeOffset? DisplayTimestamp { get; set; }
    public bool UseButtonStyle { get; set; }
    public List<ToastImage> Images { get; } = new();
    public List<ToastButton> Buttons { get; } = new();
    public List<ToastInput> Inputs { get; } = new();
    public ToastProgress? Progress { get; set; }
    public ToastHeader? Header { get; set; }
    public ToastAudioConfig? Audio { get; set; }
    public string? Tag { get; set; }
    public string? Group { get; set; }
    public bool SuppressPopup { get; set; }
    public ToastPriority Priority { get; set; }
    public bool ExpiresOnReboot { get; set; }
    public DateTimeOffset? ExpirationTime { get; set; }
    public NotificationMirroring NotificationMirroring { get; set; }
    public string? RemoteId { get; set; }
    public List<ToastGroup> Groups { get; } = new();
    public ToastProgressTracker? ProgressTracker { get; set; }
}
