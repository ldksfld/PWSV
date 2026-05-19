using PWSV.Application.Common.Interfaces;

namespace PWSV.Infrastructure.Security;

public sealed class BCryptPasswordHasher : IPasswordHasher
{
    private const int WorkFactor = 12;

    public string Hash(string plaintext)
    {
        return BCrypt.Net.BCrypt.HashPassword(plaintext, WorkFactor);
    }

    public bool Verify(string plaintext, string hash)
    {
        if (string.IsNullOrEmpty(hash))
        {
            return false;
        }

        try
        {
            return BCrypt.Net.BCrypt.Verify(plaintext, hash);
        }
        catch (BCrypt.Net.SaltParseException)
        {
            return false;
        }
    }
}
