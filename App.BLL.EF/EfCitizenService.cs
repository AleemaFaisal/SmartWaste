using App.Core;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;
using System.Diagnostics;



namespace App.BLL.EF;

public class EfCitizenService : ICitizenService
{
    private readonly AppDbContext _db;

    public EfCitizenService(AppDbContext db)
    {
        _db = db;
    }

    // ============================================
    // REGISTRATION
    // ============================================

    public async Task<string> RegisterCitizenAsync(CitizenRegistrationDto dto)
    {
        using var transaction = await _db.Database.BeginTransactionAsync();
        try
        {
            // Validate CNIC format
            if (!App.Core.Validation.ValidationHelper.ValidateCNIC(dto.CNIC))
            {
                throw new ArgumentException("Invalid CNIC format");
            }

            // Check if user already exists
            var existingUser = await _db.Users.FindAsync(dto.CNIC);
            if (existingUser != null)
            {
                throw new InvalidOperationException("User with this CNIC already exists");
            }

            // Hash password
            var passwordHash = HashPassword(dto.Password);

            // Create user account
            var user = new User
            {
                UserID = dto.CNIC,
                PasswordHash = passwordHash,
                RoleID = 2, // Citizen role
                CreatedAt = DateTime.Now
            };

            _db.Users.Add(user);
            await _db.SaveChangesAsync();

            // Create citizen profile
            var citizen = new Citizen
            {
                CitizenID = dto.CNIC,
                FullName = dto.FullName,
                PhoneNumber = dto.PhoneNumber,
                AreaID = dto.AreaID,
                Address = dto.Address
            };

            _db.Citizens.Add(citizen);
            await _db.SaveChangesAsync();

            await transaction.CommitAsync();
            return citizen.CitizenID;
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    // ============================================
    // WASTE LISTINGS
    // ============================================

    public async Task<int> CreateWasteListingAsync(CreateListingDto dto)
    {
        DebugLogger.LogSeparator();
        DebugLogger.Log("EfCitizenService.CreateWasteListingAsync START");
        DebugLogger.Log($"DTO - CitizenID: {dto.CitizenID}, CategoryID: {dto.CategoryID}, Weight: {dto.Weight}");

        try
        {
            DebugLogger.Log("Calculating price using database function...");

            // Calculate price using database function with proper column alias
            var result = await _db.Database
                .SqlQueryRaw<decimal>("SELECT WasteManagement.fn_CalculatePrice({0}, {1}) AS Value", dto.CategoryID, dto.Weight)
                .ToListAsync();

            var estimatedPrice = result.FirstOrDefault();
            DebugLogger.Log($"Estimated price calculated: {estimatedPrice}");

            // Set CreatedAt explicitly since it's part of composite key
            var createdAt = DateTime.Now;
            DebugLogger.Log($"CreatedAt: {createdAt}");

            DebugLogger.Log("Preparing INSERT command...");

            // Use a stored procedure-like approach to insert and get ID
            using var command = _db.Database.GetDbConnection().CreateCommand();

            command.CommandText = @"
                INSERT INTO WasteManagement.WasteListing
                    (CitizenID, CategoryID, Weight, EstimatedPrice, Status, CreatedAt)
                VALUES
                    (@CitizenID, @CategoryID, @Weight, @EstimatedPrice, @Status, @CreatedAt);
                SELECT CAST(SCOPE_IDENTITY() as int);";

            command.Parameters.Add(new Microsoft.Data.SqlClient.SqlParameter("@CitizenID", dto.CitizenID));
            command.Parameters.Add(new Microsoft.Data.SqlClient.SqlParameter("@CategoryID", dto.CategoryID));
            command.Parameters.Add(new Microsoft.Data.SqlClient.SqlParameter("@Weight", dto.Weight));
            command.Parameters.Add(new Microsoft.Data.SqlClient.SqlParameter("@EstimatedPrice", estimatedPrice));
            command.Parameters.Add(new Microsoft.Data.SqlClient.SqlParameter("@Status", "Pending"));
            command.Parameters.Add(new Microsoft.Data.SqlClient.SqlParameter("@CreatedAt", createdAt));

            DebugLogger.Log("Executing INSERT command...");

            await _db.Database.OpenConnectionAsync();
            var scalarResult = await command.ExecuteScalarAsync();
            await _db.Database.CloseConnectionAsync();

            var listingId = scalarResult != null ? Convert.ToInt32(scalarResult) : 0;

            DebugLogger.Log($"INSERT successful! ListingID returned: {listingId}");
            DebugLogger.Log("EfCitizenService.CreateWasteListingAsync COMPLETED");

            return listingId;
        }
        catch (Exception ex)
        {
            DebugLogger.Log($"ERROR in CreateWasteListingAsync: {ex.Message}");
            DebugLogger.Log($"Stack trace: {ex.StackTrace}");
            throw new InvalidOperationException($"Failed to create waste listing: {ex.Message}", ex);
        }
    }

    public async Task<List<ListingDto>> GetMyListingsAsync(string citizenID)
    {
        DebugLogger.LogSeparator();
        DebugLogger.Log($"EfCitizenService.GetMyListingsAsync START");
        DebugLogger.Log($"CitizenID parameter: '{citizenID}'");

        try
        {
            DebugLogger.Log("Executing SQL query...");

            // Use FromSqlRaw to bypass composite key issues, then eagerly load Category
            var listings = await _db.WasteListings
                .FromSqlRaw(@"
                    SELECT
                        ListingID,
                        CreatedAt,
                        CitizenID,
                        CategoryID,
                        Weight,
                        Status,
                        EstimatedPrice,
                        TransactionID
                    FROM WasteManagement.WasteListing
                    WHERE CitizenID = {0}", citizenID)
                .AsNoTracking()
                .Include(w => w.Category)
                .OrderByDescending(w => w.CreatedAt)
                .ToListAsync();

            DebugLogger.Log($"Query executed successfully. Returned {listings.Count} listings");

            // Map to DTOs to avoid circular reference issues
            var listingDtos = listings.Select(l => new ListingDto
            {
                ListingID = l.ListingID,
                CreatedAt = l.CreatedAt,
                CitizenID = l.CitizenID,
                CategoryID = l.CategoryID,
                CategoryName = l.Category?.CategoryName ?? "",
                Weight = l.Weight,
                Status = l.Status,
                EstimatedPrice = l.EstimatedPrice,
                TransactionID = l.TransactionID
            }).ToList();

            if (listingDtos.Count == 0)
            {
                DebugLogger.Log("WARNING: No listings found for this CitizenID");
            }
            else
            {
                DebugLogger.Log("Listing details:");
                foreach (var listing in listingDtos)
                {
                    DebugLogger.Log($"  - ListingID: {listing.ListingID}");
                    DebugLogger.Log($"    CitizenID: {listing.CitizenID}");
                    DebugLogger.Log($"    CategoryID: {listing.CategoryID}");
                    DebugLogger.Log($"    CategoryName: {listing.CategoryName}");
                    DebugLogger.Log($"    Weight: {listing.Weight}");
                    DebugLogger.Log($"    Status: {listing.Status}");
                    DebugLogger.Log($"    EstimatedPrice: {listing.EstimatedPrice}");
                    DebugLogger.Log($"    CreatedAt: {listing.CreatedAt}");
                }
            }

            DebugLogger.Log("EfCitizenService.GetMyListingsAsync COMPLETED");
            return listingDtos;
        }
        catch (Exception ex)
        {
            DebugLogger.Log($"ERROR in GetMyListingsAsync: {ex.Message}");
            DebugLogger.Log($"Stack trace: {ex.StackTrace}");
            throw;
        }
    }

    public async Task<bool> CancelListingAsync(int listingID, string citizenID)
    {
        DebugLogger.LogSeparator();
        DebugLogger.Log($"EfCitizenService.CancelListingAsync START");
        DebugLogger.Log($"ListingID: {listingID}, CitizenID: '{citizenID}'");

        try
        {
            // Use raw SQL to update status - avoids composite key issues with EF change tracking
            var rowsAffected = await _db.Database.ExecuteSqlRawAsync(
                @"UPDATE WasteManagement.WasteListing
                  SET Status = 'Cancelled'
                  WHERE ListingID = {0} AND CitizenID = {1} AND Status = 'Pending'",
                listingID, citizenID);

            if (rowsAffected == 0)
            {
                DebugLogger.Log("ERROR: No rows affected - listing not found, doesn't belong to citizen, or not in Pending status");
                return false;
            }

            DebugLogger.Log($"SUCCESS: Listing cancelled - {rowsAffected} row(s) updated");
            return true;
        }
        catch (Exception ex)
        {
            DebugLogger.Log($"ERROR in CancelListingAsync: {ex.Message}");
            return false;
        }
    }

    // ============================================
    // PRICE ESTIMATION
    // ============================================

    public async Task<decimal> CalculatePriceAsync(int categoryID, decimal weight)
    {
        try
        {
            var result = await _db.Database
                .SqlQueryRaw<decimal>("SELECT WasteManagement.fn_CalculatePrice({0}, {1}) AS Value", categoryID, weight)
                .ToListAsync();

            return result.FirstOrDefault();
        }
        catch
        {
            return 0;
        }
    }

    public async Task<PriceEstimateDto> GetPriceEstimateAsync(int categoryID, decimal weight)
    {
        var category = await _db.Categories.FindAsync(categoryID);
        if (category == null)
        {
            throw new ArgumentException("Category not found");
        }

        var price = await CalculatePriceAsync(categoryID, weight);

        return new PriceEstimateDto
        {
            CategoryID = categoryID,
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
        return await _db.TransactionRecords
            .Where(t => t.CitizenID == citizenID)
            .OrderByDescending(t => t.TransactionDate)
            .ToListAsync();
    }

    // ============================================
    // PROFILE
    // ============================================

    public async Task<CitizenProfileView?> GetMyProfileAsync(string citizenID)
    {
        return await _db.CitizenProfiles
            .FirstOrDefaultAsync(p => p.CitizenID == citizenID);
    }

    // ============================================
    // REFERENCE DATA
    // ============================================

    public async Task<List<Area>> GetAreasAsync()
    {
        return await _db.Areas
            .OrderBy(a => a.City)
            .ThenBy(a => a.AreaName)
            .ToListAsync();
    }

    public async Task<List<Category>> GetActiveCategoriesAsync()
    {
        return await _db.Categories
            .OrderBy(c => c.CategoryName)
            .ToListAsync();
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
