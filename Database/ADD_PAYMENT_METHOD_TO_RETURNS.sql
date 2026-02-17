-- Add payment method tracking to POS_Returns table
-- This allows cashup to deduct returns from the correct payment method

-- Check if columns exist before adding
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('dbo.POS_Returns') AND name = 'PaymentMethod')
BEGIN
    ALTER TABLE dbo.POS_Returns
    ADD PaymentMethod NVARCHAR(20) NOT NULL DEFAULT 'Cash';
    
    PRINT 'PaymentMethod column added to POS_Returns';
END
ELSE
BEGIN
    PRINT 'PaymentMethod column already exists in POS_Returns';
END
GO

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('dbo.POS_Returns') AND name = 'CashAmount')
BEGIN
    ALTER TABLE dbo.POS_Returns
    ADD CashAmount DECIMAL(18,2) NOT NULL DEFAULT 0;
    
    PRINT 'CashAmount column added to POS_Returns';
END
ELSE
BEGIN
    PRINT 'CashAmount column already exists in POS_Returns';
END
GO

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('dbo.POS_Returns') AND name = 'CardAmount')
BEGIN
    ALTER TABLE dbo.POS_Returns
    ADD CardAmount DECIMAL(18,2) NOT NULL DEFAULT 0;
    
    PRINT 'CardAmount column added to POS_Returns';
END
ELSE
BEGIN
    PRINT 'CardAmount column already exists in POS_Returns';
END
GO

-- Update existing returns to have Cash payment method and amount
UPDATE dbo.POS_Returns
SET PaymentMethod = 'Cash',
    CashAmount = TotalAmount,
    CardAmount = 0
WHERE PaymentMethod IS NULL OR PaymentMethod = '';

PRINT 'Payment method columns added successfully to POS_Returns table';
