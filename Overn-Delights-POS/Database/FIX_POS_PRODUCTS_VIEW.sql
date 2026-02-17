-- Fix vw_POS_Products view to use SKU as ItemCode and CurrentStock

-- Drop existing view
IF EXISTS (SELECT * FROM sys.views WHERE name = 'vw_POS_Products')
    DROP VIEW vw_POS_Products
GO

-- Create view with correct columns
CREATE VIEW vw_POS_Products AS
SELECT 
    p.ProductID,
    p.SKU AS ItemCode,
    p.Name AS ProductName,
    ISNULL(p.Barcode, p.SKU) AS Barcode,
    ISNULL(
        (SELECT TOP 1 SellingPrice FROM Demo_Retail_Price 
         WHERE ProductID = p.ProductID AND BranchID = p.BranchID 
         ORDER BY EffectiveFrom DESC),
        (SELECT TOP 1 SellingPrice FROM Demo_Retail_Price 
         WHERE ProductID = p.ProductID AND BranchID IS NULL 
         ORDER BY EffectiveFrom DESC)
    ) AS SellingPrice,
    ISNULL(p.CurrentStock, 0) AS QtyOnHand,
    0 AS ReorderLevel,
    p.Category,
    p.BranchID
FROM Demo_Retail_Product p
WHERE p.IsActive = 1
  AND (p.ProductType = 'External' OR p.ProductType = 'Internal')
GO

PRINT 'vw_POS_Products view created successfully with SKU as ItemCode and CurrentStock'
