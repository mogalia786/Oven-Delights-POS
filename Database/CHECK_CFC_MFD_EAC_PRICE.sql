-- Check why CFC-MFD-EAC shows R0.00 in Umhlanga POS

-- Step 1: Find the product
SELECT 'PRODUCT INFO' AS Section, ProductID, SKU, Name, BranchID, CategoryID, SubCategoryID, IsActive
FROM Demo_Retail_Product
WHERE SKU = 'CFC-MFD-EAC'
ORDER BY BranchID;

-- Step 2: Check all price records for this product
SELECT 'ALL PRICES' AS Section, 
    pr.PriceID,
    pr.ProductID,
    pr.BranchID,
    pr.SellingPrice,
    pr.CostPrice,
    pr.EffectiveFrom,
    pr.EffectiveTo,
    p.SKU,
    p.BranchID AS ProductBranchID
FROM Demo_Retail_Price pr
INNER JOIN Demo_Retail_Product p ON pr.ProductID = p.ProductID
WHERE p.SKU = 'CFC-MFD-EAC'
ORDER BY pr.BranchID, pr.EffectiveFrom DESC;

-- Step 3: Check what the POS query would return (simulating CategoryNavigationService)
SELECT 'POS QUERY RESULT' AS Section,
    p.ProductID,
    p.SKU,
    p.Name,
    p.BranchID AS ProductBranchID,
    price.PriceID,
    price.BranchID AS PriceBranchID,
    price.SellingPrice,
    price.EffectiveFrom
FROM Demo_Retail_Product p
LEFT JOIN Demo_Retail_Price price ON price.ProductID = p.ProductID AND price.BranchID = 4
WHERE p.SKU = 'CFC-MFD-EAC'
    AND p.IsActive = 1
ORDER BY p.ProductID;

-- Step 4: Check for latest price only
SELECT 'LATEST PRICE PER PRODUCT' AS Section,
    p.ProductID,
    p.SKU,
    p.Name,
    p.BranchID AS ProductBranchID,
    latest_price.SellingPrice,
    latest_price.EffectiveFrom
FROM Demo_Retail_Product p
LEFT JOIN (
    SELECT ProductID, BranchID, SellingPrice, EffectiveFrom,
           ROW_NUMBER() OVER (PARTITION BY ProductID, BranchID ORDER BY EffectiveFrom DESC) AS rn
    FROM Demo_Retail_Price
    WHERE BranchID = 4
) latest_price ON latest_price.ProductID = p.ProductID AND latest_price.rn = 1
WHERE p.SKU = 'CFC-MFD-EAC'
    AND p.IsActive = 1
ORDER BY p.ProductID;
