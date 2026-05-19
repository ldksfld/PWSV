using System.Windows;
using System.Windows.Controls;
using PWSV.Client.Models;
using PWSV.Client.ViewModels.Categories;

namespace PWSV.Client.Views.Categories;

public partial class CategoriesView : UserControl
{
    public CategoriesView()
    {
        InitializeComponent();
    }

    private void OnTreeSelectionChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
    {
        if (DataContext is CategoriesViewModel vm)
        {
            vm.SelectedNode = e.NewValue as CategoryTreeNodeModel;
        }
    }
}
