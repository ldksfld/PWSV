using FluentValidation;
using PWSV.Application.Common.Interfaces;
using PWSV.Application.Common.Validation;

namespace PWSV.Application.ExchangeRates.Commands.CreateExchangeRate;

public sealed class CreateExchangeRateCommandValidator : AbstractValidator<CreateExchangeRateCommand>
{
    public CreateExchangeRateCommandValidator(IDateTimeProvider clock)
    {
        RuleFor(x => x.BaseCurrencyId).GreaterThan(0);
        RuleFor(x => x.QuoteCurrencyId)
            .GreaterThan(0)
            .NotEqual(x => x.BaseCurrencyId)
                .WithMessage("Базова та котирувана валюти повинні відрізнятися.");
        RuleFor(x => x.Rate).ExchangeRateValue();
        RuleFor(x => x.EffectiveDate).RateEffectiveDate(clock);
    }
}
