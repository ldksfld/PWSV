using System.ComponentModel.DataAnnotations;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PWSV.Client.Services;
using PWSV.Client.Services.Interfaces;

namespace PWSV.Client.ViewModels;

public sealed partial class RegisterViewModel(IApiClient api, ITokenStorage tokenStorage, INavigationService navigation) : ViewModelBase
{
    [ObservableProperty]
    [Required]
    [MinLength(3)]
    [MaxLength(64)]
    private string _username = string.Empty;

    [ObservableProperty]
    [Required]
    [MinLength(8)]
    [MaxLength(128)]
    [RegularExpression(
        @"(?=.*[a-z])(?=.*[A-Z])(?=.*\d).+",
        ErrorMessage = "Має містити нижній, верхній регістр та цифру.")]
    private string _password = string.Empty;

    [ObservableProperty]
    private string _confirmPassword = string.Empty;

    [ObservableProperty]
    [Required]
    [MinLength(8)]
    private string _masterPassword = string.Empty;

    [ObservableProperty]
    private string _confirmMasterPassword = string.Empty;

    [RelayCommand(CanExecute = nameof(CanSubmit))]
    private async Task RegisterAsync(CancellationToken cancellationToken)
    {
        ClearError();
        ValidateAllProperties();

        if (Password != ConfirmPassword)
        {
            ErrorMessage = "Паролі не співпадають.";
            return;
        }

        if (MasterPassword != ConfirmMasterPassword)
        {
            ErrorMessage = "Мастер-паролі не співпадають.";
            return;
        }

        if (HasErrors)
        {
            ErrorMessage = "Перевірте коректність полів.";
            return;
        }

        try
        {
            IsBusy = true;
            var response = await api.RegisterAsync(Username, Password, MasterPassword, cancellationToken);
            tokenStorage.Set(response.UserId, response.Username, response.AccessToken, response.ExpiresAt);
            navigation.NavigateTo<MainViewModel>();
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
    private void GoToLogin()
    {
        navigation.NavigateTo<LoginViewModel>();
    }

    private bool CanSubmit() => !IsBusy;

    protected override void OnIsBusyChanged(bool value) => RegisterCommand.NotifyCanExecuteChanged();
}
