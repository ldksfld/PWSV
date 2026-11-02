using PWSV.Application.Common.Interfaces;
using PWSV.Domain.Entities;
using PWSV.Domain.ValueObjects;

namespace PWSV.Application.Auth;

public static class MasterKeyVerifier
{
    private const string Marker = "PWSV";

    public static void Store(KeyDerivationConfig config, ICryptoService crypto)
    {
        var encrypted = crypto.Encrypt(Marker);
        config.VerifierCipher = encrypted.Cipher;
        config.VerifierNonce = encrypted.Nonce;
        config.VerifierTag = encrypted.Tag;
    }

    public static EncryptedString? Read(KeyDerivationConfig config) =>
        config is { VerifierCipher: { } cipher, VerifierNonce: { } nonce, VerifierTag: { } tag }
            ? new EncryptedString(cipher, nonce, tag)
            : null;
}
