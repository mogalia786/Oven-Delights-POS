-- Complete GL Tables Setup
-- =========================
-- Run this script to set up all GL tables in the correct order

PRINT '========================================';
PRINT 'STEP 1: Creating GeneralJournal Table';
PRINT '========================================';

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'GeneralJournal')
BEGIN
    CREATE TABLE GeneralJournal (
        JournalID INT IDENTITY(1,1) PRIMARY KEY,
        TransactionDate DATETIME NOT NULL DEFAULT GETDATE(),
        JournalType NVARCHAR(50),
        Reference NVARCHAR(100),
        LedgerID INT,
        AccountCode NVARCHAR(20),
        AccountName NVARCHAR(200),
        Debit DECIMAL(18,2) DEFAULT 0,
        Credit DECIMAL(18,2) DEFAULT 0,
        Description NVARCHAR(500),
        BranchID INT,
        CreatedBy NVARCHAR(100),
        CreatedDate DATETIME NOT NULL DEFAULT GETDATE(),
        
        CONSTRAINT FK_GeneralJournal_Branch FOREIGN KEY (BranchID) 
            REFERENCES Branches(BranchID)
    );
    
    CREATE INDEX IX_GeneralJournal_Date ON GeneralJournal(TransactionDate);
    CREATE INDEX IX_GeneralJournal_Reference ON GeneralJournal(Reference);
    CREATE INDEX IX_GeneralJournal_AccountCode ON GeneralJournal(AccountCode);
    CREATE INDEX IX_GeneralJournal_Branch ON GeneralJournal(BranchID);
    
    PRINT '✓ GeneralJournal table created';
END
ELSE
BEGIN
    PRINT '! GeneralJournal table already exists';
    
    -- Add missing columns if table exists
    IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS 
                   WHERE TABLE_NAME = 'GeneralJournal' AND COLUMN_NAME = 'AccountCode')
    BEGIN
        ALTER TABLE GeneralJournal ADD AccountCode NVARCHAR(20) NULL;
        PRINT '✓ AccountCode column added to GeneralJournal';
    END
    
    IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS 
                   WHERE TABLE_NAME = 'GeneralJournal' AND COLUMN_NAME = 'AccountName')
    BEGIN
        ALTER TABLE GeneralJournal ADD AccountName NVARCHAR(200) NULL;
        PRINT '✓ AccountName column added to GeneralJournal';
    END
END
GO

PRINT '';
PRINT '========================================';
PRINT 'STEP 2: Updating Ledgers Table';
PRINT '========================================';

-- Check if Ledgers table exists and add AccountCode column
IF EXISTS (SELECT * FROM sys.tables WHERE name = 'Ledgers')
BEGIN
    IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS 
                   WHERE TABLE_NAME = 'Ledgers' AND COLUMN_NAME = 'AccountCode')
    BEGIN
        ALTER TABLE Ledgers ADD AccountCode NVARCHAR(20) NULL;
        PRINT '✓ AccountCode column added to Ledgers';
    END
    ELSE
    BEGIN
        PRINT '! AccountCode column already exists in Ledgers';
    END
END
ELSE
BEGIN
    PRINT '⚠ Ledgers table does not exist - skipping';
END
GO

-- Update existing ledgers with account codes (separate batch after ALTER TABLE)
IF EXISTS (SELECT * FROM sys.tables WHERE name = 'Ledgers')
    AND EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS 
                WHERE TABLE_NAME = 'Ledgers' AND COLUMN_NAME = 'AccountCode')
BEGIN
    UPDATE Ledgers SET AccountCode = '1100' WHERE LedgerName LIKE '%Cash%' AND AccountCode IS NULL;
    UPDATE Ledgers SET AccountCode = '4000' WHERE LedgerName LIKE '%Sales%Revenue%' AND AccountCode IS NULL;
    UPDATE Ledgers SET AccountCode = '2100' WHERE LedgerName LIKE '%VAT%Output%' AND AccountCode IS NULL;
    UPDATE Ledgers SET AccountCode = '5000' WHERE LedgerName LIKE '%Cost%Sales%' AND AccountCode IS NULL;
    UPDATE Ledgers SET AccountCode = '1300' WHERE LedgerName LIKE '%Inventory%' AND AccountCode IS NULL;
    
    PRINT '✓ Account codes updated for existing ledgers';
END
GO

PRINT '';
PRINT '========================================';
PRINT 'STEP 3: Verification';
PRINT '========================================';

-- Show GeneralJournal structure
PRINT 'GeneralJournal columns:';
SELECT COLUMN_NAME, DATA_TYPE, IS_NULLABLE
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME = 'GeneralJournal'
ORDER BY ORDINAL_POSITION;

PRINT '';
PRINT 'Ledgers with AccountCode:';
IF EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS 
           WHERE TABLE_NAME = 'Ledgers' AND COLUMN_NAME = 'AccountCode')
BEGIN
    SELECT LedgerID, LedgerName, LedgerType, AccountCode
    FROM Ledgers
    WHERE AccountCode IS NOT NULL
    ORDER BY AccountCode;
END
ELSE
BEGIN
    PRINT '⚠ AccountCode column does not exist in Ledgers table';
    SELECT LedgerID, LedgerName, LedgerType
    FROM Ledgers
    ORDER BY LedgerID;
END

PRINT '';
PRINT '========================================';
PRINT '✓ GL TABLES SETUP COMPLETE!';
PRINT '========================================';
PRINT 'You can now process POS sales with GL posting.';
GO
