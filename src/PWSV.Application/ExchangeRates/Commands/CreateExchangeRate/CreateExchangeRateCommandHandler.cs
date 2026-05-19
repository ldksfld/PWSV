using Mapster;
using MediatR;
using Microsoft.EntityFrameworkCore;
using PWSV.Application.Common.Exceptions;
using PWSV.Application.Common.Interfaces;
using PWSV.Application.ExchangeRates.Dto;
using PWSV.Domain.Entities;

namespace PWSV.Application.ExchangeRates.Commands.CreateExchangeRate;

public sealed class CreateExchangeRateCommandHandler(
    IApplicationDbContext db,
    IDateTimeProvider clock) : IRequestHandler<CreateExchangeRateCommand, ExchangeRateDto>
{
    public async Task<ExchangeRateDto> Handle(CreateExchangeRateCommand command, CancellationToken cancellationToken)
    {
        var baseExists = await db.Currencies.AnyAsync(c => c.Id == command.BaseCurrencyId, cancellationToken).ConfigureAwait(false);
        if (!baseExists)
        {
            throw new NotFoundException("Currency", command.BaseCurrencyId);
        }

        var quoteExists = await db.Currencies.AnyAsync(c => c.Id == command.QuoteCurrencyId, cancellationToken).ConfigureAwait(false);
        if (!quoteExists)
        {
            throw new NotFoundException("Currency", command.QuoteCurrencyId);
        }

        var duplicate = await db.ExchangeRates.AnyAsync(r =>
            r.BaseCurrencyId == command.BaseCurrencyId
            && r.QuoteCurrencyId == command.QuoteCurrencyId
            && r.EffectiveDate == command.EffectiveDate, cancellationToken).ConfigureAwait(false);

        if (duplicate)
        {
            throw new ConflictException("Курс для цієї пари валют на цю дату вже існує.");
        }

        var rate = new ExchangeRate
        {
            BaseCurrencyId = command.BaseCurrencyId,
            QuoteCurrencyId = command.QuoteCurrencyId,
            Rate = command.Rate,
            EffectiveDate = command.EffectiveDate,
            CreatedAt = clock.UtcNow
        };

        db.ExchangeRates.Add(rate);
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        var saved = await db.ExchangeRates
            .AsNoTracking()
            .Include(r => r.BaseCurrency)
            .Include(r => r.QuoteCurrency)
            .SingleAsync(r => r.Id == rate.Id, cancellationToken)
            .ConfigureAwait(false);

        return saved.Adapt<ExchangeRateDto>();
    }
}
