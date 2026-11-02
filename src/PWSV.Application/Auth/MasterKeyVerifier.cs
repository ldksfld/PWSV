using Microsoft.EntityFrameworkCore;
using PWSV.Application.Common.Interfaces;
using PWSV.Domain.Entities;
using PWSV.Domain.ValueObjects;

namespace PWSV.Application.Auth;

public static class MasterKeyVerifier
{
    private const string Marker = "PWSV";
    private const int LegacySamplesPerSource = 3;

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

    public static async Task<IReadOnlyCollection<EncryptedString>> GetVerificationSamplesAsync(
        IApplicationDbContext db,
        KeyDerivationConfig config,
        int userId,
        CancellationToken cancellationToken)
    {
        if (Read(config) is { } verifier)
        {
            return [verifier];
        }

        var accountNumbers = await db.Accounts
            .AsNoTracking()
            .Where(a => a.UserId == userId
                && a.AccountNumberCipher != null
                && a.AccountNumberNonce != null
                && a.AccountNumberTag != null)
            .OrderBy(a => a.Id)
            .Take(LegacySamplesPerSource)
            .Select(a => new EncryptedString(a.AccountNumberCipher!, a.AccountNumberNonce!, a.AccountNumberTag!))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var descriptions = await db.Transactions
            .AsNoTracking()
            .Where(t => t.Account.UserId == userId
                && t.DescriptionCipher != null
                && t.DescriptionNonce != null
                && t.DescriptionTag != null)
            .OrderBy(t => t.Id)
            .Take(LegacySamplesPerSource)
            .Select(t => new EncryptedString(t.DescriptionCipher!, t.DescriptionNonce!, t.DescriptionTag!))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return [.. accountNumbers, .. descriptions];
    }
}
