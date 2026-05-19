using MediatR;
using PWSV.Application.AccountTypes.Dto;

namespace PWSV.Application.AccountTypes.Queries.GetAccountTypesList;

public sealed record GetAccountTypesListQuery : IRequest<IReadOnlyList<AccountTypeDto>>;
