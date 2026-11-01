using System.Globalization;
using FluentValidation;

namespace PWSV.Application.Common.Validation;

public static class ValidationConfig
{
    private static readonly Dictionary<string, string> DisplayNames = new(StringComparer.Ordinal)
    {
        ["Id"] = "Ідентифікатор",
        ["Username"] = "Ім'я користувача",
        ["Password"] = "Пароль",
        ["OldPassword"] = "Старий пароль",
        ["NewPassword"] = "Новий пароль",
        ["MasterPassword"] = "Майстер-пароль",
        ["Name"] = "Назва",
        ["AccountTypeId"] = "Тип рахунку",
        ["CurrencyId"] = "Валюта",
        ["AccountNumber"] = "Номер рахунку",
        ["InitialBalance"] = "Початковий баланс",
        ["Kind"] = "Тип",
        ["ParentCategoryId"] = "Батьківська категорія",
        ["Code"] = "Код",
        ["Symbol"] = "Символ",
        ["DecimalPlaces"] = "Кількість знаків після коми",
        ["BaseCurrencyId"] = "Базова валюта",
        ["QuoteCurrencyId"] = "Котирувана валюта",
        ["Rate"] = "Курс",
        ["EffectiveDate"] = "Дата курсу",
        ["AccountId"] = "Рахунок",
        ["CategoryId"] = "Категорія",
        ["Amount"] = "Сума",
        ["OccurredAt"] = "Дата операції",
        ["Description"] = "Опис",
        ["Counterparty"] = "Контрагент",
        ["SourceAccountId"] = "Рахунок-джерело",
        ["DestinationAccountId"] = "Рахунок-отримувач",
        ["ExchangeRate"] = "Курс конверсії",
        ["From"] = "Дата від",
        ["To"] = "Дата до"
    };

    public static void Register()
    {
        ValidatorOptions.Global.LanguageManager.Culture = CultureInfo.GetCultureInfo("uk-UA");
        ValidatorOptions.Global.DisplayNameResolver = (_, member, _) =>
            member is not null && DisplayNames.TryGetValue(member.Name, out var displayName)
                ? displayName
                : member?.Name;
    }
}
