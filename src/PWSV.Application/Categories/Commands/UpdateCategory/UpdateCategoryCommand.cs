using MediatR;
using PWSV.Application.Categories.Dto;

namespace PWSV.Application.Categories.Commands.UpdateCategory;

public sealed record UpdateCategoryCommand(int Id, string Name, int? ParentCategoryId, bool IsActive = true) : IRequest<CategoryDto>;
