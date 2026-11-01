using System.Text.RegularExpressions;
using Serilog.Context;

namespace PWSV.Api.Middleware;

public sealed partial class CorrelationIdMiddleware(RequestDelegate next)
{
    private const string HeaderName = "X-Correlation-Id";

    public async Task InvokeAsync(HttpContext context)
    {
        var incoming = context.Request.Headers[HeaderName].ToString();
        var correlationId = CorrelationIdPattern().IsMatch(incoming)
            ? incoming
            : Guid.NewGuid().ToString();

        context.TraceIdentifier = correlationId;
        context.Response.OnStarting(() =>
        {
            context.Response.Headers[HeaderName] = correlationId;
            return Task.CompletedTask;
        });

        using (LogContext.PushProperty("CorrelationId", correlationId))
        {
            await next(context).ConfigureAwait(false);
        }
    }

    [GeneratedRegex("^[A-Za-z0-9._-]{1,64}$")]
    private static partial Regex CorrelationIdPattern();
}
