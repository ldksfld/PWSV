using PWSV.Client.Models;

namespace PWSV.Client.Services;

public static class CategoryPaths
{
    private const string Separator = " / ";

    public static IReadOnlyList<OptionModel> BuildOptions(
        IReadOnlyCollection<CategoryModel> categories,
        Func<CategoryModel, bool> include)
    {
        var byId = categories.ToDictionary(c => c.Id);
        return categories
            .Where(include)
            .Select(c => new OptionModel(c.Id, BuildDisplayName(c, byId)))
            .OrderBy(option => option.DisplayName, StringComparer.CurrentCultureIgnoreCase)
            .ToList();
    }

    public static HashSet<int> GetDescendantIds(IReadOnlyCollection<CategoryModel> categories, int rootId)
    {
        var children = categories
            .Where(c => c.ParentCategoryId is not null)
            .ToLookup(c => c.ParentCategoryId!.Value, c => c.Id);

        var descendants = new HashSet<int>();
        var pending = new Queue<int>(children[rootId]);
        while (pending.Count > 0)
        {
            var id = pending.Dequeue();
            if (id == rootId || !descendants.Add(id))
            {
                continue;
            }

            foreach (var childId in children[id])
            {
                pending.Enqueue(childId);
            }
        }

        return descendants;
    }

    private static string BuildDisplayName(CategoryModel category, Dictionary<int, CategoryModel> byId)
    {
        var names = new List<string> { category.Name };
        var visited = new HashSet<int> { category.Id };
        var parentId = category.ParentCategoryId;
        while (parentId is { } id && byId.TryGetValue(id, out var parent) && visited.Add(id))
        {
            names.Add(parent.Name);
            parentId = parent.ParentCategoryId;
        }

        names.Reverse();
        var path = string.Join(Separator, names);
        return category.IsActive ? path : $"{path} (неактивна)";
    }
}
