using App.Core;
using Microsoft.Data.SqlClient;
using System.Data;
using System.Security.Cryptography;
using System.Text;

namespace App.BLL.SP;

public class SpAuthenticationService : IAuthenticationService
{
    private readonly string _connectionString;

    public SpAuthenticationService(string connectionString)
    {
        _connectionString = connectionString;
    }

    public async Task<LoginResult> LoginAsync(string cnic, string password)
    {
        try
        {
            // Hash password for comparison
            var passwordHash = HashPassword(password);

            using var conn = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand("WasteManagement.sp_AuthenticateUser", conn)
            {
                CommandType = CommandType.StoredProcedure
            };

            cmd.Parameters.AddWithValue("@CNIC", cnic);
            cmd.Parameters.AddWithValue("@PasswordHash", passwordHash);

            var userIDParam = new SqlParameter("@UserID", SqlDbType.VarChar, 15) { Direction = ParameterDirection.Output };
            var roleIDParam = new SqlParameter("@RoleID", SqlDbType.Int) { Direction = ParameterDirection.Output };
            var roleNameParam = new SqlParameter("@RoleName", SqlDbType.VarChar, 50) { Direction = ParameterDirection.Output };
            var citizenIDParam = new SqlParameter("@CitizenID", SqlDbType.VarChar, 15) { Direction = ParameterDirection.Output };
            var operatorIDParam = new SqlParameter("@OperatorID", SqlDbType.VarChar, 15) { Direction = ParameterDirection.Output };

            cmd.Parameters.Add(userIDParam);
            cmd.Parameters.Add(roleIDParam);
            cmd.Parameters.Add(roleNameParam);
            cmd.Parameters.Add(citizenIDParam);
            cmd.Parameters.Add(operatorIDParam);

            await conn.OpenAsync();
            await cmd.ExecuteNonQueryAsync();

            var userID = userIDParam.Value as string;
            if (string.IsNullOrEmpty(userID))
            {
                return new LoginResult
                {
                    Success = false,
                    Message = "Invalid CNIC or password"
                };
            }

            return new LoginResult
            {
                Success = true,
                Message = "Login successful",
                UserID = userID,
                RoleID = roleIDParam.Value != DBNull.Value ? (int)roleIDParam.Value : null,
                RoleName = roleNameParam.Value as string,
                CitizenID = citizenIDParam.Value != DBNull.Value ? citizenIDParam.Value as string : null,
                OperatorID = operatorIDParam.Value != DBNull.Value ? operatorIDParam.Value as string : null
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
        try
        {
            using var conn = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand("SELECT WasteManagement.fn_ValidateCNIC(@CNIC)", conn);

            cmd.Parameters.AddWithValue("@CNIC", cnic);

            await conn.OpenAsync();
            var result = await cmd.ExecuteScalarAsync();

            return result != null && Convert.ToInt32(result) == 1;
        }
        catch
        {
            return App.Core.Validation.ValidationHelper.ValidateCNIC(cnic);
        }
    }

    public Task<string> GeneratePasswordHashAsync(string password)
    {
        return Task.FromResult(HashPassword(password));
    }

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
