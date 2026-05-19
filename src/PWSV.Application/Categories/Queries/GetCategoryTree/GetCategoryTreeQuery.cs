using MediatR;
using PWSV.Application.Categories.Dto;

namespace PWSV.Application.Categories.Queries.GetCategoryTree;

public sealed record GetCategoryTreeQuery(bool IncludeInactive) : IRequest<IReadOnlyList<CategoryTreeNodeDto>>;
