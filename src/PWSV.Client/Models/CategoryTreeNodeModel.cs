namespace PWSV.Client.Models;

public sealed record CategoryTreeNodeModel
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Kind { get; init; } = string.Empty;
    public bool IsActive { get; init; }
    public IReadOnlyList<CategoryTreeNodeModel> Children { get; init; } = [];
}
