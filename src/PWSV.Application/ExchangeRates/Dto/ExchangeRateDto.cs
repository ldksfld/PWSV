namespace PWSV.Application.ExchangeRates.Dto;

public sealed record ExchangeRateDto
{
    public long Id { get; init; }
    public int BaseCurrencyId { get; init; }
    public string BaseCurrencyCode { get; init; } = string.Empty;
    public int QuoteCurrencyId { get; init; }
    public string QuoteCurrencyCode { get; init; } = string.Empty;
    public decimal Rate { get; init; }
    public DateOnly EffectiveDate { get; init; }
    public DateTime CreatedAt { get; init; }
}
