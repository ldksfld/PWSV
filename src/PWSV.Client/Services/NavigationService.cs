using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.DependencyInjection;
using PWSV.Client.Services.Interfaces;
using PWSV.Client.ViewModels;

namespace PWSV.Client.Services;

public sealed partial class NavigationService(IServiceProvider services) : ObservableObject, INavigationService
{
    [ObservableProperty]
    private ViewModelBase? _currentViewModel;

    [ObservableProperty]
    private ViewModelBase? _currentContent;

    public event Action? Navigated;
    public event Action? ContentNavigated;

    public void NavigateTo<TViewModel>() where TViewModel : ViewModelBase
    {
        var viewModel = services.GetRequiredService<TViewModel>();
        CurrentViewModel = viewModel;
        Navigated?.Invoke();
    }

    public void NavigateContent<TViewModel>() where TViewModel : ViewModelBase
    {
        var viewModel = services.GetRequiredService<TViewModel>();
        CurrentContent = viewModel;
        ContentNavigated?.Invoke();
    }

    public void NavigateContent(ViewModelBase viewModel)
    {
        CurrentContent = viewModel;
        ContentNavigated?.Invoke();
    }
}
