-- Update Invoices table to add missing columns for proper receipt recreation

-- Add BranchID if it doesn't exist
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Invoices') AND name = 'BranchID')
BEGIN
    ALTER TABLE Invoices ADD BranchID INT NULL
    PRINT 'Added BranchID column to Invoices table'
END
GO

-- Add SaleDate if it doesn't exist
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Invoices') AND name = 'SaleDate')
BEGIN
    ALTER TABLE Invoices ADD SaleDate DATETIME NULL
    PRINT 'Added SaleDate column to Invoices table'
END
GO

-- Add CashierID if it doesn't exist
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Invoices') AND name = 'CashierID')
BEGIN
    ALTER TABLE Invoices ADD CashierID INT NULL
    PRINT 'Added CashierID column to Invoices table'
END
GO

-- Update existing records with data from Sales table (if Sales table exists)
IF EXISTS (SELECT * FROM sys.tables WHERE name = 'Sales')
BEGIN
    UPDATE i
    SET 
        i.BranchID = s.BranchID,
        i.SaleDate = s.SaleDate,
        i.CashierID = s.CashierID
    FROM Invoices i
    INNER JOIN Sales s ON i.SalesID = s.SalesID
    WHERE i.BranchID IS NULL OR i.SaleDate IS NULL OR i.CashierID IS NULL
    PRINT 'Updated existing Invoices records from Sales table'
END
ELSE
BEGIN
    PRINT 'WARNING: Sales table does not exist. Run Create_Sales_Tables.sql first!'
END
GO

-- Add foreign key constraint for BranchID if it doesn't exist
IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_Invoices_Branch')
BEGIN
    ALTER TABLE Invoices
    ADD CONSTRAINT FK_Invoices_Branch FOREIGN KEY (BranchID) REFERENCES Branches(BranchID)
    PRINT 'Added FK_Invoices_Branch constraint'
END
GO

-- Add index on BranchID if it doesn't exist
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Invoices_BranchID' AND object_id = OBJECT_ID('Invoices'))
BEGIN
    CREATE INDEX IX_Invoices_BranchID ON Invoices(BranchID)
    PRINT 'Created index IX_Invoices_BranchID'
END
GO

PRINT 'Invoices table updated successfully!'
PRINT 'Now each line item contains:'
PRINT '  - InvoiceNumber (for grouping)'
PRINT '  - BranchID (for branch identification)'
PRINT '  - SaleDate (for date/time)'
PRINT '  - CashierID (for cashier identification)'
PRINT '  - Product details (ProductID, ItemCode, ProductName, Qty, Price, Total)'
PRINT 'This allows full receipt recreation and line-item returns/amendments!'
GO
