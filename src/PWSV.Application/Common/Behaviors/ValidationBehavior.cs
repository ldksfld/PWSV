using FluentValidation;
using MediatR;
using PWSV.Application.Common.Exceptions;
using ValidationException = PWSV.Application.Common.Exceptions.ValidationException;

namespace PWSV.Application.Common.Behaviors;

public sealed class ValidationBehavior<TRequest, TResponse>(IEnumerable<IValidator<TRequest>> validators)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        if (!validators.Any())
        {
            return await next().ConfigureAwait(false);
        }

        var context = new ValidationContext<TRequest>(request);
        var results = await Task.WhenAll(validators.Select(v => v.ValidateAsync(context, cancellationToken))).ConfigureAwait(false);
        var failures = results.SelectMany(r => r.Errors).Where(f => f is not null).ToArray();

        if (failures.Length > 0)
        {
            throw new ValidationException(failures);
        }

        return await next().ConfigureAwait(false);
    }
}
