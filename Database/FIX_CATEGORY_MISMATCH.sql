-- Fix category mismatch for Beverages/Drinks products
-- Products have CategoryID=12 but SubCategoryID=26 which belongs to CategoryID=77

PRINT 'Fixing category mismatch...'
PRINT ''

-- Show current state
PRINT 'Products with mismatched CategoryID and SubCategoryID:'
SELECT 
    COUNT(*) AS MismatchedProducts,
    p.CategoryID AS ProductCategoryID,
    sc.CategoryID AS SubCategoryCategoryID
FROM Demo_Retail_Product p
INNER JOIN SubCategories sc ON sc.SubCategoryID = p.SubCategoryID
WHERE p.CategoryID <> sc.CategoryID
AND p.IsActive = 1
GROUP BY p.CategoryID, sc.CategoryID

PRINT ''
PRINT 'Updating products to match their subcategory...'

-- Fix: Update products to match their subcategory's CategoryID
UPDATE p
SET p.CategoryID = sc.CategoryID
FROM Demo_Retail_Product p
INNER JOIN SubCategories sc ON sc.SubCategoryID = p.SubCategoryID
WHERE p.CategoryID <> sc.CategoryID
AND p.IsActive = 1

PRINT 'Updated ' + CAST(@@ROWCOUNT AS VARCHAR) + ' products'
PRINT ''

-- Verify Americano
PRINT 'Verifying Americano Tall:'
SELECT 
    p.SKU,
    p.Name,
    p.CategoryID,
    c.CategoryName,
    p.SubCategoryID,
    sc.SubCategoryName
FROM Demo_Retail_Product p
INNER JOIN Categories c ON c.CategoryID = p.CategoryID
INNER JOIN SubCategories sc ON sc.SubCategoryID = p.SubCategoryID
WHERE p.Name LIKE '%Americano Tall%'

PRINT ''
PRINT '✓ Category mismatch fixed!'
PRINT 'Rebuild POS and products will appear in correct categories.'
