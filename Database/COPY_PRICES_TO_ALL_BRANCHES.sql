-- Copy global prices to all branches
-- This creates branch-specific prices for all branches

PRINT 'Copying global prices to all branches...'
PRINT ''

-- Branch 1: Phoenix
INSERT INTO Demo_Retail_Price (ProductID, BranchID, SellingPrice, CostPrice, EffectiveFrom)
SELECT ProductID, 1, SellingPrice, CostPrice, EffectiveFrom
FROM Demo_Retail_Price
WHERE BranchID IS NULL
AND NOT EXISTS (SELECT 1 FROM Demo_Retail_Price WHERE ProductID = Demo_Retail_Price.ProductID AND BranchID = 1)
PRINT 'Branch 1 (Phoenix): ' + CAST(@@ROWCOUNT AS VARCHAR) + ' prices added'

-- Branch 3: Chatsworth
INSERT INTO Demo_Retail_Price (ProductID, BranchID, SellingPrice, CostPrice, EffectiveFrom)
SELECT ProductID, 3, SellingPrice, CostPrice, EffectiveFrom
FROM Demo_Retail_Price
WHERE BranchID IS NULL
AND NOT EXISTS (SELECT 1 FROM Demo_Retail_Price WHERE ProductID = Demo_Retail_Price.ProductID AND BranchID = 3)
PRINT 'Branch 3 (Chatsworth): ' + CAST(@@ROWCOUNT AS VARCHAR) + ' prices added'

-- Branch 4: Umhlanga
INSERT INTO Demo_Retail_Price (ProductID, BranchID, SellingPrice, CostPrice, EffectiveFrom)
SELECT ProductID, 4, SellingPrice, CostPrice, EffectiveFrom
FROM Demo_Retail_Price
WHERE BranchID IS NULL
AND NOT EXISTS (SELECT 1 FROM Demo_Retail_Price WHERE ProductID = Demo_Retail_Price.ProductID AND BranchID = 4)
PRINT 'Branch 4 (Umhlanga): ' + CAST(@@ROWCOUNT AS VARCHAR) + ' prices added'

-- Branch 5: Durban
INSERT INTO Demo_Retail_Price (ProductID, BranchID, SellingPrice, CostPrice, EffectiveFrom)
SELECT ProductID, 5, SellingPrice, CostPrice, EffectiveFrom
FROM Demo_Retail_Price
WHERE BranchID IS NULL
AND NOT EXISTS (SELECT 1 FROM Demo_Retail_Price WHERE ProductID = Demo_Retail_Price.ProductID AND BranchID = 5)
PRINT 'Branch 5 (Durban): ' + CAST(@@ROWCOUNT AS VARCHAR) + ' prices added'

-- Branch 6: Ayesha Centre
INSERT INTO Demo_Retail_Price (ProductID, BranchID, SellingPrice, CostPrice, EffectiveFrom)
SELECT ProductID, 6, SellingPrice, CostPrice, EffectiveFrom
FROM Demo_Retail_Price
WHERE BranchID IS NULL
AND NOT EXISTS (SELECT 1 FROM Demo_Retail_Price WHERE ProductID = Demo_Retail_Price.ProductID AND BranchID = 6)
PRINT 'Branch 6 (Ayesha Centre): ' + CAST(@@ROWCOUNT AS VARCHAR) + ' prices added'

-- Branch 8: Johannesburg
INSERT INTO Demo_Retail_Price (ProductID, BranchID, SellingPrice, CostPrice, EffectiveFrom)
SELECT ProductID, 8, SellingPrice, CostPrice, EffectiveFrom
FROM Demo_Retail_Price
WHERE BranchID IS NULL
AND NOT EXISTS (SELECT 1 FROM Demo_Retail_Price WHERE ProductID = Demo_Retail_Price.ProductID AND BranchID = 8)
PRINT 'Branch 8 (Johannesburg): ' + CAST(@@ROWCOUNT AS VARCHAR) + ' prices added'

-- Branch 9
INSERT INTO Demo_Retail_Price (ProductID, BranchID, SellingPrice, CostPrice, EffectiveFrom)
SELECT ProductID, 9, SellingPrice, CostPrice, EffectiveFrom
FROM Demo_Retail_Price
WHERE BranchID IS NULL
AND NOT EXISTS (SELECT 1 FROM Demo_Retail_Price WHERE ProductID = Demo_Retail_Price.ProductID AND BranchID = 9)
PRINT 'Branch 9: ' + CAST(@@ROWCOUNT AS VARCHAR) + ' prices added'

-- Branch 10
INSERT INTO Demo_Retail_Price (ProductID, BranchID, SellingPrice, CostPrice, EffectiveFrom)
SELECT ProductID, 10, SellingPrice, CostPrice, EffectiveFrom
FROM Demo_Retail_Price
WHERE BranchID IS NULL
AND NOT EXISTS (SELECT 1 FROM Demo_Retail_Price WHERE ProductID = Demo_Retail_Price.ProductID AND BranchID = 10)
PRINT 'Branch 10: ' + CAST(@@ROWCOUNT AS VARCHAR) + ' prices added'

-- Branch 11: Pietermaritzburg
INSERT INTO Demo_Retail_Price (ProductID, BranchID, SellingPrice, CostPrice, EffectiveFrom)
SELECT ProductID, 11, SellingPrice, CostPrice, EffectiveFrom
FROM Demo_Retail_Price
WHERE BranchID IS NULL
AND NOT EXISTS (SELECT 1 FROM Demo_Retail_Price WHERE ProductID = Demo_Retail_Price.ProductID AND BranchID = 11)
PRINT 'Branch 11 (Pietermaritzburg): ' + CAST(@@ROWCOUNT AS VARCHAR) + ' prices added'

PRINT ''
PRINT '✓ All branches now have complete pricing!'
PRINT 'You can now set different prices per branch in the ERP system.'
