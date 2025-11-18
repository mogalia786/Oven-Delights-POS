-- ========================================
-- POS GENERAL LEDGER INTEGRATION SETUP
-- ========================================
-- Complete guide to enable GL posting for POS sales

-- CURRENT STATUS:
-- ✓ GL posting code EXISTS in PaymentTenderForm.vb
-- ✓ PostToJournalsAndLedgers method is implemented
-- ✗ GL posting is DISABLED (line 1309-1311: "Skip journal posting for now")
-- ✗ Cost of Sales calculation returns 0 (line 1316: "Return 0D")

-- WHAT HAPPENS NOW:
-- When a sale is made:
-- 1. ✓ Sale is recorded
-- 2. ✓ Receipt is printed
-- 3. ✗ NO GL posting (disabled)
-- 4. ✗ NO Cost of Sales calculation
-- 5. ✗ NO inventory reduction

-- ========================================
-- STEP-BY-STEP SETUP INSTRUCTIONS
-- ========================================

-- STEP 1: Run these SQL scripts in order
-- ========================================

-- 1.1 Create POS transaction tables
PRINT 'Step 1.1: Creating POS_Transactions tables...';
-- Run: CREATE_POS_TRANSACTIONS_TABLE.sql
GO

-- 1.2 Create GL account mapping
PRINT 'Step 1.2: Creating GL account mappings...';
-- Run: CREATE_POS_GL_ACCOUNTS.sql
GO

-- 1.3 Create GL posting stored procedure
PRINT 'Step 1.3: Creating GL posting procedure...';
-- Run: CREATE_SP_POST_SALE_TO_GL.sql
GO

-- STEP 2: Verify Chart of Accounts
-- ========================================
PRINT 'Step 2: Checking Chart of Accounts...';

-- Check if required GL accounts exist
SELECT 
    AccountCode,
    AccountName,
    AccountType,
    CASE 
        WHEN AccountCode IN ('4000', '5000', '1100', '1110', '1300', '1200') THEN '✓ Required for POS'
        ELSE ''
    END AS POSRequired
FROM ChartOfAccounts
WHERE AccountCode IN ('4000', '5000', '1100', '1110', '1300', '1200')
ORDER BY AccountCode;

-- If accounts are missing, create them:
/*
INSERT INTO ChartOfAccounts (AccountCode, AccountName, AccountType, IsActive)
VALUES 
('4000', 'Sales Revenue - Retail', 'Revenue', 1),
('5000', 'Cost of Sales - Retail', 'Expense', 1),
('1100', 'Cash on Hand - Till', 'Asset', 1),
('1110', 'Card Payments - Pending', 'Asset', 1),
('1300', 'Inventory - Retail Products', 'Asset', 1),
('1200', 'Accounts Receivable - Customers', 'Asset', 1);
*/
GO

-- STEP 3: Update Retail_Stock table to include AverageCost
-- ========================================
PRINT 'Step 3: Checking Retail_Stock for AverageCost column...';

IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS 
               WHERE TABLE_NAME = 'Retail_Stock' AND COLUMN_NAME = 'AverageCost')
BEGIN
    ALTER TABLE Retail_Stock ADD AverageCost DECIMAL(18,2) DEFAULT 0;
    PRINT '✓ Added AverageCost column to Retail_Stock';
    
    -- Update AverageCost from Products table if available
    UPDATE rs
    SET rs.AverageCost = ISNULL(pv.AverageCost, p.LastPaidPrice)
    FROM Retail_Stock rs
    INNER JOIN ProductVariants pv ON rs.VariantID = pv.VariantID
    INNER JOIN Products p ON pv.ProductID = p.ProductID
    WHERE rs.AverageCost IS NULL OR rs.AverageCost = 0;
    
    PRINT '✓ Populated AverageCost from Products table';
END
ELSE
BEGIN
    PRINT '! AverageCost column already exists';
END
GO

-- STEP 4: Enable GL Posting in PaymentTenderForm.vb
-- ========================================
PRINT 'Step 4: Manual code changes required in PaymentTenderForm.vb';
PRINT '';
PRINT 'CHANGE 1: Enable InsertJournalEntry method';
PRINT 'Location: Line 1308-1312';
PRINT 'FROM:';
PRINT '    Private Sub InsertJournalEntry(...)';
PRINT '        '' Skip journal posting for now - table structure mismatch';
PRINT '        '' TODO: Fix Journals table structure';
PRINT '        Return';
PRINT '    End Sub';
PRINT '';
PRINT 'TO:';
PRINT '    Private Sub InsertJournalEntry(conn As SqlConnection, transaction As SqlTransaction, journalDate As DateTime, journalType As String, reference As String, ledgerID As Integer, debit As Decimal, credit As Decimal, description As String)';
PRINT '        Dim sql = "INSERT INTO GeneralJournal (TransactionDate, JournalType, Reference, LedgerID, Debit, Credit, Description, BranchID, CreatedBy, CreatedDate) VALUES (@Date, @Type, @Ref, @LedgerID, @Debit, @Credit, @Desc, @BranchID, @CreatedBy, GETDATE())"';
PRINT '        Using cmd As New SqlCommand(sql, conn, transaction)';
PRINT '            cmd.Parameters.AddWithValue("@Date", journalDate)';
PRINT '            cmd.Parameters.AddWithValue("@Type", journalType)';
PRINT '            cmd.Parameters.AddWithValue("@Ref", reference)';
PRINT '            cmd.Parameters.AddWithValue("@LedgerID", ledgerID)';
PRINT '            cmd.Parameters.AddWithValue("@Debit", debit)';
PRINT '            cmd.Parameters.AddWithValue("@Credit", credit)';
PRINT '            cmd.Parameters.AddWithValue("@Desc", description)';
PRINT '            cmd.Parameters.AddWithValue("@BranchID", _branchID)';
PRINT '            cmd.Parameters.AddWithValue("@CreatedBy", _cashierName)';
PRINT '            cmd.ExecuteNonQuery()';
PRINT '        End Using';
PRINT '    End Sub';
PRINT '';
PRINT 'CHANGE 2: Enable GetAverageCost method';
PRINT 'Location: Line 1314-1317';
PRINT 'FROM:';
PRINT '    Private Function GetAverageCost(...) As Decimal';
PRINT '        '' Return 0 for now - AverageCost column doesn''t exist';
PRINT '        Return 0D';
PRINT '    End Function';
PRINT '';
PRINT 'TO:';
PRINT '    Private Function GetAverageCost(conn As SqlConnection, transaction As SqlTransaction, productID As Integer, branchID As Integer) As Decimal';
PRINT '        Try';
PRINT '            '' First try to get VariantID from cart item, then get cost';
PRINT '            Dim sql = "SELECT TOP 1 ISNULL(rs.AverageCost, 0) FROM Retail_Stock rs INNER JOIN ProductVariants pv ON rs.VariantID = pv.VariantID WHERE pv.ProductID = @ProductID AND rs.BranchID = @BranchID"';
PRINT '            Using cmd As New SqlCommand(sql, conn, transaction)';
PRINT '                cmd.Parameters.AddWithValue("@ProductID", productID)';
PRINT '                cmd.Parameters.AddWithValue("@BranchID", branchID)';
PRINT '                Dim result = cmd.ExecuteScalar()';
PRINT '                Return If(result IsNot Nothing AndAlso Not IsDBNull(result), CDec(result), 0D)';
PRINT '            End Using';
PRINT '        Catch ex As Exception';
PRINT '            System.Diagnostics.Debug.WriteLine($"Error getting average cost: {ex.Message}")';
PRINT '            Return 0D';
PRINT '        End Try';
PRINT '    End Function';
PRINT '';
GO

-- STEP 5: Test the Integration
-- ========================================
PRINT 'Step 5: Testing checklist';
PRINT '1. Make a test sale in POS';
PRINT '2. Check GeneralJournal table for entries';
PRINT '3. Check Retail_Stock for inventory reduction';
PRINT '4. Run Profit & Loss report';
PRINT '';

-- Query to check recent GL postings
PRINT 'Query to check recent GL postings:';
PRINT 'SELECT TOP 20 * FROM GeneralJournal ORDER BY CreatedDate DESC;';
GO

-- ========================================
-- EXPECTED JOURNAL ENTRIES FOR A SALE
-- ========================================
/*
Example: R100 sale (R87 excl VAT + R13 VAT), Cost = R50

DR Cash                 R100
    CR Sales Revenue            R87
    CR VAT Output               R13

DR Cost of Sales        R50
    CR Inventory                R50

Net Result:
- Cash increases by R100
- Sales Revenue increases by R87
- VAT liability increases by R13
- Cost of Sales increases by R50
- Inventory decreases by R50
- Gross Profit = R87 - R50 = R37
*/
GO

PRINT '';
PRINT '========================================';
PRINT 'SETUP COMPLETE!';
PRINT '========================================';
PRINT 'Next steps:';
PRINT '1. Run all SQL scripts above';
PRINT '2. Make code changes in PaymentTenderForm.vb';
PRINT '3. Rebuild POS application';
PRINT '4. Test with a sale';
PRINT '5. Verify GL postings';
PRINT '';
GO
