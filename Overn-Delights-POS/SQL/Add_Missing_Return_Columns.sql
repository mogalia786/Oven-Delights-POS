-- Add missing columns to Demo_Returns table

-- Add CustomerName if missing
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Demo_Returns') AND name = 'CustomerName')
BEGIN
    ALTER TABLE Demo_Returns ADD CustomerName VARCHAR(200) NULL
    PRINT 'Added CustomerName column'
END
ELSE
BEGIN
    PRINT 'CustomerName column already exists'
END
GO

-- Add CustomerPhone if missing
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Demo_Returns') AND name = 'CustomerPhone')
BEGIN
    ALTER TABLE Demo_Returns ADD CustomerPhone VARCHAR(50) NULL
    PRINT 'Added CustomerPhone column'
END
ELSE
BEGIN
    PRINT 'CustomerPhone column already exists'
END
GO

-- Add CustomerAddress if missing
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Demo_Returns') AND name = 'CustomerAddress')
BEGIN
    ALTER TABLE Demo_Returns ADD CustomerAddress VARCHAR(500) NULL
    PRINT 'Added CustomerAddress column'
END
ELSE
BEGIN
    PRINT 'CustomerAddress column already exists'
END
GO

-- Add OriginalInvoiceNumber if missing
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Demo_Returns') AND name = 'OriginalInvoiceNumber')
BEGIN
    ALTER TABLE Demo_Returns ADD OriginalInvoiceNumber VARCHAR(50) NULL
    PRINT 'Added OriginalInvoiceNumber column'
END
ELSE
BEGIN
    PRINT 'OriginalInvoiceNumber column already exists'
END
GO

-- Add TillPointID if missing
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Demo_Returns') AND name = 'TillPointID')
BEGIN
    ALTER TABLE Demo_Returns ADD TillPointID INT NULL
    PRINT 'Added TillPointID column'
END
ELSE
BEGIN
    PRINT 'TillPointID column already exists'
END
GO

-- Add CashierID if missing
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Demo_Returns') AND name = 'CashierID')
BEGIN
    ALTER TABLE Demo_Returns ADD CashierID INT NULL
    PRINT 'Added CashierID column'
END
ELSE
BEGIN
    PRINT 'CashierID column already exists'
END
GO

-- Add SupervisorID if missing
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Demo_Returns') AND name = 'SupervisorID')
BEGIN
    ALTER TABLE Demo_Returns ADD SupervisorID INT NULL
    PRINT 'Added SupervisorID column'
END
ELSE
BEGIN
    PRINT 'SupervisorID column already exists'
END
GO

PRINT 'All missing columns added successfully!'
GO
