-- =============================================
-- Diagnose Missing Prices for Specific Products
-- =============================================

-- Check BD Fruit Cake products specifically
PRINT 'BD Fruit Cake Products in Demo_Retail_Product:'
SELECT 
    p.ProductID,
    p.SKU,
    p.Name,
    p.BranchID,
    p.CategoryID,
    p.SubCategoryID,
    p.IsActive
FROM Demo_Retail_Product p
WHERE p.Name LIKE '%BD%Fruit%Cake%'
ORDER BY p.Name, p.BranchID

PRINT ''
PRINT '========================================'
PRINT ''

-- Check if prices exist for these products
PRINT 'Prices for BD Fruit Cake Products in Demo_Retail_Price:'
SELECT 
    p.ProductID,
    p.Name,
    p.BranchID AS Product_BranchID,
    pr.PriceID,
    pr.BranchID AS Price_BranchID,
    pr.SellingPrice,
    pr.CostPrice,
    pr.EffectiveFrom
FROM Demo_Retail_Product p
LEFT JOIN Demo_Retail_Price pr ON p.ProductID = pr.ProductID
WHERE p.Name LIKE '%BD%Fruit%Cake%'
ORDER BY p.Name, p.BranchID, pr.BranchID

PRINT ''
PRINT '========================================'
PRINT ''

-- Check what the ProductCacheService query returns
DECLARE @TestBranchID INT = 6 -- Change this to your actual branch ID

PRINT 'ProductCacheService Query Result for BranchID ' + CAST(@TestBranchID AS NVARCHAR(10)) + ':'
SELECT 
    p.ProductID,
    p.SKU,
    p.Name,
    p.BranchID AS Product_BranchID,
    ISNULL(pr.SellingPrice, 0) AS SellingPrice,
    ISNULL(pr.CostPrice, 0) AS CostPrice,
    pr.BranchID AS Price_BranchID
FROM Demo_Retail_Product p
LEFT JOIN Demo_Retail_Price pr ON p.ProductID = pr.ProductID AND pr.BranchID = @TestBranchID
WHERE p.IsActive = 1 
  AND p.BranchID = @TestBranchID
  AND p.Name LIKE '%BD%Fruit%Cake%'
ORDER BY p.Name

PRINT ''
PRINT '========================================'
PRINT ''

-- Find products with missing prices
PRINT 'Products with NO price records in Demo_Retail_Price:'
SELECT 
    p.ProductID,
    p.SKU,
    p.Name,
    p.BranchID
FROM Demo_Retail_Product p
WHERE p.IsActive = 1
  AND NOT EXISTS (
      SELECT 1 
      FROM Demo_Retail_Price pr 
      WHERE pr.ProductID = p.ProductID 
        AND pr.BranchID = p.BranchID
  )
ORDER BY p.Name
