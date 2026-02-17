-- Create missing variant records for products that don't have them
-- This ensures the foreign key constraint doesn't fail during returns

INSERT INTO Demo_Retail_Variant (ProductID, Barcode, IsActive, CreatedAt)
SELECT 
    p.ProductID,
    NULL AS Barcode,
    1 AS IsActive,
    GETDATE() AS CreatedAt
FROM Demo_Retail_Product p
WHERE p.IsActive = 1
  AND NOT EXISTS (
      SELECT 1 FROM Demo_Retail_Variant v 
      WHERE v.ProductID = p.ProductID
  );

PRINT 'Created missing variant records';

-- Verify
SELECT COUNT(*) AS ProductsWithoutVariants
FROM Demo_Retail_Product p
WHERE p.IsActive = 1
  AND NOT EXISTS (
      SELECT 1 FROM Demo_Retail_Variant v 
      WHERE v.ProductID = p.ProductID
  );
