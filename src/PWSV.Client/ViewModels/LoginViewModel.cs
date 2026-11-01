using System.ComponentModel.DataAnnotations;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PWSV.Client.Services;
using PWSV.Client.Services.Interfaces;

namespace PWSV.Client.ViewModels;

public sealed partial class LoginViewModel(IApiClient api, ITokenStorage tokenStorage, INavigationService navigation) : ViewModelBase
{
    [ObservableProperty]
    [Required(ErrorMessage = "Ім'я користувача обов'язкове.")]
    [MinLength(3, ErrorMessage = "Ім'я користувача має містити не менше 3 символів.")]
    [MaxLength(64, ErrorMessage = "Ім'я користувача має містити не більше 64 символів.")]
    private string _username = string.Empty;

    [ObservableProperty]
    [Required(ErrorMessage = "Пароль обов'язковий.")]
    [MaxLength(128, ErrorMessage = "Пароль має містити не більше 128 символів.")]
    private string _password = string.Empty;

    [ObservableProperty]
    [Required(ErrorMessage = "Майстер-пароль обов'язковий.")]
    [MaxLength(256, ErrorMessage = "Майстер-пароль має містити не більше 256 символів.")]
    private string _masterPassword = string.Empty;

    [RelayCommand(CanExecute = nameof(CanSubmit))]
    private async Task LoginAsync(CancellationToken cancellationToken)
    {
        ClearError();
        if (!TryValidate())
        {
            return;
        }

        try
        {
            IsBusy = true;
            var response = await api.LoginAsync(Username, Password, MasterPassword, cancellationToken);
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
    private void GoToRegister()
    {
        navigation.NavigateTo<RegisterViewModel>();
    }

    private bool CanSubmit() => !IsBusy;

    protected override void OnIsBusyChanged(bool value) => LoginCommand.NotifyCanExecuteChanged();
}
