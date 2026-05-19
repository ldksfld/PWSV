using System.Collections.ObjectModel;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using PWSV.Client.Models;
using PWSV.Client.Services;
using PWSV.Client.Services.Interfaces;
using PWSV.Client.Views.Accounts;

namespace PWSV.Client.ViewModels.Accounts;

public sealed partial class AccountsListViewModel : ViewModelBase
{
    private readonly IApiClient _api;
    private readonly IDialogService _dialog;
    private readonly IServiceProvider _services;
    private readonly INavigationService _navigation;
    private CancellationTokenSource? _loadCts;

    public AccountsListViewModel(IApiClient api, IDialogService dialog, IServiceProvider services, INavigationService navigation)
    {
        _api = api;
        _dialog = dialog;
        _services = services;
        _navigation = navigation;
        _ = LoadAsync(CancellationToken.None);
    }

    public ObservableCollection<AccountModel> Accounts { get; } = [];

    [ObservableProperty]
    private bool _includeInactive;

    [ObservableProperty]
    private AccountModel? _selectedAccount;

    [RelayCommand(CanExecute = nameof(CanExecuteCommands))]
    public async Task LoadAsync(CancellationToken cancellationToken)
    {
        _loadCts?.Cancel();
        _loadCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var token = _loadCts.Token;

        try
        {
            IsBusy = true;
            ClearError();
            var items = await _api.GetAccountsAsync(IncludeInactive, token);
            if (token.IsCancellationRequested)
            {
                return;
            }

            Accounts.Clear();
            foreach (var item in items)
            {
                Accounts.Add(item);
            }
        }
        catch (OperationCanceledException)
        {
            return;
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

    [RelayCommand(CanExecute = nameof(CanExecuteCommands))]
    private async Task AddAsync(CancellationToken cancellationToken)
    {
        var vm = _services.GetRequiredService<AccountEditViewModel>();
        await vm.InitializeForCreateAsync(cancellationToken);
        var dialog = new AccountEditDialog(vm) { Owner = Application.Current.MainWindow };
        if (dialog.ShowDialog() == true)
        {
            await LoadAsync(cancellationToken);
        }
    }

    [RelayCommand(CanExecute = nameof(CanModifySelected))]
    private async Task EditAsync(CancellationToken cancellationToken)
    {
        if (SelectedAccount is null)
        {
            return;
        }

        var vm = _services.GetRequiredService<AccountEditViewModel>();
        await vm.InitializeForEditAsync(SelectedAccount, cancellationToken);
        var dialog = new AccountEditDialog(vm) { Owner = Application.Current.MainWindow };
        if (dialog.ShowDialog() == true)
        {
            await LoadAsync(cancellationToken);
        }
    }

    [RelayCommand(CanExecute = nameof(CanModifySelected))]
    private async Task ViewDetailsAsync(CancellationToken cancellationToken)
    {
        if (SelectedAccount is null)
        {
            return;
        }

        var vm = _services.GetRequiredService<AccountDetailsViewModel>();
        await vm.InitializeAsync(SelectedAccount.Id, cancellationToken);
        _navigation.NavigateContent(vm);
    }

    [RelayCommand(CanExecute = nameof(CanModifySelected))]
    private async Task DeactivateAsync(CancellationToken cancellationToken)
    {
        if (SelectedAccount is null)
        {
            return;
        }

        if (!_dialog.Confirm("Підтвердження", $"Деактивувати рахунок '{SelectedAccount.Name}'?"))
        {
            return;
        }

        try
        {
            IsBusy = true;
            ClearError();
            await _api.DeactivateAccountAsync(SelectedAccount.Id, cancellationToken);
            await LoadAsync(cancellationToken);
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

    private bool CanExecuteCommands() => !IsBusy;
    private bool CanModifySelected() => !IsBusy && SelectedAccount is not null;

    partial void OnIncludeInactiveChanged(bool value)
    {
        _ = LoadAsync(CancellationToken.None);
    }

    partial void OnSelectedAccountChanged(AccountModel? value)
    {
        EditCommand.NotifyCanExecuteChanged();
        ViewDetailsCommand.NotifyCanExecuteChanged();
        DeactivateCommand.NotifyCanExecuteChanged();
    }

    protected override void OnIsBusyChanged(bool value)
    {
        LoadCommand.NotifyCanExecuteChanged();
        AddCommand.NotifyCanExecuteChanged();
        EditCommand.NotifyCanExecuteChanged();
        ViewDetailsCommand.NotifyCanExecuteChanged();
        DeactivateCommand.NotifyCanExecuteChanged();
    }
}
