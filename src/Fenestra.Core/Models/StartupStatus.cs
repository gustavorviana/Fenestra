namespace Fenestra.Core.Models;

public readonly struct StartupStatus : IEquatable<StartupStatus>
{
    public StartupType Status { get; }
    public DateTime ModifiedDate { get; }
    public bool Enabled => Status == StartupType.Enabled;

    public StartupStatus(StartupType status)
    {
        Status = status;
        ModifiedDate = DateTime.Now;
    }

    public StartupStatus(StartupType status, DateTime modifiedDate)
    {
        Status = status;
        ModifiedDate = modifiedDate;
    }

    public StartupStatus(byte[] bytes)
    {
        if (bytes == null || bytes.Length < 12)
            throw new ArgumentException("Bytes array must be at least 12 bytes long.");

        Status = (StartupType)BitConverter.ToUInt32(bytes, 0);

        long fileTime = BitConverter.ToInt64(bytes, 4);
        ModifiedDate = DateTime.FromFileTime(fileTime);
    }

    public byte[] ToBytes()
    {
        var result = new byte[12];
        Array.Copy(BitConverter.GetBytes((uint)Status), 0, result, 0, 4);
        Array.Copy(BitConverter.GetBytes(ModifiedDate.ToFileTime()), 0, result, 4, 8);
        return result;
    }

    public static StartupStatus CreateEnabled() => new(StartupType.Enabled);
    public static StartupStatus CreateDisabled() => new(StartupType.Disabled);

    public bool Equals(StartupStatus other) => Status == other.Status && ModifiedDate == other.ModifiedDate;
    public override bool Equals(object? obj) => obj is StartupStatus other && Equals(other);
    public override int GetHashCode() => ((int)Status * 397) ^ ModifiedDate.GetHashCode();
    public static bool operator ==(StartupStatus left, StartupStatus right) => left.Equals(right);
    public static bool operator !=(StartupStatus left, StartupStatus right) => !left.Equals(right);

    public override string ToString() => $"{Status} - Modified: {ModifiedDate:yyyy-MM-dd HH:mm:ss}";
}
