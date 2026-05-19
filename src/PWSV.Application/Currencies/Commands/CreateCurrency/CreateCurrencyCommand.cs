using MediatR;
using PWSV.Application.Currencies.Dto;

namespace PWSV.Application.Currencies.Commands.CreateCurrency;

public sealed record CreateCurrencyCommand(string Code, string Name, string Symbol, byte DecimalPlaces) : IRequest<CurrencyDto>;
