-- =============================================
-- Debug User-Defined Orders for Returns System
-- Run this to verify data exists and check column names
-- =============================================

-- 1. Check if user-defined orders exist with status 'PickedUp'
SELECT 
    o.UserDefinedOrderID,
    o.OrderNumber,
    o.BranchID,
    o.CustomerCellNumber,
    o.CustomerName,
    o.Status,
    o.OrderDate,
    o.CollectionDate
FROM POS_UserDefinedOrders o
WHERE o.Status = 'PickedUp'
ORDER BY o.OrderDate DESC;

-- 2. Check order items with product codes
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
    o.Status
FROM POS_UserDefinedOrderItems oi
INNER JOIN POS_UserDefinedOrders o ON oi.UserDefinedOrderID = o.UserDefinedOrderID
WHERE o.Status = 'PickedUp'
ORDER BY o.OrderDate DESC;

-- 3. Test the exact query used in returns (replace @Barcode, @BranchID, @CellNumber with actual values)
-- Example: Test with BranchID=6, CellNumber='0123456789', Barcode='12345'
DECLARE @Barcode NVARCHAR(50) = '12345'; -- Replace with actual barcode
DECLARE @BranchID INT = 6; -- Replace with actual branch ID
DECLARE @CellNumber NVARCHAR(20) = '0123456789'; -- Replace with actual cell number

SELECT TOP 1
    oi.ProductID,
    ISNULL(oi.ProductCode, p.SKU) AS ItemCode,
    oi.ProductName,
    oi.UnitPrice,
    o.OrderNumber,
    o.CustomerCellNumber,
    o.Status,
    o.OrderDate
FROM POS_UserDefinedOrderItems oi
INNER JOIN POS_UserDefinedOrders o ON oi.UserDefinedOrderID = o.UserDefinedOrderID
LEFT JOIN Demo_Retail_Product p ON oi.ProductID = p.ProductID AND p.BranchID = @BranchID
WHERE (ISNULL(oi.ProductCode, p.SKU) LIKE '%' + @Barcode + '%' OR p.Barcode LIKE '%' + @Barcode + '%')
  AND o.BranchID = @BranchID
  AND o.CustomerCellNumber = @CellNumber
  AND o.Status = 'PickedUp'
  AND CAST(o.OrderDate AS DATE) >= DATEADD(DAY, -30, CAST(GETDATE() AS DATE));

-- 4. Check Branches table for address and phone info
-- First check what columns actually exist
SELECT 
    COLUMN_NAME,
    DATA_TYPE,
    IS_NULLABLE
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME = 'Branches'
ORDER BY ORDINAL_POSITION;

-- Then show sample data
SELECT TOP 5 * FROM Branches;

-- 5. Check if columns exist in Branches table
SELECT 
    COLUMN_NAME,
    DATA_TYPE,
    IS_NULLABLE
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME = 'Branches'
ORDER BY ORDINAL_POSITION;

PRINT '========================================='
PRINT 'Debug queries completed!'
PRINT 'Check results to verify:'
PRINT '1. User-defined orders with PickedUp status exist'
PRINT '2. Order items have ProductCode populated'
PRINT '3. Branches table has BranchAddress, BranchPhone, VATRegistrationNumber columns'
PRINT '4. Test query returns results with sample data'
PRINT '========================================='
