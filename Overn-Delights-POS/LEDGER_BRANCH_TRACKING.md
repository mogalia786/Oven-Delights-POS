# Ledger Branch Tracking Implementation

## Overview
All ledgers are now branch-specific to ensure accurate financial reporting per branch.

## SQL Script Changes

### `Create_Stock_Adjustment_Ledgers.sql`

**Features:**
1. ✅ Adds `BranchID` column to `Ledgers` table if it doesn't exist
2. ✅ Uses cursor to loop through all active branches
3. ✅ Creates 4 ledgers for EACH branch:
   - Inventory (Asset)
   - Cost of Sales (Expense)
   - Stock Write-Off (Expense)
   - Sales Returns (Contra-Revenue)

**Example Output:**
```
Added BranchID column to Ledgers table
----------------------------------------
Creating ledgers for Branch: Phoenix (ID: 1)
  ✓ Inventory ledger created
  ✓ Cost of Sales ledger created
  ✓ Stock Write-Off ledger created
  ✓ Sales Returns ledger created
----------------------------------------
Creating ledgers for Branch: Sandton (ID: 2)
  ✓ Inventory ledger created
  ✓ Cost of Sales ledger created
  ✓ Stock Write-Off ledger created
  ✓ Sales Returns ledger created
Stock adjustment ledgers setup completed!
```

## Code Changes

### `ReturnLineItemsForm.vb` - `GetLedgerID()` Function

**Before:**
```vb
SELECT LedgerID FROM Ledgers 
WHERE LedgerName = @LedgerName
```

**After:**
```vb
SELECT LedgerID FROM Ledgers 
WHERE LedgerName = @LedgerName 
AND (BranchID = @BranchID OR BranchID IS NULL)
```

**Fallback Logic:**
If no branch-specific ledger found, tries to find global ledger (BranchID IS NULL)

## Database Structure

### Ledgers Table
```sql
CREATE TABLE Ledgers (
    LedgerID INT IDENTITY(1,1) PRIMARY KEY,
    LedgerName NVARCHAR(100) NOT NULL,
    LedgerType NVARCHAR(50) NOT NULL,
    BranchID INT NULL,              -- ✅ ADDED
    ParentLedgerID INT NULL,
    IsActive BIT NOT NULL DEFAULT 1,
    CreatedDate DATETIME NOT NULL DEFAULT GETDATE(),
    ...
)
```

### Example Data After Script Runs

| LedgerID | LedgerName | LedgerType | BranchID |
|----------|------------|------------|----------|
| 1 | Inventory | Asset | 1 |
| 2 | Cost of Sales | Expense | 1 |
| 3 | Stock Write-Off | Expense | 1 |
| 4 | Sales Returns | Contra-Revenue | 1 |
| 5 | Inventory | Asset | 2 |
| 6 | Cost of Sales | Expense | 2 |
| 7 | Stock Write-Off | Expense | 2 |
| 8 | Sales Returns | Contra-Revenue | 2 |

## Return Transaction Flow with Branch Tracking

```
1. Return initiated at Branch 1 (Phoenix)
   └─ _branchID = 1

2. GetLedgerID("Inventory", BranchID=1)
   └─ Returns LedgerID = 1 (Phoenix Inventory)

3. PostToJournal(LedgerID=1, BranchID=1)
   └─ Journal entry tagged to Branch 1

4. UpdateLedger(LedgerID=1)
   └─ Phoenix Inventory ledger balance updated
```

## Reporting by Branch

### Branch-Specific Ledger Balances
```sql
SELECT 
    l.LedgerName,
    l.LedgerType,
    b.BranchName,
    l.DebitBalance,
    l.CreditBalance,
    l.Balance
FROM Ledgers l
INNER JOIN Branches b ON l.BranchID = b.BranchID
WHERE l.BranchID = @BranchID
ORDER BY l.LedgerType, l.LedgerName
```

### Journal Entries by Branch and Ledger
```sql
SELECT 
    j.TransactionDate,
    j.Reference,
    l.LedgerName,
    j.EntryType,
    j.Amount,
    b.BranchName
FROM Journals j
INNER JOIN Ledgers l ON j.LedgerID = l.LedgerID
INNER JOIN Branches b ON j.BranchID = b.BranchID
WHERE j.BranchID = @BranchID
AND j.Reference LIKE 'RET-%'
ORDER BY j.TransactionDate DESC
```

### Stock Write-Offs by Branch
```sql
SELECT 
    b.BranchName,
    SUM(j.Amount) AS TotalWriteOffs
FROM Journals j
INNER JOIN Ledgers l ON j.LedgerID = l.LedgerID
INNER JOIN Branches b ON j.BranchID = b.BranchID
WHERE l.LedgerName = 'Stock Write-Off'
AND j.EntryType = 'Debit'
GROUP BY b.BranchName
ORDER BY TotalWriteOffs DESC
```

## Multi-Branch Scenarios

### Scenario 1: Phoenix Branch Return
```
Branch: Phoenix (BranchID = 1)
Return: RET-PH-TILL01-000001
Ledgers Used:
  ├─ Inventory (LedgerID = 1, BranchID = 1)
  ├─ Cost of Sales (LedgerID = 2, BranchID = 1)
  ├─ Stock Write-Off (LedgerID = 3, BranchID = 1)
  └─ Sales Returns (LedgerID = 4, BranchID = 1)
```

### Scenario 2: Sandton Branch Return
```
Branch: Sandton (BranchID = 2)
Return: RET-SA-TILL02-000001
Ledgers Used:
  ├─ Inventory (LedgerID = 5, BranchID = 2)
  ├─ Cost of Sales (LedgerID = 6, BranchID = 2)
  ├─ Stock Write-Off (LedgerID = 7, BranchID = 2)
  └─ Sales Returns (LedgerID = 8, BranchID = 2)
```

## Consolidated Reporting

### All Branches Combined
```sql
SELECT 
    b.BranchName,
    SUM(CASE WHEN l.LedgerName = 'Sales Returns' THEN j.Amount ELSE 0 END) AS TotalReturns,
    SUM(CASE WHEN l.LedgerName = 'Stock Write-Off' THEN j.Amount ELSE 0 END) AS TotalWriteOffs,
    SUM(CASE WHEN l.LedgerName = 'Inventory' AND j.EntryType = 'Debit' THEN j.Amount ELSE 0 END) AS TotalRestocked
FROM Journals j
INNER JOIN Ledgers l ON j.LedgerID = l.LedgerID
INNER JOIN Branches b ON j.BranchID = b.BranchID
WHERE j.Reference LIKE 'RET-%'
GROUP BY b.BranchName
ORDER BY b.BranchName
```

## Benefits

✅ **Branch Isolation**: Each branch has its own set of ledgers
✅ **Accurate Reporting**: Financial reports per branch are accurate
✅ **Audit Trail**: Complete tracking of which branch posted which entries
✅ **Scalability**: Easy to add new branches (script auto-creates ledgers)
✅ **Consolidation**: Can still aggregate across all branches for head office
✅ **Compliance**: Meets multi-branch accounting requirements

## Setup Instructions

1. **Run SQL Script:**
   ```sql
   SQL/Create_Stock_Adjustment_Ledgers.sql
   ```

2. **Verify Ledgers Created:**
   ```sql
   SELECT l.*, b.BranchName 
   FROM Ledgers l
   LEFT JOIN Branches b ON l.BranchID = b.BranchID
   WHERE l.LedgerName IN ('Inventory', 'Cost of Sales', 'Stock Write-Off', 'Sales Returns')
   ORDER BY b.BranchName, l.LedgerName
   ```

3. **Rebuild Application**

4. **Test Return Transaction**
   - Process a return at each branch
   - Verify ledgers are updated for correct branch
   - Check journal entries have correct BranchID

## Troubleshooting

### Issue: Ledger Not Found
**Cause:** Script not run for all branches
**Solution:** Re-run `Create_Stock_Adjustment_Ledgers.sql`

### Issue: Wrong Branch Ledger Updated
**Cause:** GetLedgerID not filtering by BranchID
**Solution:** Verify code changes in `ReturnLineItemsForm.vb`

### Issue: Global Ledgers Being Used
**Cause:** Branch-specific ledgers don't exist
**Solution:** Check `Ledgers` table for BranchID values

## Summary

✅ **Ledgers Table:** BranchID column added
✅ **SQL Script:** Creates ledgers per branch automatically
✅ **Code:** GetLedgerID filters by BranchID with fallback
✅ **Journals:** Already had BranchID tracking
✅ **Complete:** Full branch isolation for accounting
