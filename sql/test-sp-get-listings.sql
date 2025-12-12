-- Test the stored procedure directly
-- Replace the CitizenID with your actual CitizenID

-- First, check what listings exist
SELECT
    ListingID,
    CitizenID,
    CategoryID,
    Weight,
    Status,
    CreatedAt,
    LEN(CitizenID) as CitizenIDLength,
    DATALENGTH(CitizenID) as CitizenIDBytes
FROM WasteManagement.WasteListing
ORDER BY CreatedAt DESC;

-- Then test the stored procedure
EXEC WasteManagement.sp_GetCitizenListings @CitizenID = '35201-0000001-0';

-- Check for any leading/trailing spaces
SELECT
    ListingID,
    '[' + CitizenID + ']' as CitizenIDWithBrackets,
    LEN(CitizenID) as Length
FROM WasteManagement.WasteListing;
