-- Create Ledgers Table
-- =====================
-- This table stores the chart of accounts/ledgers

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Ledgers')
BEGIN
    CREATE TABLE Ledgers (
        LedgerID INT IDENTITY(1,1) PRIMARY KEY,
        LedgerName NVARCHAR(200) NOT NULL,
        LedgerType NVARCHAR(50), -- 'Asset', 'Liability', 'Revenue', 'Expense', 'Equity'
        AccountCode NVARCHAR(20),
        ParentLedgerID INT NULL, -- For sub-accounts
        IsActive BIT NOT NULL DEFAULT 1,
        CreatedDate DATETIME NOT NULL DEFAULT GETDATE(),
        CreatedBy NVARCHAR(100),
        ModifiedDate DATETIME,
        ModifiedBy NVARCHAR(100),
        
        CONSTRAINT FK_Ledgers_Parent FOREIGN KEY (ParentLedgerID) 
            REFERENCES Ledgers(LedgerID)
    );
    
    CREATE INDEX IX_Ledgers_Name ON Ledgers(LedgerName);
    CREATE INDEX IX_Ledgers_Type ON Ledgers(LedgerType);
    
    PRINT '✓ Ledgers table created successfully';
    
    -- Insert default ledgers for POS
    INSERT INTO Ledgers (LedgerName, LedgerType, AccountCode, IsActive, CreatedBy)
    VALUES 
    ('Cash', 'Asset', '1100', 1, 'SYSTEM'),
    ('Sales Revenue', 'Revenue', '4000', 1, 'SYSTEM'),
    ('VAT Output', 'Liability', '2100', 1, 'SYSTEM'),
    ('Cost of Sales', 'Expense', '5000', 1, 'SYSTEM'),
    ('Inventory', 'Asset', '1300', 1, 'SYSTEM');
    
    PRINT '✓ Default ledgers inserted';
END
ELSE
BEGIN
    PRINT '! Ledgers table already exists';
END
GO

-- Show current ledgers
SELECT 
    LedgerID,
    LedgerName,
    LedgerType,
    AccountCode,
    IsActive
FROM Ledgers
ORDER BY LedgerType, LedgerName;
GO

PRINT '';
PRINT 'Ledgers table is ready!';
GO
