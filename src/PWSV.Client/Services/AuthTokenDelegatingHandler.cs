using System.Net;
using System.Net.Http.Headers;
using PWSV.Client.Services.Interfaces;

namespace PWSV.Client.Services;

public sealed class AuthTokenDelegatingHandler(ITokenStorage tokenStorage) : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var tokenAttached = false;
        if (tokenStorage.AccessToken is { Length: > 0 } token)
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            tokenAttached = true;
        }

        var response = await base.SendAsync(request, cancellationToken);

        if (response.StatusCode == HttpStatusCode.Unauthorized && tokenAttached)
        {
            tokenStorage.Clear();
        }

        return response;
    }
}
