using System.Data;
using MediatR;
using PWSV.Application.Common.Behaviors;
using PWSV.Application.Transactions.Dto;

namespace PWSV.Application.Transactions.Commands.CreateTransferTransaction;

public sealed record CreateTransferTransactionCommand(
    int SourceAccountId,
    int DestinationAccountId,
    decimal Amount,
    decimal? ExchangeRate,
    DateTime OccurredAt,
    string? Description) : IRequest<TransferResult>, ITransactionalRequest
{
    public IsolationLevel IsolationLevel => IsolationLevel.RepeatableRead;
}

public sealed record TransferResult(TransactionDto Source, TransactionDto Destination);
