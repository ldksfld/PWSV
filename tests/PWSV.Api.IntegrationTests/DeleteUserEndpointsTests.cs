using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PWSV.Application.Common.Exceptions;
using PWSV.Application.Common.Interfaces;
using PWSV.Infrastructure.Persistence;

namespace PWSV.Api.IntegrationTests;

public sealed class DeleteUserEndpointsTests : IAsyncLifetime
{
    private const string DeleteUserPath = "/api/v1/auth/delete-user";

    private readonly PwsvWebApplicationFactory _factory = new();
    private ApiTestClient _api = null!;

    public async Task InitializeAsync()
    {
        _api = new ApiTestClient(_factory.CreateClient());
        await _api.RegisterAsync();
    }

    public async Task DisposeAsync()
    {
        await _factory.DisposeDatabaseAsync();
        _factory.Dispose();
    }

    [Fact]
    public async Task DeleteUser_WithWrongOrMissingSecrets_ReturnsFieldErrorsAndKeepsData()
    {
        var account = await _api.CreateAccountAsync("Готівка", "UAH", 500m, "UA00 0000 0000 0000 0262 0000 0000 9");

        var (passwordStatus, passwordBody) = await _api.SendAsync(HttpMethod.Post, DeleteUserPath,
            new { password = "Wrong#Pass1", masterPassword = ApiTestClient.MasterPassword });
        var (masterStatus, masterBody) = await _api.SendAsync(HttpMethod.Post, DeleteUserPath,
            new { password = ApiTestClient.Password, masterPassword = "Typo#Master2026" });
        var (emptyStatus, emptyBody) = await _api.SendAsync(HttpMethod.Post, DeleteUserPath,
            new { password = "", masterPassword = "" });
        var (_, details) = await _api.SendAsync(HttpMethod.Get, $"/api/v1/accounts/{account}");

        passwordStatus.Should().Be(HttpStatusCode.BadRequest);
        passwordBody.GetProperty("errors").GetProperty("password")[0].GetString().Should().Be("Пароль невірний.");
        masterStatus.Should().Be(HttpStatusCode.BadRequest);
        masterBody.GetProperty("errors").GetProperty("masterPassword")[0].GetString().Should().Be("Майстер-пароль невірний.");
        emptyStatus.Should().Be(HttpStatusCode.BadRequest);
        emptyBody.GetProperty("errors").EnumerateObject().Select(p => p.Name).Should().BeEquivalentTo(["password", "masterPassword"]);
        details.GetProperty("account").GetProperty("balance").GetString().Should().Be("500.00");
        details.GetProperty("account").GetProperty("accountNumber").GetString().Should().Be("UA00 0000 0000 0000 0262 0000 0000 9");
    }

    [Fact]
    public async Task DeleteUser_RemovesAllUserDataKeepsReferenceDataAndAllowsNewRegistration()
    {
        var uah = await _api.CreateAccountAsync("Готівка", "UAH", 1_000m, "UA00 0000 0000 0000 0262 0000 0000 9");
        var usd = await _api.CreateAccountAsync("Долари", "USD");
        var salary = await _api.CreateCategoryAsync("Зарплата", "Income");
        var food = await _api.CreateCategoryAsync("Продукти", "Expense");
        var (_, market) = await _api.SendAsync(HttpMethod.Post, "/api/v1/categories", new { name = "Ринок", kind = "Expense", parentCategoryId = food });
        await _api.SendAsync(HttpMethod.Post, $"/api/v1/categories/{market.GetProperty("id").GetInt32()}/deactivate");
        var occurredAt = DateTime.UtcNow.AddHours(-1);
        await _api.SendAsync(HttpMethod.Post, "/api/v1/transactions/income",
            new { accountId = uah, categoryId = salary, amount = 2_500m, occurredAt, description = "Зарплата за жовтень" });
        await _api.SendAsync(HttpMethod.Post, "/api/v1/transactions/expense",
            new { accountId = uah, categoryId = food, amount = 320m, occurredAt, counterparty = "Ринок" });
        var (transferStatus, _) = await _api.SendAsync(HttpMethod.Post, "/api/v1/transactions/transfer",
            new { sourceAccountId = uah, destinationAccountId = usd, amount = 410m, exchangeRate = 0.0241m, occurredAt });
        transferStatus.Should().Be(HttpStatusCode.Created);
        await _api.SendAsync(HttpMethod.Post, "/api/v1/exchange-rates", new
        {
            baseCurrencyId = await _api.GetCurrencyIdAsync("USD"),
            quoteCurrencyId = await _api.GetCurrencyIdAsync("UAH"),
            rate = 41.5m,
            effectiveDate = DateOnly.FromDateTime(DateTime.UtcNow).ToString("yyyy-MM-dd")
        });
        var (currencyStatus, _) = await _api.SendAsync(HttpMethod.Post, "/api/v1/currencies", new { code = "CHF", name = "Швейцарський франк", symbol = "₣", decimalPlaces = 2 });
        currencyStatus.Should().Be(HttpStatusCode.Created);
        int auditEntries;
        using (var scope = _factory.Services.CreateScope())
        {
            auditEntries = await scope.ServiceProvider.GetRequiredService<ApplicationDbContext>().AuditEntries.CountAsync();
        }

        var (status, _) = await _api.SendAsync(HttpMethod.Post, DeleteUserPath,
            new { password = ApiTestClient.Password, masterPassword = ApiTestClient.MasterPassword });

        status.Should().Be(HttpStatusCode.NoContent);
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            (await db.Users.AnyAsync()).Should().BeFalse();
            (await db.KeyDerivationConfigs.AnyAsync()).Should().BeFalse();
            (await db.Accounts.AnyAsync()).Should().BeFalse();
            (await db.Transactions.AnyAsync()).Should().BeFalse();
            (await db.Categories.AnyAsync()).Should().BeFalse();
            (await db.ExchangeRates.AnyAsync()).Should().BeFalse();
            (await db.Currencies.Select(c => c.Code).ToListAsync()).Should().Contain(["UAH", "USD", "CHF"]).And.HaveCount(9);
            (await db.AccountTypes.CountAsync()).Should().Be(4);
            (await db.AuditEntries.CountAsync()).Should().Be(auditEntries);
            (await db.AuditEntries.AnyAsync(a => a.UserId != null)).Should().BeFalse();
        }

        _factory.Services.GetRequiredService<ICryptoService>()
            .Invoking(c => c.Encrypt("PWSV")).Should().Throw<UnauthorizedException>();

        var fresh = new ApiTestClient(_factory.CreateClient());
        await fresh.RegisterAsync();
        var (_, accounts) = await fresh.SendAsync(HttpMethod.Get, "/api/v1/accounts?includeInactive=true");
        var (_, categories) = await fresh.SendAsync(HttpMethod.Get, "/api/v1/categories?includeInactive=true");
        var (_, rates) = await fresh.SendAsync(HttpMethod.Get, "/api/v1/exchange-rates");
        var (_, transactions) = await fresh.SendAsync(HttpMethod.Get, "/api/v1/transactions");
        accounts.GetArrayLength().Should().Be(0);
        categories.GetArrayLength().Should().Be(0);
        rates.GetArrayLength().Should().Be(0);
        transactions.GetProperty("totalCount").GetInt32().Should().Be(0);
    }

    [Fact]
    public async Task DeleteUser_WithoutToken_ReturnsUnauthorizedAndKeepsUser()
    {
        var anonymous = _factory.CreateClient();

        var response = await anonymous.PostAsJsonAsync(DeleteUserPath,
            new { password = ApiTestClient.Password, masterPassword = ApiTestClient.MasterPassword });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        using var scope = _factory.Services.CreateScope();
        (await scope.ServiceProvider.GetRequiredService<ApplicationDbContext>().Users.CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task StaleToken_AfterDeletion_IsRejectedForReadsAndWrites()
    {
        var secrets = new { password = ApiTestClient.Password, masterPassword = ApiTestClient.MasterPassword };

        var (deleteStatus, _) = await _api.SendAsync(HttpMethod.Post, DeleteUserPath, secrets);
        var (repeatStatus, _) = await _api.SendAsync(HttpMethod.Post, DeleteUserPath, secrets);
        var (readStatus, _) = await _api.SendAsync(HttpMethod.Get, "/api/v1/accounts");
        var (writeStatus, _) = await _api.SendAsync(HttpMethod.Post, "/api/v1/categories", new { name = "Привид", kind = "Expense", parentCategoryId = (int?)null });

        deleteStatus.Should().Be(HttpStatusCode.NoContent);
        repeatStatus.Should().Be(HttpStatusCode.Unauthorized);
        readStatus.Should().Be(HttpStatusCode.Unauthorized);
        writeStatus.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task StaleToken_AfterReRegistrationWithSameUsername_IsRejectedWhileNewTokenWorks()
    {
        await _api.SendAsync(HttpMethod.Post, DeleteUserPath,
            new { password = ApiTestClient.Password, masterPassword = ApiTestClient.MasterPassword });
        var fresh = new ApiTestClient(_factory.CreateClient());
        await fresh.RegisterAsync();

        var (staleStatus, _) = await _api.SendAsync(HttpMethod.Get, "/api/v1/accounts");
        var (freshStatus, _) = await fresh.SendAsync(HttpMethod.Get, "/api/v1/accounts");

        staleStatus.Should().Be(HttpStatusCode.Unauthorized);
        freshStatus.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task TokenWithExistingUserIdButAnotherUsername_IsRejected()
    {
        int userId;
        using (var scope = _factory.Services.CreateScope())
        {
            userId = await scope.ServiceProvider.GetRequiredService<ApplicationDbContext>().Users.Select(u => u.Id).SingleAsync();
        }

        var forged = _factory.Services.GetRequiredService<IJwtService>().Generate(userId, "someone-else");
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", forged.AccessToken);

        var response = await client.GetAsync("/api/v1/accounts");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
