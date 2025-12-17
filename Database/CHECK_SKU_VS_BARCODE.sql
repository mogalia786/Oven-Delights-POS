-- Check SKU vs Barcode fields for product 16009
-- This will show why scanner returns different ItemCode than manual entry

SELECT 
    p.ProductID,
    p.SKU,
    p.Barcode,
    p.Name,
    p.BranchID,
    pr.SellingPrice
FROM Demo_Retail_Product p
LEFT JOIN Demo_Retail_Price pr ON p.ProductID = pr.ProductID AND pr.BranchID = p.BranchID
WHERE p.ProductID = 59564
  OR p.SKU LIKE '%16009%'
  OR p.Barcode LIKE '%16009%'
  OR p.SKU = 'SHP-BVS-EAC'
  OR p.Barcode = 'SHP-BVS-EAC'
ORDER BY p.BranchID
