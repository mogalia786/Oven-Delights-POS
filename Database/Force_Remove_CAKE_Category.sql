-- =============================================
-- Force Remove CAKE Category
-- Direct deactivation regardless of product count
-- =============================================

-- Check current status
PRINT 'Current CAKE category status:'
SELECT CategoryID, CategoryName, IsActive, DisplayOrder
FROM Categories
WHERE LOWER(LTRIM(RTRIM(CategoryName))) = 'cake'

PRINT ''
PRINT 'Products under CAKE category:'
SELECT COUNT(*) AS ProductCount
FROM Demo_Retail_Product
WHERE CategoryID IN (SELECT CategoryID FROM Categories WHERE LOWER(LTRIM(RTRIM(CategoryName))) = 'cake')

PRINT ''
PRINT '========================================='

-- Force deactivate CAKE category
UPDATE Categories
SET IsActive = 0
WHERE LOWER(LTRIM(RTRIM(CategoryName))) = 'cake'

PRINT '✓ CAKE category deactivated'
PRINT ''

-- Verify
PRINT 'Verification - CAKE category after update:'
SELECT CategoryID, CategoryName, IsActive, DisplayOrder
FROM Categories
WHERE LOWER(LTRIM(RTRIM(CategoryName))) = 'cake'

PRINT ''
PRINT '========================================='
PRINT 'IMPORTANT: You MUST completely close and restart POS'
PRINT 'Do NOT just refresh - fully exit and reopen the application'
PRINT '========================================='
