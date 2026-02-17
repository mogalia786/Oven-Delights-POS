-- Add Priority column to Demo_Retail_Price for branch-specific item display ordering
-- Lower priority number = displayed first (1, 2, 3...)
-- NULL priority = displayed alphabetically after prioritized items

-- Check if column exists before adding
IF NOT EXISTS (
    SELECT 1 
    FROM INFORMATION_SCHEMA.COLUMNS 
    WHERE TABLE_NAME = 'Demo_Retail_Price' 
    AND COLUMN_NAME = 'DisplayPriority'
)
BEGIN
    ALTER TABLE Demo_Retail_Price
    ADD DisplayPriority INT NULL;
    
    PRINT 'DisplayPriority column added to Demo_Retail_Price table';
END
ELSE
BEGIN
    PRINT 'DisplayPriority column already exists in Demo_Retail_Price table';
END
GO

-- Create index for better performance when ordering by priority
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Demo_Retail_Price_DisplayPriority' AND object_id = OBJECT_ID('Demo_Retail_Price'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_Demo_Retail_Price_DisplayPriority
    ON Demo_Retail_Price(BranchID, ProductID, DisplayPriority);
    
    PRINT 'Index IX_Demo_Retail_Price_DisplayPriority created';
END
ELSE
BEGIN
    PRINT 'Index IX_Demo_Retail_Price_DisplayPriority already exists';
END
GO
