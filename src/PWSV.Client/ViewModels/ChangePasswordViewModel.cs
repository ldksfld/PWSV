using System.ComponentModel.DataAnnotations;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PWSV.Client.Services;
using PWSV.Client.Services.Interfaces;

namespace PWSV.Client.ViewModels;

public sealed partial class ChangePasswordViewModel(IApiClient api) : ViewModelBase
{
    [ObservableProperty]
    [Required(ErrorMessage = "Поточний пароль обов'язковий.")]
    [MaxLength(128, ErrorMessage = "Поточний пароль має містити не більше 128 символів.")]
    private string _oldPassword = string.Empty;

    [ObservableProperty]
    [Required(ErrorMessage = "Новий пароль обов'язковий.")]
    [MinLength(8, ErrorMessage = "Новий пароль має містити не менше 8 символів.")]
    [MaxLength(128, ErrorMessage = "Новий пароль має містити не більше 128 символів.")]
    [RegularExpression(
        @"(?=.*\p{Ll})(?=.*\p{Lu})(?=.*[0-9]).+",
        ErrorMessage = "Новий пароль має містити малу й велику літери та цифру.")]
    private string _newPassword = string.Empty;

    [ObservableProperty]
    private string _confirmPassword = string.Empty;

    public Action? Saved { get; set; }

    [RelayCommand(CanExecute = nameof(CanSave))]
    private async Task SaveAsync(CancellationToken cancellationToken)
    {
        ClearError();
        if (!TryValidate())
        {
            return;
        }

        if (NewPassword != ConfirmPassword)
        {
            ErrorMessage = "Новий пароль і підтвердження не збігаються.";
            return;
        }

        if (NewPassword == OldPassword)
        {
            ErrorMessage = "Новий пароль не повинен збігатися з поточним.";
            return;
        }

        try
        {
            IsBusy = true;
            await api.ChangePasswordAsync(OldPassword, NewPassword, cancellationToken);
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

    private bool CanSave() => !IsBusy;

    protected override void OnIsBusyChanged(bool value) => SaveCommand.NotifyCanExecuteChanged();
}
