using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using PWSV.Client.Models;
using PWSV.Client.Services;
using PWSV.Client.Services.Interfaces;

namespace PWSV.Client.ViewModels.Accounts;

public sealed partial class AccountsListViewModel(
    IApiClient api,
    IDialogService dialog,
    IServiceProvider services,
    INavigationService navigation) : ViewModelBase, ILoadableViewModel
{
    public ObservableCollection<AccountModel> Accounts { get; } = [];

    [ObservableProperty]
    private bool _includeInactive;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(EditCommand))]
    [NotifyCanExecuteChangedFor(nameof(ViewDetailsCommand))]
    [NotifyCanExecuteChangedFor(nameof(DeactivateCommand))]
    private AccountModel? _selectedAccount;

    [RelayCommand(CanExecute = nameof(CanExecuteCommands))]
    private async Task LoadAsync(CancellationToken cancellationToken)
    {
        try
        {
            IsBusy = true;
            ClearError();
            var selectedId = SelectedAccount?.Id;
            var items = await api.GetAccountsAsync(IncludeInactive, cancellationToken);

            Accounts.Clear();
            foreach (var item in items)
            {
                Accounts.Add(item);
            }

            SelectedAccount = Accounts.FirstOrDefault(a => a.Id == selectedId);
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
        var editor = services.GetRequiredService<AccountEditViewModel>();
        await editor.InitializeForCreateAsync(cancellationToken);
        if (dialog.ShowDialog(editor))
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

        var editor = services.GetRequiredService<AccountEditViewModel>();
        await editor.InitializeForEditAsync(SelectedAccount.Id, cancellationToken);
        if (dialog.ShowDialog(editor))
        {
            await LoadAsync(cancellationToken);
        }
    }

    [RelayCommand(CanExecute = nameof(CanModifySelected))]
    private void ViewDetails()
    {
        if (SelectedAccount is null)
        {
            return;
        }

        var details = services.GetRequiredService<AccountDetailsViewModel>();
        details.AccountId = SelectedAccount.Id;
        navigation.NavigateContent(details);
    }

    [RelayCommand(CanExecute = nameof(CanDeactivateSelected))]
    private async Task DeactivateAsync(CancellationToken cancellationToken)
    {
        if (SelectedAccount is null)
        {
            return;
        }

        if (!dialog.Confirm("Підтвердження", $"Деактивувати рахунок '{SelectedAccount.Name}'?"))
        {
            return;
        }

        try
        {
            IsBusy = true;
            ClearError();
            await api.DeactivateAccountAsync(SelectedAccount.Id, cancellationToken);
        }
        catch (Exception ex) when (ex is ApiException or HttpRequestException or TaskCanceledException)
        {
            SetErrorFromException(ex);
            return;
        }
        finally
        {
            IsBusy = false;
        }

        await LoadAsync(cancellationToken);
    }

    private bool CanExecuteCommands() => !IsBusy;
    private bool CanModifySelected() => !IsBusy && SelectedAccount is not null;
    private bool CanDeactivateSelected() => CanModifySelected() && SelectedAccount!.IsActive;

    partial void OnIncludeInactiveChanged(bool value) => LoadCommand.Execute(null);

    protected override void OnIsBusyChanged(bool value)
    {
        LoadCommand.NotifyCanExecuteChanged();
        AddCommand.NotifyCanExecuteChanged();
        EditCommand.NotifyCanExecuteChanged();
        ViewDetailsCommand.NotifyCanExecuteChanged();
        DeactivateCommand.NotifyCanExecuteChanged();
    }
}
