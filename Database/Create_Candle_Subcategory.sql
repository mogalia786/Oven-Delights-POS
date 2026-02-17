-- Create Candle subcategory under Candle category and move all products
BEGIN TRANSACTION;

-- Get Candle category ID
DECLARE @CandleCategoryID INT;
SELECT @CandleCategoryID = CategoryID FROM Categories WHERE LOWER(CategoryName) = 'candle';

-- Create Candle subcategory if it doesn't exist
DECLARE @CandleSubCategoryID INT;

IF NOT EXISTS (SELECT 1 FROM SubCategories WHERE CategoryID = @CandleCategoryID AND SubCategoryName = 'Candles')
BEGIN
    INSERT INTO SubCategories (CategoryID, SubCategoryName, DisplayOrder, IsActive)
    VALUES (@CandleCategoryID, 'Candles', 1, 1);
    PRINT 'Created Candles subcategory';
END

SELECT @CandleSubCategoryID = SubCategoryID 
FROM SubCategories 
WHERE CategoryID = @CandleCategoryID AND SubCategoryName = 'Candles';

-- Update ALL Candle products to use the Candles subcategory
UPDATE Demo_Retail_Product
SET SubCategoryID = @CandleSubCategoryID
WHERE CategoryID = @CandleCategoryID
  AND IsActive = 1;

PRINT 'Updated all Candle products to Candles subcategory';

COMMIT TRANSACTION;

-- Verify results
SELECT 
    c.CategoryName,
    sc.SubCategoryName,
    COUNT(*) AS ProductCount
FROM Demo_Retail_Product p
INNER JOIN Categories c ON c.CategoryID = p.CategoryID
INNER JOIN SubCategories sc ON sc.SubCategoryID = p.SubCategoryID
WHERE c.CategoryID = @CandleCategoryID
  AND p.IsActive = 1
GROUP BY c.CategoryName, sc.SubCategoryName;

PRINT 'Candle subcategory created and products moved!';
