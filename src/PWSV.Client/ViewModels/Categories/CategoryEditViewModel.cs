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
    private static readonly OptionModel RootOption = new(null, "— Без батьківської категорії —");

    public ObservableCollection<OptionModel> Parents { get; } = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsCreating))]
    [NotifyPropertyChangedFor(nameof(IsEditing))]
    private int? _editingId;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    private bool _isLoaded;

    [ObservableProperty]
    [NotifyDataErrorInfo]
    [Required(ErrorMessage = "Назва категорії обов'язкова.")]
    [MaxLength(128, ErrorMessage = "Назва категорії має містити не більше 128 символів.")]
    private string _name = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(KindDisplay))]
    private string _kind = "Income";

    [ObservableProperty]
    private OptionModel? _selectedParent;

    [ObservableProperty]
    private bool _isActive = true;

    public bool IsCreating => EditingId is null;
    public bool IsEditing => EditingId is not null;
    public string KindDisplay => Kind == "Income" ? "Дохід" : "Витрата";

    public Action? Saved { get; set; }

    public async Task InitializeForCreateAsync(string kind, int? parentId, CancellationToken cancellationToken)
    {
        EditingId = null;
        Kind = kind;
        IsActive = true;

        var categories = await LoadCategoriesAsync(includeInactive: false, cancellationToken);
        if (categories is null)
        {
            return;
        }

        FillParents(CategoryPaths.BuildOptions(categories, _ => true));
        SelectedParent = Parents.FirstOrDefault(p => p.Id == parentId) ?? RootOption;
        IsLoaded = true;
    }

    public async Task InitializeForEditAsync(CategoryTreeNodeModel node, CancellationToken cancellationToken)
    {
        EditingId = node.Id;
        Name = node.Name;
        Kind = node.Kind;
        IsActive = node.IsActive;

        var categories = await LoadCategoriesAsync(includeInactive: true, cancellationToken);
        if (categories is null)
        {
            return;
        }

        var excluded = CategoryPaths.GetDescendantIds(categories, node.Id);
        excluded.Add(node.Id);
        var current = categories.FirstOrDefault(c => c.Id == node.Id);

        FillParents(CategoryPaths.BuildOptions(categories, c => !excluded.Contains(c.Id)));
        SelectedParent = Parents.FirstOrDefault(p => p.Id == current?.ParentCategoryId) ?? RootOption;
        IsLoaded = true;
    }

    private async Task<IReadOnlyList<CategoryModel>?> LoadCategoriesAsync(bool includeInactive, CancellationToken cancellationToken)
    {
        try
        {
            return await api.GetCategoriesAsync(Kind, includeInactive, cancellationToken);
        }
        catch (Exception ex) when (ex is ApiException or HttpRequestException or TaskCanceledException)
        {
            SetErrorFromException(ex);
            return null;
        }
    }

    private void FillParents(IEnumerable<OptionModel> options)
    {
        Parents.Clear();
        Parents.Add(RootOption);
        foreach (var option in options)
        {
            Parents.Add(option);
        }
    }

    [RelayCommand(CanExecute = nameof(CanSave))]
    private async Task SaveAsync(CancellationToken cancellationToken)
    {
        ClearError();
        if (!TryValidate())
        {
            return;
        }

        try
        {
            IsBusy = true;
            var parentId = SelectedParent?.Id;
            if (EditingId is { } id)
            {
                await api.UpdateCategoryAsync(id, Name.Trim(), parentId, IsActive, cancellationToken);
            }
            else
            {
                await api.CreateCategoryAsync(Name.Trim(), Kind, parentId, cancellationToken);
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

    private bool CanSave() => !IsBusy && IsLoaded;

    protected override void OnIsBusyChanged(bool value) => SaveCommand.NotifyCanExecuteChanged();
}
