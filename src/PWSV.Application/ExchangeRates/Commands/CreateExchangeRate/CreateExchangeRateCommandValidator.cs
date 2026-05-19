using FluentValidation;

namespace PWSV.Application.ExchangeRates.Commands.CreateExchangeRate;

public sealed class CreateExchangeRateCommandValidator : AbstractValidator<CreateExchangeRateCommand>
{
    public CreateExchangeRateCommandValidator()
    {
        RuleFor(x => x.BaseCurrencyId).GreaterThan(0);
        RuleFor(x => x.QuoteCurrencyId)
            .GreaterThan(0)
            .NotEqual(x => x.BaseCurrencyId)
                .WithMessage("Базова та котирувана валюти повинні відрізнятись.");
        RuleFor(x => x.Rate).GreaterThan(0m);
    }
}
