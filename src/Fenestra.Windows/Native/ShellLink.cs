using Fenestra.Windows.Native.Interfaces;
using Fenestra.Windows.Native.Structs;
using System.Runtime.InteropServices;
using System.Text;

namespace Fenestra.Windows.Native;

/// <summary>
/// IDisposable wrapper for a Windows Shell Link (.lnk shortcut).
/// Manages the COM lifecycle of IShellLinkW, IPropertyStore, and IPersistFile.
/// Use <see cref="Create"/> to make a new shortcut or <see cref="Open"/> to read an existing one.
/// </summary>
internal sealed class ShellLink : IDisposable
{
    private static readonly Guid CLSID_ShellLink = new("00021401-0000-0000-C000-000000000046");
    private const uint STGM_READWRITE = 2;

    private static readonly PROPERTYKEY PKEY_AppUserModelId = new()
    {
        fmtid = new Guid("9F4C2855-9F79-4B39-A8D0-E1D42DE1D5F3"),
        pid = 5
    };

    private static readonly PROPERTYKEY PKEY_AppUserModelToastActivatorCLSID = new()
    {
        fmtid = new Guid("9F4C2855-9F79-4B39-A8D0-E1D42DE1D5F3"),
        pid = 26
    };

    private const ushort VT_LPWSTR = 31;
    private const ushort VT_CLSID = 72;

    private readonly ComRef<IShellLinkW> _link;
    private readonly string _path;
    private bool _disposed;

    private ShellLink(string path, ComRef<IShellLinkW> link)
    {
        _path = path;
        _link = link;
    }

    public static ShellLink Create(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException("Path cannot be null or empty.", nameof(path));

        var link = CreateShellLinkInstance();

        if (File.Exists(path))
        {
            try
            {
                var persist = (IPersistFile)link.Value;
                persist.Load(path, STGM_READWRITE);
            }
            catch { /* Failed to load — treat as new shortcut */ }
        }

        return new ShellLink(path, link);
    }

    public static ShellLink? Open(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException("Path cannot be null or empty.", nameof(path));

        if (!File.Exists(path))
            return null;

        var link = CreateShellLinkInstance();

        try
        {
            var persist = (IPersistFile)link.Value;
            persist.Load(path, 0);
        }
        catch
        {
            link.Dispose();
            return null;
        }

        return new ShellLink(path, link);
    }

    public string TargetPath
    {
        get
        {
            var sb = new StringBuilder(1024);
            _link.Value.GetPath(sb, sb.Capacity, IntPtr.Zero, 0);
            return sb.ToString();
        }
        set
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("Target path cannot be null or empty.");
            _link.Value.SetPath(value);
        }
    }

    public string WorkingDirectory
    {
        get
        {
            var sb = new StringBuilder(1024);
            _link.Value.GetWorkingDirectory(sb, sb.Capacity);
            return sb.ToString();
        }
        set => _link.Value.SetWorkingDirectory(value ?? "");
    }

    public string Arguments
    {
        get
        {
            var sb = new StringBuilder(1024);
            _link.Value.GetArguments(sb, sb.Capacity);
            return sb.ToString();
        }
        set => _link.Value.SetArguments(value ?? "");
    }

    public string Description
    {
        get
        {
            var sb = new StringBuilder(1024);
            _link.Value.GetDescription(sb, sb.Capacity);
            return sb.ToString();
        }
        set => _link.Value.SetDescription(value ?? "");
    }

    public string? AppUserModelId
    {
        get
        {
            var store = (IPropertyStore)_link.Value;
            var key = PKEY_AppUserModelId;
            ThrowIfFailed(store.GetValue(ref key, out var pv));

            try
            {
                if (pv.vt != VT_LPWSTR || pv.p == IntPtr.Zero)
                    return null;
                return Marshal.PtrToStringUni(pv.p);
            }
            finally { PropVariantClear(ref pv); }
        }
        set
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("AppId cannot be null or empty.");

            var store = (IPropertyStore)_link.Value;
            var key = PKEY_AppUserModelId;
            var pv = new PROPVARIANT
            {
                vt = VT_LPWSTR,
                p = Marshal.StringToCoTaskMemUni(value)
            };

            try { ThrowIfFailed(store.SetValue(ref key, pv)); }
            finally { PropVariantClear(ref pv); }
        }
    }

    public Guid? ToastActivatorClsid
    {
        set
        {
            if (value == null) return;

            var store = (IPropertyStore)_link.Value;
            var key = PKEY_AppUserModelToastActivatorCLSID;
            var guidBytes = value.Value.ToByteArray();
            var pv = new PROPVARIANT
            {
                vt = VT_CLSID,
                p = Marshal.AllocCoTaskMem(guidBytes.Length)
            };
            Marshal.Copy(guidBytes, 0, pv.p, guidBytes.Length);

            try { ThrowIfFailed(store.SetValue(ref key, pv)); }
            finally { PropVariantClear(ref pv); }
        }
    }

    public ShellLink SetIconLocation(string path, int index = 0)
    {
        if (!string.IsNullOrWhiteSpace(path))
            _link.Value.SetIconLocation(path, index);
        return this;
    }

    public void Save()
    {
        var directory = Path.GetDirectoryName(_path);
        if (!string.IsNullOrEmpty(directory))
            Directory.CreateDirectory(directory);

        var store = (IPropertyStore)_link.Value;
        ThrowIfFailed(store.Commit());

        var persist = (IPersistFile)_link.Value;
        persist.Save(_path, true);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _link.Dispose();
    }

    private static ComRef<IShellLinkW> CreateShellLinkInstance()
    {
        var type = Type.GetTypeFromCLSID(CLSID_ShellLink)
            ?? throw new InvalidOperationException($"Failed to get type for CLSID '{CLSID_ShellLink:B}'.");
        var instance = Activator.CreateInstance(type)
            ?? throw new InvalidOperationException($"Failed to create COM instance for CLSID '{CLSID_ShellLink:B}'.");
        return new ComRef<IShellLinkW>((IShellLinkW)instance);
    }

    private static void ThrowIfFailed(int hr)
    {
        if (hr < 0) Marshal.ThrowExceptionForHR(hr);
    }

    [DllImport("ole32.dll")]
    private static extern int PropVariantClear(ref PROPVARIANT pvar);
}
