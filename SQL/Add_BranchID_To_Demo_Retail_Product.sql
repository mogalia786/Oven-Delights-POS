-- Add BranchID column to Demo_Retail_Product table

-- Check if Demo_Retail_Product table exists
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Demo_Retail_Product')
BEGIN
    PRINT 'ERROR: Demo_Retail_Product table does not exist!'
    PRINT 'Please check the actual table name in your database.'
    RETURN
END

PRINT 'Demo_Retail_Product table found'

-- Add BranchID column to Demo_Retail_Product
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Demo_Retail_Product') AND name = 'BranchID')
BEGIN
    PRINT 'Adding BranchID column...'
    ALTER TABLE Demo_Retail_Product ADD BranchID INT NULL
    PRINT 'BranchID column added successfully'
END
ELSE
BEGIN
    PRINT 'BranchID column already exists'
END
GO

-- Now update existing records (separate batch after column is added)
IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Demo_Retail_Product') AND name = 'BranchID')
BEGIN
    PRINT 'Updating existing records with default BranchID = 1...'
    
    -- Set all existing products to BranchID = 1 (default branch)
    UPDATE Demo_Retail_Product 
    SET BranchID = 1
    WHERE BranchID IS NULL
    
    PRINT 'Updated ' + CAST(@@ROWCOUNT AS VARCHAR) + ' records with BranchID = 1'
END
GO

PRINT 'Demo_Retail_Product BranchID setup completed!'
GO
