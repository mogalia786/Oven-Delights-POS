-- Add ALL products to ALL branches with prices
-- This ensures every product is available in every branch

PRINT '======================================================================'
PRINT 'ADDING ALL PRODUCTS TO ALL BRANCHES'
PRINT '======================================================================'
PRINT ''

DECLARE @BranchCount INT
DECLARE @ProductCount INT

-- Count active products
SELECT @ProductCount = COUNT(*) 
FROM Demo_Retail_Product 
WHERE IsActive = 1
AND ProductType IN ('External', 'Internal')
AND Category NOT IN ('ingredients', 'sub recipe', 'packaging', 'consumables', 'equipment', 'miscellaneous', 'pest control')

PRINT 'Active retail products: ' + CAST(@ProductCount AS VARCHAR)
PRINT ''

-- Branch 1: Phoenix
PRINT 'Processing Branch 1 (Phoenix)...'
INSERT INTO Demo_Retail_Price (ProductID, BranchID, SellingPrice, CostPrice, EffectiveFrom)
SELECT 
    p.ProductID,
    1 AS BranchID,
    ISNULL((SELECT TOP 1 SellingPrice FROM Demo_Retail_Price WHERE ProductID = p.ProductID AND BranchID IS NULL ORDER BY EffectiveFrom DESC), 10.00) AS SellingPrice,
    ISNULL((SELECT TOP 1 CostPrice FROM Demo_Retail_Price WHERE ProductID = p.ProductID AND BranchID IS NULL ORDER BY EffectiveFrom DESC), 0.00) AS CostPrice,
    GETDATE()
FROM Demo_Retail_Product p
WHERE p.IsActive = 1
AND p.ProductType IN ('External', 'Internal')
AND p.Category NOT IN ('ingredients', 'sub recipe', 'packaging', 'consumables', 'equipment', 'miscellaneous', 'pest control')
AND NOT EXISTS (SELECT 1 FROM Demo_Retail_Price WHERE ProductID = p.ProductID AND BranchID = 1)
PRINT '  Added ' + CAST(@@ROWCOUNT AS VARCHAR) + ' prices'

-- Branch 3: Chatsworth
PRINT 'Processing Branch 3 (Chatsworth)...'
INSERT INTO Demo_Retail_Price (ProductID, BranchID, SellingPrice, CostPrice, EffectiveFrom)
SELECT 
    p.ProductID, 3,
    ISNULL((SELECT TOP 1 SellingPrice FROM Demo_Retail_Price WHERE ProductID = p.ProductID AND BranchID IS NULL ORDER BY EffectiveFrom DESC), 10.00),
    ISNULL((SELECT TOP 1 CostPrice FROM Demo_Retail_Price WHERE ProductID = p.ProductID AND BranchID IS NULL ORDER BY EffectiveFrom DESC), 0.00),
    GETDATE()
FROM Demo_Retail_Product p
WHERE p.IsActive = 1 AND p.ProductType IN ('External', 'Internal')
AND p.Category NOT IN ('ingredients', 'sub recipe', 'packaging', 'consumables', 'equipment', 'miscellaneous', 'pest control')
AND NOT EXISTS (SELECT 1 FROM Demo_Retail_Price WHERE ProductID = p.ProductID AND BranchID = 3)
PRINT '  Added ' + CAST(@@ROWCOUNT AS VARCHAR) + ' prices'

-- Branch 4: Umhlanga
PRINT 'Processing Branch 4 (Umhlanga)...'
INSERT INTO Demo_Retail_Price (ProductID, BranchID, SellingPrice, CostPrice, EffectiveFrom)
SELECT 
    p.ProductID, 4,
    ISNULL((SELECT TOP 1 SellingPrice FROM Demo_Retail_Price WHERE ProductID = p.ProductID AND BranchID IS NULL ORDER BY EffectiveFrom DESC), 10.00),
    ISNULL((SELECT TOP 1 CostPrice FROM Demo_Retail_Price WHERE ProductID = p.ProductID AND BranchID IS NULL ORDER BY EffectiveFrom DESC), 0.00),
    GETDATE()
FROM Demo_Retail_Product p
WHERE p.IsActive = 1 AND p.ProductType IN ('External', 'Internal')
AND p.Category NOT IN ('ingredients', 'sub recipe', 'packaging', 'consumables', 'equipment', 'miscellaneous', 'pest control')
AND NOT EXISTS (SELECT 1 FROM Demo_Retail_Price WHERE ProductID = p.ProductID AND BranchID = 4)
PRINT '  Added ' + CAST(@@ROWCOUNT AS VARCHAR) + ' prices'

-- Branch 5: Durban
PRINT 'Processing Branch 5 (Durban)...'
INSERT INTO Demo_Retail_Price (ProductID, BranchID, SellingPrice, CostPrice, EffectiveFrom)
SELECT 
    p.ProductID, 5,
    ISNULL((SELECT TOP 1 SellingPrice FROM Demo_Retail_Price WHERE ProductID = p.ProductID AND BranchID IS NULL ORDER BY EffectiveFrom DESC), 10.00),
    ISNULL((SELECT TOP 1 CostPrice FROM Demo_Retail_Price WHERE ProductID = p.ProductID AND BranchID IS NULL ORDER BY EffectiveFrom DESC), 0.00),
    GETDATE()
FROM Demo_Retail_Product p
WHERE p.IsActive = 1 AND p.ProductType IN ('External', 'Internal')
AND p.Category NOT IN ('ingredients', 'sub recipe', 'packaging', 'consumables', 'equipment', 'miscellaneous', 'pest control')
AND NOT EXISTS (SELECT 1 FROM Demo_Retail_Price WHERE ProductID = p.ProductID AND BranchID = 5)
PRINT '  Added ' + CAST(@@ROWCOUNT AS VARCHAR) + ' prices'

-- Branch 6: Ayesha Centre
PRINT 'Processing Branch 6 (Ayesha Centre)...'
INSERT INTO Demo_Retail_Price (ProductID, BranchID, SellingPrice, CostPrice, EffectiveFrom)
SELECT 
    p.ProductID, 6,
    ISNULL((SELECT TOP 1 SellingPrice FROM Demo_Retail_Price WHERE ProductID = p.ProductID AND BranchID IS NULL ORDER BY EffectiveFrom DESC), 10.00),
    ISNULL((SELECT TOP 1 CostPrice FROM Demo_Retail_Price WHERE ProductID = p.ProductID AND BranchID IS NULL ORDER BY EffectiveFrom DESC), 0.00),
    GETDATE()
FROM Demo_Retail_Product p
WHERE p.IsActive = 1 AND p.ProductType IN ('External', 'Internal')
AND p.Category NOT IN ('ingredients', 'sub recipe', 'packaging', 'consumables', 'equipment', 'miscellaneous', 'pest control')
AND NOT EXISTS (SELECT 1 FROM Demo_Retail_Price WHERE ProductID = p.ProductID AND BranchID = 6)
PRINT '  Added ' + CAST(@@ROWCOUNT AS VARCHAR) + ' prices'

-- Branch 8: Johannesburg
PRINT 'Processing Branch 8 (Johannesburg)...'
INSERT INTO Demo_Retail_Price (ProductID, BranchID, SellingPrice, CostPrice, EffectiveFrom)
SELECT 
    p.ProductID, 8,
    ISNULL((SELECT TOP 1 SellingPrice FROM Demo_Retail_Price WHERE ProductID = p.ProductID AND BranchID IS NULL ORDER BY EffectiveFrom DESC), 10.00),
    ISNULL((SELECT TOP 1 CostPrice FROM Demo_Retail_Price WHERE ProductID = p.ProductID AND BranchID IS NULL ORDER BY EffectiveFrom DESC), 0.00),
    GETDATE()
FROM Demo_Retail_Product p
WHERE p.IsActive = 1 AND p.ProductType IN ('External', 'Internal')
AND p.Category NOT IN ('ingredients', 'sub recipe', 'packaging', 'consumables', 'equipment', 'miscellaneous', 'pest control')
AND NOT EXISTS (SELECT 1 FROM Demo_Retail_Price WHERE ProductID = p.ProductID AND BranchID = 8)
PRINT '  Added ' + CAST(@@ROWCOUNT AS VARCHAR) + ' prices'

-- Branch 9
PRINT 'Processing Branch 9...'
INSERT INTO Demo_Retail_Price (ProductID, BranchID, SellingPrice, CostPrice, EffectiveFrom)
SELECT 
    p.ProductID, 9,
    ISNULL((SELECT TOP 1 SellingPrice FROM Demo_Retail_Price WHERE ProductID = p.ProductID AND BranchID IS NULL ORDER BY EffectiveFrom DESC), 10.00),
    ISNULL((SELECT TOP 1 CostPrice FROM Demo_Retail_Price WHERE ProductID = p.ProductID AND BranchID IS NULL ORDER BY EffectiveFrom DESC), 0.00),
    GETDATE()
FROM Demo_Retail_Product p
WHERE p.IsActive = 1 AND p.ProductType IN ('External', 'Internal')
AND p.Category NOT IN ('ingredients', 'sub recipe', 'packaging', 'consumables', 'equipment', 'miscellaneous', 'pest control')
AND NOT EXISTS (SELECT 1 FROM Demo_Retail_Price WHERE ProductID = p.ProductID AND BranchID = 9)
PRINT '  Added ' + CAST(@@ROWCOUNT AS VARCHAR) + ' prices'

-- Branch 10
PRINT 'Processing Branch 10...'
INSERT INTO Demo_Retail_Price (ProductID, BranchID, SellingPrice, CostPrice, EffectiveFrom)
SELECT 
    p.ProductID, 10,
    ISNULL((SELECT TOP 1 SellingPrice FROM Demo_Retail_Price WHERE ProductID = p.ProductID AND BranchID IS NULL ORDER BY EffectiveFrom DESC), 10.00),
    ISNULL((SELECT TOP 1 CostPrice FROM Demo_Retail_Price WHERE ProductID = p.ProductID AND BranchID IS NULL ORDER BY EffectiveFrom DESC), 0.00),
    GETDATE()
FROM Demo_Retail_Product p
WHERE p.IsActive = 1 AND p.ProductType IN ('External', 'Internal')
AND p.Category NOT IN ('ingredients', 'sub recipe', 'packaging', 'consumables', 'equipment', 'miscellaneous', 'pest control')
AND NOT EXISTS (SELECT 1 FROM Demo_Retail_Price WHERE ProductID = p.ProductID AND BranchID = 10)
PRINT '  Added ' + CAST(@@ROWCOUNT AS VARCHAR) + ' prices'

-- Branch 11: Pietermaritzburg
PRINT 'Processing Branch 11 (Pietermaritzburg)...'
INSERT INTO Demo_Retail_Price (ProductID, BranchID, SellingPrice, CostPrice, EffectiveFrom)
SELECT 
    p.ProductID, 11,
    ISNULL((SELECT TOP 1 SellingPrice FROM Demo_Retail_Price WHERE ProductID = p.ProductID AND BranchID IS NULL ORDER BY EffectiveFrom DESC), 10.00),
    ISNULL((SELECT TOP 1 CostPrice FROM Demo_Retail_Price WHERE ProductID = p.ProductID AND BranchID IS NULL ORDER BY EffectiveFrom DESC), 0.00),
    GETDATE()
FROM Demo_Retail_Product p
WHERE p.IsActive = 1 AND p.ProductType IN ('External', 'Internal')
AND p.Category NOT IN ('ingredients', 'sub recipe', 'packaging', 'consumables', 'equipment', 'miscellaneous', 'pest control')
AND NOT EXISTS (SELECT 1 FROM Demo_Retail_Price WHERE ProductID = p.ProductID AND BranchID = 11)
PRINT '  Added ' + CAST(@@ROWCOUNT AS VARCHAR) + ' prices'

PRINT ''
PRINT '======================================================================'
PRINT '✓ ALL PRODUCTS NOW AVAILABLE IN ALL BRANCHES WITH PRICES!'
PRINT '======================================================================'
PRINT ''
PRINT 'Verifying Bonaqua in Branch 6:'

SELECT 
    p.SKU,
    p.Name,
    pr.BranchID,
    pr.SellingPrice
FROM Demo_Retail_Product p
INNER JOIN Demo_Retail_Price pr ON pr.ProductID = p.ProductID AND pr.BranchID = 6
WHERE p.Name LIKE '%Bonaqua%'
