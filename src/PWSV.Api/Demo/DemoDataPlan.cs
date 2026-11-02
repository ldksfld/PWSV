using PWSV.Domain.Enums;

namespace PWSV.Api.Demo;

public sealed record DemoDataPlan(
    IReadOnlyList<DemoCategory> Categories,
    IReadOnlyList<DemoAccount> Accounts,
    IReadOnlyList<DemoExchangeRate> ExchangeRates,
    IReadOnlyList<DemoTransaction> Transactions,
    IReadOnlyDictionary<string, decimal> ExpectedBalances);

public sealed record DemoCategory(string Name, CategoryKind Kind, string? ParentName = null, bool IsActive = true);

public sealed record DemoAccount(
    string Key,
    string Name,
    string AccountTypeCode,
    string CurrencyCode,
    string? AccountNumber,
    bool IsActive = true);

public sealed record DemoExchangeRate(string BaseCurrencyCode, string QuoteCurrencyCode, decimal Rate, DateOnly EffectiveDate);

public abstract record DemoTransaction(DateTime OccurredAt, string? Description);

public sealed record DemoIncome(
    string AccountKey,
    string CategoryName,
    decimal Amount,
    DateTime OccurredAt,
    string? Description,
    string? Counterparty) : DemoTransaction(OccurredAt, Description);

public sealed record DemoExpense(
    string AccountKey,
    string CategoryName,
    decimal Amount,
    DateTime OccurredAt,
    string? Description,
    string? Counterparty) : DemoTransaction(OccurredAt, Description);

public sealed record DemoTransfer(
    string SourceAccountKey,
    string DestinationAccountKey,
    decimal Amount,
    decimal? ExchangeRate,
    DateTime OccurredAt,
    string? Description) : DemoTransaction(OccurredAt, Description);
