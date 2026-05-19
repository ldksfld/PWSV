namespace PWSV.Application.Common.Interfaces;

public interface IPasswordHasher
{
    string Hash(string plaintext);
    bool Verify(string plaintext, string hash);
}
