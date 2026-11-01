using PWSV.Domain.Entities;

namespace PWSV.Application.Categories;

public static class CategoryHierarchy
{
    public static HashSet<int> GetDescendantIds(IEnumerable<Category> categories, int rootId)
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
}
