namespace PWSV.Domain.Entities;

public sealed class Currency
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Symbol { get; set; } = string.Empty;
    public byte DecimalPlaces { get; set; } = 2;
}
