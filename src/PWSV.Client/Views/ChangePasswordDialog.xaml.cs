using System.Windows;
using System.Windows.Controls;
using PWSV.Client.ViewModels;

namespace PWSV.Client.Views;

public partial class ChangePasswordDialog : Window
{
    private readonly ChangePasswordViewModel _viewModel;

    public ChangePasswordDialog(ChangePasswordViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = viewModel;
        viewModel.Saved = () =>
        {
            DialogResult = true;
            Close();
        };
    }

    private void OldPasswordBox_OnPasswordChanged(object sender, RoutedEventArgs e)
    {
        _viewModel.OldPassword = ((PasswordBox)sender).Password;
    }

    private void NewPasswordBox_OnPasswordChanged(object sender, RoutedEventArgs e)
    {
        _viewModel.NewPassword = ((PasswordBox)sender).Password;
    }

    private void ConfirmPasswordBox_OnPasswordChanged(object sender, RoutedEventArgs e)
    {
        _viewModel.ConfirmPassword = ((PasswordBox)sender).Password;
    }

    private void OnCancelClick(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
