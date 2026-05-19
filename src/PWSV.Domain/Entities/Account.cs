using PWSV.Domain.Common;

namespace PWSV.Domain.Entities;

public sealed class Account : BaseEntity<int>
{
    public int UserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int AccountTypeId { get; set; }
    public int CurrencyId { get; set; }
    public decimal Balance { get; set; }

    public byte[]? AccountNumberCipher { get; set; }
    public byte[]? AccountNumberNonce { get; set; }
    public byte[]? AccountNumberTag { get; set; }

    public bool IsActive { get; set; } = true;
    public byte[] RowVersion { get; set; } = [];

    public User User { get; set; } = null!;
    public AccountType AccountType { get; set; } = null!;
    public Currency Currency { get; set; } = null!;
    public ICollection<Transaction> Transactions { get; set; } = [];
}
