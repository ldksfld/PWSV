using System.Security.Cryptography;
using System.Text;
using PWSV.Application.Common.Exceptions;
using PWSV.Application.Common.Interfaces;
using PWSV.Domain.ValueObjects;

namespace PWSV.Infrastructure.Security;

public sealed class AesGcmCryptoService : ICryptoService
{
    private const int NonceSize = 12;
    private const int TagSize = 16;

    private byte[]? _key;

    public void InitializeKey(string masterPassword, byte[] salt, int iterations, int keyLength)
    {
        TryInitializeKey(masterPassword, salt, iterations, keyLength, []);
    }

    public bool TryInitializeKey(
        string masterPassword,
        byte[] salt,
        int iterations,
        int keyLength,
        IReadOnlyCollection<EncryptedString> verificationSamples)
    {
        ArgumentException.ThrowIfNullOrEmpty(masterPassword);
        ArgumentNullException.ThrowIfNull(salt);
        ArgumentNullException.ThrowIfNull(verificationSamples);

        if (salt.Length == 0)
        {
            throw new ArgumentException("Salt must not be empty.", nameof(salt));
        }

        var candidate = Rfc2898DeriveBytes.Pbkdf2(masterPassword, salt, iterations, HashAlgorithmName.SHA256, keyLength);

        if (verificationSamples.Count > 0 && !verificationSamples.Any(sample => CanDecrypt(candidate, sample)))
        {
            CryptographicOperations.ZeroMemory(candidate);
            return false;
        }

        Interlocked.Exchange(ref _key, candidate);
        return true;
    }

    public void Reset()
    {
        var key = Interlocked.Exchange(ref _key, null);
        if (key is not null)
        {
            CryptographicOperations.ZeroMemory(key);
        }
    }

    public EncryptedString Encrypt(string plaintext)
    {
        var key = RequireKey();
        ArgumentNullException.ThrowIfNull(plaintext);

        var plaintextBytes = Encoding.UTF8.GetBytes(plaintext);
        var cipher = new byte[plaintextBytes.Length];
        var nonce = RandomNumberGenerator.GetBytes(NonceSize);
        var tag = new byte[TagSize];

        using var aes = new AesGcm(key, TagSize);
        aes.Encrypt(nonce, plaintextBytes, cipher, tag);

        return new EncryptedString(cipher, nonce, tag);
    }

    public string Decrypt(EncryptedString payload)
    {
        var key = RequireKey();
        ArgumentNullException.ThrowIfNull(payload);

        if (payload.Nonce.Length != NonceSize || payload.Tag.Length != TagSize)
        {
            throw new CryptographicException("Encrypted payload has an invalid nonce or tag length.");
        }

        var plaintext = new byte[payload.Cipher.Length];

        using var aes = new AesGcm(key, TagSize);
        aes.Decrypt(payload.Nonce, payload.Cipher, payload.Tag, plaintext);

        return Encoding.UTF8.GetString(plaintext);
    }

    private static bool CanDecrypt(byte[] key, EncryptedString sample)
    {
        if (sample.Nonce.Length != NonceSize || sample.Tag.Length != TagSize)
        {
            return false;
        }

        try
        {
            using var aes = new AesGcm(key, TagSize);
            aes.Decrypt(sample.Nonce, sample.Cipher, sample.Tag, new byte[sample.Cipher.Length]);
            return true;
        }
        catch (CryptographicException)
        {
            return false;
        }
    }

    private byte[] RequireKey()
    {
        return Volatile.Read(ref _key)
            ?? throw new UnauthorizedException(
                "Сеанс шифрування завершено. Увійдіть повторно з майстер-паролем.");
    }
}
