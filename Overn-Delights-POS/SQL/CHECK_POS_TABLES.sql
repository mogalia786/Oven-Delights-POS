-- Check POS Transaction Tables
-- =============================

-- List all POS-related tables
SELECT 
    TABLE_NAME,
    TABLE_TYPE
FROM INFORMATION_SCHEMA.TABLES
WHERE TABLE_NAME LIKE 'POS%'
ORDER BY TABLE_NAME;

-- Check if GeneralJournal table exists
IF EXISTS (SELECT * FROM sys.tables WHERE name = 'GeneralJournal')
BEGIN
    PRINT '✓ GeneralJournal table exists'
    
    -- Show recent POS sales in GeneralJournal
    SELECT TOP 10
        TransactionDate,
        Reference,
        AccountName,
        Debit,
        Credit,
        Description
    FROM GeneralJournal
    WHERE Reference LIKE 'INV%' OR Description LIKE '%POS%'
    ORDER BY TransactionDate DESC;
END
ELSE
BEGIN
    PRINT '✗ GeneralJournal table does NOT exist - GL posting not configured!'
END
GO

-- Check for Sales and Cost of Sales accounts
SELECT 
    AccountID,
    AccountCode,
    AccountName,
    AccountType
FROM ChartOfAccounts
WHERE AccountName LIKE '%Sales%' OR AccountName LIKE '%Cost of Sales%'
ORDER BY AccountCode;
GO
