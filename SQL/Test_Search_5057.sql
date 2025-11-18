-- Test search query for SKU containing "5057"
-- This is the exact query used in the POS search function

DECLARE @SearchText NVARCHAR(50) = '5057'
DECLARE @BranchID INT = 1  -- Change this to your actual BranchID

PRINT '========================================='
PRINT 'TESTING SEARCH FOR: ' + @SearchText
PRINT 'Branch ID: ' + CAST(@BranchID AS VARCHAR)
PRINT '========================================='
PRINT ''

-- Main search query
SELECT TOP 20
    drp.ProductID,
    drp.SKU AS ItemCode,
    drp.Name AS ProductName,
    ISNULL(price.SellingPrice, 0) AS SellingPrice,
    ISNULL(stock.QtyOnHand, 0) AS QtyOnHand,
    drp.IsActive,
    stock.BranchID AS StockBranchID,
    price.BranchID AS PriceBranchID
FROM Demo_Retail_Product drp
LEFT JOIN Demo_Retail_Variant drv ON drp.ProductID = drv.ProductID
LEFT JOIN Demo_Retail_Stock stock ON drv.VariantID = stock.VariantID AND (stock.BranchID = @BranchID OR stock.BranchID IS NULL)
LEFT JOIN Demo_Retail_Price price ON drp.ProductID = price.ProductID AND (price.BranchID = @BranchID OR price.BranchID IS NULL)
WHERE drp.SKU LIKE '%' + @SearchText + '%'
  AND drp.IsActive = 1
ORDER BY drp.Name

PRINT ''
PRINT '========================================='
PRINT 'CHECKING ALL PRODUCTS WITH 5057 IN SKU (NO FILTERS):'
PRINT '========================================='
PRINT ''

-- Check without any filters
SELECT 
    ProductID,
    SKU,
    Name,
    IsActive
FROM Demo_Retail_Product
WHERE SKU LIKE '%5057%'

PRINT ''
PRINT '========================================='
PRINT 'CHECKING STOCK FOR PRODUCTS WITH 5057:'
PRINT '========================================='
PRINT ''

-- Check stock
SELECT 
    drp.ProductID,
    drp.SKU,
    drp.Name,
    drv.VariantID,
    stock.BranchID,
    stock.QtyOnHand
FROM Demo_Retail_Product drp
LEFT JOIN Demo_Retail_Variant drv ON drp.ProductID = drv.ProductID
LEFT JOIN Demo_Retail_Stock stock ON drv.VariantID = stock.VariantID
WHERE drp.SKU LIKE '%5057%'

PRINT ''
PRINT '========================================='
PRINT 'CHECKING PRICES FOR PRODUCTS WITH 5057:'
PRINT '========================================='
PRINT ''

-- Check prices
SELECT 
    drp.ProductID,
    drp.SKU,
    drp.Name,
    price.BranchID,
    price.SellingPrice
FROM Demo_Retail_Product drp
LEFT JOIN Demo_Retail_Price price ON drp.ProductID = price.ProductID
WHERE drp.SKU LIKE '%5057%'

PRINT ''
PRINT '========================================='
PRINT 'END TEST'
PRINT '========================================='
