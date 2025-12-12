using App.Core;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;

namespace App.BLL.EF;

public class EfGovernmentService : IGovernmentService
{
    private readonly AppDbContext _db;

    public EfGovernmentService(AppDbContext db)
    {
        _db = db;
    }

    // ============================================
    // WAREHOUSE ANALYTICS
    // ============================================

    public async Task<List<WarehouseInventoryView>> GetWarehouseInventoryAsync(int? warehouseID = null)
    {
        var query = _db.WarehouseInventory.AsQueryable();

        if (warehouseID.HasValue)
        {
            query = query.Where(w => w.WarehouseID == warehouseID.Value);
        }

        return await query.ToListAsync();
    }

    public async Task<List<Warehouse>> GetAllWarehousesAsync()
    {
        return await _db.Warehouses
            .Include(w => w.Area)
            .OrderBy(w => w.WarehouseName)
            .ToListAsync();
    }

    // ============================================
    // REPORTS
    // ============================================

    public async Task<List<HighYieldAreaReport>> AnalyzeHighYieldAreasAsync(DateTime? startDate = null, DateTime? endDate = null)
    {
        // Call stored procedure sp_AnalyzeHighYieldAreas
        var results = new List<HighYieldAreaReport>();

        try
        {
            var sql = "EXEC WasteManagement.sp_AnalyzeHighYieldAreas";

            await using var command = _db.Database.GetDbConnection().CreateCommand();
            command.CommandText = sql;
            await _db.Database.OpenConnectionAsync();

            await using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                results.Add(new HighYieldAreaReport
                {
                    AreaID = reader.GetInt32(0),
                    AreaName = reader.GetString(1),
                    City = reader.GetString(2),
                    TotalListings = reader.GetInt32(3),
                    TotalWeight = reader.GetDecimal(4),
                    TotalRevenue = reader.GetDecimal(5),
                    RevenueRank = reader.GetInt32(6)
                });
            }
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to analyze high-yield areas: {ex.Message}", ex);
        }

        return results;
    }

    public async Task<List<OperatorPerformanceReport>> GetOperatorPerformanceReportAsync()
    {
        // Call stored procedure sp_OperatorPerformance
        var results = new List<OperatorPerformanceReport>();

        try
        {
            var sql = "EXEC WasteManagement.sp_OperatorPerformance";

            await using var command = _db.Database.GetDbConnection().CreateCommand();
            command.CommandText = sql;
            await _db.Database.OpenConnectionAsync();

            await using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                results.Add(new OperatorPerformanceReport
                {
                    OperatorID = reader.GetString(0),
                    FullName = reader.GetString(1),
                    TotalCollections = reader.GetInt32(2),
                    TotalWeightKg = reader.GetDecimal(3),
                    Complaints = reader.GetInt32(4),
                    Rating = reader.GetString(5)
                });
            }
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to get operator performance: {ex.Message}", ex);
        }

        return results;
    }

    // ============================================
    // CATEGORY MANAGEMENT
    // ============================================

    public async Task<List<Category>> GetAllCategoriesAsync()
    {
        return await _db.Categories
            .OrderBy(c => c.CategoryName)
            .ToListAsync();
    }

    public async Task<int> CreateCategoryAsync(CreateCategoryDto dto)
    {
        var category = new Category
        {
            CategoryName = dto.CategoryName,
            BasePricePerKg = dto.BasePricePerKg,
            Description = dto.Description
        };

        _db.Categories.Add(category);
        await _db.SaveChangesAsync();

        return category.CategoryID;
    }

    public async Task<bool> UpdateCategoryPriceAsync(int categoryID, decimal newPrice)
    {
        try
        {
            var category = await _db.Categories.FindAsync(categoryID);
            if (category == null)
                return false;

            category.BasePricePerKg = newPrice;
            await _db.SaveChangesAsync();

            // Note: The database trigger will automatically update listing prices

            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> DeleteCategoryAsync(int categoryID)
    {
        try
        {
            var category = await _db.Categories.FindAsync(categoryID);
            if (category == null)
                return false;

            _db.Categories.Remove(category);
            await _db.SaveChangesAsync();

            return true;
        }
        catch
        {
            return false;
        }
    }

    // ============================================
    // OPERATOR MANAGEMENT
    // ============================================

    public async Task<string> CreateOperatorAsync(CreateOperatorDto dto)
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

            // Generate default password (can use database function or local generation)
            var defaultPassword = GenerateRandomPassword();
            var passwordHash = HashPassword(defaultPassword);

            // Create user account
            var user = new User
            {
                UserID = dto.CNIC,
                PasswordHash = passwordHash,
                RoleID = 3, // Operator role
                CreatedAt = DateTime.Now
            };

            _db.Users.Add(user);
            await _db.SaveChangesAsync();

            // Create operator profile
            var op = new Operator
            {
                OperatorID = dto.CNIC,
                FullName = dto.FullName,
                PhoneNumber = dto.PhoneNumber,
                RouteID = dto.RouteID,
                WarehouseID = dto.WarehouseID,
                Status = "Available"
            };

            _db.Operators.Add(op);
            await _db.SaveChangesAsync();

            await transaction.CommitAsync();
            return op.OperatorID;
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task<bool> AssignOperatorToRouteAsync(string operatorID, int routeID, int warehouseID)
    {
        try
        {
            var op = await _db.Operators.FindAsync(operatorID);
            if (op == null)
                return false;

            op.RouteID = routeID;
            op.WarehouseID = warehouseID;

            await _db.SaveChangesAsync();
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> DeactivateOperatorAsync(string operatorID)
    {
        try
        {
            var op = await _db.Operators.FindAsync(operatorID);
            if (op == null)
                return false;

            op.Status = "Offline";
            op.RouteID = null;
            op.WarehouseID = null;

            await _db.SaveChangesAsync();
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<List<Operator>> GetAllOperatorsAsync()
    {
        return await _db.Operators
            .Include(o => o.Route)
            .Include(o => o.Warehouse)
            .OrderBy(o => o.FullName)
            .ToListAsync();
    }

    // ============================================
    // COMPLAINTS
    // ============================================

    public async Task<List<Complaint>> GetAllComplaintsAsync(string? status = null)
    {
        var query = _db.Complaints
            .Include(c => c.Citizen)
            .Include(c => c.Operator)
            .AsQueryable();

        if (!string.IsNullOrEmpty(status))
        {
            query = query.Where(c => c.Status == status);
        }

        return await query
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync();
    }

    public async Task<bool> UpdateComplaintStatusAsync(int complaintID, string newStatus)
    {
        try
        {
            var complaint = await _db.Complaints.FindAsync(complaintID);
            if (complaint == null)
                return false;

            complaint.Status = newStatus;
            await _db.SaveChangesAsync();

            return true;
        }
        catch
        {
            return false;
        }
    }

    // ============================================
    // ROUTES & AREAS
    // ============================================

    public async Task<List<Route>> GetAllRoutesAsync()
    {
        return await _db.Routes
            .Include(r => r.Area)
            .OrderBy(r => r.RouteName)
            .ToListAsync();
    }

    public async Task<List<Area>> GetAllAreasAsync()
    {
        return await _db.Areas
            .OrderBy(a => a.City)
            .ThenBy(a => a.AreaName)
            .ToListAsync();
    }

    // ============================================
    // HELPER METHODS
    // ============================================

    private string GenerateRandomPassword()
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789!@#$%^&*";
        var random = new Random();
        return new string(Enumerable.Repeat(chars, 12)
            .Select(s => s[random.Next(s.Length)]).ToArray());
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
