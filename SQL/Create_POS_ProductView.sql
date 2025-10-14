-- Create a view that has all product information needed for POS
-- StockID in Demo_Retail_Stock actually represents ProductID

IF OBJECT_ID('vw_POS_Products', 'V') IS NOT NULL
    DROP VIEW vw_POS_Products
GO

CREATE VIEW vw_POS_Products
AS
SELECT 
    p.ProductID,
    p.SKU AS ItemCode,
    p.Name AS ProductName,
    pc.CategoryName AS Category,  -- Get category name from ProductCategories table
    p.CategoryID,
    p.IsActive,
    s.StockID,
    s.BranchID,
    s.QtyOnHand,
    ISNULL(pr.SellingPrice, 0) AS SellingPrice,  -- Price from Demo_Retail_Price
    ISNULL(pr.CostPrice, 0) AS CostPrice
FROM Demo_Retail_Product p
INNER JOIN Demo_Retail_Stock s ON p.ProductID = s.StockID  -- StockID = ProductID
LEFT JOIN ProductCategories pc ON p.CategoryID = pc.CategoryID  -- Join to get real category name
LEFT JOIN Demo_Retail_Price pr ON s.StockID = pr.ProductID  -- StockID matches ProductID in Price table
WHERE p.IsActive = 1
GO
