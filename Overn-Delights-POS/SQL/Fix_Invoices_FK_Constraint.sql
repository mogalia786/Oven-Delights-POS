-- Fix FK constraint in Invoices table to reference Demo_Sales instead of Sales
-- This allows POS to use Demo_Sales table while maintaining referential integrity

USE Oven_Delights_Main
GO

-- Drop the old FK constraint if it exists
IF EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_Invoices_Sales')
BEGIN
    ALTER TABLE Invoices DROP CONSTRAINT FK_Invoices_Sales
    PRINT 'Dropped old FK_Invoices_Sales constraint (was pointing to Sales table)'
END
GO

-- Add new FK constraint pointing to Demo_Sales
IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_Invoices_DemoSales')
BEGIN
    ALTER TABLE Invoices 
    ADD CONSTRAINT FK_Invoices_DemoSales 
    FOREIGN KEY (SalesID) REFERENCES Demo_Sales(SaleID)
    
    PRINT 'Added new FK_Invoices_DemoSales constraint (now points to Demo_Sales table)'
END
GO

PRINT 'FK constraint fixed successfully!'
PRINT 'Invoices.SalesID now references Demo_Sales.SaleID'
GO
