namespace Fenestra.Core.Models;

/// <summary>
/// Represents a file extension filter for file dialogs.
/// </summary>
public readonly struct FileExtensionInfo : IEquatable<FileExtensionInfo>
{
    /// <summary>
    /// Gets a filter that matches all files.
    /// </summary>
    public static FileExtensionInfo All => new("*", "All Files");

    /// <summary>
    /// Gets the file extension pattern (e.g. "txt" or "*").
    /// </summary>
    public string Extension { get; }

    /// <summary>
    /// Gets the human-readable description for this extension filter.
    /// </summary>
    public string Description { get; }

    /// <summary>
    /// Initializes a new instance with the specified extension and description.
    /// </summary>
    public FileExtensionInfo(string extension, string description)
    {
        Extension = extension;
        Description = description;
    }

    public override bool Equals(object? obj)
    {
        return obj is FileExtensionInfo info && Equals(info);
    }

    public bool Equals(FileExtensionInfo other)
    {
        return Extension == other.Extension &&
               Description == other.Description;
    }

    public override int GetHashCode()
    {
        int hashCode = -1067776173;
        hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Extension);
        hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Description);
        return hashCode;
    }

    public static bool operator ==(FileExtensionInfo left, FileExtensionInfo right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(FileExtensionInfo left, FileExtensionInfo right)
    {
        return !(left == right);
    }
}
