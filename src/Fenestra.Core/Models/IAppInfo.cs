namespace Fenestra.Core.Models;

/// <summary>
/// Base application metadata. Platform-independent.
/// </summary>
public interface IAppInfo
{
    /// <summary>Gets the display name of the application.</summary>
    string AppName { get; }

    /// <summary>Gets the application identifier.</summary>
    string AppId { get; }

    /// <summary>Gets the application version.</summary>
    Version Version { get; }
}
