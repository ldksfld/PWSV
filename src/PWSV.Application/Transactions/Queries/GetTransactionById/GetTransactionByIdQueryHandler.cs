using Mapster;
using MediatR;
using Microsoft.EntityFrameworkCore;
using PWSV.Application.Common.Exceptions;
using PWSV.Application.Common.Interfaces;
using PWSV.Application.Transactions.Dto;

namespace PWSV.Application.Transactions.Queries.GetTransactionById;

public sealed class GetTransactionByIdQueryHandler(
    IApplicationDbContext db,
    ICurrentUserService currentUser,
    ICryptoService crypto) : IRequestHandler<GetTransactionByIdQuery, TransactionDto>
{
    public async Task<TransactionDto> Handle(GetTransactionByIdQuery query, CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId
            ?? throw new ForbiddenException("Необхідна авторизація.");

        var transaction = await db.Transactions
            .AsNoTracking()
            .Include(t => t.Account)
            .Include(t => t.Category)
            .SingleOrDefaultAsync(t => t.Id == query.Id, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new NotFoundException("Transaction", query.Id);

        if (transaction.Account.UserId != userId)
        {
            throw new ForbiddenException("Транзакція належить іншому користувачеві.");
        }

        return transaction.Adapt<TransactionDto>() with
        {
            Description = crypto.DecryptOrNull(
                transaction.DescriptionCipher,
                transaction.DescriptionNonce,
                transaction.DescriptionTag)
        };
    }
}
