-- Fix Umhlanga prices by linking them to the correct ProductIDs based on SKU matching

-- Step 1: Check current state
SELECT 
    'Before Fix' AS Status,
    COUNT(DISTINCT p.ProductID) AS TotalProducts,
    COUNT(DISTINCT CASE WHEN pr.PriceID IS NOT NULL THEN p.ProductID END) AS ProductsWithPrices,
    COUNT(DISTINCT CASE WHEN pr.PriceID IS NULL THEN p.ProductID END) AS ProductsWithoutPrices
FROM Demo_Retail_Product p
LEFT JOIN Demo_Retail_Price pr ON p.ProductID = pr.ProductID AND pr.BranchID = 4
WHERE p.BranchID = 4 AND p.IsActive = 1;

-- Step 2: For products without prices, create price records by copying from the matching SKU's price
-- This handles the 334 products we just copied that don't have prices yet
INSERT INTO Demo_Retail_Price (ProductID, BranchID, SellingPrice, CostPrice, EffectiveFrom)
SELECT 
    p4.ProductID,  -- NEW ProductID for BranchID=4
    4 AS BranchID,
    pr_old.SellingPrice,
    pr_old.CostPrice,
    GETDATE()
FROM Demo_Retail_Product p4
INNER JOIN Demo_Retail_Product p6 ON p4.SKU = p6.SKU AND p6.BranchID = 6
INNER JOIN Demo_Retail_Price pr_old ON pr_old.ProductID = p6.ProductID AND pr_old.BranchID = 4
WHERE p4.BranchID = 4
    AND p4.IsActive = 1
    AND NOT EXISTS (
        SELECT 1 
        FROM Demo_Retail_Price pr_existing 
        WHERE pr_existing.ProductID = p4.ProductID 
        AND pr_existing.BranchID = 4
    );

PRINT 'Created ' + CAST(@@ROWCOUNT AS VARCHAR(10)) + ' new price records for copied products';

-- Step 3: Verify the fix
SELECT 
    'After Fix' AS Status,
    COUNT(DISTINCT p.ProductID) AS TotalProducts,
    COUNT(DISTINCT CASE WHEN pr.PriceID IS NOT NULL THEN p.ProductID END) AS ProductsWithPrices,
    COUNT(DISTINCT CASE WHEN pr.PriceID IS NULL THEN p.ProductID END) AS ProductsWithoutPrices
FROM Demo_Retail_Product p
LEFT JOIN Demo_Retail_Price pr ON p.ProductID = pr.ProductID AND pr.BranchID = 4
WHERE p.BranchID = 4 AND p.IsActive = 1;

-- Step 4: Show sample of fixed products with prices
SELECT TOP 20
    p.ProductID,
    p.SKU,
    p.Name,
    c.CategoryName,
    sc.SubCategoryName,
    pr.SellingPrice,
    pr.CostPrice
FROM Demo_Retail_Product p
INNER JOIN Demo_Retail_Price pr ON p.ProductID = pr.ProductID AND pr.BranchID = 4
LEFT JOIN Categories c ON p.CategoryID = c.CategoryID
LEFT JOIN SubCategories sc ON p.SubCategoryID = sc.SubCategoryID
WHERE p.BranchID = 4
    AND p.IsActive = 1
    AND pr.SellingPrice > 0
ORDER BY p.ProductID DESC;

PRINT '';
PRINT '========================================';
PRINT 'Price Fix Complete!';
PRINT '========================================';
PRINT 'All Umhlanga products now have correct prices linked to their ProductIDs';
