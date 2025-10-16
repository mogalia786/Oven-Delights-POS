# Journals Table Structure Fix

## Problem
The code was trying to insert into Journals table with columns that don't exist:
- ❌ `EntryType` (doesn't exist)
- ❌ `Amount` (doesn't exist)
- ❌ `TransactionDate` (should be `JournalDate`)

## Actual Journals Table Structure

```sql
CREATE TABLE Journals (
    JournalID INT IDENTITY(1,1) PRIMARY KEY,
    JournalDate DATETIME NOT NULL,           -- ✅ Not TransactionDate
    JournalType NVARCHAR(50) NOT NULL,       -- ✅ Type of journal
    Reference NVARCHAR(100) NOT NULL,        -- ✅ Invoice/Return number
    LedgerID INT NOT NULL,                   -- ✅ Which ledger
    Debit DECIMAL(18,2) NOT NULL DEFAULT 0,  -- ✅ Debit amount
    Credit DECIMAL(18,2) NOT NULL DEFAULT 0, -- ✅ Credit amount
    Description NVARCHAR(500) NULL,          -- ✅ Description
    BranchID INT NULL,                       -- ✅ Branch tracking
    CreatedDate DATETIME NOT NULL DEFAULT GETDATE(),
    CreatedBy INT NULL,
    CONSTRAINT FK_Journals_Ledger FOREIGN KEY (LedgerID) REFERENCES Ledgers(LedgerID),
    CONSTRAINT FK_Journals_Branch FOREIGN KEY (BranchID) REFERENCES Branches(BranchID)
)
```

## Code Fix Applied

### Before (WRONG):
```vb
INSERT INTO Journals (TransactionDate, Reference, LedgerID, EntryType, Amount, Description, BranchID)
VALUES (GETDATE(), @Reference, @LedgerID, @EntryType, @Amount, @Description, @BranchID)
```

### After (CORRECT):
```vb
' Convert EntryType + Amount to Debit/Credit
Dim debitAmount As Decimal = If(entryType = "Debit", amount, 0)
Dim creditAmount As Decimal = If(entryType = "Credit", amount, 0)

INSERT INTO Journals (JournalDate, JournalType, Reference, LedgerID, Debit, Credit, Description, BranchID)
VALUES (GETDATE(), 'Returns', @Reference, @LedgerID, @Debit, @Credit, @Description, @BranchID)
```

## Example Journal Entries

### Return with Restock
```
JournalDate: 2025-10-16 20:00:00
JournalType: Returns
Reference: RET-PH-TILL01-000001
BranchID: 1

Entry 1: Sales Returns (Contra-Revenue)
  LedgerID: 4 (Sales Returns)
  Debit: R 100.00
  Credit: R 0.00
  Description: Return: RET-PH-TILL01-000001

Entry 2: Cash Refund
  LedgerID: 10 (Cash)
  Debit: R 0.00
  Credit: R 100.00
  Description: Refund: RET-PH-TILL01-000001

Entry 3: Inventory Increase
  LedgerID: 1 (Inventory)
  Debit: R 100.00
  Credit: R 0.00
  Description: Restock: RET-PH-TILL01-000001

Entry 4: Cost of Sales Reversal
  LedgerID: 2 (Cost of Sales)
  Debit: R 0.00
  Credit: R 100.00
  Description: Reverse COGS: RET-PH-TILL01-000001
```

### Return with Discard
```
Entry 1: Sales Returns
  Debit: R 50.00
  Credit: R 0.00

Entry 2: Cash Refund
  Debit: R 0.00
  Credit: R 50.00

Entry 3: Stock Write-Off
  LedgerID: 3 (Stock Write-Off)
  Debit: R 50.00
  Credit: R 0.00
  Description: Discarded: RET-PH-TILL01-000001
```

## Verification Query

```sql
-- Check journal entries for a specific return
SELECT 
    j.JournalDate,
    j.JournalType,
    j.Reference,
    l.LedgerName,
    l.LedgerType,
    j.Debit,
    j.Credit,
    j.Description,
    b.BranchName
FROM Journals j
INNER JOIN Ledgers l ON j.LedgerID = l.LedgerID
INNER JOIN Branches b ON j.BranchID = b.BranchID
WHERE j.Reference LIKE 'RET-%'
ORDER BY j.JournalDate DESC, j.JournalID
```

## Balance Check

```sql
-- Verify debits = credits for each return
SELECT 
    Reference,
    SUM(Debit) AS TotalDebits,
    SUM(Credit) AS TotalCredits,
    SUM(Debit) - SUM(Credit) AS Difference
FROM Journals
WHERE Reference LIKE 'RET-%'
GROUP BY Reference
HAVING SUM(Debit) - SUM(Credit) <> 0  -- Should return no rows
```

## Setup Steps

1. **Ensure BranchID exists in Journals:**
   ```sql
   -- Run: SQL/Add_BranchID_To_Journals.sql
   ```

2. **Rebuild application** with the fixed code

3. **Test return transaction**

4. **Verify journal entries** using the verification query above

## Summary

✅ Fixed column names: `JournalDate` instead of `TransactionDate`
✅ Fixed data structure: `Debit`/`Credit` instead of `EntryType`/`Amount`
✅ Added `JournalType` = 'Returns'
✅ Proper conversion from EntryType to Debit/Credit amounts
✅ BranchID properly included

**The return process will now correctly post to the Journals table!**
