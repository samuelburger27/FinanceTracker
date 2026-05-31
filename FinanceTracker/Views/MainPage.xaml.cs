using FinanceTracker.ViewModels;

namespace FinanceTracker.Views;

public partial class MainPage : ContentPage
{
    private readonly TransactionListViewModel _viewModel;

    public MainPage(TransactionListViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
        _viewModel = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.LoadDataAsync();
    }
}