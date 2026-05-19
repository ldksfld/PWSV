using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PWSV.Client.Models;
using PWSV.Client.Services;
using PWSV.Client.Services.Interfaces;

namespace PWSV.Client.ViewModels.Transactions;

public enum TransactionEditMode
{
    Income,
    Expense,
    Transfer
}

public sealed partial class TransactionEditViewModel(IApiClient api) : ViewModelBase
{
    public ObservableCollection<AccountModel> Accounts { get; } = [];
    public ObservableCollection<CategoryModel> Categories { get; } = [];

    [ObservableProperty]
    private TransactionEditMode _mode;

    [ObservableProperty]
    private AccountModel? _selectedAccount;

    [ObservableProperty]
    private AccountModel? _selectedDestinationAccount;

    [ObservableProperty]
    private CategoryModel? _selectedCategory;

    [ObservableProperty]
    private string _amountInput = string.Empty;

    [ObservableProperty]
    private string _exchangeRateInput = string.Empty;

    [ObservableProperty]
    private DateTime _occurredAt = DateTime.Now;

    [ObservableProperty]
    [MaxLength(512)]
    private string? _description;

    [ObservableProperty]
    [MaxLength(256)]
    private string? _counterparty;

    public bool IsIncome => Mode == TransactionEditMode.Income;
    public bool IsExpense => Mode == TransactionEditMode.Expense;
    public bool IsTransfer => Mode == TransactionEditMode.Transfer;
    public bool RequiresCategory => Mode != TransactionEditMode.Transfer;
    public bool RequiresExchangeRate => IsTransfer
        && SelectedAccount is not null && SelectedDestinationAccount is not null
        && SelectedAccount.CurrencyId != SelectedDestinationAccount.CurrencyId;

    public string Title => Mode switch
    {
        TransactionEditMode.Income => "Нова транзакція — дохід",
        TransactionEditMode.Expense => "Нова транзакція — витрата",
        TransactionEditMode.Transfer => "Нова транзакція — переказ",
        _ => "Транзакція"
    };

    public Action? Saved { get; set; }

    public async Task InitializeAsync(TransactionEditMode mode, CancellationToken cancellationToken)
    {
        Mode = mode;
        try
        {
            IsBusy = true;
            ClearError();
            var accounts = await api.GetAccountsAsync(false, cancellationToken);
            Accounts.Clear();
            foreach (var a in accounts)
            {
                Accounts.Add(a);
            }

            SelectedAccount = Accounts.FirstOrDefault();
            SelectedDestinationAccount = Accounts.Skip(1).FirstOrDefault();

            if (RequiresCategory)
            {
                var kind = mode == TransactionEditMode.Income ? "Income" : "Expense";
                var categories = await api.GetCategoriesAsync(kind, false, cancellationToken);
                Categories.Clear();
                foreach (var c in categories)
                {
                    Categories.Add(c);
                }

                SelectedCategory = Categories.FirstOrDefault();
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

        OnPropertyChanged(nameof(IsIncome));
        OnPropertyChanged(nameof(IsExpense));
        OnPropertyChanged(nameof(IsTransfer));
        OnPropertyChanged(nameof(RequiresCategory));
        OnPropertyChanged(nameof(RequiresExchangeRate));
        OnPropertyChanged(nameof(Title));
    }

    [RelayCommand(CanExecute = nameof(CanExecuteSave))]
    private async Task SaveAsync(CancellationToken cancellationToken)
    {
        ClearError();
        ValidateAllProperties();

        if (SelectedAccount is null)
        {
            ErrorMessage = "Виберіть рахунок.";
            return;
        }

        if (!DecimalInput.TryParse(AmountInput, out var amount) || amount <= 0)
        {
            ErrorMessage = "Сума повинна бути більше нуля.";
            return;
        }

        decimal? exchangeRate = null;
        if (!string.IsNullOrWhiteSpace(ExchangeRateInput))
        {
            if (!DecimalInput.TryParse(ExchangeRateInput, out var parsedRate))
            {
                ErrorMessage = "Курс конверсії має бути числом.";
                return;
            }

            exchangeRate = parsedRate;
        }

        if (OccurredAt > DateTime.Now.AddMinutes(1))
        {
            ErrorMessage = "Дата не повинна перевищувати поточний момент.";
            return;
        }

        try
        {
            IsBusy = true;
            var utc = OccurredAt.ToUniversalTime();
            switch (Mode)
            {
                case TransactionEditMode.Income:
                    if (SelectedCategory is null)
                    {
                        ErrorMessage = "Виберіть категорію.";
                        return;
                    }

                    await api.CreateIncomeAsync(
                        SelectedAccount.Id,
                        SelectedCategory.Id,
                        amount,
                        utc,
                        Description,
                        Counterparty,
                        cancellationToken);
                    break;
                case TransactionEditMode.Expense:
                    if (SelectedCategory is null)
                    {
                        ErrorMessage = "Виберіть категорію.";
                        return;
                    }

                    await api.CreateExpenseAsync(
                        SelectedAccount.Id,
                        SelectedCategory.Id,
                        amount,
                        utc,
                        Description,
                        Counterparty,
                        cancellationToken);
                    break;
                case TransactionEditMode.Transfer:
                    if (SelectedDestinationAccount is null)
                    {
                        ErrorMessage = "Виберіть рахунок-отримувач.";
                        return;
                    }

                    if (SelectedDestinationAccount.Id == SelectedAccount.Id)
                    {
                        ErrorMessage = "Рахунки повинні відрізнятись.";
                        return;
                    }

                    if (RequiresExchangeRate && (exchangeRate is null || exchangeRate.Value <= 0))
                    {
                        ErrorMessage = "Вкажіть курс конверсії.";
                        return;
                    }

                    await api.CreateTransferAsync(
                        SelectedAccount.Id,
                        SelectedDestinationAccount.Id,
                        amount,
                        exchangeRate,
                        utc,
                        Description,
                        cancellationToken);
                    break;
            }

            Saved?.Invoke();
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

    private bool CanExecuteSave() => !IsBusy;

    protected override void OnIsBusyChanged(bool value) => SaveCommand.NotifyCanExecuteChanged();

    partial void OnSelectedAccountChanged(AccountModel? value) => OnPropertyChanged(nameof(RequiresExchangeRate));
    partial void OnSelectedDestinationAccountChanged(AccountModel? value) => OnPropertyChanged(nameof(RequiresExchangeRate));
}
