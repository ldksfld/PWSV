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

        var categories = await db.Categories
            .Where(c => c.UserId == userId)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var descendantIds = CategoryHierarchy.GetDescendantIds(categories, category.Id);

        if (command.ParentCategoryId is { } parentId)
        {
            if (parentId == category.Id)
            {
                throw new ConflictException("Категорія не може бути власним батьком.");
            }

            var parent = categories.SingleOrDefault(c => c.Id == parentId);
            if (parent is null)
            {
                var existsForOtherUser = await db.Categories
                    .AnyAsync(c => c.Id == parentId, cancellationToken)
                    .ConfigureAwait(false);

                if (existsForOtherUser)
                {
                    throw new ForbiddenException("Батьківська категорія належить іншому користувачеві.");
                }

                throw new NotFoundException("Category", parentId);
            }

            if (parent.Kind != category.Kind)
            {
                throw new ConflictException(
                    "Тип батьківської категорії повинен збігатися з типом категорії.");
            }

            if (descendantIds.Contains(parentId))
            {
                throw new ConflictException(
                    "Не можна зробити батьком категорію, яка є власним нащадком.");
            }

            if (command.IsActive && !parent.IsActive)
            {
                throw new ConflictException(
                    "Батьківську категорію деактивовано. Спочатку активуйте її.");
            }
        }

        var name = command.Name.Trim();
        var duplicateExists = categories.Any(c =>
            c.Id != category.Id
            && c.ParentCategoryId == command.ParentCategoryId
            && string.Equals(c.Name, name, StringComparison.OrdinalIgnoreCase));

        if (duplicateExists)
        {
            throw new ConflictException(
                "Категорія з такою назвою у даного батька вже існує.");
        }

        if (category.IsActive && !command.IsActive)
        {
            foreach (var descendant in categories.Where(c => descendantIds.Contains(c.Id)))
            {
                descendant.IsActive = false;
            }
        }

        category.Name = name;
        category.ParentCategoryId = command.ParentCategoryId;
        category.IsActive = command.IsActive;
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return category.Adapt<CategoryDto>();
    }
}
