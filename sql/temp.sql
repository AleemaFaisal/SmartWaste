


USE SmartWasteDB;
GO

-- 1. CONFIGURATION: Set your desired credentials here
DECLARE @NewCNIC VARCHAR(15) = '35201-0000001-0'; 
DECLARE @NewPassword VARCHAR(50) = 'password1';
DECLARE @NewName VARCHAR(100) = 'Test Citizen';
DECLARE @NewPhone VARCHAR(20) = '0300-1233333';

BEGIN TRANSACTION;
BEGIN TRY
    -- 2. Create the User Login (RoleID 3 = Operator)
    -- We use HASHBYTES to encrypt the password exactly like the app does
    INSERT INTO WasteManagement.Users (UserID, PasswordHash, RoleID)
    VALUES (
        @NewCNIC,
        CONVERT(VARCHAR(64), HASHBYTES('SHA2_256', @NewPassword), 2),  -- hex string!
        3
    );


    -- 3. Create the Operator Profile
    -- We assign them to Warehouse 1 and Route 1 by default so they have data to see
    INSERT INTO WasteManagement.Operator (OperatorID, FullName, PhoneNumber, RouteID, WarehouseID, Status)
    VALUES (
        @NewCNIC, 
        @NewName, 
        @NewPhone, 
        1, -- Default Route ID (Make sure RouteID 1 exists)
        1, -- Default Warehouse ID (Make sure WarehouseID 1 exists)
        'Available'
    );

    COMMIT TRANSACTION;
    PRINT '✅ Operator created successfully with CNIC: ' + @NewCNIC;
END TRY
BEGIN CATCH
    ROLLBACK TRANSACTION;
    PRINT '❌ Error creating operator: ' + ERROR_MESSAGE();
END CATCH;
GO

SELECT UserID, roleID, PasswordHash 
FROM WasteManagement.Users
WHERE UserID = '42000-0300001-0';


USE SmartWasteDB;
GO

DECLARE @TargetCNIC VARCHAR(15) = '42000-0300001-0';
DECLARE @NewPassword VARCHAR(50) = 'oppass1'; 

-- Update the user with the CORRECT Hex String format (Style 2)
UPDATE WasteManagement.Users
SET PasswordHash = CONVERT(VARCHAR(64), HASHBYTES('SHA2_256', @NewPassword), 2)
WHERE UserID = @TargetCNIC;

-- Verify the result
SELECT UserID, RoleID, PasswordHash 
FROM WasteManagement.Users
WHERE UserID = @TargetCNIC;
GO


-- SELECT 
--     c.CategoryName,
--     ws.CurrentWeight,
--     ws.LastUpdated,
--     DATEDIFF(SECOND, ws.LastUpdated, GETDATE()) as SecondsAgo
-- FROM WasteManagement.WarehouseStock ws
-- INNER JOIN WasteManagement.Category c ON ws.CategoryID = c.CategoryID
-- WHERE ws.WarehouseID = 1
-- ORDER BY ws.CategoryID;