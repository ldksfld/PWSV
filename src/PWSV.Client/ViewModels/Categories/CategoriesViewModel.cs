using System.Collections.ObjectModel;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using PWSV.Client.Models;
using PWSV.Client.Services;
using PWSV.Client.Services.Interfaces;
using PWSV.Client.Views.Categories;

namespace PWSV.Client.ViewModels.Categories;

public sealed partial class CategoriesViewModel : ViewModelBase
{
    private readonly IApiClient _api;
    private readonly IDialogService _dialog;
    private readonly IServiceProvider _services;
    private CancellationTokenSource? _loadCts;

    public CategoriesViewModel(IApiClient api, IDialogService dialog, IServiceProvider services)
    {
        _api = api;
        _dialog = dialog;
        _services = services;
        _ = LoadAsync(CancellationToken.None);
    }

    public ObservableCollection<CategoryTreeNodeModel> Tree { get; } = [];

    [ObservableProperty]
    private bool _includeInactive;

    [ObservableProperty]
    private CategoryTreeNodeModel? _selectedNode;

    [RelayCommand(CanExecute = nameof(CanExecuteCommands))]
    public async Task LoadAsync(CancellationToken cancellationToken)
    {
        _loadCts?.Cancel();
        _loadCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var token = _loadCts.Token;

        try
        {
            IsBusy = true;
            ClearError();
            var tree = await _api.GetCategoryTreeAsync(IncludeInactive, token);
            if (token.IsCancellationRequested)
            {
                return;
            }

            Tree.Clear();
            foreach (var node in tree)
            {
                Tree.Add(node);
            }
        }
        catch (OperationCanceledException)
        {
            return;
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
        var vm = _services.GetRequiredService<CategoryEditViewModel>();
        await vm.InitializeForCreateAsync(kind, cancellationToken);
        var dialog = new CategoryEditDialog(vm) { Owner = Application.Current.MainWindow };
        if (dialog.ShowDialog() == true)
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

        var vm = _services.GetRequiredService<CategoryEditViewModel>();
        await vm.InitializeForEditAsync(SelectedNode.Id, SelectedNode.Name, SelectedNode.Kind, cancellationToken);
        var dialog = new CategoryEditDialog(vm) { Owner = Application.Current.MainWindow };
        if (dialog.ShowDialog() == true)
        {
            await LoadAsync(cancellationToken);
        }
    }

    [RelayCommand(CanExecute = nameof(CanModifySelected))]
    private async Task DeactivateAsync(CancellationToken cancellationToken)
    {
        if (SelectedNode is null)
        {
            return;
        }

        if (!_dialog.Confirm("Підтвердження", $"Деактивувати категорію '{SelectedNode.Name}'?"))
        {
            return;
        }

        try
        {
            IsBusy = true;
            ClearError();
            await _api.DeactivateCategoryAsync(SelectedNode.Id, cancellationToken);
            await LoadAsync(cancellationToken);
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

    private bool CanExecuteCommands() => !IsBusy;
    private bool CanModifySelected() => !IsBusy && SelectedNode is not null;

    partial void OnIncludeInactiveChanged(bool value) => _ = LoadAsync(CancellationToken.None);

    protected override void OnIsBusyChanged(bool value)
    {
        LoadCommand.NotifyCanExecuteChanged();
        AddIncomeCommand.NotifyCanExecuteChanged();
        AddExpenseCommand.NotifyCanExecuteChanged();
        EditCommand.NotifyCanExecuteChanged();
        DeactivateCommand.NotifyCanExecuteChanged();
    }

    partial void OnSelectedNodeChanged(CategoryTreeNodeModel? value)
    {
        EditCommand.NotifyCanExecuteChanged();
        DeactivateCommand.NotifyCanExecuteChanged();
    }
}
