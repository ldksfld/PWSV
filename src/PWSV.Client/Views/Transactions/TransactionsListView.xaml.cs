using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using PWSV.Client.ViewModels.Transactions;

namespace PWSV.Client.Views.Transactions;

public partial class TransactionsListView : UserControl
{
    public TransactionsListView()
    {
        InitializeComponent();
    }

    private void OnRowDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (ItemsControl.ContainerFromElement((DataGrid)sender, (DependencyObject)e.OriginalSource) is DataGridRow
            && DataContext is TransactionsListViewModel viewModel
            && viewModel.EditCommand.CanExecute(null))
        {
            viewModel.EditCommand.Execute(null);
        }
    }
}
