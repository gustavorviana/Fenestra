using Fenestra.Core.Models;
using Fenestra.Windows.Native;
using System.Text;

namespace Fenestra.Windows.Services;

/// <summary>
/// <see cref="ICredentialVault"/> implementation over the Windows Credential Manager.
///
/// <para>
/// NOTE: NO logging is allowed in this class. Target names appear in the Credential Manager
/// UI and are not themselves secret, but combining them with secret content in log output is
/// still a leak vector. See <c>plans/fenestra.md</c> — Security Model, mitigation M2.
/// </para>
/// </summary>
internal sealed class CredentialVault : ICredentialVault
{
    private const int MaxTargetLength = 256;
    private const int MaxUsernameLength = 256;

    // CRED_MAX_CREDENTIAL_BLOB_SIZE from wincred.h — hard limit enforced by CredWriteW.
    // Exceeding this at the native boundary returns ERROR_INVALID_PARAMETER; we fail
    // earlier with a clearer exception message.
    private const int MaxSecretBytes = 5 * 512; // 2560 bytes

    private readonly ICredManInterop _interop;
    private readonly string _prefix; // "{AppId}:"

    public CredentialVault(AppInfo appInfo, ICredManInterop? interop = null)
    {
        Platform.EnsureWindows();
        if (appInfo is null) throw new ArgumentNullException(nameof(appInfo));
        _interop = interop ?? new CredManInterop();
        _prefix = $"{appInfo.AppId}:";
    }

    // --- Store(string) ---

    public void Store(string target, string username, string secret)
    {
        if (secret is null) throw new ArgumentNullException(nameof(secret));

        // Validate size BEFORE allocating. GetByteCount does not allocate — it measures.
        // We validate bytes (not chars) because emojis/surrogate pairs occupy 4 bytes in UTF-16.
        var byteCount = Encoding.Unicode.GetByteCount(secret);
        ValidateSecretByteCount(byteCount, nameof(secret));

        var bytes = Encoding.Unicode.GetBytes(secret);
        try
        {
            Store(target, username, bytes);
        }
        finally
        {
            // M4: Fenestra allocated this buffer, Fenestra zeros it.
            Array.Clear(bytes, 0, bytes.Length);
        }
    }

    // --- Store(byte[]) — canonical path ---

    public void Store(string target, string username, byte[] secretBytes)
    {
        ValidateTarget(target);
        ValidateUsername(username);
        if (secretBytes is null) throw new ArgumentNullException(nameof(secretBytes));
        ValidateSecretByteCount(secretBytes.Length, nameof(secretBytes));

        var fullTarget = _prefix + target;
        // M2: exception message must NOT include target, username, or secret.
        if (!_interop.TryWrite(fullTarget, username, secretBytes))
            throw new InvalidOperationException("Failed to store credential.");

        // NOTE: do NOT zero secretBytes — caller owns the buffer and decides when to clear.
    }

    // --- Read(string) ---

    public StoredCredential? Read(string target)
    {
        using var binary = ReadBytes(target);
        if (binary is null) return null;
        var secret = Encoding.Unicode.GetString(binary.Secret);
        return new StoredCredential(target, binary.Username, secret);
        // `using` → binary.Dispose() → Array.Clear(binary.Secret) on scope exit.
    }

    // --- ReadBytes ---

    public StoredBinaryCredential? ReadBytes(string target)
    {
        ValidateTarget(target);
        var fullTarget = _prefix + target;

        if (!_interop.TryRead(fullTarget, out var raw))
            return null;

        // Transfer ownership of raw.SecretBlob to the returned StoredBinaryCredential.
        // The caller is responsible for Dispose() to zero-fill.
        return new StoredBinaryCredential(target, raw.UserName, raw.SecretBlob);
    }

    // --- Delete ---

    public bool Delete(string target)
    {
        ValidateTarget(target);
        return _interop.TryDelete(_prefix + target);
    }

    // --- Enumerate (M5: OS filter + defense-in-depth managed re-filter) ---

    public IReadOnlyList<string> Enumerate()
    {
        var raw = _interop.EnumerateTargets(_prefix + "*");
        var result = new List<string>(raw.Count);
        foreach (var t in raw)
        {
            if (t.StartsWith(_prefix, StringComparison.Ordinal))
                result.Add(t.Substring(_prefix.Length));
        }
        return result;
    }

    // --- M1: strict input validation ---

    private static void ValidateTarget(string target)
    {
        if (target is null) throw new ArgumentNullException(nameof(target));
        if (string.IsNullOrWhiteSpace(target))
            throw new ArgumentException("Target cannot be empty or whitespace.", nameof(target));
        if (target.Length > MaxTargetLength)
            throw new ArgumentException(
                $"Target exceeds maximum length of {MaxTargetLength} characters.", nameof(target));
        foreach (var c in target)
        {
            if (char.IsControl(c))
                throw new ArgumentException("Target cannot contain control characters.", nameof(target));
        }
    }

    private static void ValidateUsername(string username)
    {
        if (username is null) throw new ArgumentNullException(nameof(username));
        if (username.Length > MaxUsernameLength)
            throw new ArgumentException(
                $"Username exceeds maximum length of {MaxUsernameLength} characters.", nameof(username));
        foreach (var c in username)
        {
            if (char.IsControl(c))
                throw new ArgumentException("Username cannot contain control characters.", nameof(username));
        }
    }

    private static void ValidateSecretByteCount(int byteCount, string paramName)
    {
        if (byteCount > MaxSecretBytes)
            throw new ArgumentException(
                $"Secret exceeds the Windows Credential Manager limit of {MaxSecretBytes} bytes (got {byteCount}).",
                paramName);
    }
}
