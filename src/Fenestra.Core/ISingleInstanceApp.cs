namespace Fenestra.Core;

/// <summary>
/// Receives command-line arguments forwarded from subsequent application launches.
/// </summary>
public interface ISingleInstanceApp
{
    /// <summary>
    /// Handles command-line arguments forwarded from a subsequent application instance.
    /// </summary>
    void OnArgumentsReceived(string[] args);
}
