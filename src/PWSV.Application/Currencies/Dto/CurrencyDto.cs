namespace PWSV.Application.Currencies.Dto;

public sealed record CurrencyDto(int Id, string Code, string Name, string Symbol, byte DecimalPlaces);
