using App.Core;
using App.Factory;
using App.UI.Services;
using ReactiveUI;
using System;
using System.Reactive;
using System.Threading.Tasks;
using Avalonia.Controls;

namespace App.UI.ViewModels;

public class LoginViewModel : ViewModelBase
{
    private readonly string _connectionString;
    private string _cnic = string.Empty;
    private string _password = string.Empty;
    private bool _useEntityFramework = true;
    private string _statusMessage = string.Empty;

    public string CNIC
    {
        get => _cnic;
        set => this.RaiseAndSetIfChanged(ref _cnic, value);
    }

    public string Password
    {
        get => _password;
        set => this.RaiseAndSetIfChanged(ref _password, value);
    }

    public bool UseEntityFramework
    {
        get => _useEntityFramework;
        set => this.RaiseAndSetIfChanged(ref _useEntityFramework, value);
    }

    public string StatusMessage
    {
        get => _statusMessage;
        set => this.RaiseAndSetIfChanged(ref _statusMessage, value);
    }

    public ReactiveCommand<Window, Unit> LoginCommand { get; }

    public LoginViewModel()
    {
        // Load connection string from configuration
        _connectionString = "Server=localhost;Database=SmartWasteDB;User Id=sa;Password=mak@1234;TrustServerCertificate=True;";

        LoginCommand = ReactiveCommand.CreateFromTask<Window>(async (window) => await LoginAsync(window));
    }

    private async Task LoginAsync(Window? loginWindow)
    {
        try
        {
            IsBusy = true;
            StatusMessage = "Logging in...";

            // Validate input
            if (string.IsNullOrWhiteSpace(CNIC))
            {
                StatusMessage = "Please enter your CNIC";
                return;
            }

            if (string.IsNullOrWhiteSpace(Password))
            {
                StatusMessage = "Please enter your password";
                return;
            }

            // Create authentication service using factory
            var authService = ServiceFactory.CreateAuthService(UseEntityFramework, _connectionString);

            // Attempt login
            var result = await authService.LoginAsync(CNIC, Password);

            if (result.Success)
            {
                StatusMessage = "Login successful! Redirecting...";

                // Navigate to appropriate dashboard
                var navigationService = new NavigationService(_connectionString);
                navigationService.NavigateToDashboard(result, UseEntityFramework, loginWindow);
            }
            else
            {
                StatusMessage = $"Login failed: {result.Message ?? "Invalid credentials"}";
                ErrorMessage = result.Message ?? "Invalid CNIC or password";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = "An error occurred during login";
            ErrorMessage = $"Login error: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }
}
