-- ============================================
-- Smart Waste Management System
-- ============================================


-- Drop existing database if exists
IF DB_ID('SmartWasteDB') IS NOT NULL
BEGIN
    ALTER DATABASE SmartWasteDB SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
    DROP DATABASE SmartWasteDB;
END
GO

CREATE DATABASE SmartWasteDB;
GO

USE SmartWasteDB;
GO

SET NOCOUNT ON; -- Suppress row count messages
GO

PRINT '==============================================';
PRINT 'Database created and in use';
PRINT '==============================================';
GO

-- ============================================
-- STEP 1: CREATE CUSTOM SCHEMA
-- ============================================
GO
CREATE SCHEMA WasteManagement AUTHORIZATION dbo;
GO
PRINT 'Custom schema created successfully';
GO

-- ============================================
-- STEP 2: PARTITION FUNCTIONS and SCHEMES
-- ============================================

PRINT 'Creating partition functions and schemes...';
GO

-- Partition Function 1: TransactionRecord (Monthly)
CREATE PARTITION FUNCTION pf_TransactionDate (DATETIME)
AS RANGE RIGHT FOR VALUES (
    '2024-01-01', '2024-02-01', '2024-03-01', '2024-04-01',
    '2024-05-01', '2024-06-01', '2024-07-01', '2024-08-01',
    '2024-09-01', '2024-10-01', '2024-11-01', '2024-12-01',
    '2025-01-01'
);
GO

CREATE PARTITION SCHEME ps_TransactionDate
AS PARTITION pf_TransactionDate
ALL TO ([PRIMARY]);
GO

-- Partition Function 2: WasteListing (Quarterly)
CREATE PARTITION FUNCTION pf_WasteListingDate (DATETIME)
AS RANGE RIGHT FOR VALUES (
    '2024-01-01', '2024-04-01', '2024-07-01', '2024-10-01',
    '2025-01-01'
);
GO

CREATE PARTITION SCHEME ps_WasteListingDate
AS PARTITION pf_WasteListingDate
ALL TO ([PRIMARY]);
GO

-- Partition Function 3: Collection (Monthly)
CREATE PARTITION FUNCTION pf_CollectionDate (DATETIME)
AS RANGE RIGHT FOR VALUES (
    '2024-01-01', '2024-02-01', '2024-03-01', '2024-04-01',
    '2024-05-01', '2024-06-01', '2024-07-01', '2024-08-01',
    '2024-09-01', '2024-10-01', '2024-11-01', '2024-12-01',
    '2025-01-01'
);
GO

CREATE PARTITION SCHEME ps_CollectionDate
AS PARTITION pf_CollectionDate
ALL TO ([PRIMARY]);
GO

PRINT 'Partitions created: 3 functions + 3 schemes';
GO

-- ============================================
-- STEP 3: CREATE TABLES
-- ============================================

PRINT 'Creating tables...';
GO

-- Lookup Tables
CREATE TABLE WasteManagement.UserRole (
    RoleID INT IDENTITY(1,1) PRIMARY KEY,
    RoleName VARCHAR(50) NOT NULL UNIQUE
);
GO

CREATE TABLE WasteManagement.Area (
    AreaID INT IDENTITY(1,1) PRIMARY KEY,
    AreaName VARCHAR(255) NOT NULL,
    City VARCHAR(100) NOT NULL,
    CONSTRAINT UQ_Area UNIQUE (AreaName, City)
);
GO

CREATE TABLE WasteManagement.Category (
    CategoryID INT IDENTITY(1,1) PRIMARY KEY,
    CategoryName VARCHAR(255) NOT NULL UNIQUE,
    BasePricePerKg DECIMAL(10,2) NOT NULL CHECK (BasePricePerKg >= 0),
    Description TEXT
);
GO

-- Core Tables
CREATE TABLE WasteManagement.Users (
    UserID VARCHAR(15) PRIMARY KEY,
    PasswordHash VARCHAR(64) NOT NULL,
    RoleID INT NOT NULL,
    CreatedAt DATETIME DEFAULT GETDATE(),
    CONSTRAINT FK_Users_Role FOREIGN KEY (RoleID) REFERENCES WasteManagement.UserRole(RoleID),
    CONSTRAINT CHK_UserID_Format CHECK (UserID LIKE '[0-9][0-9][0-9][0-9][0-9]-[0-9][0-9][0-9][0-9][0-9][0-9][0-9]-[0-9]')
);
GO

CREATE TABLE WasteManagement.Citizen (
    CitizenID VARCHAR(15) PRIMARY KEY,
    FullName VARCHAR(255) NOT NULL,
    PhoneNumber VARCHAR(50) NOT NULL,
    AreaID INT NOT NULL,
    Address VARCHAR(500),
    CONSTRAINT FK_Citizen_User FOREIGN KEY (CitizenID) REFERENCES WasteManagement.Users(UserID),
    CONSTRAINT FK_Citizen_Area FOREIGN KEY (AreaID) REFERENCES WasteManagement.Area(AreaID)
);
GO

CREATE TABLE WasteManagement.Route (
    RouteID INT IDENTITY(1,1) PRIMARY KEY,
    RouteName VARCHAR(255) NOT NULL,
    AreaID INT NOT NULL,
    CONSTRAINT FK_Route_Area FOREIGN KEY (AreaID) REFERENCES WasteManagement.Area(AreaID)
);
GO

CREATE TABLE WasteManagement.Warehouse (
    WarehouseID INT IDENTITY(1,1) PRIMARY KEY,
    WarehouseName VARCHAR(255) NOT NULL,
    AreaID INT NOT NULL,
    Address VARCHAR(500) NOT NULL,
    Capacity FLOAT NOT NULL CHECK (Capacity > 0),
    CurrentInventory FLOAT DEFAULT 0 CHECK (CurrentInventory >= 0),
    CONSTRAINT FK_Warehouse_Area FOREIGN KEY (AreaID) REFERENCES WasteManagement.Area(AreaID),
    CONSTRAINT CHK_Warehouse_Inventory CHECK (CurrentInventory <= Capacity)
);
GO

CREATE TABLE WasteManagement.Operator (
    OperatorID VARCHAR(15) PRIMARY KEY,
    FullName VARCHAR(255) NOT NULL,
    PhoneNumber VARCHAR(50) NOT NULL,
    RouteID INT,
    WarehouseID INT,
    Status VARCHAR(50) DEFAULT 'Available' CHECK (Status IN ('Available', 'Busy', 'Offline')),
    CONSTRAINT FK_Operator_User FOREIGN KEY (OperatorID) REFERENCES WasteManagement.Users(UserID),
    CONSTRAINT FK_Operator_Route FOREIGN KEY (RouteID) REFERENCES WasteManagement.Route(RouteID),
    CONSTRAINT FK_Operator_Warehouse FOREIGN KEY (WarehouseID) REFERENCES WasteManagement.Warehouse(WarehouseID)
);
GO

-- Partitioned Tables
CREATE TABLE WasteManagement.TransactionRecord (
    TransactionID INT IDENTITY(1,1),
    CitizenID VARCHAR(15) NOT NULL,
    OperatorID VARCHAR(15) NULL,
    TotalAmount DECIMAL(10,2) NOT NULL CHECK (TotalAmount >= 0),
    PaymentStatus VARCHAR(50) DEFAULT 'Pending' CHECK (PaymentStatus IN ('Pending', 'Completed', 'Failed')),
    PaymentMethod VARCHAR(50),
    TransactionDate DATETIME NOT NULL DEFAULT GETDATE(),
    VerificationCode VARCHAR(100),
    CONSTRAINT FK_Transaction_Citizen FOREIGN KEY (CitizenID) REFERENCES WasteManagement.Citizen(CitizenID),
    CONSTRAINT FK_Transaction_Operator FOREIGN KEY (OperatorID) REFERENCES WasteManagement.Operator(OperatorID)
) ON ps_TransactionDate(TransactionDate);
GO

ALTER TABLE WasteManagement.TransactionRecord
ADD CONSTRAINT PK_TransactionRecord PRIMARY KEY CLUSTERED (TransactionID, TransactionDate)
ON ps_TransactionDate(TransactionDate);
GO

-- FIX: Changed unique index to include TransactionDate for partitioning
CREATE UNIQUE NONCLUSTERED INDEX UQ_Transaction_VerificationCode 
ON WasteManagement.TransactionRecord(VerificationCode, TransactionDate) 
WHERE VerificationCode IS NOT NULL
ON ps_TransactionDate(TransactionDate);
GO

CREATE TABLE WasteManagement.WasteListing (
    ListingID INT IDENTITY(1,1),
    CitizenID VARCHAR(15) NOT NULL,
    CategoryID INT NOT NULL,
    Weight DECIMAL(10,2) NOT NULL CHECK (Weight > 0),
    Status VARCHAR(50) DEFAULT 'Pending' CHECK (Status IN ('Pending', 'Collected', 'Completed', 'Cancelled')),
    EstimatedPrice DECIMAL(10,2),
    TransactionID INT NULL,
    CreatedAt DATETIME NOT NULL DEFAULT GETDATE(),
    CONSTRAINT FK_WasteListing_Citizen FOREIGN KEY (CitizenID) REFERENCES WasteManagement.Citizen(CitizenID),
    CONSTRAINT FK_WasteListing_Category FOREIGN KEY (CategoryID) REFERENCES WasteManagement.Category(CategoryID)
) ON ps_WasteListingDate(CreatedAt);
GO

ALTER TABLE WasteManagement.WasteListing
ADD CONSTRAINT PK_WasteListing PRIMARY KEY CLUSTERED (ListingID, CreatedAt)
ON ps_WasteListingDate(CreatedAt);
GO

CREATE TABLE WasteManagement.Collection (
    CollectionID INT IDENTITY(1,1),
    OperatorID VARCHAR(15) NOT NULL,
    ListingID INT NOT NULL,
    CollectedDate DATETIME NOT NULL,
    CollectedWeight DECIMAL(10,2) NOT NULL CHECK (CollectedWeight > 0),
    PhotoProof VARCHAR(255),
    IsVerified BIT DEFAULT 0,
    WarehouseID INT NOT NULL,
    CONSTRAINT FK_Collection_Operator FOREIGN KEY (OperatorID) REFERENCES WasteManagement.Operator(OperatorID),
    CONSTRAINT FK_Collection_Warehouse FOREIGN KEY (WarehouseID) REFERENCES WasteManagement.Warehouse(WarehouseID)
) ON ps_CollectionDate(CollectedDate);
GO

ALTER TABLE WasteManagement.Collection
ADD CONSTRAINT PK_Collection PRIMARY KEY CLUSTERED (CollectionID, CollectedDate)
ON ps_CollectionDate(CollectedDate);
GO

CREATE TABLE WasteManagement.Complaint (
    ComplaintID INT IDENTITY(1,1) PRIMARY KEY,
    CitizenID VARCHAR(15) NOT NULL,
    OperatorID VARCHAR(15) NULL,
    ComplaintType VARCHAR(100) NOT NULL,
    Description TEXT NOT NULL,
    Status VARCHAR(50) DEFAULT 'Open' CHECK (Status IN ('Open', 'In Progress', 'Resolved', 'Closed')),
    CreatedAt DATETIME DEFAULT GETDATE(),
    CONSTRAINT FK_Complaint_Citizen FOREIGN KEY (CitizenID) REFERENCES WasteManagement.Citizen(CitizenID),
    CONSTRAINT FK_Complaint_Operator FOREIGN KEY (OperatorID) REFERENCES WasteManagement.Operator(OperatorID)
);
GO

CREATE TABLE WasteManagement.WarehouseStock (
    WarehouseID INT NOT NULL,
    CategoryID INT NOT NULL,
    CurrentWeight FLOAT NOT NULL CHECK (CurrentWeight >= 0),
    LastUpdated DATETIME DEFAULT GETDATE(),
    CONSTRAINT PK_WarehouseStock PRIMARY KEY (WarehouseID, CategoryID),
    CONSTRAINT FK_WarehouseStock_Warehouse FOREIGN KEY (WarehouseID) REFERENCES WasteManagement.Warehouse(WarehouseID),
    CONSTRAINT FK_WarehouseStock_Category FOREIGN KEY (CategoryID) REFERENCES WasteManagement.Category(CategoryID)
);
GO

PRINT 'Tables created: 13 (3 partitioned)';
GO

-- ============================================
-- STEP 4: CREATE FUNCTIONS
-- ============================================

PRINT 'Creating functions...';
GO

-- Function 1: Calculate Price
CREATE FUNCTION WasteManagement.fn_CalculatePrice(
    @CategoryID INT,
    @Weight DECIMAL(10,2)
)
RETURNS DECIMAL(10,2)
AS
BEGIN
    DECLARE @Price DECIMAL(10,2);
    SELECT @Price = BasePricePerKg * @Weight
    FROM WasteManagement.Category WHERE CategoryID = @CategoryID;
    RETURN ISNULL(@Price, 0);
END;
GO

-- Function 2: Validate CNIC
CREATE FUNCTION WasteManagement.fn_ValidateCNIC(@CNIC VARCHAR(15))
RETURNS BIT
AS
BEGIN
    IF @CNIC LIKE '[0-9][0-9][0-9][0-9][0-9]-[0-9][0-9][0-9][0-9][0-9][0-9][0-9]-[0-9]'
        RETURN 1;
    RETURN 0;
END;
GO

-- Function 3: Check Warehouse Capacity
CREATE FUNCTION WasteManagement.fn_CheckWarehouseCapacity(
    @WarehouseID INT,
    @AdditionalWeight FLOAT
)
RETURNS BIT
AS
BEGIN
    DECLARE @Available BIT = 0;
    IF EXISTS (
        SELECT 1 FROM WasteManagement.Warehouse 
        WHERE WarehouseID = @WarehouseID 
        AND (CurrentInventory + @AdditionalWeight) <= Capacity
    )
        SET @Available = 1;
    RETURN @Available;
END;
GO

-- Function 4: Get Partition Info
CREATE FUNCTION WasteManagement.fn_GetPartitionInfo()
RETURNS TABLE
AS
RETURN
(
    SELECT 
        OBJECT_NAME(p.object_id) as TableName,
        p.partition_number as PartitionNumber,
        CONVERT(VARCHAR(20), prv.value, 120) as RangeBoundary,
        p.rows as RecordCount,
        au.total_pages * 8 / 1024.0 as SizeMB
    FROM sys.partitions p
    INNER JOIN sys.tables t ON p.object_id = t.object_id
    INNER JOIN sys.schemas s ON t.schema_id = s.schema_id
    INNER JOIN sys.indexes i ON p.object_id = i.object_id AND p.index_id = i.index_id
    INNER JOIN sys.partition_schemes ps ON i.data_space_id = ps.data_space_id
    LEFT JOIN sys.partition_range_values prv ON ps.function_id = prv.function_id 
        AND p.partition_number = prv.boundary_id + 1
    LEFT JOIN sys.allocation_units au ON p.partition_id = au.container_id
    WHERE s.name = 'WasteManagement' 
    AND t.name IN ('TransactionRecord', 'WasteListing', 'Collection')
    AND i.type <= 1
);
GO

-- Function 5: Generate Password
CREATE FUNCTION WasteManagement.fn_GeneratePassword()
RETURNS VARCHAR(20)
AS
BEGIN
    DECLARE 
        @letters VARCHAR(52) = 'ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz',
        @numbers VARCHAR(10) = '0123456789',
        @symbols VARCHAR(10) = '!@#$%^&*',
        @i INT = 0,
        @password VARCHAR(20) = '',
        @seed INT = CAST(SUBSTRING(CONVERT(VARCHAR(50), GETDATE(), 121), 21, 6) AS INT);

    WHILE @i < 20
    BEGIN
        SET @seed = (@seed * 1103515245 + 12345) % 2147483647;
        DECLARE @charType INT = ABS(@seed) % 3;
        DECLARE @char VARCHAR(1);

        IF @charType = 0
        BEGIN
            SET @seed = (@seed * 1103515245 + 12345) % 2147483647;
            SET @char = SUBSTRING(@letters, (ABS(@seed) % LEN(@letters)) + 1, 1);
        END
        ELSE IF @charType = 1
        BEGIN
            SET @seed = (@seed * 1103515245 + 12345) % 2147483647;
            SET @char = SUBSTRING(@numbers, (ABS(@seed) % LEN(@numbers)) + 1, 1);
        END
        ELSE
        BEGIN
            SET @seed = (@seed * 1103515245 + 12345) % 2147483647;
            SET @char = SUBSTRING(@symbols, (ABS(@seed) % LEN(@symbols)) + 1, 1);
        END

        SET @password = @password + @char;
        SET @i = @i + 1;
    END
    RETURN @password;
END;
GO

PRINT 'Functions created: 5';
GO

-- ============================================
-- STEP 5: CREATE INDEXES
-- ============================================

PRINT 'Creating indexes...';
GO

CREATE NONCLUSTERED INDEX IX_Users_RoleID ON WasteManagement.Users(RoleID);
GO
CREATE NONCLUSTERED INDEX IX_Citizen_AreaID ON WasteManagement.Citizen(AreaID);
GO
CREATE NONCLUSTERED INDEX IX_Operator_RouteID ON WasteManagement.Operator(RouteID);
GO
CREATE NONCLUSTERED INDEX IX_Operator_WarehouseID ON WasteManagement.Operator(WarehouseID);
GO

CREATE NONCLUSTERED INDEX IX_WasteListing_CitizenID 
    ON WasteManagement.WasteListing(CitizenID, CreatedAt)
    ON ps_WasteListingDate(CreatedAt);
GO

CREATE NONCLUSTERED INDEX IX_WasteListing_CategoryID 
    ON WasteManagement.WasteListing(CategoryID, CreatedAt)
    ON ps_WasteListingDate(CreatedAt);
GO

CREATE NONCLUSTERED INDEX IX_Collection_OperatorID 
    ON WasteManagement.Collection(OperatorID, CollectedDate)
    ON ps_CollectionDate(CollectedDate);
GO

CREATE NONCLUSTERED INDEX IX_Collection_WarehouseID 
    ON WasteManagement.Collection(WarehouseID, CollectedDate)
    ON ps_CollectionDate(CollectedDate);
GO

CREATE NONCLUSTERED INDEX IX_Transaction_CitizenID 
    ON WasteManagement.TransactionRecord(CitizenID, TransactionDate)
    ON ps_TransactionDate(TransactionDate);
GO

CREATE NONCLUSTERED INDEX IX_WasteListing_Status_Date 
    ON WasteManagement.WasteListing(Status, CreatedAt) 
    INCLUDE (Weight, EstimatedPrice)
    ON ps_WasteListingDate(CreatedAt);
GO

CREATE NONCLUSTERED INDEX IX_Transaction_Status_Date 
    ON WasteManagement.TransactionRecord(PaymentStatus, TransactionDate) 
    INCLUDE (TotalAmount)
    ON ps_TransactionDate(TransactionDate);
GO

CREATE NONCLUSTERED INDEX IX_WasteListing_Pending 
    ON WasteManagement.WasteListing(CitizenID, CreatedAt) 
    WHERE Status = 'Pending';
GO

CREATE NONCLUSTERED INDEX IX_Complaint_Open 
    ON WasteManagement.Complaint(CreatedAt) 
    WHERE Status IN ('Open', 'In Progress');
GO

CREATE NONCLUSTERED INDEX IX_Collection_Analytics 
    ON WasteManagement.Collection(WarehouseID, CollectedDate) 
    INCLUDE (CollectedWeight, IsVerified)
    ON ps_CollectionDate(CollectedDate);
GO

PRINT 'Indexes created: 14';
GO

-- ============================================
-- STEP 6: CREATE VIEWS
-- ============================================

PRINT 'Creating views...';
GO

-- View 1: Citizen Profile
CREATE VIEW WasteManagement.vw_CitizenProfile
AS
SELECT 
    c.CitizenID, c.FullName, c.PhoneNumber, c.Address,
    c.AreaID, a.AreaName, a.City, u.CreatedAt as MemberSince
FROM WasteManagement.Citizen c
INNER JOIN WasteManagement.Users u ON c.CitizenID = u.UserID
INNER JOIN WasteManagement.Area a ON c.AreaID = a.AreaID;
GO

-- View 2: Operator Collection Points
CREATE VIEW WasteManagement.vw_OperatorCollectionPoints
AS
SELECT 
    o.OperatorID, o.FullName as OperatorName,
    r.RouteID, r.RouteName,
    wl.ListingID, c.CitizenID, c.FullName as CitizenName,
    c.PhoneNumber, c.Address, a.AreaName,
    cat.CategoryName, wl.Weight, wl.EstimatedPrice, wl.Status,
    tr.VerificationCode
FROM WasteManagement.Operator o
INNER JOIN WasteManagement.Route r ON o.RouteID = r.RouteID
INNER JOIN WasteManagement.Area a ON r.AreaID = a.AreaID
INNER JOIN WasteManagement.Citizen c ON a.AreaID = c.AreaID
INNER JOIN WasteManagement.WasteListing wl ON c.CitizenID = wl.CitizenID
INNER JOIN WasteManagement.Category cat ON wl.CategoryID = cat.CategoryID
LEFT JOIN WasteManagement.TransactionRecord tr ON wl.TransactionID = tr.TransactionID
WHERE wl.Status IN ('Pending', 'Collected');
GO

-- View 3: Warehouse Inventory
CREATE VIEW WasteManagement.vw_WarehouseInventory
AS
SELECT 
    w.WarehouseID, w.WarehouseName,
    a.AreaName, a.City,
    w.Capacity, w.CurrentInventory,
    ROUND((w.CurrentInventory / w.Capacity) * 100, 2) as CapacityUsedPercent,
    w.Capacity - w.CurrentInventory as AvailableCapacity,
    COUNT(DISTINCT ws.CategoryID) as CategoryCount
FROM WasteManagement.Warehouse w
INNER JOIN WasteManagement.Area a ON w.AreaID = a.AreaID
LEFT JOIN WasteManagement.WarehouseStock ws ON w.WarehouseID = ws.WarehouseID
GROUP BY w.WarehouseID, w.WarehouseName, a.AreaName, a.City, 
         w.Capacity, w.CurrentInventory;
GO

-- View 4: Active Complaints
CREATE VIEW WasteManagement.vw_ActiveComplaints
AS
SELECT 
    comp.ComplaintID, comp.ComplaintType, comp.Description,
    comp.Status, comp.CreatedAt,
    c.CitizenID, c.FullName as CitizenName, c.PhoneNumber,
    o.OperatorID, o.FullName as OperatorName,
    r.RouteName, a.AreaName,
    DATEDIFF(DAY, comp.CreatedAt, GETDATE()) as DaysOpen
FROM WasteManagement.Complaint comp
INNER JOIN WasteManagement.Citizen c ON comp.CitizenID = c.CitizenID
INNER JOIN WasteManagement.Area a ON c.AreaID = a.AreaID
LEFT JOIN WasteManagement.Operator o ON comp.OperatorID = o.OperatorID
LEFT JOIN WasteManagement.Route r ON o.RouteID = r.RouteID
WHERE comp.Status IN ('Open', 'In Progress');
GO

-- View 5: Transaction Summary (with CTE)
CREATE VIEW WasteManagement.vw_TransactionSummary
AS
WITH TransactionDetails AS (
    SELECT 
        tr.TransactionID, tr.CitizenID, tr.TotalAmount,
        tr.PaymentStatus, tr.TransactionDate,
        COUNT(wl.ListingID) as ItemCount,
        SUM(wl.Weight) as TotalWeight
    FROM WasteManagement.TransactionRecord tr
    LEFT JOIN WasteManagement.WasteListing wl ON tr.TransactionID = wl.TransactionID
    GROUP BY tr.TransactionID, tr.CitizenID, tr.TotalAmount,
             tr.PaymentStatus, tr.TransactionDate
)
SELECT 
    td.TransactionID, c.FullName as CitizenName,
    td.TotalAmount, td.PaymentStatus, td.TransactionDate,
    td.ItemCount, td.TotalWeight,
    o.FullName as OperatorName
FROM TransactionDetails td
INNER JOIN WasteManagement.Citizen c ON td.CitizenID = c.CitizenID
LEFT JOIN WasteManagement.TransactionRecord tr ON td.TransactionID = tr.TransactionID
LEFT JOIN WasteManagement.Operator o ON tr.OperatorID = o.OperatorID;
GO

-- View 6: Partition Statistics
CREATE VIEW WasteManagement.vw_PartitionStatistics
AS
SELECT 
    TableName,
    PartitionNumber,
    RangeBoundary,
    RecordCount,
    SizeMB
FROM WasteManagement.fn_GetPartitionInfo();
GO

-- View 7: Latest Stock By Category (with CTE)
CREATE VIEW WasteManagement.vw_LatestStockByCategory
AS
WITH LastStock AS (
    SELECT 
        ws.WarehouseID,
        ws.CategoryID,
        CAST(ws.LastUpdated AS DATE) AS StockDate,
        ws.CurrentWeight,
        ROW_NUMBER() OVER (PARTITION BY ws.WarehouseID, ws.CategoryID, CAST(ws.LastUpdated AS DATE)
                           ORDER BY ws.LastUpdated DESC) AS rn
    FROM WasteManagement.WarehouseStock ws
)
SELECT 
    l.WarehouseID,
    l.CategoryID,
    c.CategoryName,
    l.StockDate,
    l.CurrentWeight
FROM LastStock l
INNER JOIN WasteManagement.Category c ON l.CategoryID = c.CategoryID
WHERE l.rn = 1;
GO

-- View 8: Operator Performance (with CTE)
CREATE VIEW WasteManagement.vw_OperatorPerformance
AS
SELECT 
    o.OperatorID,
    o.FullName,
    o.PhoneNumber,
    o.RouteID,
    o.WarehouseID,
    COUNT(c.CollectionID) AS TotalPickups,
    SUM(c.CollectedWeight) AS TotalCollectedWeight,
    SUM(wl.EstimatedPrice) AS TotalCollectedAmount
FROM WasteManagement.Operator o
LEFT JOIN WasteManagement.Collection c ON o.OperatorID = c.OperatorID
LEFT JOIN WasteManagement.WasteListing wl ON c.ListingID = wl.ListingID
GROUP BY o.OperatorID, o.FullName, o.PhoneNumber, o.RouteID, o.WarehouseID;
GO

PRINT 'Views created: 8';
GO

-- ============================================
-- STEP 7: CREATE STORED PROCEDURES
-- ============================================

PRINT 'Creating stored procedures...';
GO

-- Procedure 1: Register Citizen
CREATE PROCEDURE WasteManagement.sp_RegisterCitizen
    @UserID VARCHAR(15),
    @PasswordHash VARCHAR(255),
    @FullName VARCHAR(255),
    @PhoneNumber VARCHAR(50),
    @AreaID INT,
    @Address VARCHAR(500),
    @ResultMessage VARCHAR(255) OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRY
        BEGIN TRANSACTION;
        
        IF WasteManagement.fn_ValidateCNIC(@UserID) = 0
        BEGIN
            SET @ResultMessage = 'Invalid CNIC format';
            ROLLBACK TRANSACTION;
            RETURN -1;
        END
        
        INSERT INTO WasteManagement.Users (UserID, PasswordHash, RoleID)
        VALUES (@UserID, @PasswordHash, 2);
        
        INSERT INTO WasteManagement.Citizen (CitizenID, FullName, PhoneNumber, AreaID, Address)
        VALUES (@UserID, @FullName, @PhoneNumber, @AreaID, @Address);
        
        COMMIT TRANSACTION;
        SET @ResultMessage = 'Citizen registered successfully';
        RETURN 0;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        SET @ResultMessage = ERROR_MESSAGE();
        RETURN -99;
    END CATCH
END;
GO

-- Procedure 2: Create Waste Listing
CREATE PROCEDURE WasteManagement.sp_CreateWasteListing
    @CitizenID VARCHAR(15),
    @CategoryID INT,
    @Weight DECIMAL(10,2),
    @ListingID INT OUTPUT,
    @EstimatedPrice DECIMAL(10,2) OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRY
        SET @EstimatedPrice = WasteManagement.fn_CalculatePrice(@CategoryID, @Weight);
        
        INSERT INTO WasteManagement.WasteListing (CitizenID, CategoryID, Weight, EstimatedPrice)
        VALUES (@CitizenID, @CategoryID, @Weight, @EstimatedPrice);
        
        SET @ListingID = SCOPE_IDENTITY();
        RETURN 0;
    END TRY
    BEGIN CATCH
        RETURN -99;
    END CATCH
END;
GO

-- Procedure 3: Analyze High Yield Areas (with CTE)
CREATE PROCEDURE WasteManagement.sp_AnalyzeHighYieldAreas
AS
BEGIN
    SET NOCOUNT ON;
    
    WITH AreaRevenue AS (
        SELECT 
            a.AreaID, a.AreaName, a.City,
            COUNT(DISTINCT wl.ListingID) as TotalListings,
            SUM(wl.Weight) as TotalWeight,
            SUM(wl.EstimatedPrice) as TotalRevenue
        FROM WasteManagement.Area a
        LEFT JOIN WasteManagement.Citizen c ON a.AreaID = c.AreaID
        LEFT JOIN WasteManagement.WasteListing wl ON c.CitizenID = wl.CitizenID
        WHERE wl.Status IN ('Collected', 'Completed')
        GROUP BY a.AreaID, a.AreaName, a.City
    )
    SELECT 
        AreaID, AreaName, City,
        TotalListings,
        ROUND(TotalWeight, 2) as TotalWeight,
        ROUND(TotalRevenue, 2) as TotalRevenue,
        RANK() OVER (ORDER BY TotalRevenue DESC) as RevenueRank
    FROM AreaRevenue
    ORDER BY TotalRevenue DESC;
END;
GO

-- Procedure 4: Operator Performance (with CTE)
CREATE PROCEDURE WasteManagement.sp_OperatorPerformance
AS
BEGIN
    SET NOCOUNT ON;
    
    WITH OperatorStats AS (
        SELECT 
            o.OperatorID, o.FullName,
            COUNT(DISTINCT c.CollectionID) as TotalCollections,
            SUM(c.CollectedWeight) as TotalWeight,
            COUNT(DISTINCT comp.ComplaintID) as Complaints
        FROM WasteManagement.Operator o
        LEFT JOIN WasteManagement.Collection c ON o.OperatorID = c.OperatorID
        LEFT JOIN WasteManagement.Complaint comp ON o.OperatorID = comp.OperatorID
        GROUP BY o.OperatorID, o.FullName
    )
    SELECT 
        OperatorID, FullName,
        TotalCollections,
        ROUND(TotalWeight, 2) as TotalWeightKg,
        Complaints,
        CASE 
            WHEN Complaints = 0 THEN 'Excellent'
            WHEN Complaints <= 2 THEN 'Good'
            ELSE 'Needs Improvement'
        END as Rating
    FROM OperatorStats
    ORDER BY TotalCollections DESC;
END;
GO

-- Procedure 5: Get Partition Status
CREATE PROCEDURE WasteManagement.sp_GetPartitionStatus
AS
BEGIN
    SET NOCOUNT ON;
    SELECT * FROM WasteManagement.vw_PartitionStatistics
    ORDER BY TableName, PartitionNumber;
END;
GO

-- Procedure 6: Create Operator
CREATE PROCEDURE WasteManagement.sp_CreateOperator
    @Name VARCHAR(100),
    @PhoneNumber VARCHAR(15),
    @CNIC VARCHAR(15),
    @ResultMessage VARCHAR(255) OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @password VARCHAR(20);
    SET @password = WasteManagement.fn_GeneratePassword();

    BEGIN TRY
        BEGIN TRANSACTION;
        
        IF WasteManagement.fn_ValidateCNIC(@CNIC) = 0
        BEGIN
            SET @ResultMessage = 'Invalid CNIC format';
            ROLLBACK TRANSACTION;
            RETURN -1;
        END

        INSERT INTO WasteManagement.Users (UserID, RoleID, PasswordHash)
        VALUES (
            @CNIC,
            (SELECT RoleID FROM WasteManagement.UserRole WHERE RoleName = 'Operator'),
            HASHBYTES('SHA2_256', @password)
        );

        INSERT INTO WasteManagement.Operator (OperatorID, FullName, PhoneNumber)
        VALUES (@CNIC, @Name, @PhoneNumber);
        
        COMMIT TRANSACTION;
        SET @ResultMessage = 'Operator created successfully';
        RETURN 0;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        SET @ResultMessage = ERROR_MESSAGE();
        RETURN -99;
    END CATCH
END;
GO

-- Procedure 7: Assign Operator to Route
CREATE PROCEDURE WasteManagement.sp_AssignOperatorToRoute
    @OperatorID VARCHAR(15),
    @WarehouseID INT,
    @RouteID INT
AS
BEGIN
    SET NOCOUNT ON;
    
    IF EXISTS (SELECT 1 FROM WasteManagement.Operator WHERE RouteID = @RouteID)
    BEGIN
        RAISERROR('Route already has an assigned operator.', 16, 1);
        RETURN;
    END
    
    UPDATE WasteManagement.Operator
    SET RouteID = @RouteID, WarehouseID = @WarehouseID
    WHERE OperatorID = @OperatorID;
    
    PRINT 'Operator assigned to route successfully';
END;
GO

-- Procedure 8: Deactivate Operator
CREATE PROCEDURE WasteManagement.sp_DeactivateOperator
    @OperatorID VARCHAR(15)
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE WasteManagement.Operator
    SET Status = 'Offline',
        RouteID = NULL,
        WarehouseID = NULL
    WHERE OperatorID = @OperatorID;

    PRINT 'Operator deactivated successfully';
END;
GO

PRINT 'Stored Procedures created: 8';
GO

-- ============================================
-- STEP 8: CREATE TRIGGERS
-- ============================================

PRINT 'Creating triggers...';
GO

-- Trigger 1 (AFTER): Auto-calculate price
CREATE TRIGGER WasteManagement.trg_WasteListing_AutoCalculatePrice
ON WasteManagement.WasteListing
AFTER INSERT
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE WasteManagement.WasteListing
    SET EstimatedPrice = WasteManagement.fn_CalculatePrice(i.CategoryID, i.Weight)
    FROM WasteManagement.WasteListing wl
    INNER JOIN inserted i ON wl.ListingID = i.ListingID AND wl.CreatedAt = i.CreatedAt
    WHERE wl.EstimatedPrice IS NULL;
END;
GO

-- Trigger 2 (AFTER): Update listing status after collection
CREATE TRIGGER WasteManagement.trg_Collection_UpdateStatus
ON WasteManagement.Collection
AFTER INSERT
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE WasteManagement.WasteListing
    SET Status = 'Collected'
    FROM WasteManagement.WasteListing wl
    INNER JOIN inserted i ON wl.ListingID = i.ListingID;
END;
GO

-- Trigger 3 (AFTER): Update warehouse inventory
CREATE TRIGGER WasteManagement.trg_WarehouseStock_UpdateInventory
ON WasteManagement.WarehouseStock
AFTER INSERT, UPDATE
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE WasteManagement.Warehouse
    SET CurrentInventory = (
        SELECT SUM(CurrentWeight)
        FROM WasteManagement.WarehouseStock
        WHERE WarehouseID = i.WarehouseID
    )
    FROM WasteManagement.Warehouse w
    INNER JOIN inserted i ON w.WarehouseID = i.WarehouseID;
END;
GO

-- Trigger 4 (AFTER): Update listing prices when category price changes
CREATE TRIGGER WasteManagement.trg_UpdateListingPrices
ON WasteManagement.Category
AFTER UPDATE
AS
BEGIN
    SET NOCOUNT ON;

    IF NOT EXISTS (
        SELECT 1
        FROM inserted i
        JOIN deleted d ON i.CategoryID = d.CategoryID
        WHERE i.BasePricePerKg <> d.BasePricePerKg
    )
        RETURN;

    UPDATE wl
    SET wl.EstimatedPrice = wl.Weight * i.BasePricePerKg
    FROM WasteManagement.WasteListing wl
    JOIN inserted i ON wl.CategoryID = i.CategoryID
    WHERE wl.Status = 'Pending';
END;
GO

-- Trigger 5 (INSTEAD OF): Update Citizen Profile View
CREATE TRIGGER WasteManagement.trg_UpdateCitizenProfile
ON WasteManagement.vw_CitizenProfile
INSTEAD OF UPDATE
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE WasteManagement.Citizen
    SET FullName = i.FullName,
        PhoneNumber = i.PhoneNumber,
        Address = i.Address,
        AreaID = i.AreaID
    FROM WasteManagement.Citizen c
    INNER JOIN inserted i ON c.CitizenID = i.CitizenID;
END;
GO

-- Trigger 6 (INSTEAD OF): Delete cascade for user
CREATE TRIGGER WasteManagement.trg_Users_DeleteCascade
ON WasteManagement.Users
INSTEAD OF DELETE
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRY
        BEGIN TRANSACTION;
        
        DELETE FROM WasteManagement.Citizen WHERE CitizenID IN (SELECT UserID FROM deleted);
        DELETE FROM WasteManagement.Operator WHERE OperatorID IN (SELECT UserID FROM deleted);
        DELETE FROM WasteManagement.Users WHERE UserID IN (SELECT UserID FROM deleted);
        
        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH
END;
GO

PRINT 'Triggers created: 6 (4 AFTER, 2 INSTEAD OF)';
GO

-- ============================================
-- STEP 9: POPULATE DATA 
-- ============================================'
/*
PRINT '';
PRINT '==============================================';
PRINT 'STARTING DATA POPULATION - 1+ MILLION ROWS';
PRINT '==============================================';
GO

-- ===== CONFIGURATION: ADJUST THESE VALUES =====
DECLARE @NumCitizens INT = 10000;       
DECLARE @NumOperators INT = 500;        
DECLARE @NumListings INT = 500000;       
DECLARE @NumComplaints INT = 50000;     
-- ===============================================
*/

PRINT '';
PRINT '==============================================';
PRINT 'STARTING DATA POPULATION - SMALL DATASET';
PRINT '==============================================';
GO


-- ===== CONFIGURATION: ADJUST THESE VALUES =====
DECLARE @NumCitizens INT = 50;
DECLARE @NumOperators INT = 10;
DECLARE @NumListings INT = 100;
DECLARE @NumComplaints INT = 25;
-- ===============================================


PRINT 'Populating master data...';

INSERT INTO WasteManagement.UserRole (RoleName) VALUES ('Admin'), ('Citizen'), ('Operator');

INSERT INTO WasteManagement.Area (AreaName, City) VALUES
('Model Town', 'Lahore'), ('Gulberg', 'Lahore'), ('DHA Phase 5', 'Lahore'),
('Johar Town', 'Lahore'), ('Bahria Town', 'Lahore'),
('Clifton', 'Karachi'), ('Defence', 'Karachi'), ('Gulshan', 'Karachi'),
('North Nazimabad', 'Karachi'), ('Saddar', 'Karachi');

INSERT INTO WasteManagement.Category (CategoryName, BasePricePerKg, Description) VALUES
('Plastic', 15.00, 'All types of plastic waste'),
('Paper', 10.00, 'Paper, cardboard, and newspaper'),
('Metal', 25.00, 'Metal scraps and cans'),
('Glass', 8.00, 'Glass bottles and containers'),
('E-Waste', 50.00, 'Electronic waste'),
('Organic', 5.00, 'Biodegradable waste');

INSERT INTO WasteManagement.Warehouse (WarehouseName, AreaID, Address, Capacity) VALUES
('Central Warehouse Lahore', 1, 'Industrial Area, Model Town', 50000),
('North Warehouse Lahore', 4, 'Johar Town Main Boulevard', 30000),
('Clifton Warehouse', 6, 'Clifton Block 5', 40000),
('Gulshan Warehouse', 8, 'Gulshan-e-Iqbal', 35000),
('Defence Warehouse', 7, 'Defence Phase 2', 45000);

INSERT INTO WasteManagement.Route (RouteName, AreaID) VALUES
('Route A - Model Town', 1), ('Route B - Gulberg', 2), ('Route C - DHA', 3),
('Route D - Johar Town', 4), ('Route E - Bahria', 5), ('Route F - Clifton', 6),
('Route G - Defence', 7), ('Route H - Gulshan', 8), ('Route I - North Nazimabad', 9),
('Route J - Saddar', 10);

PRINT 'Master data inserted successfully';

-- Generate Citizens
PRINT 'Generating ' + CAST(@NumCitizens AS VARCHAR(10)) + ' citizens...';
DECLARE @CitizenCounter INT = 1;
WHILE @CitizenCounter <= @NumCitizens
BEGIN
    DECLARE @CNIC VARCHAR(15) = RIGHT('00000' + CAST((35200 + @CitizenCounter) AS VARCHAR(5)), 5) + '-' +
        RIGHT('0000000' + CAST(@CitizenCounter AS VARCHAR(7)), 7) + '-' +
        CAST(((@CitizenCounter - 1) % 10) AS VARCHAR(1));
    
    INSERT INTO WasteManagement.Users (UserID, PasswordHash, RoleID)
    VALUES (@CNIC, HASHBYTES('SHA2_256', 'password' + CAST(@CitizenCounter AS VARCHAR(10))), 2);
    
    INSERT INTO WasteManagement.Citizen (CitizenID, FullName, PhoneNumber, AreaID, Address)
    VALUES (@CNIC, 'Citizen ' + CAST(@CitizenCounter AS VARCHAR(10)),
        '0300-' + RIGHT('0000000' + CAST(1000000 + @CitizenCounter AS VARCHAR(7)), 7),
        ((@CitizenCounter - 1) % 10) + 1,
        'House ' + CAST(@CitizenCounter AS VARCHAR(10)));
    
    SET @CitizenCounter = @CitizenCounter + 1;
END
PRINT 'Citizens created: ' + CAST(@NumCitizens AS VARCHAR(10));

-- Generate Operators
PRINT 'Generating ' + CAST(@NumOperators AS VARCHAR(10)) + ' operators...';
DECLARE @OpCounter INT = 1;
WHILE @OpCounter <= @NumOperators
BEGIN
    DECLARE @OpCNIC VARCHAR(15) = '42000-' +
        RIGHT('0000000' + CAST(300000 + @OpCounter AS VARCHAR(7)), 7) + '-' +
        CAST(((@OpCounter - 1) % 10) AS VARCHAR(1));
    
    INSERT INTO WasteManagement.Users (UserID, PasswordHash, RoleID)
    VALUES (@OpCNIC, HASHBYTES('SHA2_256', 'oppass' + CAST(@OpCounter AS VARCHAR(10))), 3);
    
    INSERT INTO WasteManagement.Operator (OperatorID, FullName, PhoneNumber, RouteID, WarehouseID, Status)
    VALUES (@OpCNIC, 'Operator ' + CAST(@OpCounter AS VARCHAR(10)),
        '0321-' + RIGHT('0000000' + CAST(2000000 + @OpCounter AS VARCHAR(7)), 7),
        ((@OpCounter - 1) % 10) + 1, ((@OpCounter - 1) % 5) + 1,
        CASE ((@OpCounter - 1) % 10) WHEN 0 THEN 'Busy' WHEN 1 THEN 'Offline' ELSE 'Available' END);
    
    SET @OpCounter = @OpCounter + 1;
END
PRINT 'Operators created: ' + CAST(@NumOperators AS VARCHAR(10));

-- Generate Transactions and Listings
PRINT 'Generating ' + CAST(@NumListings AS VARCHAR(10)) + ' waste listings...';
DECLARE @ListingCounter INT = 1;
WHILE @ListingCounter <= @NumListings
BEGIN
    DECLARE @CurrentCitizen VARCHAR(15), @CurrentOperator VARCHAR(15);
    
    SELECT TOP 1 @CurrentCitizen = CitizenID FROM WasteManagement.Citizen ORDER BY NEWID();
    SELECT TOP 1 @CurrentOperator = OperatorID FROM WasteManagement.Operator ORDER BY NEWID();
    
    DECLARE @CurrentCategory INT = ((@ListingCounter - 1) % 6) + 1;
    DECLARE @CurrentWeight DECIMAL(10,2) = CAST(((@ListingCounter % 50) + 5) AS DECIMAL(10,2)) / 2.0;
    DECLARE @CurrentStatus VARCHAR(50) = CASE (@ListingCounter % 10) 
        WHEN 9 THEN 'Pending' WHEN 8 THEN 'Collected' ELSE 'Completed' END;
    DECLARE @CurrentDate DATETIME = DATEADD(DAY, -((@ListingCounter % 180)), GETDATE());
    
    IF @CurrentStatus IN ('Collected', 'Completed')
    BEGIN
        DECLARE @EstPrice DECIMAL(10,2) = WasteManagement.fn_CalculatePrice(@CurrentCategory, @CurrentWeight);
        
        INSERT INTO WasteManagement.TransactionRecord (CitizenID, OperatorID, TotalAmount, PaymentStatus, 
            PaymentMethod, TransactionDate, VerificationCode)
        VALUES (@CurrentCitizen, @CurrentOperator, @EstPrice,
            CASE WHEN @CurrentStatus = 'Completed' THEN 'Completed' ELSE 'Pending' END,
            CASE ((@ListingCounter % 3)) WHEN 0 THEN 'Cash' WHEN 1 THEN 'Digital Wallet' ELSE 'Bank Transfer' END,
            @CurrentDate, 'VER-' + CAST(NEWID() AS VARCHAR(50)));
        
        INSERT INTO WasteManagement.WasteListing (CitizenID, CategoryID, Weight, Status, 
            EstimatedPrice, TransactionID, CreatedAt)
        VALUES (@CurrentCitizen, @CurrentCategory, @CurrentWeight, @CurrentStatus, 
            @EstPrice, SCOPE_IDENTITY(), @CurrentDate);
    END
    ELSE
    BEGIN
        INSERT INTO WasteManagement.WasteListing (CitizenID, CategoryID, Weight, Status, CreatedAt)
        VALUES (@CurrentCitizen, @CurrentCategory, @CurrentWeight, @CurrentStatus, @CurrentDate);
    END
    
    SET @ListingCounter = @ListingCounter + 1;
END
PRINT 'Waste listings created: ' + CAST(@NumListings AS VARCHAR(10));

-- Generate Collections
PRINT 'Generating collections...';
INSERT INTO WasteManagement.Collection (OperatorID, ListingID, CollectedDate, CollectedWeight, IsVerified, WarehouseID)
SELECT 
    tr.OperatorID, wl.ListingID, wl.CreatedAt, wl.Weight,
    CASE WHEN wl.Status = 'Completed' THEN 1 ELSE 0 END,
    ((wl.ListingID - 1) % 5) + 1
FROM WasteManagement.WasteListing wl
INNER JOIN WasteManagement.TransactionRecord tr ON wl.TransactionID = tr.TransactionID
WHERE wl.Status IN ('Collected', 'Completed')
ORDER BY wl.ListingID;
PRINT 'Collections created: ' + CAST(@@ROWCOUNT AS VARCHAR(10));

-- Generate Complaints
PRINT 'Generating ' + CAST(@NumComplaints AS VARCHAR(10)) + ' complaints...';
DECLARE @ComplaintCounter INT = 1;
WHILE @ComplaintCounter <= @NumComplaints
BEGIN
    DECLARE @RandCitizen VARCHAR(15), @RandOperator VARCHAR(15);
    SELECT TOP 1 @RandCitizen = CitizenID FROM WasteManagement.Citizen ORDER BY NEWID();
    SELECT TOP 1 @RandOperator = OperatorID FROM WasteManagement.Operator ORDER BY NEWID();
    
    INSERT INTO WasteManagement.Complaint (CitizenID, OperatorID, ComplaintType, Description, Status, CreatedAt)
    VALUES (@RandCitizen, @RandOperator,
        CASE ((@ComplaintCounter - 1) % 5) WHEN 0 THEN 'Late Pickup' WHEN 1 THEN 'Missed Collection' 
            WHEN 2 THEN 'Incorrect Weight' WHEN 3 THEN 'Payment Issue' ELSE 'Other' END,
        'Complaint description ' + CAST(@ComplaintCounter AS VARCHAR(10)),
        CASE ((@ComplaintCounter - 1) % 4) WHEN 0 THEN 'Open' WHEN 1 THEN 'In Progress' 
            WHEN 2 THEN 'Resolved' ELSE 'Closed' END,
        DATEADD(DAY, -((@ComplaintCounter % 90)), GETDATE()));
    
    SET @ComplaintCounter = @ComplaintCounter + 1;
END
PRINT 'Complaints created: ' + CAST(@NumComplaints AS VARCHAR(10));

-- Update Warehouse Stock
PRINT 'Updating warehouse stock...';
INSERT INTO WasteManagement.WarehouseStock (WarehouseID, CategoryID, CurrentWeight)
SELECT c.WarehouseID, wl.CategoryID, SUM(c.CollectedWeight)
FROM WasteManagement.Collection c
INNER JOIN WasteManagement.WasteListing wl ON c.ListingID = wl.ListingID
WHERE c.IsVerified = 1
GROUP BY c.WarehouseID, wl.CategoryID;
PRINT 'Warehouse stock updated successfully';

-- ============================================
-- FINAL SUMMARY & SAMPLE DATA
-- ============================================

PRINT '';
PRINT '==============================================';
PRINT 'DATABASE CREATION COMPLETE!';
PRINT '==============================================';
PRINT '';
PRINT 'FEATURES IMPLEMENTED:';
PRINT '  - Custom Schema: WasteManagement';
PRINT '  - Tables: 13 (3 partitioned)';
PRINT '  - Functions: 5';
PRINT '  - Stored Procedures: 8';
PRINT '  - Views: 8';
PRINT '  - Triggers: 6 (4 AFTER, 2 INSTEAD OF)';
PRINT '  - Indexes: 14';
PRINT '  - CTEs: Multiple (in views and procedures)';
PRINT '  - Partitioning: 3 tables partitioned';
PRINT '';
PRINT 'DATA SUMMARY:';

SELECT 'Users' as TableName, COUNT(*) as RecordCount FROM WasteManagement.Users
UNION ALL SELECT 'Citizens', COUNT(*) FROM WasteManagement.Citizen
UNION ALL SELECT 'Operators', COUNT(*) FROM WasteManagement.Operator
UNION ALL SELECT 'WasteListing (Partitioned)', COUNT(*) FROM WasteManagement.WasteListing
UNION ALL SELECT 'TransactionRecord (Partitioned)', COUNT(*) FROM WasteManagement.TransactionRecord
UNION ALL SELECT 'Collection (Partitioned)', COUNT(*) FROM WasteManagement.Collection
UNION ALL SELECT 'Complaint', COUNT(*) FROM WasteManagement.Complaint
ORDER BY RecordCount DESC;

PRINT '';
PRINT '==============================================';
PRINT 'SAMPLE DATA FROM EACH TABLE (TOP 5)';
PRINT '==============================================';
PRINT '';

-- UserRole
PRINT '--- UserRole ---';
SELECT TOP 5 * FROM WasteManagement.UserRole;

-- Area
PRINT '--- Area ---';
SELECT TOP 5 * FROM WasteManagement.Area;

-- Category
PRINT '--- Category ---';
SELECT TOP 5 * FROM WasteManagement.Category;

-- Users
PRINT '--- Users (Top 5) ---';
SELECT TOP 5 UserID, LEFT(PasswordHash, 20) + '...' AS PasswordHash, RoleID, CreatedAt 
FROM WasteManagement.Users;

-- Citizens
PRINT '--- Citizens (Top 5) ---';
SELECT TOP 5 * FROM WasteManagement.Citizen;

-- Operators
PRINT '--- Operators (Top 5) ---';
SELECT TOP 5 * FROM WasteManagement.Operator;

-- Routes
PRINT '--- Routes (Top 5) ---';
SELECT TOP 5 * FROM WasteManagement.Route;

-- Warehouses
PRINT '--- Warehouses ---';
SELECT TOP 5 * FROM WasteManagement.Warehouse;

-- WasteListing
PRINT '--- WasteListing (Top 5) ---';
SELECT TOP 5 ListingID, CitizenID, CategoryID, Weight, Status, EstimatedPrice, TransactionID, CreatedAt 
FROM WasteManagement.WasteListing
ORDER BY CreatedAt DESC;

-- TransactionRecord
PRINT '--- TransactionRecord (Top 5) ---';
SELECT TOP 5 TransactionID, CitizenID, OperatorID, TotalAmount, PaymentStatus, PaymentMethod, TransactionDate,
       LEFT(VerificationCode, 30) + '...' AS VerificationCode
FROM WasteManagement.TransactionRecord
ORDER BY TransactionDate DESC;

-- Collection
PRINT '--- Collection (Top 5) ---';
SELECT TOP 5 CollectionID, OperatorID, ListingID, CollectedDate, CollectedWeight, IsVerified, WarehouseID
FROM WasteManagement.Collection
ORDER BY CollectedDate DESC;

-- Complaints
PRINT '--- Complaints (Top 5) ---';
SELECT TOP 5 ComplaintID, CitizenID, OperatorID, ComplaintType, Status, CreatedAt
FROM WasteManagement.Complaint
ORDER BY CreatedAt DESC;

-- WarehouseStock
PRINT '--- WarehouseStock ---';
SELECT TOP 5 * FROM WasteManagement.WarehouseStock
ORDER BY LastUpdated DESC;


PRINT '';
PRINT '==============================================';
PRINT 'Database ready for use!';
PRINT '==============================================';
GO