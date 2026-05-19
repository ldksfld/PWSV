namespace PWSV.Domain.Entities;

public sealed class KeyDerivationConfig
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public byte[] Salt { get; set; } = [];
    public int Iterations { get; set; } = 100_000;
    public int KeyLength { get; set; } = 32;

    public User? User { get; set; }
}
