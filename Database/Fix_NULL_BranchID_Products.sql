-- =============================================
-- Fix NULL BranchID in Demo_Retail_Product
-- Updates products with NULL BranchID based on their price records
-- =============================================

DECLARE @ErrMsg NVARCHAR(4000)
DECLARE @UpdatedCount INT = 0

BEGIN TRY
    BEGIN TRANSACTION

    PRINT '========================================='
    PRINT 'FIXING NULL BRANCHID IN DEMO_RETAIL_PRODUCT'
    PRINT '========================================='
    PRINT ''

    -- Show products with NULL BranchID
    PRINT 'Products with NULL BranchID:'
    SELECT 
        p.ProductID,
        p.SKU,
        p.Name,
        p.BranchID,
        COUNT(pr.PriceID) AS PriceRecordCount
    FROM Demo_Retail_Product p
    LEFT JOIN Demo_Retail_Price pr ON p.ProductID = pr.ProductID
    WHERE p.BranchID IS NULL
    GROUP BY p.ProductID, p.SKU, p.Name, p.BranchID
    ORDER BY p.Name

    PRINT ''
    PRINT 'Updating BranchID based on Demo_Retail_Price records...'
    PRINT ''

    -- Update BranchID in Demo_Retail_Product to match the BranchID from Demo_Retail_Price
    -- This handles products that have price records but NULL BranchID in product table
    UPDATE p
    SET p.BranchID = pr.BranchID
    FROM Demo_Retail_Product p
    INNER JOIN (
        -- Get the first BranchID for each product from price table
        SELECT 
            ProductID,
            MIN(BranchID) AS BranchID
        FROM Demo_Retail_Price
        WHERE BranchID IS NOT NULL
        GROUP BY ProductID
    ) pr ON p.ProductID = pr.ProductID
    WHERE p.BranchID IS NULL

    SET @UpdatedCount = @@ROWCOUNT

    PRINT '✓ Updated ' + CAST(@UpdatedCount AS NVARCHAR(10)) + ' product records with NULL BranchID'

    COMMIT TRANSACTION
    
    PRINT ''
    PRINT '========================================='
    PRINT 'SUCCESS: NULL BranchID values fixed'
    PRINT '========================================='
    PRINT ''
    PRINT 'IMPORTANT: Restart POS to reload product cache'

END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION
    SET @ErrMsg = ERROR_MESSAGE()
    PRINT 'ERROR: ' + @ErrMsg
    RAISERROR('%s', 16, 1, @ErrMsg)
    RETURN
END CATCH
GO

-- Verify - show any remaining products with NULL BranchID
PRINT ''
PRINT 'Verification - Products still with NULL BranchID:'
SELECT 
    p.ProductID,
    p.SKU,
    p.Name,
    p.BranchID
FROM Demo_Retail_Product p
WHERE p.BranchID IS NULL
ORDER BY p.Name
