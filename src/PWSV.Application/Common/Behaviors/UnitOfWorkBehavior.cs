using System.Data;
using MediatR;
using PWSV.Application.Common.Interfaces;

namespace PWSV.Application.Common.Behaviors;

public interface ITransactionalRequest
{
    IsolationLevel IsolationLevel => IsolationLevel.ReadCommitted;
}

public sealed class UnitOfWorkBehavior<TRequest, TResponse>(IApplicationDbContext db)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        if (request is not ITransactionalRequest transactional)
        {
            return await next().ConfigureAwait(false);
        }

        return await db
            .ExecuteInTransactionAsync(_ => next(), transactional.IsolationLevel, cancellationToken)
            .ConfigureAwait(false);
    }
}
