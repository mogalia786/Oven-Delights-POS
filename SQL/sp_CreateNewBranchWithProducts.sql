-- =============================================
-- Stored Procedure: sp_CreateNewBranchWithProducts
-- Description: Creates a new branch and inherits all products with base prices
-- =============================================

CREATE OR ALTER PROCEDURE sp_CreateNewBranchWithProducts
    @BranchName NVARCHAR(100),
    @BranchCode NVARCHAR(20),
    @BranchAddress NVARCHAR(255) = NULL,
    @BranchPhone NVARCHAR(50) = NULL,
    @BranchEmail NVARCHAR(100) = NULL,
    @RegistrationNumber NVARCHAR(50) = NULL,
    @IsActive BIT = 1,
    @NewBranchID INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    
    BEGIN TRY
        BEGIN TRANSACTION;
        
        -- 1. Create the new branch
        INSERT INTO Branches (BranchName, BranchCode, BranchAddress, BranchPhone, BranchEmail, RegistrationNumber, IsActive, CreatedDate)
        VALUES (@BranchName, @BranchCode, @BranchAddress, @BranchPhone, @BranchEmail, @RegistrationNumber, @IsActive, GETDATE());
        
        SET @NewBranchID = SCOPE_IDENTITY();
        
        -- 2. Copy all products to Demo_Retail_Price for this branch (inherit base prices from BranchID IS NULL)
        INSERT INTO Demo_Retail_Price (ProductID, BranchID, SellingPrice, CostPrice, EffectiveFrom, EffectiveTo, IsActive, CreatedDate, CreatedBy)
        SELECT 
            drp.ProductID,
            @NewBranchID AS BranchID,
            ISNULL(basePrice.SellingPrice, 0) AS SellingPrice,
            ISNULL(basePrice.CostPrice, 0) AS CostPrice,
            GETDATE() AS EffectiveFrom,
            NULL AS EffectiveTo,
            1 AS IsActive,
            GETDATE() AS CreatedDate,
            'SYSTEM' AS CreatedBy
        FROM Demo_Retail_Product drp
        LEFT JOIN (
            -- Get base prices (where BranchID IS NULL)
            SELECT 
                ProductID,
                SellingPrice,
                CostPrice
            FROM Demo_Retail_Price
            WHERE BranchID IS NULL 
              AND IsActive = 1
              AND EffectiveFrom <= GETDATE() 
              AND (EffectiveTo IS NULL OR EffectiveTo >= GETDATE())
        ) basePrice ON basePrice.ProductID = drp.ProductID
        WHERE drp.IsActive = 1;
        
        -- 3. Create stock records with quantity 0 for all products in RetailStock
        INSERT INTO RetailStock (ProductID, BranchID, Quantity, LastUpdated)
        SELECT 
            ProductID,
            @NewBranchID,
            0 AS Quantity,
            GETDATE() AS LastUpdated
        FROM Demo_Retail_Product
        WHERE IsActive = 1;
        
        -- 4. Create initial till point for the branch
        INSERT INTO TillPoints (BranchID, TillNumber, TillName, IsActive, CreatedDate)
        VALUES (@NewBranchID, 1, 'Till 1', 1, GETDATE());
        
        COMMIT TRANSACTION;
        
        -- Return success message
        SELECT 
            @NewBranchID AS BranchID,
            @BranchName AS BranchName,
            'SUCCESS' AS Status,
            'Branch created successfully with ' + CAST(@@ROWCOUNT AS VARCHAR) + ' products inherited' AS Message;
            
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0
            ROLLBACK TRANSACTION;
            
        -- Return error
        SELECT 
            0 AS BranchID,
            @BranchName AS BranchName,
            'ERROR' AS Status,
            ERROR_MESSAGE() AS Message;
            
        THROW;
    END CATCH
END
GO
