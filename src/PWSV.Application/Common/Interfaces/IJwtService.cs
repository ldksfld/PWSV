namespace PWSV.Application.Common.Interfaces;

public sealed record JwtTokenResult(string AccessToken, DateTime ExpiresAtUtc);

public interface IJwtService
{
    JwtTokenResult Generate(int userId, string username);
}
