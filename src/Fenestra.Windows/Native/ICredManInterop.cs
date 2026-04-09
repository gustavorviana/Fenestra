namespace Fenestra.Windows.Native;

/// <summary>
/// Thin managed abstraction over the Windows Credential Manager API.
/// Internal test seam — NOT registered in DI. Implemented by <see cref="CredManInterop"/>
/// against advapi32 and mocked by tests.
/// </summary>
internal interface ICredManInterop
{
    /// <summary>Writes or overwrites a credential. Returns false on failure.</summary>
    bool TryWrite(string targetName, string userName, byte[] secretBlob);

    /// <summary>Reads a credential by exact target name. Returns false if not found.</summary>
    bool TryRead(string targetName, out CredentialRecord credential);

    /// <summary>Deletes a credential by exact target name. Returns true if it existed and was removed.</summary>
    bool TryDelete(string targetName);

    /// <summary>
    /// Enumerates target names matching the given wildcard filter.
    /// Returns an empty list if nothing matches (including ERROR_NOT_FOUND from the OS).
    /// </summary>
    IReadOnlyList<string> EnumerateTargets(string? filter);
}

/// <summary>
/// Managed representation of a credential read from Credential Manager.
/// The <see cref="SecretBlob"/> is owned by the caller after return.
/// </summary>
internal readonly record struct CredentialRecord(string TargetName, string UserName, byte[] SecretBlob);
