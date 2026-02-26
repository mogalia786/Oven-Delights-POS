-- Import OD400 (Umhlanga) prices from CSV file
-- This script updates BranchID=4 prices from the Excel export

-- Step 1: Create temporary table to hold CSV data
IF OBJECT_ID('tempdb..#TempUmhlangaPrices') IS NOT NULL
    DROP TABLE #TempUmhlangaPrices;

CREATE TABLE #TempUmhlangaPrices (
    ItemCode VARCHAR(50),
    Barcode VARCHAR(50),
    ItemDescription VARCHAR(255),
    Category VARCHAR(100),
    ItemCategory VARCHAR(100),
    Ingredients VARCHAR(MAX),
    Description VARCHAR(255),
    Whse VARCHAR(20),
    Cost DECIMAL(18,2),
    InclPrice DECIMAL(18,2)
);

-- Step 2: Import CSV data using BULK INSERT
-- NOTE: Update the file path if needed
BULK INSERT #TempUmhlangaPrices
FROM 'c:\Development Apps\Cascades projects\Overn-Delights-POS\Overn-Delights-POS\Database\OD400_Umhlanga_Prices.csv'
WITH (
    FIRSTROW = 2,
    FIELDTERMINATOR = ',',
    ROWTERMINATOR = '\n',
    TABLOCK,
    CODEPAGE = '65001'
);

PRINT 'Loaded ' + CAST(@@ROWCOUNT AS VARCHAR(10)) + ' rows from CSV';

-- Step 3: Show summary of imported data
SELECT 
    COUNT(*) AS TotalRows,
    COUNT(CASE WHEN InclPrice > 0 THEN 1 END) AS RowsWithPrices,
    MIN(InclPrice) AS MinPrice,
    MAX(InclPrice) AS MaxPrice,
    AVG(InclPrice) AS AvgPrice
FROM #TempUmhlangaPrices;

-- Step 4: Match products by SKU and update/insert prices for BranchID=4
DECLARE @UpdatedCount INT = 0;
DECLARE @InsertedCount INT = 0;
DECLARE @NotFoundCount INT = 0;

-- Update existing prices
WITH LatestUmhlangaPrices AS (
    SELECT 
        PriceID,
        ProductID,
        ROW_NUMBER() OVER (PARTITION BY ProductID ORDER BY EffectiveFrom DESC) AS rn
    FROM Demo_Retail_Price
    WHERE BranchID = 4
)
UPDATE drp
SET 
    drp.SellingPrice = tmp.InclPrice,
    drp.CostPrice = ISNULL(tmp.Cost, 0),
    drp.EffectiveFrom = GETDATE()
FROM Demo_Retail_Price drp
INNER JOIN LatestUmhlangaPrices lup ON drp.PriceID = lup.PriceID AND lup.rn = 1
INNER JOIN Demo_Retail_Product p ON drp.ProductID = p.ProductID
INNER JOIN #TempUmhlangaPrices tmp ON p.SKU = tmp.ItemCode
WHERE tmp.InclPrice > 0;

SET @UpdatedCount = @@ROWCOUNT;
PRINT 'Updated ' + CAST(@UpdatedCount AS VARCHAR(10)) + ' existing Umhlanga prices';

-- Insert new prices for products that don't have Umhlanga pricing yet
INSERT INTO Demo_Retail_Price (ProductID, BranchID, SellingPrice, CostPrice, EffectiveFrom)
SELECT 
    p.ProductID,
    4 AS BranchID,
    tmp.InclPrice,
    ISNULL(tmp.Cost, 0),
    GETDATE()
FROM #TempUmhlangaPrices tmp
INNER JOIN Demo_Retail_Product p ON p.SKU = tmp.ItemCode
WHERE tmp.InclPrice > 0
    AND p.IsActive = 1
    AND NOT EXISTS (
        SELECT 1 
        FROM Demo_Retail_Price drp 
        WHERE drp.ProductID = p.ProductID 
        AND drp.BranchID = 4
    );

SET @InsertedCount = @@ROWCOUNT;
PRINT 'Inserted ' + CAST(@InsertedCount AS VARCHAR(10)) + ' new Umhlanga prices';

-- Step 5: Report products in CSV that weren't found in database
SELECT 
    tmp.ItemCode,
    tmp.ItemDescription,
    tmp.InclPrice,
    tmp.Cost,
    'NOT FOUND IN DATABASE' AS Status
FROM #TempUmhlangaPrices tmp
LEFT JOIN Demo_Retail_Product p ON p.SKU = tmp.ItemCode
WHERE p.ProductID IS NULL
    AND tmp.InclPrice > 0;

SET @NotFoundCount = @@ROWCOUNT;
PRINT 'Products not found in database: ' + CAST(@NotFoundCount AS VARCHAR(10));

-- Step 6: Verify the import
SELECT 
    'Umhlanga (BranchID=4)' AS Branch,
    COUNT(DISTINCT drp.ProductID) AS TotalProducts,
    MIN(drp.SellingPrice) AS MinPrice,
    MAX(drp.SellingPrice) AS MaxPrice,
    AVG(drp.SellingPrice) AS AvgPrice
FROM Demo_Retail_Price drp
WHERE drp.BranchID = 4
    AND drp.SellingPrice > 0;

-- Step 7: Show sample of updated prices
SELECT TOP 20
    p.SKU,
    p.Name AS ProductName,
    drp.SellingPrice,
    drp.CostPrice,
    drp.EffectiveFrom
FROM Demo_Retail_Price drp
INNER JOIN Demo_Retail_Product p ON drp.ProductID = p.ProductID
WHERE drp.BranchID = 4
    AND drp.SellingPrice > 0
ORDER BY drp.EffectiveFrom DESC;

-- Cleanup
DROP TABLE #TempUmhlangaPrices;

PRINT '';
PRINT '========================================';
PRINT 'OD400 (Umhlanga) Price Import Complete!';
PRINT '========================================';
PRINT 'Updated: ' + CAST(@UpdatedCount AS VARCHAR(10));
PRINT 'Inserted: ' + CAST(@InsertedCount AS VARCHAR(10));
PRINT 'Not Found: ' + CAST(@NotFoundCount AS VARCHAR(10));
