-- Stored Procedure: Post POS Sale to General Ledger
-- ====================================================
-- Posts sales transactions to GL with proper double-entry accounting

IF EXISTS (SELECT * FROM sys.objects WHERE type = 'P' AND name = 'sp_PostPOSSaleToGL')
    DROP PROCEDURE sp_PostPOSSaleToGL;
GO

CREATE PROCEDURE sp_PostPOSSaleToGL
    @TransactionID INT,
    @PostedBy NVARCHAR(100)
AS
BEGIN
    SET NOCOUNT ON;
    
    BEGIN TRY
        BEGIN TRANSACTION;
        
        -- Get transaction details
        DECLARE @TransactionNumber NVARCHAR(50);
        DECLARE @BranchID INT;
        DECLARE @TransactionDate DATETIME;
        DECLARE @PaymentMethod NVARCHAR(20);
        DECLARE @TotalAmount DECIMAL(18,2);
        DECLARE @CashAmount DECIMAL(18,2);
        DECLARE @CardAmount DECIMAL(18,2);
        DECLARE @GLPosted BIT;
        
        SELECT 
            @TransactionNumber = TransactionNumber,
            @BranchID = BranchID,
            @TransactionDate = TransactionDate,
            @PaymentMethod = PaymentMethod,
            @TotalAmount = TotalAmount,
            @CashAmount = ISNULL(CashAmount, 0),
            @CardAmount = ISNULL(CardAmount, 0),
            @GLPosted = GLPosted
        FROM POS_Transactions
        WHERE TransactionID = @TransactionID;
        
        -- Check if already posted
        IF @GLPosted = 1
        BEGIN
            RAISERROR('Transaction already posted to GL', 16, 1);
            RETURN;
        END
        
        -- Get GL account mappings for this branch
        DECLARE @SalesAccount NVARCHAR(20);
        DECLARE @SalesAccountName NVARCHAR(200);
        DECLARE @CashAccount NVARCHAR(20);
        DECLARE @CashAccountName NVARCHAR(200);
        DECLARE @CardAccount NVARCHAR(20);
        DECLARE @CardAccountName NVARCHAR(200);
        DECLARE @COSAccount NVARCHAR(20);
        DECLARE @COSAccountName NVARCHAR(200);
        DECLARE @InventoryAccount NVARCHAR(20);
        DECLARE @InventoryAccountName NVARCHAR(200);
        
        SELECT @SalesAccount = AccountCode, @SalesAccountName = AccountName
        FROM POS_GLAccountMapping WHERE BranchID = @BranchID AND AccountType = 'Sales' AND IsActive = 1;
        
        SELECT @CashAccount = AccountCode, @CashAccountName = AccountName
        FROM POS_GLAccountMapping WHERE BranchID = @BranchID AND AccountType = 'Cash' AND IsActive = 1;
        
        SELECT @CardAccount = AccountCode, @CardAccountName = AccountName
        FROM POS_GLAccountMapping WHERE BranchID = @BranchID AND AccountType = 'Card' AND IsActive = 1;
        
        SELECT @COSAccount = AccountCode, @COSAccountName = AccountName
        FROM POS_GLAccountMapping WHERE BranchID = @BranchID AND AccountType = 'CostOfSales' AND IsActive = 1;
        
        SELECT @InventoryAccount = AccountCode, @InventoryAccountName = AccountName
        FROM POS_GLAccountMapping WHERE BranchID = @BranchID AND AccountType = 'Inventory' AND IsActive = 1;
        
        -- JOURNAL ENTRY 1: Record Sale
        -- DR Cash/Card → CR Sales Revenue
        
        IF @CashAmount > 0
        BEGIN
            -- DR Cash
            INSERT INTO GeneralJournal (TransactionDate, Reference, AccountCode, AccountName, Debit, Credit, Description, BranchID, CreatedBy, CreatedDate)
            VALUES (@TransactionDate, @TransactionNumber, @CashAccount, @CashAccountName, @CashAmount, 0, 'POS Cash Sale', @BranchID, @PostedBy, GETDATE());
        END
        
        IF @CardAmount > 0
        BEGIN
            -- DR Card
            INSERT INTO GeneralJournal (TransactionDate, Reference, AccountCode, AccountName, Debit, Credit, Description, BranchID, CreatedBy, CreatedDate)
            VALUES (@TransactionDate, @TransactionNumber, @CardAccount, @CardAccountName, @CardAmount, 0, 'POS Card Sale', @BranchID, @PostedBy, GETDATE());
        END
        
        -- CR Sales Revenue
        INSERT INTO GeneralJournal (TransactionDate, Reference, AccountCode, AccountName, Debit, Credit, Description, BranchID, CreatedBy, CreatedDate)
        VALUES (@TransactionDate, @TransactionNumber, @SalesAccount, @SalesAccountName, 0, @TotalAmount, 'POS Sale Revenue', @BranchID, @PostedBy, GETDATE());
        
        -- JOURNAL ENTRY 2: Record Cost of Sales
        -- DR Cost of Sales → CR Inventory
        
        DECLARE @TotalCostOfSales DECIMAL(18,2) = 0;
        
        -- Calculate total cost of sales from line items
        SELECT @TotalCostOfSales = SUM(ISNULL(CostPrice, 0) * Quantity)
        FROM POS_TransactionItems
        WHERE TransactionID = @TransactionID;
        
        IF @TotalCostOfSales > 0
        BEGIN
            -- DR Cost of Sales
            INSERT INTO GeneralJournal (TransactionDate, Reference, AccountCode, AccountName, Debit, Credit, Description, BranchID, CreatedBy, CreatedDate)
            VALUES (@TransactionDate, @TransactionNumber, @COSAccount, @COSAccountName, @TotalCostOfSales, 0, 'Cost of Goods Sold', @BranchID, @PostedBy, GETDATE());
            
            -- CR Inventory
            INSERT INTO GeneralJournal (TransactionDate, Reference, AccountCode, AccountName, Debit, Credit, Description, BranchID, CreatedBy, CreatedDate)
            VALUES (@TransactionDate, @TransactionNumber, @InventoryAccount, @InventoryAccountName, 0, @TotalCostOfSales, 'Inventory Reduction', @BranchID, @PostedBy, GETDATE());
        END
        
        -- Update transaction as posted
        UPDATE POS_Transactions
        SET GLPosted = 1,
            GLPostDate = GETDATE()
        WHERE TransactionID = @TransactionID;
        
        -- Update inventory quantities
        UPDATE rs
        SET rs.QtyOnHand = rs.QtyOnHand - ti.Quantity
        FROM Retail_Stock rs
        INNER JOIN POS_TransactionItems ti ON rs.VariantID = ti.VariantID
        WHERE ti.TransactionID = @TransactionID
        AND rs.BranchID = @BranchID;
        
        COMMIT TRANSACTION;
        
        PRINT '✓ Transaction ' + @TransactionNumber + ' posted to GL successfully';
        PRINT '  Sales Revenue: R' + CAST(@TotalAmount AS NVARCHAR(20));
        PRINT '  Cost of Sales: R' + CAST(@TotalCostOfSales AS NVARCHAR(20));
        PRINT '  Gross Profit: R' + CAST((@TotalAmount - @TotalCostOfSales) AS NVARCHAR(20));
        
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0
            ROLLBACK TRANSACTION;
            
        DECLARE @ErrorMessage NVARCHAR(4000) = ERROR_MESSAGE();
        RAISERROR(@ErrorMessage, 16, 1);
    END CATCH
END
GO

PRINT '✓ Stored procedure sp_PostPOSSaleToGL created successfully';
GO
