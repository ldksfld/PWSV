namespace PWSV.Domain.Entities;

public sealed class KeyDerivationConfig
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public byte[] Salt { get; set; } = [];
    public int Iterations { get; set; } = 100_000;
    public int KeyLength { get; set; } = 32;
    public byte[]? VerifierCipher { get; set; }
    public byte[]? VerifierNonce { get; set; }
    public byte[]? VerifierTag { get; set; }

    public User? User { get; set; }
}
