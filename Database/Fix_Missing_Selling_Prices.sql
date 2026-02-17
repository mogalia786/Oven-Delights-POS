-- =============================================
-- Fix Missing Selling Prices
-- Sets SellingPrice = CostPrice where SellingPrice is 0 or NULL
-- =============================================

DECLARE @ErrMsg NVARCHAR(4000)
DECLARE @UpdatedCount INT = 0

BEGIN TRY
    BEGIN TRANSACTION

    PRINT '========================================='
    PRINT 'FIXING MISSING SELLING PRICES'
    PRINT '========================================='
    PRINT ''

    -- Find products with missing selling prices
    PRINT 'Products with SellingPrice = 0 or NULL:'
    SELECT 
        p.Name AS ProductName,
        p.SKU,
        pr.BranchID,
        pr.CostPrice,
        pr.SellingPrice
    FROM Demo_Retail_Price pr
    INNER JOIN Demo_Retail_Product p ON pr.ProductID = p.ProductID
    WHERE (pr.SellingPrice IS NULL OR pr.SellingPrice = 0)
      AND pr.CostPrice > 0
    ORDER BY p.Name

    PRINT ''
    PRINT 'Updating SellingPrice to match CostPrice where SellingPrice is 0 or NULL...'
    PRINT ''

    -- Update SellingPrice to match CostPrice where SellingPrice is 0 or NULL
    UPDATE Demo_Retail_Price
    SET SellingPrice = CostPrice
    WHERE (SellingPrice IS NULL OR SellingPrice = 0)
      AND CostPrice > 0

    SET @UpdatedCount = @@ROWCOUNT

    PRINT '✓ Updated ' + CAST(@UpdatedCount AS NVARCHAR(10)) + ' price records'

    COMMIT TRANSACTION
    
    PRINT ''
    PRINT '========================================='
    PRINT 'SUCCESS: Selling prices fixed'
    PRINT '========================================='
    PRINT ''
    PRINT 'IMPORTANT: Restart POS to see updated prices'

END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION
    SET @ErrMsg = ERROR_MESSAGE()
    PRINT 'ERROR: ' + @ErrMsg
    RAISERROR('%s', 16, 1, @ErrMsg)
    RETURN
END CATCH
GO

-- Verify - show any remaining products with 0 or NULL selling prices
PRINT ''
PRINT 'Verification - Products still with SellingPrice = 0 or NULL:'
SELECT 
    p.Name AS ProductName,
    p.SKU,
    pr.BranchID,
    pr.CostPrice,
    pr.SellingPrice
FROM Demo_Retail_Price pr
INNER JOIN Demo_Retail_Product p ON pr.ProductID = p.ProductID
WHERE (pr.SellingPrice IS NULL OR pr.SellingPrice = 0)
ORDER BY p.Name
