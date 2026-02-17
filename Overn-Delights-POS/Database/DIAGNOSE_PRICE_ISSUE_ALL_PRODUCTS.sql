-- Diagnose if price issue affects all products or just some
-- Check if prices in Demo_Retail_Price are VAT-exclusive (need to multiply by 1.15) or VAT-inclusive

-- Sample 20 products to see the pattern
SELECT TOP 20
    p.ProductID,
    p.SKU,
    p.Name,
    p.Barcode,
    pr.SellingPrice AS CurrentPrice,
    -- If prices are VAT-exclusive, multiplying by 1.15 would give the "wrong" cart price
    ROUND(pr.SellingPrice * 1.15, 2) AS PriceIfVATExclusive,
    -- If prices are VAT-inclusive, dividing by 1.15 gives Ex VAT
    ROUND(pr.SellingPrice / 1.15, 2) AS PriceExVAT,
    pr.BranchID
FROM Demo_Retail_Product p
INNER JOIN Demo_Retail_Price pr ON p.ProductID = pr.ProductID AND pr.BranchID = p.BranchID
WHERE p.IsActive = 1
  AND pr.SellingPrice > 0
  AND p.BranchID = 1
ORDER BY p.ProductID

-- Check if there's a pattern: are ALL prices stored as VAT-exclusive?
-- If CurrentPrice * 1.15 = the price you see in cart, then prices are VAT-exclusive
-- If CurrentPrice = the price you see in cart, then prices are VAT-inclusive
