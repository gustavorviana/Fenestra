using Fenestra.Core.Models;
using Fenestra.Windows;
using Fenestra.Windows.Native;
using Fenestra.Windows.Services;
using NSubstitute;
using System.Reflection;
using System.Text;

namespace Fenestra.Windows.Tests.Services;

public class CredentialVaultTests
{
    private const string AppId = "TestApp";
    private const string Prefix = "TestApp:";
    private const int MaxSecretBytes = 2560;

    private readonly AppInfo _appInfo = new("Test App", AppId, new Version(1, 0));
    private readonly ICredManInterop _interop = Substitute.For<ICredManInterop>();

    private CredentialVault CreateSut() => new(_appInfo, _interop);

    // =====================================================================
    // Construtor
    // =====================================================================

    [Fact]
    public void Ctor_NullAppInfo_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => new CredentialVault(null!, _interop));
    }

    [Fact]
    public void Ctor_AcceptsNullInterop_DoesNotThrow()
    {
        var ex = Record.Exception(() => new CredentialVault(_appInfo));
        Assert.Null(ex);
    }

    [Fact]
    public void Ctor_PrefixesStoreCallsWithAppId()
    {
        var sut = CreateSut();
        _interop.TryWrite(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<byte[]>()).Returns(true);

        sut.Store("foo", "user", "bar");

        _interop.Received().TryWrite($"{Prefix}foo", Arg.Any<string>(), Arg.Any<byte[]>());
    }

    // =====================================================================
    // Store(string) — happy path
    // =====================================================================

    [Fact]
    public void StoreString_PrefixesTargetWithAppId()
    {
        var sut = CreateSut();
        _interop.TryWrite(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<byte[]>()).Returns(true);

        sut.Store("mykey", "alice", "token");

        _interop.Received(1).TryWrite($"{Prefix}mykey", "alice", Arg.Any<byte[]>());
    }

    [Fact]
    public void StoreString_EncodesSecretAsUtf16Le()
    {
        var sut = CreateSut();
        byte[]? captured = null;
        _interop.TryWrite(Arg.Any<string>(), Arg.Any<string>(), Arg.Do<byte[]>(b => captured = (byte[])b.Clone()))
            .Returns(true);

        sut.Store("k", "u", "hi");

        Assert.NotNull(captured);
        Assert.Equal(Encoding.Unicode.GetBytes("hi"), captured);
    }

    [Fact]
    public void StoreString_PassesUsernameThrough()
    {
        var sut = CreateSut();
        _interop.TryWrite(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<byte[]>()).Returns(true);

        sut.Store("k", "bob", "secret");

        _interop.Received().TryWrite(Arg.Any<string>(), "bob", Arg.Any<byte[]>());
    }

    // =====================================================================
    // Store(byte[]) — happy path
    // =====================================================================

    [Fact]
    public void StoreBytes_PrefixesTargetWithAppId()
    {
        var sut = CreateSut();
        _interop.TryWrite(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<byte[]>()).Returns(true);

        sut.Store("bkey", "alice", new byte[] { 1, 2, 3 });

        _interop.Received(1).TryWrite($"{Prefix}bkey", "alice", Arg.Any<byte[]>());
    }

    [Fact]
    public void StoreBytes_PassesBufferUnchanged()
    {
        var sut = CreateSut();
        var input = new byte[] { 10, 20, 30, 40 };
        byte[]? captured = null;
        _interop.TryWrite(Arg.Any<string>(), Arg.Any<string>(), Arg.Do<byte[]>(b => captured = b))
            .Returns(true);

        sut.Store("k", "u", input);

        Assert.Same(input, captured);
    }

    [Fact]
    public void StoreBytes_PassesUsernameThrough()
    {
        var sut = CreateSut();
        _interop.TryWrite(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<byte[]>()).Returns(true);

        sut.Store("k", "charlie", new byte[] { 1 });

        _interop.Received().TryWrite(Arg.Any<string>(), "charlie", Arg.Any<byte[]>());
    }

    [Fact]
    public void StoreBytes_DoesNotZeroCallerBuffer()
    {
        var sut = CreateSut();
        var input = new byte[] { 7, 8, 9 };
        _interop.TryWrite(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<byte[]>()).Returns(true);

        sut.Store("k", "u", input);

        Assert.Equal(new byte[] { 7, 8, 9 }, input);
    }

    // =====================================================================
    // Store — input validation (M1)
    // =====================================================================

    [Fact]
    public void StoreString_NullTarget_ThrowsArgumentNullException()
    {
        var sut = CreateSut();
        Assert.Throws<ArgumentNullException>(() => sut.Store(null!, "u", "s"));
    }

    [Fact]
    public void StoreBytes_NullTarget_ThrowsArgumentNullException()
    {
        var sut = CreateSut();
        Assert.Throws<ArgumentNullException>(() => sut.Store(null!, "u", new byte[] { 1 }));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void StoreString_EmptyOrWhitespaceTarget_ThrowsArgumentException(string target)
    {
        var sut = CreateSut();
        Assert.Throws<ArgumentException>(() => sut.Store(target, "u", "s"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void StoreBytes_EmptyOrWhitespaceTarget_ThrowsArgumentException(string target)
    {
        var sut = CreateSut();
        Assert.Throws<ArgumentException>(() => sut.Store(target, "u", new byte[] { 1 }));
    }

    [Fact]
    public void Store_TargetTooLong_ThrowsArgumentException()
    {
        var sut = CreateSut();
        var longTarget = new string('a', 257);

        Assert.Throws<ArgumentException>(() => sut.Store(longTarget, "u", "s"));
    }

    [Theory]
    [InlineData('\0')]
    [InlineData('\r')]
    [InlineData('\n')]
    [InlineData('\t')]
    public void Store_TargetWithControlChar_ThrowsArgumentException(char c)
    {
        var sut = CreateSut();
        Assert.Throws<ArgumentException>(() => sut.Store($"foo{c}bar", "u", "s"));
    }

    [Fact]
    public void Store_NullUsername_ThrowsArgumentNullException()
    {
        var sut = CreateSut();
        Assert.Throws<ArgumentNullException>(() => sut.Store("t", null!, "s"));
    }

    [Fact]
    public void Store_UsernameTooLong_ThrowsArgumentException()
    {
        var sut = CreateSut();
        var longUser = new string('u', 257);

        Assert.Throws<ArgumentException>(() => sut.Store("t", longUser, "s"));
    }

    [Fact]
    public void Store_UsernameWithControlChar_ThrowsArgumentException()
    {
        var sut = CreateSut();
        Assert.Throws<ArgumentException>(() => sut.Store("t", "bad\nuser", "s"));
    }

    [Fact]
    public void StoreString_NullSecret_ThrowsArgumentNullException()
    {
        var sut = CreateSut();
        Assert.Throws<ArgumentNullException>(() => sut.Store("t", "u", (string)null!));
    }

    [Fact]
    public void StoreBytes_NullSecretBytes_ThrowsArgumentNullException()
    {
        var sut = CreateSut();
        Assert.Throws<ArgumentNullException>(() => sut.Store("t", "u", (byte[])null!));
    }

    // =====================================================================
    // Store — secret size limit (M1 — CRED_MAX_CREDENTIAL_BLOB_SIZE = 2560)
    // =====================================================================

    [Fact]
    public void StoreBytes_SecretAtMax_Succeeds()
    {
        var sut = CreateSut();
        _interop.TryWrite(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<byte[]>()).Returns(true);

        var secret = new byte[MaxSecretBytes];
        sut.Store("k", "u", secret);

        _interop.Received(1).TryWrite(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<byte[]>());
    }

    [Fact]
    public void StoreBytes_SecretOneByteOverMax_ThrowsArgumentException()
    {
        var sut = CreateSut();
        var secret = new byte[MaxSecretBytes + 1];

        Assert.Throws<ArgumentException>(() => sut.Store("k", "u", secret));
    }

    [Fact]
    public void StoreBytes_SecretExceptionMessageMentionsLimit()
    {
        var sut = CreateSut();
        var secret = new byte[MaxSecretBytes + 1];

        var ex = Assert.Throws<ArgumentException>(() => sut.Store("k", "u", secret));
        Assert.Contains("2560", ex.Message);
    }

    [Fact]
    public void StoreString_SecretAtMaxBmpChars_Succeeds()
    {
        var sut = CreateSut();
        _interop.TryWrite(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<byte[]>()).Returns(true);

        // 1280 BMP chars × 2 bytes = 2560 bytes
        var secret = new string('a', 1280);

        sut.Store("k", "u", secret);

        _interop.Received(1).TryWrite(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<byte[]>());
    }

    [Fact]
    public void StoreString_SecretOneCharOverBmpMax_ThrowsArgumentException()
    {
        var sut = CreateSut();
        var secret = new string('a', 1281); // 2562 bytes

        Assert.Throws<ArgumentException>(() => sut.Store("k", "u", secret));
    }

    [Fact]
    public void StoreString_SecretWithEmojisAtExactLimit_Succeeds()
    {
        var sut = CreateSut();
        _interop.TryWrite(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<byte[]>()).Returns(true);

        // Each emoji (outside BMP) = surrogate pair = 2 chars = 4 bytes in UTF-16.
        // 640 emojis = 2560 bytes exactly.
        var secret = string.Concat(Enumerable.Repeat("😀", 640));
        Assert.Equal(2560, Encoding.Unicode.GetByteCount(secret));

        sut.Store("k", "u", secret);

        _interop.Received(1).TryWrite(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<byte[]>());
    }

    [Fact]
    public void StoreString_SecretWithEmojisOverLimit_ThrowsArgumentException()
    {
        var sut = CreateSut();
        // 641 emojis = 2564 bytes
        var secret = string.Concat(Enumerable.Repeat("😀", 641));

        Assert.Throws<ArgumentException>(() => sut.Store("k", "u", secret));
    }

    [Fact]
    public void StoreString_SecretEmptyString_Succeeds()
    {
        var sut = CreateSut();
        _interop.TryWrite(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<byte[]>()).Returns(true);

        sut.Store("k", "u", "");

        _interop.Received(1).TryWrite(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<byte[]>());
    }

    [Fact]
    public void StoreString_SecretSizeValidatedBeforeAllocation()
    {
        var sut = CreateSut();
        var secret = new string('a', 1281);

        Assert.Throws<ArgumentException>(() => sut.Store("k", "u", secret));

        _interop.DidNotReceive().TryWrite(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<byte[]>());
    }

    // =====================================================================
    // Store — security mitigations
    // =====================================================================

    [Fact]
    public void Store_WhenInteropFails_ThrowsInvalidOperationException()
    {
        var sut = CreateSut();
        _interop.TryWrite(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<byte[]>()).Returns(false);

        Assert.Throws<InvalidOperationException>(() => sut.Store("k", "u", "s"));
    }

    [Fact]
    public void Store_ExceptionMessage_DoesNotContainTarget()
    {
        var sut = CreateSut();
        const string targetCanary = "TARGET-CANARY-xyz123";
        _interop.TryWrite(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<byte[]>()).Returns(false);

        var ex = Assert.Throws<InvalidOperationException>(() => sut.Store(targetCanary, "u", "s"));

        Assert.DoesNotContain(targetCanary, ex.Message);
        Assert.DoesNotContain(targetCanary, ex.ToString());
    }

    [Fact]
    public void Store_ExceptionMessage_DoesNotContainUsername()
    {
        var sut = CreateSut();
        const string userCanary = "USERNAME-CANARY-xyz";
        _interop.TryWrite(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<byte[]>()).Returns(false);

        var ex = Assert.Throws<InvalidOperationException>(() => sut.Store("t", userCanary, "s"));

        Assert.DoesNotContain(userCanary, ex.Message);
        Assert.DoesNotContain(userCanary, ex.ToString());
    }

    [Fact]
    public void Store_ExceptionMessage_DoesNotContainSecret()
    {
        var sut = CreateSut();
        const string secretCanary = "UNIQUE-SECRET-CANARY-12345";
        _interop.TryWrite(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<byte[]>()).Returns(false);

        var ex = Assert.Throws<InvalidOperationException>(() => sut.Store("t", "u", secretCanary));

        Assert.DoesNotContain(secretCanary, ex.Message);
        Assert.DoesNotContain(secretCanary, ex.ToString());
    }

    [Fact]
    public void StoreString_AfterWrite_TemporaryBlobIsZeroFilled()
    {
        var sut = CreateSut();
        byte[]? capturedRef = null;
        _interop.TryWrite(Arg.Any<string>(), Arg.Any<string>(), Arg.Do<byte[]>(b => capturedRef = b))
            .Returns(true);

        sut.Store("k", "u", "hi");

        Assert.NotNull(capturedRef);
        Assert.All(capturedRef!, b => Assert.Equal(0, b));
    }

    // =====================================================================
    // Read(string) — happy path
    // =====================================================================

    [Fact]
    public void ReadString_PrefixesTargetWithAppId()
    {
        var sut = CreateSut();
        _interop.TryRead($"{Prefix}mykey", out Arg.Any<CredentialRecord>()).Returns(ci =>
        {
            ci[1] = new CredentialRecord($"{Prefix}mykey", "alice", Encoding.Unicode.GetBytes("secret"));
            return true;
        });

        sut.Read("mykey");

        _interop.Received().TryRead($"{Prefix}mykey", out Arg.Any<CredentialRecord>());
    }

    [Fact]
    public void ReadString_WhenNotFound_ReturnsNull()
    {
        var sut = CreateSut();
        _interop.TryRead(Arg.Any<string>(), out Arg.Any<CredentialRecord>()).Returns(false);

        var result = sut.Read("missing");

        Assert.Null(result);
    }

    [Fact]
    public void ReadString_WhenFound_DecodesSecretFromUtf16Le()
    {
        var sut = CreateSut();
        _interop.TryRead(Arg.Any<string>(), out Arg.Any<CredentialRecord>()).Returns(ci =>
        {
            ci[1] = new CredentialRecord($"{Prefix}k", "alice", Encoding.Unicode.GetBytes("hello-world"));
            return true;
        });

        var result = sut.Read("k");

        Assert.NotNull(result);
        Assert.Equal("hello-world", result!.Secret);
    }

    [Fact]
    public void ReadString_ReturnsUnprefixedTargetName()
    {
        var sut = CreateSut();
        _interop.TryRead(Arg.Any<string>(), out Arg.Any<CredentialRecord>()).Returns(ci =>
        {
            ci[1] = new CredentialRecord($"{Prefix}mykey", "alice", Encoding.Unicode.GetBytes("x"));
            return true;
        });

        var result = sut.Read("mykey");

        Assert.Equal("mykey", result!.Target);
    }

    [Fact]
    public void ReadString_WhenFound_PreservesUsername()
    {
        var sut = CreateSut();
        _interop.TryRead(Arg.Any<string>(), out Arg.Any<CredentialRecord>()).Returns(ci =>
        {
            ci[1] = new CredentialRecord($"{Prefix}k", "dave", Encoding.Unicode.GetBytes("x"));
            return true;
        });

        var result = sut.Read("k");

        Assert.Equal("dave", result!.Username);
    }

    // =====================================================================
    // ReadBytes — happy path
    // =====================================================================

    [Fact]
    public void ReadBytes_PrefixesTargetWithAppId()
    {
        var sut = CreateSut();
        _interop.TryRead($"{Prefix}bkey", out Arg.Any<CredentialRecord>()).Returns(ci =>
        {
            ci[1] = new CredentialRecord($"{Prefix}bkey", "alice", new byte[] { 1, 2, 3 });
            return true;
        });

        sut.ReadBytes("bkey");

        _interop.Received().TryRead($"{Prefix}bkey", out Arg.Any<CredentialRecord>());
    }

    [Fact]
    public void ReadBytes_WhenNotFound_ReturnsNull()
    {
        var sut = CreateSut();
        _interop.TryRead(Arg.Any<string>(), out Arg.Any<CredentialRecord>()).Returns(false);

        var result = sut.ReadBytes("missing");

        Assert.Null(result);
    }

    [Fact]
    public void ReadBytes_WhenFound_ReturnsBufferContent()
    {
        var sut = CreateSut();
        var expected = new byte[] { 10, 20, 30, 40 };
        _interop.TryRead(Arg.Any<string>(), out Arg.Any<CredentialRecord>()).Returns(ci =>
        {
            ci[1] = new CredentialRecord($"{Prefix}k", "u", (byte[])expected.Clone());
            return true;
        });

        using var result = sut.ReadBytes("k");

        Assert.NotNull(result);
        Assert.Equal(expected, result!.Secret);
    }

    [Fact]
    public void ReadBytes_WhenFound_PreservesUsername()
    {
        var sut = CreateSut();
        _interop.TryRead(Arg.Any<string>(), out Arg.Any<CredentialRecord>()).Returns(ci =>
        {
            ci[1] = new CredentialRecord($"{Prefix}k", "eve", new byte[] { 1 });
            return true;
        });

        using var result = sut.ReadBytes("k");

        Assert.Equal("eve", result!.Username);
    }

    [Fact]
    public void ReadBytes_ReturnsUnprefixedTargetName()
    {
        var sut = CreateSut();
        _interop.TryRead(Arg.Any<string>(), out Arg.Any<CredentialRecord>()).Returns(ci =>
        {
            ci[1] = new CredentialRecord($"{Prefix}mykey", "u", new byte[] { 1 });
            return true;
        });

        using var result = sut.ReadBytes("mykey");

        Assert.Equal("mykey", result!.Target);
    }

    [Fact]
    public void ReadBytes_BufferIsOwnedByResult()
    {
        // After ReadBytes returns, the buffer is still populated — caller disposes when ready.
        var sut = CreateSut();
        _interop.TryRead(Arg.Any<string>(), out Arg.Any<CredentialRecord>()).Returns(ci =>
        {
            ci[1] = new CredentialRecord($"{Prefix}k", "u", new byte[] { 7, 8, 9 });
            return true;
        });

        var result = sut.ReadBytes("k");

        Assert.NotNull(result);
        // Not yet disposed → still has the content
        Assert.Equal(new byte[] { 7, 8, 9 }, result!.Secret);

        result.Dispose();
        // After dispose → zeroed
        Assert.Equal(new byte[] { 0, 0, 0 }, result.Secret);
    }

    // =====================================================================
    // Read/ReadBytes — validation
    // =====================================================================

    [Fact]
    public void ReadString_NullTarget_Throws()
    {
        var sut = CreateSut();
        Assert.Throws<ArgumentNullException>(() => sut.Read(null!));
    }

    [Fact]
    public void ReadBytes_NullTarget_Throws()
    {
        var sut = CreateSut();
        Assert.Throws<ArgumentNullException>(() => sut.ReadBytes(null!));
    }

    [Fact]
    public void Read_EmptyTarget_ThrowsArgumentException()
    {
        var sut = CreateSut();
        Assert.Throws<ArgumentException>(() => sut.Read(""));
        Assert.Throws<ArgumentException>(() => sut.ReadBytes(""));
    }

    [Fact]
    public void Read_TargetWithControlChar_ThrowsArgumentException()
    {
        var sut = CreateSut();
        Assert.Throws<ArgumentException>(() => sut.Read("bad\0target"));
        Assert.Throws<ArgumentException>(() => sut.ReadBytes("bad\0target"));
    }

    // =====================================================================
    // Delete
    // =====================================================================

    [Fact]
    public void Delete_PrefixesTargetWithAppId()
    {
        var sut = CreateSut();
        _interop.TryDelete($"{Prefix}foo").Returns(true);

        sut.Delete("foo");

        _interop.Received(1).TryDelete($"{Prefix}foo");
    }

    [Fact]
    public void Delete_NullTarget_Throws()
    {
        var sut = CreateSut();
        Assert.Throws<ArgumentNullException>(() => sut.Delete(null!));
    }

    [Fact]
    public void Delete_EmptyTarget_Throws()
    {
        var sut = CreateSut();
        Assert.Throws<ArgumentException>(() => sut.Delete(""));
    }

    [Fact]
    public void Delete_ReturnsTrueWhenInteropSucceeds()
    {
        var sut = CreateSut();
        _interop.TryDelete(Arg.Any<string>()).Returns(true);

        Assert.True(sut.Delete("t"));
    }

    [Fact]
    public void Delete_ReturnsFalseWhenInteropFails()
    {
        var sut = CreateSut();
        _interop.TryDelete(Arg.Any<string>()).Returns(false);

        Assert.False(sut.Delete("t"));
    }

    // =====================================================================
    // Enumerate (M5)
    // =====================================================================

    [Fact]
    public void Enumerate_PassesPrefixWildcardFilterToInterop()
    {
        var sut = CreateSut();
        _interop.EnumerateTargets(Arg.Any<string?>()).Returns(Array.Empty<string>());

        sut.Enumerate();

        _interop.Received(1).EnumerateTargets($"{Prefix}*");
    }

    [Fact]
    public void Enumerate_StripsPrefixFromResults()
    {
        var sut = CreateSut();
        _interop.EnumerateTargets(Arg.Any<string?>())
            .Returns(new[] { $"{Prefix}foo", $"{Prefix}bar" });

        var result = sut.Enumerate();

        Assert.Equal(new[] { "foo", "bar" }, result);
    }

    [Fact]
    public void Enumerate_FiltersOutEntriesWithoutAppPrefix()
    {
        // Defense in depth: even if interop returns a target outside our namespace,
        // the managed filter strips it out.
        var sut = CreateSut();
        _interop.EnumerateTargets(Arg.Any<string?>())
            .Returns(new[] { $"{Prefix}bar", "OtherApp:foo" });

        var result = sut.Enumerate();

        Assert.Equal(new[] { "bar" }, result);
    }

    [Fact]
    public void Enumerate_EmptyList_ReturnsEmpty()
    {
        var sut = CreateSut();
        _interop.EnumerateTargets(Arg.Any<string?>()).Returns(Array.Empty<string>());

        var result = sut.Enumerate();

        Assert.Empty(result);
    }

    // =====================================================================
    // StoredCredential record (M3)
    // =====================================================================

    [Fact]
    public void StoredCredential_ToString_MasksSecret()
    {
        var cred = new StoredCredential("target", "user", "SECRET-SHOULD-NOT-APPEAR");

        var str = cred.ToString();

        Assert.Contains("Secret = ***", str);
        Assert.DoesNotContain("SECRET-SHOULD-NOT-APPEAR", str);
    }

    [Fact]
    public void StoredCredential_ToString_ContainsTargetAndUsername()
    {
        var cred = new StoredCredential("my-target", "alice", "s");

        var str = cred.ToString();

        Assert.Contains("my-target", str);
        Assert.Contains("alice", str);
    }

    [Fact]
    public void StoredCredential_InterpolatedString_MasksSecret()
    {
        var cred = new StoredCredential("t", "u", "SECRET-CANARY");

        var str = $"{cred}";

        Assert.DoesNotContain("SECRET-CANARY", str);
    }

    [Fact]
    public void StoredCredential_Equality_StillWorks()
    {
        var a = new StoredCredential("t", "u", "s");
        var b = new StoredCredential("t", "u", "s");
        var c = new StoredCredential("t", "u", "different");

        Assert.Equal(a, b);
        Assert.NotEqual(a, c);
    }

    // =====================================================================
    // StoredBinaryCredential class
    // =====================================================================

    [Fact]
    public void StoredBinaryCredential_ToString_MasksSecret()
    {
        var cred = TestBinary("t", "u", new byte[] { 0xDE, 0xAD, 0xBE, 0xEF, 0xCA, 0xFE });

        var str = cred.ToString();

        // Positive assertions: must include target, username, and length summary
        Assert.Contains("Target = t", str);
        Assert.Contains("Username = u", str);
        Assert.Contains("<6 bytes>", str);
        // Negative assertions: any string representation of the byte content must NOT appear
        Assert.DoesNotContain("DEADBEEF", str, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("222", str);   // 0xDE decimal
        Assert.DoesNotContain("173", str);   // 0xAD decimal
    }

    [Fact]
    public void StoredBinaryCredential_Dispose_ZeroFillsSecret()
    {
        var cred = TestBinary("t", "u", new byte[] { 1, 2, 3, 4 });

        cred.Dispose();

        Assert.Equal(new byte[] { 0, 0, 0, 0 }, cred.Secret);
    }

    [Fact]
    public void StoredBinaryCredential_Dispose_IsIdempotent()
    {
        var cred = TestBinary("t", "u", new byte[] { 1, 2, 3 });

        cred.Dispose();
        var ex = Record.Exception((Action)(() => cred.Dispose()));

        Assert.Null(ex);
    }

    [Fact]
    public void StoredBinaryCredential_UsingBlock_ZeroFillsOnExit()
    {
        StoredBinaryCredential cred;
        {
            using var scoped = TestBinary("t", "u", new byte[] { 5, 6, 7 });
            cred = scoped;
            Assert.Equal(new byte[] { 5, 6, 7 }, cred.Secret);
        }
        Assert.Equal(new byte[] { 0, 0, 0 }, cred.Secret);
    }

    // =====================================================================
    // CredentialVault — no logging (M2)
    // =====================================================================

    [Fact]
    public void CredentialVault_NoILoggerDependency()
    {
        var ctor = typeof(CredentialVault).GetConstructors(
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance).Single();

        foreach (var param in ctor.GetParameters())
        {
            Assert.False(
                param.ParameterType.FullName?.Contains("ILogger") ?? false,
                $"Constructor parameter '{param.Name}' is an ILogger — this class must not log, see M2.");
        }
    }

    // =====================================================================
    // Helpers
    // =====================================================================

    /// <summary>
    /// Test-only factory to construct a StoredBinaryCredential without going through the
    /// (internal) ctor. Uses the public StoredBinaryCredential via reflection on the
    /// single internal ctor.
    /// </summary>
    private static StoredBinaryCredential TestBinary(string target, string username, byte[] secret)
    {
        var ctor = typeof(StoredBinaryCredential).GetConstructor(
            BindingFlags.Instance | BindingFlags.NonPublic,
            binder: null,
            types: new[] { typeof(string), typeof(string), typeof(byte[]) },
            modifiers: null)!;
        return (StoredBinaryCredential)ctor.Invoke(new object[] { target, username, secret });
    }
}
