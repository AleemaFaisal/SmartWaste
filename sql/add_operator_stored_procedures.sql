-- ============================================
-- Add Missing Operator Stored Procedures
-- ============================================
USE SmartWasteDB;
GO

PRINT '==============================================';
PRINT 'Creating Missing Operator Stored Procedures';
PRINT '==============================================';
GO

-- ================================================================
-- Procedure 1: Get Operator Details with Route and Warehouse Info
-- ================================================================
CREATE OR ALTER PROCEDURE WasteManagement.sp_GetOperatorDetails
    @OperatorID VARCHAR(15)
AS
BEGIN
    SET NOCOUNT ON;
    
    SELECT 
        o.OperatorID,
        o.FullName,
        o.PhoneNumber,
        o.RouteID,
        o.WarehouseID,
        o.Status,
        r.RouteName,
        r.AreaID,
        w.WarehouseName
    FROM WasteManagement.Operator o
    LEFT JOIN WasteManagement.Route r ON o.RouteID = r.RouteID
    LEFT JOIN WasteManagement.Warehouse w ON o.WarehouseID = w.WarehouseID
    WHERE o.OperatorID = @OperatorID;
END;
GO

-- ================================================================
-- Procedure 2: Perform Collection with Payment Processing
-- ================================================================
CREATE OR ALTER PROCEDURE WasteManagement.sp_PerformCollection
    @OperatorID VARCHAR(15),
    @ListingID INT,
    @CollectedWeight DECIMAL(10,2),
    @WarehouseID INT,
    @CollectionID INT OUTPUT,
    @TransactionID INT OUTPUT,
    @PaymentAmount DECIMAL(10,2) OUTPUT,
    @VerificationCode VARCHAR(50) OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRANSACTION;
    
    BEGIN TRY
        DECLARE @CategoryID INT;
        DECLARE @BasePricePerKg DECIMAL(10,2);
        DECLARE @ListingDate DATE;
        
        -- Get listing details
        SELECT @CategoryID = CategoryID, @ListingDate = CAST(CreatedAt AS DATE)
        FROM WasteManagement.WasteListing
        WHERE ListingID = @ListingID;
        
        -- Get category pricing
        SELECT @BasePricePerKg = BasePricePerKg
        FROM WasteManagement.Category
        WHERE CategoryID = @CategoryID;
        
        -- Calculate payment
        SET @PaymentAmount = @CollectedWeight * @BasePricePerKg;
        
        -- Generate verification code
        SET @VerificationCode = UPPER(LEFT(CONVERT(VARCHAR(50), NEWID()), 10));
        
        -- Insert Collection record
        INSERT INTO WasteManagement.Collection 
            (OperatorID, ListingID, CollectedWeight, CollectedDate, WarehouseID)
        VALUES 
            (@OperatorID, @ListingID, @CollectedWeight, GETDATE(), @WarehouseID);
        
        SET @CollectionID = SCOPE_IDENTITY();
        
        -- Update WasteListing status (trigger will do this, but explicit for SP)
        UPDATE WasteManagement.WasteListing
        SET Status = 'Collected'
        WHERE ListingID = @ListingID;
        
        -- Create Transaction Record
        INSERT INTO WasteManagement.TransactionRecord
            (CitizenID, TotalAmount, PaymentStatus, TransactionDate, VerificationCode)
        SELECT 
            CitizenID,
            @PaymentAmount,
            'Pending',
            GETDATE(),
            @VerificationCode
        FROM WasteManagement.WasteListing
        WHERE ListingID = @ListingID;
        
        SET @TransactionID = SCOPE_IDENTITY();
        
        -- Link transaction to listing
        UPDATE WasteManagement.WasteListing
        SET TransactionID = @TransactionID
        WHERE ListingID = @ListingID;
        
        -- Update WarehouseStock
        IF EXISTS (SELECT 1 FROM WasteManagement.WarehouseStock 
                   WHERE WarehouseID = @WarehouseID AND CategoryID = @CategoryID)
        BEGIN
            UPDATE WasteManagement.WarehouseStock
            SET CurrentWeight = CurrentWeight + @CollectedWeight,
                LastUpdated = GETDATE()
            WHERE WarehouseID = @WarehouseID AND CategoryID = @CategoryID;
        END
        ELSE
        BEGIN
            INSERT INTO WasteManagement.WarehouseStock 
                (WarehouseID, CategoryID, CurrentWeight, LastUpdated)
            VALUES 
                (@WarehouseID, @CategoryID, @CollectedWeight, GETDATE());
        END
        
        COMMIT TRANSACTION;
        RETURN 0;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0
            ROLLBACK TRANSACTION;
        
        -- Return error
        DECLARE @ErrorMessage NVARCHAR(4000) = ERROR_MESSAGE();
        RAISERROR(@ErrorMessage, 16, 1);
        RETURN -1;
    END CATCH
END;
GO

-- ================================================================
-- Procedure 3: Warehouse Deposit (MERGE operation)
-- ================================================================
CREATE OR ALTER PROCEDURE WasteManagement.sp_WarehouseDeposit
    @WarehouseID INT,
    @CategoryID INT,
    @Quantity DECIMAL(10,2)
AS
BEGIN
    SET NOCOUNT ON;
    
    BEGIN TRY
        MERGE WasteManagement.WarehouseStock AS target
        USING (SELECT @WarehouseID AS WarehouseID, @CategoryID AS CategoryID, @Quantity AS Quantity) AS source
        ON target.WarehouseID = source.WarehouseID AND target.CategoryID = source.CategoryID
        WHEN MATCHED THEN
            UPDATE SET 
                CurrentWeight = target.CurrentWeight + source.Quantity,
                LastUpdated = GETDATE()
        WHEN NOT MATCHED THEN
            INSERT (WarehouseID, CategoryID, CurrentWeight, LastUpdated)
            VALUES (source.WarehouseID, source.CategoryID, source.Quantity, GETDATE());
        
        RETURN 0;
    END TRY
    BEGIN CATCH
        DECLARE @ErrorMessage NVARCHAR(4000) = ERROR_MESSAGE();
        RAISERROR(@ErrorMessage, 16, 1);
        RETURN -1;
    END CATCH
END;
GO

-- ================================================================
-- Procedure 4: Get Collection History for Operator
-- ================================================================
CREATE OR ALTER PROCEDURE WasteManagement.sp_GetOperatorCollectionHistory
    @OperatorID VARCHAR(15)
AS
BEGIN
    SET NOCOUNT ON;
    
    SELECT TOP 100
        c.CollectionID,
        c.OperatorID,
        c.ListingID,
        c.CollectedWeight,
        c.CollectedDate,
        c.WarehouseID,
        w.WarehouseName
    FROM WasteManagement.Collection c
    LEFT JOIN WasteManagement.Warehouse w ON c.WarehouseID = w.WarehouseID
    WHERE c.OperatorID = @OperatorID
    ORDER BY c.CollectedDate DESC;
END;
GO

PRINT '✅ Operator stored procedures created successfully';
PRINT '';
PRINT 'Available procedures:';
PRINT '  - sp_GetOperatorDetails';
PRINT '  - sp_PerformCollection (with payment processing)';
PRINT '  - sp_WarehouseDeposit';
PRINT '  - sp_GetOperatorCollectionHistory';
PRINT '  - sp_OperatorPerformance (already existed)';
PRINT '  - sp_GetOperatorComplaints';
PRINT '  - sp_UpdateComplaintStatus';
GO
