using System.Windows;
using PWSV.Client.ViewModels.Transactions;

namespace PWSV.Client.Views.Transactions;

public partial class TransactionEditDialog : Window
{
    public TransactionEditDialog(TransactionEditViewModel viewModel)
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
