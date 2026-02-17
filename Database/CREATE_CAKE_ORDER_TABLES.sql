-- Create tables for cake order system
-- POS_CustomOrders: Main order header
-- POS_CustomOrderItems: Individual items in the order

-- Main order table
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'POS_CustomOrders')
BEGIN
    CREATE TABLE POS_CustomOrders (
        OrderID INT IDENTITY(1,1) PRIMARY KEY,
        OrderNumber NVARCHAR(50) NOT NULL UNIQUE,
        BranchID INT NOT NULL,
        BranchName NVARCHAR(100),
        OrderType NVARCHAR(50) NOT NULL, -- 'Cake', 'Catering', etc.
        
        -- Customer Details
        CustomerName NVARCHAR(100) NOT NULL,
        CustomerSurname NVARCHAR(100),
        CustomerPhone NVARCHAR(20) NOT NULL,
        CustomerEmail NVARCHAR(100),
        CustomerAddress NVARCHAR(500),
        
        -- Order Details
        OrderDate DATETIME NOT NULL DEFAULT GETDATE(),
        ReadyDate DATE NOT NULL,
        ReadyTime TIME NOT NULL,
        
        -- Financials
        TotalAmount DECIMAL(18,2) NOT NULL DEFAULT 0,
        DepositPaid DECIMAL(18,2) NOT NULL DEFAULT 0,
        BalanceDue DECIMAL(18,2) NOT NULL DEFAULT 0,
        
        -- Status
        OrderStatus NVARCHAR(50) NOT NULL DEFAULT 'New', -- New, InProgress, Ready, Completed, Cancelled
        
        -- Additional Info
        SpecialInstructions NVARCHAR(MAX), -- Special requests from dropdown/free text
        ManufacturingInstructions NVARCHAR(MAX), -- Internal notes
        CreatedBy NVARCHAR(100),
        CreatedDate DATETIME DEFAULT GETDATE(),
        ModifiedDate DATETIME DEFAULT GETDATE(),
        
        -- POS Integration
        TillPointID INT,
        CashierID INT
    );
    
    PRINT 'POS_CustomOrders table created';
END
ELSE
BEGIN
    PRINT 'POS_CustomOrders table already exists';
    
    -- Add SpecialInstructions column if missing
    IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS 
                   WHERE TABLE_NAME = 'POS_CustomOrders' AND COLUMN_NAME = 'SpecialInstructions')
    BEGIN
        ALTER TABLE POS_CustomOrders ADD SpecialInstructions NVARCHAR(MAX);
        PRINT 'Added SpecialInstructions column';
    END
END
GO

-- Order items table
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'POS_CustomOrderItems')
BEGIN
    CREATE TABLE POS_CustomOrderItems (
        ItemID INT IDENTITY(1,1) PRIMARY KEY,
        OrderID INT NOT NULL,
        
        -- Product Details
        ProductID INT NOT NULL, -- Links to Demo_Retail_Product
        ProductName NVARCHAR(200) NOT NULL,
        ProductImage NVARCHAR(500), -- Path to product image
        
        -- Pricing
        Quantity INT NOT NULL DEFAULT 1,
        UnitPrice DECIMAL(18,2) NOT NULL,
        LineTotal DECIMAL(18,2) NOT NULL,
        
        -- Additional Info
        ItemNotes NVARCHAR(500), -- Item-specific notes
        
        CONSTRAINT FK_CustomOrderItems_Order FOREIGN KEY (OrderID) 
            REFERENCES POS_CustomOrders(OrderID) ON DELETE CASCADE
    );
    
    PRINT 'POS_CustomOrderItems table created';
END
ELSE
BEGIN
    PRINT 'POS_CustomOrderItems table already exists';
    
    -- Add ProductImage column if missing
    IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS 
                   WHERE TABLE_NAME = 'POS_CustomOrderItems' AND COLUMN_NAME = 'ProductImage')
    BEGIN
        ALTER TABLE POS_CustomOrderItems ADD ProductImage NVARCHAR(500);
        PRINT 'Added ProductImage column';
    END
END
GO

-- Create indexes for performance
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_CustomOrders_OrderNumber')
BEGIN
    CREATE INDEX IX_CustomOrders_OrderNumber ON POS_CustomOrders(OrderNumber);
END

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_CustomOrders_BranchID')
BEGIN
    CREATE INDEX IX_CustomOrders_BranchID ON POS_CustomOrders(BranchID);
END

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_CustomOrders_Status')
BEGIN
    CREATE INDEX IX_CustomOrders_Status ON POS_CustomOrders(OrderStatus);
END

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_CustomOrderItems_OrderID')
BEGIN
    CREATE INDEX IX_CustomOrderItems_OrderID ON POS_CustomOrderItems(OrderID);
END

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_CustomOrderItems_ProductID')
BEGIN
    CREATE INDEX IX_CustomOrderItems_ProductID ON POS_CustomOrderItems(ProductID);
END
GO

PRINT 'Cake order tables ready!';
GO
