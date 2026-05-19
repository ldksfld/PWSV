using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using PWSV.Client.Services.Interfaces;
using PWSV.Client.ViewModels.Accounts;
using PWSV.Client.ViewModels.Categories;
using PWSV.Client.ViewModels.Currencies;
using PWSV.Client.ViewModels.ExchangeRates;
using PWSV.Client.ViewModels.Transactions;

namespace PWSV.Client.ViewModels;

public sealed partial class MainViewModel : ViewModelBase
{
    private readonly INavigationService _navigation;
    private readonly ITokenStorage _tokenStorage;
    private readonly IServiceProvider _services;

    public MainViewModel(INavigationService navigation, ITokenStorage tokenStorage, IServiceProvider services)
    {
        _navigation = navigation;
        _tokenStorage = tokenStorage;
        _services = services;
        _tokenStorage.AuthenticationChanged += () => OnPropertyChanged(nameof(Username));
        _navigation.ContentNavigated += OnContentNavigated;
        ShowAccountsCommand.Execute(null);
    }

    public string Username => _tokenStorage.Username ?? string.Empty;

    public INavigationService Navigation => _navigation;

    private string _activeSection = string.Empty;
    public string ActiveSection
    {
        get => _activeSection;
        private set => SetProperty(ref _activeSection, value);
    }

    private void OnContentNavigated()
    {
        ActiveSection = _navigation.CurrentContent switch
        {
            AccountsListViewModel => "Accounts",
            AccountDetailsViewModel => "Accounts",
            CategoriesViewModel => "Categories",
            CurrenciesViewModel => "Currencies",
            ExchangeRatesViewModel => "ExchangeRates",
            TransactionsListViewModel => "Transactions",
            _ => string.Empty
        };
    }

    [RelayCommand]
    private void ShowAccounts() => _navigation.NavigateContent<AccountsListViewModel>();

    [RelayCommand]
    private void ShowCategories() => _navigation.NavigateContent<CategoriesViewModel>();

    [RelayCommand]
    private void ShowCurrencies() => _navigation.NavigateContent<CurrenciesViewModel>();

    [RelayCommand]
    private void ShowExchangeRates() => _navigation.NavigateContent<ExchangeRatesViewModel>();

    [RelayCommand]
    private void ShowTransactions() => _navigation.NavigateContent<TransactionsListViewModel>();

    [RelayCommand]
    private async Task NewTransactionAsync(CancellationToken cancellationToken)
    {
        _navigation.NavigateContent<TransactionsListViewModel>();
        var vm = _services.GetRequiredService<TransactionsListViewModel>();
        if (vm.AddIncomeCommand.CanExecute(null))
        {
            await vm.AddIncomeCommand.ExecuteAsync(cancellationToken);
        }
    }

    [RelayCommand]
    private async Task RefreshAsync(CancellationToken cancellationToken)
    {
        switch (_navigation.CurrentContent)
        {
            case AccountsListViewModel accounts:
                await accounts.LoadAsync(cancellationToken);
                break;
            case CategoriesViewModel categories:
                await categories.LoadAsync(cancellationToken);
                break;
            case CurrenciesViewModel currencies:
                await currencies.LoadAsync(cancellationToken);
                break;
            case TransactionsListViewModel transactions:
                await transactions.LoadAsync(cancellationToken);
                break;
            case ExchangeRatesViewModel rates:
                await rates.LoadAsync(cancellationToken);
                break;
        }
    }

    [RelayCommand]
    private void Logout()
    {
        _tokenStorage.Clear();
        _navigation.NavigateTo<LoginViewModel>();
    }
}
