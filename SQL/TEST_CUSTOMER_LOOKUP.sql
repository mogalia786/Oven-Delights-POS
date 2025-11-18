-- Test customer lookup functionality

-- 1. Check if POS_Customers table exists
IF EXISTS (SELECT * FROM sys.tables WHERE name = 'POS_Customers')
BEGIN
    PRINT '✓ POS_Customers table exists'
    
    -- 2. Show table structure
    SELECT 
        COLUMN_NAME,
        DATA_TYPE,
        CHARACTER_MAXIMUM_LENGTH,
        IS_NULLABLE
    FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_NAME = 'POS_Customers'
    ORDER BY ORDINAL_POSITION;
    
    -- 3. Show existing customers
    PRINT ''
    PRINT 'Existing customers:'
    SELECT 
        CustomerID,
        CellNumber,
        FirstName,
        Surname,
        Email,
        CreatedDate,
        LastOrderDate,
        TotalOrders,
        IsActive
    FROM POS_Customers
    ORDER BY CreatedDate DESC;
    
    -- 4. Test lookup for sample customer
    PRINT ''
    PRINT 'Testing lookup for cell: 0765144058'
    SELECT 
        FirstName,
        Surname,
        Email
    FROM POS_Customers 
    WHERE CellNumber = '0765144058' 
    AND IsActive = 1;
    
END
ELSE
BEGIN
    PRINT '✗ POS_Customers table does NOT exist - run CREATE_POS_CUSTOMERS_TABLE.sql first!'
END
GO
