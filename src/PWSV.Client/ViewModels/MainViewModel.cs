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
    private readonly IDialogService _dialog;

    public MainViewModel(INavigationService navigation, ITokenStorage tokenStorage, IServiceProvider services, IDialogService dialog)
    {
        _navigation = navigation;
        _tokenStorage = tokenStorage;
        _services = services;
        _dialog = dialog;
        _tokenStorage.AuthenticationChanged += OnAuthenticationChanged;
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

    private void OnAuthenticationChanged()
    {
        OnPropertyChanged(nameof(Username));
        if (_tokenStorage.IsAuthenticated)
        {
            _navigation.NavigateContent<AccountsListViewModel>();
        }
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
    private async Task NewTransactionAsync()
    {
        _navigation.NavigateContent<TransactionsListViewModel>();
        var transactions = _services.GetRequiredService<TransactionsListViewModel>();
        if (transactions.LoadCommand.ExecutionTask is { } loading)
        {
            await loading;
        }

        if (transactions.AddIncomeCommand.CanExecute(null))
        {
            await transactions.AddIncomeCommand.ExecuteAsync(null);
        }
    }

    [RelayCommand]
    private async Task RefreshAsync()
    {
        if (_navigation.CurrentContent is ILoadableViewModel loadable)
        {
            await loadable.LoadCommand.ExecuteAsync(null);
        }
    }

    [RelayCommand]
    private void ChangePassword()
    {
        var changePassword = _services.GetRequiredService<ChangePasswordViewModel>();
        if (_dialog.ShowDialog(changePassword))
        {
            _dialog.ShowInfo("Зміна пароля", "Пароль успішно змінено.");
        }
    }

    [RelayCommand]
    private void DeleteUser()
    {
        var deleteUser = _services.GetRequiredService<DeleteUserViewModel>();
        if (!_dialog.ShowDialog(deleteUser))
        {
            return;
        }

        _navigation.NavigateTo<RegisterViewModel>();
        _tokenStorage.Clear();
        _dialog.ShowInfo(
            "Обліковий запис видалено",
            "Обліковий запис і всі його дані видалено. Тепер можна зареєструвати новий обліковий запис.");
    }

    [RelayCommand]
    private void Logout()
    {
        _tokenStorage.Clear();
        _navigation.NavigateTo<LoginViewModel>();
    }
}
