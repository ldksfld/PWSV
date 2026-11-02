using PWSV.Domain.Enums;

namespace PWSV.Domain.Exceptions;

public sealed class CategoryTypeMismatchException : DomainException
{
    public CategoryTypeMismatchException(CategoryKind categoryKind, TransactionKind transactionKind)
        : base($"Тип категорії «{Describe(categoryKind)}» не відповідає типу транзакції «{Describe(transactionKind)}».")
    {
        CategoryKind = categoryKind;
        TransactionKind = transactionKind;
    }

    public CategoryKind CategoryKind { get; }
    public TransactionKind TransactionKind { get; }

    private static string Describe(CategoryKind kind) => kind switch
    {
        CategoryKind.Income => "дохід",
        CategoryKind.Expense => "витрата",
        _ => kind.ToString()
    };

    private static string Describe(TransactionKind kind) => kind switch
    {
        TransactionKind.Income => "дохід",
        TransactionKind.Expense => "витрата",
        TransactionKind.Transfer => "переказ",
        _ => kind.ToString()
    };
}
