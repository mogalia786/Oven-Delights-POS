-- Check actual column names in Branches table
SELECT 
    COLUMN_NAME,
    DATA_TYPE,
    IS_NULLABLE,
    CHARACTER_MAXIMUM_LENGTH
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME = 'Branches'
ORDER BY ORDINAL_POSITION;

-- Show sample data from Branches table
SELECT TOP 5 * FROM Branches;
