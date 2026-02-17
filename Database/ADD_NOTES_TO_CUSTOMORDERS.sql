-- Add Notes column to POS_CustomOrders table

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'POS_CustomOrders') AND name = 'Notes')
BEGIN
    ALTER TABLE POS_CustomOrders
    ADD Notes NVARCHAR(500) NULL
    
    PRINT 'Notes column added to POS_CustomOrders table'
END
ELSE
BEGIN
    PRINT 'Notes column already exists in POS_CustomOrders table'
END
GO
