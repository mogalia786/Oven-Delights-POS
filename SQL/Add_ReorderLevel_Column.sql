-- Add ReorderLevel column to Demo_Retail_Stock table if it doesn't exist

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Demo_Retail_Stock') AND name = 'ReorderLevel')
BEGIN
    ALTER TABLE Demo_Retail_Stock
    ADD ReorderLevel DECIMAL(18,3) NULL DEFAULT 5
    
    PRINT 'Added ReorderLevel column to Demo_Retail_Stock table'
    
    -- Set default reorder level for existing records
    UPDATE Demo_Retail_Stock
    SET ReorderLevel = 5
    WHERE ReorderLevel IS NULL
    
    PRINT 'Set default ReorderLevel = 5 for all existing stock records'
END
ELSE
BEGIN
    PRINT 'ReorderLevel column already exists in Demo_Retail_Stock table'
END
GO

-- Now recreate the view with ReorderLevel
IF OBJECT_ID('vw_POS_Products', 'V') IS NOT NULL
    DROP VIEW vw_POS_Products
GO

CREATE VIEW vw_POS_Products
AS
SELECT 
    p.ProductID,
    p.SKU AS ItemCode,
    p.Name AS ProductName,
    pc.CategoryName AS Category,
    p.CategoryID,
    p.IsActive,
    s.StockID,
    s.BranchID,
    s.QtyOnHand,
    ISNULL(s.ReorderLevel, 5) AS ReorderLevel,
    ISNULL(pr.SellingPrice, 0) AS SellingPrice,
    ISNULL(pr.CostPrice, 0) AS CostPrice
FROM Demo_Retail_Product p
INNER JOIN Demo_Retail_Stock s ON p.ProductID = s.StockID
LEFT JOIN ProductCategories pc ON p.CategoryID = pc.CategoryID
LEFT JOIN Demo_Retail_Price pr ON s.StockID = pr.ProductID
WHERE p.IsActive = 1
GO

PRINT 'View vw_POS_Products created successfully with ReorderLevel!'
