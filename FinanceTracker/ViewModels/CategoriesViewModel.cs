using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FinanceTracker.Models;
using FinanceTracker.Services;

namespace FinanceTracker.ViewModels;

public partial class CategoriesViewModel : ObservableObject
{
    private readonly ICategoryService _categoryService;
    private readonly IAuthService _authService;

    public ObservableCollection<Category> UserCategories { get; } = new();

    public ObservableCollection<Category> PredefinedCategories { get; } = new();

    [ObservableProperty]
    private string _newCategoryName = string.Empty;

    [ObservableProperty]
    private bool _isBusy;

    public bool HasUserCategories => UserCategories.Count > 0;
    public bool HasNoUserCategories => UserCategories.Count == 0;

    public CategoriesViewModel(ICategoryService categoryService, IAuthService authService)
    {
        _categoryService = categoryService;
        _authService = authService;
        UserCategories.CollectionChanged += (_, _) =>
        {
            OnPropertyChanged(nameof(HasUserCategories));
            OnPropertyChanged(nameof(HasNoUserCategories));
        };
    }

    [RelayCommand]
    public async Task LoadAsync()
    {
        if (_authService.CurrentUser == null) return;
        if (IsBusy) return;

        IsBusy = true;
        try
        {
            var userId = _authService.CurrentUser.Id;
            var all = await _categoryService.GetCategoriesAsync(userId);

            // Update in place so the CollectionView keeps its measured layout and
            // re-renders correctly when items go from N → 0 or 0 → N.
            UserCategories.Clear();
            foreach (var c in all.Where(c => c.UserId == userId).OrderBy(c => c.Name))
                UserCategories.Add(c);

            PredefinedCategories.Clear();
            foreach (var c in all.Where(c => c.UserId == null).OrderBy(c => c.Name))
                PredefinedCategories.Add(c);
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task AddCategoryAsync()
    {
        if (_authService.CurrentUser == null) return;

        var name = NewCategoryName.Trim();
        if (string.IsNullOrEmpty(name))
        {
            await Shell.Current.DisplayAlertAsync("Add category", "Please enter a name.", "OK");
            return;
        }

        var (ok, err, _) = await _categoryService.AddCategoryAsync(name, _authService.CurrentUser.Id);
        if (!ok)
        {
            await Shell.Current.DisplayAlertAsync("Add category", err, "OK");
            return;
        }

        NewCategoryName = string.Empty;
        await LoadAsync();
    }

    [RelayCommand]
    private async Task RenameCategoryAsync(Category category)
    {
        if (_authService.CurrentUser == null) return;

        var input = await Shell.Current.DisplayPromptAsync(
            "Rename category", "Enter new name:", "Save",
            initialValue: category.Name, maxLength: 100);
        if (input == null) return;

        var (ok, err) = await _categoryService.RenameCategoryAsync(
            category.Id, input, _authService.CurrentUser.Id);
        if (!ok)
        {
            await Shell.Current.DisplayAlertAsync("Rename category", err, "OK");
            return;
        }
        await LoadAsync();
    }

    [RelayCommand]
    private async Task DeleteCategoryAsync(Category category)
    {
        if (_authService.CurrentUser == null) return;

        var confirm = await Shell.Current.DisplayAlertAsync(
            "Delete category", $"Delete '{category.Name}'?", "Delete", "Cancel");
        if (!confirm) return;

        var (ok, err) = await _categoryService.DeleteCategoryAsync(
            category.Id, _authService.CurrentUser.Id);
        if (!ok)
        {
            await Shell.Current.DisplayAlertAsync("Delete category", err, "OK");
            return;
        }
        await LoadAsync();
    }
}
