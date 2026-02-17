-- COMPLETE BARCODE IMPORT SCRIPT
-- This script adds Barcode column and imports data from CSV in one go

-- Step 1: Add Barcode column if it doesn't exist
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS 
               WHERE TABLE_NAME = 'Demo_Retail_Product' AND COLUMN_NAME = 'Barcode')
BEGIN
    ALTER TABLE Demo_Retail_Product ADD Barcode NVARCHAR(50) NULL
    PRINT '✓ Added Barcode column to Demo_Retail_Product'
END
ELSE
BEGIN
    PRINT '✓ Barcode column already exists'
END

-- Step 2: Import CSV data using BULK INSERT
-- Create temp table
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

PRINT '✓ Created temp table #ItemBarcodes'

-- Import CSV using BULK INSERT
BEGIN TRY
    BULK INSERT #ItemBarcodes
    FROM 'C:\Development Apps\Cascades projects\Oven-Delights-ERP\Oven-Delights-ERP\Oven-Delights-ERP\Documentation\Copy of ITEM LIST NEW 2025 updated.csv'
    WITH (
        FIRSTROW = 2,
        FIELDTERMINATOR = ',',
        ROWTERMINATOR = '\n',
        TABLOCK,
        CODEPAGE = '65001'
    )
    PRINT '✓ CSV data imported successfully'
END TRY
BEGIN CATCH
    PRINT '✗ Error importing CSV: ' + ERROR_MESSAGE()
    PRINT ''
    PRINT 'If BULK INSERT fails, you need to:'
    PRINT '1. Enable xp_cmdshell, OR'
    PRINT '2. Use SQL Server Import Wizard, OR'
    PRINT '3. Run the Python script instead'
    -- Don't stop, continue to show what would happen
END CATCH

-- Step 3: Show how many rows were imported
DECLARE @RowCount INT
SELECT @RowCount = COUNT(*) FROM #ItemBarcodes
PRINT '✓ Imported ' + CAST(@RowCount AS VARCHAR) + ' rows from CSV'

-- Step 4: Update Demo_Retail_Product with barcodes
DECLARE @UpdatedCount INT = 0

UPDATE p
SET p.Barcode = csv.Barcode,
    @UpdatedCount = @UpdatedCount + 1
FROM Demo_Retail_Product p
INNER JOIN #ItemBarcodes csv ON p.SKU = csv.ItemCode
WHERE ISNULL(csv.Barcode, '') <> '' 
  AND csv.Barcode <> '0'

PRINT '✓ Updated ' + CAST(@UpdatedCount AS VARCHAR) + ' products with barcodes'

-- Step 5: Show summary
PRINT ''
PRINT '========================================='
PRINT 'SUMMARY'
PRINT '========================================='

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
PRINT ''
PRINT 'Sample products with barcodes:'
SELECT TOP 10
    SKU,
    Name,
    Barcode,
    Category
FROM Demo_Retail_Product
WHERE Barcode IS NOT NULL AND Barcode <> ''
ORDER BY Name

PRINT ''
PRINT '✓ Barcode import completed!'
PRINT ''
PRINT 'Next steps:'
PRINT '1. Run FIX_POS_PRODUCTS_VIEW.sql'
PRINT '2. Rebuild POS application'
PRINT '3. Barcodes will display on product cards'
