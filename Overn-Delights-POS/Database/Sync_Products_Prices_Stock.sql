-- Comprehensive sync: Ensure all POS products have prices and stock for all branches
BEGIN TRANSACTION;

-- 1. Create default prices for products that don't have any (set to 0.00)
INSERT INTO Demo_Retail_Price (ProductID, BranchID, CostPrice, SellingPrice, EffectiveFrom, CreatedAt)
SELECT 
    p.ProductID,
    b.BranchID,
    0.00 AS CostPrice,
    0.00 AS SellingPrice,
    GETDATE() AS EffectiveFrom,
    GETDATE() AS CreatedAt
FROM Demo_Retail_Product p
CROSS JOIN Branches b
WHERE p.IsActive = 1
  AND b.IsActive = 1
  AND p.ProductType IN ('External', 'Internal')
  AND NOT EXISTS (
      SELECT 1 FROM Demo_Retail_Price pr 
      WHERE pr.ProductID = p.ProductID 
        AND pr.BranchID = b.BranchID
  );

PRINT 'Created default prices for products without prices';

-- 2. Create stock records for products that don't have any (set to 0 quantity)
INSERT INTO RetailStock (ProductID, BranchID, Quantity, StockType, LastUpdated, UpdatedBy)
SELECT 
    p.ProductID,
    b.BranchID,
    0 AS Quantity,
    p.ProductType AS StockType,
    GETDATE() AS LastUpdated,
    'SYSTEM' AS UpdatedBy
FROM Demo_Retail_Product p
CROSS JOIN Branches b
WHERE p.IsActive = 1
  AND b.IsActive = 1
  AND p.ProductType IN ('External', 'Internal')
  AND NOT EXISTS (
      SELECT 1 FROM RetailStock s 
      WHERE s.ProductID = p.ProductID 
        AND s.BranchID = b.BranchID
  );

PRINT 'Created stock records for products without stock';

COMMIT TRANSACTION;

-- Verify results
SELECT 'Products without prices' AS CheckType, COUNT(*) AS Count
FROM Demo_Retail_Product p
WHERE p.IsActive = 1
  AND NOT EXISTS (SELECT 1 FROM Demo_Retail_Price pr WHERE pr.ProductID = p.ProductID)
UNION ALL
SELECT 'Products without stock', COUNT(*)
FROM Demo_Retail_Product p
WHERE p.IsActive = 1
  AND NOT EXISTS (SELECT 1 FROM RetailStock s WHERE s.ProductID = p.ProductID);

PRINT 'Product sync completed!';
