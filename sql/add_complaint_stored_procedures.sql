-- ============================================
-- Add Complaint Management Stored Procedures
-- ============================================
USE SmartWasteDB;
GO

PRINT '==============================================';
PRINT 'Adding Complaint Management Stored Procedures';
PRINT '==============================================';
GO

-- Procedure: Get complaints for a specific operator
CREATE OR ALTER PROCEDURE WasteManagement.sp_GetOperatorComplaints
    @OperatorID VARCHAR(15)
AS
BEGIN
    SET NOCOUNT ON;
    
    SELECT 
        ComplaintID,
        ComplaintType,
        Description,
        Status,
        CreatedAt,
        CitizenID,
        CitizenName,
        PhoneNumber,
        OperatorID,
        OperatorName,
        RouteName,
        AreaName,
        DaysOpen
    FROM WasteManagement.vw_ActiveComplaints
    WHERE OperatorID = @OperatorID
    ORDER BY CreatedAt DESC;
END;
GO

-- Procedure: Update complaint status
CREATE OR ALTER PROCEDURE WasteManagement.sp_UpdateComplaintStatus
    @ComplaintID INT,
    @NewStatus VARCHAR(20),
    @RowsAffected INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    
    UPDATE WasteManagement.Complaint
    SET Status = @NewStatus
    WHERE ComplaintID = @ComplaintID;
    
    SET @RowsAffected = @@ROWCOUNT;
END;
GO

PRINT '✅ Complaint stored procedures created successfully';
PRINT 'Available procedures:';
PRINT '  - sp_GetOperatorComplaints';
PRINT '  - sp_UpdateComplaintStatus';
GO
