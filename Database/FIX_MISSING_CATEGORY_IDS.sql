-- Fix products with NULL CategoryID by mapping from Category text field
-- This is why products don't show in category navigation!

PRINT '======================================================================'
PRINT 'FIXING MISSING CATEGORY IDS'
PRINT '======================================================================'
PRINT ''

-- Update CategoryID based on Category text field
UPDATE p
SET p.CategoryID = c.CategoryID
FROM Demo_Retail_Product p
INNER JOIN Categories c ON LOWER(p.Category) = LOWER(c.CategoryName)
WHERE p.CategoryID IS NULL

PRINT 'Updated ' + CAST(@@ROWCOUNT AS VARCHAR) + ' products with CategoryID'

-- For products where category text doesn't match exactly, try partial match
UPDATE p
SET p.CategoryID = c.CategoryID
FROM Demo_Retail_Product p
INNER JOIN Categories c ON LOWER(p.Category) LIKE '%' + LOWER(c.CategoryName) + '%'
WHERE p.CategoryID IS NULL
AND c.CategoryName NOT IN ('ingredient', 'ingredients')  -- Avoid false matches

PRINT 'Updated ' + CAST(@@ROWCOUNT AS VARCHAR) + ' more products with partial match'

-- Set default SubCategoryID (1 = General/Other)
UPDATE Demo_Retail_Product
SET SubCategoryID = 1
WHERE SubCategoryID IS NULL
AND CategoryID IS NOT NULL

PRINT 'Updated ' + CAST(@@ROWCOUNT AS VARCHAR) + ' products with default SubCategoryID'

PRINT ''
PRINT 'Verifying Bonaqua:'
SELECT SKU, Name, Category, CategoryID, SubCategoryID
FROM Demo_Retail_Product
WHERE Name LIKE '%Bonaqua%'

PRINT ''
PRINT '✓ Category IDs fixed! Products will now appear in category navigation.'
PRINT 'Rebuild POS to see changes.'
