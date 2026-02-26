-- Fix NULL categories for Umhlanga products by copying category assignments from Ayesha Centre

-- Step 1: Check how many products have NULL categories
SELECT 
    'Products with NULL categories' AS Issue,
    BranchID,
    COUNT(*) AS ProductCount
FROM Demo_Retail_Product
WHERE BranchID IN (4, 6)
    AND IsActive = 1
    AND (CategoryID IS NULL OR SubCategoryID IS NULL)
GROUP BY BranchID;

-- Step 2: Update Umhlanga products to have same categories as matching Ayesha products (by SKU)
UPDATE p4
SET 
    p4.CategoryID = p6.CategoryID,
    p4.SubCategoryID = p6.SubCategoryID
FROM Demo_Retail_Product p4
INNER JOIN Demo_Retail_Product p6 ON p4.SKU = p6.SKU AND p6.BranchID = 6
WHERE p4.BranchID = 4
    AND p4.IsActive = 1
    AND p6.IsActive = 1
    AND (p4.CategoryID IS NULL OR p4.SubCategoryID IS NULL)
    AND p6.CategoryID IS NOT NULL
    AND p6.SubCategoryID IS NOT NULL;

PRINT 'Updated ' + CAST(@@ROWCOUNT AS VARCHAR(10)) + ' products with categories from Ayesha Centre';

-- Step 3: Verify the fix
SELECT 
    'After Fix - Products with NULL categories' AS Status,
    BranchID,
    COUNT(*) AS ProductCount
FROM Demo_Retail_Product
WHERE BranchID IN (4, 6)
    AND IsActive = 1
    AND (CategoryID IS NULL OR SubCategoryID IS NULL)
GROUP BY BranchID;

-- Step 4: Check freshcream products specifically
SELECT 
    'Umhlanga freshcream products with categories' AS Description,
    COUNT(*) AS ProductCount
FROM Demo_Retail_Product p
INNER JOIN SubCategories sc ON p.SubCategoryID = sc.SubCategoryID
WHERE p.BranchID = 4 
    AND p.IsActive = 1
    AND sc.SubCategoryName LIKE '%fresh%cream%';

-- Step 5: Show sample of fixed products
SELECT TOP 20
    p.ProductID,
    p.SKU,
    p.Name,
    p.BranchID,
    c.CategoryName,
    sc.SubCategoryName,
    pr.SellingPrice
FROM Demo_Retail_Product p
LEFT JOIN Categories c ON p.CategoryID = c.CategoryID
LEFT JOIN SubCategories sc ON p.SubCategoryID = sc.SubCategoryID
LEFT JOIN Demo_Retail_Price pr ON p.ProductID = pr.ProductID AND pr.BranchID = 4
WHERE p.BranchID = 4
    AND p.IsActive = 1
    AND p.CategoryID IS NOT NULL
    AND p.SubCategoryID IS NOT NULL
ORDER BY p.ProductID DESC;

PRINT '';
PRINT '========================================';
PRINT 'Category Assignment Complete!';
PRINT '========================================';
PRINT 'Umhlanga products now have proper category assignments';
PRINT 'Products should now appear in POS categories';
