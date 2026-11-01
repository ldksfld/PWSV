using System.Net;
using FluentAssertions;

namespace PWSV.Api.IntegrationTests;

public sealed class TransactionEndpointsTests : IAsyncLifetime
{
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

    [Theory]
    [InlineData("source")]
    [InlineData("destination")]
    public async Task DeletingTransfer_ByEitherLeg_RestoresBothBalances(string leg)
    {
        var cash = await _api.CreateAccountAsync("Готівка", "UAH", 1000m);
        var card = await _api.CreateAccountAsync("Картка", "UAH");

        var (status, transfer) = await _api.SendAsync(HttpMethod.Post, "/api/v1/transactions/transfer",
            new { sourceAccountId = cash, destinationAccountId = card, amount = 300m, occurredAt = DateTime.UtcNow.AddMinutes(-1) });
        status.Should().Be(HttpStatusCode.Created);
        (await _api.GetBalanceAsync(cash)).Should().Be(700m);
        (await _api.GetBalanceAsync(card)).Should().Be(300m);

        var legId = transfer.GetProperty(leg).GetProperty("id").GetInt64();
        var (deleteStatus, _) = await _api.SendAsync(HttpMethod.Delete, $"/api/v1/transactions/{legId}");

        deleteStatus.Should().Be(HttpStatusCode.NoContent);
        (await _api.GetBalanceAsync(cash)).Should().Be(1000m);
        (await _api.GetBalanceAsync(card)).Should().Be(0m);
    }

    [Fact]
    public async Task IncomeExpenseUpdateDelete_FollowsIntegrityScenarios()
    {
        var account = await _api.CreateAccountAsync("Рахунок", "UAH");
        var salary = await _api.CreateCategoryAsync("Зарплата", "Income");
        var food = await _api.CreateCategoryAsync("Продукти", "Expense");
        var occurredAt = DateTime.UtcNow.AddHours(-1);

        await _api.SendAsync(HttpMethod.Post, "/api/v1/transactions/income",
            new { accountId = account, categoryId = salary, amount = 1000m, occurredAt });
        (await _api.GetBalanceAsync(account)).Should().Be(1000m);

        var (_, expense) = await _api.SendAsync(HttpMethod.Post, "/api/v1/transactions/expense",
            new { accountId = account, categoryId = food, amount = 250m, occurredAt });
        (await _api.GetBalanceAsync(account)).Should().Be(750m);

        var expenseId = expense.GetProperty("id").GetInt64();
        var (updateStatus, _) = await _api.SendAsync(HttpMethod.Put, $"/api/v1/transactions/{expenseId}",
            new { id = expenseId, accountId = account, categoryId = food, amount = 300m, occurredAt });
        updateStatus.Should().Be(HttpStatusCode.OK);
        (await _api.GetBalanceAsync(account)).Should().Be(700m);

        await _api.SendAsync(HttpMethod.Delete, $"/api/v1/transactions/{expenseId}");
        (await _api.GetBalanceAsync(account)).Should().Be(1000m);
    }

    [Fact]
    public async Task CrossCurrencyTransfer_CreditsRoundedConvertedAmount()
    {
        var uah = await _api.CreateAccountAsync("Гривні", "UAH", 5000m);
        var usd = await _api.CreateAccountAsync("Долари", "USD");

        var (status, transfer) = await _api.SendAsync(HttpMethod.Post, "/api/v1/transactions/transfer",
            new { sourceAccountId = uah, destinationAccountId = usd, amount = 4150m, exchangeRate = 0.02412345m, occurredAt = DateTime.UtcNow.AddMinutes(-1) });

        status.Should().Be(HttpStatusCode.Created);
        ApiTestClient.ParseMoney(transfer.GetProperty("destination").GetProperty("amount")).Should().Be(100.11m);
        transfer.GetProperty("source").GetProperty("direction").GetString().Should().Be("Outgoing");
        transfer.GetProperty("destination").GetProperty("direction").GetString().Should().Be("Incoming");
        (await _api.GetBalanceAsync(uah)).Should().Be(850m);
        (await _api.GetBalanceAsync(usd)).Should().Be(100.11m);
    }

    [Fact]
    public async Task ExchangeRate_IsReturnedWithFullPrecision_AndDuplicateDateIsRejected()
    {
        var uah = await _api.GetCurrencyIdAsync("UAH");
        var eur = await _api.GetCurrencyIdAsync("EUR");
        var payload = new { baseCurrencyId = uah, quoteCurrencyId = eur, rate = 0.02163498m, effectiveDate = "2026-10-15" };

        var (status, created) = await _api.SendAsync(HttpMethod.Post, "/api/v1/exchange-rates", payload);
        var (duplicateStatus, duplicate) = await _api.SendAsync(HttpMethod.Post, "/api/v1/exchange-rates", payload);

        status.Should().Be(HttpStatusCode.Created);
        ApiTestClient.ParseMoney(created.GetProperty("rate")).Should().Be(0.02163498m);
        duplicateStatus.Should().Be(HttpStatusCode.Conflict);
        duplicate.GetProperty("correlationId").GetString().Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task InvalidAmount_ReturnsLocalizedValidationProblem()
    {
        var account = await _api.CreateAccountAsync("Рахунок", "UAH");
        var food = await _api.CreateCategoryAsync("Продукти", "Expense");

        var (status, problem) = await _api.SendAsync(HttpMethod.Post, "/api/v1/transactions/expense",
            new { accountId = account, categoryId = food, amount = 0.001m, occurredAt = DateTime.UtcNow });

        status.Should().Be(HttpStatusCode.BadRequest);
        problem.GetProperty("title").GetString().Should().Be("Помилки валідації.");
        problem.GetProperty("errors").TryGetProperty("amount", out _).Should().BeTrue();
        problem.GetProperty("correlationId").GetString().Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task TransactionDates_AreReturnedAsUtc()
    {
        var account = await _api.CreateAccountAsync("Рахунок", "UAH", 100m);

        var (status, page) = await _api.SendAsync(HttpMethod.Get, $"/api/v1/transactions?accountId={account}");

        status.Should().Be(HttpStatusCode.OK);
        page.GetProperty("pageNumber").GetInt32().Should().Be(1);
        page.GetProperty("items").EnumerateArray()
            .Should().OnlyContain(item => item.GetProperty("occurredAt").GetString()!.EndsWith('Z'));
    }

    [Fact]
    public async Task RequestWithoutToken_ReturnsProblemDetails()
    {
        _api.Http.DefaultRequestHeaders.Authorization = null;

        var (status, problem) = await _api.SendAsync(HttpMethod.Get, "/api/v1/accounts");

        status.Should().Be(HttpStatusCode.Unauthorized);
        problem.GetProperty("title").GetString().Should().Be("Необхідна автентифікація.");
    }
}
