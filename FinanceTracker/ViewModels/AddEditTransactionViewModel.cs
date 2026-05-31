using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FinanceTracker.Models;
using FinanceTracker.Services;

namespace FinanceTracker.ViewModels;

[QueryProperty(nameof(EditTransaction), "Transaction")]
public partial class AddEditTransactionViewModel : ObservableObject
{
    private readonly ITransactionService _transactionService;
    private readonly ICategoryService _categoryService;
    private readonly IAuthService _authService;

    [ObservableProperty]
    private decimal _amount;

    [ObservableProperty]
    private DateTime _date = DateTime.Today;

    [ObservableProperty]
    private string _note = string.Empty;

    [ObservableProperty]
    private Category? _selectedCategory;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsIncomeSelected))]
    [NotifyPropertyChangedFor(nameof(IsExpenseSelected))]
    private TransactionType _selectedType = TransactionType.Expense;

    public bool IsIncomeSelected => SelectedType == TransactionType.Income;
    public bool IsExpenseSelected => SelectedType == TransactionType.Expense;

    [ObservableProperty]
    private ObservableCollection<Category> _categories = new();

    [ObservableProperty]
    private bool _isEditing;

    [ObservableProperty]
    private string _pageTitle = "Add Transaction";

    private Transaction? _editTransaction;
    public Transaction? EditTransaction
    {
        get => _editTransaction;
        set
        {
            _editTransaction = value;
            if (value != null)
            {
                IsEditing = true;
                PageTitle = "Edit Transaction";
                Amount = value.Amount;
                Date = value.Date;
                Note = value.Note ?? string.Empty;
                SelectedType = value.Type;
                SelectedCategory = Categories.FirstOrDefault(c => c.Id == value.CategoryId);
            }
        }
    }

    public List<TransactionType> TransactionTypes { get; } =
        Enum.GetValues<TransactionType>().ToList();

    public AddEditTransactionViewModel(
        ITransactionService transactionService,
        ICategoryService categoryService,
        IAuthService authService)
    {
        _transactionService = transactionService;
        _categoryService = categoryService;
        _authService = authService;
    }

    [RelayCommand]
    public async Task LoadCategoriesAsync()
    {
        var userId = _authService.CurrentUser?.Id;
        var cats = await _categoryService.GetCategoriesAsync(userId);
        // Update in-place to avoid rebinding the Picker ItemsSource mid-interaction.
        Categories.Clear();
        foreach (var category in cats)
        {
            Categories.Add(category);
        }

        // Restore selection after loading categories for edit mode
        if (_editTransaction != null)
        {
            SelectedCategory = Categories.FirstOrDefault(c => c.Id == _editTransaction.CategoryId);
        }
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        if (_authService.CurrentUser == null) return;

        if (Amount <= 0)
        {
            await Shell.Current.DisplayAlertAsync("Error", "Amount must be greater than zero.", "OK");
            return;
        }

        if (SelectedCategory == null)
        {
            await Shell.Current.DisplayAlertAsync("Error", "Please select a category.", "OK");
            return;
        }

        if (IsEditing && _editTransaction != null)
        {
            _editTransaction.Amount = Amount;
            _editTransaction.Date = Date;
            _editTransaction.Note = string.IsNullOrWhiteSpace(Note) ? null : Note;
            _editTransaction.Type = SelectedType;
            _editTransaction.CategoryId = SelectedCategory.Id;
            await _transactionService.UpdateTransactionAsync(_editTransaction);
        }
        else
        {
            var transaction = new Transaction
            {
                UserId = _authService.CurrentUser.Id,
                Amount = Amount,
                Date = Date,
                Note = string.IsNullOrWhiteSpace(Note) ? null : Note,
                Type = SelectedType,
                CategoryId = SelectedCategory.Id
            };
            await _transactionService.AddTransactionAsync(transaction);
        }

        await Shell.Current.GoToAsync("..");
    }

    [RelayCommand]
    private async Task Cancel()
    {
        await Shell.Current.GoToAsync("..");
    }

    [RelayCommand]
    private void SetType(string type)
    {
        if (Enum.TryParse<TransactionType>(type, out var parsed))
        {
            SelectedType = parsed;
        }
    }
}
