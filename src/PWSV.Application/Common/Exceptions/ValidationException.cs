using FluentValidation.Results;

namespace PWSV.Application.Common.Exceptions;

public sealed class ValidationException : BusinessException
{
    public ValidationException(IEnumerable<ValidationFailure> failures)
        : base("Помилки валідації запиту.")
    {
        Errors = failures
            .GroupBy(f => f.PropertyName, f => f.ErrorMessage)
            .ToDictionary(g => g.Key, g => g.ToArray());
    }

    public IReadOnlyDictionary<string, string[]> Errors { get; }
}
