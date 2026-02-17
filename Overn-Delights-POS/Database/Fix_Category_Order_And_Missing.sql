-- Fix Category Display Order and Add Missing Categories
-- Run this script to set the correct order and add Beverages & Miscellaneous

BEGIN TRANSACTION;

-- Update DisplayOrder for existing categories - EXACT order from your list
UPDATE Categories SET DisplayOrder = 1 WHERE CategoryName IN ('Fresh Cream', 'fresh cream');
UPDATE Categories SET DisplayOrder = 2 WHERE CategoryName IN ('Butter Cream', 'butter cream', 'buttercream');
UPDATE Categories SET DisplayOrder = 3 WHERE CategoryName IN ('Exotic Cakes', 'exotic cakes');
UPDATE Categories SET DisplayOrder = 4 WHERE CategoryName IN ('Shop Front', 'shop front');
UPDATE Categories SET DisplayOrder = 5 WHERE CategoryName IN ('Pies', 'pies');
UPDATE Categories SET DisplayOrder = 6 WHERE CategoryName IN ('Birthday Cake Fresh Cream', 'Fresh Cream Birthday Cakes');
UPDATE Categories SET DisplayOrder = 7 WHERE CategoryName IN ('Birthday Cake Butter Cream', 'Buttercream Birthday Cake');
UPDATE Categories SET DisplayOrder = 8 WHERE CategoryName IN ('Novelty', 'novelty');
UPDATE Categories SET DisplayOrder = 9 WHERE CategoryName IN ('Biscuits', 'biscuits');
UPDATE Categories SET DisplayOrder = 10 WHERE CategoryName IN ('Platters', 'platter', 'platters');
UPDATE Categories SET DisplayOrder = 11 WHERE CategoryName IN ('Savouries', 'savoury');
UPDATE Categories SET DisplayOrder = 12 WHERE CategoryName IN ('Drinks', 'drinks');
UPDATE Categories SET DisplayOrder = 13 WHERE CategoryName IN ('Beverages', 'beverages');
UPDATE Categories SET DisplayOrder = 14 WHERE CategoryName IN ('Snacks', 'snacks');
UPDATE Categories SET DisplayOrder = 15 WHERE CategoryName IN ('Sweets', 'sweets');
UPDATE Categories SET DisplayOrder = 16 WHERE CategoryName IN ('Wedding Cake', 'Wedding Cakes', 'wedding cake');
UPDATE Categories SET DisplayOrder = 17 WHERE CategoryName IN ('Fruit Cake', 'Fruitcake', 'fruit cake');
UPDATE Categories SET DisplayOrder = 18 WHERE CategoryName IN ('Candle', 'candle');
UPDATE Categories SET DisplayOrder = 19 WHERE CategoryName IN ('Miscellaneous', 'miscellaneous');

-- Add Beverages category if it doesn't exist
IF NOT EXISTS (SELECT 1 FROM Categories WHERE CategoryName = 'Beverages')
BEGIN
    DECLARE @sqlBev NVARCHAR(MAX);
    -- Check if CreatedDate column exists
    IF EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Categories' AND COLUMN_NAME = 'CreatedDate')
        SET @sqlBev = 'INSERT INTO Categories (CategoryName, DisplayOrder, IsActive, CreatedDate) VALUES (''Beverages'', 13, 1, GETDATE())';
    ELSE
        SET @sqlBev = 'INSERT INTO Categories (CategoryName, DisplayOrder, IsActive) VALUES (''Beverages'', 13, 1)';
    
    EXEC sp_executesql @sqlBev;
    PRINT 'Added Beverages category';
END
ELSE
BEGIN
    PRINT 'Beverages category already exists';
END

-- Add Miscellaneous category if it doesn't exist
IF NOT EXISTS (SELECT 1 FROM Categories WHERE CategoryName = 'Miscellaneous')
BEGIN
    DECLARE @sqlMisc NVARCHAR(MAX);
    -- Check if CreatedDate column exists
    IF EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Categories' AND COLUMN_NAME = 'CreatedDate')
        SET @sqlMisc = 'INSERT INTO Categories (CategoryName, DisplayOrder, IsActive, CreatedDate) VALUES (''Miscellaneous'', 18, 1, GETDATE())';
    ELSE
        SET @sqlMisc = 'INSERT INTO Categories (CategoryName, DisplayOrder, IsActive) VALUES (''Miscellaneous'', 18, 1)';
    
    EXEC sp_executesql @sqlMisc;
    PRINT 'Added Miscellaneous category';
END
ELSE
BEGIN
    PRINT 'Miscellaneous category already exists';
END

-- Candle remains as a main category (no changes needed)

COMMIT TRANSACTION;

-- Show results
SELECT CategoryID, CategoryName, DisplayOrder, IsActive 
FROM Categories 
WHERE IsActive = 1
ORDER BY DisplayOrder;

PRINT 'Category ordering fixed successfully!';
