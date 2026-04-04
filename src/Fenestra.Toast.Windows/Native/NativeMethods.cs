using System.Runtime.InteropServices;
using System.Text;

namespace Fenestra.Toast.Windows.Native;

internal static class NativeMethods
{
    internal const int APPMODEL_ERROR_NO_PACKAGE = 15700;
    internal const uint STGM_READ = 0x00000000;


    [DllImport("shell32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    internal static extern int SetCurrentProcessExplicitAppUserModelID(string appID);

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
    internal static extern int GetCurrentPackageFullName(ref int packageFullNameLength, StringBuilder? packageFullName);
}