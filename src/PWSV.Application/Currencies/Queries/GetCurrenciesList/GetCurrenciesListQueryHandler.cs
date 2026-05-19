using Mapster;
using MediatR;
using Microsoft.EntityFrameworkCore;
using PWSV.Application.Common.Interfaces;
using PWSV.Application.Currencies.Dto;

namespace PWSV.Application.Currencies.Queries.GetCurrenciesList;

public sealed class GetCurrenciesListQueryHandler(IApplicationDbContext db)
    : IRequestHandler<GetCurrenciesListQuery, IReadOnlyList<CurrencyDto>>
{
    public async Task<IReadOnlyList<CurrencyDto>> Handle(GetCurrenciesListQuery query, CancellationToken cancellationToken)
    {
        var currencies = await db.Currencies
            .AsNoTracking()
            .OrderBy(c => c.Code)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return currencies.Adapt<List<CurrencyDto>>();
    }
}
