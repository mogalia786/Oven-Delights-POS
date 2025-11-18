-- =============================================
-- CAKE ORDER PAYMENTS TABLE
-- Tracks deposits and final payments
-- =============================================

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Demo_CakeOrder_Payments')
BEGIN
    CREATE TABLE Demo_CakeOrder_Payments (
        PaymentID INT IDENTITY(1,1) PRIMARY KEY,
        OrderID INT NOT NULL,
        PaymentDate DATETIME DEFAULT GETDATE(),
        PaymentAmount DECIMAL(18,2) NOT NULL,
        PaymentType NVARCHAR(50) NOT NULL, -- 'Deposit', 'Balance', 'Full'
        PaymentMethod NVARCHAR(50) DEFAULT 'Cash', -- 'Cash', 'Card'
        CashierID INT NOT NULL,
        BranchID INT NOT NULL,
        TillPointID INT NOT NULL,
        IsDeposit BIT DEFAULT 0,
        CreatedDate DATETIME DEFAULT GETDATE(),
        CONSTRAINT FK_CakePayments_Order FOREIGN KEY (OrderID) REFERENCES CakeOrders(OrderID),
        CONSTRAINT FK_CakePayments_Cashier FOREIGN KEY (CashierID) REFERENCES Users(UserID),
        CONSTRAINT FK_CakePayments_Branch FOREIGN KEY (BranchID) REFERENCES Branches(BranchID)
    )
    PRINT 'Created table: Demo_CakeOrder_Payments'
END
ELSE
BEGIN
    PRINT 'Table Demo_CakeOrder_Payments already exists'
END

PRINT ''
PRINT '========================================='
PRINT 'Cake Order Payments Table Ready!'
PRINT '========================================='
