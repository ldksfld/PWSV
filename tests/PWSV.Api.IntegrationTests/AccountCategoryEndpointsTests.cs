using System.Net;
using FluentAssertions;

namespace PWSV.Api.IntegrationTests;

public sealed class AccountCategoryEndpointsTests : IAsyncLifetime
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
    public async Task DeactivatingAccount_RequiresZeroBalanceAndIsIdempotent()
    {
        var funded = await _api.CreateAccountAsync("Готівка", "UAH", 100m);
        var empty = await _api.CreateAccountAsync("Стара картка", "UAH");

        var (fundedStatus, fundedBody) = await _api.SendAsync(HttpMethod.Post, $"/api/v1/accounts/{funded}/deactivate");
        var (firstStatus, _) = await _api.SendAsync(HttpMethod.Post, $"/api/v1/accounts/{empty}/deactivate");
        var (secondStatus, _) = await _api.SendAsync(HttpMethod.Post, $"/api/v1/accounts/{empty}/deactivate");
        var (_, activeList) = await _api.SendAsync(HttpMethod.Get, "/api/v1/accounts");
        var (_, fullList) = await _api.SendAsync(HttpMethod.Get, "/api/v1/accounts?includeInactive=true");

        fundedStatus.Should().Be(HttpStatusCode.Conflict);
        fundedBody.GetProperty("detail").GetString().Should().Contain("нульового балансу");
        firstStatus.Should().Be(HttpStatusCode.NoContent);
        secondStatus.Should().Be(HttpStatusCode.NoContent);
        activeList.EnumerateArray().Select(a => a.GetProperty("id").GetInt32()).Should().NotContain(empty);
        fullList.EnumerateArray().Select(a => a.GetProperty("id").GetInt32()).Should().Contain(empty);
    }

    [Fact]
    public async Task UpdatingAccount_EncryptsClearsAndRestoresAccountNumber()
    {
        var account = await _api.CreateAccountAsync("Картка", "UAH", accountNumber: "UA21 3223 1300 0002 6007");

        var (clearStatus, _) = await _api.SendAsync(HttpMethod.Put, $"/api/v1/accounts/{account}", new { id = account, name = "Картка", accountNumber = (string?)null, isActive = true });
        var (_, cleared) = await _api.SendAsync(HttpMethod.Get, $"/api/v1/accounts/{account}");
        var (restoreStatus, _) = await _api.SendAsync(HttpMethod.Put, $"/api/v1/accounts/{account}", new { id = account, name = "  Зарплатна картка  ", accountNumber = " PL61 1090 1014 ", isActive = true });
        var (_, restored) = await _api.SendAsync(HttpMethod.Get, $"/api/v1/accounts/{account}");

        clearStatus.Should().Be(HttpStatusCode.OK);
        cleared.GetProperty("account").GetProperty("accountNumber").ValueKind.Should().Be(System.Text.Json.JsonValueKind.Null);
        restoreStatus.Should().Be(HttpStatusCode.OK);
        restored.GetProperty("account").GetProperty("name").GetString().Should().Be("Зарплатна картка");
        restored.GetProperty("account").GetProperty("accountNumber").GetString().Should().Be("PL61 1090 1014");
    }

    [Fact]
    public async Task CreatingCategory_RejectsDuplicateSiblingButAllowsSameNameUnderOtherParent()
    {
        var food = await _api.CreateCategoryAsync("Продукти", "Expense");
        var transport = await _api.CreateCategoryAsync("Транспорт", "Expense");
        await CreateChildAsync("Супермаркети", food);

        var (duplicateStatus, _) = await _api.SendAsync(HttpMethod.Post, "/api/v1/categories", new { name = "супермаркети", kind = "Expense", parentCategoryId = food });
        var (otherParentStatus, _) = await _api.SendAsync(HttpMethod.Post, "/api/v1/categories", new { name = "Супермаркети", kind = "Expense", parentCategoryId = transport });

        duplicateStatus.Should().Be(HttpStatusCode.Conflict);
        otherParentStatus.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task MovingCategoryUnderOwnDescendant_ReturnsConflict()
    {
        var food = await _api.CreateCategoryAsync("Продукти", "Expense");
        var market = await CreateChildAsync("Ринок", food);
        var vegetables = await CreateChildAsync("Овочі", market);

        var (status, body) = await _api.SendAsync(HttpMethod.Put, $"/api/v1/categories/{food}", new { id = food, name = "Продукти", parentCategoryId = vegetables, isActive = true });

        status.Should().Be(HttpStatusCode.Conflict);
        body.GetProperty("detail").GetString().Should().Contain("нащадком");
    }

    [Fact]
    public async Task DeactivatingParentCategory_CascadesAndBlocksExpensesInChild()
    {
        var account = await _api.CreateAccountAsync("Готівка", "UAH", 500m);
        var food = await _api.CreateCategoryAsync("Продукти", "Expense");
        var market = await CreateChildAsync("Ринок", food);

        var (deactivateStatus, _) = await _api.SendAsync(HttpMethod.Post, $"/api/v1/categories/{food}/deactivate");
        var (expenseStatus, expenseBody) = await _api.SendAsync(HttpMethod.Post, "/api/v1/transactions/expense",
            new { accountId = account, categoryId = market, amount = 10m, occurredAt = DateTime.UtcNow.AddMinutes(-1) });
        var (_, activeTree) = await _api.SendAsync(HttpMethod.Get, "/api/v1/categories/tree");

        deactivateStatus.Should().Be(HttpStatusCode.NoContent);
        expenseStatus.Should().Be(HttpStatusCode.Conflict);
        expenseBody.GetProperty("detail").GetString().Should().Contain("деактивовано");
        activeTree.EnumerateArray().Select(n => n.GetProperty("id").GetInt32()).Should().NotContain(food);
        (await _api.GetBalanceAsync(account)).Should().Be(500m);
    }

    [Fact]
    public async Task MissingResources_ReturnLocalizedNotFoundDetails()
    {
        var (accountStatus, accountBody) = await _api.SendAsync(HttpMethod.Get, "/api/v1/accounts/987654");
        var (transactionStatus, transactionBody) = await _api.SendAsync(HttpMethod.Delete, "/api/v1/transactions/987654");

        accountStatus.Should().Be(HttpStatusCode.NotFound);
        accountBody.GetProperty("detail").GetString().Should().Be("Рахунок з ідентифікатором 987654 не знайдено.");
        transactionStatus.Should().Be(HttpStatusCode.NotFound);
        transactionBody.GetProperty("detail").GetString().Should().Be("Транзакцію з ідентифікатором 987654 не знайдено.");
    }

    [Fact]
    public async Task IncomeWithExpenseCategory_ReturnsLocalizedBusinessRuleViolation()
    {
        var account = await _api.CreateAccountAsync("Готівка", "UAH");
        var food = await _api.CreateCategoryAsync("Продукти", "Expense");

        var (status, body) = await _api.SendAsync(HttpMethod.Post, "/api/v1/transactions/income",
            new { accountId = account, categoryId = food, amount = 10m, occurredAt = DateTime.UtcNow.AddMinutes(-1) });

        status.Should().Be(HttpStatusCode.UnprocessableEntity);
        body.GetProperty("detail").GetString().Should().Be("Тип категорії «витрата» не відповідає типу транзакції «дохід».");
    }

    private async Task<int> CreateChildAsync(string name, int parentId)
    {
        var (status, body) = await _api.SendAsync(HttpMethod.Post, "/api/v1/categories", new { name, kind = "Expense", parentCategoryId = parentId });
        status.Should().Be(HttpStatusCode.Created);
        return body.GetProperty("id").GetInt32();
    }
}
