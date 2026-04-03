namespace Fenestra.Core.Models;

/// <summary>
/// Represents a Windows startup approval entry from the StartupApproved registry key.
/// </summary>
public readonly struct StartupStatus : IEquatable<StartupStatus>
{
    /// <summary>
    /// Gets the startup type (enabled or disabled).
    /// </summary>
    public StartupType Status { get; }

    /// <summary>
    /// Gets the date and time the startup entry was last modified.
    /// </summary>
    public DateTime ModifiedDate { get; }

    /// <summary>
    /// Gets whether the startup entry is enabled.
    /// </summary>
    public bool Enabled => Status == StartupType.Enabled;

    /// <summary>
    /// Initializes a new instance with the specified status and the current time.
    /// </summary>
    public StartupStatus(StartupType status)
    {
        Status = status;
        ModifiedDate = DateTime.Now;
    }

    /// <summary>
    /// Initializes a new instance with the specified status and modification date.
    /// </summary>
    public StartupStatus(StartupType status, DateTime modifiedDate)
    {
        Status = status;
        ModifiedDate = modifiedDate;
    }

    /// <summary>
    /// Initializes a new instance by deserializing from a 12-byte registry value.
    /// </summary>
    public StartupStatus(byte[] bytes)
    {
        if (bytes == null || bytes.Length < 12)
            throw new ArgumentException("Bytes array must be at least 12 bytes long.");

        Status = (StartupType)BitConverter.ToUInt32(bytes, 0);

        long fileTime = BitConverter.ToInt64(bytes, 4);
        ModifiedDate = DateTime.FromFileTime(fileTime);
    }

    /// <summary>
    /// Serializes this instance to a 12-byte array suitable for the StartupApproved registry key.
    /// </summary>
    public byte[] ToBytes()
    {
        var result = new byte[12];
        Array.Copy(BitConverter.GetBytes((uint)Status), 0, result, 0, 4);
        Array.Copy(BitConverter.GetBytes(ModifiedDate.ToFileTime()), 0, result, 4, 8);
        return result;
    }

    /// <summary>
    /// Creates an enabled startup status with the current timestamp.
    /// </summary>
    public static StartupStatus CreateEnabled() => new(StartupType.Enabled);

    /// <summary>
    /// Creates a disabled startup status with the current timestamp.
    /// </summary>
    public static StartupStatus CreateDisabled() => new(StartupType.Disabled);

    public bool Equals(StartupStatus other) => Status == other.Status && ModifiedDate == other.ModifiedDate;
    public override bool Equals(object? obj) => obj is StartupStatus other && Equals(other);
    public override int GetHashCode() => ((int)Status * 397) ^ ModifiedDate.GetHashCode();
    public static bool operator ==(StartupStatus left, StartupStatus right) => left.Equals(right);
    public static bool operator !=(StartupStatus left, StartupStatus right) => !left.Equals(right);

    public override string ToString() => $"{Status} - Modified: {ModifiedDate:yyyy-MM-dd HH:mm:ss}";
}
