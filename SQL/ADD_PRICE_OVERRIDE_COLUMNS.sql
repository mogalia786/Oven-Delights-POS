-- Add price override columns to POS_InvoiceLines table
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[POS_InvoiceLines]') AND name = 'OverriddenPrice')
BEGIN
    ALTER TABLE POS_InvoiceLines ADD OverriddenPrice DECIMAL(18,4) NULL
END

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[POS_InvoiceLines]') AND name = 'PriceOverrideBy')
BEGIN
    ALTER TABLE POS_InvoiceLines ADD PriceOverrideBy NVARCHAR(50) NULL
END

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[POS_InvoiceLines]') AND name = 'PriceOverrideDate')
BEGIN
    ALTER TABLE POS_InvoiceLines ADD PriceOverrideDate DATETIME NULL
END

-- Add price override columns to POS_CustomOrderItems table
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[POS_CustomOrderItems]') AND name = 'OverriddenPrice')
BEGIN
    ALTER TABLE POS_CustomOrderItems ADD OverriddenPrice DECIMAL(18,4) NULL
END

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[POS_CustomOrderItems]') AND name = 'PriceOverrideBy')
BEGIN
    ALTER TABLE POS_CustomOrderItems ADD PriceOverrideBy NVARCHAR(50) NULL
END

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[POS_CustomOrderItems]') AND name = 'PriceOverrideDate')
BEGIN
    ALTER TABLE POS_CustomOrderItems ADD PriceOverrideDate DATETIME NULL
END

PRINT 'Price override columns added successfully'
