-- Import barcodes from CSV and update Demo_Retail_Product

-- Step 1: Check if Barcode column exists in Demo_Retail_Product, add if missing
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS 
               WHERE TABLE_NAME = 'Demo_Retail_Product' AND COLUMN_NAME = 'Barcode')
BEGIN
    ALTER TABLE Demo_Retail_Product ADD Barcode NVARCHAR(50) NULL
    PRINT 'Added Barcode column to Demo_Retail_Product'
END
ELSE
BEGIN
    PRINT 'Barcode column already exists in Demo_Retail_Product'
END

-- Step 2: Create temp table to hold CSV data
IF OBJECT_ID('tempdb..#ItemBarcodes') IS NOT NULL DROP TABLE #ItemBarcodes
CREATE TABLE #ItemBarcodes (
    ItemCode NVARCHAR(50),
    Barcode NVARCHAR(50),
    ItemDescription NVARCHAR(255),
    SubCategory NVARCHAR(100),
    MainCategory NVARCHAR(100),
    ItemCategory NVARCHAR(50),
    UnitOfMeasure NVARCHAR(20)
)

PRINT ''
PRINT '========================================='
PRINT 'STEP 3: IMPORT CSV DATA NOW'
PRINT '========================================='
PRINT 'File: Copy of ITEM LIST NEW 2025 updated.csv'
PRINT ''
PRINT 'Option A: Use SQL Server Import Wizard'
PRINT '  1. Right-click database > Tasks > Import Data'
PRINT '  2. Select CSV file'
PRINT '  3. Target: #ItemBarcodes'
PRINT '  4. Skip header row (FIRSTROW = 2)'
PRINT ''
PRINT 'Option B: Use BULK INSERT (uncomment below)'
PRINT ''
PRINT 'After importing CSV, run the rest of this script.'
PRINT '========================================='
PRINT ''

-- Uncomment and adjust path if using BULK INSERT:
/*
BULK INSERT #ItemBarcodes
FROM 'C:\Development Apps\Cascades projects\Oven-Delights-ERP\Oven-Delights-ERP\Oven-Delights-ERP\Documentation\Copy of ITEM LIST NEW 2025 updated.csv'
WITH (
    FIRSTROW = 2,
    FIELDTERMINATOR = ',',
    ROWTERMINATOR = '\n',
    TABLOCK
)
PRINT 'CSV imported successfully'
*/

-- Step 4: Update Demo_Retail_Product with barcodes from CSV (match by SKU = ItemCode)
UPDATE p
SET p.Barcode = CASE 
    WHEN ISNULL(csv.Barcode, '') = '' THEN NULL
    WHEN csv.Barcode = '0' THEN NULL
    ELSE csv.Barcode
END
FROM Demo_Retail_Product p
INNER JOIN #ItemBarcodes csv ON p.SKU = csv.ItemCode
WHERE ISNULL(csv.Barcode, '') <> '' AND csv.Barcode <> '0'

PRINT 'Updated barcodes in Demo_Retail_Product'

-- Step 5: Show summary of updates
SELECT 
    'Products with Barcodes' AS Status,
    COUNT(*) AS Count
FROM Demo_Retail_Product
WHERE Barcode IS NOT NULL AND Barcode <> ''

UNION ALL

SELECT 
    'Products without Barcodes' AS Status,
    COUNT(*) AS Count
FROM Demo_Retail_Product
WHERE Barcode IS NULL OR Barcode = ''

-- Step 6: Show sample of updated products
SELECT TOP 20
    SKU,
    Name,
    Barcode,
    Category
FROM Demo_Retail_Product
WHERE Barcode IS NOT NULL AND Barcode <> ''
ORDER BY Name

PRINT 'Barcode import completed!'
