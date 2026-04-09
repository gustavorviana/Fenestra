namespace Fenestra.Windows;

/// <summary>
/// Stores and retrieves secrets via the Windows Credential Manager (DPAPI-encrypted per user).
///
/// <para>
/// <b>Security model</b>: this protects secrets at rest on disk (via DPAPI) and between
/// different Windows users on the same machine. It does <b>not</b> protect against other
/// processes running as the same user — any such process that knows the target name can
/// read the credential. See <c>docs/credential-vault.md</c> for the complete threat model.
/// </para>
/// <para>
/// Provides both <see cref="string"/> overloads (ergonomic) and <see cref="T:byte[]"/>
/// overloads (deterministic zero-fill via <see cref="StoredBinaryCredential"/>).
/// </para>
/// </summary>
public interface ICredentialVault
{
    // --- String overloads (ergonomia) ---

    /// <summary>
    /// Stores a secret under the given target. Overwrites existing values silently.
    /// The string is encoded as UTF-16 LE before persistence.
    /// </summary>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="target"/> or <paramref name="username"/> fails validation,
    /// or when <paramref name="secret"/> encoded as UTF-16 exceeds the Windows Credential
    /// Manager limit (<c>CRED_MAX_CREDENTIAL_BLOB_SIZE</c> = 2560 bytes).
    /// </exception>
    /// <exception cref="InvalidOperationException">Thrown when the underlying write operation fails.</exception>
    void Store(string target, string username, string secret);

    /// <summary>
    /// Reads a previously stored credential as a string. Returns null if not found.
    /// </summary>
    StoredCredential? Read(string target);

    // --- Byte overloads (controle determinístico de memória) ---

    /// <summary>
    /// Stores an arbitrary byte buffer under the given target. Overwrites existing values silently.
    /// The caller retains ownership of <paramref name="secretBytes"/>; Fenestra does NOT zero it.
    /// </summary>
    /// <exception cref="ArgumentException">
    /// Thrown on validation failure or when <paramref name="secretBytes"/>.Length exceeds
    /// <c>CRED_MAX_CREDENTIAL_BLOB_SIZE</c> (2560 bytes).
    /// </exception>
    /// <exception cref="InvalidOperationException">Thrown when the underlying write operation fails.</exception>
    void Store(string target, string username, byte[] secretBytes);

    /// <summary>
    /// Reads a previously stored credential as raw bytes. Returns null if not found.
    /// The returned <see cref="StoredBinaryCredential"/> owns the byte buffer; dispose it
    /// (ideally via <c>using</c>) to zero-fill the buffer when you're done with it.
    /// </summary>
    StoredBinaryCredential? ReadBytes(string target);

    // --- Common ---

    /// <summary>
    /// Deletes a credential. Returns true if the credential existed and was deleted.
    /// </summary>
    bool Delete(string target);

    /// <summary>
    /// Enumerates the targets previously stored by this application.
    /// Returns the unprefixed target names (as passed to <see cref="Store(string, string, string)"/>).
    /// </summary>
    IReadOnlyList<string> Enumerate();
}

/// <summary>
/// String credential returned by <see cref="ICredentialVault.Read"/>.
/// <see cref="ToString"/> is overridden to mask the <see cref="Secret"/> field to
/// prevent accidental leak in log output.
/// </summary>
public sealed record StoredCredential(string Target, string Username, string Secret)
{
    /// <summary>
    /// Returns a log-safe representation. The <see cref="Secret"/> field is always
    /// rendered as <c>***</c> to prevent leak via string interpolation or structured logging.
    /// </summary>
    public override string ToString()
        => $"StoredCredential {{ Target = {Target}, Username = {Username}, Secret = *** }}";
}

/// <summary>
/// Binary credential returned by <see cref="ICredentialVault.ReadBytes"/>.
/// Implements <see cref="IDisposable"/> so callers can zero-fill the <see cref="Secret"/>
/// buffer deterministically.
///
/// <para>
/// This is a <c>class</c> (not a record) because:
/// </para>
/// <list type="bullet">
///   <item><description><see cref="T:byte[]"/> equality in records compares by reference, not content.</description></item>
///   <item><description>Records are intended to be immutable value-like; we need mutability for zero-fill.</description></item>
///   <item><description><see cref="IDisposable"/> semantics don't fit the record model.</description></item>
/// </list>
/// </summary>
public sealed class StoredBinaryCredential : IDisposable
{
    /// <summary>The unprefixed target name as passed to <see cref="ICredentialVault.Store(string, string, byte[])"/>.</summary>
    public string Target { get; }

    /// <summary>The username associated with this credential.</summary>
    public string Username { get; }

    /// <summary>
    /// The raw secret buffer. Zero-filled on <see cref="Dispose"/>.
    /// <b>Do not retain references to this buffer after disposing the containing instance</b> —
    /// the content will be zeroed and the array reused semantically.
    /// </summary>
    public byte[] Secret { get; }

    internal StoredBinaryCredential(string target, string username, byte[] secret)
    {
        Target = target;
        Username = username;
        Secret = secret;
    }

    /// <summary>
    /// Zero-fills the <see cref="Secret"/> buffer. Safe to call multiple times.
    /// </summary>
    public void Dispose() => Array.Clear(Secret, 0, Secret.Length);

    /// <summary>
    /// Returns a log-safe representation. The secret content is replaced by a length summary.
    /// </summary>
    public override string ToString()
        => $"StoredBinaryCredential {{ Target = {Target}, Username = {Username}, Secret = <{Secret.Length} bytes> }}";
}
