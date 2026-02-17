-- Debug script to check actual prices in database
-- Run this to see what prices are stored

-- Check a specific product's price
SELECT TOP 5
    p.ProductID,
    p.SKU,
    p.Name,
    pr.SellingPrice,
    pr.CostPrice,
    pr.BranchID,
    -- Calculate what it would be if VAT-inclusive
    ROUND(pr.SellingPrice / 1.15, 2) AS PriceExVAT,
    ROUND(pr.SellingPrice - (pr.SellingPrice / 1.15), 2) AS VATAmount,
    -- Calculate what it would be if we add VAT
    ROUND(pr.SellingPrice * 1.15, 2) AS PriceIfAddVAT
FROM Demo_Retail_Product p
INNER JOIN Demo_Retail_Price pr ON p.ProductID = pr.ProductID AND pr.BranchID = p.BranchID
WHERE p.IsActive = 1
  AND pr.SellingPrice > 0
ORDER BY p.ProductID

-- Check the specific product from the screenshot (if we can find it by price)
SELECT 
    p.ProductID,
    p.SKU,
    p.Name,
    p.Barcode,
    pr.SellingPrice,
    pr.BranchID
FROM Demo_Retail_Product p
INNER JOIN Demo_Retail_Price pr ON p.ProductID = pr.ProductID AND pr.BranchID = p.BranchID
WHERE pr.SellingPrice BETWEEN 43 AND 45  -- Looking for R44.00
  AND p.IsActive = 1
