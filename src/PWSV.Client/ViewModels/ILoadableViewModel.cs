using CommunityToolkit.Mvvm.Input;

namespace PWSV.Client.ViewModels;

public interface ILoadableViewModel
{
    IAsyncRelayCommand LoadCommand { get; }
}
