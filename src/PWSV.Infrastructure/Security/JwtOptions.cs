namespace PWSV.Infrastructure.Security;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Issuer { get; set; } = "PWSV";
    public string Audience { get; set; } = "PWSV.Client";
    public int ExpirationHours { get; set; } = 8;
    public string SecretKey { get; set; } = string.Empty;
}
