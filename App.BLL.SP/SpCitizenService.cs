using App.Core;
using Microsoft.Data.SqlClient;
using System.Data;
using System.Security.Cryptography;
using System.Text;

namespace App.BLL.SP;

public class SpCitizenService : ICitizenService
{
    private readonly string _connectionString;

    public SpCitizenService(string connectionString)
    {
        _connectionString = connectionString;
    }

    // ============================================
    // REGISTRATION
    // ============================================

    public async Task<string> RegisterCitizenAsync(CitizenRegistrationDto dto)
    {
        using var conn = new SqlConnection(_connectionString);
        using var cmd = new SqlCommand("WasteManagement.sp_RegisterCitizen", conn)
        {
            CommandType = CommandType.StoredProcedure
        };

        var passwordHash = HashPassword(dto.Password);

        cmd.Parameters.AddWithValue("@UserID", dto.CNIC);
        cmd.Parameters.AddWithValue("@PasswordHash", passwordHash);
        cmd.Parameters.AddWithValue("@FullName", dto.FullName);
        cmd.Parameters.AddWithValue("@PhoneNumber", dto.PhoneNumber);
        cmd.Parameters.AddWithValue("@AreaID", dto.AreaID);
        cmd.Parameters.AddWithValue("@Address", dto.Address ?? (object)DBNull.Value);

        var resultParam = new SqlParameter("@ResultMessage", SqlDbType.VarChar, 255)
        {
            Direction = ParameterDirection.Output
        };
        cmd.Parameters.Add(resultParam);

        await conn.OpenAsync();
        var returnValue = await cmd.ExecuteNonQueryAsync();

        var resultMessage = resultParam.Value as string;
        if (resultMessage?.Contains("successfully") == true)
        {
            return dto.CNIC;
        }

        throw new InvalidOperationException(resultMessage ?? "Registration failed");
    }

    // ============================================
    // WASTE LISTINGS
    // ============================================

    public async Task<int> CreateWasteListingAsync(CreateListingDto dto)
    {
        using var conn = new SqlConnection(_connectionString);
        using var cmd = new SqlCommand("WasteManagement.sp_CreateWasteListing", conn)
        {
            CommandType = CommandType.StoredProcedure
        };

        cmd.Parameters.AddWithValue("@CitizenID", dto.CitizenID);
        cmd.Parameters.AddWithValue("@CategoryID", dto.CategoryID);
        cmd.Parameters.AddWithValue("@Weight", dto.Weight);

        var listingIDParam = new SqlParameter("@ListingID", SqlDbType.Int)
        {
            Direction = ParameterDirection.Output
        };
        var estimatedPriceParam = new SqlParameter("@EstimatedPrice", SqlDbType.Decimal)
        {
            Direction = ParameterDirection.Output,
            Precision = 10,
            Scale = 2
        };

        cmd.Parameters.Add(listingIDParam);
        cmd.Parameters.Add(estimatedPriceParam);

        await conn.OpenAsync();
        await cmd.ExecuteNonQueryAsync();

        return (int)listingIDParam.Value;
    }

    public async Task<List<WasteListing>> GetMyListingsAsync(string citizenID)
    {
        var listings = new List<WasteListing>();

        using var conn = new SqlConnection(_connectionString);
        using var cmd = new SqlCommand("WasteManagement.sp_GetCitizenListings", conn)
        {
            CommandType = CommandType.StoredProcedure
        };

        cmd.Parameters.AddWithValue("@CitizenID", citizenID);

        await conn.OpenAsync();
        using var reader = await cmd.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            listings.Add(new WasteListing
            {
                ListingID = reader.GetInt32(reader.GetOrdinal("ListingID")),
                CitizenID = reader.GetString(reader.GetOrdinal("CitizenID")),
                CategoryID = reader.GetInt32(reader.GetOrdinal("CategoryID")),
                Weight = reader.GetDecimal(reader.GetOrdinal("Weight")),
                Status = reader.GetString(reader.GetOrdinal("Status")),
                EstimatedPrice = reader.IsDBNull(reader.GetOrdinal("EstimatedPrice"))
                    ? null
                    : reader.GetDecimal(reader.GetOrdinal("EstimatedPrice")),
                TransactionID = reader.IsDBNull(reader.GetOrdinal("TransactionID"))
                    ? null
                    : reader.GetInt32(reader.GetOrdinal("TransactionID")),
                CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt")),
                Category = new Category
                {
                    CategoryID = reader.GetInt32(reader.GetOrdinal("CategoryID")),
                    CategoryName = reader.GetString(reader.GetOrdinal("CategoryName")),
                    BasePricePerKg = reader.GetDecimal(reader.GetOrdinal("BasePricePerKg"))
                }
            });
        }

        return listings;
    }

    public async Task<bool> CancelListingAsync(int listingID, string citizenID)
    {
        using var conn = new SqlConnection(_connectionString);
        using var cmd = new SqlCommand("WasteManagement.sp_CancelListing", conn)
        {
            CommandType = CommandType.StoredProcedure
        };

        cmd.Parameters.AddWithValue("@ListingID", listingID);
        cmd.Parameters.AddWithValue("@CitizenID", citizenID);

        var successParam = new SqlParameter("@Success", SqlDbType.Bit)
        {
            Direction = ParameterDirection.Output
        };
        cmd.Parameters.Add(successParam);

        await conn.OpenAsync();
        await cmd.ExecuteNonQueryAsync();

        return (bool)successParam.Value;
    }

    // ============================================
    // PRICE ESTIMATION
    // ============================================

    public async Task<decimal> CalculatePriceAsync(int categoryID, decimal weight)
    {
        using var conn = new SqlConnection(_connectionString);
        using var cmd = new SqlCommand("SELECT WasteManagement.fn_CalculatePrice(@CategoryID, @Weight)", conn);

        cmd.Parameters.AddWithValue("@CategoryID", categoryID);
        cmd.Parameters.AddWithValue("@Weight", weight);

        await conn.OpenAsync();
        var result = await cmd.ExecuteScalarAsync();

        return result != null ? Convert.ToDecimal(result) : 0;
    }

    public async Task<PriceEstimateDto> GetPriceEstimateAsync(int categoryID, decimal weight)
    {
        using var conn = new SqlConnection(_connectionString);
        using var cmd = new SqlCommand(
            "SELECT CategoryID, CategoryName, BasePricePerKg FROM WasteManagement.Category WHERE CategoryID = @CategoryID",
            conn);

        cmd.Parameters.AddWithValue("@CategoryID", categoryID);

        await conn.OpenAsync();
        using var reader = await cmd.ExecuteReaderAsync();

        if (!await reader.ReadAsync())
        {
            throw new ArgumentException("Category not found");
        }

        var category = new
        {
            CategoryID = reader.GetInt32(0),
            CategoryName = reader.GetString(1),
            BasePricePerKg = reader.GetDecimal(2)
        };

        await reader.CloseAsync();

        var price = await CalculatePriceAsync(categoryID, weight);

        return new PriceEstimateDto
        {
            CategoryID = category.CategoryID,
            CategoryName = category.CategoryName,
            Weight = weight,
            BasePricePerKg = category.BasePricePerKg,
            EstimatedPrice = price
        };
    }

    // ============================================
    // TRANSACTIONS
    // ============================================

    public async Task<List<TransactionRecord>> GetMyTransactionsAsync(string citizenID)
    {
        var transactions = new List<TransactionRecord>();

        using var conn = new SqlConnection(_connectionString);
        using var cmd = new SqlCommand("WasteManagement.sp_GetCitizenTransactions", conn)
        {
            CommandType = CommandType.StoredProcedure
        };

        cmd.Parameters.AddWithValue("@CitizenID", citizenID);

        await conn.OpenAsync();
        using var reader = await cmd.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            transactions.Add(new TransactionRecord
            {
                TransactionID = reader.GetInt32(reader.GetOrdinal("TransactionID")),
                CitizenID = reader.GetString(reader.GetOrdinal("CitizenID")),
                OperatorID = reader.IsDBNull(reader.GetOrdinal("OperatorID"))
                    ? null
                    : reader.GetString(reader.GetOrdinal("OperatorID")),
                TotalAmount = reader.GetDecimal(reader.GetOrdinal("TotalAmount")),
                PaymentStatus = reader.GetString(reader.GetOrdinal("PaymentStatus")),
                PaymentMethod = reader.IsDBNull(reader.GetOrdinal("PaymentMethod"))
                    ? null
                    : reader.GetString(reader.GetOrdinal("PaymentMethod")),
                VerificationCode = reader.IsDBNull(reader.GetOrdinal("VerificationCode"))
                    ? null
                    : reader.GetString(reader.GetOrdinal("VerificationCode")),
                TransactionDate = reader.GetDateTime(reader.GetOrdinal("TransactionDate"))
            });
        }

        return transactions;
    }

    // ============================================
    // PROFILE
    // ============================================

    public async Task<CitizenProfileView?> GetMyProfileAsync(string citizenID)
    {
        using var conn = new SqlConnection(_connectionString);
        using var cmd = new SqlCommand(
            "SELECT * FROM WasteManagement.vw_CitizenProfile WHERE CitizenID = @CitizenID",
            conn);

        cmd.Parameters.AddWithValue("@CitizenID", citizenID);

        await conn.OpenAsync();
        using var reader = await cmd.ExecuteReaderAsync();

        if (!await reader.ReadAsync())
            return null;

        return new CitizenProfileView
        {
            CitizenID = reader.GetString(reader.GetOrdinal("CitizenID")),
            FullName = reader.GetString(reader.GetOrdinal("FullName")),
            PhoneNumber = reader.GetString(reader.GetOrdinal("PhoneNumber")),
            Address = reader.IsDBNull(reader.GetOrdinal("Address"))
                ? null
                : reader.GetString(reader.GetOrdinal("Address")),
            AreaID = reader.GetInt32(reader.GetOrdinal("AreaID")),
            AreaName = reader.GetString(reader.GetOrdinal("AreaName")),
            City = reader.GetString(reader.GetOrdinal("City")),
            MemberSince = reader.GetDateTime(reader.GetOrdinal("MemberSince"))
        };
    }

    // ============================================
    // REFERENCE DATA
    // ============================================

    public async Task<List<Area>> GetAreasAsync()
    {
        var areas = new List<Area>();

        using var conn = new SqlConnection(_connectionString);
        using var cmd = new SqlCommand("WasteManagement.sp_GetAllAreas", conn)
        {
            CommandType = CommandType.StoredProcedure
        };

        await conn.OpenAsync();
        using var reader = await cmd.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            areas.Add(new Area
            {
                AreaID = reader.GetInt32(reader.GetOrdinal("AreaID")),
                AreaName = reader.GetString(reader.GetOrdinal("AreaName")),
                City = reader.GetString(reader.GetOrdinal("City"))
            });
        }

        return areas;
    }

    public async Task<List<Category>> GetActiveCategoriesAsync()
    {
        var categories = new List<Category>();

        using var conn = new SqlConnection(_connectionString);
        using var cmd = new SqlCommand("WasteManagement.sp_GetAllCategories", conn)
        {
            CommandType = CommandType.StoredProcedure
        };

        await conn.OpenAsync();
        using var reader = await cmd.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            categories.Add(new Category
            {
                CategoryID = reader.GetInt32(reader.GetOrdinal("CategoryID")),
                CategoryName = reader.GetString(reader.GetOrdinal("CategoryName")),
                BasePricePerKg = reader.GetDecimal(reader.GetOrdinal("BasePricePerKg")),
                Description = reader.IsDBNull(reader.GetOrdinal("Description"))
                    ? null
                    : reader.GetString(reader.GetOrdinal("Description"))
            });
        }

        return categories;
    }

    // ============================================
    // HELPER METHODS
    // ============================================

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
