-- Check the actual price stored for product with SKU 16009
SELECT 
    p.ProductID,
    p.SKU,
    p.Name,
    p.Barcode,
    p.BranchID,
    pr.SellingPrice,
    pr.CostPrice,
    pr.BranchID AS PriceBranchID
FROM Demo_Retail_Product p
LEFT JOIN Demo_Retail_Price pr ON p.ProductID = pr.ProductID AND pr.BranchID = p.BranchID
WHERE (p.SKU = '16009' OR p.Barcode = '16009')
  AND p.IsActive = 1
ORDER BY p.BranchID
