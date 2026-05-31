using System.Collections.ObjectModel;
using System.Text;
using CommunityToolkit.Maui.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FinanceTracker.Models;
using FinanceTracker.Services;

namespace FinanceTracker.ViewModels;

public enum TransactionFilterType
{
    All,
    Income,
    Expense
}

public partial class TransactionListViewModel : ObservableObject
{
    private const int PageSize = 20;

    private readonly ITransactionService _transactionService;
    private readonly ICategoryService _categoryService;
    private readonly IAuthService _authService;
    private readonly ICsvService _csvService;

    private IReadOnlyList<Transaction> _allTransactions = Array.Empty<Transaction>();
    private List<Transaction> _filteredTransactions = new();
    private int _batchDepth;
    private bool IsBatching => _batchDepth > 0;

    [ObservableProperty]
    private ObservableCollection<Transaction> _transactions = new();

    [ObservableProperty]
    private decimal _balance;

    [ObservableProperty]
    private decimal _monthlyIncome;

    [ObservableProperty]
    private decimal _monthlyExpenses;

    // ----- Filters -----

    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    private string _minAmountText = string.Empty;

    [ObservableProperty]
    private string _maxAmountText = string.Empty;

    [ObservableProperty]
    private DateTime _startDate = DateTime.Today.AddYears(-1);

    [ObservableProperty]
    private DateTime _endDate = DateTime.Today;

    [ObservableProperty]
    private bool _isDateFilterEnabled;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsFilterTypeAll))]
    [NotifyPropertyChangedFor(nameof(IsFilterTypeIncome))]
    [NotifyPropertyChangedFor(nameof(IsFilterTypeExpense))]
    private TransactionFilterType _selectedFilterType = TransactionFilterType.All;

    public bool IsFilterTypeAll => SelectedFilterType == TransactionFilterType.All;
    public bool IsFilterTypeIncome => SelectedFilterType == TransactionFilterType.Income;
    public bool IsFilterTypeExpense => SelectedFilterType == TransactionFilterType.Expense;

    [ObservableProperty]
    private ObservableCollection<Category> _filterCategories = new();

    [ObservableProperty]
    private Category? _selectedFilterCategory;

    // ----- Pagination -----

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PageDisplay))]
    [NotifyPropertyChangedFor(nameof(CanGoPrevious))]
    [NotifyPropertyChangedFor(nameof(CanGoNext))]
    [NotifyPropertyChangedFor(nameof(HasMultiplePages))]
    [NotifyCanExecuteChangedFor(nameof(NextPageCommand))]
    [NotifyCanExecuteChangedFor(nameof(PreviousPageCommand))]
    private int _currentPage = 1;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PageDisplay))]
    [NotifyPropertyChangedFor(nameof(CanGoNext))]
    [NotifyPropertyChangedFor(nameof(HasMultiplePages))]
    [NotifyCanExecuteChangedFor(nameof(NextPageCommand))]
    private int _totalPages = 1;

    [ObservableProperty]
    private int _filteredCount;

    [ObservableProperty]
    private bool _isBusy;

    public string PageDisplay => $"Page {CurrentPage} of {TotalPages}";
    public bool CanGoPrevious => CurrentPage > 1;
    public bool CanGoNext => CurrentPage < TotalPages;
    public bool HasMultiplePages => TotalPages > 1;

    public TransactionListViewModel(
        ITransactionService transactionService,
        ICategoryService categoryService,
        IAuthService authService,
        ICsvService csvService)
    {
        _transactionService = transactionService;
        _categoryService = categoryService;
        _authService = authService;
        _csvService = csvService;
    }

    [RelayCommand]
    public async Task LoadDataAsync()
    {
        if (_authService.CurrentUser == null) return;
        if (IsBusy) return;

        IsBusy = true;
        try
        {
            var userId = _authService.CurrentUser.Id;

            var loaded = await Task.Run(async () =>
            {
                var transactions = await _transactionService.GetTransactionsAsync(userId);
                var categories = await _categoryService.GetCategoriesAsync(userId);

                var now = DateTime.Now;
                decimal income = 0, expense = 0, balance = 0;
                foreach (var t in transactions)
                {
                    var signedAmount = t.Type == TransactionType.Income ? t.Amount : -t.Amount;
                    balance += signedAmount;
                    if (t.Date.Month == now.Month && t.Date.Year == now.Year)
                    {
                        if (t.Type == TransactionType.Income) income += t.Amount;
                        else expense += t.Amount;
                    }
                }

                return (transactions, categories, income, expense, balance);
            });

            using (BeginBatch())
            {
                _allTransactions = loaded.transactions;
                MonthlyIncome = loaded.income;
                MonthlyExpenses = loaded.expense;
                Balance = loaded.balance;
                PopulateFilterCategories(loaded.categories);
            }
            ApplyFilters();
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void PopulateFilterCategories(IReadOnlyList<Category> cats)
    {
        var previousId = SelectedFilterCategory?.Id;
        FilterCategories.Clear();
        var allCategory = new Category { Id = 0, Name = "All categories" };
        FilterCategories.Add(allCategory);
        foreach (var c in cats)
            FilterCategories.Add(c);
        SelectedFilterCategory = previousId.HasValue
            ? FilterCategories.FirstOrDefault(c => c.Id == previousId.Value) ?? allCategory
            : allCategory;
    }

    private void ApplyFilters()
    {
        if (IsBatching) return;

        IEnumerable<Transaction> q = _allTransactions;

        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            var s = SearchText.Trim();
            q = q.Where(t => !string.IsNullOrEmpty(t.Note)
                && t.Note.Contains(s, StringComparison.OrdinalIgnoreCase));
        }

        if (decimal.TryParse(MinAmountText, out var min))
            q = q.Where(t => t.Amount >= min);
        if (decimal.TryParse(MaxAmountText, out var max))
            q = q.Where(t => t.Amount <= max);

        if (IsDateFilterEnabled)
        {
            var start = StartDate.Date;
            var end = EndDate.Date.AddDays(1).AddTicks(-1);
            q = q.Where(t => t.Date >= start && t.Date <= end);
        }

        if (SelectedFilterType == TransactionFilterType.Income)
            q = q.Where(t => t.Type == TransactionType.Income);
        else if (SelectedFilterType == TransactionFilterType.Expense)
            q = q.Where(t => t.Type == TransactionType.Expense);

        if (SelectedFilterCategory != null && SelectedFilterCategory.Id != 0)
            q = q.Where(t => t.CategoryId == SelectedFilterCategory.Id);

        _filteredTransactions = q.ToList();
        FilteredCount = _filteredTransactions.Count;

        TotalPages = Math.Max(1, (int)Math.Ceiling(_filteredTransactions.Count / (double)PageSize));
        if (CurrentPage > TotalPages) CurrentPage = TotalPages;
        if (CurrentPage < 1) CurrentPage = 1;

        UpdatePageItems();
    }

    private void UpdatePageItems()
    {
        var skip = (CurrentPage - 1) * PageSize;
        var page = _filteredTransactions.Skip(skip).Take(PageSize).ToList();
        Transactions = new ObservableCollection<Transaction>(page);
    }

    private void ResetPageAndApply()
    {
        if (IsBatching) return;
        CurrentPage = 1;
        ApplyFilters();
    }

    partial void OnSearchTextChanged(string value) => ResetPageAndApply();
    partial void OnMinAmountTextChanged(string value) => ResetPageAndApply();
    partial void OnMaxAmountTextChanged(string value) => ResetPageAndApply();
    partial void OnStartDateChanged(DateTime value) => ResetPageAndApply();
    partial void OnEndDateChanged(DateTime value) => ResetPageAndApply();
    partial void OnIsDateFilterEnabledChanged(bool value) => ResetPageAndApply();
    partial void OnSelectedFilterTypeChanged(TransactionFilterType value) => ResetPageAndApply();
    partial void OnSelectedFilterCategoryChanged(Category? value) => ResetPageAndApply();

    [RelayCommand]
    private void ResetFilters()
    {
        using (BeginBatch())
        {
            SearchText = string.Empty;
            MinAmountText = string.Empty;
            MaxAmountText = string.Empty;
            IsDateFilterEnabled = false;
            StartDate = DateTime.Today.AddYears(-1);
            EndDate = DateTime.Today;
            SelectedFilterType = TransactionFilterType.All;
            if (FilterCategories.Count > 0)
                SelectedFilterCategory = FilterCategories[0];
        }
        ResetPageAndApply();
    }

    [RelayCommand]
    private void SetFilterType(string type)
    {
        if (Enum.TryParse<TransactionFilterType>(type, out var parsed))
            SelectedFilterType = parsed;
    }

    [RelayCommand(CanExecute = nameof(CanGoPrevious))]
    private void PreviousPage()
    {
        if (CurrentPage > 1)
        {
            CurrentPage--;
            UpdatePageItems();
        }
    }

    [RelayCommand(CanExecute = nameof(CanGoNext))]
    private void NextPage()
    {
        if (CurrentPage < TotalPages)
        {
            CurrentPage++;
            UpdatePageItems();
        }
    }

    [RelayCommand]
    private async Task AddTransaction()
    {
        await Shell.Current.GoToAsync("AddEditTransactionPage");
    }

    [RelayCommand]
    private async Task EditTransaction(Transaction transaction)
    {
        var navParams = new Dictionary<string, object> { { "Transaction", transaction } };
        await Shell.Current.GoToAsync("AddEditTransactionPage", navParams);
    }

    [RelayCommand]
    private async Task DeleteTransaction(Transaction transaction)
    {
        bool confirm = await Shell.Current.DisplayAlertAsync(
            "Delete", "Are you sure you want to delete this transaction?", "Yes", "No");

        if (confirm)
        {
            await _transactionService.DeleteTransactionAsync(transaction.Id);
            await LoadDataAsync();
        }
    }

    [RelayCommand]
    private async Task ExportCsv()
    {
        if (_authService.CurrentUser == null) return;
        if (_allTransactions.Count == 0)
        {
            await Shell.Current.DisplayAlertAsync("Export", "No transactions to export.", "OK");
            return;
        }

        try
        {
            var csv = _csvService.ExportToCsv(_allTransactions);
            var fileName = $"finance-tracker-{DateTime.Now:yyyyMMdd-HHmmss}.csv";
            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csv));

            var saveResult = await FileSaver.Default.SaveAsync(fileName, stream, CancellationToken.None);
            if (saveResult.IsSuccessful)
            {
                await Shell.Current.DisplayAlertAsync("Export complete",
                    $"Saved {_allTransactions.Count} transaction(s) to:\n{saveResult.FilePath}", "OK");
            }
            else if (saveResult.Exception is not null and not OperationCanceledException)
            {
                await Shell.Current.DisplayAlertAsync("Export failed", saveResult.Exception.Message, "OK");
            }
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlertAsync("Export failed", ex.Message, "OK");
        }
    }

    [RelayCommand]
    private async Task ImportCsv()
    {
        if (_authService.CurrentUser == null) return;

        try
        {
            var result = await FilePicker.Default.PickAsync(new PickOptions
            {
                PickerTitle = "Select a CSV file",
            });
            if (result == null) return;

            await using var stream = await result.OpenReadAsync();
            var report = await _csvService.ImportFromCsvAsync(stream, _authService.CurrentUser.Id);

            await LoadDataAsync();

            var message = $"Imported {report.Imported} transaction(s).";
            if (report.Skipped > 0) message += $"\nSkipped {report.Skipped} row(s).";
            if (report.Errors.Count > 0)
            {
                var preview = string.Join("\n", report.Errors.Take(5));
                if (report.Errors.Count > 5) preview += $"\n…and {report.Errors.Count - 5} more.";
                message += $"\n\n{preview}";
            }
            await Shell.Current.DisplayAlertAsync("Import complete", message, "OK");
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlertAsync("Import failed", ex.Message, "OK");
        }
    }

    [RelayCommand]
    private async Task Logout()
    {
        _transactionService.ClearCache();
        _categoryService.ClearCache();
        _authService.Logout();
        await Shell.Current.GoToAsync("//LoginPage");
    }

    // Suppresses ApplyFilters/ResetPageAndApply while filter properties are being mutated as a group.
    // Nests safely (e.g. if a future caller already holds a scope) by counting depth.
    private IDisposable BeginBatch() => new BatchScope(this);

    private sealed class BatchScope : IDisposable
    {
        private readonly TransactionListViewModel _vm;
        private bool _disposed;

        public BatchScope(TransactionListViewModel vm)
        {
            _vm = vm;
            _vm._batchDepth++;
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            _vm._batchDepth--;
        }
    }
}
