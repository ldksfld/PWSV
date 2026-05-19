using PWSV.Domain.Enums;

namespace PWSV.Application.Transactions.Dto;

public sealed record TransactionDto
{
    public long Id { get; init; }
    public int AccountId { get; init; }
    public string AccountName { get; init; } = string.Empty;
    public int? CategoryId { get; init; }
    public string? CategoryName { get; init; }
    public TransactionKind Kind { get; init; }
    public decimal Amount { get; init; }
    public DateTime OccurredAt { get; init; }
    public string? Description { get; init; }
    public string? Counterparty { get; init; }
    public long? LinkedTransactionId { get; init; }
    public DateTime CreatedAt { get; init; }
}
