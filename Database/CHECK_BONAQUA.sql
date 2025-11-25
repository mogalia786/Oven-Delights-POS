-- Check Bonaqua products in database

-- 1. Check if Bonaqua exists in Demo_Retail_Product
SELECT 'DEMO_RETAIL_PRODUCT' AS Source, 
    ProductID, SKU, Name, Category, ProductType, IsActive, BranchID, Barcode
FROM Demo_Retail_Product
WHERE Name LIKE '%Bonaqua%' OR SKU LIKE '%BON%' OR SKU LIKE '%BAA%' OR SKU LIKE '%BAN%'

-- 2. Check what the POS view would return
SELECT 'POS_VIEW_RESULT' AS Source,
    p.ProductID,
    p.SKU AS ItemCode,
    p.Name AS ProductName,
    ISNULL(p.Barcode, p.SKU) AS Barcode,
    p.Category,
    p.ProductType,
    p.IsActive,
    ISNULL(p.CurrentStock, 0) AS QtyOnHand
FROM Demo_Retail_Product p
WHERE (p.Name LIKE '%Bonaqua%' OR p.SKU LIKE '%BON%' OR p.SKU LIKE '%BAA%' OR p.SKU LIKE '%BAN%')

-- 3. Check if it meets POS filter criteria
SELECT 'MEETS_POS_CRITERIA' AS Source,
    p.ProductID,
    p.SKU,
    p.Name,
    p.IsActive,
    p.ProductType,
    p.Category,
    CASE WHEN p.IsActive = 1 THEN 'YES' ELSE 'NO' END AS IsActive_OK,
    CASE WHEN (p.ProductType = 'External' OR p.ProductType = 'Internal') THEN 'YES' ELSE 'NO' END AS ProductType_OK,
    CASE WHEN p.Category NOT IN ('ingredients', 'sub recipe', 'packaging', 'consumables', 'equipment', 'miscellaneous', 'pest control') THEN 'YES' ELSE 'NO' END AS Category_OK
FROM Demo_Retail_Product p
WHERE (p.Name LIKE '%Bonaqua%' OR p.SKU LIKE '%BON%' OR p.SKU LIKE '%BAA%' OR p.SKU LIKE '%BAN%')
