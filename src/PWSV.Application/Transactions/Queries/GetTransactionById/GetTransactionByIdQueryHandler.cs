using Mapster;
using MediatR;
using Microsoft.EntityFrameworkCore;
using PWSV.Application.Common.Exceptions;
using PWSV.Application.Common.Interfaces;
using PWSV.Application.Transactions.Dto;
using PWSV.Domain.ValueObjects;

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

        var dto = transaction.Adapt<TransactionDto>();
        var description = TryDecrypt(
            transaction.DescriptionCipher,
            transaction.DescriptionNonce,
            transaction.DescriptionTag);
        return dto with { Description = description };
    }

    private string? TryDecrypt(byte[]? cipher, byte[]? nonce, byte[]? tag)
    {
        if (cipher is null || nonce is null || tag is null || cipher.Length == 0 || !crypto.IsReady)
        {
            return null;
        }

        return crypto.Decrypt(new EncryptedString(cipher, nonce, tag));
    }
}
