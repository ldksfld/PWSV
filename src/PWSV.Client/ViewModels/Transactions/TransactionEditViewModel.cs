using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
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
    private const int MaxRateDecimalPlaces = 8;
    private const decimal MaxRate = 9_999_999_999.99999999m;
    private static readonly string[] TimeFormats = ["h\\:mm", "hh\\:mm"];

    private string _prefilledRateInput = string.Empty;

    public ObservableCollection<AccountModel> Accounts { get; } = [];
    public ObservableCollection<OptionModel> Categories { get; } = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsTransfer))]
    [NotifyPropertyChangedFor(nameof(RequiresCategory))]
    [NotifyPropertyChangedFor(nameof(RequiresExchangeRate))]
    [NotifyPropertyChangedFor(nameof(Title))]
    private TransactionEditMode _mode;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsEditing))]
    [NotifyPropertyChangedFor(nameof(Title))]
    private long? _editingId;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    private bool _isLoaded;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(RequiresExchangeRate))]
    [NotifyPropertyChangedFor(nameof(ConvertedAmountPreview))]
    private AccountModel? _selectedAccount;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(RequiresExchangeRate))]
    [NotifyPropertyChangedFor(nameof(ConvertedAmountPreview))]
    private AccountModel? _selectedDestinationAccount;

    [ObservableProperty]
    private OptionModel? _selectedCategory;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ConvertedAmountPreview))]
    private string _amountInput = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ConvertedAmountPreview))]
    private string _exchangeRateInput = string.Empty;

    [ObservableProperty]
    private string? _exchangeRateHint;

    [ObservableProperty]
    private DateTime? _occurredDate = DateTime.Today;

    [ObservableProperty]
    private string _occurredTimeInput = DateTime.Now.ToString("HH:mm", CultureInfo.InvariantCulture);

    [ObservableProperty]
    [NotifyDataErrorInfo]
    [MaxLength(512, ErrorMessage = "Опис має містити не більше 512 символів.")]
    private string? _description;

    [ObservableProperty]
    [NotifyDataErrorInfo]
    [MaxLength(256, ErrorMessage = "Контрагент має містити не більше 256 символів.")]
    private string? _counterparty;

    public bool IsTransfer => Mode == TransactionEditMode.Transfer;
    public bool IsEditing => EditingId is not null;
    public bool RequiresCategory => Mode != TransactionEditMode.Transfer;

    public bool RequiresExchangeRate => IsTransfer
        && SelectedAccount is not null && SelectedDestinationAccount is not null
        && SelectedAccount.CurrencyId != SelectedDestinationAccount.CurrencyId;

    public string Title => (IsEditing, Mode) switch
    {
        (true, TransactionEditMode.Income) => "Редагування транзакції — дохід",
        (true, TransactionEditMode.Expense) => "Редагування транзакції — витрата",
        (_, TransactionEditMode.Income) => "Нова транзакція — дохід",
        (_, TransactionEditMode.Expense) => "Нова транзакція — витрата",
        _ => "Нова транзакція — переказ"
    };

    public string? ConvertedAmountPreview
    {
        get
        {
            if (!RequiresExchangeRate
                || !DecimalInput.TryParse(AmountInput, out var amount)
                || !DecimalInput.TryParse(ExchangeRateInput, out var rate)
                || amount <= 0m || rate <= 0m)
            {
                return null;
            }

            var converted = ConvertAmount(amount, rate);
            var format = "N" + CurrencyDecimalPlaces(SelectedDestinationAccount);
            return $"Буде зараховано: {converted.ToString(format, CultureInfo.CurrentCulture)} {SelectedDestinationAccount!.CurrencyCode}";
        }
    }

    public Action? Saved { get; set; }

    public async Task InitializeAsync(TransactionEditMode mode, CancellationToken cancellationToken)
    {
        Mode = mode;
        EditingId = null;
        try
        {
            IsBusy = true;
            ClearError();
            var accounts = await api.GetAccountsAsync(false, cancellationToken);
            FillAccounts(accounts);
            SelectedAccount = Accounts.FirstOrDefault();
            SelectedDestinationAccount = Accounts.Skip(1).FirstOrDefault();

            if (RequiresCategory)
            {
                var categories = await api.GetCategoriesAsync(KindCode(mode), false, cancellationToken);
                FillCategories(CategoryPaths.BuildOptions(categories, _ => true));
                SelectedCategory = Categories.FirstOrDefault();
            }

            IsLoaded = true;
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

    public async Task InitializeForEditAsync(TransactionModel transaction, CancellationToken cancellationToken)
    {
        Mode = transaction.Kind == "Income" ? TransactionEditMode.Income : TransactionEditMode.Expense;
        EditingId = transaction.Id;
        AmountInput = DecimalInput.Format(transaction.Amount);
        OccurredDate = transaction.OccurredAt.Date;
        OccurredTimeInput = transaction.OccurredAt.ToString("HH:mm", CultureInfo.InvariantCulture);
        Description = transaction.Description;
        Counterparty = transaction.Counterparty;

        try
        {
            IsBusy = true;
            ClearError();
            var allAccounts = await api.GetAccountsAsync(true, cancellationToken);
            var original = allAccounts.FirstOrDefault(a => a.Id == transaction.AccountId);
            if (original is null || !original.IsActive)
            {
                ErrorMessage = "Рахунок цієї транзакції деактивовано, тому її не можна редагувати.";
                return;
            }

            FillAccounts(allAccounts.Where(a => a.IsActive && a.CurrencyId == original.CurrencyId));
            SelectedAccount = Accounts.FirstOrDefault(a => a.Id == original.Id);

            var categories = await api.GetCategoriesAsync(KindCode(Mode), true, cancellationToken);
            FillCategories(CategoryPaths.BuildOptions(categories, c => c.IsActive || c.Id == transaction.CategoryId));
            SelectedCategory = Categories.FirstOrDefault(c => c.Id == transaction.CategoryId);

            IsLoaded = true;
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
    private async Task PrefillExchangeRateAsync(CancellationToken cancellationToken)
    {
        ExchangeRateHint = null;
        if (!RequiresExchangeRate)
        {
            return;
        }

        var source = SelectedAccount!;
        var destination = SelectedDestinationAccount!;
        var day = DateOnly.FromDateTime(OccurredDate ?? DateTime.Today);
        try
        {
            var direct = await api.GetExchangeRatesAsync(source.CurrencyId, destination.CurrencyId, null, day, cancellationToken);
            var inverse = await api.GetExchangeRatesAsync(destination.CurrencyId, source.CurrencyId, null, day, cancellationToken);
            if (source != SelectedAccount || destination != SelectedDestinationAccount || day != DateOnly.FromDateTime(OccurredDate ?? DateTime.Today))
            {
                return;
            }

            var inverseRate = inverse.Count > 0
                ? decimal.Round(1m / inverse[0].Rate, MaxRateDecimalPlaces, MidpointRounding.AwayFromZero)
                : 0m;
            var useInverse = inverseRate > 0m && (direct.Count == 0 || inverse[0].EffectiveDate > direct[0].EffectiveDate);

            if (useInverse)
            {
                ApplyPrefilledRate(inverseRate, $"Розраховано з курсу {destination.CurrencyCode}/{source.CurrencyCode} від {inverse[0].EffectiveDate:dd.MM.yyyy}.");
            }
            else if (direct.Count > 0)
            {
                ApplyPrefilledRate(direct[0].Rate, $"Курс {source.CurrencyCode}/{destination.CurrencyCode} від {direct[0].EffectiveDate:dd.MM.yyyy}.");
            }
            else
            {
                ExchangeRateInput = string.Empty;
                _prefilledRateInput = string.Empty;
                ExchangeRateHint = $"Курс для цієї пари валют на {day:dd.MM.yyyy} не знайдено — введіть його вручну.";
            }
        }
        catch (Exception ex) when (ex is ApiException or HttpRequestException or TaskCanceledException)
        {
            ExchangeRateHint = "Не вдалося отримати курс — введіть його вручну.";
        }
    }

    [RelayCommand(CanExecute = nameof(CanExecuteSave))]
    private async Task SaveAsync(CancellationToken cancellationToken)
    {
        ClearError();
        if (!TryValidate())
        {
            return;
        }

        if (SelectedAccount is null)
        {
            ErrorMessage = "Виберіть рахунок.";
            return;
        }

        if (!DecimalInput.TryParse(AmountInput, out var amount))
        {
            ErrorMessage = "Сума має бути числом.";
            return;
        }

        var amountError = DecimalInput.ValidateAmount(amount, CurrencyDecimalPlaces(SelectedAccount), allowZero: false);
        if (amountError is not null)
        {
            ErrorMessage = amountError;
            return;
        }

        if (!TryGetOccurredAt(out var occurredAt))
        {
            return;
        }

        if (RequiresCategory && SelectedCategory?.Id is null)
        {
            ErrorMessage = "Виберіть категорію.";
            return;
        }

        decimal? exchangeRate = null;
        if (IsTransfer)
        {
            if (SelectedDestinationAccount is null)
            {
                ErrorMessage = "Виберіть рахунок-отримувач.";
                return;
            }

            if (SelectedDestinationAccount.Id == SelectedAccount.Id)
            {
                ErrorMessage = "Рахунок-джерело та рахунок-отримувач повинні відрізнятися.";
                return;
            }

            if (RequiresExchangeRate)
            {
                if (!DecimalInput.TryParse(ExchangeRateInput, out var rate) || rate <= 0m)
                {
                    ErrorMessage = "Вкажіть додатний курс конверсії.";
                    return;
                }

                if (rate > MaxRate || decimal.Round(rate, MaxRateDecimalPlaces) != rate)
                {
                    ErrorMessage = "Курс може містити не більше 8 знаків після коми та не перевищувати 9 999 999 999,99999999.";
                    return;
                }

                if (ConvertAmount(amount, rate) <= 0m)
                {
                    ErrorMessage = "Сума зарахування після конвертації менша за мінімальну одиницю валюти отримувача.";
                    return;
                }

                exchangeRate = rate;
            }
        }

        try
        {
            IsBusy = true;
            var description = string.IsNullOrWhiteSpace(Description) ? null : Description.Trim();
            var counterparty = string.IsNullOrWhiteSpace(Counterparty) ? null : Counterparty.Trim();

            if (EditingId is { } id)
            {
                await api.UpdateTransactionAsync(id, SelectedAccount.Id, SelectedCategory!.Id, amount, occurredAt, description, counterparty, cancellationToken);
            }
            else if (Mode == TransactionEditMode.Income)
            {
                await api.CreateIncomeAsync(SelectedAccount.Id, SelectedCategory!.Id!.Value, amount, occurredAt, description, counterparty, cancellationToken);
            }
            else if (Mode == TransactionEditMode.Expense)
            {
                await api.CreateExpenseAsync(SelectedAccount.Id, SelectedCategory!.Id!.Value, amount, occurredAt, description, counterparty, cancellationToken);
            }
            else
            {
                await api.CreateTransferAsync(SelectedAccount.Id, SelectedDestinationAccount!.Id, amount, exchangeRate, occurredAt, description, cancellationToken);
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

    private bool TryGetOccurredAt(out DateTime occurredAt)
    {
        occurredAt = default;
        if (OccurredDate is not { } date)
        {
            ErrorMessage = "Вкажіть дату операції.";
            return false;
        }

        if (!TimeSpan.TryParseExact(OccurredTimeInput.Trim(), TimeFormats, CultureInfo.InvariantCulture, out var time)
            || time >= TimeSpan.FromDays(1))
        {
            ErrorMessage = "Час має бути у форматі ГГ:ХХ, наприклад 09:30.";
            return false;
        }

        occurredAt = date.Date.Add(time);
        if (occurredAt > DateTime.Now.AddMinutes(1))
        {
            ErrorMessage = "Дата та час операції не можуть бути в майбутньому.";
            return false;
        }

        return true;
    }

    private void ApplyPrefilledRate(decimal rate, string hint)
    {
        _prefilledRateInput = rate.ToString("0.########", CultureInfo.InvariantCulture);
        ExchangeRateInput = _prefilledRateInput;
        ExchangeRateHint = hint;
    }

    private decimal ConvertAmount(decimal amount, decimal rate)
    {
        var decimalPlaces = CurrencyDecimalPlaces(SelectedDestinationAccount);
        return decimal.Round(amount * rate, decimalPlaces, MidpointRounding.AwayFromZero);
    }

    private static int CurrencyDecimalPlaces(AccountModel? account) =>
        account is null ? DecimalInput.MaxAmountDecimalPlaces : Math.Min((int)account.CurrencyDecimalPlaces, DecimalInput.MaxAmountDecimalPlaces);

    private static string KindCode(TransactionEditMode mode) => mode == TransactionEditMode.Income ? "Income" : "Expense";

    private void FillAccounts(IEnumerable<AccountModel> accounts)
    {
        Accounts.Clear();
        foreach (var account in accounts)
        {
            Accounts.Add(account);
        }
    }

    private void FillCategories(IEnumerable<OptionModel> categories)
    {
        Categories.Clear();
        foreach (var category in categories)
        {
            Categories.Add(category);
        }
    }

    private bool CanExecuteSave() => !IsBusy && IsLoaded;

    protected override void OnIsBusyChanged(bool value) => SaveCommand.NotifyCanExecuteChanged();

    partial void OnSelectedAccountChanged(AccountModel? value) => PrefillExchangeRateCommand.Execute(null);

    partial void OnSelectedDestinationAccountChanged(AccountModel? value) => PrefillExchangeRateCommand.Execute(null);

    partial void OnOccurredDateChanged(DateTime? value)
    {
        if (ExchangeRateInput.Length == 0 || ExchangeRateInput == _prefilledRateInput)
        {
            PrefillExchangeRateCommand.Execute(null);
        }
    }
}
