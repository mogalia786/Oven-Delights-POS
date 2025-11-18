-- Add CurrentStock column to Demo_Retail_Product table

-- Check if Demo_Retail_Product table exists
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Demo_Retail_Product')
BEGIN
    PRINT 'ERROR: Demo_Retail_Product table does not exist!'
    RETURN
END

PRINT 'Demo_Retail_Product table found'

-- List all columns in the table to see what exists
PRINT 'Current columns in Demo_Retail_Product:'
SELECT c.name AS ColumnName, t.name AS DataType, c.max_length, c.is_nullable
FROM sys.columns c
INNER JOIN sys.types t ON c.user_type_id = t.user_type_id
WHERE c.object_id = OBJECT_ID('Demo_Retail_Product')
ORDER BY c.column_id

-- Add CurrentStock column if it doesn't exist
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Demo_Retail_Product') AND name = 'CurrentStock')
BEGIN
    PRINT 'Adding CurrentStock column...'
    ALTER TABLE Demo_Retail_Product ADD CurrentStock DECIMAL(18,2) NOT NULL DEFAULT 0
    PRINT 'CurrentStock column added successfully'
END
ELSE
BEGIN
    PRINT 'CurrentStock column already exists'
END
GO

PRINT 'Demo_Retail_Product CurrentStock setup completed!'
GO
