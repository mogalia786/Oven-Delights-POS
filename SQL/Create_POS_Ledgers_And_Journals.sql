-- Create required Ledgers for POS transactions
-- These ledgers are essential for proper double-entry bookkeeping

-- Check if Ledgers table exists
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Ledgers')
BEGIN
    CREATE TABLE Ledgers (
        LedgerID INT IDENTITY(1,1) PRIMARY KEY,
        LedgerName NVARCHAR(100) NOT NULL UNIQUE,
        LedgerType NVARCHAR(50) NOT NULL, -- Asset, Liability, Equity, Revenue, Expense
        ParentLedgerID INT NULL,
        IsActive BIT NOT NULL DEFAULT 1,
        CreatedDate DATETIME NOT NULL DEFAULT GETDATE(),
        CreatedBy INT NULL,
        LastModifiedDate DATETIME NULL,
        LastModifiedBy INT NULL,
        CONSTRAINT FK_Ledgers_Parent FOREIGN KEY (ParentLedgerID) REFERENCES Ledgers(LedgerID)
    )
    PRINT 'Ledgers table created'
END
GO

-- Check if Journals table exists
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Journals')
BEGIN
    CREATE TABLE Journals (
        JournalID INT IDENTITY(1,1) PRIMARY KEY,
        JournalDate DATETIME NOT NULL,
        JournalType NVARCHAR(50) NOT NULL, -- Sales Journal, Purchase Journal, Cash Journal, etc.
        Reference NVARCHAR(100) NOT NULL, -- Invoice number, receipt number, etc.
        LedgerID INT NOT NULL,
        Debit DECIMAL(18,2) NOT NULL DEFAULT 0,
        Credit DECIMAL(18,2) NOT NULL DEFAULT 0,
        Description NVARCHAR(500) NULL,
        BranchID INT NULL,
        CreatedDate DATETIME NOT NULL DEFAULT GETDATE(),
        CreatedBy INT NULL,
        CONSTRAINT FK_Journals_Ledger FOREIGN KEY (LedgerID) REFERENCES Ledgers(LedgerID),
        CONSTRAINT FK_Journals_Branch FOREIGN KEY (BranchID) REFERENCES Branches(BranchID)
    )
    PRINT 'Journals table created'
END
GO

-- Insert essential ledgers if they don't exist

-- ASSETS
IF NOT EXISTS (SELECT * FROM Ledgers WHERE LedgerName = 'Cash')
    INSERT INTO Ledgers (LedgerName, LedgerType, IsActive) VALUES ('Cash', 'Asset', 1)

IF NOT EXISTS (SELECT * FROM Ledgers WHERE LedgerName = 'Bank')
    INSERT INTO Ledgers (LedgerName, LedgerType, IsActive) VALUES ('Bank', 'Asset', 1)

IF NOT EXISTS (SELECT * FROM Ledgers WHERE LedgerName = 'Inventory')
    INSERT INTO Ledgers (LedgerName, LedgerType, IsActive) VALUES ('Inventory', 'Asset', 1)

IF NOT EXISTS (SELECT * FROM Ledgers WHERE LedgerName = 'Accounts Receivable')
    INSERT INTO Ledgers (LedgerName, LedgerType, IsActive) VALUES ('Accounts Receivable', 'Asset', 1)

-- LIABILITIES
IF NOT EXISTS (SELECT * FROM Ledgers WHERE LedgerName = 'VAT Output')
    INSERT INTO Ledgers (LedgerName, LedgerType, IsActive) VALUES ('VAT Output', 'Liability', 1)

IF NOT EXISTS (SELECT * FROM Ledgers WHERE LedgerName = 'VAT Input')
    INSERT INTO Ledgers (LedgerName, LedgerType, IsActive) VALUES ('VAT Input', 'Liability', 1)

IF NOT EXISTS (SELECT * FROM Ledgers WHERE LedgerName = 'Accounts Payable')
    INSERT INTO Ledgers (LedgerName, LedgerType, IsActive) VALUES ('Accounts Payable', 'Liability', 1)

-- REVENUE
IF NOT EXISTS (SELECT * FROM Ledgers WHERE LedgerName = 'Sales Revenue')
    INSERT INTO Ledgers (LedgerName, LedgerType, IsActive) VALUES ('Sales Revenue', 'Revenue', 1)

IF NOT EXISTS (SELECT * FROM Ledgers WHERE LedgerName = 'Sales Returns')
    INSERT INTO Ledgers (LedgerName, LedgerType, IsActive) VALUES ('Sales Returns', 'Revenue', 1)

IF NOT EXISTS (SELECT * FROM Ledgers WHERE LedgerName = 'Sales Discounts')
    INSERT INTO Ledgers (LedgerName, LedgerType, IsActive) VALUES ('Sales Discounts', 'Revenue', 1)

-- EXPENSES
IF NOT EXISTS (SELECT * FROM Ledgers WHERE LedgerName = 'Cost of Sales')
    INSERT INTO Ledgers (LedgerName, LedgerType, IsActive) VALUES ('Cost of Sales', 'Expense', 1)

IF NOT EXISTS (SELECT * FROM Ledgers WHERE LedgerName = 'Cost of Goods Sold')
    INSERT INTO Ledgers (LedgerName, LedgerType, IsActive) VALUES ('Cost of Goods Sold', 'Expense', 1)

GO

-- Create indexes for performance
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Journals_JournalDate')
    CREATE INDEX IX_Journals_JournalDate ON Journals(JournalDate)

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Journals_Reference')
    CREATE INDEX IX_Journals_Reference ON Journals(Reference)

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Journals_LedgerID')
    CREATE INDEX IX_Journals_LedgerID ON Journals(LedgerID)

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Journals_BranchID')
    CREATE INDEX IX_Journals_BranchID ON Journals(BranchID)

GO

PRINT 'POS Ledgers and Journals setup complete!'
PRINT ''
PRINT 'Ledgers created:'
SELECT LedgerID, LedgerName, LedgerType FROM Ledgers ORDER BY LedgerType, LedgerName
