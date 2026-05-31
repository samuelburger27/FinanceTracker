using FinanceTracker.Services;

namespace FinanceTracker.Views;

public partial class App : Application
{
    private readonly IAuthService _authService;
    private readonly ITransactionService _transactionService;
    private readonly ICategoryService _categoryService;

    public App(
        IAuthService authService,
        ITransactionService transactionService,
        ICategoryService categoryService)
    {
        InitializeComponent();

        _authService = authService;
        _transactionService = transactionService;
        _categoryService = categoryService;
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        var shell = new AppShell(_authService, _transactionService, _categoryService);
        var window = new Window(shell)
        {
            Title = "Expense Manager",
            MinimumWidth = 960,
            MinimumHeight = 640,
            Width = 1180,
            Height = 760,
        };
        return window;
    }
}
