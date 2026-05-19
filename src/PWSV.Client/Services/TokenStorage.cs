using PWSV.Client.Services.Interfaces;

namespace PWSV.Client.Services;

public sealed class TokenStorage : ITokenStorage
{
    public string? AccessToken { get; private set; }
    public DateTime? ExpiresAt { get; private set; }
    public int? UserId { get; private set; }
    public string? Username { get; private set; }

    public bool IsAuthenticated => AccessToken is not null && ExpiresAt > DateTime.UtcNow;

    public event Action? AuthenticationChanged;

    public void Set(int userId, string username, string accessToken, DateTime expiresAt)
    {
        UserId = userId;
        Username = username;
        AccessToken = accessToken;
        ExpiresAt = expiresAt.Kind switch
        {
            DateTimeKind.Utc => expiresAt,
            DateTimeKind.Local => expiresAt.ToUniversalTime(),
            _ => DateTime.SpecifyKind(expiresAt, DateTimeKind.Utc)
        };
        AuthenticationChanged?.Invoke();
    }

    public void Clear()
    {
        AccessToken = null;
        ExpiresAt = null;
        UserId = null;
        Username = null;
        AuthenticationChanged?.Invoke();
    }
}
