-- =============================================
-- Debug Returns for User-Defined Orders
-- Test with actual barcode and cell number
-- =============================================

-- STEP 1: Check all picked up user-defined orders
SELECT 
    o.UserDefinedOrderID,
    o.OrderNumber,
    o.BranchID,
    o.CustomerCellNumber,
    o.CustomerName,
    o.Status,
    o.OrderDate,
    o.CollectionDate,
    DATEDIFF(DAY, o.OrderDate, GETDATE()) AS DaysAgo
FROM POS_UserDefinedOrders o
WHERE o.Status = 'PickedUp'
ORDER BY o.OrderDate DESC;

-- STEP 2: Check order items for picked up orders
SELECT 
    oi.ItemID,
    oi.UserDefinedOrderID,
    oi.ProductID,
    oi.ProductCode,
    oi.ProductName,
    oi.Quantity,
    oi.UnitPrice,
    o.OrderNumber,
    o.CustomerCellNumber,
    o.Status,
    o.BranchID
FROM POS_UserDefinedOrderItems oi
INNER JOIN POS_UserDefinedOrders o ON oi.UserDefinedOrderID = o.UserDefinedOrderID
WHERE o.Status = 'PickedUp'
ORDER BY o.OrderDate DESC;

-- STEP 3: Check Demo_Retail_Product for matching barcodes
-- Replace with actual barcode you're scanning
DECLARE @TestBarcode NVARCHAR(50) = 'YOUR_BARCODE_HERE'; -- REPLACE THIS

SELECT 
    ProductID,
    SKU,
    Barcode,
    Name,
    BranchID
FROM Demo_Retail_Product
WHERE (SKU LIKE '%' + @TestBarcode + '%' OR Barcode LIKE '%' + @TestBarcode + '%')
ORDER BY BranchID;

-- STEP 4: Test the EXACT returns query
-- Replace these with actual values from your test
DECLARE @Barcode NVARCHAR(50) = 'YOUR_BARCODE_HERE'; -- REPLACE THIS
DECLARE @BranchID INT = 6; -- REPLACE WITH YOUR BRANCH ID
DECLARE @CellNumber NVARCHAR(20) = '0123456789'; -- REPLACE WITH ACTUAL CELL NUMBER

PRINT '========================================='
PRINT 'Testing Returns Query with:'
PRINT 'Barcode: ' + @Barcode
PRINT 'BranchID: ' + CAST(@BranchID AS NVARCHAR)
PRINT 'Cell Number: ' + @CellNumber
PRINT '========================================='

-- This is the EXACT query from NoReceiptReturnForm
SELECT TOP 1
    oi.ProductID,
    ISNULL(oi.ProductCode, p.SKU) AS ItemCode,
    oi.ProductName,
    oi.UnitPrice,
    o.OrderNumber,
    o.CustomerCellNumber,
    o.Status,
    o.OrderDate,
    DATEDIFF(DAY, o.OrderDate, GETDATE()) AS DaysAgo
FROM POS_UserDefinedOrderItems oi
INNER JOIN POS_UserDefinedOrders o ON oi.UserDefinedOrderID = o.UserDefinedOrderID
LEFT JOIN Demo_Retail_Product p ON oi.ProductID = p.ProductID AND p.BranchID = @BranchID
WHERE (ISNULL(oi.ProductCode, p.SKU) LIKE '%' + @Barcode + '%' OR p.Barcode LIKE '%' + @Barcode + '%')
  AND o.BranchID = @BranchID
  AND o.CustomerCellNumber = @CellNumber
  AND o.Status = 'PickedUp'
  AND CAST(o.OrderDate AS DATE) >= DATEADD(DAY, -30, CAST(GETDATE() AS DATE));

-- STEP 5: Check if issue is with cell number matching
SELECT 
    o.OrderNumber,
    o.CustomerCellNumber,
    o.Status,
    o.BranchID,
    oi.ProductCode,
    oi.ProductName
FROM POS_UserDefinedOrders o
INNER JOIN POS_UserDefinedOrderItems oi ON o.UserDefinedOrderID = oi.UserDefinedOrderID
WHERE o.Status = 'PickedUp'
  AND o.BranchID = @BranchID
  AND o.CustomerCellNumber LIKE '%' + @CellNumber + '%'; -- Partial match to help debug

PRINT '========================================='
PRINT 'Debug Complete!'
PRINT 'Check results to identify issue:'
PRINT '1. Are there PickedUp orders?'
PRINT '2. Do order items have ProductCode populated?'
PRINT '3. Does barcode match any products?'
PRINT '4. Does cell number match exactly?'
PRINT '5. Is order within 30 days?'
PRINT '========================================='
