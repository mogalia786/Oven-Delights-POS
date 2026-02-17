-- Check if ProductID 56073 (Apple Tart) has prices

-- 1. Check Demo_Retail_Price for ProductID 56073, BranchID 4
SELECT 
    'ProductID 56073, BranchID 4' AS Info,
    *
FROM Demo_Retail_Price
WHERE ProductID = 56073 AND BranchID = 4;

-- 2. Check Demo_Retail_Price for ProductID 56073, ANY BranchID
SELECT 
    'ProductID 56073, ANY BranchID' AS Info,
    *
FROM Demo_Retail_Price
WHERE ProductID = 56073;

-- 3. Check what ProductIDs DO have prices for BranchID 4
SELECT TOP 10
    'BranchID 4 - Sample ProductIDs with prices' AS Info,
    ProductID,
    SellingPrice,
    BranchID
FROM Demo_Retail_Price
WHERE BranchID = 4 AND SellingPrice > 0
ORDER BY ProductID;

-- 4. Check if ProductID 56073 exists in Products table
SELECT 
    'ProductID 56073 in Products table' AS Info,
    ProductID,
    ProductCode,
    ProductName,
    RecommendedSellingPrice,
    LastPaidPrice
FROM Products
WHERE ProductID = 56073;
