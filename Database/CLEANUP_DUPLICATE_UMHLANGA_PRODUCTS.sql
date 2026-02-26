-- Clean up duplicate products created for BranchID=4 (Umhlanga)
-- Keep only the original products (BranchID=6) and use branch-specific prices

SET QUOTED_IDENTIFIER ON;

-- Step 1: Check current state
SELECT 'BEFORE CLEANUP' AS Status, BranchID, COUNT(*) AS ProductCount
FROM Demo_Retail_Product
WHERE IsActive = 1
GROUP BY BranchID
ORDER BY BranchID;

-- Step 2: Delete incorrect R0.00 prices for BranchID=4 products that were created during copy
-- These are prices linked to ProductIDs with BranchID=4 in Demo_Retail_Product
DELETE pr
FROM Demo_Retail_Price pr
INNER JOIN Demo_Retail_Product p ON pr.ProductID = p.ProductID
WHERE p.BranchID = 4
    AND pr.BranchID IN (1,3,4,5,8,9,10,11,12)  -- Delete all branch prices for these duplicate products
    AND pr.SellingPrice = 0;  -- Only delete R0.00 prices

PRINT 'Deleted ' + CAST(@@ROWCOUNT AS VARCHAR(10)) + ' incorrect R0.00 price records';

-- Step 3: Delete stock records for variants of BranchID=4 products
DELETE s
FROM Demo_Retail_Stock s
INNER JOIN Demo_Retail_Variant v ON s.VariantID = v.VariantID
INNER JOIN Demo_Retail_Product p ON v.ProductID = p.ProductID
WHERE p.BranchID = 4
    AND p.IsActive = 1;

PRINT 'Deleted ' + CAST(@@ROWCOUNT AS VARCHAR(10)) + ' stock records for duplicate products';

-- Step 4: Delete stock movements for variants of BranchID=4 products
DELETE sm
FROM Demo_Retail_StockMovements sm
INNER JOIN Demo_Retail_Variant v ON sm.VariantID = v.VariantID
INNER JOIN Demo_Retail_Product p ON v.ProductID = p.ProductID
WHERE p.BranchID = 4
    AND p.IsActive = 1;

PRINT 'Deleted ' + CAST(@@ROWCOUNT AS VARCHAR(10)) + ' stock movement records for duplicate products';

-- Step 5: Delete variants for BranchID=4 products
DELETE v
FROM Demo_Retail_Variant v
INNER JOIN Demo_Retail_Product p ON v.ProductID = p.ProductID
WHERE p.BranchID = 4
    AND p.IsActive = 1;

PRINT 'Deleted ' + CAST(@@ROWCOUNT AS VARCHAR(10)) + ' variant records for duplicate products';

-- Step 5: Delete the duplicate products with BranchID=4
-- Keep only the original products (BranchID=6)
DELETE FROM Demo_Retail_Product
WHERE BranchID = 4
    AND IsActive = 1;

PRINT 'Deleted ' + CAST(@@ROWCOUNT AS VARCHAR(10)) + ' duplicate Umhlanga products';

-- Step 4: Verify cleanup
SELECT 'AFTER CLEANUP' AS Status, BranchID, COUNT(*) AS ProductCount
FROM Demo_Retail_Product
WHERE IsActive = 1
GROUP BY BranchID
ORDER BY BranchID;

-- Step 5: Verify prices still exist for BranchID=4
SELECT 
    'Prices for BranchID=4' AS Description,
    COUNT(DISTINCT ProductID) AS ProductsWithPrices,
    MIN(SellingPrice) AS MinPrice,
    MAX(SellingPrice) AS MaxPrice
FROM Demo_Retail_Price
WHERE BranchID = 4
    AND SellingPrice > 0;

-- Step 6: Test CFC-MFD-EAC specifically
SELECT 'TEST CFC-MFD-EAC' AS Test,
    p.ProductID,
    p.SKU,
    p.Name,
    p.BranchID AS ProductBranchID,
    pr.SellingPrice,
    pr.BranchID AS PriceBranchID
FROM Demo_Retail_Product p
LEFT JOIN Demo_Retail_Price pr ON pr.ProductID = p.ProductID AND pr.BranchID = 4
WHERE p.SKU = 'CFC-MFD-EAC'
    AND p.IsActive = 1;

PRINT '';
PRINT '========================================';
PRINT 'Cleanup Complete!';
PRINT '========================================';
PRINT 'Removed duplicate BranchID=4 products';
PRINT 'POS will now use original products with branch-specific prices';
PRINT 'Umhlanga prices remain in Demo_Retail_Price table';
