using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using PWSV.Application.Auth.Dto;

namespace PWSV.Api.IntegrationTests;

public sealed class AuthEndpointsTests : IAsyncLifetime
{
    private readonly PwsvWebApplicationFactory _factory = new();

    public Task InitializeAsync()
    {
        _ = _factory.Services;
        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        await _factory.DisposeDatabaseAsync();
        _factory.Dispose();
    }

    [Fact]
    public async Task Register_Then_Login_ReturnsAccessToken()
    {
        var client = _factory.CreateClient();

        var registerResponse = await client.PostAsJsonAsync("/api/v1/auth/register", new
        {
            username = "alice",
            password = "Strong#Pass123",
            masterPassword = "MasterKey#2026"
        });

        registerResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var auth = await registerResponse.Content.ReadFromJsonAsync<AuthResponseDto>();
        auth.Should().NotBeNull();
        auth!.AccessToken.Should().NotBeNullOrEmpty();

        var second = await client.PostAsJsonAsync("/api/v1/auth/register", new
        {
            username = "bob",
            password = "Strong#Pass123",
            masterPassword = "MasterKey#2026"
        });
        second.StatusCode.Should().Be(HttpStatusCode.Conflict);

        var login = await client.PostAsJsonAsync("/api/v1/auth/login", new
        {
            username = "alice",
            password = "Strong#Pass123",
            masterPassword = "MasterKey#2026"
        });
        login.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
