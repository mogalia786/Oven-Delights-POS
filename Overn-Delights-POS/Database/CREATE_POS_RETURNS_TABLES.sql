-- =============================================
-- Create POS Returns Tables for No-Receipt Returns
-- =============================================

-- 1. POS_Returns (Header Table)
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'POS_Returns')
BEGIN
    CREATE TABLE dbo.POS_Returns (
        ReturnID INT IDENTITY(1,1) PRIMARY KEY,
        ReturnNumber NVARCHAR(50) NOT NULL UNIQUE,
        BranchID INT NOT NULL,
        TillPointID INT NOT NULL,
        CashierID INT NOT NULL,
        CashierName NVARCHAR(100) NOT NULL,
        SupervisorID INT NOT NULL,
        SupervisorName NVARCHAR(100) NOT NULL,
        CustomerID INT NULL,
        ReturnDate DATETIME NOT NULL DEFAULT GETDATE(),
        TotalAmount DECIMAL(18,2) NOT NULL,
        PaymentMethod NVARCHAR(20) NOT NULL DEFAULT 'Cash',
        CashAmount DECIMAL(18,2) NOT NULL DEFAULT 0,
        CardAmount DECIMAL(18,2) NOT NULL DEFAULT 0,
        ReturnReason NVARCHAR(500) NOT NULL,
        ReturnToStock BIT NOT NULL DEFAULT 1,
        ReturnStatus NVARCHAR(20) NOT NULL DEFAULT 'Processed',
        CreatedDate DATETIME NOT NULL DEFAULT GETDATE(),
        CreatedBy NVARCHAR(100) NOT NULL,
        CONSTRAINT FK_POS_Returns_Branch FOREIGN KEY (BranchID) REFERENCES Branches(BranchID),
        CONSTRAINT FK_POS_Returns_TillPoint FOREIGN KEY (TillPointID) REFERENCES TillPoints(TillPointID),
        CONSTRAINT FK_POS_Returns_Cashier FOREIGN KEY (CashierID) REFERENCES Users(UserID),
        CONSTRAINT FK_POS_Returns_Supervisor FOREIGN KEY (SupervisorID) REFERENCES Users(UserID),
        CONSTRAINT FK_POS_Returns_Customer FOREIGN KEY (CustomerID) REFERENCES POS_Customers(CustomerID)
    );

    CREATE INDEX IX_POS_Returns_ReturnNumber ON dbo.POS_Returns(ReturnNumber);
    CREATE INDEX IX_POS_Returns_BranchID ON dbo.POS_Returns(BranchID);
    CREATE INDEX IX_POS_Returns_ReturnDate ON dbo.POS_Returns(ReturnDate);
    CREATE INDEX IX_POS_Returns_CustomerID ON dbo.POS_Returns(CustomerID);
    
    PRINT 'POS_Returns table created successfully';
END
ELSE
BEGIN
    PRINT 'POS_Returns table already exists';
END
GO

-- 2. POS_ReturnItems (Line Items Table)
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'POS_ReturnItems')
BEGIN
    CREATE TABLE dbo.POS_ReturnItems (
        ReturnItemID INT IDENTITY(1,1) PRIMARY KEY,
        ReturnID INT NOT NULL,
        ProductID INT NOT NULL,
        ProductCode NVARCHAR(50) NOT NULL,
        ProductName NVARCHAR(200) NOT NULL,
        Quantity DECIMAL(18,3) NOT NULL,
        UnitPrice DECIMAL(18,2) NOT NULL,
        LineTotal DECIMAL(18,2) NOT NULL,
        ReturnToStock BIT NOT NULL DEFAULT 1,
        CONSTRAINT FK_POS_ReturnItems_Return FOREIGN KEY (ReturnID) REFERENCES POS_Returns(ReturnID) ON DELETE CASCADE,
        CONSTRAINT FK_POS_ReturnItems_Product FOREIGN KEY (ProductID) REFERENCES Demo_Retail_Product(ProductID)
    );

    CREATE INDEX IX_POS_ReturnItems_ReturnID ON dbo.POS_ReturnItems(ReturnID);
    CREATE INDEX IX_POS_ReturnItems_ProductID ON dbo.POS_ReturnItems(ProductID);
    
    PRINT 'POS_ReturnItems table created successfully';
END
ELSE
BEGIN
    PRINT 'POS_ReturnItems table already exists';
END
GO

-- 3. POS_Transactions (if not exists - for sales ledger tracking)
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'POS_Transactions')
BEGIN
    CREATE TABLE dbo.POS_Transactions (
        TransactionID INT IDENTITY(1,1) PRIMARY KEY,
        TransactionType NVARCHAR(20) NOT NULL, -- 'Sale', 'Return', 'Void', etc.
        Amount DECIMAL(18,2) NOT NULL, -- Negative for returns
        BranchID INT NOT NULL,
        TillPointID INT NOT NULL,
        CashierID INT NOT NULL,
        ReferenceNumber NVARCHAR(50) NULL, -- Invoice/Return number
        TransactionDate DATETIME NOT NULL DEFAULT GETDATE(),
        CONSTRAINT FK_POS_Transactions_Branch FOREIGN KEY (BranchID) REFERENCES Branches(BranchID),
        CONSTRAINT FK_POS_Transactions_TillPoint FOREIGN KEY (TillPointID) REFERENCES TillPoints(TillPointID),
        CONSTRAINT FK_POS_Transactions_Cashier FOREIGN KEY (CashierID) REFERENCES Users(UserID)
    );

    CREATE INDEX IX_POS_Transactions_TransactionDate ON dbo.POS_Transactions(TransactionDate);
    CREATE INDEX IX_POS_Transactions_BranchID ON dbo.POS_Transactions(BranchID);
    CREATE INDEX IX_POS_Transactions_ReferenceNumber ON dbo.POS_Transactions(ReferenceNumber);
    
    PRINT 'POS_Transactions table created successfully';
END
ELSE
BEGIN
    PRINT 'POS_Transactions table already exists';
END
GO

PRINT '✓ All POS Returns tables created/verified successfully!';
GO
