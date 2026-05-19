using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PWSV.Client.Models;
using PWSV.Client.Services;
using PWSV.Client.Services.Interfaces;

namespace PWSV.Client.ViewModels.ExchangeRates;

public sealed partial class ExchangeRatesViewModel : ViewModelBase
{
    private readonly IApiClient _api;

    public ExchangeRatesViewModel(IApiClient api)
    {
        _api = api;
        _ = InitializeAsync(CancellationToken.None);
    }

    public ObservableCollection<ExchangeRateModel> Rates { get; } = [];
    public ObservableCollection<CurrencyModel> Currencies { get; } = [];

    [ObservableProperty]
    private CurrencyModel? _newBaseCurrency;

    [ObservableProperty]
    private CurrencyModel? _newQuoteCurrency;

    [ObservableProperty]
    private string _newRateInput = string.Empty;

    [ObservableProperty]
    private DateTime _newEffectiveDate = DateTime.Today;

    private async Task InitializeAsync(CancellationToken cancellationToken)
    {
        try
        {
            var currencies = await _api.GetCurrenciesAsync(cancellationToken);
            Currencies.Clear();
            foreach (var c in currencies)
            {
                Currencies.Add(c);
            }

            NewBaseCurrency = Currencies.FirstOrDefault();
            NewQuoteCurrency = Currencies.Skip(1).FirstOrDefault();
            await LoadAsync(cancellationToken);
        }
        catch (Exception ex) when (ex is ApiException or HttpRequestException or TaskCanceledException)
        {
            SetErrorFromException(ex);
        }
    }

    [RelayCommand(CanExecute = nameof(CanExecuteCommands))]
    public async Task LoadAsync(CancellationToken cancellationToken)
    {
        try
        {
            IsBusy = true;
            ClearError();
            var rates = await _api.GetExchangeRatesAsync(null, null, null, null, cancellationToken);
            Rates.Clear();
            foreach (var r in rates)
            {
                Rates.Add(r);
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

    [RelayCommand(CanExecute = nameof(CanAdd))]
    private async Task AddAsync(CancellationToken cancellationToken)
    {
        if (NewBaseCurrency is null || NewQuoteCurrency is null)
        {
            ErrorMessage = "Виберіть валютну пару.";
            return;
        }

        if (NewBaseCurrency.Id == NewQuoteCurrency.Id)
        {
            ErrorMessage = "Валюти повинні відрізнятись.";
            return;
        }

        if (!DecimalInput.TryParse(NewRateInput, out var rate) || rate <= 0m)
        {
            ErrorMessage = "Курс повинен бути більше нуля.";
            return;
        }

        try
        {
            IsBusy = true;
            ClearError();
            await _api.CreateExchangeRateAsync(
                NewBaseCurrency.Id,
                NewQuoteCurrency.Id,
                rate,
                DateOnly.FromDateTime(NewEffectiveDate),
                cancellationToken);
            NewRateInput = string.Empty;
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
    private bool CanAdd() => !IsBusy;

    protected override void OnIsBusyChanged(bool value)
    {
        LoadCommand.NotifyCanExecuteChanged();
        AddCommand.NotifyCanExecuteChanged();
    }
}
