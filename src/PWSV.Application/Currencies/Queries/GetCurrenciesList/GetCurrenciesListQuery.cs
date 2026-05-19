using MediatR;
using PWSV.Application.Currencies.Dto;

namespace PWSV.Application.Currencies.Queries.GetCurrenciesList;

public sealed record GetCurrenciesListQuery : IRequest<IReadOnlyList<CurrencyDto>>;
