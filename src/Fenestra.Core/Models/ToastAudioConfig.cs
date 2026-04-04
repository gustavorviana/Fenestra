namespace Fenestra.Core.Models;

/// <summary>Configures the audio settings for a toast notification.</summary>
public class ToastAudioConfig
{
    public ToastAudio Sound { get; set; }
    public string? CustomUri { get; set; }
    public bool Loop { get; set; }
    public bool Silent { get; set; }
}
