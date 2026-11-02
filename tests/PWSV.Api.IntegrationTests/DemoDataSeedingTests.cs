using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using MediatR;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PWSV.Api.Demo;
using PWSV.Application.Common.Exceptions;
using PWSV.Application.Common.Interfaces;
using PWSV.Application.Transactions.Commands.CreateTransferTransaction;
using PWSV.Infrastructure.Persistence;

namespace PWSV.Api.IntegrationTests;

public sealed class DemoDataSeedingTests
{
    private const string DemoUsername = "demo";
    private const string DemoPassword = "Demo#Pass2026";
    private const string DemoMasterPassword = "Demo#Master2026";

    [Fact]
    public async Task FreshDatabase_WithDemoEnabled_SeedsConsistentDemoDataOnlyOnce()
    {
        var now = DateTime.UtcNow;
        var plan = DemoDataPlanBuilder.Build(now, TimeZoneInfo.Local);
        var factory = new PwsvWebApplicationFactory(seedDemoData: true, utcNow: now);
        var restarted = new PwsvWebApplicationFactory(seedDemoData: true, databaseName: factory.DatabaseName, utcNow: now);
        try
        {
            var client = factory.CreateClient();
            var crypto = factory.Services.GetRequiredService<ICryptoService>();
            crypto.Invoking(c => c.Encrypt("PWSV")).Should().Throw<UnauthorizedException>();

            var (demoStatus, demo) = await SendAsync(client, HttpMethod.Get, "/api/v1/auth/demo");
            demoStatus.Should().Be(HttpStatusCode.OK);
            demo.GetProperty("username").GetString().Should().Be(DemoUsername);
            demo.GetProperty("password").GetString().Should().Be(DemoPassword);
            demo.GetProperty("masterPassword").GetString().Should().Be(DemoMasterPassword);

            await LoginAsync(client, DemoUsername, DemoPassword, DemoMasterPassword);
            var (_, accounts) = await SendAsync(client, HttpMethod.Get, "/api/v1/accounts?includeInactive=true");
            var balances = accounts.EnumerateArray().ToDictionary(
                a => a.GetProperty("name").GetString()!,
                a => ApiTestClient.ParseMoney(a.GetProperty("balance")));
            var expected = plan.Accounts.ToDictionary(a => a.Name, a => plan.ExpectedBalances[a.Key]);
            balances.Should().Equal(expected);
            accounts.EnumerateArray().Single(a => a.GetProperty("name").GetString() == "Стара картка")
                .GetProperty("isActive").GetBoolean().Should().BeFalse();

            var salaryId = accounts.EnumerateArray().Single(a => a.GetProperty("name").GetString() == "Зарплатна картка").GetProperty("id").GetInt32();
            var (_, salary) = await SendAsync(client, HttpMethod.Get, $"/api/v1/accounts/{salaryId}");
            salary.GetProperty("account").GetProperty("accountNumber").GetString()
                .Should().Be(plan.Accounts.Single(a => a.Key == "salary").AccountNumber);

            var transactions = await GetAllTransactionsAsync(client);
            transactions.Should().HaveCount(plan.Transactions.Count + plan.Transactions.OfType<DemoTransfer>().Count());
            transactions.Select(t => t.GetProperty("description").GetString()).Should().Contain("Залишок на початок обліку");
            foreach (var account in accounts.EnumerateArray())
            {
                var id = account.GetProperty("id").GetInt32();
                var signedSum = transactions.Where(t => t.GetProperty("accountId").GetInt32() == id)
                    .Sum(t => (t.GetProperty("direction").GetString() == "Incoming" ? 1 : -1) * ApiTestClient.ParseMoney(t.GetProperty("amount")));
                signedSum.Should().Be(ApiTestClient.ParseMoney(account.GetProperty("balance")));
            }

            var (_, categories) = await SendAsync(client, HttpMethod.Get, "/api/v1/categories?includeInactive=true");
            categories.EnumerateArray().Select(c => c.GetProperty("name").GetString())
                .Should().BeEquivalentTo(plan.Categories.Select(c => c.Name));
            categories.EnumerateArray().Single(c => c.GetProperty("name").GetString() == "Хобі")
                .GetProperty("isActive").GetBoolean().Should().BeFalse();

            var (_, rates) = await SendAsync(client, HttpMethod.Get, "/api/v1/exchange-rates");
            rates.GetArrayLength().Should().Be(plan.ExchangeRates.Count);

            factory.Dispose();
            restarted.CreateClient();
            using var scope = restarted.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            (await db.Users.CountAsync()).Should().Be(1);
            (await db.Transactions.CountAsync()).Should().Be(transactions.Count);
            (await db.ExchangeRates.CountAsync()).Should().Be(plan.ExchangeRates.Count);
        }
        finally
        {
            await restarted.DisposeDatabaseAsync();
            restarted.Dispose();
            factory.Dispose();
        }
    }

    [Fact]
    public async Task ExistingDatabase_WithDemoEnabled_IsNotSeeded()
    {
        var first = new PwsvWebApplicationFactory();
        var api = new ApiTestClient(first.CreateClient());
        await api.RegisterAsync();
        first.Dispose();

        var second = new PwsvWebApplicationFactory(seedDemoData: true, databaseName: first.DatabaseName);
        try
        {
            var client = second.CreateClient();

            var (demoStatus, _) = await SendAsync(client, HttpMethod.Get, "/api/v1/auth/demo");
            demoStatus.Should().Be(HttpStatusCode.NotFound);
            await LoginAsync(client, ApiTestClient.Username, ApiTestClient.Password, ApiTestClient.MasterPassword);
            using var scope = second.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            (await db.Users.Select(u => u.Username).ToListAsync()).Should().Equal(ApiTestClient.Username);
        }
        finally
        {
            await second.DisposeDatabaseAsync();
            second.Dispose();
        }
    }

    [Fact]
    public async Task DatabaseWhoseFilesWereDeleted_IsRecreatedWithDemoData()
    {
        var first = new PwsvWebApplicationFactory();
        await new ApiTestClient(first.CreateClient()).RegisterAsync();
        List<string> files;
        using (var scope = first.Services.CreateScope())
        {
            files = await scope.ServiceProvider.GetRequiredService<ApplicationDbContext>().Database
                .SqlQueryRaw<string>("SELECT physical_name AS [Value] FROM sys.database_files")
                .ToListAsync();
        }

        first.Dispose();
        SqlConnection.ClearAllPools();
        var master = new SqlConnectionStringBuilder(first.ConnectionString) { InitialCatalog = "master" };
        await using (var connection = new SqlConnection(master.ConnectionString))
        {
            await connection.OpenAsync();
            await using var command = connection.CreateCommand();
            command.CommandText = $"ALTER DATABASE [{first.DatabaseName}] SET OFFLINE WITH ROLLBACK IMMEDIATE";
            await command.ExecuteNonQueryAsync();
        }

        files.ForEach(File.Delete);

        var second = new PwsvWebApplicationFactory(seedDemoData: true, databaseName: first.DatabaseName);
        try
        {
            var client = second.CreateClient();

            var (demoStatus, _) = await SendAsync(client, HttpMethod.Get, "/api/v1/auth/demo");
            demoStatus.Should().Be(HttpStatusCode.OK);
            files.Should().OnlyContain(file => File.Exists(file));
            using var scope = second.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            (await db.Users.Select(u => u.Username).ToListAsync()).Should().Equal(DemoUsername);
        }
        finally
        {
            await second.DisposeDatabaseAsync();
            second.Dispose();
        }
    }

    [Fact]
    public async Task FreshDatabase_WithDemoDisabled_StartsWithoutUsers()
    {
        var factory = new PwsvWebApplicationFactory();
        try
        {
            var client = factory.CreateClient();

            var (demoStatus, _) = await SendAsync(client, HttpMethod.Get, "/api/v1/auth/demo");

            demoStatus.Should().Be(HttpStatusCode.NotFound);
            using var scope = factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            (await db.Users.AnyAsync()).Should().BeFalse();
            (await db.Currencies.CountAsync()).Should().Be(8);
        }
        finally
        {
            await factory.DisposeDatabaseAsync();
            factory.Dispose();
        }
    }

    [Fact]
    public async Task SeedingFailure_RemovesPartialDemoDataAndKeepsDatabaseReadyForRegistration()
    {
        var factory = new PwsvWebApplicationFactory(
            seedDemoData: true,
            configureServices: services =>
                services.AddTransient<IPipelineBehavior<CreateTransferTransactionCommand, TransferResult>, FailingTransferBehavior>());
        try
        {
            var client = factory.CreateClient();

            using (var scope = factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                (await db.Users.AnyAsync()).Should().BeFalse();
                (await db.Accounts.AnyAsync()).Should().BeFalse();
                (await db.Transactions.AnyAsync()).Should().BeFalse();
                (await db.Categories.AnyAsync()).Should().BeFalse();
                (await db.ExchangeRates.AnyAsync()).Should().BeFalse();
                (await db.KeyDerivationConfigs.AnyAsync()).Should().BeFalse();
                (await db.AuditEntries.AnyAsync(a => a.UserId != null)).Should().BeFalse();
                (await db.AuditEntries.CountAsync(a => a.EntityName == "Transaction" && a.Action == "INSERT")).Should().BeGreaterThan(10);
            }

            factory.Services.GetRequiredService<ICryptoService>()
                .Invoking(c => c.Encrypt("PWSV")).Should().Throw<UnauthorizedException>();
            var (demoStatus, _) = await SendAsync(client, HttpMethod.Get, "/api/v1/auth/demo");
            demoStatus.Should().Be(HttpStatusCode.NotFound);
            await new ApiTestClient(client).RegisterAsync();
        }
        finally
        {
            await factory.DisposeDatabaseAsync();
            factory.Dispose();
        }
    }

    private static async Task LoginAsync(HttpClient client, string username, string password, string masterPassword)
    {
        var response = await client.PostAsJsonAsync("/api/v1/auth/login", new { username, password, masterPassword });
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", body.GetProperty("accessToken").GetString());
    }

    private static async Task<List<JsonElement>> GetAllTransactionsAsync(HttpClient client)
    {
        var items = new List<JsonElement>();
        for (var page = 1; ; page++)
        {
            var (_, body) = await SendAsync(client, HttpMethod.Get, $"/api/v1/transactions?page={page}&pageSize=200");
            items.AddRange(body.GetProperty("items").EnumerateArray());
            if (page >= body.GetProperty("totalPages").GetInt32())
            {
                return items;
            }
        }
    }

    private static Task<(HttpStatusCode Status, JsonElement Body)> SendAsync(HttpClient client, HttpMethod method, string path) =>
        new ApiTestClient(client).SendAsync(method, path);

    private sealed class FailingTransferBehavior : IPipelineBehavior<CreateTransferTransactionCommand, TransferResult>
    {
        public Task<TransferResult> Handle(
            CreateTransferTransactionCommand request,
            RequestHandlerDelegate<TransferResult> next,
            CancellationToken cancellationToken) =>
            throw new InvalidOperationException("Simulated failure while seeding demo data.");
    }
}
