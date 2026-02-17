-- Sync SKU with Barcode for ALL products
-- Client imports data from Sage with Barcode field only
-- SKU field must always equal Barcode field for POS to work correctly
-- This ensures that when scanning a barcode, the product is found by SKU match

-- BACKUP: Check current state before update
SELECT 
    ProductID,
    SKU AS OldSKU,
    Barcode,
    Name,
    BranchID,
    CASE 
        WHEN Barcode IS NOT NULL AND Barcode <> '' THEN Barcode
        ELSE SKU
    END AS NewSKU
FROM Demo_Retail_Product
WHERE IsActive = 1
ORDER BY BranchID, Name

-- Count how many products will be updated
SELECT COUNT(*) AS ProductsToUpdate
FROM Demo_Retail_Product
WHERE IsActive = 1
  AND Barcode IS NOT NULL
  AND Barcode <> ''

-- UPDATE: Set SKU = Barcode for ALL products that have a barcode
-- This includes products where SKU already equals Barcode (ensures consistency)
UPDATE Demo_Retail_Product
SET SKU = Barcode
WHERE IsActive = 1
  AND Barcode IS NOT NULL
  AND Barcode <> ''

-- VERIFY: Check products after update
SELECT 
    ProductID,
    SKU,
    Barcode,
    Name,
    BranchID
FROM Demo_Retail_Product
WHERE IsActive = 1
  AND Barcode IS NOT NULL
  AND Barcode <> ''
ORDER BY BranchID, Name

-- VERIFY: Specific check for product 16009
SELECT 
    p.ProductID,
    p.SKU,
    p.Barcode,
    p.Name,
    p.BranchID,
    pr.SellingPrice
FROM Demo_Retail_Product p
LEFT JOIN Demo_Retail_Price pr ON p.ProductID = pr.ProductID AND pr.BranchID = p.BranchID
WHERE p.Barcode = '16009'
ORDER BY p.BranchID
