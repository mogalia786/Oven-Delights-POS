-- =============================================
-- Check if BranchID 4 was properly initialized
-- =============================================

-- 1. Check if branch exists
SELECT 
    BranchID,
    BranchName,
    BranchCode,
    IsActive,
    CreatedDate
FROM Branches
WHERE BranchID = 4;

-- 2. Check if prices exist for BranchID 4
SELECT 
    'Demo_Retail_Price records for BranchID 4' AS CheckType,
    COUNT(*) AS RecordCount,
    COUNT(CASE WHEN SellingPrice > 0 THEN 1 END) AS WithPrices,
    COUNT(CASE WHEN SellingPrice = 0 OR SellingPrice IS NULL THEN 1 END) AS WithoutPrices
FROM Demo_Retail_Price
WHERE BranchID = 4;

-- 3. Check if stock records exist for BranchID 4
SELECT 
    'RetailStock records for BranchID 4' AS CheckType,
    COUNT(*) AS RecordCount
FROM RetailStock
WHERE BranchID = 4;

-- 4. Check total products in system
SELECT 
    'Total Active Products' AS CheckType,
    COUNT(*) AS RecordCount
FROM Products
WHERE IsActive = 1;

-- 5. Check if stored procedure exists
SELECT 
    'sp_InitializeBranchProducts exists' AS CheckType,
    COUNT(*) AS Exists
FROM sys.procedures
WHERE name = 'sp_InitializeBranchProducts';

-- CONCLUSION:
-- If BranchID 4 has 0 price records, it was created BEFORE the stored procedure was implemented
-- Solution: Run Fix_BranchID_4_Prices.sql ONCE to backfill the data
-- All NEW branches will automatically get prices via sp_InitializeBranchProducts
