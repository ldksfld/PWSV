using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PWSV.Client.Models;
using PWSV.Client.Services;
using PWSV.Client.Services.Interfaces;

namespace PWSV.Client.ViewModels.Accounts;

public sealed partial class AccountDetailsViewModel(IApiClient api, INavigationService navigation) : ViewModelBase
{
    [ObservableProperty]
    private int _accountId;

    [ObservableProperty]
    private AccountModel? _account;

    public ObservableCollection<TransactionModel> RecentTransactions { get; } = [];

    public async Task InitializeAsync(int accountId, CancellationToken cancellationToken)
    {
        AccountId = accountId;
        await LoadAsync(cancellationToken);
    }

    [RelayCommand(CanExecute = nameof(CanExecuteCommands))]
    private async Task LoadAsync(CancellationToken cancellationToken)
    {
        try
        {
            IsBusy = true;
            ClearError();
            var details = await api.GetAccountAsync(AccountId, cancellationToken);
            Account = details.Account;
            RecentTransactions.Clear();
            foreach (var tx in details.RecentTransactions)
            {
                RecentTransactions.Add(tx);
            }
        }
        catch (Exception ex) when (ex is ApiException or HttpRequestException or TaskCanceledException)
        {
            SetErrorFromException(ex);
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private void Back() => navigation.NavigateContent<AccountsListViewModel>();

    private bool CanExecuteCommands() => !IsBusy;

    protected override void OnIsBusyChanged(bool value) => LoadCommand.NotifyCanExecuteChanged();
}
