namespace PWSV.Client.Models;

public sealed record CategoryModel
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Kind { get; init; } = string.Empty;
    public int? ParentCategoryId { get; init; }
    public bool IsActive { get; init; }
    public DateTime CreatedAt { get; init; }
}
