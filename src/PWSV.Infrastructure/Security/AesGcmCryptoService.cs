using System.Security.Cryptography;
using System.Text;
using PWSV.Application.Common.Interfaces;
using PWSV.Domain.ValueObjects;

namespace PWSV.Infrastructure.Security;

public sealed class AesGcmCryptoService : ICryptoService
{
    private const int NonceSize = 12;
    private const int TagSize = 16;

    private byte[]? _key;

    public bool IsReady => _key is { Length: > 0 };

    public void InitializeKey(string masterPassword, byte[] salt, int iterations, int keyLength)
    {
        ArgumentException.ThrowIfNullOrEmpty(masterPassword);
        ArgumentNullException.ThrowIfNull(salt);

        if (salt.Length == 0)
        {
            throw new ArgumentException("Salt must not be empty.", nameof(salt));
        }

        _key = Rfc2898DeriveBytes.Pbkdf2(masterPassword, salt, iterations, HashAlgorithmName.SHA256, keyLength);
    }

    public void Reset()
    {
        if (_key is not null)
        {
            CryptographicOperations.ZeroMemory(_key);
            _key = null;
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

        var plaintext = new byte[payload.Cipher.Length];

        using var aes = new AesGcm(key, TagSize);
        aes.Decrypt(payload.Nonce, payload.Cipher, payload.Tag, plaintext);

        return Encoding.UTF8.GetString(plaintext);
    }

    private byte[] RequireKey()
    {
        if (_key is null || _key.Length == 0)
        {
            throw new InvalidOperationException(
                "Сервіс шифрування не ініціалізовано. Спочатку виконайте вхід з мастер-паролем.");
        }

        return _key;
    }
}
