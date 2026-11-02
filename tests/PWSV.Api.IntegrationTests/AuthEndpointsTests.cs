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

    [Fact]
    public async Task Login_WithWrongMasterPasswordBeforeAnyEncryptedData_ReturnsUnauthorized()
    {
        var api = new ApiTestClient(_factory.CreateClient());
        await api.RegisterAsync();
        var client = _factory.CreateClient();

        var typo = await client.PostAsJsonAsync("/api/v1/auth/login", new
        {
            username = ApiTestClient.Username,
            password = ApiTestClient.Password,
            masterPassword = "Typo#Master2026"
        });
        var correct = await client.PostAsJsonAsync("/api/v1/auth/login", new
        {
            username = ApiTestClient.Username,
            password = ApiTestClient.Password,
            masterPassword = ApiTestClient.MasterPassword
        });

        typo.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        correct.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Login_WithWrongMasterPassword_ReturnsUnauthorizedAndKeepsSessionKey()
    {
        var api = new ApiTestClient(_factory.CreateClient());
        await api.RegisterAsync();
        var account = await api.CreateAccountAsync("Картка", "UAH", accountNumber: "UA00 0000 0000 0000");

        var login = await _factory.CreateClient().PostAsJsonAsync("/api/v1/auth/login", new
        {
            username = ApiTestClient.Username,
            password = ApiTestClient.Password,
            masterPassword = "WrongMaster#2026"
        });

        login.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        var (status, details) = await api.SendAsync(HttpMethod.Get, $"/api/v1/accounts/{account}");
        status.Should().Be(HttpStatusCode.OK);
        details.GetProperty("account").GetProperty("accountNumber").GetString().Should().Be("UA00 0000 0000 0000");
    }

    [Fact]
    public async Task Login_MoreThanThreeTimesPerMinute_ReturnsTooManyRequests()
    {
        var api = new ApiTestClient(_factory.CreateClient());
        await api.RegisterAsync();
        var client = _factory.CreateClient();
        var payload = new
        {
            username = ApiTestClient.Username,
            password = ApiTestClient.Password,
            masterPassword = ApiTestClient.MasterPassword
        };

        for (var attempt = 0; attempt < 3; attempt++)
        {
            (await client.PostAsJsonAsync("/api/v1/auth/login", payload)).StatusCode.Should().Be(HttpStatusCode.OK);
        }

        var rejected = await client.PostAsJsonAsync("/api/v1/auth/login", payload);

        rejected.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
        rejected.Headers.Contains("Retry-After").Should().BeTrue();
        (await rejected.Content.ReadAsStringAsync()).Should().Contain("Забагато спроб входу");
    }
}
