-- Check if products have prices and are synced across branches

-- 1. Products without prices
SELECT COUNT(*) AS ProductsWithoutPrices
FROM Demo_Retail_Product p
WHERE p.IsActive = 1
  AND NOT EXISTS (
      SELECT 1 FROM Demo_Retail_Price pr 
      WHERE pr.ProductID = p.ProductID
  );

-- 2. Show some products without prices
SELECT TOP 20 
    p.ProductID,
    p.SKU,
    p.Name,
    p.Category
FROM Demo_Retail_Product p
WHERE p.IsActive = 1
  AND NOT EXISTS (
      SELECT 1 FROM Demo_Retail_Price pr 
      WHERE pr.ProductID = p.ProductID
  );

-- 3. Check how many branches exist
SELECT BranchID, BranchName, IsActive 
FROM Branches 
WHERE IsActive = 1;

-- 4. Check price distribution across branches
SELECT 
    b.BranchID,
    b.BranchName,
    COUNT(DISTINCT pr.ProductID) AS ProductsWithPrices
FROM Branches b
LEFT JOIN Demo_Retail_Price pr ON pr.BranchID = b.BranchID
WHERE b.IsActive = 1
GROUP BY b.BranchID, b.BranchName
ORDER BY b.BranchID;

-- 5. Check stock distribution across branches
SELECT 
    b.BranchID,
    b.BranchName,
    COUNT(DISTINCT s.ProductID) AS ProductsWithStock
FROM Branches b
LEFT JOIN RetailStock s ON s.BranchID = b.BranchID
WHERE b.IsActive = 1
GROUP BY b.BranchID, b.BranchName
ORDER BY b.BranchID;

-- 6. Total active products
SELECT COUNT(*) AS TotalActiveProducts
FROM Demo_Retail_Product
WHERE IsActive = 1;
