using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using PWSV.Client.Models;
using PWSV.Client.Services.Interfaces;

namespace PWSV.Client.Services;

public sealed class ApiClient(IHttpClientFactory factory) : IApiClient
{
    public const string HttpClientName = "PwsvApi";

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters =
        {
            new JsonStringEnumConverter(),
            new MoneyJsonConverter(),
            new NullableMoneyJsonConverter()
        }
    };

    public Task<AuthResponseModel> RegisterAsync(
        string username,
        string password,
        string masterPassword,
        CancellationToken cancellationToken)
    {
        var body = new { username, password, masterPassword };
        return PostAsync<AuthResponseModel>("/api/v1/auth/register", body, cancellationToken);
    }

    public Task<AuthResponseModel> LoginAsync(
        string username,
        string password,
        string masterPassword,
        CancellationToken cancellationToken)
    {
        var body = new { username, password, masterPassword };
        return PostAsync<AuthResponseModel>("/api/v1/auth/login", body, cancellationToken);
    }

    public Task ChangePasswordAsync(string oldPassword, string newPassword, CancellationToken cancellationToken)
    {
        var body = new { oldPassword, newPassword };
        return SendVoidAsync(HttpMethod.Post, "/api/v1/auth/change-password", body, cancellationToken);
    }

    public Task<IReadOnlyList<AccountModel>> GetAccountsAsync(bool includeInactive, CancellationToken cancellationToken)
    {
        var flag = includeInactive.ToString().ToLowerInvariant();
        var url = $"/api/v1/accounts?includeInactive={flag}";
        return GetAsync<IReadOnlyList<AccountModel>>(url, cancellationToken);
    }

    public Task<AccountDetailsModel> GetAccountAsync(int id, CancellationToken cancellationToken)
        => GetAsync<AccountDetailsModel>($"/api/v1/accounts/{id}", cancellationToken);

    public Task<AccountModel> CreateAccountAsync(
        string name,
        int accountTypeId,
        int currencyId,
        string? accountNumber,
        decimal initialBalance,
        CancellationToken cancellationToken)
    {
        var body = new { name, accountTypeId, currencyId, accountNumber, initialBalance };
        return PostAsync<AccountModel>("/api/v1/accounts", body, cancellationToken);
    }

    public Task<AccountModel> UpdateAccountAsync(
        int id,
        string name,
        string? accountNumber,
        bool isActive,
        CancellationToken cancellationToken)
    {
        var body = new { id, name, accountNumber, isActive };
        return SendAsync<AccountModel>(HttpMethod.Put, $"/api/v1/accounts/{id}", body, cancellationToken);
    }

    public Task DeactivateAccountAsync(int id, CancellationToken cancellationToken)
        => SendVoidAsync(HttpMethod.Post, $"/api/v1/accounts/{id}/deactivate", null, cancellationToken);

    public Task<IReadOnlyList<CategoryModel>> GetCategoriesAsync(
        string? kind,
        bool includeInactive,
        CancellationToken cancellationToken)
    {
        var flag = includeInactive.ToString().ToLowerInvariant();
        var url = $"/api/v1/categories?includeInactive={flag}";
        if (!string.IsNullOrEmpty(kind))
        {
            url += $"&kind={Uri.EscapeDataString(kind)}";
        }

        return GetAsync<IReadOnlyList<CategoryModel>>(url, cancellationToken);
    }

    public Task<IReadOnlyList<CategoryTreeNodeModel>> GetCategoryTreeAsync(
        bool includeInactive,
        CancellationToken cancellationToken)
    {
        var flag = includeInactive.ToString().ToLowerInvariant();
        var url = $"/api/v1/categories/tree?includeInactive={flag}";
        return GetAsync<IReadOnlyList<CategoryTreeNodeModel>>(url, cancellationToken);
    }

    public Task<CategoryModel> CreateCategoryAsync(
        string name,
        string kind,
        int? parentCategoryId,
        CancellationToken cancellationToken)
    {
        var body = new { name, kind, parentCategoryId };
        return PostAsync<CategoryModel>("/api/v1/categories", body, cancellationToken);
    }

    public Task<CategoryModel> UpdateCategoryAsync(
        int id,
        string name,
        int? parentCategoryId,
        CancellationToken cancellationToken)
    {
        var body = new { id, name, parentCategoryId };
        return SendAsync<CategoryModel>(HttpMethod.Put, $"/api/v1/categories/{id}", body, cancellationToken);
    }

    public Task DeactivateCategoryAsync(int id, CancellationToken cancellationToken)
        => SendVoidAsync(HttpMethod.Post, $"/api/v1/categories/{id}/deactivate", null, cancellationToken);

    public Task<IReadOnlyList<AccountTypeModel>> GetAccountTypesAsync(CancellationToken cancellationToken)
        => GetAsync<IReadOnlyList<AccountTypeModel>>("/api/v1/account-types", cancellationToken);

    public Task<IReadOnlyList<CurrencyModel>> GetCurrenciesAsync(CancellationToken cancellationToken)
        => GetAsync<IReadOnlyList<CurrencyModel>>("/api/v1/currencies", cancellationToken);

    public Task<CurrencyModel> CreateCurrencyAsync(
        string code,
        string name,
        string symbol,
        byte decimalPlaces,
        CancellationToken cancellationToken)
    {
        var body = new { code, name, symbol, decimalPlaces };
        return PostAsync<CurrencyModel>("/api/v1/currencies", body, cancellationToken);
    }

    public Task<IReadOnlyList<ExchangeRateModel>> GetExchangeRatesAsync(
        int? baseCurrencyId,
        int? quoteCurrencyId,
        DateOnly? from,
        DateOnly? to,
        CancellationToken cancellationToken)
    {
        var parts = new List<string>();
        if (baseCurrencyId.HasValue)
        {
            parts.Add($"baseCurrencyId={baseCurrencyId.Value}");
        }
        if (quoteCurrencyId.HasValue)
        {
            parts.Add($"quoteCurrencyId={quoteCurrencyId.Value}");
        }
        if (from.HasValue)
        {
            parts.Add($"from={from.Value:yyyy-MM-dd}");
        }
        if (to.HasValue)
        {
            parts.Add($"to={to.Value:yyyy-MM-dd}");
        }

        var url = "/api/v1/exchange-rates?" + string.Join("&", parts);
        return GetAsync<IReadOnlyList<ExchangeRateModel>>(url, cancellationToken);
    }

    public Task<ExchangeRateModel> CreateExchangeRateAsync(
        int baseCurrencyId,
        int quoteCurrencyId,
        decimal rate,
        DateOnly effectiveDate,
        CancellationToken cancellationToken)
    {
        var body = new
        {
            baseCurrencyId,
            quoteCurrencyId,
            rate,
            effectiveDate = effectiveDate.ToString("yyyy-MM-dd")
        };
        return PostAsync<ExchangeRateModel>("/api/v1/exchange-rates", body, cancellationToken);
    }

    public Task<PagedResultModel<TransactionModel>> GetTransactionsAsync(
        int? accountId,
        int? categoryId,
        string? kind,
        DateTime? from,
        DateTime? to,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var url = $"/api/v1/transactions?page={page}&pageSize={pageSize}";
        if (accountId.HasValue)
        {
            url += $"&accountId={accountId.Value}";
        }
        if (categoryId.HasValue)
        {
            url += $"&categoryId={categoryId.Value}";
        }
        if (!string.IsNullOrEmpty(kind))
        {
            url += $"&kind={Uri.EscapeDataString(kind)}";
        }
        if (from.HasValue)
        {
            url += $"&from={from.Value:o}";
        }
        if (to.HasValue)
        {
            url += $"&to={to.Value:o}";
        }

        return GetAsync<PagedResultModel<TransactionModel>>(url, cancellationToken);
    }

    public Task<TransactionModel> GetTransactionAsync(long id, CancellationToken cancellationToken)
        => GetAsync<TransactionModel>($"/api/v1/transactions/{id}", cancellationToken);

    public Task<TransactionModel> CreateIncomeAsync(
        int accountId,
        int categoryId,
        decimal amount,
        DateTime occurredAt,
        string? description,
        string? counterparty,
        CancellationToken cancellationToken)
    {
        var body = new { accountId, categoryId, amount, occurredAt, description, counterparty };
        return PostAsync<TransactionModel>("/api/v1/transactions/income", body, cancellationToken);
    }

    public Task<TransactionModel> CreateExpenseAsync(
        int accountId,
        int categoryId,
        decimal amount,
        DateTime occurredAt,
        string? description,
        string? counterparty,
        CancellationToken cancellationToken)
    {
        var body = new { accountId, categoryId, amount, occurredAt, description, counterparty };
        return PostAsync<TransactionModel>("/api/v1/transactions/expense", body, cancellationToken);
    }

    public Task CreateTransferAsync(
        int sourceAccountId,
        int destinationAccountId,
        decimal amount,
        decimal? exchangeRate,
        DateTime occurredAt,
        string? description,
        CancellationToken cancellationToken)
    {
        var body = new
        {
            sourceAccountId,
            destinationAccountId,
            amount,
            exchangeRate,
            occurredAt,
            description
        };
        return SendVoidAsync(HttpMethod.Post, "/api/v1/transactions/transfer", body, cancellationToken);
    }

    public Task<TransactionModel> UpdateTransactionAsync(
        long id,
        int? accountId,
        int? categoryId,
        decimal amount,
        DateTime occurredAt,
        string? description,
        string? counterparty,
        CancellationToken cancellationToken)
    {
        var body = new { id, accountId, categoryId, amount, occurredAt, description, counterparty };
        return SendAsync<TransactionModel>(HttpMethod.Put, $"/api/v1/transactions/{id}", body, cancellationToken);
    }

    public Task DeleteTransactionAsync(long id, CancellationToken cancellationToken)
        => SendVoidAsync(HttpMethod.Delete, $"/api/v1/transactions/{id}", null, cancellationToken);

    private async Task<T> GetAsync<T>(string url, CancellationToken cancellationToken)
    {
        var client = factory.CreateClient(HttpClientName);
        using var response = await client.GetAsync(url, cancellationToken);
        return await ParseAsync<T>(response, cancellationToken);
    }

    private async Task<T> PostAsync<T>(string url, object body, CancellationToken cancellationToken)
    {
        var client = factory.CreateClient(HttpClientName);
        using var response = await client.PostAsJsonAsync(url, body, JsonOptions, cancellationToken);
        return await ParseAsync<T>(response, cancellationToken);
    }

    private async Task<T> SendAsync<T>(HttpMethod method, string url, object? body, CancellationToken cancellationToken)
    {
        var client = factory.CreateClient(HttpClientName);
        using var request = new HttpRequestMessage(method, url);
        if (body is not null)
        {
            request.Content = JsonContent.Create(body, options: JsonOptions);
        }

        using var response = await client.SendAsync(request, cancellationToken);
        return await ParseAsync<T>(response, cancellationToken);
    }

    private async Task SendVoidAsync(HttpMethod method, string url, object? body, CancellationToken cancellationToken)
    {
        var client = factory.CreateClient(HttpClientName);
        using var request = new HttpRequestMessage(method, url);
        if (body is not null)
        {
            request.Content = JsonContent.Create(body, options: JsonOptions);
        }

        using var response = await client.SendAsync(request, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
    }

    private static async Task<T> ParseAsync<T>(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        await EnsureSuccessAsync(response, cancellationToken);
        var data = await response.Content.ReadFromJsonAsync<T>(JsonOptions, cancellationToken);
        if (data is null)
        {
            var emptyProblem = new ProblemDetailsModel { Title = "Порожня відповідь сервера." };
            throw new ApiException((int)response.StatusCode, emptyProblem);
        }

        return data;
    }

    private static async Task EnsureSuccessAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        ProblemDetailsModel? problem = null;
        try
        {
            problem = await response.Content.ReadFromJsonAsync<ProblemDetailsModel>(JsonOptions, cancellationToken);
        }
        catch (JsonException)
        {
            problem = null;
        }

        throw new ApiException((int)response.StatusCode, problem);
    }
}
