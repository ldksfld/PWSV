using PWSV.Domain.ValueObjects;

namespace PWSV.Application.Common.Interfaces;

public interface ICryptoService
{
    void InitializeKey(string masterPassword, byte[] salt, int iterations, int keyLength);
    bool TryInitializeKey(string masterPassword, byte[] salt, int iterations, int keyLength, IReadOnlyCollection<EncryptedString> verificationSamples);
    void Reset();
    EncryptedString Encrypt(string plaintext);
    string Decrypt(EncryptedString payload);
}
