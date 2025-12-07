-- Check what category ID 45 is
SELECT * FROM Categories WHERE CategoryID = 45;

-- Update Candle products to use the correct Candle category (ID 16)
UPDATE Demo_Retail_Product
SET CategoryID = 16
WHERE Category = 'candle' AND IsActive = 1;

-- Verify the fix
SELECT COUNT(*) AS UpdatedProducts
FROM Demo_Retail_Product
WHERE CategoryID = 16 AND IsActive = 1;

-- Show the Candle category with correct product count
SELECT c.CategoryID, c.CategoryName, c.IsActive, COUNT(p.ProductID) AS ProductCount
FROM Categories c
LEFT JOIN Demo_Retail_Product p ON p.CategoryID = c.CategoryID AND p.IsActive = 1
WHERE c.CategoryName = 'candle'
GROUP BY c.CategoryID, c.CategoryName, c.IsActive;

PRINT 'Candle products remapped to correct category!';
