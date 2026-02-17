-- Fix SellingPrice for product 16009 (BC Vanilla Swiss Roll)
-- Database shows 50.00 but should be 44.00 (VAT-inclusive)

-- Update Demo_Retail_Price table
UPDATE Demo_Retail_Price
SET SellingPrice = 44.00
WHERE ProductID = 59564
  AND BranchID = 1

-- Verify the update
SELECT 
    p.ProductID,
    p.SKU,
    p.Name,
    p.Barcode,
    p.BranchID,
    pr.SellingPrice,
    pr.CostPrice
FROM Demo_Retail_Product p
LEFT JOIN Demo_Retail_Price pr ON p.ProductID = pr.ProductID AND pr.BranchID = p.BranchID
WHERE p.ProductID = 59564
  AND p.BranchID = 1
