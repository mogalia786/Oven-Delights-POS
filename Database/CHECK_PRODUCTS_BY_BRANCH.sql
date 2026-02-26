-- Check product distribution by BranchID

-- Count products per branch
SELECT 
    BranchID,
    COUNT(*) AS ProductCount
FROM Demo_Retail_Product
WHERE IsActive = 1
GROUP BY BranchID
ORDER BY BranchID;

-- Check if freshcream products exist for BranchID 4 vs 6
SELECT 
    'BranchID 4 (Umhlanga)' AS Branch,
    COUNT(*) AS ProductCount
FROM Demo_Retail_Product p
INNER JOIN SubCategories sc ON p.SubCategoryID = sc.SubCategoryID
WHERE p.BranchID = 4 
    AND p.IsActive = 1
    AND sc.SubCategoryName LIKE '%fresh%cream%';

SELECT 
    'BranchID 6 (Ayesha Centre)' AS Branch,
    COUNT(*) AS ProductCount
FROM Demo_Retail_Product p
INNER JOIN SubCategories sc ON p.SubCategoryID = sc.SubCategoryID
WHERE p.BranchID = 6 
    AND p.IsActive = 1
    AND sc.SubCategoryName LIKE '%fresh%cream%';

-- Show sample products for each branch
SELECT TOP 10
    ProductID,
    SKU,
    Name,
    BranchID,
    CategoryID,
    SubCategoryID
FROM Demo_Retail_Product
WHERE BranchID = 4 AND IsActive = 1
ORDER BY Name;

SELECT TOP 10
    ProductID,
    SKU,
    Name,
    BranchID,
    CategoryID,
    SubCategoryID
FROM Demo_Retail_Product
WHERE BranchID = 6 AND IsActive = 1
ORDER BY Name;
