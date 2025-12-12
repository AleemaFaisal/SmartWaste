use SmartWasteDB
go

select *
from wastemanagement.users
where UserID = '35203-0000003-2'

select *
from wastemanagement.warehouse

SELECT *
from WasteManagement.Category

Select *
from WasteManagement.Operator

UPDATE WasteManagement.Category
set BasePricePerKg = 350
where CategoryID = 5;

SELECT COLUMN_NAME, DATA_TYPE, NUMERIC_PRECISION, NUMERIC_SCALE
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME = 'Category' AND COLUMN_NAME = 'BasePricePerKg';

ALTER LOGIN sa ENABLE;
ALTER LOGIN sa WITH PASSWORD = '1753*dbcd frontend';

USE SmartWasteDB;
EXEC sp_change_users_login 'Auto_Fix', 'sa';


USE SmartWasteDB;
CREATE USER sa FOR LOGIN sa;
EXEC sp_addrolemember 'db_owner', 'sa';

USE master;
ALTER DATABASE SmartWasteDB SET MULTI_USER WITH ROLLBACK IMMEDIATE;

USE master;
GO