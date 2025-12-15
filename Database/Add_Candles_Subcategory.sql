-- =============================================
-- Consolidate Candles as a subcategory of Miscellaneous
-- Handles multiple Candles subcategories and "candle" category
-- =============================================

DECLARE @MiscCategoryID INT
DECLARE @CandlesSubCategoryID INT
DECLARE @CandlesCategoryID INT
DECLARE @ErrMsg NVARCHAR(4000)
DECLARE @ProductsMoved INT = 0

BEGIN TRY
    BEGIN TRANSACTION

    -- 1) Find the Miscellaneous category
    SELECT TOP 1 @MiscCategoryID = CategoryID
    FROM Categories
    WHERE LOWER(LTRIM(RTRIM(CategoryName))) LIKE '%misc%'
      AND ISNULL(IsActive, 1) = 1

    IF @MiscCategoryID IS NULL
    BEGIN
        RAISERROR('Miscellaneous category not found (active). Create/activate it first.', 16, 1)
    END

    PRINT 'Found Miscellaneous Category ID: ' + CAST(@MiscCategoryID AS NVARCHAR(10))

    -- 2) Get or create ONE Candles subcategory under Miscellaneous (keep lowest ID)
    SELECT TOP 1 @CandlesSubCategoryID = SubCategoryID
    FROM SubCategories
    WHERE LOWER(LTRIM(RTRIM(SubCategoryName))) = 'candles'
      AND CategoryID = @MiscCategoryID
    ORDER BY SubCategoryID ASC

    IF @CandlesSubCategoryID IS NULL
    BEGIN
        INSERT INTO SubCategories (CategoryID, SubCategoryName, IsActive)
        VALUES (@MiscCategoryID, 'Candles', 1)

        SET @CandlesSubCategoryID = SCOPE_IDENTITY()
        PRINT '✓ Candles subcategory created under Miscellaneous (ID: ' + CAST(@CandlesSubCategoryID AS NVARCHAR(10)) + ')'
    END
    ELSE
    BEGIN
        PRINT '✓ Using existing Candles subcategory under Miscellaneous (ID: ' + CAST(@CandlesSubCategoryID AS NVARCHAR(10)) + ')'
    END

    -- 3) Find "candle" or "candles" TOP-LEVEL category
    SELECT TOP 1 @CandlesCategoryID = CategoryID
    FROM Categories
    WHERE LOWER(LTRIM(RTRIM(CategoryName))) IN ('candle', 'candles')
      AND CategoryID <> @MiscCategoryID

    IF @CandlesCategoryID IS NOT NULL
    BEGIN
        PRINT 'Found Candle/Candles category ID: ' + CAST(@CandlesCategoryID AS NVARCHAR(10))

        -- 4) Move ALL products from "candle" category to Miscellaneous > Candles
        IF OBJECT_ID('Demo_Retail_Product', 'U') IS NOT NULL
        BEGIN
            UPDATE Demo_Retail_Product
            SET CategoryID = @MiscCategoryID,
                SubCategoryID = @CandlesSubCategoryID,
                Category = 'Miscellaneous'
            WHERE CategoryID = @CandlesCategoryID

            SET @ProductsMoved = @@ROWCOUNT
            PRINT '✓ Moved ' + CAST(@ProductsMoved AS NVARCHAR(10)) + ' products from Candle category to Miscellaneous > Candles'
        END

        -- 5) Move products from ANY Candles subcategory under the old "candle" category
        IF OBJECT_ID('Demo_Retail_Product', 'U') IS NOT NULL
        BEGIN
            UPDATE Demo_Retail_Product
            SET CategoryID = @MiscCategoryID,
                SubCategoryID = @CandlesSubCategoryID,
                Category = 'Miscellaneous'
            WHERE SubCategoryID IN (
                SELECT SubCategoryID 
                FROM SubCategories 
                WHERE CategoryID = @CandlesCategoryID
                  AND LOWER(LTRIM(RTRIM(SubCategoryName))) = 'candles'
            )

            SET @ProductsMoved = @@ROWCOUNT
            IF @ProductsMoved > 0
                PRINT '✓ Moved ' + CAST(@ProductsMoved AS NVARCHAR(10)) + ' products from old Candles subcategories'
        END

        -- 6) Also update master Products table if it exists
        IF OBJECT_ID('Products', 'U') IS NOT NULL
           AND COL_LENGTH('Products', 'CategoryID') IS NOT NULL
           AND COL_LENGTH('Products', 'SubcategoryID') IS NOT NULL
        BEGIN
            UPDATE Products
            SET CategoryID = @MiscCategoryID,
                SubcategoryID = @CandlesSubCategoryID
            WHERE CategoryID = @CandlesCategoryID

            PRINT '✓ Products table updated'
        END

        -- 7) Deactivate duplicate Candles subcategories (keep only the one we're using)
        UPDATE SubCategories
        SET IsActive = 0
        WHERE LOWER(LTRIM(RTRIM(SubCategoryName))) = 'candles'
          AND SubCategoryID <> @CandlesSubCategoryID

        PRINT '✓ Deactivated duplicate Candles subcategories'

        -- 8) Deactivate the "candle" top-level category
        UPDATE Categories
        SET IsActive = 0
        WHERE CategoryID = @CandlesCategoryID

        PRINT '✓ Candle/Candles top-level category deactivated (will no longer show on main panel)'
    END
    ELSE
    BEGIN
        PRINT '✓ No top-level Candle/Candles category found'
    END

    COMMIT TRANSACTION
    PRINT ''
    PRINT '========================================='
    PRINT 'SUCCESS: Candles consolidated under Miscellaneous'
    PRINT '========================================='
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION
    SET @ErrMsg = ERROR_MESSAGE()
    PRINT 'ERROR: ' + @ErrMsg
    RAISERROR('%s', 16, 1, @ErrMsg)
    RETURN
END CATCH

-- Verify
SELECT 
    c.CategoryName,
    sc.SubCategoryName,
    sc.SubCategoryID,
    sc.IsActive
FROM SubCategories sc
INNER JOIN Categories c ON sc.CategoryID = c.CategoryID
WHERE sc.SubCategoryName = 'Candles'

SELECT CategoryID, CategoryName, IsActive
FROM Categories
WHERE CategoryName = 'Candles' OR CategoryName LIKE '%misc%'
