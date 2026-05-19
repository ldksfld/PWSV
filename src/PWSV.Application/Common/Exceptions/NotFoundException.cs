namespace PWSV.Application.Common.Exceptions;

public sealed class NotFoundException : BusinessException
{
    public NotFoundException(string entityName, object key)
        : base($"Сутність '{entityName}' з ключем '{key}' не знайдена.")
    {
        EntityName = entityName;
        Key = key;
    }

    public string EntityName { get; }
    public object Key { get; }
}
