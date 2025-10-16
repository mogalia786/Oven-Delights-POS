-- =============================================
-- Create Stock Adjustment Ledgers and Journals
-- =============================================
-- NOTE: This creates ledgers for ALL branches
-- Run this script for each branch or modify to loop through branches
-- =============================================

-- First check if Ledgers table has BranchID column
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Ledgers') AND name = 'BranchID')
BEGIN
    ALTER TABLE Ledgers ADD BranchID INT NULL
    PRINT 'Added BranchID column to Ledgers table'
END
GO

-- Drop the UNIQUE constraint on LedgerName (we need same name for multiple branches)
IF EXISTS (SELECT * FROM sys.indexes WHERE name = 'UQ__Ledgers__6A639FF0504E1BD1' AND object_id = OBJECT_ID('Ledgers'))
BEGIN
    ALTER TABLE Ledgers DROP CONSTRAINT UQ__Ledgers__6A639FF0504E1BD1
    PRINT 'Dropped UNIQUE constraint on LedgerName'
END
GO

-- Create a new UNIQUE constraint on LedgerName + BranchID combination
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'UQ_Ledgers_Name_Branch' AND object_id = OBJECT_ID('Ledgers'))
BEGIN
    ALTER TABLE Ledgers ADD CONSTRAINT UQ_Ledgers_Name_Branch UNIQUE (LedgerName, BranchID)
    PRINT 'Created UNIQUE constraint on LedgerName + BranchID'
END
GO

-- Create ledgers for each branch
DECLARE @BranchID INT
DECLARE @BranchName NVARCHAR(100)

DECLARE branch_cursor CURSOR FOR 
SELECT BranchID, BranchName FROM Branches WHERE IsActive = 1

OPEN branch_cursor
FETCH NEXT FROM branch_cursor INTO @BranchID, @BranchName

WHILE @@FETCH_STATUS = 0
BEGIN
    PRINT '----------------------------------------'
    PRINT 'Creating ledgers for Branch: ' + @BranchName + ' (ID: ' + CAST(@BranchID AS VARCHAR) + ')'
    
    -- Check if Inventory ledger exists for this branch
    IF NOT EXISTS (SELECT * FROM Ledgers WHERE LedgerName = 'Inventory' AND BranchID = @BranchID)
    BEGIN
        INSERT INTO Ledgers (LedgerName, LedgerType, BranchID, IsActive)
        VALUES ('Inventory', 'Asset', @BranchID, 1)
        PRINT '  ✓ Inventory ledger created'
    END
    ELSE
    BEGIN
        PRINT '  - Inventory ledger already exists'
    END
    
    -- Check if Cost of Sales ledger exists for this branch
    IF NOT EXISTS (SELECT * FROM Ledgers WHERE LedgerName = 'Cost of Sales' AND BranchID = @BranchID)
    BEGIN
        INSERT INTO Ledgers (LedgerName, LedgerType, BranchID, IsActive)
        VALUES ('Cost of Sales', 'Expense', @BranchID, 1)
        PRINT '  ✓ Cost of Sales ledger created'
    END
    ELSE
    BEGIN
        PRINT '  - Cost of Sales ledger already exists'
    END
    
    -- Check if Stock Write-Off ledger exists for this branch
    IF NOT EXISTS (SELECT * FROM Ledgers WHERE LedgerName = 'Stock Write-Off' AND BranchID = @BranchID)
    BEGIN
        INSERT INTO Ledgers (LedgerName, LedgerType, BranchID, IsActive)
        VALUES ('Stock Write-Off', 'Expense', @BranchID, 1)
        PRINT '  ✓ Stock Write-Off ledger created'
    END
    ELSE
    BEGIN
        PRINT '  - Stock Write-Off ledger already exists'
    END
    
    -- Check if Sales Returns ledger exists for this branch
    IF NOT EXISTS (SELECT * FROM Ledgers WHERE LedgerName = 'Sales Returns' AND BranchID = @BranchID)
    BEGIN
        INSERT INTO Ledgers (LedgerName, LedgerType, BranchID, IsActive)
        VALUES ('Sales Returns', 'Contra-Revenue', @BranchID, 1)
        PRINT '  ✓ Sales Returns ledger created'
    END
    ELSE
    BEGIN
        PRINT '  - Sales Returns ledger already exists'
    END
    
    FETCH NEXT FROM branch_cursor INTO @BranchID, @BranchName
END

CLOSE branch_cursor
DEALLOCATE branch_cursor

PRINT 'Stock adjustment ledgers setup completed!'
GO
