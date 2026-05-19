using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using PWSV.Application.Common.Exceptions;
using PWSV.Domain.Exceptions;
using ValidationException = PWSV.Application.Common.Exceptions.ValidationException;

namespace PWSV.Api.Middleware;

public sealed class ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            await HandleAsync(context, ex).ConfigureAwait(false);
        }
    }

    private async Task HandleAsync(HttpContext context, Exception exception)
    {
        var problem = MapToProblem(exception);

        logger.LogError(exception, "Unhandled exception {ExceptionType} mapped to status {StatusCode}",
            exception.GetType().Name, problem.Status);

        context.Response.ContentType = "application/problem+json";
        context.Response.StatusCode = problem.Status ?? StatusCodes.Status500InternalServerError;
        await context.Response.WriteAsync(JsonSerializer.Serialize(problem)).ConfigureAwait(false);
    }

    private static ProblemDetails MapToProblem(Exception exception) => exception switch
    {
        ValidationException validation => new ValidationProblemDetails(validation.Errors.ToDictionary(kv => kv.Key, kv => kv.Value))
        {
            Status = StatusCodes.Status400BadRequest,
            Title = "Помилки валідації."
        },
        NotFoundException notFound => new ProblemDetails
        {
            Status = StatusCodes.Status404NotFound,
            Title = "Ресурс не знайдено.",
            Detail = notFound.Message
        },
        UnauthorizedException unauthorized => new ProblemDetails
        {
            Status = StatusCodes.Status401Unauthorized,
            Title = "Неавторизовано.",
            Detail = unauthorized.Message
        },
        ForbiddenException forbidden => new ProblemDetails
        {
            Status = StatusCodes.Status403Forbidden,
            Title = "Доступ заборонено.",
            Detail = forbidden.Message
        },
        ConflictException conflict => new ProblemDetails
        {
            Status = StatusCodes.Status409Conflict,
            Title = "Конфлікт стану.",
            Detail = conflict.Message
        },
        DomainException domain => new ProblemDetails
        {
            Status = StatusCodes.Status422UnprocessableEntity,
            Title = "Порушення бізнес-правила.",
            Detail = domain.Message
        },
        _ => new ProblemDetails
        {
            Status = StatusCodes.Status500InternalServerError,
            Title = "Внутрішня помилка серверу.",
            Detail = "Сталася неочікувана помилка. Спробуйте пізніше."
        }
    };
}
