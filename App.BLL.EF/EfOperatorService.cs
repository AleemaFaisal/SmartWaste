using App.Core;
using Microsoft.EntityFrameworkCore;

namespace App.BLL.EF;

public class EfOperatorService : IOperatorService
{
    private readonly AppDbContext _db;

    public EfOperatorService(AppDbContext db)
    {
        _db = db;
    }

    // ============================================
    // OPERATOR INFO
    // ============================================

    public async Task<Operator?> GetOperatorDetailsAsync(string operatorID)
    {
        return await _db.Operators
            .Include(o => o.Route)
                .ThenInclude(r => r!.Area)
            .Include(o => o.Warehouse)
            .FirstOrDefaultAsync(o => o.OperatorID == operatorID);
    }

    // ============================================
    // COLLECTION POINTS
    // ============================================

    public async Task<List<OperatorCollectionPointView>> GetMyCollectionPointsAsync(string operatorID)
    {
        return await _db.OperatorCollectionPoints
            .Where(cp => cp.OperatorID == operatorID && cp.Status == "Pending")
            .ToListAsync();
    }

    // ============================================
    // PERFORM COLLECTION
    // ============================================

    public async Task<int> CollectWasteAsync(CollectionDto dto)
    {
        using var transaction = await _db.Database.BeginTransactionAsync();
        try
        {
            // Create collection record
            var collection = new Collection
            {
                OperatorID = dto.OperatorID,
                ListingID = dto.ListingID,
                CollectedDate = DateTime.Now,
                CollectedWeight = dto.CollectedWeight,
                IsVerified = true,
                WarehouseID = dto.WarehouseID
            };

            _db.Collections.Add(collection);
            await _db.SaveChangesAsync();

            // Update waste listing status
            var listing = await _db.WasteListings
                .FirstOrDefaultAsync(w => w.ListingID == dto.ListingID);

            if (listing != null)
            {
                listing.Status = "Collected";
                await _db.SaveChangesAsync();
            }

            // Get listing details for warehouse stock update
            if (listing != null)
            {
                // Update warehouse stock
                var existingStock = await _db.WarehouseStocks
                    .FirstOrDefaultAsync(ws => ws.WarehouseID == dto.WarehouseID
                                            && ws.CategoryID == listing.CategoryID);

                if (existingStock != null)
                {
                    existingStock.CurrentWeight += (float)dto.CollectedWeight;
                    existingStock.LastUpdated = DateTime.Now;
                }
                else
                {
                    _db.WarehouseStocks.Add(new WarehouseStock
                    {
                        WarehouseID = dto.WarehouseID,
                        CategoryID = listing.CategoryID,
                        CurrentWeight = (float)dto.CollectedWeight,
                        LastUpdated = DateTime.Now
                    });
                }

                await _db.SaveChangesAsync();
            }

            await transaction.CommitAsync();
            return collection.CollectionID;
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    // ============================================
    // WAREHOUSE OPERATIONS
    // ============================================

    public async Task<bool> DepositWasteAsync(WarehouseDepositDto dto)
    {
        try
        {
            var existingStock = await _db.WarehouseStocks
                .FirstOrDefaultAsync(ws => ws.WarehouseID == dto.WarehouseID
                                        && ws.CategoryID == dto.CategoryID);

            if (existingStock != null)
            {
                existingStock.CurrentWeight += (float)dto.Quantity;
                existingStock.LastUpdated = DateTime.Now;
            }
            else
            {
                _db.WarehouseStocks.Add(new WarehouseStock
                {
                    WarehouseID = dto.WarehouseID,
                    CategoryID = dto.CategoryID,
                    CurrentWeight = (float)dto.Quantity,
                    LastUpdated = DateTime.Now
                });
            }

            await _db.SaveChangesAsync();
            return true;
        }
        catch
        {
            return false;
        }
    }

    // ============================================
    // PERFORMANCE
    // ============================================

    public async Task<List<Collection>> GetMyCollectionHistoryAsync(string operatorID)
    {
        return await _db.Collections
            .Where(c => c.OperatorID == operatorID)
            .OrderByDescending(c => c.CollectedDate)
            .Take(100) // Limit to recent 100 collections
            .ToListAsync();
    }

    public async Task<OperatorPerformanceView?> GetMyPerformanceAsync(string operatorID)
    {
        return await _db.OperatorPerformance
            .FirstOrDefaultAsync(op => op.OperatorID == operatorID);
    }
}
