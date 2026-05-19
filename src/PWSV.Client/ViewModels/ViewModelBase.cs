using CommunityToolkit.Mvvm.ComponentModel;
using PWSV.Client.Services;

namespace PWSV.Client.ViewModels;

public abstract class ViewModelBase : ObservableValidator
{
    private bool _isBusy;
    private string? _errorMessage;

    public bool IsBusy
    {
        get => _isBusy;
        set
        {
            if (SetProperty(ref _isBusy, value))
            {
                OnIsBusyChanged(value);
            }
        }
    }

    public string? ErrorMessage
    {
        get => _errorMessage;
        set => SetProperty(ref _errorMessage, value);
    }

    protected virtual void OnIsBusyChanged(bool value)
    {
    }

    protected void ClearError() => ErrorMessage = null;

    protected void SetErrorFromException(Exception ex)
    {
        ErrorMessage = ex switch
        {
            ApiException apiEx => apiEx.Message,
            HttpRequestException => "Не вдалося з'єднатися з сервером. Перевірте, що API запущено.",
            TaskCanceledException => "Запит перервано (таймаут або скасування).",
            _ => $"Неочікувана помилка: {ex.Message}"
        };
    }
}
