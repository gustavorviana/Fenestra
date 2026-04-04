using System.Runtime.InteropServices;

namespace Fenestra.Windows.Native.Structs;

[StructLayout(LayoutKind.Sequential, Pack = 4)]
internal struct PROPERTYKEY
{
    public Guid fmtid;
    public uint pid;
}