using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PWSV.Client.Models;
using PWSV.Client.Services;
using PWSV.Client.Services.Interfaces;

namespace PWSV.Client.ViewModels.Currencies;

public sealed partial class CurrenciesViewModel(IApiClient api) : ViewModelBase, ILoadableViewModel
{
    public ObservableCollection<CurrencyModel> Currencies { get; } = [];

    [ObservableProperty]
    [NotifyDataErrorInfo]
    [Required(ErrorMessage = "Код валюти обов'язковий.")]
    [RegularExpression(@"^\s*[A-Za-z0-9]{3,10}\s*$", ErrorMessage = "Код валюти має містити від 3 до 10 латинських літер або цифр.")]
    private string _newCode = string.Empty;

    [ObservableProperty]
    [NotifyDataErrorInfo]
    [Required(ErrorMessage = "Назва валюти обов'язкова.")]
    [MaxLength(64, ErrorMessage = "Назва валюти має містити не більше 64 символів.")]
    private string _newName = string.Empty;

    [ObservableProperty]
    [NotifyDataErrorInfo]
    [Required(ErrorMessage = "Символ валюти обов'язковий.")]
    [MaxLength(8, ErrorMessage = "Символ валюти має містити не більше 8 символів.")]
    private string _newSymbol = string.Empty;

    [ObservableProperty]
    [NotifyDataErrorInfo]
    [Required(ErrorMessage = "Кількість знаків після коми обов'язкова.")]
    [RegularExpression(@"^\s*[0-8]\s*$", ErrorMessage = "Кількість знаків після коми має бути цілим числом від 0 до 8.")]
    private string _newDecimalPlaces = "2";

    [RelayCommand(CanExecute = nameof(CanExecuteCommands))]
    private async Task LoadAsync(CancellationToken cancellationToken)
    {
        try
        {
            IsBusy = true;
            ClearError();
            var items = await api.GetCurrenciesAsync(cancellationToken);
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
        if (!TryValidate())
        {
            return;
        }

        try
        {
            IsBusy = true;
            await api.CreateCurrencyAsync(
                NewCode.Trim().ToUpperInvariant(),
                NewName.Trim(),
                NewSymbol.Trim(),
                byte.Parse(NewDecimalPlaces.Trim(), CultureInfo.InvariantCulture),
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

        NewCode = string.Empty;
        NewName = string.Empty;
        NewSymbol = string.Empty;
        NewDecimalPlaces = "2";
        ClearErrors();
        await LoadAsync(cancellationToken);
    }

    private bool CanExecuteCommands() => !IsBusy;

    protected override void OnIsBusyChanged(bool value)
    {
        LoadCommand.NotifyCanExecuteChanged();
        AddCommand.NotifyCanExecuteChanged();
    }
}
