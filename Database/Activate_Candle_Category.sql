-- Activate Candle category
UPDATE Categories 
SET IsActive = 1, 
    DisplayOrder = 18 
WHERE CategoryName = 'candle';

-- Verify
SELECT CategoryID, CategoryName, IsActive, DisplayOrder 
FROM Categories 
WHERE CategoryName = 'candle';

PRINT 'Candle category activated!';
