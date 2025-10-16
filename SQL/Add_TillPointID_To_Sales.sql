-- Add TillPointID column to Sales table if it doesn't exist
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Sales') AND name = 'TillPointID')
BEGIN
    ALTER TABLE Sales
    ADD TillPointID INT NULL
    
    PRINT 'Added TillPointID column to Sales table'
END
ELSE
BEGIN
    PRINT 'TillPointID column already exists in Sales table'
END
GO

-- Add foreign key constraint if it doesn't exist
IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_Sales_TillPoint')
BEGIN
    ALTER TABLE Sales
    ADD CONSTRAINT FK_Sales_TillPoint FOREIGN KEY (TillPointID) REFERENCES TillPoints(TillPointID)
    
    PRINT 'Added foreign key constraint FK_Sales_TillPoint'
END
ELSE
BEGIN
    PRINT 'Foreign key constraint FK_Sales_TillPoint already exists'
END
GO

PRINT 'Sales table updated successfully!'
