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
    /// <summary>CLSID for the Shell Link COM coclass (00021401-...).</summary>
    private static readonly Guid CLSID_ShellLink = new("00021401-0000-0000-C000-000000000046");

    /// <summary>STGM_READWRITE — read/write access for IPersistFile.Load.</summary>
    private const uint STGM_READWRITE = 2;

    /// <summary>PKEY_AppUserModel_ID — {9F4C2855-...}, pid=5. Sets the AUMID on the shortcut.</summary>
    private static readonly PROPERTYKEY PKEY_AppUserModelId = new()
    {
        fmtid = new Guid("9F4C2855-9F79-4B39-A8D0-E1D42DE1D5F3"),
        pid = 5
    };

    /// <summary>PKEY_AppUserModel_ToastActivatorCLSID — {9F4C2855-...}, pid=26. Sets the COM activator CLSID for background toast activation.</summary>
    private static readonly PROPERTYKEY PKEY_AppUserModelToastActivatorCLSID = new()
    {
        fmtid = new Guid("9F4C2855-9F79-4B39-A8D0-E1D42DE1D5F3"),
        pid = 26
    };

    /// <summary>VARIANT type for a wide string pointer (LPWSTR). Value: 31.</summary>
    private const ushort VT_LPWSTR = 31;

    /// <summary>VARIANT type for a CLSID (GUID). Value: 72.</summary>
    private const ushort VT_CLSID = 72;

    private readonly IShellLinkW _link;
    private readonly string _path;
    private bool _disposed;

    private ShellLink(string path, IShellLinkW link)
    {
        _path = path;
        _link = link;
    }

    /// <summary>
    /// Opens an existing shortcut or creates a new one targeting the specified executable.
    /// </summary>
    public static ShellLink Create(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException("Path cannot be null or empty.", nameof(path));

        var link = ComFactory.Create<IShellLinkW>(CLSID_ShellLink);

        if (File.Exists(path))
        {
            try
            {
                var persist = (IPersistFile)link;
                persist.Load(path, STGM_READWRITE);
            }
            catch
            {
                // Failed to load — treat as new shortcut
            }
        }

        return new ShellLink(path, link);
    }

    /// <summary>
    /// Opens an existing shortcut. Returns null if the file does not exist.
    /// </summary>
    public static ShellLink? Open(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException("Path cannot be null or empty.", nameof(path));

        if (!File.Exists(path))
            return null;

        var link = ComFactory.Create<IShellLinkW>(CLSID_ShellLink);
        var persist = (IPersistFile)link;

        try
        {
            persist.Load(path, 0);
        }
        catch
        {
            Marshal.ReleaseComObject(link);
            return null;
        }

        return new ShellLink(path, link);
    }

    /// <summary>
    /// The target executable path of this shortcut.
    /// </summary>
    public string TargetPath
    {
        get
        {
            var sb = new StringBuilder(1024);
            _link.GetPath(sb, sb.Capacity, IntPtr.Zero, 0);
            return sb.ToString();
        }
        set
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("Target path cannot be null or empty.");
            _link.SetPath(value);
        }
    }

    /// <summary>
    /// The working directory.
    /// </summary>
    public string WorkingDirectory
    {
        get
        {
            var sb = new StringBuilder(1024);
            _link.GetWorkingDirectory(sb, sb.Capacity);
            return sb.ToString();
        }
        set => _link.SetWorkingDirectory(value ?? "");
    }

    /// <summary>
    /// Command line arguments.
    /// </summary>
    public string Arguments
    {
        get
        {
            var sb = new StringBuilder(1024);
            _link.GetArguments(sb, sb.Capacity);
            return sb.ToString();
        }
        set => _link.SetArguments(value ?? "");
    }

    /// <summary>
    /// The shortcut description.
    /// </summary>
    public string Description
    {
        get
        {
            var sb = new StringBuilder(1024);
            _link.GetDescription(sb, sb.Capacity);
            return sb.ToString();
        }
        set => _link.SetDescription(value ?? "");
    }

    /// <summary>
    /// The AppUserModelID property. Used by Windows for toast notification identification.
    /// </summary>
    public string? AppUserModelId
    {
        get
        {
            var store = (IPropertyStore)_link;
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

            var store = (IPropertyStore)_link;
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

    /// <summary>
    /// The ToastActivatorCLSID property. Used for background toast activation.
    /// </summary>
    public Guid? ToastActivatorClsid
    {
        set
        {
            if (value == null) return;

            var store = (IPropertyStore)_link;
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

    /// <summary>
    /// Sets the icon location and index.
    /// </summary>
    public ShellLink SetIconLocation(string path, int index = 0)
    {
        if (!string.IsNullOrWhiteSpace(path))
            _link.SetIconLocation(path, index);
        return this;
    }

    /// <summary>
    /// Commits property changes and saves the shortcut to disk.
    /// </summary>
    public void Save()
    {
        var directory = Path.GetDirectoryName(_path);
        if (!string.IsNullOrEmpty(directory))
            Directory.CreateDirectory(directory);

        var store = (IPropertyStore)_link;
        ThrowIfFailed(store.Commit());

        var persist = (IPersistFile)_link;
        persist.Save(_path, true);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        Marshal.ReleaseComObject(_link);
    }

    private static void ThrowIfFailed(uint hr)
    {
        if (hr != 0) Marshal.ThrowExceptionForHR(unchecked((int)hr));
    }

    [DllImport("ole32.dll")]
    private static extern int PropVariantClear(ref PROPVARIANT pvar);
}
