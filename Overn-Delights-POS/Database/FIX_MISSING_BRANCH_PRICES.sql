-- Fix missing prices for products in their assigned branches
-- Products have BranchID set but no corresponding price record

PRINT 'Finding products without prices in their assigned branches...'

-- Insert missing prices for products that have a BranchID but no price for that branch
INSERT INTO Demo_Retail_Price (ProductID, BranchID, SellingPrice, CostPrice, EffectiveFrom)
SELECT 
    p.ProductID,
    p.BranchID,
    10.00 AS SellingPrice,  -- Default price
    0.00 AS CostPrice,
    GETDATE() AS EffectiveFrom
FROM Demo_Retail_Product p
WHERE p.IsActive = 1
AND p.BranchID IS NOT NULL
AND NOT EXISTS (
    SELECT 1 FROM Demo_Retail_Price pr
    WHERE pr.ProductID = p.ProductID 
    AND pr.BranchID = p.BranchID
)

PRINT 'Added ' + CAST(@@ROWCOUNT AS VARCHAR) + ' missing branch-specific prices'

-- Verify Bonaqua now has prices
PRINT ''
PRINT 'Bonaqua products in Branch 6:'
SELECT 
    p.SKU,
    p.Name,
    pr.BranchID,
    pr.SellingPrice
FROM Demo_Retail_Product p
INNER JOIN Demo_Retail_Price pr ON pr.ProductID = p.ProductID AND pr.BranchID = 6
WHERE p.Name LIKE '%Bonaqua%'

PRINT ''
PRINT '✓ All products now have prices in their assigned branches!'
