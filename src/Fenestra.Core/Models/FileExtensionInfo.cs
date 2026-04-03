namespace Fenestra.Core.Models;

public readonly struct FileExtensionInfo : IEquatable<FileExtensionInfo>
{
    public static FileExtensionInfo All => new("*", "All Files");

    public string Extension { get; }
    public string Description { get; }

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
