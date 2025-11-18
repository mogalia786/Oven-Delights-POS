-- Debug: Check if ProductIDs match between Demo_Retail_Product and Demo_Retail_Price

-- 1. Check a specific product that shows R 0.00 in POS
-- Let's check "Apple Tart" (CEX-APP-EAC)
SELECT 
    'Demo_Retail_Product' AS TableName,
    ProductID,
    ProductCode,
    Name,
    IsActive,
    ProductType
FROM Demo_Retail_Product
WHERE ProductCode = 'CEX-APP-EAC';

-- 2. Check if this ProductID has a price for BranchID 4
SELECT 
    'Demo_Retail_Price for BranchID 4' AS TableName,
    ProductID,
    BranchID,
    SellingPrice,
    CostPrice,
    EffectiveFrom
FROM Demo_Retail_Price
WHERE ProductID = (SELECT ProductID FROM Demo_Retail_Product WHERE ProductCode = 'CEX-APP-EAC')
  AND BranchID = 4;

-- 3. Check if this ProductID has a price for BranchID 6
SELECT 
    'Demo_Retail_Price for BranchID 6' AS TableName,
    ProductID,
    BranchID,
    SellingPrice,
    CostPrice,
    EffectiveFrom
FROM Demo_Retail_Price
WHERE ProductID = (SELECT ProductID FROM Demo_Retail_Product WHERE ProductCode = 'CEX-APP-EAC')
  AND BranchID = 6;

-- 4. Run the EXACT query the POS uses for BranchID 4
DECLARE @BranchID INT = 4;
DECLARE @CategoryID INT = (SELECT CategoryID FROM Categories WHERE CategoryName = 'exotic cakes');
DECLARE @SubCategoryID INT = (SELECT SubCategoryID FROM SubCategories WHERE SubCategoryName = 'exotic cakes');

SELECT 
    p.ProductID,
    p.Code,
    p.ProductCode,
    p.Name AS ProductName,
    ISNULL(
        (SELECT TOP 1 SellingPrice FROM Demo_Retail_Price 
         WHERE ProductID = p.ProductID AND BranchID = @BranchID 
         ORDER BY EffectiveFrom DESC),
        (SELECT TOP 1 SellingPrice FROM Demo_Retail_Price 
         WHERE ProductID = p.ProductID AND BranchID IS NULL 
         ORDER BY EffectiveFrom DESC)
    ) AS SellingPrice
FROM Demo_Retail_Product p
INNER JOIN Categories c ON c.CategoryID = p.CategoryID
INNER JOIN SubCategories sc ON sc.SubCategoryID = p.SubCategoryID
WHERE p.CategoryID = @CategoryID
  AND p.SubCategoryID = @SubCategoryID
  AND p.IsActive = 1
  AND p.ProductCode IN ('CEX-APP-EAC', 'CEX-NUT-18', 'CEX-ATS-EAC')
ORDER BY p.Name;
