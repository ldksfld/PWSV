using PWSV.Client.Models;

namespace PWSV.Client.Services.Interfaces;

public interface IApiClient
{
    Task<AuthResponseModel> RegisterAsync(
        string username,
        string password,
        string masterPassword,
        CancellationToken cancellationToken);

    Task<AuthResponseModel> LoginAsync(
        string username,
        string password,
        string masterPassword,
        CancellationToken cancellationToken);

    Task ChangePasswordAsync(string oldPassword, string newPassword, CancellationToken cancellationToken);

    Task<IReadOnlyList<AccountModel>> GetAccountsAsync(bool includeInactive, CancellationToken cancellationToken);

    Task<AccountDetailsModel> GetAccountAsync(int id, CancellationToken cancellationToken);

    Task<AccountModel> CreateAccountAsync(
        string name,
        int accountTypeId,
        int currencyId,
        string? accountNumber,
        decimal initialBalance,
        CancellationToken cancellationToken);

    Task<AccountModel> UpdateAccountAsync(
        int id,
        string name,
        string? accountNumber,
        bool isActive,
        CancellationToken cancellationToken);

    Task DeactivateAccountAsync(int id, CancellationToken cancellationToken);

    Task<IReadOnlyList<CategoryModel>> GetCategoriesAsync(
        string? kind,
        bool includeInactive,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<CategoryTreeNodeModel>> GetCategoryTreeAsync(
        bool includeInactive,
        CancellationToken cancellationToken);

    Task<CategoryModel> CreateCategoryAsync(
        string name,
        string kind,
        int? parentCategoryId,
        CancellationToken cancellationToken);

    Task<CategoryModel> UpdateCategoryAsync(
        int id,
        string name,
        int? parentCategoryId,
        CancellationToken cancellationToken);

    Task DeactivateCategoryAsync(int id, CancellationToken cancellationToken);

    Task<IReadOnlyList<AccountTypeModel>> GetAccountTypesAsync(CancellationToken cancellationToken);

    Task<IReadOnlyList<CurrencyModel>> GetCurrenciesAsync(CancellationToken cancellationToken);

    Task<CurrencyModel> CreateCurrencyAsync(
        string code,
        string name,
        string symbol,
        byte decimalPlaces,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<ExchangeRateModel>> GetExchangeRatesAsync(
        int? baseCurrencyId,
        int? quoteCurrencyId,
        DateOnly? from,
        DateOnly? to,
        CancellationToken cancellationToken);

    Task<ExchangeRateModel> CreateExchangeRateAsync(
        int baseCurrencyId,
        int quoteCurrencyId,
        decimal rate,
        DateOnly effectiveDate,
        CancellationToken cancellationToken);

    Task<PagedResultModel<TransactionModel>> GetTransactionsAsync(
        int? accountId,
        int? categoryId,
        string? kind,
        DateTime? from,
        DateTime? to,
        int page,
        int pageSize,
        CancellationToken cancellationToken);

    Task<TransactionModel> GetTransactionAsync(long id, CancellationToken cancellationToken);

    Task<TransactionModel> CreateIncomeAsync(
        int accountId,
        int categoryId,
        decimal amount,
        DateTime occurredAt,
        string? description,
        string? counterparty,
        CancellationToken cancellationToken);

    Task<TransactionModel> CreateExpenseAsync(
        int accountId,
        int categoryId,
        decimal amount,
        DateTime occurredAt,
        string? description,
        string? counterparty,
        CancellationToken cancellationToken);

    Task CreateTransferAsync(
        int sourceAccountId,
        int destinationAccountId,
        decimal amount,
        decimal? exchangeRate,
        DateTime occurredAt,
        string? description,
        CancellationToken cancellationToken);

    Task<TransactionModel> UpdateTransactionAsync(
        long id,
        int? accountId,
        int? categoryId,
        decimal amount,
        DateTime occurredAt,
        string? description,
        string? counterparty,
        CancellationToken cancellationToken);

    Task DeleteTransactionAsync(long id, CancellationToken cancellationToken);
}
