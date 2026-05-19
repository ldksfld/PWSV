using PWSV.Domain.ValueObjects;

namespace PWSV.Application.Common.Interfaces;

public interface ICryptoService
{
    bool IsReady { get; }
    void InitializeKey(string masterPassword, byte[] salt, int iterations, int keyLength);
    void Reset();
    EncryptedString Encrypt(string plaintext);
    string Decrypt(EncryptedString payload);
}
