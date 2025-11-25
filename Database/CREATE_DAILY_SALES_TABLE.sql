-- Create DailySales table for tracking daily sales
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'DailySales')
BEGIN
    CREATE TABLE DailySales (
        DailySaleID INT IDENTITY(1,1) PRIMARY KEY,
        SaleDate DATE NOT NULL,
        BranchID INT NOT NULL,
        TillNumber NVARCHAR(20) NOT NULL,
        CashierID INT NULL,
        CashierName NVARCHAR(100) NOT NULL,
        InvoiceNumber NVARCHAR(50) NOT NULL,
        SaleType NVARCHAR(20) NOT NULL, -- 'SALE' or 'ORDER'
        TotalAmount DECIMAL(18,2) NOT NULL,
        PaymentMethod NVARCHAR(50) NOT NULL,
        ItemCount INT NOT NULL,
        CreatedDate DATETIME NOT NULL DEFAULT GETDATE(),
        CONSTRAINT FK_DailySales_Branch FOREIGN KEY (BranchID) REFERENCES Branches(BranchID)
    )
    
    CREATE INDEX IX_DailySales_Date ON DailySales(SaleDate)
    CREATE INDEX IX_DailySales_Branch ON DailySales(BranchID, SaleDate)
    CREATE INDEX IX_DailySales_Till ON DailySales(TillNumber, SaleDate)
    CREATE INDEX IX_DailySales_Cashier ON DailySales(CashierID, SaleDate)
    
    PRINT 'DailySales table created successfully'
END
ELSE
BEGIN
    PRINT 'DailySales table already exists'
END
GO
