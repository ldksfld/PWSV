namespace PWSV.Client.Models;

public sealed record AccountModel
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public int AccountTypeId { get; init; }
    public string AccountTypeCode { get; init; } = string.Empty;
    public string AccountTypeName { get; init; } = string.Empty;
    public int CurrencyId { get; init; }
    public string CurrencyCode { get; init; } = string.Empty;
    public string CurrencySymbol { get; init; } = string.Empty;
    public decimal Balance { get; init; }
    public bool IsActive { get; init; }
    public string? AccountNumber { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
}
