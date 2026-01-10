-- =============================================
-- Add Cost of Sales Tracking to POS System
-- =============================================
-- POS must track cost of sales for accounting entries
-- Cost is NOT displayed to users, only used for ledger/journal posting
-- =============================================

PRINT '📊 Adding Cost of Sales tracking to POS tables...';
PRINT '';

-- =============================================
-- 1. Add CostOfSales column to Demo_Sales table
-- =============================================
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Demo_Sales') AND name = 'CostOfSales')
BEGIN
    ALTER TABLE Demo_Sales
    ADD CostOfSales DECIMAL(18,2) DEFAULT 0;
    
    PRINT '✅ Added CostOfSales column to Demo_Sales';
END
ELSE
BEGIN
    PRINT '⚠️  CostOfSales column already exists in Demo_Sales';
END
GO

-- =============================================
-- 2. Add cost tracking to POS_InvoiceLines table
-- =============================================
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('POS_InvoiceLines') AND name = 'UnitCost')
BEGIN
    ALTER TABLE POS_InvoiceLines
    ADD UnitCost DECIMAL(18,6) DEFAULT 0,
        LineCost DECIMAL(18,2) DEFAULT 0;
    
    PRINT '✅ Added UnitCost and LineCost columns to POS_InvoiceLines';
END
ELSE
BEGIN
    PRINT '⚠️  Cost columns already exist in POS_InvoiceLines';
END
GO

-- =============================================
-- 3. Add cost tracking to Invoices table (ERP integration)
-- =============================================
IF EXISTS (SELECT * FROM sys.tables WHERE name = 'Invoices')
BEGIN
    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Invoices') AND name = 'UnitCost')
    BEGIN
        ALTER TABLE Invoices
        ADD UnitCost DECIMAL(18,6) DEFAULT 0,
            LineCost DECIMAL(18,2) DEFAULT 0;
        
        PRINT '✅ Added UnitCost and LineCost columns to Invoices';
    END
    ELSE
    BEGIN
        PRINT '⚠️  Cost columns already exist in Invoices';
    END
END
GO

-- =============================================
-- 4. Create stored procedure to get product cost
-- =============================================
IF OBJECT_ID('sp_GetProductCostForPOS', 'P') IS NOT NULL
    DROP PROCEDURE sp_GetProductCostForPOS;
GO

CREATE PROCEDURE sp_GetProductCostForPOS
    @ProductID INT,
    @BranchID INT
AS
BEGIN
    SET NOCOUNT ON;
    
    -- Get cost from Demo_Retail_Price (set by manufacturing or purchase orders)
    SELECT ISNULL(CostPrice, 0) AS CostPrice
    FROM Demo_Retail_Price
    WHERE ProductID = @ProductID 
      AND BranchID = @BranchID;
    
    -- If no price record exists, return 0
    IF @@ROWCOUNT = 0
        SELECT 0 AS CostPrice;
END
GO

PRINT '✅ Created sp_GetProductCostForPOS';
PRINT '';

-- =============================================
-- 5. Create stored procedure to post accounting entries
-- =============================================
IF OBJECT_ID('sp_PostSaleAccountingEntries', 'P') IS NOT NULL
    DROP PROCEDURE sp_PostSaleAccountingEntries;
GO

CREATE PROCEDURE sp_PostSaleAccountingEntries
    @SalesID INT,
    @InvoiceNumber NVARCHAR(50),
    @BranchID INT,
    @SaleDate DATETIME,
    @TotalAmount DECIMAL(18,2),
    @CostOfSales DECIMAL(18,2),
    @PaymentMethod NVARCHAR(50)
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRANSACTION;
    
    BEGIN TRY
        DECLARE @JournalBatchID INT;
        DECLARE @TransactionDate DATETIME = @SaleDate;
        
        -- Create journal batch for this sale
        INSERT INTO JournalBatches (BatchNumber, BatchDate, Description, Status, CreatedBy, CreatedDate)
        VALUES (
            'POS-' + @InvoiceNumber,
            @TransactionDate,
            'POS Sale - Invoice ' + @InvoiceNumber,
            'Posted',
            'POS System',
            GETDATE()
        );
        
        SET @JournalBatchID = SCOPE_IDENTITY();
        
        -- =============================================
        -- ACCOUNTING ENTRIES FOR SALE
        -- =============================================
        
        -- 1. DR Cash/Bank (or Accounts Receivable) - DEBIT
        INSERT INTO JournalEntries (JournalBatchID, AccountCode, AccountName, DebitAmount, CreditAmount, Description, TransactionDate, ReferenceType, ReferenceNumber, BranchID)
        VALUES (
            @JournalBatchID,
            CASE @PaymentMethod 
                WHEN 'Cash' THEN '1010' -- Cash Account
                WHEN 'Card' THEN '1020' -- Bank Account
                WHEN 'EFT' THEN '1020'  -- Bank Account
                ELSE '1100' -- Accounts Receivable
            END,
            CASE @PaymentMethod 
                WHEN 'Cash' THEN 'Cash on Hand'
                WHEN 'Card' THEN 'Bank - Card Payments'
                WHEN 'EFT' THEN 'Bank - EFT Payments'
                ELSE 'Accounts Receivable'
            END,
            @TotalAmount,  -- DEBIT
            0,             -- CREDIT
            'Sale - Invoice ' + @InvoiceNumber,
            @TransactionDate,
            'Sale',
            @InvoiceNumber,
            @BranchID
        );
        
        -- 2. CR Sales Revenue - CREDIT
        INSERT INTO JournalEntries (JournalBatchID, AccountCode, AccountName, DebitAmount, CreditAmount, Description, TransactionDate, ReferenceType, ReferenceNumber, BranchID)
        VALUES (
            @JournalBatchID,
            '4000', -- Sales Revenue
            'Sales Revenue',
            0,              -- DEBIT
            @TotalAmount,   -- CREDIT
            'Sale - Invoice ' + @InvoiceNumber,
            @TransactionDate,
            'Sale',
            @InvoiceNumber,
            @BranchID
        );
        
        -- 3. DR Cost of Sales - DEBIT (Expense)
        INSERT INTO JournalEntries (JournalBatchID, AccountCode, AccountName, DebitAmount, CreditAmount, Description, TransactionDate, ReferenceType, ReferenceNumber, BranchID)
        VALUES (
            @JournalBatchID,
            '5000', -- Cost of Sales
            'Cost of Sales',
            @CostOfSales,  -- DEBIT
            0,             -- CREDIT
            'Cost of Sale - Invoice ' + @InvoiceNumber,
            @TransactionDate,
            'Sale',
            @InvoiceNumber,
            @BranchID
        );
        
        -- 4. CR Inventory - CREDIT (Asset reduction)
        INSERT INTO JournalEntries (JournalBatchID, AccountCode, AccountName, DebitAmount, CreditAmount, Description, TransactionDate, ReferenceType, ReferenceNumber, BranchID)
        VALUES (
            @JournalBatchID,
            '1300', -- Inventory
            'Inventory - Finished Goods',
            0,             -- DEBIT
            @CostOfSales,  -- CREDIT
            'Inventory Reduction - Invoice ' + @InvoiceNumber,
            @TransactionDate,
            'Sale',
            @InvoiceNumber,
            @BranchID
        );
        
        COMMIT TRANSACTION;
        
        SELECT 'SUCCESS' AS Result, @JournalBatchID AS JournalBatchID;
        
    END TRY
    BEGIN CATCH
        IF XACT_STATE() <> 0
            ROLLBACK TRANSACTION;
        
        DECLARE @ErrorMessage NVARCHAR(4000) = ERROR_MESSAGE();
        RAISERROR(@ErrorMessage, 16, 1);
    END CATCH
END
GO

PRINT '✅ Created sp_PostSaleAccountingEntries';
PRINT '';
PRINT '═══════════════════════════════════════════════';
PRINT '📋 SUMMARY OF CHANGES:';
PRINT '';
PRINT '1. ✅ Demo_Sales.CostOfSales - Tracks total cost per sale';
PRINT '2. ✅ POS_InvoiceLines.UnitCost, LineCost - Tracks cost per line item';
PRINT '3. ✅ Invoices.UnitCost, LineCost - ERP integration';
PRINT '4. ✅ sp_GetProductCostForPOS - Retrieves cost from Demo_Retail_Price';
PRINT '5. ✅ sp_PostSaleAccountingEntries - Posts to ledgers/journals';
PRINT '';
PRINT '🔄 ACCOUNTING ENTRIES POSTED FOR EACH SALE:';
PRINT '   DR Cash/Bank/AR     R100.00  (Asset increase)';
PRINT '   CR Sales Revenue    R100.00  (Revenue)';
PRINT '   DR Cost of Sales    R63.33   (Expense)';
PRINT '   CR Inventory        R63.33   (Asset decrease)';
PRINT '';
PRINT '💡 PROFIT CALCULATION:';
PRINT '   Gross Profit = Sales Revenue - Cost of Sales';
PRINT '   Example: R100.00 - R63.33 = R36.67';
PRINT '';
PRINT '⚠️  IMPORTANT: Cost is NOT displayed in POS UI';
PRINT '   Cost is only used for accounting entries';
PRINT '   Cashiers/users never see cost prices';
PRINT '═══════════════════════════════════════════════';
GO
