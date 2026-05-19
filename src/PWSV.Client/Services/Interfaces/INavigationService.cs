using PWSV.Client.ViewModels;

namespace PWSV.Client.Services.Interfaces;

public interface INavigationService
{
    ViewModelBase? CurrentViewModel { get; }
    ViewModelBase? CurrentContent { get; }
    event Action? Navigated;
    event Action? ContentNavigated;

    void NavigateTo<TViewModel>() where TViewModel : ViewModelBase;
    void NavigateContent<TViewModel>() where TViewModel : ViewModelBase;
    void NavigateContent(ViewModelBase viewModel);
}
