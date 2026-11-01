using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using PWSV.Client.Models;
using PWSV.Client.Services;
using PWSV.Client.Services.Interfaces;

namespace PWSV.Client.ViewModels.Categories;

public sealed partial class CategoriesViewModel(
    IApiClient api,
    IDialogService dialog,
    IServiceProvider services) : ViewModelBase, ILoadableViewModel
{
    public ObservableCollection<CategoryTreeNodeModel> Tree { get; } = [];

    [ObservableProperty]
    private bool _includeInactive;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(EditCommand))]
    [NotifyCanExecuteChangedFor(nameof(DeactivateCommand))]
    private CategoryTreeNodeModel? _selectedNode;

    [RelayCommand(CanExecute = nameof(CanExecuteCommands))]
    private async Task LoadAsync(CancellationToken cancellationToken)
    {
        try
        {
            IsBusy = true;
            ClearError();
            var tree = await api.GetCategoryTreeAsync(IncludeInactive, cancellationToken);

            Tree.Clear();
            foreach (var node in tree)
            {
                Tree.Add(node);
            }

            SelectedNode = null;
        }
        catch (Exception ex) when (ex is ApiException or HttpRequestException or TaskCanceledException)
        {
            SetErrorFromException(ex);
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand(CanExecute = nameof(CanExecuteCommands))]
    private Task AddIncomeAsync(CancellationToken cancellationToken)
        => OpenAddAsync("Income", cancellationToken);

    [RelayCommand(CanExecute = nameof(CanExecuteCommands))]
    private Task AddExpenseAsync(CancellationToken cancellationToken)
        => OpenAddAsync("Expense", cancellationToken);

    private async Task OpenAddAsync(string kind, CancellationToken cancellationToken)
    {
        var parentId = SelectedNode is { IsActive: true } node && node.Kind == kind ? node.Id : (int?)null;
        var editor = services.GetRequiredService<CategoryEditViewModel>();
        await editor.InitializeForCreateAsync(kind, parentId, cancellationToken);
        if (dialog.ShowDialog(editor))
        {
            await LoadAsync(cancellationToken);
        }
    }

    [RelayCommand(CanExecute = nameof(CanModifySelected))]
    private async Task EditAsync(CancellationToken cancellationToken)
    {
        if (SelectedNode is null)
        {
            return;
        }

        var editor = services.GetRequiredService<CategoryEditViewModel>();
        await editor.InitializeForEditAsync(SelectedNode, cancellationToken);
        if (dialog.ShowDialog(editor))
        {
            await LoadAsync(cancellationToken);
        }
    }

    [RelayCommand(CanExecute = nameof(CanDeactivateSelected))]
    private async Task DeactivateAsync(CancellationToken cancellationToken)
    {
        if (SelectedNode is null)
        {
            return;
        }

        var message = SelectedNode.Children.Count > 0
            ? $"Деактивувати категорію '{SelectedNode.Name}' разом з усіма підкатегоріями?"
            : $"Деактивувати категорію '{SelectedNode.Name}'?";
        if (!dialog.Confirm("Підтвердження", message))
        {
            return;
        }

        try
        {
            IsBusy = true;
            ClearError();
            await api.DeactivateCategoryAsync(SelectedNode.Id, cancellationToken);
        }
        catch (Exception ex) when (ex is ApiException or HttpRequestException or TaskCanceledException)
        {
            SetErrorFromException(ex);
            return;
        }
        finally
        {
            IsBusy = false;
        }

        await LoadAsync(cancellationToken);
    }

    private bool CanExecuteCommands() => !IsBusy;
    private bool CanModifySelected() => !IsBusy && SelectedNode is not null;
    private bool CanDeactivateSelected() => CanModifySelected() && SelectedNode!.IsActive;

    partial void OnIncludeInactiveChanged(bool value) => LoadCommand.Execute(null);

    protected override void OnIsBusyChanged(bool value)
    {
        LoadCommand.NotifyCanExecuteChanged();
        AddIncomeCommand.NotifyCanExecuteChanged();
        AddExpenseCommand.NotifyCanExecuteChanged();
        EditCommand.NotifyCanExecuteChanged();
        DeactivateCommand.NotifyCanExecuteChanged();
    }
}
