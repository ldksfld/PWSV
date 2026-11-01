using FluentValidation;
using PWSV.Application.Common.Interfaces;

namespace PWSV.Application.Common.Validation;

public static class ValidationRules
{
    public const decimal MaxAmount = 999_999_999_999.99m;

    private static readonly DateTime MinOperationDate = new(1900, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    private static readonly DateOnly MinRateDate = new(1900, 1, 1);

    public const int MaxSecretLength = 256;

    public static IRuleBuilderOptions<T, string> Username<T>(this IRuleBuilderInitial<T, string> rule) =>
        rule.Cascade(CascadeMode.Stop)
            .NotEmpty()
            .Must(username => username.Trim().Length is >= 3 and <= 64)
                .WithMessage("Ім'я користувача має містити від 3 до 64 символів.");

    public static IRuleBuilderOptions<T, string> StrongPassword<T>(this IRuleBuilderInitial<T, string> rule) =>
        rule.Cascade(CascadeMode.Stop)
            .NotEmpty()
            .Length(8, 128)
            .Matches(@"\p{Ll}")
                .WithMessage("Пароль повинен містити щонайменше одну літеру нижнього регістру.")
            .Matches(@"\p{Lu}")
                .WithMessage("Пароль повинен містити щонайменше одну літеру верхнього регістру.")
            .Matches("[0-9]")
                .WithMessage("Пароль повинен містити щонайменше одну цифру.");

    public static IRuleBuilderOptions<T, decimal> PositiveAmount<T>(this IRuleBuilderInitial<T, decimal> rule) =>
        rule.Cascade(CascadeMode.Stop)
            .GreaterThan(0m)
                .WithMessage("Сума повинна бути більшою за нуль.")
            .AmountFormat();

    public static IRuleBuilderOptions<T, decimal> NonNegativeAmount<T>(this IRuleBuilderInitial<T, decimal> rule) =>
        rule.Cascade(CascadeMode.Stop)
            .GreaterThanOrEqualTo(0m)
                .WithMessage("Сума не може бути від'ємною.")
            .AmountFormat();

    public static IRuleBuilderOptions<T, decimal> ExchangeRateValue<T>(this IRuleBuilderInitial<T, decimal> rule) =>
        rule.Cascade(CascadeMode.Stop)
            .GreaterThan(0m)
                .WithMessage("Курс повинен бути більшим за нуль.")
            .PrecisionScale(18, 8, true)
                .WithMessage("Курс може містити не більше 8 знаків після коми та не перевищувати 9 999 999 999,99999999.");

    public static IRuleBuilderOptions<T, DateTime> OperationDate<T>(this IRuleBuilderInitial<T, DateTime> rule, IDateTimeProvider clock) =>
        rule.Cascade(CascadeMode.Stop)
            .Must(date => ToUtc(date) >= MinOperationDate)
                .WithMessage("Дата операції обов'язкова і не може бути раніше 01.01.1900.")
            .Must(date => ToUtc(date) <= clock.UtcNow.AddMinutes(1))
                .WithMessage("Дата операції не може бути в майбутньому.");

    public static IRuleBuilderOptions<T, DateOnly> RateEffectiveDate<T>(this IRuleBuilderInitial<T, DateOnly> rule, IDateTimeProvider clock) =>
        rule.Cascade(CascadeMode.Stop)
            .Must(date => date >= MinRateDate)
                .WithMessage("Дата курсу обов'язкова і не може бути раніше 01.01.1900.")
            .Must(date => date <= DateOnly.FromDateTime(clock.UtcNow).AddDays(1))
                .WithMessage("Дата курсу не може бути в майбутньому.");

    private static IRuleBuilderOptions<T, decimal> AmountFormat<T>(this IRuleBuilder<T, decimal> rule) =>
        rule.PrecisionScale(14, 2, true)
            .WithMessage("Сума може містити не більше 2 знаків після коми та не перевищувати 999 999 999 999,99.");

    private static DateTime ToUtc(DateTime value) => value.Kind switch
    {
        DateTimeKind.Local => value.ToUniversalTime(),
        DateTimeKind.Unspecified => DateTime.SpecifyKind(value, DateTimeKind.Utc),
        _ => value
    };
}
