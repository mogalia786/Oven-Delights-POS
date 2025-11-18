-- Create POS_Customers table for customer lookup and auto-populate
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'POS_Customers')
BEGIN
    CREATE TABLE dbo.POS_Customers (
        CustomerID INT IDENTITY(1,1) PRIMARY KEY,
        CellNumber NVARCHAR(20) NOT NULL UNIQUE,
        FirstName NVARCHAR(100) NOT NULL,
        Surname NVARCHAR(100) NOT NULL,
        Email NVARCHAR(255) NULL,
        CreatedDate DATETIME DEFAULT GETDATE(),
        LastOrderDate DATETIME NULL,
        TotalOrders INT DEFAULT 0,
        IsActive BIT DEFAULT 1
    );

    CREATE INDEX IX_POS_Customers_CellNumber ON dbo.POS_Customers(CellNumber);
    
    PRINT 'POS_Customers table created successfully';
END
ELSE
BEGIN
    PRINT 'POS_Customers table already exists';
END
GO

-- Sample data
IF NOT EXISTS (SELECT * FROM dbo.POS_Customers WHERE CellNumber = '0765144058')
BEGIN
    INSERT INTO dbo.POS_Customers (CellNumber, FirstName, Surname, Email)
    VALUES ('0765144058', 'MINNIE', 'SMITH', 'minnie@example.com');
    
    PRINT 'Sample customer added';
END
GO
