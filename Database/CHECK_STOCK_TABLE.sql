-- Check if Demo_Retail_Stock has QtyOnHand or Quantity column
SELECT TOP 5
    ProductID,
    BranchID,
    CASE 
        WHEN EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Demo_Retail_Stock' AND COLUMN_NAME = 'QtyOnHand') THEN 'Has QtyOnHand'
        WHEN EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Demo_Retail_Stock' AND COLUMN_NAME = 'Quantity') THEN 'Has Quantity'
        ELSE 'Unknown column'
    END AS StockColumnName
FROM Demo_Retail_Stock

-- Show actual columns in Demo_Retail_Stock table
SELECT 
    COLUMN_NAME,
    DATA_TYPE
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME = 'Demo_Retail_Stock'
ORDER BY ORDINAL_POSITION
