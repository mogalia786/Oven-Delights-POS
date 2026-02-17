-- =============================================
-- Fix BranchID 4 Prices - Copy from Master Prices
-- =============================================

-- 1. Check current state
SELECT 
    'BranchID 4 Products with Prices' AS CheckType,
    COUNT(*) AS Count
FROM Demo_Retail_Price
WHERE BranchID = 4;

SELECT 
    'BranchID 4 Products with Zero Prices' AS CheckType,
    COUNT(*) AS Count
FROM Demo_Retail_Price
WHERE BranchID = 4 AND (SellingPrice = 0 OR SellingPrice IS NULL);

SELECT 
    'Master Prices (BranchID IS NULL)' AS CheckType,
    COUNT(*) AS Count
FROM Demo_Retail_Price
WHERE BranchID IS NULL;

-- 2. Fix: Copy prices from Products table to BranchID 4 for products that don't have prices
-- Only insert for products that exist in Demo_Retail_Product (to avoid FK constraint error)
INSERT INTO Demo_Retail_Price (ProductID, BranchID, SellingPrice, CostPrice, EffectiveFrom, EffectiveTo)
SELECT 
    drp.ProductID,
    4 AS BranchID,
    COALESCE(p.RecommendedSellingPrice, p.LastPaidPrice, 10) AS SellingPrice,
    COALESCE(p.AverageCost, p.LastPaidPrice, 5) AS CostPrice,
    GETDATE() AS EffectiveFrom,
    NULL AS EffectiveTo
FROM Demo_Retail_Product drp
INNER JOIN Products p ON p.ProductID = drp.ProductID
WHERE drp.IsActive = 1
  AND (drp.ProductType = 'External' OR drp.ProductType = 'Internal')
  AND NOT EXISTS (
      -- Only insert if price doesn't exist for BranchID 4
      SELECT 1 FROM Demo_Retail_Price 
      WHERE ProductID = drp.ProductID 
        AND BranchID = 4
  );

-- 3. Update existing zero prices for BranchID 4 from Products table
UPDATE drp4
SET 
    drp4.SellingPrice = COALESCE(p.RecommendedSellingPrice, p.LastPaidPrice, 10),
    drp4.CostPrice = COALESCE(p.AverageCost, p.LastPaidPrice, 5)
FROM Demo_Retail_Price drp4
INNER JOIN Demo_Retail_Product drp ON drp.ProductID = drp4.ProductID
INNER JOIN Products p ON p.ProductID = drp.ProductID
WHERE drp4.BranchID = 4
  AND drp.IsActive = 1
  AND (drp.ProductType = 'External' OR drp.ProductType = 'Internal')
  AND (drp4.SellingPrice = 0 OR drp4.SellingPrice IS NULL);

-- 4. Verify fix
SELECT 
    'After Fix - BranchID 4 Products with Prices' AS CheckType,
    COUNT(*) AS Count
FROM Demo_Retail_Price
WHERE BranchID = 4 AND SellingPrice > 0;

SELECT 
    'After Fix - BranchID 4 Products with Zero Prices' AS CheckType,
    COUNT(*) AS Count
FROM Demo_Retail_Price
WHERE BranchID = 4 AND (SellingPrice = 0 OR SellingPrice IS NULL);

-- 5. Show sample of fixed prices
SELECT TOP 10
    p.ProductCode,
    p.ProductName,
    drp.SellingPrice,
    drp.CostPrice,
    drp.BranchID
FROM Demo_Retail_Price drp
INNER JOIN Products p ON p.ProductID = drp.ProductID
WHERE drp.BranchID = 4
ORDER BY p.ProductName;
