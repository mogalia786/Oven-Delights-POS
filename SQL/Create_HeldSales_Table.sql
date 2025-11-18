-- Table to store held sales (on-hold transactions)
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Demo_HeldSales')
BEGIN
    CREATE TABLE Demo_HeldSales (
        HeldSaleID INT IDENTITY(1,1) PRIMARY KEY,
        HoldNumber VARCHAR(50) NOT NULL UNIQUE,
        CashierID INT NOT NULL,
        BranchID INT NOT NULL,
        TillPointID INT NOT NULL,
        HoldDate DATETIME NOT NULL DEFAULT GETDATE(),
        CustomerName VARCHAR(200) NULL,
        Notes VARCHAR(500) NULL,
        IsRecalled BIT NOT NULL DEFAULT 0,
        RecalledDate DATETIME NULL,
        CONSTRAINT FK_HeldSales_Cashier FOREIGN KEY (CashierID) REFERENCES Users(UserID),
        CONSTRAINT FK_HeldSales_Branch FOREIGN KEY (BranchID) REFERENCES Branches(BranchID)
    )
    
    CREATE INDEX IX_HeldSales_HoldNumber ON Demo_HeldSales(HoldNumber)
    CREATE INDEX IX_HeldSales_Cashier ON Demo_HeldSales(CashierID)
    CREATE INDEX IX_HeldSales_IsRecalled ON Demo_HeldSales(IsRecalled)
END
GO

-- Table to store line items for held sales
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Demo_HeldSaleItems')
BEGIN
    CREATE TABLE Demo_HeldSaleItems (
        HeldSaleItemID INT IDENTITY(1,1) PRIMARY KEY,
        HeldSaleID INT NOT NULL,
        ProductID INT NOT NULL,
        ItemCode VARCHAR(50) NOT NULL,
        ProductName VARCHAR(200) NOT NULL,
        Quantity DECIMAL(10,2) NOT NULL,
        UnitPrice DECIMAL(10,2) NOT NULL,
        DiscountPercent DECIMAL(5,2) NOT NULL DEFAULT 0,
        LineTotal DECIMAL(10,2) NOT NULL,
        CONSTRAINT FK_HeldSaleItems_HeldSale FOREIGN KEY (HeldSaleID) REFERENCES Demo_HeldSales(HeldSaleID) ON DELETE CASCADE
    )
    
    CREATE INDEX IX_HeldSaleItems_HeldSaleID ON Demo_HeldSaleItems(HeldSaleID)
END
GO

PRINT 'Held Sales tables created successfully'
