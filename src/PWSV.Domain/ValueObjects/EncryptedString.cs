namespace PWSV.Domain.ValueObjects;

public sealed record EncryptedString(byte[] Cipher, byte[] Nonce, byte[] Tag)
{
    public bool IsEmpty => Cipher.Length == 0;

    public static EncryptedString Empty { get; } = new([], [], []);
}
