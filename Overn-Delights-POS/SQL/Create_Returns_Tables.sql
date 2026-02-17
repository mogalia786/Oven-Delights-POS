-- =============================================
-- Create Returns Tables for POS System
-- =============================================

-- Demo_Returns Table (Header)
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Demo_Returns')
BEGIN
    CREATE TABLE Demo_Returns (
        ReturnID INT IDENTITY(1,1) PRIMARY KEY,
        ReturnNumber VARCHAR(50) NOT NULL UNIQUE,
        OriginalInvoiceNumber VARCHAR(50) NOT NULL,
        BranchID INT NOT NULL,
        TillPointID INT NOT NULL,
        CashierID INT NOT NULL,
        SupervisorID INT NOT NULL,
        CustomerName VARCHAR(200) NULL,
        CustomerPhone VARCHAR(50) NULL,
        CustomerAddress VARCHAR(500) NULL,
        ReturnReason VARCHAR(500) NULL,
        ReturnDate DATETIME NOT NULL DEFAULT GETDATE(),
        TotalReturnAmount DECIMAL(18,2) NOT NULL,
        TotalTaxAmount DECIMAL(18,2) NOT NULL DEFAULT 0,
        Status VARCHAR(20) NOT NULL DEFAULT 'Completed',
        CreatedDate DATETIME NOT NULL DEFAULT GETDATE(),
        CreatedBy INT NULL,
        CONSTRAINT FK_Returns_Branch FOREIGN KEY (BranchID) REFERENCES Branches(BranchID),
        CONSTRAINT FK_Returns_TillPoint FOREIGN KEY (TillPointID) REFERENCES TillPoints(TillPointID),
        CONSTRAINT FK_Returns_Cashier FOREIGN KEY (CashierID) REFERENCES Users(UserID),
        CONSTRAINT FK_Returns_Supervisor FOREIGN KEY (SupervisorID) REFERENCES Users(UserID)
    )
    
    PRINT 'Demo_Returns table created successfully'
END
ELSE
BEGIN
    PRINT 'Demo_Returns table already exists - checking for missing columns...'
    
    -- Add OriginalInvoiceNumber if missing
    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Demo_Returns') AND name = 'OriginalInvoiceNumber')
    BEGIN
        ALTER TABLE Demo_Returns ADD OriginalInvoiceNumber VARCHAR(50) NULL
        PRINT 'Added OriginalInvoiceNumber column to Demo_Returns'
    END
    
    -- Add ReturnReason if missing
    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Demo_Returns') AND name = 'ReturnReason')
    BEGIN
        ALTER TABLE Demo_Returns ADD ReturnReason VARCHAR(500) NULL
        PRINT 'Added ReturnReason column to Demo_Returns'
    END
    
    -- Add TotalTaxAmount if missing
    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Demo_Returns') AND name = 'TotalTaxAmount')
    BEGIN
        ALTER TABLE Demo_Returns ADD TotalTaxAmount DECIMAL(18,2) NULL
        PRINT 'Added TotalTaxAmount column to Demo_Returns'
    END
END
GO

-- Demo_ReturnDetails Table (Line Items)
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Demo_ReturnDetails')
BEGIN
    CREATE TABLE Demo_ReturnDetails (
        ReturnLineID INT IDENTITY(1,1) PRIMARY KEY,
        ReturnID INT NOT NULL,
        ProductID INT NOT NULL,
        ItemCode VARCHAR(50) NOT NULL,
        ProductName VARCHAR(200) NOT NULL,
        QtyReturned DECIMAL(18,2) NOT NULL,
        UnitPrice DECIMAL(18,2) NOT NULL,
        LineTotal DECIMAL(18,2) NOT NULL,
        TaxAmount DECIMAL(18,2) NOT NULL DEFAULT 0,
        CONSTRAINT FK_ReturnDetails_Return FOREIGN KEY (ReturnID) REFERENCES Demo_Returns(ReturnID) ON DELETE CASCADE,
        CONSTRAINT FK_ReturnLineItems_Product FOREIGN KEY (ProductID) REFERENCES Demo_Retail_Product(ProductID)
    )
    
    PRINT 'Demo_ReturnDetails table created successfully'
END
ELSE
BEGIN
    PRINT 'Demo_ReturnDetails table already exists'
END
GO

-- Create indexes for performance
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID('Demo_Returns') AND name = 'IX_Returns_ReturnNumber')
BEGIN
    CREATE INDEX IX_Returns_ReturnNumber ON Demo_Returns(ReturnNumber)
    PRINT 'Index IX_Returns_ReturnNumber created'
END
ELSE
BEGIN
    PRINT 'Index IX_Returns_ReturnNumber already exists'
END
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID('Demo_Returns') AND name = 'IX_Returns_OriginalInvoice')
BEGIN
    CREATE INDEX IX_Returns_OriginalInvoice ON Demo_Returns(OriginalInvoiceNumber)
    PRINT 'Index IX_Returns_OriginalInvoice created'
END
ELSE
BEGIN
    PRINT 'Index IX_Returns_OriginalInvoice already exists'
END
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID('Demo_Returns') AND name = 'IX_Returns_ReturnDate')
BEGIN
    CREATE INDEX IX_Returns_ReturnDate ON Demo_Returns(ReturnDate)
    PRINT 'Index IX_Returns_ReturnDate created'
END
ELSE
BEGIN
    PRINT 'Index IX_Returns_ReturnDate already exists'
END
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID('Demo_ReturnDetails') AND name = 'IX_ReturnDetails_ReturnID')
BEGIN
    CREATE INDEX IX_ReturnDetails_ReturnID ON Demo_ReturnDetails(ReturnID)
    PRINT 'Index IX_ReturnLineItems_ReturnID created'
END
ELSE
BEGIN
    PRINT 'Index IX_ReturnLineItems_ReturnID already exists'
END
GO

PRINT 'Returns tables setup completed successfully!'
GO
