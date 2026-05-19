namespace PWSV.Application.Common.Exceptions;

public sealed class ConflictException : BusinessException
{
    public ConflictException(string message) : base(message)
    {
    }
}
