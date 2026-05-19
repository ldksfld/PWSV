using System.Windows;
using PWSV.Client.ViewModels.Accounts;

namespace PWSV.Client.Views.Accounts;

public partial class AccountEditDialog : Window
{
    public AccountEditDialog(AccountEditViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
        viewModel.Saved = () =>
        {
            DialogResult = true;
            Close();
        };
    }

    private void OnCancelClick(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
