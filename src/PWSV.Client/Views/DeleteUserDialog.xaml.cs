using System.Windows;
using System.Windows.Controls;
using PWSV.Client.ViewModels;

namespace PWSV.Client.Views;

public partial class DeleteUserDialog : Window
{
    private readonly DeleteUserViewModel _viewModel;

    public DeleteUserDialog(DeleteUserViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = viewModel;
        viewModel.Deleted = () =>
        {
            DialogResult = true;
            Close();
        };
    }

    private void PasswordBox_OnPasswordChanged(object sender, RoutedEventArgs e)
    {
        _viewModel.Password = ((PasswordBox)sender).Password;
    }

    private void MasterPasswordBox_OnPasswordChanged(object sender, RoutedEventArgs e)
    {
        _viewModel.MasterPassword = ((PasswordBox)sender).Password;
    }

    private void OnCancelClick(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
