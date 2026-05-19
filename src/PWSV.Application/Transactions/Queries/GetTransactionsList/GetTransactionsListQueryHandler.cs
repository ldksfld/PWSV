using Mapster;
using MediatR;
using Microsoft.EntityFrameworkCore;
using PWSV.Application.Common.Exceptions;
using PWSV.Application.Common.Interfaces;
using PWSV.Application.Common.Models;
using PWSV.Application.Transactions.Dto;
using PWSV.Domain.ValueObjects;

namespace PWSV.Application.Transactions.Queries.GetTransactionsList;

public sealed class GetTransactionsListQueryHandler(
    IApplicationDbContext db,
    ICurrentUserService currentUser,
    ICryptoService crypto) : IRequestHandler<GetTransactionsListQuery, PagedResult<TransactionDto>>
{
    private const int DefaultPageSize = 50;
    private const int MaxPageSize = 200;

    public async Task<PagedResult<TransactionDto>> Handle(GetTransactionsListQuery query, CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId
            ?? throw new ForbiddenException("Необхідна авторизація.");

        var page = query.Page <= 0 ? 1 : query.Page;
        var pageSize = query.PageSize switch
        {
            <= 0 => DefaultPageSize,
            > MaxPageSize => MaxPageSize,
            _ => query.PageSize
        };

        var queryable = db.Transactions
            .AsNoTracking()
            .Include(t => t.Account)
            .Include(t => t.Category)
            .Where(t => t.Account.UserId == userId);

        if (query.AccountId.HasValue)
        {
            queryable = queryable.Where(t => t.AccountId == query.AccountId.Value);
        }

        if (query.CategoryId.HasValue)
        {
            queryable = queryable.Where(t => t.CategoryId == query.CategoryId.Value);
        }

        if (query.Kind.HasValue)
        {
            queryable = queryable.Where(t => t.Kind == query.Kind.Value);
        }

        if (query.From.HasValue)
        {
            queryable = queryable.Where(t => t.OccurredAt >= query.From.Value);
        }

        if (query.To.HasValue)
        {
            queryable = queryable.Where(t => t.OccurredAt <= query.To.Value);
        }

        var totalCount = await queryable.CountAsync(cancellationToken).ConfigureAwait(false);

        var ordered = (query.SortBy, query.SortDirection) switch
        {
            (TransactionSortField.Amount, SortDirection.Ascending) => queryable.OrderBy(t => t.Amount).ThenBy(t => t.Id),
            (TransactionSortField.Amount, _) => queryable.OrderByDescending(t => t.Amount).ThenByDescending(t => t.Id),
            (_, SortDirection.Ascending) => queryable.OrderBy(t => t.OccurredAt).ThenBy(t => t.Id),
            _ => queryable.OrderByDescending(t => t.OccurredAt).ThenByDescending(t => t.Id)
        };

        var transactions = await ordered
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var items = transactions
            .Select(t =>
            {
                var dto = t.Adapt<TransactionDto>();
                return dto with { Description = TryDecrypt(t.DescriptionCipher, t.DescriptionNonce, t.DescriptionTag) };
            })
            .ToList();

        return new PagedResult<TransactionDto>(items, page, pageSize, totalCount);
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
