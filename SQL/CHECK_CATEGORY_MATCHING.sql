-- Check Category Matching Between Categories and Products
-- ========================================================

PRINT '1. Categories in Categories table:';
SELECT CategoryID, CategoryName
FROM Categories
WHERE IsActive = 1
ORDER BY CategoryName;

PRINT '';
PRINT '2. Unique categories in Products table:';
SELECT DISTINCT Category, COUNT(*) AS ProductCount
FROM Products
WHERE IsActive = 1
GROUP BY Category
ORDER BY Category;

PRINT '';
PRINT '3. Products with no matching category:';
SELECT p.ProductID, p.ProductName, p.Category
FROM Products p
LEFT JOIN Categories c ON p.Category = c.CategoryName
WHERE p.IsActive = 1 
AND c.CategoryID IS NULL
ORDER BY p.Category;

PRINT '';
PRINT '4. Sample products with their categories:';
SELECT TOP 20
    p.ProductID,
    p.ItemCode,
    p.ProductName,
    p.Category,
    c.CategoryName AS MatchingCategory
FROM Products p
LEFT JOIN Categories c ON p.Category = c.CategoryName
WHERE p.IsActive = 1
ORDER BY p.Category, p.ProductName;
