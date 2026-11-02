using System.ComponentModel.DataAnnotations;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PWSV.Client.Models;
using PWSV.Client.Services;
using PWSV.Client.Services.Interfaces;

namespace PWSV.Client.ViewModels;

public sealed partial class LoginViewModel(IApiClient api, ITokenStorage tokenStorage, INavigationService navigation) : ViewModelBase, ILoadableViewModel
{
    private const int DemoAccountAttempts = 30;
    private static readonly TimeSpan DemoAccountRetryDelay = TimeSpan.FromSeconds(2);

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

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasDemoAccount))]
    [NotifyCanExecuteChangedFor(nameof(DemoLoginCommand))]
    private DemoAccountModel? _demoAccount;

    public bool HasDemoAccount => DemoAccount is not null;

    [RelayCommand]
    private async Task LoadAsync(CancellationToken cancellationToken)
    {
        for (var attempt = 1; ; attempt++)
        {
            try
            {
                DemoAccount = await api.GetDemoAccountAsync(cancellationToken);
                return;
            }
            catch (HttpRequestException) when (attempt < DemoAccountAttempts && navigation.CurrentViewModel == this)
            {
                await Task.Delay(DemoAccountRetryDelay, cancellationToken);
            }
            catch (Exception ex) when (ex is ApiException or HttpRequestException or TaskCanceledException)
            {
                DemoAccount = null;
                return;
            }
        }
    }

    [RelayCommand(CanExecute = nameof(CanSubmit))]
    private async Task LoginAsync(CancellationToken cancellationToken)
    {
        ClearError();
        if (!TryValidate())
        {
            return;
        }

        await SignInAsync(Username, Password, MasterPassword, cancellationToken);
    }

    [RelayCommand(CanExecute = nameof(CanLoginAsDemo))]
    private async Task DemoLoginAsync(CancellationToken cancellationToken)
    {
        ClearError();
        if (DemoAccount is { } demo)
        {
            await SignInAsync(demo.Username, demo.Password, demo.MasterPassword, cancellationToken);
        }
    }

    [RelayCommand]
    private void GoToRegister()
    {
        navigation.NavigateTo<RegisterViewModel>();
    }

    private async Task SignInAsync(string username, string password, string masterPassword, CancellationToken cancellationToken)
    {
        try
        {
            IsBusy = true;
            var response = await api.LoginAsync(username, password, masterPassword, cancellationToken);
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

    private bool CanSubmit() => !IsBusy;
    private bool CanLoginAsDemo() => !IsBusy && DemoAccount is not null;

    protected override void OnIsBusyChanged(bool value)
    {
        LoginCommand.NotifyCanExecuteChanged();
        DemoLoginCommand.NotifyCanExecuteChanged();
    }
}
