using System.ComponentModel.DataAnnotations;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PWSV.Client.Services;
using PWSV.Client.Services.Interfaces;

namespace PWSV.Client.ViewModels;

public sealed partial class DeleteUserViewModel(IApiClient api) : ViewModelBase
{
    [ObservableProperty]
    [Required(ErrorMessage = "Пароль обов'язковий.")]
    [MaxLength(128, ErrorMessage = "Пароль має містити не більше 128 символів.")]
    private string _password = string.Empty;

    [ObservableProperty]
    [Required(ErrorMessage = "Майстер-пароль обов'язковий.")]
    [MaxLength(256, ErrorMessage = "Майстер-пароль має містити не більше 256 символів.")]
    private string _masterPassword = string.Empty;

    public Action? Deleted { get; set; }

    [RelayCommand(CanExecute = nameof(CanDelete))]
    private async Task DeleteAsync(CancellationToken cancellationToken)
    {
        ClearError();
        if (!TryValidate())
        {
            return;
        }

        try
        {
            IsBusy = true;
            await api.DeleteUserAsync(Password, MasterPassword, cancellationToken);
            Deleted?.Invoke();
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

    private bool CanDelete() => !IsBusy;

    protected override void OnIsBusyChanged(bool value) => DeleteCommand.NotifyCanExecuteChanged();
}
