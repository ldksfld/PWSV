using System.Collections.ObjectModel;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using PWSV.Client.Models;
using PWSV.Client.Services;
using PWSV.Client.Services.Interfaces;
using PWSV.Client.Views.Transactions;

namespace PWSV.Client.ViewModels.Transactions;

public sealed partial class TransactionsListViewModel : ViewModelBase
{
    private readonly IApiClient _api;
    private readonly IDialogService _dialog;
    private readonly IServiceProvider _services;

    public TransactionsListViewModel(IApiClient api, IDialogService dialog, IServiceProvider services)
    {
        _api = api;
        _dialog = dialog;
        _services = services;
        _ = LoadAsync(CancellationToken.None);
    }

    public ObservableCollection<TransactionModel> Transactions { get; } = [];

    [ObservableProperty]
    private TransactionModel? _selectedTransaction;

    [ObservableProperty]
    private int _page = 1;

    [ObservableProperty]
    private int _pageSize = 50;

    [ObservableProperty]
    private int _totalCount;

    [ObservableProperty]
    private string? _filterKind;

    [RelayCommand(CanExecute = nameof(CanExecuteCommands))]
    public async Task LoadAsync(CancellationToken cancellationToken)
    {
        try
        {
            IsBusy = true;
            ClearError();
            var result = await _api.GetTransactionsAsync(null, null, FilterKind, null, null, Page, PageSize, cancellationToken);
            TotalCount = result.TotalCount;
            Transactions.Clear();
            foreach (var item in result.Items)
            {
                Transactions.Add(item);
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

    [RelayCommand(CanExecute = nameof(CanExecuteCommands))]
    private Task AddIncomeAsync(CancellationToken cancellationToken)
        => OpenAddAsync(TransactionEditMode.Income, cancellationToken);

    [RelayCommand(CanExecute = nameof(CanExecuteCommands))]
    private Task AddExpenseAsync(CancellationToken cancellationToken)
        => OpenAddAsync(TransactionEditMode.Expense, cancellationToken);

    [RelayCommand(CanExecute = nameof(CanExecuteCommands))]
    private Task AddTransferAsync(CancellationToken cancellationToken)
        => OpenAddAsync(TransactionEditMode.Transfer, cancellationToken);

    private async Task OpenAddAsync(TransactionEditMode mode, CancellationToken cancellationToken)
    {
        var vm = _services.GetRequiredService<TransactionEditViewModel>();
        await vm.InitializeAsync(mode, cancellationToken);
        var dialog = new TransactionEditDialog(vm) { Owner = Application.Current.MainWindow };
        if (dialog.ShowDialog() == true)
        {
            await LoadAsync(cancellationToken);
        }
    }

    [RelayCommand(CanExecute = nameof(CanModifySelected))]
    private async Task DeleteAsync(CancellationToken cancellationToken)
    {
        if (SelectedTransaction is null)
        {
            return;
        }

        if (!_dialog.Confirm("Підтвердження", "Видалити транзакцію?"))
        {
            return;
        }

        try
        {
            IsBusy = true;
            ClearError();
            await _api.DeleteTransactionAsync(SelectedTransaction.Id, cancellationToken);
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
    private bool CanModifySelected() => !IsBusy && SelectedTransaction is not null;

    protected override void OnIsBusyChanged(bool value)
    {
        LoadCommand.NotifyCanExecuteChanged();
        AddIncomeCommand.NotifyCanExecuteChanged();
        AddExpenseCommand.NotifyCanExecuteChanged();
        AddTransferCommand.NotifyCanExecuteChanged();
        DeleteCommand.NotifyCanExecuteChanged();
    }

    partial void OnSelectedTransactionChanged(TransactionModel? value)
    {
        DeleteCommand.NotifyCanExecuteChanged();
    }
}
