use smartwastedb
go

select *
from wastemanagement.users
where UserID = '35203-0000003-2'

select *
from wastemanagement.warehouse

SELECT *
from WasteManagement.Category


UPDATE WasteManagement.Category
set BasePricePerKg = 350
where CategoryID = 5;

SELECT COLUMN_NAME, DATA_TYPE, NUMERIC_PRECISION, NUMERIC_SCALE
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME = 'Category' AND COLUMN_NAME = 'BasePricePerKg';
