using PWSV.Domain.Enums;

namespace PWSV.Domain.Exceptions;

public sealed class CategoryTypeMismatchException : DomainException
{
    public CategoryTypeMismatchException(CategoryKind categoryKind, TransactionKind transactionKind)
        : base($"Тип категорії '{categoryKind}' не відповідає типу транзакції '{transactionKind}'.")
    {
        CategoryKind = categoryKind;
        TransactionKind = transactionKind;
    }

    public CategoryKind CategoryKind { get; }
    public TransactionKind TransactionKind { get; }
}
