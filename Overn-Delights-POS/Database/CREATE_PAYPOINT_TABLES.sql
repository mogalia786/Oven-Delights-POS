-- =============================================
-- Paypoint Payment Gateway Integration Tables
-- MiniPOS Cloud Gateway Support
-- =============================================

-- Store Paypoint configuration per branch
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'PaypointConfig')
BEGIN
    CREATE TABLE PaypointConfig (
        ConfigID INT IDENTITY(1,1) PRIMARY KEY,
        BranchID INT NOT NULL,
        MerchantID NVARCHAR(100),
        APIKey NVARCHAR(200) NOT NULL,
        ClientID NVARCHAR(100) NOT NULL,
        ClientSecret NVARCHAR(200) NOT NULL,
        IsTestMode BIT DEFAULT 1,
        IsActive BIT DEFAULT 1,
        CreatedDate DATETIME DEFAULT GETDATE(),
        UpdatedDate DATETIME,
        CONSTRAINT FK_PaypointConfig_Branch FOREIGN KEY (BranchID) REFERENCES Branches(BranchID)
    );
    
    CREATE INDEX IX_PaypointConfig_Branch ON PaypointConfig(BranchID);
    PRINT '✓ PaypointConfig table created';
END
ELSE
BEGIN
    PRINT '✓ PaypointConfig table already exists';
END
GO

-- Store Paypoint transactions
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'PaypointTransactions')
BEGIN
    CREATE TABLE PaypointTransactions (
        TransactionID INT IDENTITY(1,1) PRIMARY KEY,
        PaypointTransactionID NVARCHAR(100) NOT NULL, -- Paypoint's transaction ID
        POSTransactionID INT, -- Link to Demo_Sales
        InvoiceNumber NVARCHAR(50),
        TransactionType NVARCHAR(20) NOT NULL, -- 'SALE', 'REFUND', 'REVERSAL'
        Amount DECIMAL(18,2) NOT NULL,
        Currency NVARCHAR(3) DEFAULT 'ZAR',
        CardType NVARCHAR(50), -- 'VISA', 'MASTERCARD', 'AMEX', etc.
        CardLastFour NVARCHAR(4),
        CardholderName NVARCHAR(100),
        AuthCode NVARCHAR(50),
        Status NVARCHAR(20) NOT NULL, -- 'PENDING', 'APPROVED', 'DECLINED', 'FAILED', 'CANCELLED'
        StatusMessage NVARCHAR(500),
        RequestData NVARCHAR(MAX), -- JSON request sent to Paypoint
        ResponseData NVARCHAR(MAX), -- JSON response from Paypoint
        BranchID INT NOT NULL,
        TillNumber NVARCHAR(20),
        CashierID INT,
        CashierName NVARCHAR(100),
        CreatedDate DATETIME DEFAULT GETDATE(),
        UpdatedDate DATETIME,
        CompletedDate DATETIME,
        CONSTRAINT FK_PaypointTransactions_Branch FOREIGN KEY (BranchID) REFERENCES Branches(BranchID)
    );
    
    CREATE INDEX IX_PaypointTransactions_PaypointID ON PaypointTransactions(PaypointTransactionID);
    CREATE INDEX IX_PaypointTransactions_Invoice ON PaypointTransactions(InvoiceNumber);
    CREATE INDEX IX_PaypointTransactions_Branch ON PaypointTransactions(BranchID);
    CREATE INDEX IX_PaypointTransactions_Status ON PaypointTransactions(Status);
    CREATE INDEX IX_PaypointTransactions_Date ON PaypointTransactions(CreatedDate);
    PRINT '✓ PaypointTransactions table created';
END
ELSE
BEGIN
    PRINT '✓ PaypointTransactions table already exists';
END
GO

-- Store daily settlement reports
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'PaypointSettlements')
BEGIN
    CREATE TABLE PaypointSettlements (
        SettlementID INT IDENTITY(1,1) PRIMARY KEY,
        BranchID INT NOT NULL,
        SettlementDate DATE NOT NULL,
        SettlementReference NVARCHAR(100),
        TotalTransactions INT DEFAULT 0,
        TotalSales DECIMAL(18,2) DEFAULT 0,
        TotalRefunds DECIMAL(18,2) DEFAULT 0,
        NetAmount DECIMAL(18,2) DEFAULT 0,
        VisaCount INT DEFAULT 0,
        VisaAmount DECIMAL(18,2) DEFAULT 0,
        MastercardCount INT DEFAULT 0,
        MastercardAmount DECIMAL(18,2) DEFAULT 0,
        AmexCount INT DEFAULT 0,
        AmexAmount DECIMAL(18,2) DEFAULT 0,
        IsReconciled BIT DEFAULT 0,
        ReconciledDate DATETIME,
        ReconciledBy INT,
        Notes NVARCHAR(500),
        CreatedDate DATETIME DEFAULT GETDATE(),
        CONSTRAINT FK_PaypointSettlements_Branch FOREIGN KEY (BranchID) REFERENCES Branches(BranchID)
    );
    
    CREATE INDEX IX_PaypointSettlements_Branch ON PaypointSettlements(BranchID);
    CREATE INDEX IX_PaypointSettlements_Date ON PaypointSettlements(SettlementDate);
    PRINT '✓ PaypointSettlements table created';
END
ELSE
BEGIN
    PRINT '✓ PaypointSettlements table already exists';
END
GO

-- Store transaction audit log
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'PaypointAuditLog')
BEGIN
    CREATE TABLE PaypointAuditLog (
        AuditID INT IDENTITY(1,1) PRIMARY KEY,
        PaypointTransactionID NVARCHAR(100),
        Action NVARCHAR(50) NOT NULL, -- 'INITIATED', 'APPROVED', 'DECLINED', 'CANCELLED', 'REFUNDED', 'RESUMED'
        ActionBy INT,
        ActionByName NVARCHAR(100),
        BranchID INT,
        Details NVARCHAR(MAX),
        CreatedDate DATETIME DEFAULT GETDATE()
    );
    
    CREATE INDEX IX_PaypointAuditLog_TransactionID ON PaypointAuditLog(PaypointTransactionID);
    CREATE INDEX IX_PaypointAuditLog_Date ON PaypointAuditLog(CreatedDate);
    PRINT '✓ PaypointAuditLog table created';
END
ELSE
BEGIN
    PRINT '✓ PaypointAuditLog table already exists';
END
GO

-- =============================================
-- Stored Procedure: Get Paypoint Configuration
-- =============================================
IF EXISTS (SELECT * FROM sys.procedures WHERE name = 'sp_GetPaypointConfig')
    DROP PROCEDURE sp_GetPaypointConfig
GO

CREATE PROCEDURE sp_GetPaypointConfig
    @BranchID INT
AS
BEGIN
    SET NOCOUNT ON;
    
    SELECT 
        ConfigID,
        BranchID,
        MerchantID,
        APIKey,
        ClientID,
        ClientSecret,
        IsTestMode,
        IsActive
    FROM PaypointConfig
    WHERE BranchID = @BranchID
      AND IsActive = 1;
END
GO

PRINT '✓ sp_GetPaypointConfig procedure created';
GO

-- =============================================
-- Stored Procedure: Log Paypoint Transaction
-- =============================================
IF EXISTS (SELECT * FROM sys.procedures WHERE name = 'sp_LogPaypointTransaction')
    DROP PROCEDURE sp_LogPaypointTransaction
GO

CREATE PROCEDURE sp_LogPaypointTransaction
    @PaypointTransactionID NVARCHAR(100),
    @InvoiceNumber NVARCHAR(50),
    @TransactionType NVARCHAR(20),
    @Amount DECIMAL(18,2),
    @Status NVARCHAR(20),
    @StatusMessage NVARCHAR(500),
    @RequestData NVARCHAR(MAX),
    @ResponseData NVARCHAR(MAX),
    @BranchID INT,
    @TillNumber NVARCHAR(20),
    @CashierID INT,
    @CashierName NVARCHAR(100),
    @CardType NVARCHAR(50) = NULL,
    @CardLastFour NVARCHAR(4) = NULL,
    @AuthCode NVARCHAR(50) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    
    BEGIN TRY
        BEGIN TRANSACTION;
        
        -- Insert transaction
        INSERT INTO PaypointTransactions (
            PaypointTransactionID, InvoiceNumber, TransactionType, Amount,
            Status, StatusMessage, RequestData, ResponseData,
            BranchID, TillNumber, CashierID, CashierName,
            CardType, CardLastFour, AuthCode, CreatedDate
        )
        VALUES (
            @PaypointTransactionID, @InvoiceNumber, @TransactionType, @Amount,
            @Status, @StatusMessage, @RequestData, @ResponseData,
            @BranchID, @TillNumber, @CashierID, @CashierName,
            @CardType, @CardLastFour, @AuthCode, GETDATE()
        );
        
        -- Log audit entry
        INSERT INTO PaypointAuditLog (
            PaypointTransactionID, Action, ActionBy, ActionByName, BranchID, Details
        )
        VALUES (
            @PaypointTransactionID, 'INITIATED', @CashierID, @CashierName, @BranchID,
            CONCAT('Transaction initiated: ', @TransactionType, ' - Amount: R', @Amount)
        );
        
        COMMIT TRANSACTION;
        
        SELECT 'SUCCESS' AS Result, 'Transaction logged successfully' AS Message;
        
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0
            ROLLBACK TRANSACTION;
            
        SELECT 'ERROR' AS Result, ERROR_MESSAGE() AS Message;
    END CATCH
END
GO

PRINT '✓ sp_LogPaypointTransaction procedure created';
GO

-- =============================================
-- Stored Procedure: Update Transaction Status
-- =============================================
IF EXISTS (SELECT * FROM sys.procedures WHERE name = 'sp_UpdatePaypointTransactionStatus')
    DROP PROCEDURE sp_UpdatePaypointTransactionStatus
GO

CREATE PROCEDURE sp_UpdatePaypointTransactionStatus
    @PaypointTransactionID NVARCHAR(100),
    @Status NVARCHAR(20),
    @StatusMessage NVARCHAR(500),
    @ResponseData NVARCHAR(MAX) = NULL,
    @CardType NVARCHAR(50) = NULL,
    @CardLastFour NVARCHAR(4) = NULL,
    @AuthCode NVARCHAR(50) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    
    BEGIN TRY
        UPDATE PaypointTransactions
        SET Status = @Status,
            StatusMessage = @StatusMessage,
            ResponseData = ISNULL(@ResponseData, ResponseData),
            CardType = ISNULL(@CardType, CardType),
            CardLastFour = ISNULL(@CardLastFour, CardLastFour),
            AuthCode = ISNULL(@AuthCode, AuthCode),
            UpdatedDate = GETDATE(),
            CompletedDate = CASE WHEN @Status IN ('APPROVED', 'DECLINED', 'FAILED', 'CANCELLED') THEN GETDATE() ELSE CompletedDate END
        WHERE PaypointTransactionID = @PaypointTransactionID;
        
        -- Log audit entry
        INSERT INTO PaypointAuditLog (PaypointTransactionID, Action, Details)
        VALUES (@PaypointTransactionID, @Status, @StatusMessage);
        
        SELECT 'SUCCESS' AS Result, 'Status updated successfully' AS Message;
        
    END TRY
    BEGIN CATCH
        SELECT 'ERROR' AS Result, ERROR_MESSAGE() AS Message;
    END CATCH
END
GO

PRINT '✓ sp_UpdatePaypointTransactionStatus procedure created';
GO

-- =============================================
-- Sample Test Data (for testing only)
-- =============================================
PRINT '';
PRINT '=== PAYPOINT INTEGRATION TABLES CREATED ===';
PRINT 'Next steps:';
PRINT '1. Insert Paypoint configuration for each branch';
PRINT '2. Update POS PaymentTenderForm to use PaypointPaymentService';
PRINT '3. Test with Paypoint test environment';
PRINT '';
PRINT 'Sample configuration insert:';
PRINT 'INSERT INTO PaypointConfig (BranchID, MerchantID, APIKey, ClientID, ClientSecret, IsTestMode, IsActive)';
PRINT 'VALUES (1, ''MERCHANT123'', ''Q7w30FOnntfiLzJuKKJrKqVqXg9BHPCq'', ''your_client_id'', ''your_client_secret'', 1, 1);';
