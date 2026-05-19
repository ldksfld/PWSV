using Mapster;
using MediatR;
using Microsoft.EntityFrameworkCore;
using PWSV.Application.Categories.Dto;
using PWSV.Application.Common.Exceptions;
using PWSV.Application.Common.Interfaces;
using PWSV.Domain.Entities;

namespace PWSV.Application.Categories.Commands.CreateCategory;

public sealed class CreateCategoryCommandHandler(
    IApplicationDbContext db,
    ICurrentUserService currentUser,
    IDateTimeProvider clock) : IRequestHandler<CreateCategoryCommand, CategoryDto>
{
    public async Task<CategoryDto> Handle(CreateCategoryCommand command, CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId
            ?? throw new ForbiddenException("Необхідна авторизація.");

        if (command.ParentCategoryId is { } parentId)
        {
            var parent = await db.Categories
                .SingleOrDefaultAsync(c => c.Id == parentId, cancellationToken)
                .ConfigureAwait(false)
                ?? throw new NotFoundException("Category", parentId);

            if (parent.UserId != userId)
            {
                throw new ForbiddenException(
                    "Батьківська категорія належить іншому користувачеві.");
            }

            if (parent.Kind != command.Kind)
            {
                throw new ConflictException(
                    "Тип підкатегорії повинен співпадати з типом батьківської категорії.");
            }
        }

        var name = command.Name.Trim();
        var duplicateExists = await db.Categories
            .AnyAsync(
                c => c.UserId == userId
                    && c.ParentCategoryId == command.ParentCategoryId
                    && c.Name == name,
                cancellationToken)
            .ConfigureAwait(false);

        if (duplicateExists)
        {
            throw new ConflictException(
                "Категорія з такою назвою у даного батька вже існує.");
        }

        var category = new Category
        {
            UserId = userId,
            Name = name,
            Kind = command.Kind,
            ParentCategoryId = command.ParentCategoryId,
            IsActive = true,
            CreatedAt = clock.UtcNow
        };

        db.Categories.Add(category);
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return category.Adapt<CategoryDto>();
    }
}
