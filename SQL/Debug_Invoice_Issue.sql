-- Debug script to check invoice and line items

DECLARE @InvoiceNumber NVARCHAR(100) = 'INV-PH-TILL-01-000001'
DECLARE @VariantID INT = 389

PRINT '========================================='
PRINT 'DEBUGGING INVOICE: ' + @InvoiceNumber
PRINT '========================================='

-- Check if sale exists
IF EXISTS (SELECT * FROM Demo_Sales WHERE InvoiceNumber = @InvoiceNumber)
BEGIN
    PRINT 'Sale found!'
    
    SELECT 
        SaleID,
        InvoiceNumber,
        SaleDate,
        Subtotal,
        TotalAmount,
        CustomerName
    FROM Demo_Sales 
    WHERE InvoiceNumber = @InvoiceNumber
    
    DECLARE @SaleID INT
    SELECT @SaleID = SaleID FROM Demo_Sales WHERE InvoiceNumber = @InvoiceNumber
    
    PRINT ''
    PRINT 'SaleID: ' + CAST(@SaleID AS VARCHAR)
    PRINT ''
    PRINT 'Line items for this sale:'
    
    -- Check line items
    IF EXISTS (SELECT * FROM Demo_SalesDetails WHERE SaleID = @SaleID)
    BEGIN
        SELECT 
            SaleDetailID,
            VariantID,
            ProductName,
            Quantity,
            UnitPrice,
            LineTotal
        FROM Demo_SalesDetails
        WHERE SaleID = @SaleID
        ORDER BY SaleDetailID
    END
    ELSE
    BEGIN
        PRINT '*** NO LINE ITEMS FOUND FOR THIS SALE ***'
    END
    
    PRINT ''
    PRINT 'Checking if VariantID ' + CAST(@VariantID AS VARCHAR) + ' exists in this sale:'
    
    IF EXISTS (SELECT * FROM Demo_SalesDetails WHERE SaleID = @SaleID AND VariantID = @VariantID)
    BEGIN
        SELECT * FROM Demo_SalesDetails WHERE SaleID = @SaleID AND VariantID = @VariantID
    END
    ELSE
    BEGIN
        PRINT '*** VARIANT ' + CAST(@VariantID AS VARCHAR) + ' NOT FOUND IN THIS SALE ***'
    END
    
    PRINT ''
    PRINT 'Checking returns for this invoice:'
    
    IF EXISTS (SELECT * FROM Demo_Returns WHERE OriginalInvoiceNumber = @InvoiceNumber)
    BEGIN
        SELECT 
            ReturnID,
            ReturnNumber,
            ReturnDate,
            TotalAmount,
            Reason
        FROM Demo_Returns
        WHERE OriginalInvoiceNumber = @InvoiceNumber
        ORDER BY ReturnDate DESC
        
        PRINT ''
        PRINT 'Return details:'
        
        SELECT 
            rd.ReturnID,
            r.ReturnNumber,
            rd.VariantID,
            rd.ProductName,
            rd.Quantity,
            rd.LineTotal,
            rd.RestockItem
        FROM Demo_ReturnDetails rd
        INNER JOIN Demo_Returns r ON rd.ReturnID = r.ReturnID
        WHERE r.OriginalInvoiceNumber = @InvoiceNumber
        ORDER BY r.ReturnDate DESC, rd.VariantID
    END
    ELSE
    BEGIN
        PRINT 'No returns found for this invoice'
    END
    
END
ELSE
BEGIN
    PRINT '*** SALE NOT FOUND ***'
END

PRINT ''
PRINT '========================================='
PRINT 'END DEBUG'
PRINT '========================================='
