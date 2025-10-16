-- Add RestockItem column to Demo_ReturnDetails table

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Demo_ReturnDetails') AND name = 'RestockItem')
BEGIN
    ALTER TABLE Demo_ReturnDetails ADD RestockItem BIT NOT NULL DEFAULT 1
    PRINT 'Added RestockItem column to Demo_ReturnDetails'
END
ELSE
BEGIN
    PRINT 'RestockItem column already exists in Demo_ReturnDetails'
END
GO
