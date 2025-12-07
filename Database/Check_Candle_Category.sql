-- Check if Candle category exists
SELECT * FROM Categories WHERE CategoryName LIKE '%candle%';

-- Check if there are any Candle products
SELECT COUNT(*) AS CandleProductCount 
FROM Demo_Retail_Product 
WHERE Category LIKE '%candle%' AND IsActive = 1;

-- Show some Candle products if they exist
SELECT TOP 10 SKU, Name, Category, CategoryID, SubCategoryID, ProductType
FROM Demo_Retail_Product 
WHERE Category LIKE '%candle%' AND IsActive = 1;
