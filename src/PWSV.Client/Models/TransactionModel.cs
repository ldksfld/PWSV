namespace PWSV.Client.Models;

public sealed record TransactionModel
{
    public long Id { get; init; }
    public int AccountId { get; init; }
    public string AccountName { get; init; } = string.Empty;
    public int? CategoryId { get; init; }
    public string? CategoryName { get; init; }
    public string Kind { get; init; } = string.Empty;
    public decimal Amount { get; init; }
    public DateTime OccurredAt { get; init; }
    public string? Description { get; init; }
    public string? Counterparty { get; init; }
    public long? LinkedTransactionId { get; init; }
    public DateTime CreatedAt { get; init; }
}
