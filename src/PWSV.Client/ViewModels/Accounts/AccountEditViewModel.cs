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
    [Required(ErrorMessage = "Назва обов'язкова.")]
    [MaxLength(128, ErrorMessage = "Не більше 128 символів.")]
    private string _name = string.Empty;

    [ObservableProperty]
    private AccountTypeModel? _selectedAccountType;

    [ObservableProperty]
    private CurrencyModel? _selectedCurrency;

    [ObservableProperty]
    [MaxLength(64, ErrorMessage = "Не більше 64 символів.")]
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
        OnPropertyChanged(nameof(IsCreating));
        OnPropertyChanged(nameof(IsEditing));
        await LoadDictionariesAsync(cancellationToken);
        SelectedAccountType = AccountTypes.FirstOrDefault();
        SelectedCurrency = Currencies.FirstOrDefault();
    }

    public async Task InitializeForEditAsync(AccountModel source, CancellationToken cancellationToken)
    {
        EditingId = source.Id;
        Name = source.Name;
        AccountNumber = source.AccountNumber;
        IsActive = source.IsActive;
        await LoadDictionariesAsync(cancellationToken);
        SelectedAccountType = AccountTypes.FirstOrDefault(t => t.Id == source.AccountTypeId);
        SelectedCurrency = Currencies.FirstOrDefault(c => c.Id == source.CurrencyId);
        OnPropertyChanged(nameof(IsCreating));
        OnPropertyChanged(nameof(IsEditing));
    }

    private async Task LoadDictionariesAsync(CancellationToken cancellationToken)
    {
        try
        {
            var types = await api.GetAccountTypesAsync(cancellationToken);
            AccountTypes.Clear();
            foreach (var t in types)
            {
                AccountTypes.Add(t);
            }

            var currencies = await api.GetCurrenciesAsync(cancellationToken);
            Currencies.Clear();
            foreach (var c in currencies)
            {
                Currencies.Add(c);
            }
        }
        catch (Exception ex) when (ex is ApiException or HttpRequestException or TaskCanceledException)
        {
            SetErrorFromException(ex);
        }
    }

    [RelayCommand(CanExecute = nameof(CanExecuteSave))]
    private async Task SaveAsync(CancellationToken cancellationToken)
    {
        ClearError();
        ValidateAllProperties();
        if (HasErrors)
        {
            ErrorMessage = "Перевірте коректність полів.";
            return;
        }

        if (SelectedAccountType is null || SelectedCurrency is null)
        {
            ErrorMessage = "Виберіть тип рахунку та валюту.";
            return;
        }

        try
        {
            IsBusy = true;
            if (EditingId is { } id)
            {
                await api.UpdateAccountAsync(id, Name.Trim(), AccountNumber, IsActive, cancellationToken);
            }
            else
            {
                var initialBalance = 0m;
                if (!string.IsNullOrWhiteSpace(InitialBalanceInput)
                    && !DecimalInput.TryParse(InitialBalanceInput, out initialBalance))
                {
                    ErrorMessage = "Початковий баланс має бути числом.";
                    return;
                }

                await api.CreateAccountAsync(
                    Name.Trim(),
                    SelectedAccountType.Id,
                    SelectedCurrency.Id,
                    AccountNumber,
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

    private bool CanExecuteSave() => !IsBusy;

    protected override void OnIsBusyChanged(bool value) => SaveCommand.NotifyCanExecuteChanged();
}
