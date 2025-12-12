-- ============================================
-- Create Test Data for Pending Pickups
-- ============================================
USE SmartWasteDB;
GO

PRINT '==============================================';
PRINT 'Creating test pending waste listings...';
PRINT '==============================================';
GO

-- Get the operator's route and area information
DECLARE @OperatorID VARCHAR(15) = '42000-0300001-0';
DECLARE @RouteID INT;
DECLARE @AreaID INT;

SELECT @RouteID = RouteID FROM WasteManagement.Operator WHERE OperatorID = @OperatorID;
SELECT @AreaID = AreaID FROM WasteManagement.Route WHERE RouteID = @RouteID;

PRINT 'Operator Route ID: ' + CAST(@RouteID AS VARCHAR);
PRINT 'Route Area ID: ' + CAST(@AreaID AS VARCHAR);

-- Get existing citizens from the SAME AREA as the operator's route
DECLARE @CitizenID1 VARCHAR(15);
DECLARE @CitizenID2 VARCHAR(15);
DECLARE @CitizenID3 VARCHAR(15);

-- Get the first 3 citizens from the operator's service area
SELECT TOP 1 @CitizenID1 = CitizenID 
FROM WasteManagement.Citizen 
WHERE AreaID = @AreaID 
ORDER BY CitizenID;

SELECT TOP 1 @CitizenID2 = CitizenID 
FROM WasteManagement.Citizen 
WHERE AreaID = @AreaID AND CitizenID > @CitizenID1 
ORDER BY CitizenID;

SELECT TOP 1 @CitizenID3 = CitizenID 
FROM WasteManagement.Citizen 
WHERE AreaID = @AreaID AND CitizenID > @CitizenID2 
ORDER BY CitizenID;

-- Fallback if not enough citizens in the area
IF @CitizenID2 IS NULL SET @CitizenID2 = @CitizenID1;
IF @CitizenID3 IS NULL SET @CitizenID3 = @CitizenID1;

PRINT 'Using Citizens from Area ' + CAST(@AreaID AS VARCHAR) + ':';
PRINT '  Citizen 1: ' + @CitizenID1;
PRINT '  Citizen 2: ' + @CitizenID2;
PRINT '  Citizen 3: ' + @CitizenID3;

-- Listing 1: Plastic waste from Citizen 1
INSERT INTO WasteManagement.WasteListing (CitizenID, CategoryID, Weight, Status, CreatedAt)
VALUES (@CitizenID1, 1, 8.5, 'Pending', GETDATE());
PRINT '✓ Added Plastic waste listing (8.5 kg) for Citizen 1';

-- Listing 2: Paper waste from Citizen 1
INSERT INTO WasteManagement.WasteListing (CitizenID, CategoryID, Weight, Status, CreatedAt)
VALUES (@CitizenID1, 2, 12.0, 'Pending', GETDATE());
PRINT '✓ Added Paper waste listing (12.0 kg) for Citizen 1';

-- Listing 3: Glass waste from Citizen 2
INSERT INTO WasteManagement.WasteListing (CitizenID, CategoryID, Weight, Status, CreatedAt)
VALUES (@CitizenID2, 3, 6.3, 'Pending', GETDATE());
PRINT '✓ Added Glass waste listing (6.3 kg) for Citizen 2';

-- Listing 4: Metal waste from Citizen 2
INSERT INTO WasteManagement.WasteListing (CitizenID, CategoryID, Weight, Status, CreatedAt)
VALUES (@CitizenID2, 4, 15.8, 'Pending', GETDATE());
PRINT '✓ Added Metal waste listing (15.8 kg) for Citizen 2';

-- Listing 5: Organic waste from Citizen 3
INSERT INTO WasteManagement.WasteListing (CitizenID, CategoryID, Weight, Status, CreatedAt)
VALUES (@CitizenID3, 5, 20.0, 'Pending', GETDATE());
PRINT '✓ Added Organic waste listing (20.0 kg) for Citizen 3';

-- Listing 6: E-waste from Citizen 3
INSERT INTO WasteManagement.WasteListing (CitizenID, CategoryID, Weight, Status, CreatedAt)
VALUES (@CitizenID3, 6, 4.5, 'Pending', GETDATE());
PRINT '✓ Added E-waste listing (4.5 kg) for Citizen 3';

-- Listing 7: Plastic waste from Citizen 2
INSERT INTO WasteManagement.WasteListing (CitizenID, CategoryID, Weight, Status, CreatedAt)
VALUES (@CitizenID2, 1, 10.2, 'Pending', GETDATE());
PRINT '✓ Added Plastic waste listing (10.2 kg) for Citizen 2';

GO

-- Verify the created listings
PRINT '';
PRINT '==============================================';
PRINT 'Verification - Newly Created Pending Pickups:';
PRINT '==============================================';

SELECT 
    wl.ListingID,
    c.FullName AS CitizenName,
    cat.CategoryName,
    wl.Weight,
    wl.EstimatedPrice,
    wl.Status,
    wl.CreatedAt
FROM WasteManagement.WasteListing wl
INNER JOIN WasteManagement.Citizen c ON wl.CitizenID = c.CitizenID
INNER JOIN WasteManagement.Category cat ON wl.CategoryID = cat.CategoryID
WHERE wl.Status = 'Pending'
    AND wl.CreatedAt >= CAST(GETDATE() AS DATE)
ORDER BY wl.CreatedAt DESC;

GO

PRINT '';
PRINT '==============================================';
PRINT 'Test data creation complete!';
PRINT 'You can now test the collection feature.';
PRINT '==============================================';


-- Clean up test listings created today
-- DELETE FROM WasteManagement.WasteListing 
-- WHERE Status = 'Pending' 
--   AND CreatedAt >= CAST(GETDATE() AS DATE);