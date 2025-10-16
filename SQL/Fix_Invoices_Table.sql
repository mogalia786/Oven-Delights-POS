-- Fix Invoices table - Add SalesID if missing and recreate properly

-- Check if Invoices table exists
IF EXISTS (SELECT * FROM sys.tables WHERE name = 'Invoices')
BEGIN
    -- Add SalesID if it doesn't exist
    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Invoices') AND name = 'SalesID')
    BEGIN
        ALTER TABLE Invoices ADD SalesID INT NULL
        PRINT 'Added SalesID column to Invoices table'
    END
    
    -- Add BranchID if it doesn't exist
    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Invoices') AND name = 'BranchID')
    BEGIN
        ALTER TABLE Invoices ADD BranchID INT NULL
        PRINT 'Added BranchID column to Invoices table'
    END
    
    -- Add SaleDate if it doesn't exist
    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Invoices') AND name = 'SaleDate')
    BEGIN
        ALTER TABLE Invoices ADD SaleDate DATETIME NULL
        PRINT 'Added SaleDate column to Invoices table'
    END
    
    -- Add CashierID if it doesn't exist
    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Invoices') AND name = 'CashierID')
    BEGIN
        ALTER TABLE Invoices ADD CashierID INT NULL
        PRINT 'Added CashierID column to Invoices table'
    END
    
    PRINT 'Invoices table structure updated!'
END
ELSE
BEGIN
    PRINT 'Invoices table does not exist. Run Create_Sales_Tables.sql to create it.'
END
GO

-- Now run Create_Sales_Tables.sql to create the Sales table
PRINT ''
PRINT '========================================='
PRINT 'IMPORTANT: Now run Create_Sales_Tables.sql'
PRINT 'to create the Sales table!'
PRINT '========================================='
GO
