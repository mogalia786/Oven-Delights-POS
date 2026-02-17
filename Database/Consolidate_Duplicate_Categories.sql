-- =============================================
-- Consolidate Duplicate Categories
-- Removes duplicates like BISCUIT/BISCUITS, multiple MISCELLANEOUS
-- =============================================

DECLARE @ErrMsg NVARCHAR(4000)

BEGIN TRY
    BEGIN TRANSACTION

    PRINT '========================================='
    PRINT 'CONSOLIDATING DUPLICATE CATEGORIES'
    PRINT '========================================='
    PRINT ''

    -- ============================================
    -- 1. BISCUIT vs BISCUITS - Keep BISCUITS
    -- ============================================
    DECLARE @BiscuitID INT, @BiscuitsID INT

    SELECT @BiscuitID = CategoryID 
    FROM Categories 
    WHERE LOWER(LTRIM(RTRIM(CategoryName))) = 'biscuit'

    SELECT @BiscuitsID = CategoryID 
    FROM Categories 
    WHERE LOWER(LTRIM(RTRIM(CategoryName))) = 'biscuits'

    IF @BiscuitID IS NOT NULL AND @BiscuitsID IS NOT NULL
    BEGIN
        PRINT 'Found BISCUIT (ID: ' + CAST(@BiscuitID AS NVARCHAR(10)) + ') and BISCUITS (ID: ' + CAST(@BiscuitsID AS NVARCHAR(10)) + ')'
        
        -- Move products from BISCUIT to BISCUITS
        UPDATE Demo_Retail_Product
        SET CategoryID = @BiscuitsID,
            Category = 'Biscuits'
        WHERE CategoryID = @BiscuitID

        PRINT '✓ Moved ' + CAST(@@ROWCOUNT AS NVARCHAR(10)) + ' products from BISCUIT to BISCUITS'

        -- Move subcategories
        UPDATE SubCategories
        SET CategoryID = @BiscuitsID
        WHERE CategoryID = @BiscuitID

        PRINT '✓ Moved subcategories from BISCUIT to BISCUITS'

        -- Deactivate BISCUIT
        UPDATE Categories
        SET IsActive = 0
        WHERE CategoryID = @BiscuitID

        PRINT '✓ Deactivated BISCUIT category'
        PRINT ''
    END
    ELSE
    BEGIN
        PRINT '✓ No BISCUIT/BISCUITS duplicate found'
        PRINT ''
    END

    -- ============================================
    -- 2. MISCELLANEOUS duplicates - Keep first active one
    -- ============================================
    DECLARE @PrimaryMiscID INT
    DECLARE @DuplicateMiscIDs TABLE (CategoryID INT)

    -- Get the primary (lowest ID) MISCELLANEOUS
    SELECT TOP 1 @PrimaryMiscID = CategoryID
    FROM Categories
    WHERE LOWER(LTRIM(RTRIM(CategoryName))) LIKE '%misc%'
      AND ISNULL(IsActive, 1) = 1
    ORDER BY CategoryID ASC

    -- Get all other MISCELLANEOUS categories
    INSERT INTO @DuplicateMiscIDs
    SELECT CategoryID
    FROM Categories
    WHERE LOWER(LTRIM(RTRIM(CategoryName))) LIKE '%misc%'
      AND CategoryID <> @PrimaryMiscID

    IF EXISTS (SELECT 1 FROM @DuplicateMiscIDs)
    BEGIN
        PRINT 'Found duplicate MISCELLANEOUS categories. Keeping ID: ' + CAST(@PrimaryMiscID AS NVARCHAR(10))

        -- Move products from duplicate MISCELLANEOUS to primary
        UPDATE Demo_Retail_Product
        SET CategoryID = @PrimaryMiscID,
            Category = 'Miscellaneous'
        WHERE CategoryID IN (SELECT CategoryID FROM @DuplicateMiscIDs)

        PRINT '✓ Moved ' + CAST(@@ROWCOUNT AS NVARCHAR(10)) + ' products to primary MISCELLANEOUS'

        -- Move subcategories from duplicates to primary
        UPDATE SubCategories
        SET CategoryID = @PrimaryMiscID
        WHERE CategoryID IN (SELECT CategoryID FROM @DuplicateMiscIDs)

        PRINT '✓ Moved subcategories to primary MISCELLANEOUS'

        -- Deactivate duplicate MISCELLANEOUS categories
        UPDATE Categories
        SET IsActive = 0
        WHERE CategoryID IN (SELECT CategoryID FROM @DuplicateMiscIDs)

        PRINT '✓ Deactivated ' + CAST(@@ROWCOUNT AS NVARCHAR(10)) + ' duplicate MISCELLANEOUS categories'
        PRINT ''
    END
    ELSE
    BEGIN
        PRINT '✓ No duplicate MISCELLANEOUS categories found'
        PRINT ''
    END

    -- ============================================
    -- 3. Remove CAKE top-level category
    -- ============================================
    DECLARE @CakeID INT

    SELECT @CakeID = CategoryID 
    FROM Categories 
    WHERE LOWER(LTRIM(RTRIM(CategoryName))) = 'cake'

    IF @CakeID IS NOT NULL
    BEGIN
        PRINT 'Found CAKE category (ID: ' + CAST(@CakeID AS NVARCHAR(10)) + ')'
        
        -- Check if there are products under CAKE category
        DECLARE @CakeProductCount INT
        SELECT @CakeProductCount = COUNT(*) 
        FROM Demo_Retail_Product 
        WHERE CategoryID = @CakeID

        IF @CakeProductCount > 0
        BEGIN
            PRINT 'WARNING: CAKE category has ' + CAST(@CakeProductCount AS NVARCHAR(10)) + ' products.'
            PRINT 'These products need to be reassigned to appropriate categories (Fresh Cream, Buttercream, etc.)'
            PRINT 'Skipping CAKE deactivation - please reassign products first.'
        END
        ELSE
        BEGIN
            -- Deactivate CAKE category
            UPDATE Categories
            SET IsActive = 0
            WHERE CategoryID = @CakeID

            PRINT '✓ Deactivated CAKE category (no products found)'
        END
        PRINT ''
    END
    ELSE
    BEGIN
        PRINT '✓ No CAKE category found'
        PRINT ''
    END

    -- ============================================
    -- 4. General duplicate detection (same name, different case/spacing)
    -- ============================================
    PRINT 'Checking for other potential duplicates...'
    PRINT ''

    -- Find categories with similar names (case-insensitive, trimmed)
    SELECT 
        LOWER(LTRIM(RTRIM(CategoryName))) AS NormalizedName,
        COUNT(*) AS DuplicateCount,
        STRING_AGG(CAST(CategoryID AS NVARCHAR(10)), ', ') AS CategoryIDs
    FROM Categories
    WHERE IsActive = 1
    GROUP BY LOWER(LTRIM(RTRIM(CategoryName)))
    HAVING COUNT(*) > 1

    COMMIT TRANSACTION
    
    PRINT ''
    PRINT '========================================='
    PRINT 'SUCCESS: Duplicate categories consolidated'
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

-- Verify final category list
PRINT ''
PRINT 'Active Categories:'
SELECT CategoryID, CategoryName, IsActive
FROM Categories
WHERE IsActive = 1
ORDER BY CategoryName
