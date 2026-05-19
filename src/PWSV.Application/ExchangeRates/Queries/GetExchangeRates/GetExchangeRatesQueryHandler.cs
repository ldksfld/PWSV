using Mapster;
using MediatR;
using Microsoft.EntityFrameworkCore;
using PWSV.Application.Common.Interfaces;
using PWSV.Application.ExchangeRates.Dto;

namespace PWSV.Application.ExchangeRates.Queries.GetExchangeRates;

public sealed class GetExchangeRatesQueryHandler(IApplicationDbContext db)
    : IRequestHandler<GetExchangeRatesQuery, IReadOnlyList<ExchangeRateDto>>
{
    public async Task<IReadOnlyList<ExchangeRateDto>> Handle(GetExchangeRatesQuery query, CancellationToken cancellationToken)
    {
        var queryable = db.ExchangeRates
            .AsNoTracking()
            .Include(r => r.BaseCurrency)
            .Include(r => r.QuoteCurrency)
            .AsQueryable();

        if (query.BaseCurrencyId.HasValue)
        {
            queryable = queryable.Where(r => r.BaseCurrencyId == query.BaseCurrencyId.Value);
        }

        if (query.QuoteCurrencyId.HasValue)
        {
            queryable = queryable.Where(r => r.QuoteCurrencyId == query.QuoteCurrencyId.Value);
        }

        if (query.From.HasValue)
        {
            queryable = queryable.Where(r => r.EffectiveDate >= query.From.Value);
        }

        if (query.To.HasValue)
        {
            queryable = queryable.Where(r => r.EffectiveDate <= query.To.Value);
        }

        var rates = await queryable
            .OrderByDescending(r => r.EffectiveDate)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return rates.Adapt<List<ExchangeRateDto>>();
    }
}
