using System.Globalization;
using System.Security.Claims;
using PWSV.Application.Common.Interfaces;

namespace PWSV.Api.Services;

public sealed class CurrentUserService(IHttpContextAccessor httpContextAccessor) : ICurrentUserService
{
    public int? UserId
    {
        get
        {
            var value = httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? httpContextAccessor.HttpContext?.User.FindFirstValue("sub");

            return int.TryParse(value, CultureInfo.InvariantCulture, out var id) ? id : null;
        }
    }

    public string? Username =>
        httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.Name)
        ?? httpContextAccessor.HttpContext?.User.FindFirstValue("unique_name");

    public bool IsAuthenticated => httpContextAccessor.HttpContext?.User.Identity?.IsAuthenticated == true;
}
