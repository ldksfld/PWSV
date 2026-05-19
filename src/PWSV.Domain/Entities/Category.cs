using PWSV.Domain.Enums;
using PWSV.Domain.Exceptions;

namespace PWSV.Domain.Entities;

public sealed class Category
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public CategoryKind Kind { get; set; }
    public int? ParentCategoryId { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }

    public User? User { get; set; }
    public Category? ParentCategory { get; set; }
    public ICollection<Category> Children { get; set; } = [];
    public ICollection<Transaction> Transactions { get; set; } = [];

    public void EnsureMatches(TransactionKind transactionKind)
    {
        var matches = (Kind, transactionKind) switch
        {
            (CategoryKind.Income, TransactionKind.Income) => true,
            (CategoryKind.Expense, TransactionKind.Expense) => true,
            _ => false
        };

        if (!matches)
        {
            throw new CategoryTypeMismatchException(Kind, transactionKind);
        }
    }
}
