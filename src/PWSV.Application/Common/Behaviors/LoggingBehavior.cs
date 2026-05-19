using System.Diagnostics;
using MediatR;
using Microsoft.Extensions.Logging;
using PWSV.Application.Common.Interfaces;

namespace PWSV.Application.Common.Behaviors;

public sealed class LoggingBehavior<TRequest, TResponse>(
    ILogger<LoggingBehavior<TRequest, TResponse>> logger,
    ICurrentUserService currentUser) : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;
        var userId = currentUser.UserId;

        logger.LogInformation("Handling {RequestName} for user {UserId}", requestName, userId);

        var stopwatch = Stopwatch.StartNew();
        var response = await next().ConfigureAwait(false);
        stopwatch.Stop();

        logger.LogInformation("Handled {RequestName} in {ElapsedMs} ms", requestName, stopwatch.ElapsedMilliseconds);

        return response;
    }
}
