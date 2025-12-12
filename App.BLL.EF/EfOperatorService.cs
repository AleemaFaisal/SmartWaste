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

    public async Task<CollectionResultDto> CollectWasteAsync(CollectionDto dto)
    {
        Console.WriteLine($"[EF] CollectWasteAsync - Using ENTITY FRAMEWORK (LINQ + Raw SQL)");
        Console.WriteLine($"[EfOperatorService] CollectWasteAsync - Start");
        Console.WriteLine($"[EfOperatorService] DTO - OperatorID: '{dto.OperatorID}', ListingID: {dto.ListingID}, Weight: {dto.CollectedWeight}, WarehouseID: {dto.WarehouseID}");
        
        int? transactionId = null;
        decimal? paymentAmount = null;
        string? verificationCode = null;
        
        using var transaction = await _db.Database.BeginTransactionAsync();
        try
        {
            // Get the current date for partitioning
            var collectedDate = DateTime.Now;
            
            // Create collection record using raw SQL (table has triggers, can't use OUTPUT or SqlQuery)
            Console.WriteLine($"[EfOperatorService] Inserting Collection via ExecuteSqlRaw");
            
            // Execute INSERT and get SCOPE_IDENTITY in separate statements
            var insertSql = @"
                INSERT INTO WasteManagement.Collection 
                    (OperatorID, ListingID, CollectedDate, CollectedWeight, IsVerified, WarehouseID)
                VALUES 
                    (@p0, @p1, @p2, @p3, @p4, @p5);";
            
            await _db.Database.ExecuteSqlRawAsync(insertSql,
                dto.OperatorID, 
                dto.ListingID, 
                collectedDate, 
                dto.CollectedWeight, 
                true, 
                dto.WarehouseID);
            
            // Get the inserted ID using a separate query with column alias
            var getIdSql = "SELECT CAST(IDENT_CURRENT('WasteManagement.Collection') AS INT) AS Value";
            var collectionId = await _db.Database.SqlQueryRaw<int>(getIdSql).FirstOrDefaultAsync();
            
            Console.WriteLine($"[EfOperatorService] Collection saved, ID: {collectionId}");

            // Update waste listing status using raw SQL (table has triggers)
            Console.WriteLine($"[EfOperatorService] Updating WasteListing {dto.ListingID} status");
            var updateListingSql = @"
                UPDATE WasteManagement.WasteListing 
                SET Status = @p0 
                WHERE ListingID = @p1";
            
            await _db.Database.ExecuteSqlRawAsync(updateListingSql, "Collected", dto.ListingID);
            Console.WriteLine($"[EfOperatorService] Listing status updated");
            
            // Fetch listing details for CategoryID
            Console.WriteLine($"[EfOperatorService] Fetching WasteListing {dto.ListingID}");
            var listing = await _db.WasteListings
                .AsNoTracking()
                .FirstOrDefaultAsync(w => w.ListingID == dto.ListingID);

            if (listing == null)
            {
                Console.WriteLine($"[EfOperatorService] WARNING: Listing {dto.ListingID} not found!");
            }

            // Update warehouse stock using raw SQL (avoiding potential trigger issues)
            if (listing != null)
            {
                Console.WriteLine($"[EfOperatorService] Updating WarehouseStock for WarehouseID: {dto.WarehouseID}, CategoryID: {listing.CategoryID}");
                
                var upsertStockSql = @"
                    MERGE WasteManagement.WarehouseStock AS target
                    USING (SELECT @p0 AS WarehouseID, @p1 AS CategoryID, @p2 AS Weight) AS source
                    ON target.WarehouseID = source.WarehouseID AND target.CategoryID = source.CategoryID
                    WHEN MATCHED THEN
                        UPDATE SET CurrentWeight = target.CurrentWeight + source.Weight, LastUpdated = GETDATE()
                    WHEN NOT MATCHED THEN
                        INSERT (WarehouseID, CategoryID, CurrentWeight, LastUpdated)
                        VALUES (source.WarehouseID, source.CategoryID, source.Weight, GETDATE());";
                
                await _db.Database.ExecuteSqlRawAsync(upsertStockSql, 
                    dto.WarehouseID, 
                    listing.CategoryID, 
                    (double)dto.CollectedWeight);
                
                Console.WriteLine($"[EfOperatorService] WarehouseStock updated successfully");
                
                // ============================================
                // PAYMENT PROCESSING
                // ============================================
                
                // Get category pricing for payment calculation
                Console.WriteLine($"[EfOperatorService] Fetching category pricing for payment");
                var category = await _db.Categories
                    .AsNoTracking()
                    .FirstOrDefaultAsync(c => c.CategoryID == listing.CategoryID);
                
                if (category != null)
                {
                    // Calculate payment amount
                    paymentAmount = dto.CollectedWeight * category.BasePricePerKg;
                    Console.WriteLine($"[EfOperatorService] Payment amount: {dto.CollectedWeight}kg × {category.BasePricePerKg} = {paymentAmount}");
                    
                    // Generate verification code
                    verificationCode = Guid.NewGuid().ToString("N").Substring(0, 10).ToUpper();
                    var transactionDate = DateTime.Now;
                    
                    // Create transaction record using raw SQL (table has triggers)
                    Console.WriteLine($"[EfOperatorService] Creating TransactionRecord");
                    var insertTransactionSql = @"
                        INSERT INTO WasteManagement.TransactionRecord 
                            (CitizenID, OperatorID, TotalAmount, PaymentStatus, PaymentMethod, TransactionDate, VerificationCode)
                        VALUES 
                            (@p0, @p1, @p2, @p3, @p4, @p5, @p6);";
                    
                    await _db.Database.ExecuteSqlRawAsync(insertTransactionSql,
                        listing.CitizenID,
                        dto.OperatorID,
                        paymentAmount,
                        "Pending",
                        "Cash",
                        transactionDate,
                        verificationCode);
                    
                    // Get the generated transaction ID
                    var getTransactionIdSql = "SELECT CAST(IDENT_CURRENT('WasteManagement.TransactionRecord') AS INT) AS Value";
                    transactionId = await _db.Database.SqlQueryRaw<int>(getTransactionIdSql).FirstOrDefaultAsync();
                    
                    Console.WriteLine($"[EfOperatorService] Transaction created, ID: {transactionId}, Code: {verificationCode}");
                    
                    // Link transaction to waste listing
                    Console.WriteLine($"[EfOperatorService] Linking transaction {transactionId} to listing {dto.ListingID}");
                    var updateListingTransactionSql = @"
                        UPDATE WasteManagement.WasteListing 
                        SET TransactionID = @p0 
                        WHERE ListingID = @p1";
                    
                    await _db.Database.ExecuteSqlRawAsync(updateListingTransactionSql, transactionId, dto.ListingID);
                    Console.WriteLine($"[EfOperatorService] Payment processing completed");
                }
                else
                {
                    Console.WriteLine($"[EfOperatorService] WARNING: Category {listing.CategoryID} not found, skipping payment");
                }
            }

            Console.WriteLine($"[EfOperatorService] Committing transaction");
            await transaction.CommitAsync();
            Console.WriteLine($"[EfOperatorService] Transaction committed successfully");
            
            return new CollectionResultDto
            {
                Success = true,
                CollectionID = collectionId,
                TransactionID = transactionId,
                PaymentAmount = paymentAmount,
                VerificationCode = verificationCode,
                Message = transactionId.HasValue 
                    ? $"Collection recorded. Payment of Rs.{paymentAmount:F2} pending. Code: {verificationCode}" 
                    : "Collection recorded successfully"
            };
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[EfOperatorService] CollectWasteAsync ERROR: {ex.Message}");
            Console.WriteLine($"[EfOperatorService] CollectWasteAsync INNER: {ex.InnerException?.Message}");
            Console.WriteLine($"[EfOperatorService] CollectWasteAsync STACK: {ex.StackTrace}");
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
            Console.WriteLine($"[EF] DepositWasteAsync - Using ENTITY FRAMEWORK (Raw SQL MERGE)");
            Console.WriteLine($"[EfOperatorService] DepositWasteAsync - WarehouseID: {dto.WarehouseID}, CategoryID: {dto.CategoryID}, Quantity: {dto.Quantity}");
            
            // Use MERGE to upsert warehouse stock (table has triggers, avoid SaveChanges)
            var upsertStockSql = @"
                MERGE WasteManagement.WarehouseStock AS target
                USING (SELECT @p0 AS WarehouseID, @p1 AS CategoryID, @p2 AS Weight) AS source
                ON target.WarehouseID = source.WarehouseID AND target.CategoryID = source.CategoryID
                WHEN MATCHED THEN
                    UPDATE SET CurrentWeight = target.CurrentWeight + source.Weight, LastUpdated = GETDATE()
                WHEN NOT MATCHED THEN
                    INSERT (WarehouseID, CategoryID, CurrentWeight, LastUpdated)
                    VALUES (source.WarehouseID, source.CategoryID, source.Weight, GETDATE());";
            
            await _db.Database.ExecuteSqlRawAsync(upsertStockSql, 
                dto.WarehouseID, 
                dto.CategoryID, 
                (double)dto.Quantity);
            
            Console.WriteLine($"[EfOperatorService] DepositWasteAsync - Deposit successful");
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[EfOperatorService] DepositWasteAsync ERROR: {ex.Message}");
            return false;
        }
    }

    // ============================================
    // PERFORMANCE
    // ============================================

    public async Task<List<Collection>> GetMyCollectionHistoryAsync(string operatorID)
    {
        Console.WriteLine($"[EfOperatorService] GetMyCollectionHistoryAsync - OperatorID: '{operatorID}'");
        try
        {
            var history = await _db.Collections
                .Include(c => c.Warehouse)
                .Where(c => c.OperatorID == operatorID)
                .OrderByDescending(c => c.CollectedDate)
                .Take(100) // Limit to recent 100 collections
                .ToListAsync();
            Console.WriteLine($"[EfOperatorService] GetMyCollectionHistoryAsync - Retrieved {history.Count} records");
            return history;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[EfOperatorService] GetMyCollectionHistoryAsync ERROR: {ex.Message}");
            Console.WriteLine($"[EfOperatorService] GetMyCollectionHistoryAsync INNER: {ex.InnerException?.Message}");
            throw;
        }
    }

    public async Task<OperatorPerformanceView?> GetMyPerformanceAsync(string operatorID)
    {
        return await _db.OperatorPerformance
            .FirstOrDefaultAsync(op => op.OperatorID == operatorID);
    }

    // ============================================
    // COMPLAINTS
    // ============================================

    public async Task<List<ActiveComplaintView>> GetMyComplaintsAsync(string operatorID)
    {
        Console.WriteLine($"[EF] GetMyComplaintsAsync - Using ENTITY FRAMEWORK (LINQ)");
        return await _db.ActiveComplaints
            .Where(c => c.OperatorID == operatorID)
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync();
    }

    public async Task<bool> UpdateComplaintStatusAsync(int complaintID, string newStatus)
    {
        var complaint = await _db.Complaints.FindAsync(complaintID);
        if (complaint == null) return false;

        complaint.Status = newStatus;
        await _db.SaveChangesAsync();
        return true;
    }
}
