-- Fix Demo_Retail_Product to use proper CategoryID instead of hardcoded 'General'
-- This links products to the actual ProductCategories table

-- Step 1: Add CategoryID column if it doesn't exist
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Demo_Retail_Product') AND name = 'CategoryID')
BEGIN
    ALTER TABLE Demo_Retail_Product
    ADD CategoryID INT NULL
END
GO

-- Step 2: Since SKUs don't match, assign categories based on product name patterns
-- Beverages
UPDATE drp
SET drp.CategoryID = pc.CategoryID,
    drp.Category = pc.CategoryName
FROM Demo_Retail_Product drp
CROSS JOIN ProductCategories pc
WHERE pc.CategoryName = 'Beverages'
  AND (drp.Name LIKE '%Coffee%' OR drp.Name LIKE '%Tea%' OR drp.Name LIKE '%Drink%' OR drp.Name LIKE '%Juice%')
GO

-- Bread
UPDATE drp
SET drp.CategoryID = pc.CategoryID,
    drp.Category = pc.CategoryName
FROM Demo_Retail_Product drp
CROSS JOIN ProductCategories pc
WHERE pc.CategoryName = 'Bread'
  AND (drp.Name LIKE '%Bread%' OR drp.Name LIKE '%Loaf%' OR drp.Name LIKE '%Roll%')
GO

-- Cakes
UPDATE drp
SET drp.CategoryID = pc.CategoryID,
    drp.Category = pc.CategoryName
FROM Demo_Retail_Product drp
CROSS JOIN ProductCategories pc
WHERE pc.CategoryName = 'Cakes'
  AND (drp.Name LIKE '%Cake%' OR drp.Name LIKE '%Sponge%')
GO

-- Pastries
UPDATE drp
SET drp.CategoryID = pc.CategoryID,
    drp.Category = pc.CategoryName
FROM Demo_Retail_Product drp
CROSS JOIN ProductCategories pc
WHERE pc.CategoryName = 'Pastries'
  AND (drp.Name LIKE '%Pastry%' OR drp.Name LIKE '%Croissant%' OR drp.Name LIKE '%Danish%')
GO

-- Packaging
UPDATE drp
SET drp.CategoryID = pc.CategoryID,
    drp.Category = pc.CategoryName
FROM Demo_Retail_Product drp
CROSS JOIN ProductCategories pc
WHERE pc.CategoryName = 'Packaging'
  AND (drp.Name LIKE '%Cup%' OR drp.Name LIKE '%Box%' OR drp.Name LIKE '%Bag%' OR drp.Name LIKE '%Paper%')
GO

-- Raw Materials
UPDATE drp
SET drp.CategoryID = pc.CategoryID,
    drp.Category = pc.CategoryName
FROM Demo_Retail_Product drp
CROSS JOIN ProductCategories pc
WHERE pc.CategoryName = 'Raw Materials'
  AND (drp.Name LIKE '%Flour%' OR drp.Name LIKE '%Sugar%' OR drp.Name LIKE '%Butter%' OR drp.Name LIKE '%Egg%')
GO

-- Step 3: For any products still without a category, set to General/Manufactured Goods
DECLARE @DefaultCategoryID INT
SELECT TOP 1 @DefaultCategoryID = CategoryID FROM ProductCategories WHERE CategoryName IN ('Manufactured Goods', 'General') ORDER BY CategoryName

UPDATE Demo_Retail_Product
SET CategoryID = @DefaultCategoryID,
    Category = (SELECT CategoryName FROM ProductCategories WHERE CategoryID = @DefaultCategoryID)
WHERE CategoryID IS NULL AND @DefaultCategoryID IS NOT NULL
GO

-- Step 4: Add foreign key constraint
IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_Demo_Retail_Product_CategoryID')
BEGIN
    ALTER TABLE Demo_Retail_Product
    ADD CONSTRAINT FK_Demo_Retail_Product_CategoryID 
    FOREIGN KEY (CategoryID) REFERENCES ProductCategories(CategoryID)
END
GO

-- Verify the update
SELECT 
    drp.ProductID,
    drp.SKU,
    drp.Name,
    drp.CategoryID,
    drp.Category,
    pc.CategoryName AS ActualCategoryName
FROM Demo_Retail_Product drp
LEFT JOIN ProductCategories pc ON drp.CategoryID = pc.CategoryID
ORDER BY pc.CategoryName, drp.Name
GO
