-- Drop and recreate User Defined Orders tables with correct structure

PRINT '========================================='
PRINT 'RECREATING USER DEFINED ORDERS TABLES'
PRINT '========================================='
PRINT ''

-- Drop existing tables if they exist
IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[POS_UserDefinedOrderItems]') AND type in (N'U'))
BEGIN
    DROP TABLE [dbo].[POS_UserDefinedOrderItems]
    PRINT 'Dropped existing POS_UserDefinedOrderItems table'
END

IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[POS_UserDefinedOrders]') AND type in (N'U'))
BEGIN
    DROP TABLE [dbo].[POS_UserDefinedOrders]
    PRINT 'Dropped existing POS_UserDefinedOrders table'
END
GO

-- Create POS_UserDefinedOrders table
CREATE TABLE [dbo].[POS_UserDefinedOrders](
    [UserDefinedOrderID] [int] IDENTITY(1,1) NOT NULL,
    [OrderNumber] [nvarchar](20) NOT NULL,
    [BranchID] [int] NOT NULL,
    [BranchName] [nvarchar](100) NULL,
    [CashierID] [int] NOT NULL,
    [CashierName] [nvarchar](100) NOT NULL,
    [TillPointID] [int] NULL,
    [CustomerCellNumber] [nvarchar](20) NOT NULL,
    [CustomerName] [nvarchar](100) NOT NULL,
    [CustomerSurname] [nvarchar](100) NULL,
    [CakeColour] [nvarchar](100) NULL,
    [CakeImage] [nvarchar](200) NULL,
    [SpecialRequest] [nvarchar](500) NULL,
    [CollectionDate] [date] NOT NULL,
    [CollectionTime] [time](7) NOT NULL,
    [CollectionDay] [nvarchar](20) NULL,
    [OrderDate] [date] NOT NULL,
    [OrderTime] [time](7) NOT NULL,
    [OrderDateTime] [datetime] NOT NULL DEFAULT GETDATE(),
    [TotalAmount] [decimal](18, 2) NOT NULL,
    [AmountPaid] [decimal](18, 2) NOT NULL,
    [PaymentMethod] [nvarchar](20) NOT NULL,
    [CashAmount] [decimal](18, 2) NULL,
    [CardAmount] [decimal](18, 2) NULL,
    [Status] [nvarchar](20) NOT NULL DEFAULT 'Created',
    [CompletedDate] [datetime] NULL,
    [CompletedBy] [nvarchar](100) NULL,
    [PickedUpDate] [date] NULL,
    [PickedUpTime] [time](7) NULL,
    [PickedUpDateTime] [datetime] NULL,
    [PickedUpBy] [nvarchar](100) NULL,
    [SaleID] [int] NULL,
    [InvoiceNumber] [nvarchar](50) NULL,
    [CreatedDate] [datetime] NOT NULL DEFAULT GETDATE(),
    [ModifiedDate] [datetime] NULL,
    
    CONSTRAINT [PK_POS_UserDefinedOrders] PRIMARY KEY CLUSTERED ([UserDefinedOrderID] ASC),
    CONSTRAINT [UQ_UserDefinedOrderNumber] UNIQUE ([OrderNumber])
)
PRINT 'Created POS_UserDefinedOrders table'
GO

-- Create POS_UserDefinedOrderItems table
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
PRINT 'Created POS_UserDefinedOrderItems table'
GO

-- Create Indexes
CREATE NONCLUSTERED INDEX [IX_UserDefinedOrders_BranchID] ON [dbo].[POS_UserDefinedOrders]([BranchID])
PRINT 'Created index IX_UserDefinedOrders_BranchID'

CREATE NONCLUSTERED INDEX [IX_UserDefinedOrders_Status] ON [dbo].[POS_UserDefinedOrders]([Status])
PRINT 'Created index IX_UserDefinedOrders_Status'

CREATE NONCLUSTERED INDEX [IX_UserDefinedOrders_CustomerCell] ON [dbo].[POS_UserDefinedOrders]([CustomerCellNumber])
PRINT 'Created index IX_UserDefinedOrders_CustomerCell'

CREATE NONCLUSTERED INDEX [IX_UserDefinedOrders_CollectionDate] ON [dbo].[POS_UserDefinedOrders]([CollectionDate])
PRINT 'Created index IX_UserDefinedOrders_CollectionDate'

CREATE NONCLUSTERED INDEX [IX_UserDefinedOrderItems_OrderID] ON [dbo].[POS_UserDefinedOrderItems]([UserDefinedOrderID])
PRINT 'Created index IX_UserDefinedOrderItems_OrderID'
GO

-- Verify table structure
PRINT ''
PRINT 'POS_UserDefinedOrderItems columns:'
SELECT 
    c.name AS ColumnName,
    t.name AS DataType,
    c.max_length AS MaxLength,
    c.is_nullable AS IsNullable
FROM sys.columns c
INNER JOIN sys.types t ON c.user_type_id = t.user_type_id
WHERE c.object_id = OBJECT_ID('POS_UserDefinedOrderItems')
ORDER BY c.column_id
GO

PRINT ''
PRINT '========================================='
PRINT 'TABLES RECREATED SUCCESSFULLY!'
PRINT '========================================='
GO
