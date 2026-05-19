using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using PWSV.Infrastructure.Security;

namespace PWSV.Api.Extensions;

public static class WebApplicationBuilderExtensions
{
    public static WebApplicationBuilder ConfigureJwtSecret(this WebApplicationBuilder builder)
    {
        var section = builder.Configuration.GetSection(JwtOptions.SectionName);
        var existing = section["SecretKey"];

        if (!string.IsNullOrWhiteSpace(existing))
        {
            return builder;
        }

        var secretsPath = Path.Combine(builder.Environment.ContentRootPath, "secrets.json");
        string? secret = null;

        if (File.Exists(secretsPath))
        {
            using var stream = File.OpenRead(secretsPath);
            using var doc = JsonDocument.Parse(stream);
            if (doc.RootElement.TryGetProperty("Jwt", out var jwt)
                && jwt.TryGetProperty("SecretKey", out var key)
                && key.ValueKind == JsonValueKind.String)
            {
                secret = key.GetString();
            }
        }

        if (string.IsNullOrWhiteSpace(secret))
        {
            secret = Convert.ToBase64String(RandomNumberGenerator.GetBytes(48));
            var payload = JsonSerializer.SerializeToUtf8Bytes(new { Jwt = new { SecretKey = secret } },
                new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllBytes(secretsPath, payload);
        }

        section["SecretKey"] = secret;
        return builder;
    }

    public static WebApplicationBuilder ConfigureJwtAuthentication(this WebApplicationBuilder builder)
    {
        var section = builder.Configuration.GetSection(JwtOptions.SectionName);
        var issuer = section["Issuer"] ?? "PWSV";
        var audience = section["Audience"] ?? "PWSV.Client";
        var secret = section["SecretKey"]
            ?? throw new InvalidOperationException("JWT SecretKey is not configured.");

        builder.Services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.RequireHttpsMetadata = false;
                options.SaveToken = false;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = issuer,
                    ValidAudience = audience,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret)),
                    ClockSkew = TimeSpan.FromSeconds(30)
                };
            });

        builder.Services.AddAuthorization();
        return builder;
    }
}
