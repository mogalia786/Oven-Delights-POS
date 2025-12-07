-- Debug: Check Candle products
-- 1. Check Candle category
SELECT CategoryID, CategoryName, IsActive, DisplayOrder 
FROM Categories 
WHERE CategoryName LIKE '%candle%';

-- 2. Count products in Candle category
SELECT 
    CategoryID,
    SubCategoryID,
    COUNT(*) AS ProductCount
FROM Demo_Retail_Product
WHERE CategoryID = 16 AND IsActive = 1
GROUP BY CategoryID, SubCategoryID;

-- 3. Show some Candle products
SELECT TOP 20
    ProductID,
    SKU,
    Name,
    Category,
    CategoryID,
    SubCategoryID,
    ProductType,
    IsActive
FROM Demo_Retail_Product
WHERE CategoryID = 16 AND IsActive = 1
ORDER BY Name;

-- 4. Check if products have NULL SubCategoryID
SELECT COUNT(*) AS ProductsWithNullSubCategory
FROM Demo_Retail_Product
WHERE CategoryID = 16 
  AND SubCategoryID IS NULL 
  AND IsActive = 1;
