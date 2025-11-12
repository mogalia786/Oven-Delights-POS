-- Create POS General Ledger Account Mapping
-- ===========================================
-- Maps POS transaction types to GL accounts for automatic posting

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'POS_GLAccountMapping')
BEGIN
    CREATE TABLE POS_GLAccountMapping (
        MappingID INT IDENTITY(1,1) PRIMARY KEY,
        BranchID INT NOT NULL,
        AccountType NVARCHAR(50) NOT NULL, -- 'Sales', 'CostOfSales', 'Cash', 'Card', 'Inventory', 'Debtors'
        AccountCode NVARCHAR(20) NOT NULL,
        AccountName NVARCHAR(200) NOT NULL,
        IsActive BIT NOT NULL DEFAULT 1,
        CreatedDate DATETIME NOT NULL DEFAULT GETDATE(),
        CreatedBy NVARCHAR(100),
        ModifiedDate DATETIME,
        ModifiedBy NVARCHAR(100),
        
        -- Unique constraint per branch and account type
        CONSTRAINT UQ_POS_GLMapping UNIQUE (BranchID, AccountType),
        
        -- Foreign key to Branches
        CONSTRAINT FK_POSGLMapping_Branch FOREIGN KEY (BranchID) 
            REFERENCES Branches(BranchID)
    );
    
    PRINT '✓ POS_GLAccountMapping table created successfully';
    
    -- Insert default GL account mappings for all active branches
    INSERT INTO POS_GLAccountMapping (BranchID, AccountType, AccountCode, AccountName, CreatedBy)
    SELECT 
        b.BranchID,
        'Sales' AS AccountType,
        '4000' AS AccountCode,
        'Sales Revenue - Retail' AS AccountName,
        'SYSTEM' AS CreatedBy
    FROM Branches b
    WHERE b.IsActive = 1;
    
    INSERT INTO POS_GLAccountMapping (BranchID, AccountType, AccountCode, AccountName, CreatedBy)
    SELECT 
        b.BranchID,
        'CostOfSales' AS AccountType,
        '5000' AS AccountCode,
        'Cost of Sales - Retail' AS AccountName,
        'SYSTEM' AS CreatedBy
    FROM Branches b
    WHERE b.IsActive = 1;
    
    INSERT INTO POS_GLAccountMapping (BranchID, AccountType, AccountCode, AccountName, CreatedBy)
    SELECT 
        b.BranchID,
        'Cash' AS AccountType,
        '1100' AS AccountCode,
        'Cash on Hand - Till' AS AccountName,
        'SYSTEM' AS CreatedBy
    FROM Branches b
    WHERE b.IsActive = 1;
    
    INSERT INTO POS_GLAccountMapping (BranchID, AccountType, AccountCode, AccountName, CreatedBy)
    SELECT 
        b.BranchID,
        'Card' AS AccountType,
        '1110' AS AccountCode,
        'Card Payments - Pending' AS AccountName,
        'SYSTEM' AS CreatedBy
    FROM Branches b
    WHERE b.IsActive = 1;
    
    INSERT INTO POS_GLAccountMapping (BranchID, AccountType, AccountCode, AccountName, CreatedBy)
    SELECT 
        b.BranchID,
        'Inventory' AS AccountType,
        '1300' AS AccountCode,
        'Inventory - Retail Products' AS AccountName,
        'SYSTEM' AS CreatedBy
    FROM Branches b
    WHERE b.IsActive = 1;
    
    INSERT INTO POS_GLAccountMapping (BranchID, AccountType, AccountCode, AccountName, CreatedBy)
    SELECT 
        b.BranchID,
        'Debtors' AS AccountType,
        '1200' AS AccountCode,
        'Accounts Receivable - Customers' AS AccountName,
        'SYSTEM' AS CreatedBy
    FROM Branches b
    WHERE b.IsActive = 1;
    
    PRINT '✓ Default GL account mappings inserted for all branches';
END
ELSE
BEGIN
    PRINT '! POS_GLAccountMapping table already exists';
END
GO

-- View current mappings
SELECT 
    m.MappingID,
    b.BranchName,
    m.AccountType,
    m.AccountCode,
    m.AccountName,
    m.IsActive
FROM POS_GLAccountMapping m
INNER JOIN Branches b ON m.BranchID = b.BranchID
ORDER BY b.BranchName, m.AccountType;
GO
