-- ============================================
-- Additional Stored Procedures for SmartWaste
-- These procedures are needed by the SP implementation layer
-- ============================================

USE SmartWasteDB;
GO

-- ============================================
-- AUTHENTICATION
-- ============================================

-- Get user for login authentication
CREATE OR ALTER PROCEDURE WasteManagement.sp_AuthenticateUser
    @CNIC VARCHAR(15),
    @PasswordHash VARCHAR(255),
    @UserID VARCHAR(15) OUTPUT,
    @RoleID INT OUTPUT,
    @RoleName VARCHAR(50) OUTPUT,
    @CitizenID VARCHAR(15) OUTPUT,
    @OperatorID VARCHAR(15) OUTPUT
AS
BEGIN
    SET NOCOUNT ON;

    -- Find user
    SELECT
        @UserID = u.UserID,
        @RoleID = u.RoleID,
        @RoleName = ur.RoleName
    FROM WasteManagement.Users u
    INNER JOIN WasteManagement.UserRole ur ON u.RoleID = ur.RoleID
    WHERE u.UserID = @CNIC AND u.PasswordHash = @PasswordHash;

    -- If user not found, return
    IF @UserID IS NULL
        RETURN;

    -- Get CitizenID if citizen
    IF @RoleID = 2
    BEGIN
        SELECT @CitizenID = CitizenID
        FROM WasteManagement.Citizen
        WHERE CitizenID = @CNIC;
    END

    -- Get OperatorID if operator
    IF @RoleID = 3
    BEGIN
        SELECT @OperatorID = OperatorID
        FROM WasteManagement.Operator
        WHERE OperatorID = @CNIC;
    END
END;
GO

-- ============================================
-- CITIZEN OPERATIONS
-- ============================================

-- Get citizen listings with category details
CREATE OR ALTER PROCEDURE WasteManagement.sp_GetCitizenListings
    @CitizenID VARCHAR(15)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        wl.ListingID,
        wl.CitizenID,
        wl.CategoryID,
        wl.Weight,
        wl.Status,
        wl.EstimatedPrice,
        wl.TransactionID,
        wl.CreatedAt,
        c.CategoryName,
        c.BasePricePerKg
    FROM WasteManagement.WasteListing wl
    INNER JOIN WasteManagement.Category c ON wl.CategoryID = c.CategoryID
    WHERE wl.CitizenID = @CitizenID
    ORDER BY wl.CreatedAt DESC;
END;
GO

-- Get citizen transactions
CREATE OR ALTER PROCEDURE WasteManagement.sp_GetCitizenTransactions
    @CitizenID VARCHAR(15)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT *
    FROM WasteManagement.TransactionRecord
    WHERE CitizenID = @CitizenID
    ORDER BY TransactionDate DESC;
END;
GO

-- Cancel waste listing
CREATE OR ALTER PROCEDURE WasteManagement.sp_CancelListing
    @ListingID INT,
    @CitizenID VARCHAR(15),
    @Success BIT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET @Success = 0;

    -- Check if listing exists and belongs to citizen
    IF EXISTS (
        SELECT 1 FROM WasteManagement.WasteListing
        WHERE ListingID = @ListingID
        AND CitizenID = @CitizenID
        AND Status = 'Pending'
    )
    BEGIN
        UPDATE WasteManagement.WasteListing
        SET Status = 'Cancelled'
        WHERE ListingID = @ListingID;

        SET @Success = 1;
    END
END;
GO

-- ============================================
-- OPERATOR OPERATIONS
-- ============================================

-- Get operator details
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
        w.WarehouseName,
        a.AreaName,
        a.City
    FROM WasteManagement.Operator o
    LEFT JOIN WasteManagement.Route r ON o.RouteID = r.RouteID
    LEFT JOIN WasteManagement.Warehouse w ON o.WarehouseID = w.WarehouseID
    LEFT JOIN WasteManagement.Area a ON r.AreaID = a.AreaID
    WHERE o.OperatorID = @OperatorID;
END;
GO

-- Perform waste collection
CREATE OR ALTER PROCEDURE WasteManagement.sp_PerformCollection
    @OperatorID VARCHAR(15),
    @ListingID INT,
    @CollectedWeight DECIMAL(10,2),
    @WarehouseID INT,
    @CollectionID INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRANSACTION;

    BEGIN TRY
        -- Create collection record
        DECLARE @CollectedDate DATETIME = GETDATE();

        INSERT INTO WasteManagement.Collection (
            OperatorID, ListingID, CollectedDate, CollectedWeight,
            IsVerified, WarehouseID
        )
        VALUES (
            @OperatorID, @ListingID, @CollectedDate, @CollectedWeight,
            1, @WarehouseID
        );

        SET @CollectionID = SCOPE_IDENTITY();

        -- Update listing status
        UPDATE WasteManagement.WasteListing
        SET Status = 'Collected'
        WHERE ListingID = @ListingID;

        -- Update warehouse stock
        DECLARE @CategoryID INT;
        SELECT @CategoryID = CategoryID
        FROM WasteManagement.WasteListing
        WHERE ListingID = @ListingID;

        IF EXISTS (
            SELECT 1 FROM WasteManagement.WarehouseStock
            WHERE WarehouseID = @WarehouseID AND CategoryID = @CategoryID
        )
        BEGIN
            UPDATE WasteManagement.WarehouseStock
            SET CurrentWeight = CurrentWeight + @CollectedWeight,
                LastUpdated = GETDATE()
            WHERE WarehouseID = @WarehouseID AND CategoryID = @CategoryID;
        END
        ELSE
        BEGIN
            INSERT INTO WasteManagement.WarehouseStock (
                WarehouseID, CategoryID, CurrentWeight, LastUpdated
            )
            VALUES (
                @WarehouseID, @CategoryID, @CollectedWeight, GETDATE()
            );
        END

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0
            ROLLBACK TRANSACTION;
        THROW;
    END CATCH
END;
GO

-- Get operator collection history
CREATE OR ALTER PROCEDURE WasteManagement.sp_GetOperatorCollectionHistory
    @OperatorID VARCHAR(15)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT TOP 100 *
    FROM WasteManagement.Collection
    WHERE OperatorID = @OperatorID
    ORDER BY CollectedDate DESC;
END;
GO

-- ============================================
-- GOVERNMENT OPERATIONS
-- ============================================

-- Get all categories
CREATE OR ALTER PROCEDURE WasteManagement.sp_GetAllCategories
AS
BEGIN
    SET NOCOUNT ON;

    SELECT *
    FROM WasteManagement.Category
    ORDER BY CategoryName;
END;
GO

-- Update category price
CREATE OR ALTER PROCEDURE WasteManagement.sp_UpdateCategoryPrice
    @CategoryID INT,
    @NewPrice DECIMAL(10,2),
    @Success BIT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET @Success = 0;

    IF EXISTS (SELECT 1 FROM WasteManagement.Category WHERE CategoryID = @CategoryID)
    BEGIN
        UPDATE WasteManagement.Category
        SET BasePricePerKg = @NewPrice
        WHERE CategoryID = @CategoryID;

        SET @Success = 1;
    END
END;
GO

-- Delete category
CREATE OR ALTER PROCEDURE WasteManagement.sp_DeleteCategory
    @CategoryID INT,
    @Success BIT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET @Success = 0;

    BEGIN TRY
        DELETE FROM WasteManagement.Category
        WHERE CategoryID = @CategoryID;

        SET @Success = 1;
    END TRY
    BEGIN CATCH
        SET @Success = 0;
    END CATCH
END;
GO

-- Get all operators
CREATE OR ALTER PROCEDURE WasteManagement.sp_GetAllOperators
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        o.*,
        r.RouteName,
        w.WarehouseName
    FROM WasteManagement.Operator o
    LEFT JOIN WasteManagement.Route r ON o.RouteID = r.RouteID
    LEFT JOIN WasteManagement.Warehouse w ON o.WarehouseID = w.WarehouseID
    ORDER BY o.FullName;
END;
GO

-- Get all warehouses
CREATE OR ALTER PROCEDURE WasteManagement.sp_GetAllWarehouses
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        w.*,
        a.AreaName,
        a.City
    FROM WasteManagement.Warehouse w
    INNER JOIN WasteManagement.Area a ON w.AreaID = a.AreaID
    ORDER BY w.WarehouseName;
END;
GO

-- Get all complaints
CREATE OR ALTER PROCEDURE WasteManagement.sp_GetAllComplaints
    @Status VARCHAR(50) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        c.*,
        cit.FullName AS CitizenName,
        op.FullName AS OperatorName
    FROM WasteManagement.Complaint c
    INNER JOIN WasteManagement.Citizen cit ON c.CitizenID = cit.CitizenID
    LEFT JOIN WasteManagement.Operator op ON c.OperatorID = op.OperatorID
    WHERE (@Status IS NULL OR c.Status = @Status)
    ORDER BY c.CreatedAt DESC;
END;
GO

-- Update complaint status
CREATE OR ALTER PROCEDURE WasteManagement.sp_UpdateComplaintStatus
    @ComplaintID INT,
    @NewStatus VARCHAR(50),
    @Success BIT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET @Success = 0;

    IF EXISTS (SELECT 1 FROM WasteManagement.Complaint WHERE ComplaintID = @ComplaintID)
    BEGIN
        UPDATE WasteManagement.Complaint
        SET Status = @NewStatus
        WHERE ComplaintID = @ComplaintID;

        SET @Success = 1;
    END
END;
GO

-- Get all routes
CREATE OR ALTER PROCEDURE WasteManagement.sp_GetAllRoutes
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        r.*,
        a.AreaName,
        a.City
    FROM WasteManagement.Route r
    INNER JOIN WasteManagement.Area a ON r.AreaID = a.AreaID
    ORDER BY r.RouteName;
END;
GO

-- Get all areas
CREATE OR ALTER PROCEDURE WasteManagement.sp_GetAllAreas
AS
BEGIN
    SET NOCOUNT ON;

    SELECT *
    FROM WasteManagement.Area
    ORDER BY City, AreaName;
END;
GO

PRINT 'Additional stored procedures created successfully!';
GO
