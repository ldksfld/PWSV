namespace PWSV.Client.Models;

public sealed record AuthResponseModel(int UserId, string Username, string AccessToken, DateTime ExpiresAt);
