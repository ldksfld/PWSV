using System.Diagnostics;
using System.Globalization;
using System.Security.Claims;
using MediatR;
using Microsoft.Extensions.Options;
using PWSV.Application.Accounts.Commands.CreateAccount;
using PWSV.Application.Accounts.Commands.DeactivateAccount;
using PWSV.Application.AccountTypes.Queries.GetAccountTypesList;
using PWSV.Application.Auth;
using PWSV.Application.Auth.Commands.DeleteUser;
using PWSV.Application.Auth.Commands.Register;
using PWSV.Application.Categories.Commands.CreateCategory;
using PWSV.Application.Categories.Commands.DeactivateCategory;
using PWSV.Application.Common.Interfaces;
using PWSV.Application.Currencies.Queries.GetCurrenciesList;
using PWSV.Application.ExchangeRates.Commands.CreateExchangeRate;
using PWSV.Application.Transactions.Commands.CreateExpenseTransaction;
using PWSV.Application.Transactions.Commands.CreateIncomeTransaction;
using PWSV.Application.Transactions.Commands.CreateTransferTransaction;

namespace PWSV.Api.Demo;

public sealed class DemoDataSeeder(
    IServiceScopeFactory scopeFactory,
    IHttpContextAccessor httpContextAccessor,
    ICryptoService cryptoService,
    IDateTimeProvider clock,
    IOptions<DemoAccountOptions> options,
    ILogger<DemoDataSeeder> logger)
{
    public const string SeedDemoDataKey = "Database:SeedDemoData";
    public const string NoDemoSwitch = "--no-demo";

    public async Task SeedAsync(CancellationToken cancellationToken)
    {
        var demo = options.Value;
        if (!demo.IsConfigured)
        {
            logger.LogWarning("Demo data was not created because the demo account is not configured");
            return;
        }

        var plan = DemoDataPlanBuilder.Build(clock.UtcNow, TimeZoneInfo.Local);
        var stopwatch = Stopwatch.StartNew();
        logger.LogInformation("Creating demo data for user {Username}", demo.Username);

        try
        {
            var auth = await SendAsync(new RegisterCommand(demo.Username, demo.Password, demo.MasterPassword), cancellationToken)
                .ConfigureAwait(false);
            httpContextAccessor.HttpContext = CreateHttpContext(auth.UserId, auth.Username);

            await ApplyAsync(plan, cancellationToken).ConfigureAwait(false);

            logger.LogInformation(
                "Demo data created in {ElapsedMs} ms: {Accounts} accounts, {Categories} categories, {Rates} exchange rates, {Transactions} transactions",
                stopwatch.ElapsedMilliseconds,
                plan.Accounts.Count,
                plan.Categories.Count,
                plan.ExchangeRates.Count,
                plan.Transactions.Count);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "Demo data creation failed");
            if (httpContextAccessor.HttpContext is not null)
            {
                await RemoveDemoAccountAsync(demo, cancellationToken).ConfigureAwait(false);
            }
        }
        finally
        {
            httpContextAccessor.HttpContext = null;
            cryptoService.Reset();
        }
    }

    private async Task ApplyAsync(DemoDataPlan plan, CancellationToken cancellationToken)
    {
        var accountTypes = (await SendAsync(new GetAccountTypesListQuery(), cancellationToken).ConfigureAwait(false))
            .ToDictionary(t => t.Code, t => t.Id);
        var currencies = (await SendAsync(new GetCurrenciesListQuery(), cancellationToken).ConfigureAwait(false))
            .ToDictionary(c => c.Code, c => c.Id);

        var categories = new Dictionary<string, int>();
        foreach (var category in plan.Categories)
        {
            int? parentId = category.ParentName is null ? null : categories[category.ParentName];
            var created = await SendAsync(new CreateCategoryCommand(category.Name, category.Kind, parentId), cancellationToken)
                .ConfigureAwait(false);
            categories[category.Name] = created.Id;
        }

        var accounts = new Dictionary<string, int>();
        foreach (var account in plan.Accounts)
        {
            var command = new CreateAccountCommand(
                account.Name,
                accountTypes[account.AccountTypeCode],
                currencies[account.CurrencyCode],
                account.AccountNumber,
                0m);
            var created = await SendAsync(command, cancellationToken).ConfigureAwait(false);
            accounts[account.Key] = created.Id;
        }

        foreach (var rate in plan.ExchangeRates)
        {
            var command = new CreateExchangeRateCommand(
                currencies[rate.BaseCurrencyCode],
                currencies[rate.QuoteCurrencyCode],
                rate.Rate,
                rate.EffectiveDate);
            await SendAsync(command, cancellationToken).ConfigureAwait(false);
        }

        foreach (var transaction in plan.Transactions)
        {
            await SendTransactionAsync(transaction, accounts, categories, cancellationToken).ConfigureAwait(false);
        }

        foreach (var account in plan.Accounts.Where(a => !a.IsActive))
        {
            await SendAsync(new DeactivateAccountCommand(accounts[account.Key]), cancellationToken).ConfigureAwait(false);
        }

        foreach (var category in plan.Categories.Where(c => !c.IsActive))
        {
            await SendAsync(new DeactivateCategoryCommand(categories[category.Name]), cancellationToken).ConfigureAwait(false);
        }
    }

    private Task SendTransactionAsync(
        DemoTransaction transaction,
        IReadOnlyDictionary<string, int> accounts,
        IReadOnlyDictionary<string, int> categories,
        CancellationToken cancellationToken) => transaction switch
        {
            DemoIncome income => SendAsync(
                new CreateIncomeTransactionCommand(
                    accounts[income.AccountKey],
                    categories[income.CategoryName],
                    income.Amount,
                    income.OccurredAt,
                    income.Description,
                    income.Counterparty),
                cancellationToken),
            DemoExpense expense => SendAsync(
                new CreateExpenseTransactionCommand(
                    accounts[expense.AccountKey],
                    categories[expense.CategoryName],
                    expense.Amount,
                    expense.OccurredAt,
                    expense.Description,
                    expense.Counterparty),
                cancellationToken),
            DemoTransfer transfer => SendAsync(
                new CreateTransferTransactionCommand(
                    accounts[transfer.SourceAccountKey],
                    accounts[transfer.DestinationAccountKey],
                    transfer.Amount,
                    transfer.ExchangeRate,
                    transfer.OccurredAt,
                    transfer.Description),
                cancellationToken),
            _ => throw new ArgumentOutOfRangeException(nameof(transaction), transaction.GetType().Name, "Unknown demo transaction type.")
        };

    private async Task RemoveDemoAccountAsync(DemoAccountOptions demo, CancellationToken cancellationToken)
    {
        try
        {
            await SendAsync(new DeleteUserCommand(demo.Password, demo.MasterPassword), cancellationToken).ConfigureAwait(false);
            logger.LogWarning("The partially created demo account was removed, the database has no users");
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "The partially created demo account could not be removed");
        }
    }

    private async Task<TResponse> SendAsync<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();
        return await sender.Send(request, cancellationToken).ConfigureAwait(false);
    }

    private static DefaultHttpContext CreateHttpContext(int userId, string username) => new()
    {
        User = new ClaimsPrincipal(new ClaimsIdentity(
            [
                new Claim(ClaimTypes.NameIdentifier, userId.ToString(CultureInfo.InvariantCulture)),
                new Claim(ClaimTypes.Name, username)
            ],
            nameof(DemoDataSeeder)))
    };
}
