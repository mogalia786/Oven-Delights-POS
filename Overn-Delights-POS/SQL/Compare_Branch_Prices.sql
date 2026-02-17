-- Compare BranchID 4 vs BranchID 6 prices

-- Check BranchID 4 prices
SELECT TOP 10
    'BranchID 4' AS Branch,
    ProductID,
    SellingPrice,
    CostPrice,
    EffectiveFrom,
    EffectiveTo
FROM Demo_Retail_Price
WHERE BranchID = 4
ORDER BY ProductID;

-- Check BranchID 6 prices
SELECT TOP 10
    'BranchID 6' AS Branch,
    ProductID,
    SellingPrice,
    CostPrice,
    EffectiveFrom,
    EffectiveTo
FROM Demo_Retail_Price
WHERE BranchID = 6
ORDER BY ProductID;

-- Check if there are NULL BranchID prices (global prices)
SELECT TOP 10
    'Global (NULL)' AS Branch,
    ProductID,
    SellingPrice,
    CostPrice,
    EffectiveFrom,
    EffectiveTo
FROM Demo_Retail_Price
WHERE BranchID IS NULL
ORDER BY ProductID;

-- Check current date
SELECT GETDATE() AS CurrentServerDate;
