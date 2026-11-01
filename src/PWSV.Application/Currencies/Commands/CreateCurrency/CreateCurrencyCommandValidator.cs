using System.Text.RegularExpressions;
using FluentValidation;

namespace PWSV.Application.Currencies.Commands.CreateCurrency;

public sealed partial class CreateCurrencyCommandValidator : AbstractValidator<CreateCurrencyCommand>
{
    public CreateCurrencyCommandValidator()
    {
        RuleFor(x => x.Code)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .Must(code => CurrencyCodePattern().IsMatch(code.Trim()))
                .WithMessage("Код валюти має містити від 3 до 10 латинських літер або цифр.");
        RuleFor(x => x.Name).NotEmpty().MaximumLength(64);
        RuleFor(x => x.Symbol).NotEmpty().MaximumLength(8);
        RuleFor(x => x.DecimalPlaces).InclusiveBetween((byte)0, (byte)8);
    }

    [GeneratedRegex("^[A-Za-z0-9]{3,10}$")]
    private static partial Regex CurrencyCodePattern();
}
