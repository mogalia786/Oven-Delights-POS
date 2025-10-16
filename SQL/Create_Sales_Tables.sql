-- Create Sales and Invoices tables for POS transactions

-- TillPoints table MUST be created first
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'TillPoints')
BEGIN
    CREATE TABLE TillPoints (
        TillPointID INT IDENTITY(1,1) PRIMARY KEY,
        TillNumber NVARCHAR(50) NOT NULL UNIQUE,
        BranchID INT NOT NULL,
        MachineName NVARCHAR(100) NOT NULL,
        IsActive BIT NOT NULL DEFAULT 1,
        CreatedDate DATETIME NOT NULL DEFAULT GETDATE(),
        CreatedBy INT NULL,
        LastModifiedDate DATETIME NULL,
        LastModifiedBy INT NULL,
        CONSTRAINT FK_TillPoints_Branch FOREIGN KEY (BranchID) REFERENCES Branches(BranchID)
    )
    
    PRINT 'TillPoints table created'
END
GO

-- Sales table (Invoice Header)
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Sales')
BEGIN
    CREATE TABLE Sales (
        SalesID INT IDENTITY(1,1) PRIMARY KEY,
        InvoiceNumber NVARCHAR(50) NOT NULL UNIQUE,
        BranchID INT NOT NULL,
        CashierID INT NOT NULL,
        TillPointID INT NULL,
        SaleDate DATETIME NOT NULL,
        Subtotal DECIMAL(18,2) NOT NULL,
        TaxAmount DECIMAL(18,2) NOT NULL,
        TotalAmount DECIMAL(18,2) NOT NULL,
        PaymentMethod NVARCHAR(20) NOT NULL, -- CASH, CARD, SPLIT
        CashAmount DECIMAL(18,2) DEFAULT 0,
        CardAmount DECIMAL(18,2) DEFAULT 0,
        CreatedDate DATETIME DEFAULT GETDATE(),
        CONSTRAINT FK_Sales_Branch FOREIGN KEY (BranchID) REFERENCES Branches(BranchID),
        CONSTRAINT FK_Sales_TillPoint FOREIGN KEY (TillPointID) REFERENCES TillPoints(TillPointID)
    )
    
    PRINT 'Sales table created'
END
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Sales_InvoiceNumber')
    CREATE INDEX IX_Sales_InvoiceNumber ON Sales(InvoiceNumber)
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Sales_BranchID')
    CREATE INDEX IX_Sales_BranchID ON Sales(BranchID)
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Sales_SaleDate')
    CREATE INDEX IX_Sales_SaleDate ON Sales(SaleDate)
GO

-- Invoices table (Line Items for returns/amendments)
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Invoices')
BEGIN
    CREATE TABLE Invoices (
        InvoiceLineID INT IDENTITY(1,1) PRIMARY KEY,
        InvoiceNumber NVARCHAR(50) NOT NULL,
        SalesID INT NOT NULL,
        BranchID INT NOT NULL,
        ProductID INT NOT NULL,
        ItemCode NVARCHAR(50) NOT NULL,
        ProductName NVARCHAR(200) NOT NULL,
        Quantity DECIMAL(18,3) NOT NULL,
        UnitPrice DECIMAL(18,2) NOT NULL,
        LineTotal DECIMAL(18,2) NOT NULL,
        SaleDate DATETIME NOT NULL,
        CashierID INT NOT NULL,
        CreatedDate DATETIME DEFAULT GETDATE(),
        CONSTRAINT FK_Invoices_Sales FOREIGN KEY (SalesID) REFERENCES Sales(SalesID),
        CONSTRAINT FK_Invoices_Branch FOREIGN KEY (BranchID) REFERENCES Branches(BranchID)
    )
    
    PRINT 'Invoices table created'
END
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Invoices_InvoiceNumber')
    CREATE INDEX IX_Invoices_InvoiceNumber ON Invoices(InvoiceNumber)
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Invoices_SalesID')
    CREATE INDEX IX_Invoices_SalesID ON Invoices(SalesID)
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Invoices_ProductID')
    CREATE INDEX IX_Invoices_ProductID ON Invoices(ProductID)
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Invoices_BranchID')
    CREATE INDEX IX_Invoices_BranchID ON Invoices(BranchID)
GO

PRINT 'TillPoints, Sales and Invoices tables created successfully!'
