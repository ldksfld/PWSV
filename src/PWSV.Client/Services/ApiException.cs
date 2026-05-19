using PWSV.Client.Models;

namespace PWSV.Client.Services;

public sealed class ApiException : Exception
{
    public ApiException(int statusCode, ProblemDetailsModel? problem)
        : base(problem?.Detail ?? problem?.Title ?? $"Помилка серверу ({statusCode}).")
    {
        StatusCode = statusCode;
        Problem = problem;
    }

    public int StatusCode { get; }
    public ProblemDetailsModel? Problem { get; }
}
