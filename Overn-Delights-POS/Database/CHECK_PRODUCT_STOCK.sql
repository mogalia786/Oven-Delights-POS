-- Check stock for "Bar One Swissroll Slice" product
DECLARE @ProductName NVARCHAR(255) = 'Bar One Swissroll Slice'
DECLARE @BranchID INT = 1

-- 1. Product Info
SELECT 'PRODUCT' AS Section, ProductID, SKU, Name, ProductType, Category
FROM Demo_Retail_Product 
WHERE Name LIKE '%' + @ProductName + '%'

-- 2. Variants
SELECT 'VARIANTS' AS Section, VariantID, ProductID, IsActive
FROM Demo_Retail_Variant
WHERE ProductID IN (SELECT ProductID FROM Demo_Retail_Product WHERE Name LIKE '%' + @ProductName + '%')

-- 3. Retail_Stock (Manufacturing updates here)
SELECT 'RETAIL_STOCK' AS Section, rs.*, p.Name
FROM Retail_Stock rs
INNER JOIN Demo_Retail_Variant v ON v.VariantID = rs.VariantID
INNER JOIN Demo_Retail_Product p ON p.ProductID = v.ProductID
WHERE p.Name LIKE '%' + @ProductName + '%'

-- 4. RetailStock (Old table)
SELECT 'RETAILSTOCK' AS Section, rs.*, p.Name
FROM RetailStock rs
INNER JOIN Demo_Retail_Product p ON p.ProductID = rs.ProductID
WHERE p.Name LIKE '%' + @ProductName + '%'

-- 5. Demo_Retail_Stock (Check if manufacturing wrote here)
SELECT 'DEMO_RETAIL_STOCK' AS Section, drs.*, p.Name
FROM Demo_Retail_Stock drs
INNER JOIN Demo_Retail_Variant v ON v.VariantID = drs.VariantID
INNER JOIN Demo_Retail_Product p ON p.ProductID = v.ProductID
WHERE p.Name LIKE '%' + @ProductName + '%'

-- 6. POS Query Result
SELECT 'POS_SEES' AS Section,
    p.ProductID,
    p.SKU,
    p.Name,
    v.VariantID,
    ISNULL(s.QtyOnHand, 0) AS QtyOnHand,
    s.BranchID
FROM Demo_Retail_Product p
LEFT JOIN Demo_Retail_Variant v ON v.ProductID = p.ProductID AND v.IsActive = 1
LEFT JOIN Retail_Stock s ON s.VariantID = v.VariantID AND s.BranchID = @BranchID
WHERE p.Name LIKE '%' + @ProductName + '%'
