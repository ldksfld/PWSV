using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using PWSV.Application.Common.Exceptions;
using PWSV.Domain.Exceptions;
using ValidationException = PWSV.Application.Common.Exceptions.ValidationException;

namespace PWSV.Api.Middleware;

public sealed class ExceptionHandlingMiddleware(
    RequestDelegate next,
    IProblemDetailsService problemDetailsService,
    ILogger<ExceptionHandlingMiddleware> logger)
{
    private const int SqlUniqueIndexViolation = 2601;
    private const int SqlUniqueConstraintViolation = 2627;
    private const int SqlConstraintViolation = 547;
    private const int SqlUserDefinedError = 50000;

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
        {
            logger.LogInformation("Request {RequestPath} was cancelled by the client", context.Request.Path);
        }
        catch (Exception exception)
        {
            if (context.Response.HasStarted)
            {
                logger.LogError(exception, "Unhandled exception after the response has started");
                throw;
            }

            await HandleAsync(context, exception).ConfigureAwait(false);
        }
    }

    private async Task HandleAsync(HttpContext context, Exception exception)
    {
        var problem = MapToProblem(exception);
        var status = problem.Status ?? StatusCodes.Status500InternalServerError;

        if (status >= StatusCodes.Status500InternalServerError)
        {
            logger.LogError(exception, "Unhandled exception {ExceptionType} mapped to status {StatusCode}",
                exception.GetType().Name, status);
        }
        else
        {
            logger.LogWarning("Request rejected with {ExceptionType} mapped to status {StatusCode}: {Message}",
                exception.GetType().Name, status, exception.Message);
        }

        context.Response.Clear();
        context.Response.StatusCode = status;

        var written = await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = context,
            ProblemDetails = problem,
            Exception = exception
        }).ConfigureAwait(false);

        if (!written)
        {
            await context.Response.WriteAsync(problem.Detail ?? problem.Title ?? string.Empty).ConfigureAwait(false);
        }
    }

    private static ProblemDetails MapToProblem(Exception exception) => exception switch
    {
        ValidationException validation => new ValidationProblemDetails(
            validation.Errors.ToDictionary(kv => JsonNamingPolicy.CamelCase.ConvertName(kv.Key), kv => kv.Value))
        {
            Status = StatusCodes.Status400BadRequest,
            Title = "Помилки валідації.",
            Detail = string.Join(" ", validation.Errors.SelectMany(kv => kv.Value).Distinct())
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
        DbUpdateConcurrencyException => new ProblemDetails
        {
            Status = StatusCodes.Status409Conflict,
            Title = "Конфлікт стану.",
            Detail = "Дані було змінено паралельно. Оновіть дані та повторіть дію."
        },
        DbUpdateException { InnerException: SqlException { Number: SqlUniqueIndexViolation or SqlUniqueConstraintViolation } } => new ProblemDetails
        {
            Status = StatusCodes.Status409Conflict,
            Title = "Конфлікт стану.",
            Detail = "Запис з такими даними вже існує."
        },
        DbUpdateException { InnerException: SqlException { Number: SqlConstraintViolation } } => new ProblemDetails
        {
            Status = StatusCodes.Status409Conflict,
            Title = "Конфлікт стану.",
            Detail = "Операцію відхилено обмеженням цілісності бази даних."
        },
        DbUpdateException { InnerException: SqlException { Number: SqlUserDefinedError } sql } => new ProblemDetails
        {
            Status = StatusCodes.Status409Conflict,
            Title = "Конфлікт стану.",
            Detail = sql.Message
        },
        RetryLimitExceededException => new ProblemDetails
        {
            Status = StatusCodes.Status503ServiceUnavailable,
            Title = "Сервіс тимчасово недоступний.",
            Detail = "Дані зараз змінюються іншою операцією. Повторіть спробу за кілька секунд."
        },
        CryptographicException => new ProblemDetails
        {
            Status = StatusCodes.Status409Conflict,
            Title = "Конфлікт стану.",
            Detail = "Не вдалося розшифрувати захищені дані: їх пошкоджено або змінено."
        },
        BadHttpRequestException badRequest => new ProblemDetails
        {
            Status = badRequest.StatusCode
        },
        _ => new ProblemDetails
        {
            Status = StatusCodes.Status500InternalServerError,
            Title = "Внутрішня помилка серверу.",
            Detail = "Сталася неочікувана помилка. Спробуйте пізніше."
        }
    };
}
