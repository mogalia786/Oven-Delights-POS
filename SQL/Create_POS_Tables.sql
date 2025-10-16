-- =============================================
-- Create POS Tables for Point of Sale System
-- =============================================

-- Demo_Sales Table (Sales Header)
-- Check if table exists and add missing columns if needed
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Demo_Sales')
BEGIN
    CREATE TABLE Demo_Sales (
        SaleID INT IDENTITY(1,1) PRIMARY KEY,
        InvoiceNumber VARCHAR(50) NOT NULL UNIQUE,
        SaleDate DATETIME NOT NULL DEFAULT GETDATE(),
        CashierID INT NOT NULL,
        BranchID INT NOT NULL,
        TillPointID INT NOT NULL,
        Subtotal DECIMAL(18,2) NOT NULL,
        TaxAmount DECIMAL(18,2) NOT NULL,
        TotalAmount DECIMAL(18,2) NOT NULL,
        PaymentMethod VARCHAR(20) NOT NULL,
        CashAmount DECIMAL(18,2) NULL,
        CardAmount DECIMAL(18,2) NULL,
        ChangeAmount DECIMAL(18,2) NULL,
        Status VARCHAR(20) NOT NULL DEFAULT 'Completed',
        CreatedDate DATETIME NOT NULL DEFAULT GETDATE(),
        CONSTRAINT FK_Demo_Sales_Cashier FOREIGN KEY (CashierID) REFERENCES Users(UserID),
        CONSTRAINT FK_Demo_Sales_Branch FOREIGN KEY (BranchID) REFERENCES Branches(BranchID),
        CONSTRAINT FK_Demo_Sales_TillPoint FOREIGN KEY (TillPointID) REFERENCES TillPoints(TillPointID)
    )
    
    PRINT 'Demo_Sales table created successfully'
END
ELSE
BEGIN
    PRINT 'Demo_Sales table already exists - checking for missing columns...'
    
    -- Add InvoiceNumber if missing
    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Demo_Sales') AND name = 'InvoiceNumber')
    BEGIN
        ALTER TABLE Demo_Sales ADD InvoiceNumber VARCHAR(50) NULL
        PRINT 'Added InvoiceNumber column to Demo_Sales'
    END
    
    -- Add TillPointID if missing
    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Demo_Sales') AND name = 'TillPointID')
    BEGIN
        ALTER TABLE Demo_Sales ADD TillPointID INT NULL
        PRINT 'Added TillPointID column to Demo_Sales'
    END
    
    -- Add PaymentMethod if missing
    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Demo_Sales') AND name = 'PaymentMethod')
    BEGIN
        ALTER TABLE Demo_Sales ADD PaymentMethod VARCHAR(20) NULL
        PRINT 'Added PaymentMethod column to Demo_Sales'
    END
    
    -- Add CashAmount if missing
    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Demo_Sales') AND name = 'CashAmount')
    BEGIN
        ALTER TABLE Demo_Sales ADD CashAmount DECIMAL(18,2) NULL
        PRINT 'Added CashAmount column to Demo_Sales'
    END
    
    -- Add CardAmount if missing
    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Demo_Sales') AND name = 'CardAmount')
    BEGIN
        ALTER TABLE Demo_Sales ADD CardAmount DECIMAL(18,2) NULL
        PRINT 'Added CardAmount column to Demo_Sales'
    END
END
GO

-- POS_InvoiceLines Table (Invoice Line Items)
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'POS_InvoiceLines')
BEGIN
    CREATE TABLE POS_InvoiceLines (
        LineID INT IDENTITY(1,1) PRIMARY KEY,
        InvoiceNumber VARCHAR(50) NOT NULL,
        SalesID INT NOT NULL,
        BranchID INT NOT NULL,
        ProductID INT NOT NULL,
        ItemCode VARCHAR(50) NOT NULL,
        ProductName VARCHAR(200) NOT NULL,
        Quantity DECIMAL(18,2) NOT NULL,
        UnitPrice DECIMAL(18,2) NOT NULL,
        LineTotal DECIMAL(18,2) NOT NULL,
        TaxAmount DECIMAL(18,2) NULL,
        SaleDate DATETIME NOT NULL DEFAULT GETDATE(),
        CashierID INT NOT NULL,
        CreatedDate DATETIME NOT NULL DEFAULT GETDATE(),
        CONSTRAINT FK_POS_InvoiceLines_Sales FOREIGN KEY (SalesID) REFERENCES Demo_Sales(SaleID) ON DELETE CASCADE,
        CONSTRAINT FK_POS_InvoiceLines_Branch FOREIGN KEY (BranchID) REFERENCES Branches(BranchID),
        CONSTRAINT FK_POS_InvoiceLines_Product FOREIGN KEY (ProductID) REFERENCES Demo_Retail_Product(ProductID),
        CONSTRAINT FK_POS_InvoiceLines_Cashier FOREIGN KEY (CashierID) REFERENCES Users(UserID)
    )
    
    PRINT 'POS_InvoiceLines table created successfully'
END
ELSE
BEGIN
    PRINT 'POS_InvoiceLines table already exists'
END
GO

-- Create indexes for performance
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID('Demo_Sales') AND name = 'IX_Demo_Sales_InvoiceNumber')
BEGIN
    CREATE INDEX IX_Demo_Sales_InvoiceNumber ON Demo_Sales(InvoiceNumber)
    PRINT 'Index IX_Demo_Sales_InvoiceNumber created'
END
ELSE
BEGIN
    PRINT 'Index IX_Demo_Sales_InvoiceNumber already exists'
END
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID('Demo_Sales') AND name = 'IX_Demo_Sales_SaleDate')
BEGIN
    CREATE INDEX IX_Demo_Sales_SaleDate ON Demo_Sales(SaleDate)
    PRINT 'Index IX_Demo_Sales_SaleDate created'
END
ELSE
BEGIN
    PRINT 'Index IX_Demo_Sales_SaleDate already exists'
END
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID('Demo_Sales') AND name = 'IX_Demo_Sales_TillPoint')
BEGIN
    CREATE INDEX IX_Demo_Sales_TillPoint ON Demo_Sales(TillPointID, SaleDate)
    PRINT 'Index IX_Demo_Sales_TillPoint created'
END
ELSE
BEGIN
    PRINT 'Index IX_Demo_Sales_TillPoint already exists'
END
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID('POS_InvoiceLines') AND name = 'IX_POS_InvoiceLines_InvoiceNumber')
BEGIN
    CREATE INDEX IX_POS_InvoiceLines_InvoiceNumber ON POS_InvoiceLines(InvoiceNumber)
    PRINT 'Index IX_POS_InvoiceLines_InvoiceNumber created'
END
ELSE
BEGIN
    PRINT 'Index IX_POS_InvoiceLines_InvoiceNumber already exists'
END
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID('POS_InvoiceLines') AND name = 'IX_POS_InvoiceLines_SalesID')
BEGIN
    CREATE INDEX IX_POS_InvoiceLines_SalesID ON POS_InvoiceLines(SalesID)
    PRINT 'Index IX_POS_InvoiceLines_SalesID created'
END
ELSE
BEGIN
    PRINT 'Index IX_POS_InvoiceLines_SalesID already exists'
END
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID('POS_InvoiceLines') AND name = 'IX_POS_InvoiceLines_Product')
BEGIN
    CREATE INDEX IX_POS_InvoiceLines_Product ON POS_InvoiceLines(ProductID, SaleDate)
    PRINT 'Index IX_POS_InvoiceLines_Product created'
END
ELSE
BEGIN
    PRINT 'Index IX_POS_InvoiceLines_Product already exists'
END
GO

PRINT 'POS tables setup completed successfully!'
GO
