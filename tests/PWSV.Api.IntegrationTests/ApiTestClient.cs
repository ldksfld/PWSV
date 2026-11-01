using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;

namespace PWSV.Api.IntegrationTests;

public sealed class ApiTestClient(HttpClient client)
{
    public const string Username = "olena";
    public const string Password = "Strong#Pass123";
    public const string MasterPassword = "MasterKey#2026";

    public HttpClient Http { get; } = client;

    public async Task RegisterAsync()
    {
        var response = await Http.PostAsJsonAsync("/api/v1/auth/register", new { username = Username, password = Password, masterPassword = MasterPassword });
        response.EnsureSuccessStatusCode();
        var body = await ReadJsonAsync(response);
        Http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", body.GetProperty("accessToken").GetString());
    }

    public async Task<int> CreateAccountAsync(string name, string currencyCode, decimal initialBalance = 0m, string? accountNumber = null)
    {
        var currencyId = await GetCurrencyIdAsync(currencyCode);
        var (status, body) = await SendAsync(HttpMethod.Post, "/api/v1/accounts", new { name, accountTypeId = 2, currencyId, accountNumber, initialBalance });
        status.Should().Be(HttpStatusCode.Created);
        return body.GetProperty("id").GetInt32();
    }

    public async Task<int> CreateCategoryAsync(string name, string kind)
    {
        var (status, body) = await SendAsync(HttpMethod.Post, "/api/v1/categories", new { name, kind, parentCategoryId = (int?)null });
        status.Should().Be(HttpStatusCode.Created);
        return body.GetProperty("id").GetInt32();
    }

    public async Task<decimal> GetBalanceAsync(int accountId)
    {
        var (status, body) = await SendAsync(HttpMethod.Get, $"/api/v1/accounts/{accountId}");
        status.Should().Be(HttpStatusCode.OK);
        return ParseMoney(body.GetProperty("account").GetProperty("balance"));
    }

    public async Task<int> GetCurrencyIdAsync(string code)
    {
        var (_, body) = await SendAsync(HttpMethod.Get, "/api/v1/currencies");
        return body.EnumerateArray().Single(c => c.GetProperty("code").GetString() == code).GetProperty("id").GetInt32();
    }

    public async Task<(HttpStatusCode Status, JsonElement Body)> SendAsync(HttpMethod method, string path, object? payload = null)
    {
        using var request = new HttpRequestMessage(method, path);
        if (payload is not null)
        {
            request.Content = JsonContent.Create(payload);
        }

        using var response = await Http.SendAsync(request);
        return (response.StatusCode, await ReadJsonAsync(response));
    }

    public static decimal ParseMoney(JsonElement value) =>
        decimal.Parse(value.GetString()!, NumberStyles.Number, CultureInfo.InvariantCulture);

    private static async Task<JsonElement> ReadJsonAsync(HttpResponseMessage response)
    {
        var content = await response.Content.ReadAsStringAsync();
        return string.IsNullOrEmpty(content)
            ? default
            : JsonDocument.Parse(content).RootElement.Clone();
    }
}
