-- Add fields for pre-printed cake order form
-- These fields capture data that will be printed in specific positions on the form

-- Add CakeColor field
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS 
               WHERE TABLE_NAME = 'POS_CustomOrders' AND COLUMN_NAME = 'CakeColor')
BEGIN
    ALTER TABLE POS_CustomOrders ADD CakeColor NVARCHAR(100);
    PRINT 'Added CakeColor column';
END

-- Add CakePicture field (reference to picture/design)
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS 
               WHERE TABLE_NAME = 'POS_CustomOrders' AND COLUMN_NAME = 'CakePicture')
BEGIN
    ALTER TABLE POS_CustomOrders ADD CakePicture NVARCHAR(200);
    PRINT 'Added CakePicture column';
END

-- Add CollectionDay field (e.g., "Friday")
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS 
               WHERE TABLE_NAME = 'POS_CustomOrders' AND COLUMN_NAME = 'CollectionDay')
BEGIN
    ALTER TABLE POS_CustomOrders ADD CollectionDay NVARCHAR(20);
    PRINT 'Added CollectionDay column';
END

-- Add CollectionPoint field (branch/location)
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS 
               WHERE TABLE_NAME = 'POS_CustomOrders' AND COLUMN_NAME = 'CollectionPoint')
BEGIN
    ALTER TABLE POS_CustomOrders ADD CollectionPoint NVARCHAR(100);
    PRINT 'Added CollectionPoint column';
END

-- Add AccountNumber field (for customer accounts)
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS 
               WHERE TABLE_NAME = 'POS_CustomOrders' AND COLUMN_NAME = 'AccountNumber')
BEGIN
    ALTER TABLE POS_CustomOrders ADD AccountNumber NVARCHAR(50);
    PRINT 'Added AccountNumber column';
END

-- Ensure SpecialInstructions exists (for dropdown items)
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS 
               WHERE TABLE_NAME = 'POS_CustomOrders' AND COLUMN_NAME = 'SpecialInstructions')
BEGIN
    ALTER TABLE POS_CustomOrders ADD SpecialInstructions NVARCHAR(MAX);
    PRINT 'Added SpecialInstructions column';
END

-- Add IsRevised field (to track revised orders)
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS 
               WHERE TABLE_NAME = 'POS_CustomOrders' AND COLUMN_NAME = 'IsRevised')
BEGIN
    ALTER TABLE POS_CustomOrders ADD IsRevised BIT DEFAULT 0;
    PRINT 'Added IsRevised column';
END

-- Add RevisionNotes field
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS 
               WHERE TABLE_NAME = 'POS_CustomOrders' AND COLUMN_NAME = 'RevisionNotes')
BEGIN
    ALTER TABLE POS_CustomOrders ADD RevisionNotes NVARCHAR(500);
    PRINT 'Added RevisionNotes column';
END

GO

PRINT 'Cake order form fields updated successfully!';
GO
