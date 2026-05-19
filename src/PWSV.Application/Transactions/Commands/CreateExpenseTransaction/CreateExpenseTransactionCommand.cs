using System.Data;
using MediatR;
using PWSV.Application.Common.Behaviors;
using PWSV.Application.Transactions.Dto;

namespace PWSV.Application.Transactions.Commands.CreateExpenseTransaction;

public sealed record CreateExpenseTransactionCommand(
    int AccountId,
    int CategoryId,
    decimal Amount,
    DateTime OccurredAt,
    string? Description,
    string? Counterparty) : IRequest<TransactionDto>, ITransactionalRequest
{
    public IsolationLevel IsolationLevel => IsolationLevel.ReadCommitted;
}
