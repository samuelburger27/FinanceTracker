using FinanceTracker.ViewModels;

namespace FinanceTracker.Views;

public partial class AddEditTransactionPage : ContentPage
{
    private readonly AddEditTransactionViewModel _viewModel;

    private bool _didInitialLoad;

    public AddEditTransactionPage(AddEditTransactionViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
        _viewModel = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        if (_didInitialLoad)
        {
            return;
        }

        try
        {
            IsEnabled = false;
            await _viewModel.LoadCategoriesAsync();
            _didInitialLoad = true;
        }
        finally
        {
            IsEnabled = true;
        }

        Dispatcher.Dispatch(() => AmountEntry.Focus());
    }
}
