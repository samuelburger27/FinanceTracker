using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FinanceTracker.Services;

namespace FinanceTracker.ViewModels;

public partial class LoginViewModel : ObservableObject
{
    private readonly IAuthService _authService;

    [ObservableProperty]
    private string _username = string.Empty;

    [ObservableProperty]
    private string _password = string.Empty;

    [ObservableProperty]
    private string _errorMessage = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ActionButtonText))]
    [NotifyPropertyChangedFor(nameof(ToggleButtonText))]
    private bool _isRegistering;

    public string ActionButtonText => IsRegistering ? "Register" : "Login";

    public string ToggleButtonText => IsRegistering
        ? "Already have an account? Login"
        : "Don't have an account? Register";

    public LoginViewModel(IAuthService authService)
    {
        _authService = authService;
    }

    [RelayCommand]
    private async Task SubmitAsync()
    {
        ErrorMessage = string.Empty;

        (bool success, string error) = IsRegistering
            ? await _authService.RegisterAsync(Username, Password)
            : await _authService.LoginAsync(Username, Password);

        if (success)
        {
            await Shell.Current.GoToAsync("//MainPage");
        }
        else
        {
            ErrorMessage = error;
        }
    }

    [RelayCommand]
    private void ToggleMode()
    {
        IsRegistering = !IsRegistering;
        ErrorMessage = string.Empty;
    }
}
