using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PWSV.Client.Models;
using PWSV.Client.Services;
using PWSV.Client.Services.Interfaces;

namespace PWSV.Client.ViewModels.ExchangeRates;

public sealed partial class ExchangeRatesViewModel(IApiClient api) : ViewModelBase, ILoadableViewModel
{
    private const int MaxRateDecimalPlaces = 8;
    private const decimal MaxRate = 9_999_999_999.99999999m;

    public ObservableCollection<ExchangeRateModel> Rates { get; } = [];
    public ObservableCollection<CurrencyModel> Currencies { get; } = [];

    [ObservableProperty]
    private CurrencyModel? _newBaseCurrency;

    [ObservableProperty]
    private CurrencyModel? _newQuoteCurrency;

    [ObservableProperty]
    private string _newRateInput = string.Empty;

    [ObservableProperty]
    private DateTime? _newEffectiveDate = DateTime.Today;

    [RelayCommand(CanExecute = nameof(CanExecuteCommands))]
    private async Task LoadAsync(CancellationToken cancellationToken)
    {
        try
        {
            IsBusy = true;
            ClearError();

            var baseId = NewBaseCurrency?.Id;
            var quoteId = NewQuoteCurrency?.Id;
            var currencies = await api.GetCurrenciesAsync(cancellationToken);
            Currencies.Clear();
            foreach (var currency in currencies)
            {
                Currencies.Add(currency);
            }

            NewBaseCurrency = Currencies.FirstOrDefault(c => c.Id == baseId) ?? Currencies.FirstOrDefault(c => c.Code == "USD");
            NewQuoteCurrency = Currencies.FirstOrDefault(c => c.Id == quoteId) ?? Currencies.FirstOrDefault(c => c.Code == "UAH");

            var rates = await api.GetExchangeRatesAsync(null, null, null, null, cancellationToken);
            Rates.Clear();
            foreach (var rate in rates)
            {
                Rates.Add(rate);
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
    private async Task AddAsync(CancellationToken cancellationToken)
    {
        ClearError();

        if (NewBaseCurrency is null || NewQuoteCurrency is null)
        {
            ErrorMessage = "Виберіть валютну пару.";
            return;
        }

        if (NewBaseCurrency.Id == NewQuoteCurrency.Id)
        {
            ErrorMessage = "Базова та котирувана валюти повинні відрізнятися.";
            return;
        }

        if (!DecimalInput.TryParse(NewRateInput, out var rate) || rate <= 0m)
        {
            ErrorMessage = "Курс повинен бути додатним числом.";
            return;
        }

        if (rate > MaxRate || decimal.Round(rate, MaxRateDecimalPlaces) != rate)
        {
            ErrorMessage = "Курс може містити не більше 8 знаків після коми та не перевищувати 9 999 999 999,99999999.";
            return;
        }

        if (NewEffectiveDate is not { } effectiveDate)
        {
            ErrorMessage = "Вкажіть дату курсу.";
            return;
        }

        if (effectiveDate.Date > DateTime.Today)
        {
            ErrorMessage = "Дата курсу не може бути в майбутньому.";
            return;
        }

        try
        {
            IsBusy = true;
            await api.CreateExchangeRateAsync(
                NewBaseCurrency.Id,
                NewQuoteCurrency.Id,
                rate,
                DateOnly.FromDateTime(effectiveDate),
                cancellationToken);
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

        NewRateInput = string.Empty;
        await LoadAsync(cancellationToken);
    }

    private bool CanExecuteCommands() => !IsBusy;

    protected override void OnIsBusyChanged(bool value)
    {
        LoadCommand.NotifyCanExecuteChanged();
        AddCommand.NotifyCanExecuteChanged();
    }
}
