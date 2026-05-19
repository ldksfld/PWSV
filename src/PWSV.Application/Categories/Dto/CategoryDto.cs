using PWSV.Domain.Enums;

namespace PWSV.Application.Categories.Dto;

public sealed record CategoryDto
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public CategoryKind Kind { get; init; }
    public int? ParentCategoryId { get; init; }
    public bool IsActive { get; init; }
    public DateTime CreatedAt { get; init; }
}
