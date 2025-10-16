-- Drop and recreate Sales and Invoices tables with correct structure

-- Drop ALL foreign key constraints on Invoices table
DECLARE @sql NVARCHAR(MAX) = ''
SELECT @sql += 'ALTER TABLE ' + QUOTENAME(OBJECT_SCHEMA_NAME(parent_object_id)) + '.' + QUOTENAME(OBJECT_NAME(parent_object_id)) + 
               ' DROP CONSTRAINT ' + QUOTENAME(name) + ';'
FROM sys.foreign_keys
WHERE referenced_object_id = OBJECT_ID('Invoices') OR parent_object_id = OBJECT_ID('Invoices')

IF @sql <> ''
BEGIN
    EXEC sp_executesql @sql
    PRINT 'Dropped all foreign key constraints on Invoices table'
END
GO

-- Drop existing tables (if they exist)
IF EXISTS (SELECT * FROM sys.tables WHERE name = 'Invoices')
BEGIN
    DROP TABLE Invoices
    PRINT 'Dropped Invoices table'
END
GO

IF EXISTS (SELECT * FROM sys.tables WHERE name = 'Sales')
BEGIN
    DROP TABLE Sales
    PRINT 'Dropped Sales table'
END
GO

-- Create Sales table (Invoice Header)
CREATE TABLE Sales (
    SalesID INT IDENTITY(1,1) PRIMARY KEY,
    InvoiceNumber NVARCHAR(50) NOT NULL UNIQUE,
    BranchID INT NOT NULL,
    CashierID INT NOT NULL,
    SaleDate DATETIME NOT NULL,
    Subtotal DECIMAL(18,2) NOT NULL,
    TaxAmount DECIMAL(18,2) NOT NULL,
    TotalAmount DECIMAL(18,2) NOT NULL,
    PaymentMethod NVARCHAR(20) NOT NULL, -- CASH, CARD, SPLIT
    CashAmount DECIMAL(18,2) DEFAULT 0,
    CardAmount DECIMAL(18,2) DEFAULT 0,
    CreatedDate DATETIME DEFAULT GETDATE(),
    CONSTRAINT FK_Sales_Branch FOREIGN KEY (BranchID) REFERENCES Branches(BranchID)
)

CREATE INDEX IX_Sales_InvoiceNumber ON Sales(InvoiceNumber)
CREATE INDEX IX_Sales_BranchID ON Sales(BranchID)
CREATE INDEX IX_Sales_SaleDate ON Sales(SaleDate)

PRINT 'Created Sales table'
GO

-- Create Invoices table (Line Items for returns/amendments)
CREATE TABLE Invoices (
    InvoiceLineID INT IDENTITY(1,1) PRIMARY KEY,
    InvoiceNumber NVARCHAR(50) NOT NULL,
    SalesID INT NOT NULL,
    BranchID INT NOT NULL,
    ProductID INT NOT NULL,
    ItemCode NVARCHAR(50) NOT NULL,
    ProductName NVARCHAR(200) NOT NULL,
    Quantity DECIMAL(18,3) NOT NULL,
    UnitPrice DECIMAL(18,2) NOT NULL,
    LineTotal DECIMAL(18,2) NOT NULL,
    SaleDate DATETIME NOT NULL,
    CashierID INT NOT NULL,
    CreatedDate DATETIME DEFAULT GETDATE(),
    CONSTRAINT FK_Invoices_Sales FOREIGN KEY (SalesID) REFERENCES Sales(SalesID),
    CONSTRAINT FK_Invoices_Branch FOREIGN KEY (BranchID) REFERENCES Branches(BranchID)
)

CREATE INDEX IX_Invoices_InvoiceNumber ON Invoices(InvoiceNumber)
CREATE INDEX IX_Invoices_SalesID ON Invoices(SalesID)
CREATE INDEX IX_Invoices_ProductID ON Invoices(ProductID)
CREATE INDEX IX_Invoices_BranchID ON Invoices(BranchID)

PRINT 'Created Invoices table'
GO

PRINT ''
PRINT '========================================='
PRINT 'SUCCESS! Tables created with correct structure'
PRINT '========================================='
PRINT ''
PRINT 'Sales table includes:'
PRINT '  - InvoiceNumber, BranchID, CashierID'
PRINT '  - SaleDate, Subtotal, TaxAmount, TotalAmount'
PRINT '  - PaymentMethod, CashAmount, CardAmount'
PRINT ''
PRINT 'Invoices table includes:'
PRINT '  - InvoiceNumber, SalesID, BranchID'
PRINT '  - ProductID, ItemCode, ProductName'
PRINT '  - Quantity, UnitPrice, LineTotal'
PRINT '  - SaleDate, CashierID'
PRINT ''
PRINT 'Ready to process sales!'
GO
