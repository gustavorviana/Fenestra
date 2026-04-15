using Microsoft.Win32;
using System.Globalization;
using System.Reflection;

namespace Fenestra.Windows.Services;

/// <summary>
/// <see cref="IRegistryConfig"/> implementation backed by HKEY_CURRENT_USER.
/// </summary>
public sealed class RegistryConfigService : IRegistryConfig
{
    private readonly RegistryKey _key;
    private readonly bool _ownsKey;
    private bool _disposed;

    /// <summary>
    /// Opens or creates a key under HKEY_CURRENT_USER.
    /// </summary>
    public RegistryConfigService(string keyPath)
    {
        Platform.EnsureWindows();

        if (string.IsNullOrWhiteSpace(keyPath))
            throw new ArgumentException("Registry key path cannot be empty.", nameof(keyPath));

        _key = Registry.CurrentUser.CreateSubKey(keyPath.Replace('/', '\\'), true)
            ?? throw new InvalidOperationException($"Failed to open or create registry key: {keyPath}");
        _ownsKey = true;
    }

    /// <summary>
    /// Wraps an already-open key (used for child sections).
    /// </summary>
    public RegistryConfigService(RegistryKey key)
    {
        _key = key ?? throw new ArgumentNullException(nameof(key));
        _ownsKey = true;
    }

    public void Set(string name, object? value)
    {
        ThrowIfDisposed();

        if (value is null)
        {
            if (_key.GetValue(name) is not null)
                _key.DeleteValue(name);
            return;
        }

        var (regValue, kind) = ConvertToRegistry(value);
        _key.SetValue(name, regValue, kind);
    }

    public T? Get<T>(string name)
    {
        ThrowIfDisposed();
        var raw = _key.GetValue(name);
        if (raw is null) return default;
        return (T)ConvertFromRegistry(raw, typeof(T));
    }

    public bool TryGet<T>(string name, out T? value)
    {
        ThrowIfDisposed();
        try
        {
            var raw = _key.GetValue(name);
            if (raw is null) { value = default; return false; }
            value = (T)ConvertFromRegistry(raw, typeof(T));
            return true;
        }
        catch
        {
            value = default;
            return false;
        }
    }

    public string? GetString(string name, string? defaultValue = null)
    {
        ThrowIfDisposed();
        return _key.GetValue(name, defaultValue)?.ToString() ?? defaultValue;
    }

    public object? GetValue(string name, object? defaultValue = null)
    {
        ThrowIfDisposed();
        return _key.GetValue(name, defaultValue);
    }

    public T GetSection<T>(string sectionName) where T : new()
    {
        ThrowIfDisposed();

        if (sectionName is null) throw new ArgumentNullException(nameof(sectionName));

        var section = new T();
        using var subKey = _key.OpenSubKey(sectionName, false);
        if (subKey is null) return section;

        ReadInto(section, subKey);
        return section;
    }

    public bool TryGetSection<T>(string sectionName, out T? value) where T : new()
    {
        ThrowIfDisposed();
        try
        {
            using var subKey = _key.OpenSubKey(sectionName, false);
            if (subKey is null) { value = default; return false; }

            var section = new T();
            ReadInto(section, subKey);
            value = section;
            return true;
        }
        catch
        {
            value = default;
            return false;
        }
    }

    public void SetSection(string sectionName, object section)
    {
        ThrowIfDisposed();

        if (sectionName is null) throw new ArgumentNullException(nameof(sectionName));
        if (section is null) throw new ArgumentNullException(nameof(section));

        using var subKey = _key.CreateSubKey(sectionName, true);
        WriteFrom(section, subKey);
    }

    public IRegistryConfig? GetSection(string sectionName, bool createIfNotExists = false)
    {
        ThrowIfDisposed();

        var subKey = _key.OpenSubKey(sectionName, true);
        if (subKey is not null)
            return new RegistryConfigService(subKey);

        if (!createIfNotExists) return null;

        return new RegistryConfigService(_key.CreateSubKey(sectionName, true));
    }

    public bool Exists(string name)
    {
        ThrowIfDisposed();
        return _key.GetValue(name) is not null;
    }

    public string[] GetValueNames()
    {
        ThrowIfDisposed();
        return _key.GetValueNames();
    }

    public string[] GetSections()
    {
        ThrowIfDisposed();
        return _key.GetSubKeyNames();
    }

    public bool DeleteSection(string sectionName)
    {
        ThrowIfDisposed();

        if (!_key.GetSubKeyNames().Contains(sectionName))
            return false;

        _key.DeleteSubKeyTree(sectionName);
        return true;
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        if (_ownsKey) _key.Dispose();
    }

    // ── Reflection-based section read/write ──────────────────────────

    private static void ReadInto(object target, RegistryKey source)
    {
        foreach (var prop in target.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (!prop.CanWrite) continue;

            if (IsSection(prop.PropertyType))
            {
                using var childKey = source.OpenSubKey(prop.Name, false);
                if (childKey is null) continue;

                var child = Activator.CreateInstance(prop.PropertyType)!;
                ReadInto(child, childKey);
                prop.SetValue(target, child);
            }
            else
            {
                var raw = source.GetValue(prop.Name);
                if (raw is null) continue;
                prop.SetValue(target, ConvertFromRegistry(raw, prop.PropertyType));
            }
        }
    }

    private static void WriteFrom(object source, RegistryKey target)
    {
        foreach (var prop in source.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (!prop.CanRead) continue;

            var value = prop.GetValue(source);
            if (value is null) continue;

            if (IsSection(prop.PropertyType))
            {
                using var childKey = target.CreateSubKey(prop.Name, true);
                WriteFrom(value, childKey);
            }
            else
            {
                var (regValue, kind) = ConvertToRegistry(value);
                target.SetValue(prop.Name, regValue, kind);
            }
        }
    }

    private static bool IsSection(Type type)
    {
        return type.GetCustomAttribute<RegistrySectionAttribute>() is not null;
    }

    // ── Type conversion ──────────────────────────────────────────────

    private static (object value, RegistryValueKind kind) ConvertToRegistry(object value)
    {
        return value switch
        {
            int v    => (v, RegistryValueKind.DWord),
            uint v   => (unchecked((int)v), RegistryValueKind.DWord),
            short v  => ((int)v, RegistryValueKind.DWord),
            ushort v => ((int)v, RegistryValueKind.DWord),
            byte v   => ((int)v, RegistryValueKind.DWord),
            sbyte v  => ((int)v, RegistryValueKind.DWord),
            bool v   => (v ? 1 : 0, RegistryValueKind.DWord),

            long v   => (v, RegistryValueKind.QWord),
            ulong v  => (unchecked((long)v), RegistryValueKind.QWord),

            string v => (v, RegistryValueKind.String),
            byte[] v => (v, RegistryValueKind.Binary),

            Enum v   => (Convert.ToInt32(v), RegistryValueKind.DWord),

            Guid v           => (v.ToString("D"), RegistryValueKind.String),
            DateTime v       => (v.ToString("O", CultureInfo.InvariantCulture), RegistryValueKind.String),
            DateTimeOffset v => (v.ToString("O", CultureInfo.InvariantCulture), RegistryValueKind.String),
            TimeSpan v       => (v.ToString("c"), RegistryValueKind.String),
            float v          => (v.ToString("R", CultureInfo.InvariantCulture), RegistryValueKind.String),
            double v         => (v.ToString("R", CultureInfo.InvariantCulture), RegistryValueKind.String),
            decimal v        => (v.ToString(CultureInfo.InvariantCulture), RegistryValueKind.String),
            Version v        => (v.ToString(), RegistryValueKind.String),
            Uri v            => (v.AbsoluteUri, RegistryValueKind.String),

            _ => throw new NotSupportedException($"Type '{value.GetType().FullName}' is not supported by RegistryConfig.")
        };
    }

    private static object ConvertFromRegistry(object raw, Type target)
    {
        var underlying = Nullable.GetUnderlyingType(target) ?? target;

        if (underlying == typeof(string))  return raw.ToString()!;
        if (underlying == typeof(byte[]))  return (byte[])raw;

        // DWord-backed types
        if (underlying == typeof(int))     return Convert.ToInt32(raw);
        if (underlying == typeof(uint))    return unchecked((uint)Convert.ToInt32(raw));
        if (underlying == typeof(short))   return (short)Convert.ToInt32(raw);
        if (underlying == typeof(ushort))  return (ushort)Convert.ToInt32(raw);
        if (underlying == typeof(byte))    return (byte)Convert.ToInt32(raw);
        if (underlying == typeof(sbyte))   return (sbyte)Convert.ToInt32(raw);
        if (underlying == typeof(bool))    return Convert.ToInt32(raw) != 0;

        // QWord-backed types
        if (underlying == typeof(long))    return Convert.ToInt64(raw);
        if (underlying == typeof(ulong))   return unchecked((ulong)Convert.ToInt64(raw));

        // Enum
        if (underlying.IsEnum)             return Enum.ToObject(underlying, Convert.ToInt32(raw));

        // String-backed types
        var str = raw.ToString()!;

        if (underlying == typeof(Guid))           return Guid.Parse(str);
        if (underlying == typeof(DateTime))       return DateTime.Parse(str, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);
        if (underlying == typeof(DateTimeOffset)) return DateTimeOffset.Parse(str, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);
        if (underlying == typeof(TimeSpan))       return TimeSpan.Parse(str, CultureInfo.InvariantCulture);
        if (underlying == typeof(float))          return float.Parse(str, CultureInfo.InvariantCulture);
        if (underlying == typeof(double))         return double.Parse(str, CultureInfo.InvariantCulture);
        if (underlying == typeof(decimal))        return decimal.Parse(str, CultureInfo.InvariantCulture);
        if (underlying == typeof(Version))        return Version.Parse(str);
        if (underlying == typeof(Uri))            return new Uri(str);

        throw new NotSupportedException($"Type '{target.FullName}' is not supported by RegistryConfig.");
    }

    private void ThrowIfDisposed()
    {
#if NET7_0_OR_GREATER
        ObjectDisposedException.ThrowIf(_disposed, this);
#else
        if (_disposed) throw new ObjectDisposedException(nameof(RegistryConfigService));
#endif
    }
}
