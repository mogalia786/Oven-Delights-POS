-- Diagnose why prices aren't showing for Umhlanga products

-- Step 1: Check if prices exist for the NEW ProductIDs (created during copy)
SELECT 
    'Prices for NEW Umhlanga ProductIDs' AS Description,
    COUNT(DISTINCT pr.ProductID) AS ProductsWithPrices
FROM Demo_Retail_Price pr
INNER JOIN Demo_Retail_Product p ON pr.ProductID = p.ProductID
WHERE pr.BranchID = 4
    AND p.BranchID = 4
    AND p.IsActive = 1;

-- Step 2: Check if prices exist for OLD ProductIDs (before copy)
SELECT 
    'Prices for OLD ProductIDs (BranchID=4 in price table)' AS Description,
    COUNT(DISTINCT ProductID) AS ProductsWithPrices
FROM Demo_Retail_Price
WHERE BranchID = 4;

-- Step 3: Show sample of products WITHOUT prices
SELECT TOP 20
    p.ProductID,
    p.SKU,
    p.Name,
    p.BranchID AS ProductBranchID,
    pr.PriceID,
    pr.SellingPrice,
    pr.BranchID AS PriceBranchID
FROM Demo_Retail_Product p
LEFT JOIN Demo_Retail_Price pr ON p.ProductID = pr.ProductID AND pr.BranchID = 4
WHERE p.BranchID = 4
    AND p.IsActive = 1
    AND pr.PriceID IS NULL
ORDER BY p.ProductID DESC;

-- Step 4: Check if there are prices for the same SKU but different ProductID
SELECT TOP 20
    p4.ProductID AS NewProductID,
    p4.SKU,
    p4.Name,
    p4.BranchID AS NewProductBranch,
    pr.ProductID AS OldProductID,
    pr.SellingPrice,
    pr.BranchID AS PriceBranch
FROM Demo_Retail_Product p4
LEFT JOIN Demo_Retail_Price pr ON pr.BranchID = 4
LEFT JOIN Demo_Retail_Product p_old ON pr.ProductID = p_old.ProductID AND p_old.SKU = p4.SKU
WHERE p4.BranchID = 4
    AND p4.IsActive = 1
    AND pr.ProductID != p4.ProductID
    AND p_old.ProductID IS NOT NULL
ORDER BY p4.ProductID DESC;

PRINT 'Diagnosis complete - check results to understand price mismatch';
