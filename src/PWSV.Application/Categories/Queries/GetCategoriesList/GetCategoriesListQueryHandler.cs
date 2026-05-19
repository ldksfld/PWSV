using Mapster;
using MediatR;
using Microsoft.EntityFrameworkCore;
using PWSV.Application.Categories.Dto;
using PWSV.Application.Common.Exceptions;
using PWSV.Application.Common.Interfaces;

namespace PWSV.Application.Categories.Queries.GetCategoriesList;

public sealed class GetCategoriesListQueryHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    : IRequestHandler<GetCategoriesListQuery, IReadOnlyList<CategoryDto>>
{
    public async Task<IReadOnlyList<CategoryDto>> Handle(GetCategoriesListQuery query, CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId
            ?? throw new ForbiddenException("Необхідна авторизація.");

        var queryable = db.Categories
            .AsNoTracking()
            .Where(c => c.UserId == userId);

        if (query.Kind.HasValue)
        {
            queryable = queryable.Where(c => c.Kind == query.Kind.Value);
        }

        if (!query.IncludeInactive)
        {
            queryable = queryable.Where(c => c.IsActive);
        }

        var categories = await queryable
            .OrderBy(c => c.ParentCategoryId)
            .ThenBy(c => c.Name)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return categories.Adapt<List<CategoryDto>>();
    }
}
