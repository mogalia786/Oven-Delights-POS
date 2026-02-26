-- Copy all products from BranchID=6 (Ayesha Centre) to BranchID=4 (Umhlanga)
-- This will make all products available in Umhlanga POS

-- Step 1: Check current state
SELECT 'Before Copy' AS Status, BranchID, COUNT(*) AS ProductCount
FROM Demo_Retail_Product
WHERE BranchID IN (4, 6) AND IsActive = 1
GROUP BY BranchID;

-- Step 2: Insert products from BranchID=6 to BranchID=4 that don't already exist
-- Match by SKU to avoid duplicates
INSERT INTO Demo_Retail_Product (
    SKU,
    Barcode,
    Name,
    CategoryID,
    SubCategoryID,
    ProductType,
    BranchID,
    IsActive,
    Code,
    ProductCode
)
SELECT 
    p6.SKU,
    p6.Barcode,
    p6.Name,
    p6.CategoryID,
    p6.SubCategoryID,
    p6.ProductType,
    4 AS BranchID,  -- Umhlanga
    1 AS IsActive,
    p6.Code,
    p6.ProductCode
FROM Demo_Retail_Product p6
WHERE p6.BranchID = 6
    AND p6.IsActive = 1
    AND NOT EXISTS (
        SELECT 1 
        FROM Demo_Retail_Product p4 
        WHERE p4.SKU = p6.SKU 
        AND p4.BranchID = 4
    );

PRINT 'Inserted ' + CAST(@@ROWCOUNT AS VARCHAR(10)) + ' products for Umhlanga';

-- Step 3: Verify the copy
SELECT 'After Copy' AS Status, BranchID, COUNT(*) AS ProductCount
FROM Demo_Retail_Product
WHERE BranchID IN (4, 6) AND IsActive = 1
GROUP BY BranchID;

-- Step 4: Check freshcream products specifically
SELECT 
    'Umhlanga freshcream products' AS Description,
    COUNT(*) AS ProductCount
FROM Demo_Retail_Product p
INNER JOIN SubCategories sc ON p.SubCategoryID = sc.SubCategoryID
WHERE p.BranchID = 4 
    AND p.IsActive = 1
    AND sc.SubCategoryName LIKE '%fresh%cream%';

-- Step 5: Show sample of copied products
SELECT TOP 20
    p.ProductID,
    p.SKU,
    p.Name,
    p.BranchID,
    c.CategoryName,
    sc.SubCategoryName
FROM Demo_Retail_Product p
LEFT JOIN Categories c ON p.CategoryID = c.CategoryID
LEFT JOIN SubCategories sc ON p.SubCategoryID = sc.SubCategoryID
WHERE p.BranchID = 4
    AND p.IsActive = 1
ORDER BY p.ProductID DESC;

PRINT '';
PRINT '========================================';
PRINT 'Product Copy Complete!';
PRINT '========================================';
PRINT 'Umhlanga (BranchID=4) now has all products from Ayesha Centre (BranchID=6)';
PRINT 'Prices for these products already exist in Demo_Retail_Price';
PRINT 'Products should now appear in POS categories for Umhlanga';
