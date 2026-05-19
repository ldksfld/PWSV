using MediatR;
using PWSV.Application.Categories.Dto;
using PWSV.Domain.Enums;

namespace PWSV.Application.Categories.Queries.GetCategoriesList;

public sealed record GetCategoriesListQuery(CategoryKind? Kind, bool IncludeInactive) : IRequest<IReadOnlyList<CategoryDto>>;
