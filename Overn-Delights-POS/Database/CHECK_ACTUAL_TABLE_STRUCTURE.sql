-- Check if POS_ReturnItems table exists and what columns it actually has
SELECT 
    c.COLUMN_NAME,
    c.DATA_TYPE,
    c.IS_NULLABLE,
    c.CHARACTER_MAXIMUM_LENGTH
FROM INFORMATION_SCHEMA.COLUMNS c
WHERE c.TABLE_NAME = 'POS_ReturnItems'
ORDER BY c.ORDINAL_POSITION;

-- Also check the actual table definition
SELECT 
    t.TABLE_NAME,
    t.TABLE_TYPE
FROM INFORMATION_SCHEMA.TABLES t
WHERE t.TABLE_NAME = 'POS_ReturnItems';
