-- First, check existing Ledgers table structure
IF EXISTS (SELECT * FROM sys.tables WHERE name = 'Ledgers')
BEGIN
    PRINT 'Ledgers table exists. Checking structure...'
    
    -- Check if LedgerID column exists
    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Ledgers') AND name = 'LedgerID')
    BEGIN
        PRINT 'LedgerID column missing. Checking for alternative primary key...'
        
        -- Check what the actual primary key column is
        SELECT c.name AS ColumnName, t.name AS DataType
        FROM sys.columns c
        INNER JOIN sys.types t ON c.user_type_id = t.user_type_id
        WHERE c.object_id = OBJECT_ID('Ledgers')
        ORDER BY c.column_id
    END
END
GO

-- Check Journals table structure
IF EXISTS (SELECT * FROM sys.tables WHERE name = 'Journals')
BEGIN
    PRINT 'Journals table exists. Checking structure...'
    
    SELECT c.name AS ColumnName, t.name AS DataType
    FROM sys.columns c
    INNER JOIN sys.types t ON c.user_type_id = t.user_type_id
    WHERE c.object_id = OBJECT_ID('Journals')
    ORDER BY c.column_id
END
ELSE
BEGIN
    PRINT 'Journals table does NOT exist'
END
GO

-- Insert essential ledgers using the correct column names
-- Assuming the table uses different column names, let's try common alternatives

-- Check if we can insert with just LedgerName
IF EXISTS (SELECT * FROM sys.tables WHERE name = 'Ledgers')
BEGIN
    -- Try to insert basic ledgers
    IF NOT EXISTS (SELECT * FROM Ledgers WHERE LedgerName = 'Cash')
        INSERT INTO Ledgers (LedgerName, LedgerType, IsActive) VALUES ('Cash', 'Asset', 1)
    
    IF NOT EXISTS (SELECT * FROM Ledgers WHERE LedgerName = 'Bank')
        INSERT INTO Ledgers (LedgerName, LedgerType, IsActive) VALUES ('Bank', 'Asset', 1)
    
    IF NOT EXISTS (SELECT * FROM Ledgers WHERE LedgerName = 'Inventory')
        INSERT INTO Ledgers (LedgerName, LedgerType, IsActive) VALUES ('Inventory', 'Asset', 1)
    
    IF NOT EXISTS (SELECT * FROM Ledgers WHERE LedgerName = 'Sales Revenue')
        INSERT INTO Ledgers (LedgerName, LedgerType, IsActive) VALUES ('Sales Revenue', 'Revenue', 1)
    
    IF NOT EXISTS (SELECT * FROM Ledgers WHERE LedgerName = 'VAT Output')
        INSERT INTO Ledgers (LedgerName, LedgerType, IsActive) VALUES ('VAT Output', 'Liability', 1)
    
    IF NOT EXISTS (SELECT * FROM Ledgers WHERE LedgerName = 'Cost of Sales')
        INSERT INTO Ledgers (LedgerName, LedgerType, IsActive) VALUES ('Cost of Sales', 'Expense', 1)
    
    PRINT 'Essential ledgers inserted'
    
    -- Show what we have
    SELECT * FROM Ledgers
END
GO

-- Create Journals table if it doesn't exist
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Journals')
BEGIN
    -- Get the primary key column name from Ledgers table
    DECLARE @pkColumn NVARCHAR(128)
    SELECT @pkColumn = c.name
    FROM sys.columns c
    INNER JOIN sys.indexes i ON i.object_id = c.object_id
    INNER JOIN sys.index_columns ic ON ic.object_id = i.object_id AND ic.index_id = i.index_id AND ic.column_id = c.column_id
    WHERE c.object_id = OBJECT_ID('Ledgers') AND i.is_primary_key = 1
    
    PRINT 'Creating Journals table with FK to Ledgers.' + @pkColumn
    
    -- Create with dynamic SQL based on actual column name
    DECLARE @sql NVARCHAR(MAX) = '
    CREATE TABLE Journals (
        JournalID INT IDENTITY(1,1) PRIMARY KEY,
        JournalDate DATETIME NOT NULL,
        JournalType NVARCHAR(50) NOT NULL,
        Reference NVARCHAR(100) NOT NULL,
        ' + @pkColumn + ' INT NOT NULL,
        Debit DECIMAL(18,2) NOT NULL DEFAULT 0,
        Credit DECIMAL(18,2) NOT NULL DEFAULT 0,
        Description NVARCHAR(500) NULL,
        BranchID INT NULL,
        CreatedDate DATETIME NOT NULL DEFAULT GETDATE(),
        CreatedBy INT NULL,
        CONSTRAINT FK_Journals_Ledger FOREIGN KEY (' + @pkColumn + ') REFERENCES Ledgers(' + @pkColumn + ')
    )'
    
    EXEC sp_executesql @sql
    PRINT 'Journals table created'
END
GO

PRINT 'Setup complete!'
