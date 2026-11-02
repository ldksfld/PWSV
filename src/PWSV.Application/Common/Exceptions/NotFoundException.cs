namespace PWSV.Application.Common.Exceptions;

public sealed class NotFoundException : BusinessException
{
    private static readonly Dictionary<string, string> EntityTitles = new(StringComparer.Ordinal)
    {
        ["Account"] = "Рахунок",
        ["AccountType"] = "Тип рахунку",
        ["Category"] = "Категорію",
        ["Currency"] = "Валюту",
        ["ExchangeRate"] = "Курс валют",
        ["Transaction"] = "Транзакцію",
        ["User"] = "Користувача"
    };

    public NotFoundException(string entityName, object key)
        : base($"{EntityTitles.GetValueOrDefault(entityName, entityName)} з ідентифікатором {key} не знайдено.")
    {
        EntityName = entityName;
        Key = key;
    }

    public string EntityName { get; }
    public object Key { get; }
}
