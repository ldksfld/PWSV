using System.Security.Cryptography;
using FluentAssertions;
using PWSV.Application.Common.Exceptions;
using PWSV.Domain.ValueObjects;
using PWSV.Infrastructure.Security;

namespace PWSV.Application.Tests.Security;

public sealed class AesGcmCryptoServiceTests
{
    private const int Iterations = 1_000;
    private const int KeyLength = 32;
    private static readonly byte[] Salt = RandomNumberGenerator.GetBytes(32);

    [Fact]
    public void EncryptThenDecrypt_ReturnsOriginalText()
    {
        var crypto = CreateInitialized("MasterKey#1");

        var encrypted = crypto.Encrypt("UA21 3052 9900 0002 6007 2335 6600 1");

        encrypted.Nonce.Should().HaveCount(12);
        encrypted.Tag.Should().HaveCount(16);
        crypto.Decrypt(encrypted).Should().Be("UA21 3052 9900 0002 6007 2335 6600 1");
    }

    [Fact]
    public void Decrypt_WithTamperedTag_ThrowsCryptographicException()
    {
        var crypto = CreateInitialized("MasterKey#1");
        var encrypted = crypto.Encrypt("Опис операції");
        var tamperedTag = encrypted.Tag.ToArray();
        tamperedTag[0] ^= 0xFF;

        var act = () => crypto.Decrypt(encrypted with { Tag = tamperedTag });

        act.Should().Throw<CryptographicException>();
    }

    [Fact]
    public void Decrypt_WithInvalidNonceLength_ThrowsCryptographicException()
    {
        var crypto = CreateInitialized("MasterKey#1");
        var encrypted = crypto.Encrypt("Опис");

        var act = () => crypto.Decrypt(encrypted with { Nonce = [1, 2, 3] });

        act.Should().Throw<CryptographicException>();
    }

    [Fact]
    public void Encrypt_WithoutKey_ThrowsUnauthorized()
    {
        var crypto = new AesGcmCryptoService();

        var act = () => crypto.Encrypt("text");

        act.Should().Throw<UnauthorizedException>();
    }

    [Fact]
    public void TryInitializeKey_WithWrongPassword_ReturnsFalseAndKeepsCurrentKey()
    {
        var crypto = CreateInitialized("MasterKey#1");
        var sample = crypto.Encrypt("Контрольне значення");

        var accepted = crypto.TryInitializeKey("WrongMaster#1", Salt, Iterations, KeyLength, [sample]);

        accepted.Should().BeFalse();
        crypto.Decrypt(sample).Should().Be("Контрольне значення");
    }

    [Fact]
    public void TryInitializeKey_WithCorrectPassword_AcceptsWhenAnySampleDecrypts()
    {
        var crypto = CreateInitialized("MasterKey#1");
        var valid = crypto.Encrypt("Контрольне значення");
        var corrupted = valid with { Tag = new byte[16] };
        var restarted = new AesGcmCryptoService();

        var accepted = restarted.TryInitializeKey("MasterKey#1", Salt, Iterations, KeyLength, [corrupted, valid]);

        accepted.Should().BeTrue();
        restarted.Decrypt(valid).Should().Be("Контрольне значення");
    }

    [Fact]
    public void TryInitializeKey_WithoutSamples_AcceptsAnyPassword()
    {
        var crypto = new AesGcmCryptoService();

        var accepted = crypto.TryInitializeKey("AnyMaster#1", Salt, Iterations, KeyLength, Array.Empty<EncryptedString>());

        accepted.Should().BeTrue();
    }

    [Fact]
    public void Reset_RemovesKey()
    {
        var crypto = CreateInitialized("MasterKey#1");

        crypto.Reset();

        var act = () => crypto.Encrypt("text");
        act.Should().Throw<UnauthorizedException>();
    }

    private static AesGcmCryptoService CreateInitialized(string masterPassword)
    {
        var crypto = new AesGcmCryptoService();
        crypto.InitializeKey(masterPassword, Salt, Iterations, KeyLength);
        return crypto;
    }
}
