-- Test the exact query that ProcessBarcodeScan uses
-- This will show if the query can find product with barcode 16009 at BranchID 6

DECLARE @ItemCode VARCHAR(50) = '16009'
DECLARE @BranchID INT = 6

SELECT TOP 1
    p.ProductID,
    p.SKU,
    p.Barcode,
    p.Name AS ProductName,
    p.BranchID AS ProductBranchID,
    price.SellingPrice,
    price.BranchID AS PriceBranchID
FROM Demo_Retail_Product p
INNER JOIN Demo_Retail_Price price ON p.ProductID = price.ProductID AND price.BranchID = p.BranchID
WHERE (p.SKU = @ItemCode OR p.Barcode = @ItemCode)
  AND p.BranchID = @BranchID
  AND p.IsActive = 1
  AND price.SellingPrice > 0

-- If this returns no rows, the query is failing
-- Check what products exist with this barcode:
SELECT 
    p.ProductID,
    p.SKU,
    p.Barcode,
    p.Name,
    p.BranchID,
    p.IsActive,
    pr.SellingPrice,
    pr.BranchID AS PriceBranchID
FROM Demo_Retail_Product p
LEFT JOIN Demo_Retail_Price pr ON p.ProductID = pr.ProductID
WHERE p.Barcode = '16009' OR p.SKU = '16009'
ORDER BY p.BranchID
