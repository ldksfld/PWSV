namespace PWSV.Client.Services.Interfaces;

public interface ITokenStorage
{
    string? AccessToken { get; }
    DateTime? ExpiresAt { get; }
    int? UserId { get; }
    string? Username { get; }
    bool IsAuthenticated { get; }

    event Action? AuthenticationChanged;

    void Set(int userId, string username, string accessToken, DateTime expiresAt);
    void Clear();
}
