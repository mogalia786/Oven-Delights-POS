-- Check all sales tables for recent invoices

PRINT '========================================='
PRINT 'CHECKING ALL SALES TABLES'
PRINT '========================================='

PRINT ''
PRINT '1. Demo_Sales table:'
IF EXISTS (SELECT * FROM sys.tables WHERE name = 'Demo_Sales')
BEGIN
    SELECT TOP 10 
        SaleID,
        InvoiceNumber,
        SaleDate,
        TotalAmount
    FROM Demo_Sales
    ORDER BY SaleDate DESC
    
    DECLARE @Count1 INT
    SELECT @Count1 = COUNT(*) FROM Demo_Sales
    PRINT 'Total records: ' + CAST(@Count1 AS VARCHAR)
END
ELSE
BEGIN
    PRINT '*** Demo_Sales table does not exist ***'
END

PRINT ''
PRINT '2. POS_Sales table:'
IF EXISTS (SELECT * FROM sys.tables WHERE name = 'POS_Sales')
BEGIN
    SELECT TOP 10 *
    FROM POS_Sales
    ORDER BY SaleDate DESC
    
    DECLARE @Count2 INT
    SELECT @Count2 = COUNT(*) FROM POS_Sales
    PRINT 'Total records: ' + CAST(@Count2 AS VARCHAR)
END
ELSE
BEGIN
    PRINT '*** POS_Sales table does not exist ***'
END

PRINT ''
PRINT '3. Sales table:'
IF EXISTS (SELECT * FROM sys.tables WHERE name = 'Sales')
BEGIN
    SELECT TOP 10 *
    FROM Sales
    ORDER BY SaleDate DESC
    
    DECLARE @Count3 INT
    SELECT @Count3 = COUNT(*) FROM Sales
    PRINT 'Total records: ' + CAST(@Count3 AS VARCHAR)
END
ELSE
BEGIN
    PRINT '*** Sales table does not exist ***'
END

PRINT ''
PRINT '4. POS_InvoiceLines table:'
IF EXISTS (SELECT * FROM sys.tables WHERE name = 'POS_InvoiceLines')
BEGIN
    SELECT DISTINCT TOP 10
        InvoiceNumber,
        SaleDate,
        COUNT(*) as LineCount
    FROM POS_InvoiceLines
    GROUP BY InvoiceNumber, SaleDate
    ORDER BY SaleDate DESC
    
    DECLARE @Count4 INT
    SELECT @Count4 = COUNT(*) FROM POS_InvoiceLines
    PRINT 'Total invoice lines: ' + CAST(@Count4 AS VARCHAR)
END
ELSE
BEGIN
    PRINT '*** POS_InvoiceLines table does not exist ***'
END

PRINT ''
PRINT '5. Checking for invoice INV-PH-TILL-01-000006:'
PRINT ''

IF EXISTS (SELECT * FROM Demo_Sales WHERE InvoiceNumber = 'INV-PH-TILL-01-000006')
    PRINT '✓ Found in Demo_Sales'
ELSE
    PRINT '✗ NOT in Demo_Sales'

IF EXISTS (SELECT * FROM sys.tables WHERE name = 'POS_Sales')
BEGIN
    IF EXISTS (SELECT * FROM POS_Sales WHERE InvoiceNumber = 'INV-PH-TILL-01-000006')
        PRINT '✓ Found in POS_Sales'
    ELSE
        PRINT '✗ NOT in POS_Sales'
END

IF EXISTS (SELECT * FROM POS_InvoiceLines WHERE InvoiceNumber = 'INV-PH-TILL-01-000006')
    PRINT '✓ Found in POS_InvoiceLines'
ELSE
    PRINT '✗ NOT in POS_InvoiceLines'

PRINT ''
PRINT '========================================='
PRINT 'END CHECK'
PRINT '========================================='
