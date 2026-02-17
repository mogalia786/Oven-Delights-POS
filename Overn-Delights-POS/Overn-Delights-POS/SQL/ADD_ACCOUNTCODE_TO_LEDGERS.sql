-- Add AccountCode Column to Ledgers and GeneralJournal Tables
-- ==============================================================

-- 1. Add AccountCode to Ledgers table
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS 
               WHERE TABLE_NAME = 'Ledgers' AND COLUMN_NAME = 'AccountCode')
BEGIN
    ALTER TABLE Ledgers ADD AccountCode NVARCHAR(20) NULL;
    PRINT '✓ AccountCode column added to Ledgers table';
    
    -- Update existing ledgers with account codes
    UPDATE Ledgers SET AccountCode = '1100' WHERE LedgerName = 'Cash';
    UPDATE Ledgers SET AccountCode = '4000' WHERE LedgerName = 'Sales Revenue';
    UPDATE Ledgers SET AccountCode = '2100' WHERE LedgerName = 'VAT Output';
    UPDATE Ledgers SET AccountCode = '5000' WHERE LedgerName = 'Cost of Sales';
    UPDATE Ledgers SET AccountCode = '1300' WHERE LedgerName = 'Inventory';
    
    PRINT '✓ Account codes updated for existing ledgers';
END
ELSE
BEGIN
    PRINT '! AccountCode column already exists in Ledgers';
END
GO

-- 2. Add AccountCode to GeneralJournal table
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS 
               WHERE TABLE_NAME = 'GeneralJournal' AND COLUMN_NAME = 'AccountCode')
BEGIN
    ALTER TABLE GeneralJournal ADD AccountCode NVARCHAR(20) NULL;
    PRINT '✓ AccountCode column added to GeneralJournal table';
END
ELSE
BEGIN
    PRINT '! AccountCode column already exists in GeneralJournal';
END
GO

-- 3. Add AccountName to GeneralJournal if missing
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS 
               WHERE TABLE_NAME = 'GeneralJournal' AND COLUMN_NAME = 'AccountName')
BEGIN
    ALTER TABLE GeneralJournal ADD AccountName NVARCHAR(200) NULL;
    PRINT '✓ AccountName column added to GeneralJournal table';
END
ELSE
BEGIN
    PRINT '! AccountName column already exists in GeneralJournal';
END
GO

-- Show updated ledgers
SELECT 
    LedgerID,
    LedgerName,
    LedgerType,
    AccountCode,
    IsActive
FROM Ledgers
ORDER BY AccountCode;
GO

PRINT '';
PRINT 'All tables updated successfully!';
GO
