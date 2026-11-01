using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using PWSV.Client.Models;
using PWSV.Client.Services;
using PWSV.Client.Services.Interfaces;

namespace PWSV.Client.ViewModels.Transactions;

public sealed partial class TransactionsListViewModel(
    IApiClient api,
    IDialogService dialog,
    IServiceProvider services) : ViewModelBase, ILoadableViewModel
{
    private const int PageSize = 10;

    private static readonly OptionModel AllAccounts = new(null, "Усі рахунки");
    private static readonly OptionModel AllCategories = new(null, "Усі категорії");
    private static readonly KindOptionModel[] AllKinds =
    [
        new(null, "Усі типи"),
        new("Income", "Дохід"),
        new("Expense", "Витрата"),
        new("Transfer", "Переказ")
    ];

    private TransactionFilterModel _appliedFilter = TransactionFilterModel.Empty;

    public ObservableCollection<TransactionModel> Transactions { get; } = [];
    public ObservableCollection<OptionModel> AccountOptions { get; } = [AllAccounts];
    public ObservableCollection<OptionModel> CategoryOptions { get; } = [AllCategories];
    public IReadOnlyList<KindOptionModel> KindOptions => AllKinds;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(EditCommand))]
    [NotifyCanExecuteChangedFor(nameof(DeleteCommand))]
    private TransactionModel? _selectedTransaction;

    [ObservableProperty]
    private DateTime? _filterFrom;

    [ObservableProperty]
    private DateTime? _filterTo;

    [ObservableProperty]
    private OptionModel? _selectedAccountOption = AllAccounts;

    [ObservableProperty]
    private OptionModel? _selectedCategoryOption = AllCategories;

    [ObservableProperty]
    private KindOptionModel? _selectedKindOption = AllKinds[0];

    [ObservableProperty]
    private bool _isFilterApplied;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PageDisplay))]
    [NotifyCanExecuteChangedFor(nameof(PreviousPageCommand))]
    [NotifyCanExecuteChangedFor(nameof(NextPageCommand))]
    private int _pageNumber = 1;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PageDisplay))]
    [NotifyCanExecuteChangedFor(nameof(PreviousPageCommand))]
    [NotifyCanExecuteChangedFor(nameof(NextPageCommand))]
    private int _totalPages;

    [ObservableProperty]
    private int _totalCount;

    public string PageDisplay => $"Сторінка {PageNumber} з {Math.Max(TotalPages, 1)}";

    [RelayCommand(CanExecute = nameof(CanExecuteCommands))]
    private async Task LoadAsync(CancellationToken cancellationToken)
    {
        try
        {
            IsBusy = true;
            ClearError();
            await LoadFilterOptionsAsync(cancellationToken);

            var page = await FetchPageAsync(cancellationToken);
            if (page.Items.Count == 0 && page.TotalPages > 0 && PageNumber > page.TotalPages)
            {
                PageNumber = page.TotalPages;
                page = await FetchPageAsync(cancellationToken);
            }

            var selectedId = SelectedTransaction?.Id;
            Transactions.Clear();
            foreach (var item in page.Items)
            {
                Transactions.Add(item);
            }

            TotalCount = page.TotalCount;
            TotalPages = page.TotalPages;
            SelectedTransaction = Transactions.FirstOrDefault(t => t.Id == selectedId);
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
    private async Task ApplyFiltersAsync(CancellationToken cancellationToken)
    {
        if (FilterFrom is { } from && FilterTo is { } to && from.Date > to.Date)
        {
            ErrorMessage = "Дата \"від\" не може бути пізніше дати \"до\".";
            return;
        }

        _appliedFilter = new TransactionFilterModel(
            SelectedAccountOption?.Id,
            SelectedCategoryOption?.Id,
            SelectedKindOption?.Kind,
            FilterFrom?.Date,
            FilterTo?.Date.AddDays(1).AddTicks(-1));
        IsFilterApplied = _appliedFilter != TransactionFilterModel.Empty;
        PageNumber = 1;
        await LoadAsync(cancellationToken);
    }

    [RelayCommand(CanExecute = nameof(CanExecuteCommands))]
    private async Task ResetFiltersAsync(CancellationToken cancellationToken)
    {
        FilterFrom = null;
        FilterTo = null;
        SelectedAccountOption = AllAccounts;
        SelectedCategoryOption = AllCategories;
        SelectedKindOption = AllKinds[0];
        await ApplyFiltersAsync(cancellationToken);
    }

    [RelayCommand(CanExecute = nameof(CanGoToPreviousPage))]
    private async Task PreviousPageAsync(CancellationToken cancellationToken)
    {
        PageNumber--;
        await LoadAsync(cancellationToken);
    }

    [RelayCommand(CanExecute = nameof(CanGoToNextPage))]
    private async Task NextPageAsync(CancellationToken cancellationToken)
    {
        PageNumber++;
        await LoadAsync(cancellationToken);
    }

    [RelayCommand(CanExecute = nameof(CanExecuteCommands))]
    private Task AddIncomeAsync(CancellationToken cancellationToken)
        => OpenCreateAsync(TransactionEditMode.Income, cancellationToken);

    [RelayCommand(CanExecute = nameof(CanExecuteCommands))]
    private Task AddExpenseAsync(CancellationToken cancellationToken)
        => OpenCreateAsync(TransactionEditMode.Expense, cancellationToken);

    [RelayCommand(CanExecute = nameof(CanExecuteCommands))]
    private Task AddTransferAsync(CancellationToken cancellationToken)
        => OpenCreateAsync(TransactionEditMode.Transfer, cancellationToken);

    [RelayCommand(CanExecute = nameof(CanEditSelected))]
    private async Task EditAsync(CancellationToken cancellationToken)
    {
        if (SelectedTransaction is null)
        {
            return;
        }

        var editor = services.GetRequiredService<TransactionEditViewModel>();
        await editor.InitializeForEditAsync(SelectedTransaction, cancellationToken);
        if (dialog.ShowDialog(editor))
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

        var message = SelectedTransaction.Kind == "Transfer"
            ? "Видалити переказ? Буде видалено обидві частини операції, баланси обох рахунків буде відновлено."
            : "Видалити транзакцію?";
        if (!dialog.Confirm("Підтвердження", message))
        {
            return;
        }

        try
        {
            IsBusy = true;
            ClearError();
            await api.DeleteTransactionAsync(SelectedTransaction.Id, cancellationToken);
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

    private async Task OpenCreateAsync(TransactionEditMode mode, CancellationToken cancellationToken)
    {
        var editor = services.GetRequiredService<TransactionEditViewModel>();
        await editor.InitializeAsync(mode, cancellationToken);
        if (dialog.ShowDialog(editor))
        {
            await LoadAsync(cancellationToken);
        }
    }

    private Task<PagedResultModel<TransactionModel>> FetchPageAsync(CancellationToken cancellationToken) =>
        api.GetTransactionsAsync(
            _appliedFilter.AccountId,
            _appliedFilter.CategoryId,
            _appliedFilter.Kind,
            _appliedFilter.From,
            _appliedFilter.To,
            PageNumber,
            PageSize,
            cancellationToken);

    private async Task LoadFilterOptionsAsync(CancellationToken cancellationToken)
    {
        var selectedAccountId = SelectedAccountOption?.Id;
        var selectedCategoryId = SelectedCategoryOption?.Id;

        var accounts = await api.GetAccountsAsync(true, cancellationToken);
        AccountOptions.Clear();
        AccountOptions.Add(AllAccounts);
        foreach (var account in accounts)
        {
            AccountOptions.Add(new OptionModel(account.Id, account.IsActive ? account.DisplayName : $"{account.DisplayName} (неактивний)"));
        }

        var categories = await api.GetCategoriesAsync(null, true, cancellationToken);
        CategoryOptions.Clear();
        CategoryOptions.Add(AllCategories);
        AddCategoryOptions(categories, "Income", "Дохід");
        AddCategoryOptions(categories, "Expense", "Витрата");

        SelectedAccountOption = AccountOptions.FirstOrDefault(o => o.Id == selectedAccountId) ?? AllAccounts;
        SelectedCategoryOption = CategoryOptions.FirstOrDefault(o => o.Id == selectedCategoryId) ?? AllCategories;
    }

    private void AddCategoryOptions(IReadOnlyList<CategoryModel> categories, string kind, string kindDisplay)
    {
        var ofKind = categories.Where(c => c.Kind == kind).ToList();
        foreach (var option in CategoryPaths.BuildOptions(ofKind, _ => true))
        {
            CategoryOptions.Add(option with { DisplayName = $"{kindDisplay}: {option.DisplayName}" });
        }
    }

    private bool CanExecuteCommands() => !IsBusy;
    private bool CanModifySelected() => !IsBusy && SelectedTransaction is not null;
    private bool CanEditSelected() => CanModifySelected() && SelectedTransaction!.Kind != "Transfer";
    private bool CanGoToPreviousPage() => !IsBusy && PageNumber > 1;
    private bool CanGoToNextPage() => !IsBusy && PageNumber < TotalPages;

    protected override void OnIsBusyChanged(bool value)
    {
        LoadCommand.NotifyCanExecuteChanged();
        ApplyFiltersCommand.NotifyCanExecuteChanged();
        ResetFiltersCommand.NotifyCanExecuteChanged();
        PreviousPageCommand.NotifyCanExecuteChanged();
        NextPageCommand.NotifyCanExecuteChanged();
        AddIncomeCommand.NotifyCanExecuteChanged();
        AddExpenseCommand.NotifyCanExecuteChanged();
        AddTransferCommand.NotifyCanExecuteChanged();
        EditCommand.NotifyCanExecuteChanged();
        DeleteCommand.NotifyCanExecuteChanged();
    }
}
