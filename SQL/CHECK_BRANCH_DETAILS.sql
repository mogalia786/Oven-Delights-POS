-- Check Branch Details for Receipt Display
-- =========================================

-- 1. Check if Branches table exists
IF EXISTS (SELECT * FROM sys.tables WHERE name = 'Branches')
BEGIN
    PRINT '✓ Branches table exists'
    PRINT ''
    
    -- 2. Show all branches with their details
    PRINT 'All Branches:'
    SELECT 
        BranchID,
        BranchName,
        Address,
        Phone,
        IsActive
    FROM Branches
    ORDER BY BranchID;
    
    -- 3. Check for NULL or empty values
    PRINT ''
    PRINT 'Branches with missing details:'
    SELECT 
        BranchID,
        BranchName,
        CASE 
            WHEN Address IS NULL OR LTRIM(RTRIM(Address)) = '' THEN '❌ Missing Address'
            ELSE '✓ Has Address'
        END AS AddressStatus,
        CASE 
            WHEN Phone IS NULL OR LTRIM(RTRIM(Phone)) = '' THEN '❌ Missing Phone'
            ELSE '✓ Has Phone'
        END AS PhoneStatus
    FROM Branches;
    
    -- 4. Sample receipt format
    PRINT ''
    PRINT 'Sample Receipt Format:'
    PRINT '========================================'
    PRINT '*** PICKUP LOCATION ***'
    
    DECLARE @Name NVARCHAR(100), @Address NVARCHAR(200), @Phone NVARCHAR(20)
    
    SELECT TOP 1
        @Name = ISNULL(BranchName, 'BRANCH'),
        @Address = ISNULL(Address, 'Address not available'),
        @Phone = ISNULL(Phone, 'Phone not available')
    FROM Branches
    WHERE IsActive = 1
    ORDER BY BranchID;
    
    PRINT @Name
    PRINT @Address
    PRINT 'Tel: ' + @Phone
    PRINT '========================================'
    
END
ELSE
BEGIN
    PRINT '✗ Branches table does NOT exist!'
END
GO
