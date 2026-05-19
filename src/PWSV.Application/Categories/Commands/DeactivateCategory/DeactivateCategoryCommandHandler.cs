using MediatR;
using Microsoft.EntityFrameworkCore;
using PWSV.Application.Common.Exceptions;
using PWSV.Application.Common.Interfaces;

namespace PWSV.Application.Categories.Commands.DeactivateCategory;

public sealed class DeactivateCategoryCommandHandler(
    IApplicationDbContext db,
    ICurrentUserService currentUser) : IRequestHandler<DeactivateCategoryCommand, Unit>
{
    public async Task<Unit> Handle(DeactivateCategoryCommand command, CancellationToken cancellationToken)
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

        category.IsActive = false;
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Unit.Value;
    }
}
