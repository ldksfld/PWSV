using MediatR;
using PWSV.Application.ExchangeRates.Dto;

namespace PWSV.Application.ExchangeRates.Queries.GetExchangeRates;

public sealed record GetExchangeRatesQuery(
    int? BaseCurrencyId,
    int? QuoteCurrencyId,
    DateOnly? From,
    DateOnly? To) : IRequest<IReadOnlyList<ExchangeRateDto>>;
