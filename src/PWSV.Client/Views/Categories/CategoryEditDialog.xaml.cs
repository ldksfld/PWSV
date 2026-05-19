using System.Windows;
using PWSV.Client.ViewModels.Categories;

namespace PWSV.Client.Views.Categories;

public partial class CategoryEditDialog : Window
{
    public CategoryEditDialog(CategoryEditViewModel viewModel)
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
