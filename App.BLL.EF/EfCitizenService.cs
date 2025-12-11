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
        try
        {
            // Calculate price using database function with proper column alias
            var result = await _db.Database
                .SqlQueryRaw<decimal>("SELECT WasteManagement.fn_CalculatePrice({0}, {1}) AS Value", dto.CategoryID, dto.Weight)
                .ToListAsync();

            var estimatedPrice = result.FirstOrDefault();

            // Set CreatedAt explicitly since it's part of composite key
            var createdAt = DateTime.Now;

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

            await _db.Database.OpenConnectionAsync();
            var scalarResult = await command.ExecuteScalarAsync();
            await _db.Database.CloseConnectionAsync();

            var listingId = scalarResult != null ? Convert.ToInt32(scalarResult) : 0;

            return listingId;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to create waste listing: {ex.Message}", ex);
        }
    }

    public async Task<List<WasteListing>> GetMyListingsAsync(string citizenID)
    {
        return await _db.WasteListings
            .AsNoTracking()
            .Include(w => w.Category)
            .Where(w => w.CitizenID == citizenID)
            .OrderByDescending(w => w.CreatedAt)
            .ToListAsync();
    }

    public async Task<bool> CancelListingAsync(int listingID, string citizenID)
    {
        try
        {
            // Find the listing (need to query without knowing CreatedAt for partitioned table)
            var listing = await _db.WasteListings
                .FirstOrDefaultAsync(w => w.ListingID == listingID && w.CitizenID == citizenID);

            if (listing == null)
                return false;

            // Only allow cancellation of pending listings
            if (listing.Status != "Pending")
                return false;

            listing.Status = "Cancelled";
            await _db.SaveChangesAsync();

            return true;
        }
        catch
        {
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
