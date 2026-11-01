using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PWSV.Client.Models;
using PWSV.Client.Services;
using PWSV.Client.Services.Interfaces;

namespace PWSV.Client.ViewModels.Accounts;

public sealed partial class AccountEditViewModel(IApiClient api) : ViewModelBase
{
    public ObservableCollection<AccountTypeModel> AccountTypes { get; } = [];
    public ObservableCollection<CurrencyModel> Currencies { get; } = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsCreating))]
    [NotifyPropertyChangedFor(nameof(IsEditing))]
    private int? _editingId;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    private bool _isLoaded;

    [ObservableProperty]
    [NotifyDataErrorInfo]
    [Required(ErrorMessage = "Назва рахунку обов'язкова.")]
    [MaxLength(128, ErrorMessage = "Назва рахунку має містити не більше 128 символів.")]
    private string _name = string.Empty;

    [ObservableProperty]
    private AccountTypeModel? _selectedAccountType;

    [ObservableProperty]
    private CurrencyModel? _selectedCurrency;

    [ObservableProperty]
    [NotifyDataErrorInfo]
    [MaxLength(64, ErrorMessage = "Номер рахунку має містити не більше 64 символів.")]
    private string? _accountNumber;

    [ObservableProperty]
    private string _initialBalanceInput = "0";

    [ObservableProperty]
    private bool _isActive = true;

    public bool IsCreating => EditingId is null;
    public bool IsEditing => EditingId is not null;

    public Action? Saved { get; set; }

    public async Task InitializeForCreateAsync(CancellationToken cancellationToken)
    {
        EditingId = null;
        IsLoaded = await LoadDictionariesAsync(cancellationToken);
        SelectedAccountType = AccountTypes.FirstOrDefault();
        SelectedCurrency = Currencies.FirstOrDefault(c => c.Code == "UAH") ?? Currencies.FirstOrDefault();
    }

    public async Task InitializeForEditAsync(int accountId, CancellationToken cancellationToken)
    {
        EditingId = accountId;
        if (!await LoadDictionariesAsync(cancellationToken))
        {
            return;
        }

        try
        {
            var details = await api.GetAccountAsync(accountId, cancellationToken);
            var account = details.Account;
            Name = account.Name;
            AccountNumber = account.AccountNumber;
            IsActive = account.IsActive;
            SelectedAccountType = AccountTypes.FirstOrDefault(t => t.Id == account.AccountTypeId);
            SelectedCurrency = Currencies.FirstOrDefault(c => c.Id == account.CurrencyId);
            IsLoaded = true;
        }
        catch (Exception ex) when (ex is ApiException or HttpRequestException or TaskCanceledException)
        {
            SetErrorFromException(ex);
        }
    }

    private async Task<bool> LoadDictionariesAsync(CancellationToken cancellationToken)
    {
        try
        {
            var types = await api.GetAccountTypesAsync(cancellationToken);
            AccountTypes.Clear();
            foreach (var type in types)
            {
                AccountTypes.Add(type);
            }

            var currencies = await api.GetCurrenciesAsync(cancellationToken);
            Currencies.Clear();
            foreach (var currency in currencies)
            {
                Currencies.Add(currency);
            }

            return true;
        }
        catch (Exception ex) when (ex is ApiException or HttpRequestException or TaskCanceledException)
        {
            SetErrorFromException(ex);
            return false;
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

        if (SelectedAccountType is null || SelectedCurrency is null)
        {
            ErrorMessage = "Виберіть тип рахунку та валюту.";
            return;
        }

        var initialBalance = 0m;
        if (IsCreating && !string.IsNullOrWhiteSpace(InitialBalanceInput))
        {
            if (!DecimalInput.TryParse(InitialBalanceInput, out initialBalance))
            {
                ErrorMessage = "Початковий баланс має бути числом.";
                return;
            }

            var amountError = DecimalInput.ValidateAmount(initialBalance, SelectedCurrency.DecimalPlaces, allowZero: true);
            if (amountError is not null)
            {
                ErrorMessage = $"Початковий баланс: {amountError}";
                return;
            }
        }

        try
        {
            IsBusy = true;
            if (EditingId is { } id)
            {
                await api.UpdateAccountAsync(id, Name.Trim(), AccountNumber?.Trim(), IsActive, cancellationToken);
            }
            else
            {
                await api.CreateAccountAsync(
                    Name.Trim(),
                    SelectedAccountType.Id,
                    SelectedCurrency.Id,
                    AccountNumber?.Trim(),
                    initialBalance,
                    cancellationToken);
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

    private bool CanExecuteSave() => !IsBusy && IsLoaded;

    protected override void OnIsBusyChanged(bool value) => SaveCommand.NotifyCanExecuteChanged();
}
