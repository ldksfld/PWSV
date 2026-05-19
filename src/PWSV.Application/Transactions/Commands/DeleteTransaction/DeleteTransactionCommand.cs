using System.Data;
using MediatR;
using PWSV.Application.Common.Behaviors;

namespace PWSV.Application.Transactions.Commands.DeleteTransaction;

public sealed record DeleteTransactionCommand(long Id) : IRequest<Unit>, ITransactionalRequest
{
    public IsolationLevel IsolationLevel => IsolationLevel.RepeatableRead;
}
