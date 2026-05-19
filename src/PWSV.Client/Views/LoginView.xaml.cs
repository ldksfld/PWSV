using System.Windows.Controls;
using PWSV.Client.ViewModels;

namespace PWSV.Client.Views;

public partial class LoginView : UserControl
{
    public LoginView()
    {
        InitializeComponent();
    }

    private void PasswordBox_OnPasswordChanged(object sender, System.Windows.RoutedEventArgs e)
    {
        if (DataContext is LoginViewModel vm && sender is PasswordBox pb)
        {
            vm.Password = pb.Password;
        }
    }

    private void MasterPasswordBox_OnPasswordChanged(object sender, System.Windows.RoutedEventArgs e)
    {
        if (DataContext is LoginViewModel vm && sender is PasswordBox pb)
        {
            vm.MasterPassword = pb.Password;
        }
    }
}
