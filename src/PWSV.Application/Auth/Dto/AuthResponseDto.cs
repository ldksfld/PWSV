namespace PWSV.Application.Auth.Dto;

public sealed record AuthResponseDto(int UserId, string Username, string AccessToken, DateTime ExpiresAt);
