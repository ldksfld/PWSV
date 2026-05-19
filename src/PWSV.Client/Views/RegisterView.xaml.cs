using System.Windows.Controls;
using PWSV.Client.ViewModels;

namespace PWSV.Client.Views;

public partial class RegisterView : UserControl
{
    public RegisterView()
    {
        InitializeComponent();
    }

    private void PasswordBox_OnPasswordChanged(object sender, System.Windows.RoutedEventArgs e)
    {
        if (DataContext is RegisterViewModel vm && sender is PasswordBox pb)
        {
            vm.Password = pb.Password;
        }
    }

    private void ConfirmPasswordBox_OnPasswordChanged(object sender, System.Windows.RoutedEventArgs e)
    {
        if (DataContext is RegisterViewModel vm && sender is PasswordBox pb)
        {
            vm.ConfirmPassword = pb.Password;
        }
    }

    private void MasterPasswordBox_OnPasswordChanged(object sender, System.Windows.RoutedEventArgs e)
    {
        if (DataContext is RegisterViewModel vm && sender is PasswordBox pb)
        {
            vm.MasterPassword = pb.Password;
        }
    }

    private void ConfirmMasterPasswordBox_OnPasswordChanged(object sender, System.Windows.RoutedEventArgs e)
    {
        if (DataContext is RegisterViewModel vm && sender is PasswordBox pb)
        {
            vm.ConfirmMasterPassword = pb.Password;
        }
    }
}
