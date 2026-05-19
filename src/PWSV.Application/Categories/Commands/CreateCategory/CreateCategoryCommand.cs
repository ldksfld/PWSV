using MediatR;
using PWSV.Application.Categories.Dto;
using PWSV.Domain.Enums;

namespace PWSV.Application.Categories.Commands.CreateCategory;

public sealed record CreateCategoryCommand(string Name, CategoryKind Kind, int? ParentCategoryId) : IRequest<CategoryDto>;
