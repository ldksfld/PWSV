using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.WebUtilities;

namespace PWSV.Api.Extensions;

public static class ProblemDetailsExtensions
{
    public const string CorrelationIdKey = "correlationId";

    private const string DefaultValidationTitle = "One or more validation errors occurred.";
    private const string RequestBodyKey = "body";

    private static readonly Dictionary<int, (string Title, string Detail)> LocalizedDefaults = new()
    {
        [StatusCodes.Status400BadRequest] = ("Некоректний запит.", "Запит містить некоректні дані."),
        [StatusCodes.Status401Unauthorized] = ("Необхідна автентифікація.", "Увійдіть до системи, щоб виконати цю дію."),
        [StatusCodes.Status403Forbidden] = ("Доступ заборонено.", "Недостатньо прав для виконання цієї дії."),
        [StatusCodes.Status404NotFound] = ("Ресурс не знайдено.", "Запитаний ресурс не існує."),
        [StatusCodes.Status405MethodNotAllowed] = ("Метод не підтримується.", "Цей HTTP-метод не підтримується для ресурсу."),
        [StatusCodes.Status409Conflict] = ("Конфлікт стану.", "Операцію неможливо виконати в поточному стані даних."),
        [StatusCodes.Status415UnsupportedMediaType] = ("Непідтримуваний формат даних.", "Тіло запиту має бути у форматі JSON."),
        [StatusCodes.Status422UnprocessableEntity] = ("Порушення бізнес-правила.", "Операція порушує бізнес-правило."),
        [StatusCodes.Status429TooManyRequests] = ("Забагато запитів.", "Перевищено ліміт запитів. Спробуйте пізніше."),
        [StatusCodes.Status500InternalServerError] = ("Внутрішня помилка серверу.", "Сталася неочікувана помилка. Спробуйте пізніше.")
    };

    public static IServiceCollection AddLocalizedProblemDetails(this IServiceCollection services)
    {
        return services.AddProblemDetails(options => options.CustomizeProblemDetails = Customize);
    }

    public static IActionResult CreateInvalidModelStateResponse(ActionContext context)
    {
        var factory = context.HttpContext.RequestServices.GetRequiredService<ProblemDetailsFactory>();
        var problem = factory.CreateValidationProblemDetails(
            context.HttpContext,
            context.ModelState,
            StatusCodes.Status400BadRequest,
            title: "Помилки валідації.",
            detail: "Запит відсутній або має некоректний формат.");

        var bodyParameters = context.ActionDescriptor.Parameters
            .Where(p => p.BindingInfo?.BindingSource == BindingSource.Body)
            .Select(p => p.Name)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var errors = problem.Errors.Keys
            .Select(key => NormalizeKey(key, bodyParameters))
            .Distinct(StringComparer.Ordinal)
            .ToList();

        problem.Errors.Clear();
        foreach (var key in errors)
        {
            problem.Errors[key] = key == RequestBodyKey
                ? ["Тіло запиту відсутнє або не є коректним JSON."]
                : ["Некоректне значення."];
        }

        return new BadRequestObjectResult(problem)
        {
            ContentTypes = { "application/problem+json" }
        };
    }

    private static void Customize(ProblemDetailsContext context)
    {
        var problem = context.ProblemDetails;
        var status = problem.Status ?? context.HttpContext.Response.StatusCode;
        problem.Extensions[CorrelationIdKey] = context.HttpContext.TraceIdentifier;

        if (!LocalizedDefaults.TryGetValue(status, out var localized))
        {
            return;
        }

        var isDefaultTitle = problem.Title is null
            || problem.Title == DefaultValidationTitle
            || problem.Title == ReasonPhrases.GetReasonPhrase(status);

        if (isDefaultTitle)
        {
            problem.Title = localized.Title;
            problem.Detail ??= localized.Detail;
        }
    }

    private static string NormalizeKey(string key, HashSet<string> bodyParameters)
    {
        if (key.StartsWith("$.", StringComparison.Ordinal))
        {
            return key[2..];
        }

        return key is "" or "$" || bodyParameters.Contains(key) ? RequestBodyKey : key;
    }
}
