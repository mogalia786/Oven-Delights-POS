-- Fix POS_UserDefinedOrderItems table to add missing ProductID column if needed

-- Check if ProductID column exists, if not add it
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('POS_UserDefinedOrderItems') AND name = 'ProductID')
BEGIN
    ALTER TABLE POS_UserDefinedOrderItems
    ADD ProductID INT NOT NULL DEFAULT 0
    
    PRINT 'Added ProductID column to POS_UserDefinedOrderItems'
END
ELSE
BEGIN
    PRINT 'ProductID column already exists in POS_UserDefinedOrderItems'
END
GO

-- Verify table structure
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

PRINT 'POS_UserDefinedOrderItems table structure verified'
GO
