-- Fix: Add Barcode=16009 to the product at BranchID 6
-- This will allow barcode scanner to find the product with correct price R44.00

-- Current situation:
-- ProductID 56624 at BranchID 6: SKU=200016009, Barcode=NULL, Price=R44.00
-- ProductID 59564 at BranchID 1: SKU=SHP-BVS-EAC, Barcode=16009, Price=R50.00

-- Solution: Update BranchID 6 product to have Barcode=16009
UPDATE Demo_Retail_Product
SET Barcode = '16009'
WHERE SKU = '200016009'
  AND BranchID = 6
  AND Name = 'BC Vanilla Swiss Roll'

-- Verify the update
SELECT 
    p.ProductID,
    p.SKU,
    p.Barcode,
    p.Name,
    p.BranchID,
    pr.SellingPrice
FROM Demo_Retail_Product p
LEFT JOIN Demo_Retail_Price pr ON p.ProductID = pr.ProductID AND pr.BranchID = p.BranchID
WHERE (p.SKU = '200016009' OR p.Barcode = '16009')
  AND p.BranchID = 6
ORDER BY p.BranchID
