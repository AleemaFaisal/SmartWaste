namespace App.Core;

/// <summary>
/// Authentication service for user login and CNIC validation
/// </summary>
public interface IAuthenticationService
{
    /// <summary>
    /// Authenticate user with CNIC and password
    /// </summary>
    Task<LoginResult> LoginAsync(string cnic, string password);

    /// <summary>
    /// Validate CNIC format using database function
    /// </summary>
    Task<bool> ValidateCNICFormatAsync(string cnic);

    /// <summary>
    /// Generate password hash for storage
    /// </summary>
    Task<string> GeneratePasswordHashAsync(string password);
}
