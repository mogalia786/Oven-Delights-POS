-- =============================================
-- CAKE ORDER SYSTEM - DATABASE TABLES
-- =============================================

-- 1. CAKE ORDER QUESTIONS (Universal questions with options and prices)
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'CakeOrder_Questions')
BEGIN
    CREATE TABLE CakeOrder_Questions (
        QuestionID INT IDENTITY(1,1) PRIMARY KEY,
        BranchID INT NOT NULL,
        QuestionText NVARCHAR(200) NOT NULL,
        QuestionType NVARCHAR(50) NOT NULL, -- 'SingleChoice', 'MultiChoice', 'Text', 'Numeric'
        DisplayOrder INT NOT NULL,
        IsActive BIT DEFAULT 1,
        CreatedDate DATETIME DEFAULT GETDATE(),
        CONSTRAINT FK_CakeQuestions_Branch FOREIGN KEY (BranchID) REFERENCES Branches(BranchID)
    )
    PRINT 'Created table: CakeOrder_Questions'
END

-- 2. CAKE ORDER QUESTION OPTIONS (Options for each question with prices)
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'CakeOrder_QuestionOptions')
BEGIN
    CREATE TABLE CakeOrder_QuestionOptions (
        OptionID INT IDENTITY(1,1) PRIMARY KEY,
        QuestionID INT NOT NULL,
        OptionText NVARCHAR(200) NOT NULL,
        Price DECIMAL(18,2) DEFAULT 0,
        IsActive BIT DEFAULT 1,
        DisplayOrder INT NOT NULL,
        CONSTRAINT FK_CakeOptions_Question FOREIGN KEY (QuestionID) REFERENCES CakeOrder_Questions(QuestionID)
    )
    PRINT 'Created table: CakeOrder_QuestionOptions'
END

-- 3. CAKE ORDERS (Main orders table)
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'CakeOrders')
BEGIN
    CREATE TABLE CakeOrders (
        OrderID INT IDENTITY(1,1) PRIMARY KEY,
        OrderNumber NVARCHAR(50) UNIQUE NOT NULL,
        BranchID INT NOT NULL,
        TillPointID INT NOT NULL,
        CashierID INT NOT NULL,
        
        -- Customer Details
        CustomerName NVARCHAR(200) NOT NULL,
        CustomerPhone NVARCHAR(50),
        CustomerEmail NVARCHAR(200),
        CustomerAddress NVARCHAR(500),
        
        -- Order Details
        OrderDate DATETIME DEFAULT GETDATE(),
        PickupDate DATETIME NOT NULL,
        PickupTime NVARCHAR(20) NOT NULL,
        
        -- Financial
        TotalAmount DECIMAL(18,2) NOT NULL,
        DepositAmount DECIMAL(18,2) NOT NULL,
        BalanceAmount DECIMAL(18,2) NOT NULL,
        
        -- Status
        OrderStatus NVARCHAR(50) DEFAULT 'Pending', -- 'Pending', 'InProduction', 'Ready', 'Delivered', 'Cancelled'
        IsDelivered BIT DEFAULT 0,
        DeliveredDate DATETIME NULL,
        DeliveredBy INT NULL,
        
        -- Manufacturing
        SentToManufacturing BIT DEFAULT 0,
        ManufacturingSentDate DATETIME NULL,
        
        -- Ledger
        DebtorLedgerID INT NULL,
        SaleID INT NULL, -- Links to Demo_Sales when order is completed
        
        CreatedDate DATETIME DEFAULT GETDATE(),
        ModifiedDate DATETIME DEFAULT GETDATE(),
        
        CONSTRAINT FK_CakeOrders_Branch FOREIGN KEY (BranchID) REFERENCES Branches(BranchID),
        CONSTRAINT FK_CakeOrders_Cashier FOREIGN KEY (CashierID) REFERENCES Users(UserID)
    )
    PRINT 'Created table: CakeOrders'
END

-- 4. CAKE ORDER DETAILS (Answers to questions)
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'CakeOrder_Details')
BEGIN
    CREATE TABLE CakeOrder_Details (
        DetailID INT IDENTITY(1,1) PRIMARY KEY,
        OrderID INT NOT NULL,
        QuestionID INT NOT NULL,
        QuestionText NVARCHAR(200) NOT NULL,
        AnswerText NVARCHAR(MAX) NOT NULL,
        AnswerPrice DECIMAL(18,2) DEFAULT 0,
        CONSTRAINT FK_CakeDetails_Order FOREIGN KEY (OrderID) REFERENCES CakeOrders(OrderID),
        CONSTRAINT FK_CakeDetails_Question FOREIGN KEY (QuestionID) REFERENCES CakeOrder_Questions(QuestionID)
    )
    PRINT 'Created table: CakeOrder_Details'
END

-- 5. CAKE ORDER ACCESSORIES (Selected accessories)
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'CakeOrder_Accessories')
BEGIN
    CREATE TABLE CakeOrder_Accessories (
        OrderAccessoryID INT IDENTITY(1,1) PRIMARY KEY,
        OrderID INT NOT NULL,
        AccessoryID INT NOT NULL,
        AccessoryName NVARCHAR(200) NOT NULL,
        Quantity INT DEFAULT 1,
        UnitPrice DECIMAL(18,2) NOT NULL,
        TotalPrice DECIMAL(18,2) NOT NULL,
        CONSTRAINT FK_CakeAccessories_Order FOREIGN KEY (OrderID) REFERENCES CakeOrders(OrderID)
    )
    PRINT 'Created table: CakeOrder_Accessories'
END

-- 6. CAKE ORDER TOPPINGS (Selected toppings)
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'CakeOrder_Toppings')
BEGIN
    CREATE TABLE CakeOrder_Toppings (
        OrderToppingID INT IDENTITY(1,1) PRIMARY KEY,
        OrderID INT NOT NULL,
        ToppingID INT NOT NULL,
        ToppingName NVARCHAR(200) NOT NULL,
        Quantity INT DEFAULT 1,
        UnitPrice DECIMAL(18,2) NOT NULL,
        TotalPrice DECIMAL(18,2) NOT NULL,
        CONSTRAINT FK_CakeToppings_Order FOREIGN KEY (OrderID) REFERENCES CakeOrders(OrderID)
    )
    PRINT 'Created table: CakeOrder_Toppings'
END

PRINT ''
PRINT '========================================='
PRINT 'Cake Order Tables Created Successfully!'
PRINT '========================================='
