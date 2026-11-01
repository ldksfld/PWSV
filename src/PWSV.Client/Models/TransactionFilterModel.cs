namespace PWSV.Client.Models;

public sealed record TransactionFilterModel(
    int? AccountId,
    int? CategoryId,
    string? Kind,
    DateTime? From,
    DateTime? To)
{
    public static TransactionFilterModel Empty { get; } = new(null, null, null, null, null);
}
