using PWSV.Domain.Enums;

namespace PWSV.Application.Categories.Dto;

public sealed record CategoryTreeNodeDto
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public CategoryKind Kind { get; init; }
    public bool IsActive { get; init; }
    public IReadOnlyList<CategoryTreeNodeDto> Children { get; init; } = [];
}
