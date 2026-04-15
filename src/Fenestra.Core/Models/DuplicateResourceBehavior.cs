namespace Fenestra.Core.Models;

/// <summary>
/// Defines how the system should behave when multiple resources resolve to the same alias
/// during registration (e.g., from different assemblies or namespaces).
/// </summary>
public enum DuplicateResourceBehavior
{
    /// <summary>
    /// Throws an exception when a duplicate alias is detected.
    /// This enforces strict uniqueness and helps catch configuration issues early.
    /// </summary>
    Throw,

    /// <summary>
    /// Replaces the previously registered resource with the new one.
    /// The last registration wins when duplicates occur.
    /// </summary>
    Replace,

    /// <summary>
    /// Keeps both resources by assigning a new alias based on the full resource base name
    /// (typically including namespace). This avoids collisions while preserving access to all resources.
    /// </summary>
    UseFullName
}