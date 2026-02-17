-- Find duplicate ProductCodes in Demo_Retail_Product

SELECT 
    ProductCode,
    COUNT(*) AS DuplicateCount,
    STRING_AGG(CAST(ProductID AS VARCHAR), ', ') AS ProductIDs
FROM Demo_Retail_Product
WHERE IsActive = 1
GROUP BY ProductCode
HAVING COUNT(*) > 1
ORDER BY DuplicateCount DESC;

-- Find duplicate ProductIDs in Demo_Retail_Price for BranchID 4
SELECT 
    ProductID,
    BranchID,
    COUNT(*) AS DuplicateCount,
    STRING_AGG(CAST(PriceID AS VARCHAR), ', ') AS PriceIDs
FROM Demo_Retail_Price
WHERE BranchID = 4
GROUP BY ProductID, BranchID
HAVING COUNT(*) > 1
ORDER BY DuplicateCount DESC;

-- Check if Demo_Retail_Product has duplicate ProductIDs
SELECT 
    ProductID,
    COUNT(*) AS DuplicateCount,
    STRING_AGG(ProductCode, ', ') AS ProductCodes
FROM Demo_Retail_Product
WHERE IsActive = 1
GROUP BY ProductID
HAVING COUNT(*) > 1;
