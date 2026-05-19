using MediatR;
using Microsoft.EntityFrameworkCore;
using PWSV.Application.AccountTypes.Dto;
using PWSV.Application.Common.Interfaces;

namespace PWSV.Application.AccountTypes.Queries.GetAccountTypesList;

public sealed class GetAccountTypesListQueryHandler(IApplicationDbContext db)
    : IRequestHandler<GetAccountTypesListQuery, IReadOnlyList<AccountTypeDto>>
{
    public async Task<IReadOnlyList<AccountTypeDto>> Handle(GetAccountTypesListQuery query, CancellationToken cancellationToken)
    {
        return await db.AccountTypes
            .AsNoTracking()
            .OrderBy(t => t.Id)
            .Select(t => new AccountTypeDto(t.Id, t.Code, t.DisplayName))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }
}
