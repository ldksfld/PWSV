using System.ComponentModel.DataAnnotations;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PWSV.Client.Services;
using PWSV.Client.Services.Interfaces;

namespace PWSV.Client.ViewModels;

public sealed partial class RegisterViewModel(IApiClient api, ITokenStorage tokenStorage, INavigationService navigation) : ViewModelBase
{
    [ObservableProperty]
    [Required(ErrorMessage = "Ім'я користувача обов'язкове.")]
    [MinLength(3, ErrorMessage = "Ім'я користувача має містити не менше 3 символів.")]
    [MaxLength(64, ErrorMessage = "Ім'я користувача має містити не більше 64 символів.")]
    private string _username = string.Empty;

    [ObservableProperty]
    [Required(ErrorMessage = "Пароль обов'язковий.")]
    [MinLength(8, ErrorMessage = "Пароль має містити не менше 8 символів.")]
    [MaxLength(128, ErrorMessage = "Пароль має містити не більше 128 символів.")]
    [RegularExpression(
        @"(?=.*\p{Ll})(?=.*\p{Lu})(?=.*[0-9]).+",
        ErrorMessage = "Пароль має містити малу й велику літери та цифру.")]
    private string _password = string.Empty;

    [ObservableProperty]
    private string _confirmPassword = string.Empty;

    [ObservableProperty]
    [Required(ErrorMessage = "Майстер-пароль обов'язковий.")]
    [MinLength(8, ErrorMessage = "Майстер-пароль має містити не менше 8 символів.")]
    [MaxLength(256, ErrorMessage = "Майстер-пароль має містити не більше 256 символів.")]
    private string _masterPassword = string.Empty;

    [ObservableProperty]
    private string _confirmMasterPassword = string.Empty;

    [RelayCommand(CanExecute = nameof(CanSubmit))]
    private async Task RegisterAsync(CancellationToken cancellationToken)
    {
        ClearError();
        if (!TryValidate())
        {
            return;
        }

        if (Password != ConfirmPassword)
        {
            ErrorMessage = "Паролі не збігаються.";
            return;
        }

        if (MasterPassword != ConfirmMasterPassword)
        {
            ErrorMessage = "Майстер-паролі не збігаються.";
            return;
        }

        try
        {
            IsBusy = true;
            var response = await api.RegisterAsync(Username.Trim(), Password, MasterPassword, cancellationToken);
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
