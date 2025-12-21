-- =============================================
-- Box Barcode System - Database Schema
-- Purpose: Store boxed items with unique box barcode
-- =============================================

-- Create BoxedItems table
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'BoxedItems')
BEGIN
    CREATE TABLE BoxedItems (
        BoxItemID INT IDENTITY(1,1) PRIMARY KEY,
        BoxBarcode VARCHAR(50) NOT NULL,
        ItemBarcode VARCHAR(50) NOT NULL,
        ProductName VARCHAR(255) NOT NULL,
        Quantity DECIMAL(10,2) NOT NULL DEFAULT 1,
        Price DECIMAL(10,2) NOT NULL,
        BranchID INT NOT NULL,
        CreatedBy VARCHAR(100) NOT NULL,
        CreatedDate DATETIME NOT NULL DEFAULT GETDATE(),
        IsActive BIT NOT NULL DEFAULT 1,
        CONSTRAINT FK_BoxedItems_Branch FOREIGN KEY (BranchID) REFERENCES Branches(BranchID)
    )
    
    -- Create index on BoxBarcode for fast lookup
    CREATE INDEX IX_BoxedItems_BoxBarcode ON BoxedItems(BoxBarcode)
    CREATE INDEX IX_BoxedItems_BranchID ON BoxedItems(BranchID)
    
    PRINT 'BoxedItems table created successfully'
END
ELSE
BEGIN
    PRINT 'BoxedItems table already exists'
END
GO

-- Create stored procedure to get box items
IF EXISTS (SELECT * FROM sys.procedures WHERE name = 'sp_GetBoxItems')
    DROP PROCEDURE sp_GetBoxItems
GO

CREATE PROCEDURE sp_GetBoxItems
    @BoxBarcode VARCHAR(50)
AS
BEGIN
    SET NOCOUNT ON
    
    SELECT 
        BoxItemID,
        BoxBarcode,
        ItemBarcode,
        ProductName,
        Quantity,
        Price,
        BranchID,
        CreatedBy,
        CreatedDate
    FROM BoxedItems
    WHERE BoxBarcode = @BoxBarcode
      AND IsActive = 1
    ORDER BY BoxItemID
END
GO

-- Create stored procedure to save box items
IF EXISTS (SELECT * FROM sys.procedures WHERE name = 'sp_SaveBoxItems')
    DROP PROCEDURE sp_SaveBoxItems
GO

CREATE PROCEDURE sp_SaveBoxItems
    @BoxBarcode VARCHAR(50),
    @ItemBarcode VARCHAR(50),
    @ProductName VARCHAR(255),
    @Quantity DECIMAL(10,2),
    @Price DECIMAL(10,2),
    @BranchID INT,
    @CreatedBy VARCHAR(100)
AS
BEGIN
    SET NOCOUNT ON
    
    INSERT INTO BoxedItems (
        BoxBarcode,
        ItemBarcode,
        ProductName,
        Quantity,
        Price,
        BranchID,
        CreatedBy,
        CreatedDate,
        IsActive
    )
    VALUES (
        @BoxBarcode,
        @ItemBarcode,
        @ProductName,
        @Quantity,
        @Price,
        @BranchID,
        @CreatedBy,
        GETDATE(),
        1
    )
    
    SELECT SCOPE_IDENTITY() AS BoxItemID
END
GO

-- Create stored procedure to check if box barcode exists
IF EXISTS (SELECT * FROM sys.procedures WHERE name = 'sp_CheckBoxBarcodeExists')
    DROP PROCEDURE sp_CheckBoxBarcodeExists
GO

CREATE PROCEDURE sp_CheckBoxBarcodeExists
    @BoxBarcode VARCHAR(50)
AS
BEGIN
    SET NOCOUNT ON
    
    SELECT COUNT(*) AS BoxExists
    FROM BoxedItems
    WHERE BoxBarcode = @BoxBarcode
      AND IsActive = 1
END
GO

PRINT 'Box Barcode System database schema created successfully'
