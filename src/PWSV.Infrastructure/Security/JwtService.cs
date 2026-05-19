using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using PWSV.Application.Common.Interfaces;

namespace PWSV.Infrastructure.Security;

public sealed class JwtService : IJwtService
{
    private readonly IDateTimeProvider _clock;
    private readonly JwtOptions _options;

    public JwtService(IOptions<JwtOptions> options, IDateTimeProvider clock)
    {
        _options = options.Value;
        _clock = clock;

        if (string.IsNullOrWhiteSpace(_options.SecretKey) || Encoding.UTF8.GetByteCount(_options.SecretKey) < 32)
        {
            throw new InvalidOperationException("JWT SecretKey must be at least 32 bytes long.");
        }
    }

    public JwtTokenResult Generate(int userId, string username)
    {
        var now = _clock.UtcNow;
        var expires = now.AddHours(_options.ExpirationHours);

        var issuedAtSeconds = new DateTimeOffset(now)
            .ToUnixTimeSeconds()
            .ToString(CultureInfo.InvariantCulture);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, userId.ToString(CultureInfo.InvariantCulture)),
            new Claim(JwtRegisteredClaimNames.UniqueName, username),
            new Claim(JwtRegisteredClaimNames.Iat, issuedAtSeconds, ClaimValueTypes.Integer64)
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.SecretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims: claims,
            notBefore: now,
            expires: expires,
            signingCredentials: credentials);

        var accessToken = new JwtSecurityTokenHandler().WriteToken(token);
        return new JwtTokenResult(accessToken, expires);
    }
}
