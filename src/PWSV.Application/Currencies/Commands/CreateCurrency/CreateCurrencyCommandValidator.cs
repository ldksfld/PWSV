using FluentValidation;

namespace PWSV.Application.Currencies.Commands.CreateCurrency;

public sealed class CreateCurrencyCommandValidator : AbstractValidator<CreateCurrencyCommand>
{
    public CreateCurrencyCommandValidator()
    {
        RuleFor(x => x.Code).NotEmpty().Length(3, 10);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(64);
        RuleFor(x => x.Symbol).NotEmpty().MaximumLength(8);
        RuleFor(x => x.DecimalPlaces).InclusiveBetween((byte)0, (byte)8);
    }
}
