CREATE DATABASE AppDB;
GO

USE AppDB;
GO

CREATE TABLE Users
(
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Name NVARCHAR(100)
);
GO

INSERT INTO Users
    (Name)
VALUES
    ('Alice'),
    ('Bob'),
    ('Charlie');
GO

-- Stored Procedure
CREATE OR ALTER PROCEDURE GetUsers
AS
BEGIN
    SELECT Id, Name
    FROM Users;
END
GO
