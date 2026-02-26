-- Update Umhlanga (BranchID=4) prices from master branch (BranchID=1)
-- This script copies all selling prices and cost prices from master to Umhlanga

-- Step 1: Check current price discrepancies between master and Umhlanga (latest prices only)
WITH LatestMasterPrices AS (
    SELECT 
        ProductID,
        SellingPrice,
        CostPrice,
        ROW_NUMBER() OVER (PARTITION BY ProductID ORDER BY EffectiveFrom DESC) AS rn
    FROM Demo_Retail_Price
    WHERE BranchID = 1
),
LatestUmhlangaPrices AS (
    SELECT 
        ProductID,
        SellingPrice,
        CostPrice,
        ROW_NUMBER() OVER (PARTITION BY ProductID ORDER BY EffectiveFrom DESC) AS rn
    FROM Demo_Retail_Price
    WHERE BranchID = 4
)
SELECT 
    p.Name AS ProductName,
    p.SKU,
    master.SellingPrice AS MasterPrice,
    umhlanga.SellingPrice AS UmhlangaPrice,
    master.CostPrice AS MasterCost,
    umhlanga.CostPrice AS UmhlangaCost,
    CASE 
        WHEN umhlanga.SellingPrice IS NULL THEN 'MISSING IN UMHLANGA'
        WHEN master.SellingPrice <> umhlanga.SellingPrice THEN 'PRICE MISMATCH'
        WHEN master.CostPrice <> umhlanga.CostPrice THEN 'COST MISMATCH'
        ELSE 'OK'
    END AS Status
FROM Demo_Retail_Product p
LEFT JOIN LatestMasterPrices master ON p.ProductID = master.ProductID AND master.rn = 1
LEFT JOIN LatestUmhlangaPrices umhlanga ON p.ProductID = umhlanga.ProductID AND umhlanga.rn = 1
WHERE p.IsActive = 1
    AND (master.SellingPrice <> umhlanga.SellingPrice OR master.CostPrice <> umhlanga.CostPrice OR umhlanga.SellingPrice IS NULL)
ORDER BY p.Name;

-- Step 2: Update existing Umhlanga prices from master (latest records only)
WITH LatestMaster AS (
    SELECT 
        ProductID,
        SellingPrice,
        CostPrice,
        ROW_NUMBER() OVER (PARTITION BY ProductID ORDER BY EffectiveFrom DESC) AS rn
    FROM Demo_Retail_Price
    WHERE BranchID = 1
),
LatestUmhlanga AS (
    SELECT 
        PriceID,
        ProductID,
        SellingPrice,
        CostPrice,
        ROW_NUMBER() OVER (PARTITION BY ProductID ORDER BY EffectiveFrom DESC) AS rn
    FROM Demo_Retail_Price
    WHERE BranchID = 4
)
UPDATE u
SET 
    u.SellingPrice = m.SellingPrice,
    u.CostPrice = m.CostPrice,
    u.EffectiveFrom = GETDATE()
FROM Demo_Retail_Price u
INNER JOIN LatestUmhlanga lu ON u.PriceID = lu.PriceID AND lu.rn = 1
INNER JOIN LatestMaster m ON lu.ProductID = m.ProductID AND m.rn = 1
WHERE (u.SellingPrice <> m.SellingPrice OR u.CostPrice <> m.CostPrice);

PRINT 'Updated ' + CAST(@@ROWCOUNT AS VARCHAR(10)) + ' existing Umhlanga prices from master';

-- Step 3: Insert missing products for Umhlanga from master (latest prices only)
WITH LatestMaster AS (
    SELECT 
        ProductID,
        SellingPrice,
        CostPrice,
        ROW_NUMBER() OVER (PARTITION BY ProductID ORDER BY EffectiveFrom DESC) AS rn
    FROM Demo_Retail_Price
    WHERE BranchID = 1
)
INSERT INTO Demo_Retail_Price (ProductID, BranchID, SellingPrice, CostPrice, EffectiveFrom)
SELECT 
    m.ProductID,
    4 AS BranchID,
    m.SellingPrice,
    m.CostPrice,
    GETDATE()
FROM LatestMaster m
INNER JOIN Demo_Retail_Product p ON m.ProductID = p.ProductID
WHERE m.rn = 1
    AND p.IsActive = 1
    AND NOT EXISTS (
        SELECT 1 
        FROM Demo_Retail_Price umhlanga 
        WHERE umhlanga.ProductID = m.ProductID 
        AND umhlanga.BranchID = 4
    );

PRINT 'Inserted ' + CAST(@@ROWCOUNT AS VARCHAR(10)) + ' missing products for Umhlanga';

-- Step 4: Verify the update
SELECT 
    COUNT(*) AS TotalProducts,
    COUNT(CASE WHEN master.SellingPrice = umhlanga.SellingPrice THEN 1 END) AS MatchingPrices,
    COUNT(CASE WHEN master.SellingPrice <> umhlanga.SellingPrice THEN 1 END) AS MismatchedPrices
FROM Demo_Retail_Product p
INNER JOIN Demo_Retail_Price master ON p.ProductID = master.ProductID AND master.BranchID = 1
INNER JOIN Demo_Retail_Price umhlanga ON p.ProductID = umhlanga.ProductID AND umhlanga.BranchID = 4
WHERE p.IsActive = 1;

PRINT 'Price sync completed for Umhlanga branch';
