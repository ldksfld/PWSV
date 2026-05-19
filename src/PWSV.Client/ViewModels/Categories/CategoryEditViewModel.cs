using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PWSV.Client.Models;
using PWSV.Client.Services;
using PWSV.Client.Services.Interfaces;

namespace PWSV.Client.ViewModels.Categories;

public sealed partial class CategoryEditViewModel(IApiClient api) : ViewModelBase
{
    public ObservableCollection<CategoryModel> Parents { get; } = [];

    [ObservableProperty]
    private int? _editingId;

    [ObservableProperty]
    [Required(ErrorMessage = "Назва обов'язкова.")]
    [MaxLength(128)]
    private string _name = string.Empty;

    [ObservableProperty]
    private string _kind = "Income";

    [ObservableProperty]
    private CategoryModel? _selectedParent;

    public bool IsCreating => EditingId is null;
    public Action? Saved { get; set; }

    public async Task InitializeForCreateAsync(string kind, CancellationToken cancellationToken)
    {
        EditingId = null;
        Kind = kind;
        await LoadParentsAsync(cancellationToken);
    }

    public async Task InitializeForEditAsync(CategoryModel source, CancellationToken cancellationToken)
    {
        EditingId = source.Id;
        Name = source.Name;
        Kind = source.Kind;
        await LoadParentsAsync(cancellationToken);
        SelectedParent = Parents.FirstOrDefault(p => p.Id == source.ParentCategoryId);
    }

    public async Task InitializeForEditAsync(int id, string name, string kind, CancellationToken cancellationToken)
    {
        EditingId = id;
        Name = name;
        Kind = kind;
        try
        {
            var all = await api.GetCategoriesAsync(kind, true, cancellationToken);
            var current = all.FirstOrDefault(c => c.Id == id);
            Parents.Clear();
            foreach (var c in all.Where(c => c.Id != id && c.IsActive))
            {
                Parents.Add(c);
            }
            if (current is not null)
            {
                SelectedParent = Parents.FirstOrDefault(p => p.Id == current.ParentCategoryId);
            }
        }
        catch (Exception ex) when (ex is ApiException or HttpRequestException or TaskCanceledException)
        {
            SetErrorFromException(ex);
        }
    }

    private async Task LoadParentsAsync(CancellationToken cancellationToken)
    {
        try
        {
            var all = await api.GetCategoriesAsync(Kind, false, cancellationToken);
            Parents.Clear();
            foreach (var c in all.Where(c => c.Id != EditingId))
            {
                Parents.Add(c);
            }
        }
        catch (Exception ex) when (ex is ApiException or HttpRequestException or TaskCanceledException)
        {
            SetErrorFromException(ex);
        }
    }

    [RelayCommand(CanExecute = nameof(CanSave))]
    private async Task SaveAsync(CancellationToken cancellationToken)
    {
        ClearError();
        ValidateAllProperties();
        if (HasErrors)
        {
            ErrorMessage = "Перевірте коректність полів.";
            return;
        }

        try
        {
            IsBusy = true;
            if (EditingId is { } id)
            {
                await api.UpdateCategoryAsync(id, Name.Trim(), SelectedParent?.Id, cancellationToken);
            }
            else
            {
                await api.CreateCategoryAsync(Name.Trim(), Kind, SelectedParent?.Id, cancellationToken);
            }
            Saved?.Invoke();
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

    private bool CanSave() => !IsBusy;
    protected override void OnIsBusyChanged(bool value) => SaveCommand.NotifyCanExecuteChanged();
}
