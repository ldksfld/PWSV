using System.Windows;
using PWSV.Client.Services.Interfaces;
using PWSV.Client.ViewModels;
using PWSV.Client.ViewModels.Accounts;
using PWSV.Client.ViewModels.Categories;
using PWSV.Client.ViewModels.Transactions;
using PWSV.Client.Views;
using PWSV.Client.Views.Accounts;
using PWSV.Client.Views.Categories;
using PWSV.Client.Views.Transactions;

namespace PWSV.Client.Services;

public sealed class DialogService : IDialogService
{
    public void ShowInfo(string title, string message)
    {
        ShowMessage(title, message, MessageDialogKind.Information);
    }

    public void ShowError(string title, string message)
    {
        ShowMessage(title, message, MessageDialogKind.Error);
    }

    public bool Confirm(string title, string message)
    {
        return ShowMessage(title, message, MessageDialogKind.Confirmation);
    }

    public bool ShowDialog(ViewModelBase viewModel)
    {
        Window dialog = viewModel switch
        {
            AccountEditViewModel account => new AccountEditDialog(account),
            CategoryEditViewModel category => new CategoryEditDialog(category),
            TransactionEditViewModel transaction => new TransactionEditDialog(transaction),
            ChangePasswordViewModel password => new ChangePasswordDialog(password),
            DeleteUserViewModel deleteUser => new DeleteUserDialog(deleteUser),
            _ => throw new ArgumentException($"Dialog for {viewModel.GetType().Name} is not registered.", nameof(viewModel))
        };

        dialog.Owner = Application.Current.MainWindow;
        return dialog.ShowDialog() == true;
    }

    private static bool ShowMessage(string title, string message, MessageDialogKind kind)
    {
        var dialog = new MessageDialog(title, message, kind);
        if (Application.Current.MainWindow is { IsVisible: true } owner)
        {
            dialog.Owner = owner;
        }
        else
        {
            dialog.WindowStartupLocation = WindowStartupLocation.CenterScreen;
        }

        return dialog.ShowDialog() == true;
    }
}
