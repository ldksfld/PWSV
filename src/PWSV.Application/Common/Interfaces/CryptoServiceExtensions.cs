using PWSV.Domain.ValueObjects;

namespace PWSV.Application.Common.Interfaces;

public static class CryptoServiceExtensions
{
    public static string? DecryptOrNull(this ICryptoService crypto, byte[]? cipher, byte[]? nonce, byte[]? tag)
    {
        if (cipher is null || nonce is null || tag is null || cipher.Length == 0)
        {
            return null;
        }

        return crypto.Decrypt(new EncryptedString(cipher, nonce, tag));
    }
}
