-- Check if Demo_Retail_Stock has ProductID column
SELECT 
    c.name AS ColumnName
FROM sys.columns c
WHERE c.object_id = OBJECT_ID('Demo_Retail_Stock')
  AND c.name LIKE '%Product%'
GO

-- Show all columns again
SELECT 
    c.name AS ColumnName,
    t.name AS DataType
FROM sys.columns c
INNER JOIN sys.types t ON c.user_type_id = t.user_type_id
WHERE c.object_id = OBJECT_ID('Demo_Retail_Stock')
ORDER BY c.column_id
GO
