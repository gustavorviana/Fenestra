using Fenestra.Core.Models;

namespace Fenestra.Core;

/// <summary>
/// Abstracts the persistence of window position data. Implement to provide custom storage.
/// </summary>
public interface IWindowPositionStorage
{
    /// <summary>
    /// Loads the saved window position for the given key, or null if not found.
    /// </summary>
    WindowPositionData? Load(string key);

    /// <summary>
    /// Saves the window position data under the given key.
    /// </summary>
    void Save(string key, WindowPositionData data);

    /// <summary>
    /// Deletes the saved window position for the given key.
    /// </summary>
    void Delete(string key);
}
