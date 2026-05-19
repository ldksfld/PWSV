using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PWSV.Client.Models;
using PWSV.Client.Services;
using PWSV.Client.Services.Interfaces;

namespace PWSV.Client.ViewModels.Currencies;

public sealed partial class CurrenciesViewModel : ViewModelBase
{
    private readonly IApiClient _api;

    public CurrenciesViewModel(IApiClient api)
    {
        _api = api;
        _ = LoadAsync(CancellationToken.None);
    }

    public ObservableCollection<CurrencyModel> Currencies { get; } = [];

    [ObservableProperty]
    [Required(ErrorMessage = "Код обов'язковий.")]
    [StringLength(10, MinimumLength = 3, ErrorMessage = "Код від 3 до 10 символів.")]
    private string _newCode = string.Empty;

    [ObservableProperty]
    [Required]
    [MaxLength(64)]
    private string _newName = string.Empty;

    [ObservableProperty]
    [Required]
    [MaxLength(8)]
    private string _newSymbol = string.Empty;

    [ObservableProperty]
    [Range(0, 8)]
    private byte _newDecimalPlaces = 2;

    [RelayCommand(CanExecute = nameof(CanExecuteCommands))]
    public async Task LoadAsync(CancellationToken cancellationToken)
    {
        try
        {
            IsBusy = true;
            ClearError();
            var items = await _api.GetCurrenciesAsync(cancellationToken);
            Currencies.Clear();
            foreach (var item in items)
            {
                Currencies.Add(item);
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
        ValidateAllProperties();
        if (HasErrors)
        {
            ErrorMessage = "Перевірте коректність полів.";
            return;
        }

        try
        {
            IsBusy = true;
            await _api.CreateCurrencyAsync(NewCode.Trim(), NewName.Trim(), NewSymbol.Trim(), NewDecimalPlaces, cancellationToken);
            NewCode = string.Empty;
            NewName = string.Empty;
            NewSymbol = string.Empty;
            NewDecimalPlaces = 2;
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

    protected override void OnIsBusyChanged(bool value)
    {
        LoadCommand.NotifyCanExecuteChanged();
        AddCommand.NotifyCanExecuteChanged();
    }
}
