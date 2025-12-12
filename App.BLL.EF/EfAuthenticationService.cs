using App.Core;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;

namespace App.BLL.EF;

public class EfAuthenticationService : IAuthenticationService
{
    private readonly AppDbContext _db;

    public EfAuthenticationService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<LoginResult> LoginAsync(string cnic, string password)
    {
        try
        {
            // Hash the password for comparison
            var passwordHash = HashPassword(password);
            Console.WriteLine($"EF Login attempt for CNIC: {cnic} with hashed password: {passwordHash} and password: {password}");

            // Find user by CNIC and password hash
            var user = await _db.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.UserID == cnic && u.PasswordHash == passwordHash);

            Console.WriteLine(user != null
                ? $"EF Login successful for user: {user.UserID} with role: {user.Role?.RoleName}"
                : "EF Login failed: User not found or invalid credentials");

            if (user == null)
            {
                return new LoginResult
                {
                    Success = false,
                    Message = "Invalid CNIC or password"
                };
            }

            // Get citizen or operator ID based on role
            string? citizenID = null;
            string? operatorID = null;

            if (user.RoleID == 2) // Citizen
            {
                var citizen = await _db.Citizens
                    .FirstOrDefaultAsync(c => c.CitizenID == cnic);
                citizenID = citizen?.CitizenID;
            }
            else if (user.RoleID == 3) // Operator
            {
                var op = await _db.Operators
                    .FirstOrDefaultAsync(o => o.OperatorID == cnic);
                operatorID = op?.OperatorID;
            }

            return new LoginResult
            {
                Success = true,
                Message = "Login successful",
                UserID = user.UserID,
                RoleID = user.RoleID,
                RoleName = user.Role?.RoleName,
                CitizenID = citizenID,
                OperatorID = operatorID
            };
        }
        catch (Exception ex)
        {
            return new LoginResult
            {
                Success = false,
                Message = $"Login error: {ex.Message}"
            };
        }
    }

    public async Task<bool> ValidateCNICFormatAsync(string cnic)
    {
        // Use database function for validation
        try
        {
            var result = await _db.Database
                .SqlQuery<int>($"SELECT WasteManagement.fn_ValidateCNIC({cnic})")
                .FirstOrDefaultAsync();

            return result == 1;
        }
        catch
        {
            // Fallback to local validation if database function fails
            return App.Core.Validation.ValidationHelper.ValidateCNIC(cnic);
        }
    }

    public Task<string> GeneratePasswordHashAsync(string password)
    {
        return Task.FromResult(HashPassword(password));
    }

    // Helper method to hash password using SHA256
    private string HashPassword(string password)
    {
        using (var sha256 = SHA256.Create())
        {
            var bytes = Encoding.UTF8.GetBytes(password);
            var hash = sha256.ComputeHash(bytes);
            return Convert.ToHexString(hash);
        }
    }
}
