-- =============================================
-- Copy prices from BranchID 6 to BranchID 4
-- Since BranchID 6 works, just copy its prices
-- =============================================

-- 1. Delete existing prices for BranchID 4 (already done)
-- Already empty from previous script

-- 2. Copy prices from BranchID 6 to BranchID 4
INSERT INTO Demo_Retail_Price (ProductID, BranchID, SellingPrice, CostPrice, Currency, EffectiveFrom, EffectiveTo, CreatedAt, SellingPriceExVAT)
SELECT 
    ProductID,
    4 AS BranchID,  -- Change to BranchID 4
    SellingPrice,
    CostPrice,
    Currency,
    EffectiveFrom,
    EffectiveTo,
    GETDATE() AS CreatedAt,
    SellingPriceExVAT
FROM Demo_Retail_Price
WHERE BranchID = 6;

-- 3. Verify
SELECT 
    'BranchID 4 - Products with Prices' AS CheckType,
    COUNT(*) AS Count
FROM Demo_Retail_Price
WHERE BranchID = 4 AND SellingPrice > 0;

-- 4. Show sample
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
