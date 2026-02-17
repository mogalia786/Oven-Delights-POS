-- Fix vw_POS_Products to show ALL products with proper ItemCode handling
-- This ensures ALL products from Demo_Retail_Product are visible in POS

USE Oven_Delights_Main
GO

IF OBJECT_ID('vw_POS_Products', 'V') IS NOT NULL
    DROP VIEW vw_POS_Products
GO

CREATE VIEW vw_POS_Products
AS
SELECT 
    p.ProductID,
    -- Use SKU if available, otherwise use ProductID as fallback
    ISNULL(NULLIF(p.SKU, ''), CAST(p.ProductID AS VARCHAR(20))) AS ItemCode,
    p.Name AS ProductName,
    ISNULL(pc.CategoryName, 'Uncategorized') AS Category,
    p.CategoryID,
    p.IsActive,
    s.StockID,
    s.BranchID,
    ISNULL(s.QtyOnHand, 0) AS QtyOnHand,
    ISNULL(s.ReorderPoint, 5) AS ReorderLevel,
    ISNULL(pr.SellingPrice, 0) AS SellingPrice,
    ISNULL(pr.CostPrice, 0) AS CostPrice
FROM Demo_Retail_Product p
-- Changed to LEFT JOIN so products without stock still show
LEFT JOIN Demo_Retail_Stock s ON p.ProductID = s.StockID
LEFT JOIN ProductCategories pc ON p.CategoryID = pc.CategoryID
LEFT JOIN Demo_Retail_Price pr ON p.ProductID = pr.ProductID AND (pr.BranchID = s.BranchID OR pr.BranchID IS NULL)
WHERE p.IsActive = 1
GO

PRINT 'vw_POS_Products view updated successfully!'
PRINT 'Now shows ALL active products with proper ItemCode handling'
PRINT 'Products without SKU will use ProductID as ItemCode'
GO
