-- =============================================
-- SIMPLIFIED Cost of Sales Tracking
-- Saves cost data without posting to accounting tables
-- (Accounting integration can be added later)
-- =============================================

PRINT '📊 Fixing Cost of Sales tracking...';
PRINT '';

-- Drop the problematic stored procedure
IF OBJECT_ID('sp_PostSaleAccountingEntries', 'P') IS NOT NULL
    DROP PROCEDURE sp_PostSaleAccountingEntries;
GO

PRINT '✅ Removed sp_PostSaleAccountingEntries (accounting tables not ready)';
PRINT '';
GO

-- =============================================
-- Create simplified stored procedure
-- Just retrieves cost - no accounting posting
-- =============================================
CREATE PROCEDURE sp_GetProductCostForSale
    @ProductID INT,
    @BranchID INT
AS
BEGIN
    SET NOCOUNT ON;
    
    -- Get cost from Demo_Retail_Price (set by manufacturing or purchase orders)
    DECLARE @CostPrice DECIMAL(18,6) = 0;
    
    SELECT @CostPrice = ISNULL(CostPrice, 0)
    FROM Demo_Retail_Price
    WHERE ProductID = @ProductID 
      AND BranchID = @BranchID;
    
    -- Return cost (0 if no price record exists)
    SELECT ISNULL(@CostPrice, 0) AS CostPrice;
END
GO

PRINT '✅ Created sp_GetProductCostForSale';
PRINT '';
PRINT '═══════════════════════════════════════════════';
PRINT '📋 CURRENT SETUP:';
PRINT '';
PRINT '1. ✅ Demo_Sales.CostOfSales - Tracks total cost per sale';
PRINT '2. ✅ POS_InvoiceLines.UnitCost, LineCost - Tracks cost per line';
PRINT '3. ✅ sp_GetProductCostForSale - Retrieves cost from database';
PRINT '';
PRINT '⚠️  ACCOUNTING INTEGRATION:';
PRINT '   Accounting posting to ledgers/journals is DISABLED';
PRINT '   Cost data is saved but not posted to accounting tables';
PRINT '   This can be enabled later when accounting tables are ready';
PRINT '';
PRINT '🔄 POS INTEGRATION STEPS:';
PRINT '   1. POS retrieves cost using sp_GetProductCostForSale';
PRINT '   2. POS saves cost to Demo_Sales.CostOfSales';
PRINT '   3. POS saves line costs to POS_InvoiceLines';
PRINT '   4. Cost is NEVER displayed to users';
PRINT '';
PRINT '💡 COST DATA AVAILABLE FOR:';
PRINT '   - Profit reports (Revenue - Cost of Sales)';
PRINT '   - Inventory valuation';
PRINT '   - Product profitability analysis';
PRINT '   - Future accounting integration';
PRINT '═══════════════════════════════════════════════';
GO
