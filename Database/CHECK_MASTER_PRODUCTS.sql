-- Check which products are the "master" products

-- Count products by BranchID
SELECT 
    CASE 
        WHEN BranchID IS NULL THEN 'NULL (Master)'
        WHEN BranchID = 6 THEN '6 (Ayesha Centre)'
        WHEN BranchID = 4 THEN '4 (Umhlanga)'
        ELSE CAST(BranchID AS VARCHAR)
    END AS BranchType,
    COUNT(*) AS ProductCount,
    COUNT(DISTINCT CategoryID) AS Categories,
    COUNT(DISTINCT SubCategoryID) AS Subcategories
FROM Demo_Retail_Product
WHERE IsActive = 1
GROUP BY BranchID
ORDER BY BranchID;

-- Check if BranchID=6 products have variants
SELECT 
    'BranchID=6 with variants' AS CheckType,
    COUNT(DISTINCT p.ProductID) AS ProductsWithVariants
FROM Demo_Retail_Product p
INNER JOIN Demo_Retail_Variant v ON p.ProductID = v.ProductID
WHERE p.BranchID = 6 AND p.IsActive = 1;

-- Check if BranchID=NULL products exist and have variants
SELECT 
    'BranchID=NULL with variants' AS CheckType,
    COUNT(DISTINCT p.ProductID) AS ProductsWithVariants
FROM Demo_Retail_Product p
INNER JOIN Demo_Retail_Variant v ON p.ProductID = v.ProductID
WHERE p.BranchID IS NULL AND p.IsActive = 1;

-- Sample of master products
SELECT TOP 10
    p.ProductID,
    p.SKU,
    p.Name,
    p.BranchID,
    v.VariantID,
    pr4.SellingPrice AS UmhlangaPrice,
    pr6.SellingPrice AS AyeshaPrice
FROM Demo_Retail_Product p
LEFT JOIN Demo_Retail_Variant v ON p.ProductID = v.ProductID
LEFT JOIN Demo_Retail_Price pr4 ON p.ProductID = pr4.ProductID AND pr4.BranchID = 4
LEFT JOIN Demo_Retail_Price pr6 ON p.ProductID = pr6.ProductID AND pr6.BranchID = 6
WHERE p.BranchID = 6 AND p.IsActive = 1
ORDER BY p.ProductID;
