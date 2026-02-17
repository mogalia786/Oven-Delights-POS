-- =============================================
-- Restore Category Display Order
-- Based on user's original layout
-- =============================================

DECLARE @ErrMsg NVARCHAR(4000)

BEGIN TRY
    BEGIN TRANSACTION

    PRINT '========================================='
    PRINT 'RESTORING CATEGORY DISPLAY ORDER'
    PRINT '========================================='
    PRINT ''

    -- Set DisplayOrder based on user's original layout
    -- Row 1 (positions 1-6)
    UPDATE Categories SET DisplayOrder = 1 WHERE LOWER(LTRIM(RTRIM(CategoryName))) = 'fresh cream'
    UPDATE Categories SET DisplayOrder = 2 WHERE LOWER(LTRIM(RTRIM(CategoryName))) = 'buttercream'
    UPDATE Categories SET DisplayOrder = 3 WHERE LOWER(LTRIM(RTRIM(CategoryName))) = 'exotic cakes'
    UPDATE Categories SET DisplayOrder = 4 WHERE LOWER(LTRIM(RTRIM(CategoryName))) = 'shop front'
    UPDATE Categories SET DisplayOrder = 5 WHERE LOWER(LTRIM(RTRIM(CategoryName))) = 'pies'
    UPDATE Categories SET DisplayOrder = 6 WHERE LOWER(LTRIM(RTRIM(CategoryName))) LIKE '%fresh cream%birthday%'

    -- Row 2 (positions 7-12)
    UPDATE Categories SET DisplayOrder = 7 WHERE LOWER(LTRIM(RTRIM(CategoryName))) LIKE '%buttercream%birthday%'
    UPDATE Categories SET DisplayOrder = 8 WHERE LOWER(LTRIM(RTRIM(CategoryName))) = 'novelty'
    UPDATE Categories SET DisplayOrder = 9 WHERE LOWER(LTRIM(RTRIM(CategoryName))) = 'biscuits'
    UPDATE Categories SET DisplayOrder = 10 WHERE LOWER(LTRIM(RTRIM(CategoryName))) = 'platter'
    UPDATE Categories SET DisplayOrder = 11 WHERE LOWER(LTRIM(RTRIM(CategoryName))) = 'savoury'
    UPDATE Categories SET DisplayOrder = 12 WHERE LOWER(LTRIM(RTRIM(CategoryName))) = 'drinks'

    -- Row 3 (positions 13-18)
    UPDATE Categories SET DisplayOrder = 13 WHERE LOWER(LTRIM(RTRIM(CategoryName))) = 'beverages'
    UPDATE Categories SET DisplayOrder = 14 WHERE LOWER(LTRIM(RTRIM(CategoryName))) = 'snacks'
    UPDATE Categories SET DisplayOrder = 15 WHERE LOWER(LTRIM(RTRIM(CategoryName))) = 'sweets'
    UPDATE Categories SET DisplayOrder = 16 WHERE LOWER(LTRIM(RTRIM(CategoryName))) LIKE '%wedding%cake%'
    UPDATE Categories SET DisplayOrder = 17 WHERE LOWER(LTRIM(RTRIM(CategoryName))) = 'fruitcake'
    -- Note: "candle" category will be deactivated by consolidation script

    -- Row 4 (position 19)
    UPDATE Categories SET DisplayOrder = 99 WHERE LOWER(LTRIM(RTRIM(CategoryName))) LIKE '%misc%'

    PRINT '✓ Display order updated for all categories'

    COMMIT TRANSACTION
    
    PRINT ''
    PRINT '========================================='
    PRINT 'SUCCESS: Category display order restored'
    PRINT '========================================='
    PRINT ''
    PRINT 'IMPORTANT: Restart POS to see changes'

END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION
    SET @ErrMsg = ERROR_MESSAGE()
    PRINT 'ERROR: ' + @ErrMsg
    RAISERROR('%s', 16, 1, @ErrMsg)
    RETURN
END CATCH
GO

-- Verify display order
PRINT ''
PRINT 'Active Categories (in display order):'
SELECT CategoryID, CategoryName, DisplayOrder, IsActive
FROM Categories
WHERE IsActive = 1
ORDER BY DisplayOrder, CategoryName
