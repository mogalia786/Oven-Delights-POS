-- Add SpecialInstructions column to POS_CustomOrders if it doesn't exist
IF NOT EXISTS (
    SELECT * FROM INFORMATION_SCHEMA.COLUMNS 
    WHERE TABLE_NAME = 'POS_CustomOrders' 
    AND COLUMN_NAME = 'SpecialInstructions'
)
BEGIN
    ALTER TABLE POS_CustomOrders
    ADD SpecialInstructions NVARCHAR(500) NULL;
    
    PRINT 'SpecialInstructions column added to POS_CustomOrders';
END
ELSE
BEGIN
    PRINT 'SpecialInstructions column already exists';
END
GO
