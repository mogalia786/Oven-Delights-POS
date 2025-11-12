-- Create POS Transactions Tables
-- ================================

-- Main transactions table
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'POS_Transactions')
BEGIN
    CREATE TABLE POS_Transactions (
        TransactionID INT IDENTITY(1,1) PRIMARY KEY,
        TransactionNumber NVARCHAR(50) NOT NULL UNIQUE,
        BranchID INT NOT NULL,
        TillPointID INT NOT NULL,
        CashierID INT NOT NULL,
        CashierName NVARCHAR(100),
        TransactionDate DATETIME NOT NULL DEFAULT GETDATE(),
        TransactionType NVARCHAR(20) NOT NULL, -- 'Sale', 'Return', 'Void'
        PaymentMethod NVARCHAR(20), -- 'Cash', 'Card', 'Account', 'Split'
        SubtotalAmount DECIMAL(18,2) NOT NULL DEFAULT 0,
        TaxAmount DECIMAL(18,2) NOT NULL DEFAULT 0,
        TotalAmount DECIMAL(18,2) NOT NULL DEFAULT 0,
        CashAmount DECIMAL(18,2),
        CardAmount DECIMAL(18,2),
        ChangeGiven DECIMAL(18,2),
        GLPosted BIT NOT NULL DEFAULT 0,
        GLPostDate DATETIME,
        IsVoid BIT NOT NULL DEFAULT 0,
        VoidReason NVARCHAR(500),
        CreatedDate DATETIME NOT NULL DEFAULT GETDATE(),
        
        CONSTRAINT FK_POSTrans_Branch FOREIGN KEY (BranchID) REFERENCES Branches(BranchID)
    );
    
    CREATE INDEX IX_POSTrans_Date ON POS_Transactions(TransactionDate);
    CREATE INDEX IX_POSTrans_Branch ON POS_Transactions(BranchID, TransactionDate);
    CREATE INDEX IX_POSTrans_GLPosted ON POS_Transactions(GLPosted);
    
    PRINT '✓ POS_Transactions table created';
END
ELSE
BEGIN
    PRINT '! POS_Transactions table already exists';
    
    -- Add GLPosted column if it doesn't exist
    IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'POS_Transactions' AND COLUMN_NAME = 'GLPosted')
    BEGIN
        ALTER TABLE POS_Transactions ADD GLPosted BIT NOT NULL DEFAULT 0;
        ALTER TABLE POS_Transactions ADD GLPostDate DATETIME;
        PRINT '✓ Added GLPosted columns to POS_Transactions';
    END
END
GO

-- Transaction line items table
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'POS_TransactionItems')
BEGIN
    CREATE TABLE POS_TransactionItems (
        ItemID INT IDENTITY(1,1) PRIMARY KEY,
        TransactionID INT NOT NULL,
        ProductID INT,
        VariantID INT,
        ItemCode NVARCHAR(50),
        ProductName NVARCHAR(200),
        Quantity DECIMAL(18,3) NOT NULL,
        UnitPrice DECIMAL(18,2) NOT NULL,
        LineTotal DECIMAL(18,2) NOT NULL,
        CostPrice DECIMAL(18,2), -- For Cost of Sales calculation
        
        CONSTRAINT FK_POSTransItems_Trans FOREIGN KEY (TransactionID) 
            REFERENCES POS_Transactions(TransactionID) ON DELETE CASCADE
    );
    
    CREATE INDEX IX_POSTransItems_Trans ON POS_TransactionItems(TransactionID);
    CREATE INDEX IX_POSTransItems_Product ON POS_TransactionItems(ProductID);
    
    PRINT '✓ POS_TransactionItems table created';
END
ELSE
BEGIN
    PRINT '! POS_TransactionItems table already exists';
    
    -- Add CostPrice column if it doesn't exist
    IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'POS_TransactionItems' AND COLUMN_NAME = 'CostPrice')
    BEGIN
        ALTER TABLE POS_TransactionItems ADD CostPrice DECIMAL(18,2);
        PRINT '✓ Added CostPrice column to POS_TransactionItems';
    END
END
GO

PRINT 'POS transaction tables ready for GL integration!';
GO
