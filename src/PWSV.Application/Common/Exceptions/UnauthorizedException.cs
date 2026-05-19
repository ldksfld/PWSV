namespace PWSV.Application.Common.Exceptions;

public sealed class UnauthorizedException : BusinessException
{
    public UnauthorizedException(string message) : base(message)
    {
    }
}
