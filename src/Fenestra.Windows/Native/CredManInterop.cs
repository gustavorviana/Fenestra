using System.Runtime.InteropServices;

namespace Fenestra.Windows.Native;

/// <summary>
/// Concrete <see cref="ICredManInterop"/> implementation over advapi32 Credential Manager APIs.
///
/// This class contains all the marshal/ptr handling for the Credential Manager surface.
/// It is NOT testable directly (pure P/Invoke) — callers test the layers above it
/// (<see cref="Services.CredentialVault"/>) by mocking <see cref="ICredManInterop"/>.
/// </summary>
internal sealed class CredManInterop : ICredManInterop
{
    public bool TryWrite(string targetName, string userName, byte[] secretBlob)
    {
        // Allocate native copies of strings + blob. We must free each of these in
        // the finally block. Do NOT call CredFree on these — CredFree is only for
        // pointers returned BY CredRead/CredEnumerate.
        var pTarget = IntPtr.Zero;
        var pUser = IntPtr.Zero;
        var pBlob = IntPtr.Zero;

        try
        {
            pTarget = Marshal.StringToHGlobalUni(targetName);
            pUser = Marshal.StringToHGlobalUni(userName);
            pBlob = Marshal.AllocHGlobal(secretBlob.Length);
            Marshal.Copy(secretBlob, 0, pBlob, secretBlob.Length);

            var cred = new CredManNative.CREDENTIAL
            {
                Type = CredManNative.CRED_TYPE_GENERIC,
                TargetName = pTarget,
                UserName = pUser,
                CredentialBlob = pBlob,
                CredentialBlobSize = secretBlob.Length,
                Persist = CredManNative.CRED_PERSIST_LOCAL_MACHINE,
            };

            return CredManNative.CredWriteW(ref cred, 0);
        }
        finally
        {
            // Zero the native blob before freeing to shrink the exposure window
            if (pBlob != IntPtr.Zero)
            {
                for (int i = 0; i < secretBlob.Length; i++)
                    Marshal.WriteByte(pBlob, i, 0);
                Marshal.FreeHGlobal(pBlob);
            }
            if (pTarget != IntPtr.Zero) Marshal.ZeroFreeGlobalAllocUnicode(pTarget);
            if (pUser != IntPtr.Zero) Marshal.ZeroFreeGlobalAllocUnicode(pUser);
        }
    }

    public bool TryRead(string targetName, out CredentialRecord credential)
    {
        credential = default;

        if (!CredManNative.CredReadW(targetName, CredManNative.CRED_TYPE_GENERIC, 0, out IntPtr pCred))
            return false; // not found or other failure

        try
        {
            var cred = Marshal.PtrToStructure<CredManNative.CREDENTIAL>(pCred);
            var user = cred.UserName != IntPtr.Zero
                ? Marshal.PtrToStringUni(cred.UserName) ?? string.Empty
                : string.Empty;
            var target = cred.TargetName != IntPtr.Zero
                ? Marshal.PtrToStringUni(cred.TargetName) ?? string.Empty
                : string.Empty;

            var blob = new byte[cred.CredentialBlobSize];
            if (cred.CredentialBlobSize > 0 && cred.CredentialBlob != IntPtr.Zero)
                Marshal.Copy(cred.CredentialBlob, blob, 0, cred.CredentialBlobSize);

            credential = new CredentialRecord(target, user, blob);
            return true;
        }
        finally
        {
            CredManNative.CredFree(pCred);
        }
    }

    public bool TryDelete(string targetName)
    {
        return CredManNative.CredDeleteW(targetName, CredManNative.CRED_TYPE_GENERIC, 0);
    }

    public IReadOnlyList<string> EnumerateTargets(string? filter)
    {
        if (!CredManNative.CredEnumerateW(filter, 0, out int count, out IntPtr pCreds))
        {
            // ERROR_NOT_FOUND just means "no matches" — treat as empty list, not error
            return Array.Empty<string>();
        }

        try
        {
            if (count == 0 || pCreds == IntPtr.Zero)
                return Array.Empty<string>();

            var result = new List<string>(count);
            var ptrSize = IntPtr.Size;
            for (int i = 0; i < count; i++)
            {
                var pCred = Marshal.ReadIntPtr(pCreds, i * ptrSize);
                if (pCred == IntPtr.Zero) continue;

                var cred = Marshal.PtrToStructure<CredManNative.CREDENTIAL>(pCred);
                if (cred.TargetName != IntPtr.Zero)
                {
                    var name = Marshal.PtrToStringUni(cred.TargetName);
                    if (!string.IsNullOrEmpty(name))
                        result.Add(name!);
                }
            }
            return result;
        }
        finally
        {
            CredManNative.CredFree(pCreds);
        }
    }
}
