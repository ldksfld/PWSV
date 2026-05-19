namespace PWSV.Domain.Entities;

public sealed class ExchangeRate
{
    public long Id { get; set; }
    public int BaseCurrencyId { get; set; }
    public int QuoteCurrencyId { get; set; }
    public decimal Rate { get; set; }
    public DateOnly EffectiveDate { get; set; }
    public DateTime CreatedAt { get; set; }

    public Currency? BaseCurrency { get; set; }
    public Currency? QuoteCurrency { get; set; }
}
