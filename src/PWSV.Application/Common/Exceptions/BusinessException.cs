namespace PWSV.Application.Common.Exceptions;

public abstract class BusinessException : Exception
{
    protected BusinessException(string message) : base(message)
    {
    }
}
