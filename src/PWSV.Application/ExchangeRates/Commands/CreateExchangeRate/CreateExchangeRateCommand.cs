using MediatR;
using PWSV.Application.ExchangeRates.Dto;

namespace PWSV.Application.ExchangeRates.Commands.CreateExchangeRate;

public sealed record CreateExchangeRateCommand(
    int BaseCurrencyId,
    int QuoteCurrencyId,
    decimal Rate,
    DateOnly EffectiveDate) : IRequest<ExchangeRateDto>;
