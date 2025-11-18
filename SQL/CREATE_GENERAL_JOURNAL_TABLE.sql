-- Create GeneralJournal Table
-- ============================
-- This table stores all journal entries for GL posting

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'GeneralJournal')
BEGIN
    CREATE TABLE GeneralJournal (
        JournalID INT IDENTITY(1,1) PRIMARY KEY,
        TransactionDate DATETIME NOT NULL DEFAULT GETDATE(),
        JournalType NVARCHAR(50), -- 'Sales Journal', 'Purchase Journal', etc.
        Reference NVARCHAR(100), -- Invoice number, receipt number, etc.
        LedgerID INT, -- FK to Ledgers table (if you have one)
        AccountCode NVARCHAR(20),
        AccountName NVARCHAR(200),
        Debit DECIMAL(18,2) DEFAULT 0,
        Credit DECIMAL(18,2) DEFAULT 0,
        Description NVARCHAR(500),
        BranchID INT,
        CreatedBy NVARCHAR(100),
        CreatedDate DATETIME NOT NULL DEFAULT GETDATE(),
        
        -- Optional: Add FK to Branches if table exists
        CONSTRAINT FK_GeneralJournal_Branch FOREIGN KEY (BranchID) 
            REFERENCES Branches(BranchID)
    );
    
    -- Create indexes for better performance
    CREATE INDEX IX_GeneralJournal_Date ON GeneralJournal(TransactionDate);
    CREATE INDEX IX_GeneralJournal_Reference ON GeneralJournal(Reference);
    CREATE INDEX IX_GeneralJournal_AccountCode ON GeneralJournal(AccountCode);
    CREATE INDEX IX_GeneralJournal_Branch ON GeneralJournal(BranchID);
    
    PRINT '✓ GeneralJournal table created successfully';
END
ELSE
BEGIN
    PRINT '! GeneralJournal table already exists';
END
GO

-- Verify table structure
SELECT 
    COLUMN_NAME,
    DATA_TYPE,
    CHARACTER_MAXIMUM_LENGTH,
    IS_NULLABLE
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME = 'GeneralJournal'
ORDER BY ORDINAL_POSITION;
GO

PRINT '';
PRINT 'GeneralJournal table is ready for GL posting!';
GO
