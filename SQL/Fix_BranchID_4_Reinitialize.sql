-- =============================================
-- Fix BranchID 4 - Delete old prices and reinitialize
-- =============================================

-- STEP 0: First run the updated sp_CreateNewBranchWithProducts.sql from ERP project!

-- 1. Backup current prices (just in case)
IF OBJECT_ID('Demo_Retail_Price_BranchID4_Backup', 'U') IS NOT NULL
    DROP TABLE Demo_Retail_Price_BranchID4_Backup;

SELECT * 
INTO Demo_Retail_Price_BranchID4_Backup
FROM Demo_Retail_Price
WHERE BranchID = 4;

-- 2. Delete old/incorrect prices for BranchID 4
DELETE FROM Demo_Retail_Price
WHERE BranchID = 4;

-- 3. Delete old stock records for BranchID 4
DELETE FROM RetailStock
WHERE BranchID = 4;

-- 4. Re-initialize BranchID 4 with correct ProductIDs
EXEC sp_InitializeBranchProducts @BranchID = 4;

-- 5. Verify fix
SELECT 
    'After Reinitialize - BranchID 4 Products with Prices' AS CheckType,
    COUNT(*) AS Count
FROM Demo_Retail_Price
WHERE BranchID = 4 AND SellingPrice > 0;

-- 6. Check sample products
SELECT TOP 10
    drp.ProductID,
    p.ProductCode,
    p.Name,
    drp.SellingPrice,
    drp.BranchID
FROM Demo_Retail_Price drp
INNER JOIN Demo_Retail_Product p ON p.ProductID = drp.ProductID
WHERE drp.BranchID = 4
ORDER BY p.Name;
