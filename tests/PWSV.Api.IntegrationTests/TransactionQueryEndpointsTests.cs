using System.Net;
using FluentAssertions;

namespace PWSV.Api.IntegrationTests;

public sealed class TransactionQueryEndpointsTests : IAsyncLifetime
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

    [Fact]
    public async Task CategoryFilter_IncludesExpensesOfDescendantCategories()
    {
        var account = await _api.CreateAccountAsync("Готівка", "UAH", 1000m);
        var food = await _api.CreateCategoryAsync("Продукти", "Expense");
        var market = await CreateChildAsync("Ринок", food);
        var vegetables = await CreateChildAsync("Овочі", market);
        var taxi = await _api.CreateCategoryAsync("Таксі", "Expense");
        foreach (var category in new[] { food, market, vegetables, taxi })
        {
            await CreateExpenseAsync(account, category, 10m, DateTime.UtcNow.AddHours(-1));
        }

        var (_, byFood) = await _api.SendAsync(HttpMethod.Get, $"/api/v1/transactions?categoryId={food}");
        var (_, byMarket) = await _api.SendAsync(HttpMethod.Get, $"/api/v1/transactions?categoryId={market}");

        byFood.GetProperty("totalCount").GetInt32().Should().Be(3);
        byMarket.GetProperty("items").EnumerateArray().Select(t => t.GetProperty("categoryName").GetString())
            .Should().BeEquivalentTo(["Ринок", "Овочі"]);
    }

    [Fact]
    public async Task DateRange_IncludesBothBoundaries()
    {
        var account = await _api.CreateAccountAsync("Готівка", "UAH", 1000m);
        var food = await _api.CreateCategoryAsync("Продукти", "Expense");
        var start = new DateTime(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc);
        for (var day = 0; day < 10; day++)
        {
            await CreateExpenseAsync(account, food, day + 1, start.AddDays(day).AddHours(12));
        }

        var (status, page) = await _api.SendAsync(HttpMethod.Get, "/api/v1/transactions?kind=Expense&from=2026-09-03T00:00:00Z&to=2026-09-05T23:59:59.999Z");

        status.Should().Be(HttpStatusCode.OK);
        page.GetProperty("items").EnumerateArray().Select(t => ApiTestClient.ParseMoney(t.GetProperty("amount")))
            .Should().BeEquivalentTo([3m, 4m, 5m]);
    }

    [Fact]
    public async Task Pagination_ReturnsEveryItemOnceInDescendingOrder()
    {
        var account = await _api.CreateAccountAsync("Готівка", "UAH", 1000m);
        var food = await _api.CreateCategoryAsync("Продукти", "Expense");
        var start = new DateTime(2026, 8, 1, 9, 0, 0, DateTimeKind.Utc);
        for (var i = 0; i < 12; i++)
        {
            await CreateExpenseAsync(account, food, 1m, start.AddDays(i));
        }

        var ids = new List<long>();
        var dates = new List<DateTime>();
        for (var page = 1; page <= 3; page++)
        {
            var (_, body) = await _api.SendAsync(HttpMethod.Get, $"/api/v1/transactions?kind=Expense&page={page}&pageSize=5");
            body.GetProperty("totalPages").GetInt32().Should().Be(3);
            foreach (var item in body.GetProperty("items").EnumerateArray())
            {
                ids.Add(item.GetProperty("id").GetInt64());
                dates.Add(item.GetProperty("occurredAt").GetDateTime());
            }
        }
        var (_, beyond) = await _api.SendAsync(HttpMethod.Get, "/api/v1/transactions?kind=Expense&page=4&pageSize=5");

        ids.Should().HaveCount(12).And.OnlyHaveUniqueItems();
        dates.Should().BeInDescendingOrder();
        beyond.GetProperty("items").GetArrayLength().Should().Be(0);
    }

    [Fact]
    public async Task ExchangeRates_AreOrderedByDateDescendingThenNewestFirst()
    {
        var uah = await _api.GetCurrencyIdAsync("UAH");
        var usd = await _api.GetCurrencyIdAsync("USD");
        var eur = await _api.GetCurrencyIdAsync("EUR");
        var day = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-3).ToString("yyyy-MM-dd");
        var older = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-10).ToString("yyyy-MM-dd");
        await _api.SendAsync(HttpMethod.Post, "/api/v1/exchange-rates", new { baseCurrencyId = usd, quoteCurrencyId = uah, rate = 41.5m, effectiveDate = older });
        await _api.SendAsync(HttpMethod.Post, "/api/v1/exchange-rates", new { baseCurrencyId = usd, quoteCurrencyId = uah, rate = 41.7m, effectiveDate = day });
        await _api.SendAsync(HttpMethod.Post, "/api/v1/exchange-rates", new { baseCurrencyId = eur, quoteCurrencyId = uah, rate = 48.2m, effectiveDate = day });

        var (_, rates) = await _api.SendAsync(HttpMethod.Get, "/api/v1/exchange-rates");

        rates.EnumerateArray().Select(r => ApiTestClient.ParseMoney(r.GetProperty("rate")))
            .Should().Equal(48.2m, 41.7m, 41.5m);
    }

    private async Task CreateExpenseAsync(int accountId, int categoryId, decimal amount, DateTime occurredAt)
    {
        var (status, _) = await _api.SendAsync(HttpMethod.Post, "/api/v1/transactions/expense", new { accountId, categoryId, amount, occurredAt });
        status.Should().Be(HttpStatusCode.Created);
    }

    private async Task<int> CreateChildAsync(string name, int parentId)
    {
        var (status, body) = await _api.SendAsync(HttpMethod.Post, "/api/v1/categories", new { name, kind = "Expense", parentCategoryId = parentId });
        status.Should().Be(HttpStatusCode.Created);
        return body.GetProperty("id").GetInt32();
    }
}
