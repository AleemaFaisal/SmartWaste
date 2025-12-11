using ReactiveUI;
using System;
using System.Threading.Tasks;

namespace App.UI.ViewModels;

/// <summary>
/// Base ViewModel with common functionality for error handling and busy states
/// </summary>
public class ViewModelBase : ReactiveObject
{
    private string? _errorMessage;
    private string? _successMessage;
    private bool _isBusy;

    /// <summary>
    /// Error message to display to user
    /// </summary>
    public string? ErrorMessage
    {
        get => _errorMessage;
        set => this.RaiseAndSetIfChanged(ref _errorMessage, value);
    }

    /// <summary>
    /// Success message to display to user
    /// </summary>
    public string? SuccessMessage
    {
        get => _successMessage;
        set => this.RaiseAndSetIfChanged(ref _successMessage, value);
    }

    /// <summary>
    /// Indicates if the ViewModel is busy performing an operation
    /// </summary>
    public bool IsBusy
    {
        get => _isBusy;
        set => this.RaiseAndSetIfChanged(ref _isBusy, value);
    }

    /// <summary>
    /// Clear all messages
    /// </summary>
    protected void ClearMessages()
    {
        ErrorMessage = null;
        SuccessMessage = null;
    }

    /// <summary>
    /// Clear the error message
    /// </summary>
    protected void ClearError()
    {
        ErrorMessage = null;
    }

    /// <summary>
    /// Clear the success message
    /// </summary>
    protected void ClearSuccess()
    {
        SuccessMessage = null;
    }

    /// <summary>
    /// Execute an async operation with error handling and busy state management
    /// </summary>
    protected async Task ExecuteAsync(Func<Task> action, string? errorPrefix = null)
    {
        try
        {
            IsBusy = true;
            ClearMessages();
            await action();
        }
        catch (Exception ex)
        {
            ErrorMessage = errorPrefix != null
                ? $"{errorPrefix}: {ex.Message}"
                : ex.Message;
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>
    /// Execute an async operation that returns a value with error handling
    /// </summary>
    protected async Task<T?> ExecuteAsync<T>(Func<Task<T>> func, string? errorPrefix = null)
    {
        try
        {
            IsBusy = true;
            ClearMessages();
            return await func();
        }
        catch (Exception ex)
        {
            ErrorMessage = errorPrefix != null
                ? $"{errorPrefix}: {ex.Message}"
                : ex.Message;
            return default;
        }
        finally
        {
            IsBusy = false;
        }
    }
}