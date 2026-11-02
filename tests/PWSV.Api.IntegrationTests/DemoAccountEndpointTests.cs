using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;

namespace PWSV.Api.IntegrationTests;

public sealed class DemoAccountEndpointTests : IAsyncLifetime
{
    private const string DemoPath = "/api/v1/auth/demo";

    private readonly PwsvWebApplicationFactory _factory = new();

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync()
    {
        await _factory.DisposeDatabaseAsync();
        _factory.Dispose();
    }

    [Fact]
    public async Task GetDemo_WithoutUsers_ReturnsNotFound()
    {
        var response = await _factory.CreateClient().GetAsync(DemoPath);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetDemo_ForDemoUser_ReturnsCredentialsUntilPasswordIsChanged()
    {
        var client = _factory.CreateClient();
        var register = await client.PostAsJsonAsync("/api/v1/auth/register",
            new { username = "demo", password = "Demo#Pass2026", masterPassword = "Demo#Master2026" });
        var token = (await register.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("accessToken").GetString();

        var before = await client.GetFromJsonAsync<JsonElement>(DemoPath);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var change = await client.PostAsJsonAsync("/api/v1/auth/change-password",
            new { oldPassword = "Demo#Pass2026", newPassword = "MyOwn#Pass2026" });
        client.DefaultRequestHeaders.Authorization = null;
        var after = await client.GetAsync(DemoPath);

        before.GetProperty("username").GetString().Should().Be("demo");
        before.GetProperty("password").GetString().Should().Be("Demo#Pass2026");
        before.GetProperty("masterPassword").GetString().Should().Be("Demo#Master2026");
        change.StatusCode.Should().Be(HttpStatusCode.NoContent);
        after.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetDemo_WhenOwnAccountIsRegistered_ReturnsNotFound()
    {
        await new ApiTestClient(_factory.CreateClient()).RegisterAsync();

        var response = await _factory.CreateClient().GetAsync(DemoPath);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetDemo_MoreThanTenTimesPerMinute_ReturnsTooManyRequests()
    {
        var client = _factory.CreateClient();

        var statuses = new List<HttpStatusCode>();
        for (var i = 0; i < 11; i++)
        {
            statuses.Add((await client.GetAsync(DemoPath)).StatusCode);
        }

        statuses.Take(10).Should().OnlyContain(status => status == HttpStatusCode.NotFound);
        statuses.Last().Should().Be(HttpStatusCode.TooManyRequests);
    }
}
