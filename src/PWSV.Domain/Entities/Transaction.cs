using PWSV.Domain.Enums;
using PWSV.Domain.Exceptions;

namespace PWSV.Domain.Entities;

public sealed class Transaction
{
    public long Id { get; set; }
    public int AccountId { get; set; }
    public int? CategoryId { get; set; }
    public TransactionKind Kind { get; set; }
    public decimal Amount { get; set; }
    public DateTime OccurredAt { get; set; }

    public byte[]? DescriptionCipher { get; set; }
    public byte[]? DescriptionNonce { get; set; }
    public byte[]? DescriptionTag { get; set; }

    public string? Counterparty { get; set; }
    public long? LinkedTransactionId { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public Account Account { get; set; } = null!;
    public Category? Category { get; set; }
    public Transaction? LinkedTransaction { get; set; }

    public void EnsurePositiveAmount()
    {
        if (Amount <= 0m)
        {
            throw new InvalidAmountException(Amount);
        }
    }
}
