using MediatR;

namespace PWSV.Application.Categories.Commands.DeactivateCategory;

public sealed record DeactivateCategoryCommand(int Id) : IRequest<Unit>;
