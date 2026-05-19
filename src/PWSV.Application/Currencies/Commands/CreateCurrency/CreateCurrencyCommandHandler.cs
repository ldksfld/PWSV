using Mapster;
using MediatR;
using Microsoft.EntityFrameworkCore;
using PWSV.Application.Common.Exceptions;
using PWSV.Application.Common.Interfaces;
using PWSV.Application.Currencies.Dto;
using PWSV.Domain.Entities;

namespace PWSV.Application.Currencies.Commands.CreateCurrency;

public sealed class CreateCurrencyCommandHandler(IApplicationDbContext db)
    : IRequestHandler<CreateCurrencyCommand, CurrencyDto>
{
    public async Task<CurrencyDto> Handle(CreateCurrencyCommand command, CancellationToken cancellationToken)
    {
        var code = command.Code.Trim().ToUpperInvariant();

        var exists = await db.Currencies.AnyAsync(c => c.Code == code, cancellationToken).ConfigureAwait(false);
        if (exists)
        {
            throw new ConflictException($"Валюта з кодом '{code}' вже існує.");
        }

        var currency = new Currency
        {
            Code = code,
            Name = command.Name.Trim(),
            Symbol = command.Symbol.Trim(),
            DecimalPlaces = command.DecimalPlaces
        };

        db.Currencies.Add(currency);
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return currency.Adapt<CurrencyDto>();
    }
}
