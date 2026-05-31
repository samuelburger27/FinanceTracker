using FinanceTracker.Services;

namespace FinanceTracker.Views;

public partial class AppShell : Shell
{
    private readonly IAuthService _authService;
    private readonly ITransactionService _transactionService;
    private readonly ICategoryService _categoryService;

    public AppShell(
        IAuthService authService,
        ITransactionService transactionService,
        ICategoryService categoryService)
    {
        InitializeComponent();

        _authService = authService;
        _transactionService = transactionService;
        _categoryService = categoryService;

        Routing.RegisterRoute("AddEditTransactionPage", typeof(AddEditTransactionPage));
    }

    private async void OnLogoutClicked(object? sender, EventArgs e)
    {
        _transactionService.ClearCache();
        _categoryService.ClearCache();
        _authService.Logout();
        await GoToAsync("//LoginPage");
    }
}
