using MediatR;
using Microsoft.EntityFrameworkCore;
using PWSV.Application.Categories.Dto;
using PWSV.Application.Common.Exceptions;
using PWSV.Application.Common.Interfaces;
using PWSV.Domain.Entities;

namespace PWSV.Application.Categories.Queries.GetCategoryTree;

public sealed class GetCategoryTreeQueryHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    : IRequestHandler<GetCategoryTreeQuery, IReadOnlyList<CategoryTreeNodeDto>>
{
    public async Task<IReadOnlyList<CategoryTreeNodeDto>> Handle(GetCategoryTreeQuery query, CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId
            ?? throw new ForbiddenException("Необхідна авторизація.");

        var queryable = db.Categories
            .AsNoTracking()
            .Where(c => c.UserId == userId);

        if (!query.IncludeInactive)
        {
            queryable = queryable.Where(c => c.IsActive);
        }

        var categories = await queryable.ToListAsync(cancellationToken).ConfigureAwait(false);
        var grouped = categories.ToLookup(c => c.ParentCategoryId);

        return categories
            .Where(c => c.ParentCategoryId is null)
            .OrderBy(c => c.Kind)
            .ThenBy(c => c.Name)
            .Select(c => BuildNode(c, grouped))
            .ToList();
    }

    private static CategoryTreeNodeDto BuildNode(Category category, ILookup<int?, Category> lookup)
    {
        var children = lookup[category.Id]
            .OrderBy(c => c.Name)
            .Select(child => BuildNode(child, lookup))
            .ToList();

        return new CategoryTreeNodeDto
        {
            Id = category.Id,
            Name = category.Name,
            Kind = category.Kind,
            IsActive = category.IsActive,
            Children = children
        };
    }
}
