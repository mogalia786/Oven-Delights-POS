-- =============================================
-- Create User Defined Orders Tables
-- Purpose: Handle same-day/next-day cake orders with full upfront payment
-- Order Number Format: BranchID&6&sequence (e.g., 6600001)
-- =============================================

-- Main User Defined Orders Table
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[POS_UserDefinedOrders]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[POS_UserDefinedOrders](
        [UserDefinedOrderID] [int] IDENTITY(1,1) NOT NULL,
        [OrderNumber] [nvarchar](20) NOT NULL,
        [BranchID] [int] NOT NULL,
        [BranchName] [nvarchar](100) NULL,
        [CashierID] [int] NOT NULL,
        [CashierName] [nvarchar](100) NOT NULL,
        [TillPointID] [int] NULL,
        
        -- Customer Information
        [CustomerCellNumber] [nvarchar](20) NOT NULL,
        [CustomerName] [nvarchar](100) NOT NULL,
        [CustomerSurname] [nvarchar](100) NULL,
        
        -- Order Details
        [CakeColour] [nvarchar](100) NULL,
        [CakeImage] [nvarchar](200) NULL,
        [SpecialRequest] [nvarchar](500) NULL,
        
        -- Collection Information
        [CollectionDate] [date] NOT NULL,
        [CollectionTime] [time](7) NOT NULL,
        [CollectionDay] [nvarchar](20) NULL,
        
        -- Order Dates
        [OrderDate] [date] NOT NULL,
        [OrderTime] [time](7) NOT NULL,
        [OrderDateTime] [datetime] NOT NULL DEFAULT GETDATE(),
        
        -- Financial Information
        [TotalAmount] [decimal](18, 2) NOT NULL,
        [AmountPaid] [decimal](18, 2) NOT NULL,
        [PaymentMethod] [nvarchar](20) NOT NULL, -- CASH, CARD, SPLIT
        [CashAmount] [decimal](18, 2) NULL,
        [CardAmount] [decimal](18, 2) NULL,
        
        -- Status Tracking
        [Status] [nvarchar](20) NOT NULL DEFAULT 'Created', -- Created, Completed, PickedUp
        
        -- Completion Information (Manufacturer)
        [CompletedDate] [datetime] NULL,
        [CompletedBy] [nvarchar](100) NULL,
        
        -- Pickup Information (Cashier)
        [PickedUpDate] [date] NULL,
        [PickedUpTime] [time](7) NULL,
        [PickedUpDateTime] [datetime] NULL,
        [PickedUpBy] [nvarchar](100) NULL,
        
        -- Links to Sales System
        [SaleID] [int] NULL,
        [InvoiceNumber] [nvarchar](50) NULL,
        
        -- Audit Fields
        [CreatedDate] [datetime] NOT NULL DEFAULT GETDATE(),
        [ModifiedDate] [datetime] NULL,
        
        CONSTRAINT [PK_POS_UserDefinedOrders] PRIMARY KEY CLUSTERED ([UserDefinedOrderID] ASC),
        CONSTRAINT [UQ_UserDefinedOrderNumber] UNIQUE ([OrderNumber])
    )
    
    PRINT 'Table POS_UserDefinedOrders created successfully.'
END
ELSE
BEGIN
    PRINT 'Table POS_UserDefinedOrders already exists.'
END
GO

-- User Defined Order Items Table
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[POS_UserDefinedOrderItems]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[POS_UserDefinedOrderItems](
        [ItemID] [int] IDENTITY(1,1) NOT NULL,
        [UserDefinedOrderID] [int] NOT NULL,
        [ProductID] [int] NOT NULL,
        [ProductName] [nvarchar](200) NOT NULL,
        [ProductCode] [nvarchar](50) NULL,
        [Quantity] [decimal](18, 2) NOT NULL,
        [UnitPrice] [decimal](18, 2) NOT NULL,
        [LineTotal] [decimal](18, 2) NOT NULL,
        [CreatedDate] [datetime] NOT NULL DEFAULT GETDATE(),
        
        CONSTRAINT [PK_POS_UserDefinedOrderItems] PRIMARY KEY CLUSTERED ([ItemID] ASC),
        CONSTRAINT [FK_UserDefinedOrderItems_Orders] FOREIGN KEY ([UserDefinedOrderID]) 
            REFERENCES [dbo].[POS_UserDefinedOrders]([UserDefinedOrderID]) ON DELETE CASCADE
    )
    
    PRINT 'Table POS_UserDefinedOrderItems created successfully.'
END
ELSE
BEGIN
    PRINT 'Table POS_UserDefinedOrderItems already exists.'
END
GO

-- Create Indexes for Performance
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_UserDefinedOrders_BranchID' AND object_id = OBJECT_ID('POS_UserDefinedOrders'))
BEGIN
    CREATE NONCLUSTERED INDEX [IX_UserDefinedOrders_BranchID] ON [dbo].[POS_UserDefinedOrders]([BranchID])
    PRINT 'Index IX_UserDefinedOrders_BranchID created.'
END

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_UserDefinedOrders_Status' AND object_id = OBJECT_ID('POS_UserDefinedOrders'))
BEGIN
    CREATE NONCLUSTERED INDEX [IX_UserDefinedOrders_Status] ON [dbo].[POS_UserDefinedOrders]([Status])
    PRINT 'Index IX_UserDefinedOrders_Status created.'
END

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_UserDefinedOrders_CustomerCell' AND object_id = OBJECT_ID('POS_UserDefinedOrders'))
BEGIN
    CREATE NONCLUSTERED INDEX [IX_UserDefinedOrders_CustomerCell] ON [dbo].[POS_UserDefinedOrders]([CustomerCellNumber])
    PRINT 'Index IX_UserDefinedOrders_CustomerCell created.'
END

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_UserDefinedOrders_CollectionDate' AND object_id = OBJECT_ID('POS_UserDefinedOrders'))
BEGIN
    CREATE NONCLUSTERED INDEX [IX_UserDefinedOrders_CollectionDate] ON [dbo].[POS_UserDefinedOrders]([CollectionDate])
    PRINT 'Index IX_UserDefinedOrders_CollectionDate created.'
END

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_UserDefinedOrderItems_OrderID' AND object_id = OBJECT_ID('POS_UserDefinedOrderItems'))
BEGIN
    CREATE NONCLUSTERED INDEX [IX_UserDefinedOrderItems_OrderID] ON [dbo].[POS_UserDefinedOrderItems]([UserDefinedOrderID])
    PRINT 'Index IX_UserDefinedOrderItems_OrderID created.'
END

GO

PRINT '========================================='
PRINT 'User Defined Orders tables created successfully!'
PRINT 'Tables: POS_UserDefinedOrders, POS_UserDefinedOrderItems'
PRINT '========================================='
