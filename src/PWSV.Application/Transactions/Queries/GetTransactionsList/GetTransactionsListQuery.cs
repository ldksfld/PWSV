using MediatR;
using PWSV.Application.Common.Models;
using PWSV.Application.Transactions.Dto;
using PWSV.Domain.Enums;

namespace PWSV.Application.Transactions.Queries.GetTransactionsList;

public enum TransactionSortField
{
    OccurredAt = 0,
    Amount = 1
}

public enum SortDirection
{
    Descending = 0,
    Ascending = 1
}

public sealed record GetTransactionsListQuery(
    int? AccountId,
    int? CategoryId,
    TransactionKind? Kind,
    DateTime? From,
    DateTime? To,
    int Page,
    int PageSize,
    TransactionSortField SortBy = TransactionSortField.OccurredAt,
    SortDirection SortDirection = SortDirection.Descending) : IRequest<PagedResult<TransactionDto>>;
