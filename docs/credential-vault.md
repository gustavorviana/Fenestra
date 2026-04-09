# Credential Vault

Stores and retrieves secrets via the **Windows Credential Manager**, a DPAPI-encrypted per-user secret store that's managed by Windows and visible in the Control Panel.

## Overview

`IRegistryConfig` persists in `HKCU\SOFTWARE\{AppName}` as cleartext — inadequate for secrets like API tokens, refresh tokens, or user passwords. The Credential Vault wraps the Windows `advapi32` Credential Manager APIs (`CredWrite`, `CredRead`, `CredDelete`, `CredEnumerate`) to provide a small, opinionated, log-safe credential store.

Credentials are:

- **Encrypted at rest** via DPAPI (user-scoped key derived from the Windows login password)
- **Isolated per Windows user** — another user on the same machine cannot read them
- **Manageable via the standard Windows UI** (Control Panel → User Accounts → Credential Manager → Windows Credentials)
- **Namespaced per app** via an internal `{AppId}:` prefix so different Fenestra apps don't collide

## Registration

```csharp
var builder = FenestraApplication.CreateBuilder(args);
builder.UseMainWindow<MainWindow>();
builder.Services.AddWindowsCredentialVault();
builder.RegisterWindows();
```

Then inject `ICredentialVault` where you need it:

```csharp
public class AuthService
{
    private readonly ICredentialVault _vault;
    public AuthService(ICredentialVault vault) => _vault = vault;
}
```

## Usage

### String overload (ergonomic — the common case)

Use this for API tokens, short passwords, and anything that naturally lives as a string.

```csharp
var vault = services.GetRequiredService<ICredentialVault>();

// Store
vault.Store("github-pat", "alice", "ghp_xxxxxxxxxxxxxxxxxxxx");

// Read
var cred = vault.Read("github-pat");
if (cred is not null)
{
    _httpClient.DefaultRequestHeaders.Authorization =
        new AuthenticationHeaderValue("Bearer", cred.Secret);
}

// Delete
vault.Delete("github-pat");
```

The returned `StoredCredential` is a `record` with a `ToString()` override that masks `Secret`, so accidental logging like `_logger.LogInformation("Got {Cred}", cred)` does NOT leak the secret.

### Byte[] overload (deterministic memory control)

Use this for cryptographic keys, random bytes, or any case where you want to zero-fill the buffer deterministically via `using`.

```csharp
// Derive an AES key from a passphrase and store it
var key = new Rfc2898DeriveBytes(passphrase, salt, 100_000, HashAlgorithmName.SHA256).GetBytes(32);
try
{
    vault.Store("aes-master-key", "alice", key);
}
finally
{
    Array.Clear(key, 0, key.Length); // caller owns the buffer — caller zeros
}

// Read and use with deterministic cleanup
using var stored = vault.ReadBytes("aes-master-key");
if (stored is not null)
{
    using var aes = Aes.Create();
    aes.Key = stored.Secret; // aes.Key copies the bytes internally
    // ... decrypt a file with aes ...
}
// `using var stored` → Array.Clear(stored.Secret) on scope exit
```

The returned `StoredBinaryCredential` is an `IDisposable` class — the `Dispose()` method zero-fills `Secret`. Always use it with `using`.

### Enumerate

Lists the targets previously stored by this application (with the `{AppId}:` prefix stripped):

```csharp
foreach (var targetName in vault.Enumerate())
    Console.WriteLine(targetName);
```

Only credentials stored through `ICredentialVault` by this application's `AppId` are returned — credentials from other apps are filtered out automatically.

## Security Model

**Read this section carefully before using Credential Vault for anything sensitive.** The Windows Credential Manager has a specific security model, and understanding its limits is essential.

### 4.1 What this protects

- **At rest on disk**: credentials are encrypted via DPAPI with a key derived from the user's Windows login password. If someone steals the disk but the user isn't logged in, the credentials are inaccessible.
- **Cross-user isolation**: another Windows user on the same machine cannot read your credentials (except Admin/SYSTEM with specific tools).
- **Log-safe by default**: `StoredCredential.ToString()` masks the `Secret` field; `StoredBinaryCredential.ToString()` shows only the byte count; `ICredentialVault` does **not** log internally.

### 4.2 What this does NOT protect

Be honest about the limits:

- **NOT protected against processes running as the same Windows user.** Any code running as you that knows the target name can read the credential directly via Win32 `CredReadW`. There is **no ACL per process** — the Credential Manager only isolates by user. Malware running as you has full access.
- **Target names are NOT secret.** They appear in the Credential Manager UI (Control Panel → User Accounts → Credential Manager → Windows Credentials). The `{AppId}:` prefix is a **namespace**, not a security boundary.
- **Another app can call `CredReadW("YourApp:api-token", ...)` directly** — Windows only verifies the calling user, not the calling application.
- **`string secret` lives in your managed heap.** Once a secret enters the caller's code as a `string`, it stays in the managed heap until the GC collects it. Fenestra has no control over that; keep the scope short or use the `byte[]` overload.

### 4.3 Anti-patterns

**❌ Do not:**

```csharp
// Leaks the secret in structured log output
_logger.LogInformation("Got credential: {Cred}", vault.Read("api-token"));

// Leaks the secret via manual interpolation
_logger.LogInformation($"Got {vault.Read("api-token")?.Secret}");

// Retains the secret in a long-lived field
public class MyService
{
    private readonly string _token;  // lives for the whole service lifetime
    public MyService(ICredentialVault v) => _token = v.Read("api-token")?.Secret!;
}

// Stores a user-typed secret without validating it
vault.Store(userTypedTarget, userTypedUsername, userTypedSecret);
```

**✅ Do:**

```csharp
// Use in the smallest possible scope
public async Task<string> FetchDataAsync()
{
    var cred = _vault.Read("api-token");
    if (cred is null) throw new InvalidOperationException("API token not configured.");

    using var client = new HttpClient();
    client.DefaultRequestHeaders.Authorization =
        new AuthenticationHeaderValue("Bearer", cred.Secret);
    return await client.GetStringAsync("https://api.example.com/data");
    // `cred` goes out of scope here; its string secret will be GC'd eventually
}

// For high-sensitivity secrets, prefer the byte[] overload with deterministic cleanup
using var stored = _vault.ReadBytes("private-key");

// Log only what is safe to log
_logger.LogInformation("Loaded credential for target {Target}", cred.Target);
```

### 4.4 When NOT to use this

- **Secrets that need to leave memory deterministically, guaranteed** — no managed-code solution can guarantee this 100%. Use out-of-process vaults (HSM, Azure Key Vault, AWS KMS, HashiCorp Vault).
- **Secrets shared between Windows users** — DPAPI is per-user, so a credential stored by User A cannot be read by User B.
- **High-value secrets under compliance (PCI, HIPAA, etc.)** — check your compliance framework; Credential Manager may or may not qualify, and you'll probably need an audited external vault.
- **Secrets that should be bound to a specific application** — Windows Credential Manager cannot enforce "only this `.exe` can read this secret". Client-side app authentication is a fundamentally unsolved problem in client software (it's the same reason DRM can be bypassed).

### 4.5 Choosing between the string and byte[] overloads

**Use the `string` overload when:**

- The secret is an API token, short password, or other text that goes directly into an HTTP header or similar.
- You don't have a requirement to zero memory deterministically.
- Ergonomics and readability matter more than fine-grained control.

**Use the `byte[]` overload (and `ReadBytes`) when:**

- The secret is a cryptographic key (AES key, HMAC key, encryption IV, etc.).
- You want deterministic zero-fill via `using` / `Dispose`.
- The secret is not textual (e.g., random bytes, PBKDF2-derived).
- You're concerned about the secret being interned in the string pool or lingering in the managed heap.

Both use the same underlying storage (Credential Manager + DPAPI) — the difference is purely about how the buffer is handled on the Fenestra/caller side, not about what Windows does with the data.

## Limitations

**Maximum secret size: 2560 bytes** (`CRED_MAX_CREDENTIAL_BLOB_SIZE` from the Windows Credential Manager API).

- For `byte[] secret`: `secretBytes.Length` must be ≤ 2560
- For `string secret`: approximately **1280 BMP characters** (ASCII, Latin, most common languages). Characters outside the Basic Multilingual Plane (e.g., emojis) occupy 4 bytes each in UTF-16, so the limit drops proportionally — about 640 emojis, for example.
- Fenestra validates **before** calling the Credential Manager, throwing `ArgumentException` with a clear message instead of the raw HRESULT from `CredWriteW`.

For larger secrets (certificates, RSA private keys, large binary blobs), the Credential Manager is not the right store — consider an encrypted file using `ProtectedData.Protect` (DPAPI directly) or an external KMS.

**Other limits:**

- Only `CRED_TYPE_GENERIC` is supported — no domain password credentials.
- No custom `Attributes` on the `CREDENTIAL` struct — simplified on purpose.
- One credential per target — `Store` overwrites silently.
- Persistence scope is `CRED_PERSIST_LOCAL_MACHINE`, hard-coded. The credential does **not** travel with a roaming profile.
- `target` name ≤ 256 characters (well under the Windows limit of 32767; enough for any realistic use).
- `username` ≤ 256 characters.

## Exceptions

| Exception | When |
|---|---|
| `ArgumentNullException` | `target`, `username`, or `secret` is `null` |
| `ArgumentException` | `target` is empty/whitespace; `target` or `username` exceeds length limit or contains control characters (`\0`, `\r`, `\n`, etc.); `secret` exceeds the 2560-byte limit |
| `InvalidOperationException` | The underlying `CredWriteW` call failed. Exception messages deliberately do **not** include the target, username, or secret — they only say "Failed to store credential." |
| `PlatformNotSupportedException` | Running on a non-Windows platform |

## Verification manual (smoke test)

After running code that stores a credential, you can verify it went through the real Credential Manager:

1. Open **Control Panel → User Accounts → Credential Manager** (or press Win key and search for "Credential Manager")
2. Click the **Windows Credentials** tab
3. Look for entries prefixed with your `AppId` — e.g., `MyApp:github-pat`
4. After calling `Delete`, confirm the entry is gone

This is useful for debugging and for convincing yourself that the integration is actually working end-to-end.
