using Mapster;
using MediatR;
using Microsoft.EntityFrameworkCore;
using PWSV.Application.Categories.Dto;
using PWSV.Application.Common.Exceptions;
using PWSV.Application.Common.Interfaces;

namespace PWSV.Application.Categories.Commands.UpdateCategory;

public sealed class UpdateCategoryCommandHandler(
    IApplicationDbContext db,
    ICurrentUserService currentUser) : IRequestHandler<UpdateCategoryCommand, CategoryDto>
{
    public async Task<CategoryDto> Handle(UpdateCategoryCommand command, CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId
            ?? throw new ForbiddenException("Необхідна авторизація.");

        var category = await db.Categories
            .SingleOrDefaultAsync(c => c.Id == command.Id, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new NotFoundException("Category", command.Id);

        if (category.UserId != userId)
        {
            throw new ForbiddenException("Категорія належить іншому користувачеві.");
        }

        if (command.ParentCategoryId is { } parentId && parentId != category.ParentCategoryId)
        {
            if (parentId == category.Id)
            {
                throw new ConflictException("Категорія не може бути власним батьком.");
            }

            var parent = await db.Categories
                .SingleOrDefaultAsync(c => c.Id == parentId, cancellationToken)
                .ConfigureAwait(false)
                ?? throw new NotFoundException("Category", parentId);

            if (parent.UserId != userId)
            {
                throw new ForbiddenException(
                    "Батьківська категорія належить іншому користувачеві.");
            }

            if (parent.Kind != category.Kind)
            {
                throw new ConflictException(
                    "Тип батьківської категорії повинен співпадати з типом категорії.");
            }

            var isDescendant = await IsDescendantAsync(parentId, category.Id, cancellationToken)
                .ConfigureAwait(false);
            if (isDescendant)
            {
                throw new ConflictException(
                    "Не можна зробити батьком категорію, яка є власним нащадком.");
            }
        }

        category.Name = command.Name.Trim();
        category.ParentCategoryId = command.ParentCategoryId;
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return category.Adapt<CategoryDto>();
    }

    private async Task<bool> IsDescendantAsync(int candidateParentId, int rootCategoryId, CancellationToken cancellationToken)
    {
        var currentId = (int?)candidateParentId;
        var safety = 0;
        while (currentId is not null && safety++ < 64)
        {
            if (currentId.Value == rootCategoryId)
            {
                return true;
            }

            currentId = await db.Categories
                .Where(c => c.Id == currentId.Value)
                .Select(c => c.ParentCategoryId)
                .SingleOrDefaultAsync(cancellationToken)
                .ConfigureAwait(false);
        }
        return false;
    }
}
