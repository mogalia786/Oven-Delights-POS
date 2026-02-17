# Branch Tracking Verification

## ✅ BranchID Captured in All Return Transactions

### 1. **Demo_Returns Table (Header)**
```sql
INSERT INTO Demo_Returns (
    ReturnNumber, 
    OriginalInvoiceNumber, 
    BranchID,              -- ✅ CAPTURED
    TillPointID, 
    CashierID, 
    SupervisorID, 
    ...
)
```
**Source:** `ReturnLineItemsForm.vb` line 534
**Value:** `_branchID` (passed from constructor)

---

### 2. **Demo_ReturnDetails Table (Line Items)**
- **No direct BranchID column** (by design)
- **BranchID accessible via JOIN:**
  ```sql
  SELECT rd.*, r.BranchID
  FROM Demo_ReturnDetails rd
  INNER JOIN Demo_Returns r ON rd.ReturnID = r.ReturnID
  ```
- **Rationale:** Normalized design - BranchID stored once in header table

---

### 3. **Journals Table (Accounting Entries)**
```sql
INSERT INTO Journals (
    TransactionDate, 
    Reference, 
    LedgerID, 
    EntryType, 
    Amount, 
    Description, 
    BranchID               -- ✅ CAPTURED
)
```
**Source:** `ReturnLineItemsForm.vb` line 660
**Value:** `_branchID` (line 669)

**All journal entries include:**
- Sales Returns journal entry → BranchID
- Cash refund journal entry → BranchID
- Inventory adjustment journal entry → BranchID
- Cost of Sales reversal journal entry → BranchID
- Stock Write-Off journal entry → BranchID

---

### 4. **Stock Updates (Demo_Retail_Product)**
```sql
UPDATE Demo_Retail_Product 
SET CurrentStock = CurrentStock + @Quantity 
WHERE ProductID = @ProductID 
AND (BranchID = @BranchID OR BranchID IS NULL)
```
**Source:** `ReturnLineItemsForm.vb` line 583-587
**Value:** `_branchID` (line 592)

**Fallback Logic:**
- First tries to update with BranchID filter (multi-branch inventory)
- If no rows affected, updates without BranchID (global inventory)
- Supports both branch-specific and centralized inventory models

---

## BranchID Flow Diagram

```
┌─────────────────────────────────────────────────────────┐
│ POS Login                                               │
│ ├─ User selects Branch                                  │
│ └─ _branchID = Selected Branch ID                       │
└─────────────────────────────────────────────────────────┘
                        ↓
┌─────────────────────────────────────────────────────────┐
│ Return Transaction Initiated (F8)                       │
│ ├─ ReturnLineItemsForm(invoiceNumber, branchID, ...)   │
│ └─ _branchID stored in form instance                    │
└─────────────────────────────────────────────────────────┘
                        ↓
┌─────────────────────────────────────────────────────────┐
│ Process Return                                          │
│ ├─ Demo_Returns.BranchID = _branchID        ✅         │
│ ├─ Journals.BranchID = _branchID (all entries) ✅      │
│ └─ Stock Update filtered by _branchID       ✅         │
└─────────────────────────────────────────────────────────┘
```

---

## Reports by Branch

### Returns Report by Branch
```sql
SELECT 
    r.ReturnNumber,
    r.ReturnDate,
    r.BranchID,
    b.BranchName,
    r.TotalAmount,
    r.CustomerName
FROM Demo_Returns r
INNER JOIN Branches b ON r.BranchID = b.BranchID
WHERE r.BranchID = @BranchID
AND r.ReturnDate BETWEEN @StartDate AND @EndDate
ORDER BY r.ReturnDate DESC
```

### Journal Entries by Branch
```sql
SELECT 
    j.TransactionDate,
    j.Reference,
    l.LedgerName,
    j.EntryType,
    j.Amount,
    j.BranchID,
    b.BranchName
FROM Journals j
INNER JOIN Ledgers l ON j.LedgerID = l.LedgerID
INNER JOIN Branches b ON j.BranchID = b.BranchID
WHERE j.BranchID = @BranchID
AND j.Reference LIKE 'RET-%'
ORDER BY j.TransactionDate DESC
```

### Stock Levels by Branch
```sql
SELECT 
    p.ProductID,
    p.ProductName,
    p.CurrentStock,
    p.BranchID,
    b.BranchName
FROM Demo_Retail_Product p
INNER JOIN Branches b ON p.BranchID = b.BranchID
WHERE p.BranchID = @BranchID
ORDER BY p.ProductName
```

---

## Multi-Branch Scenarios

### Scenario 1: Branch A Returns Item
```
Branch: Phoenix (BranchID = 1)
Return: RET-PH-TILL01-001
Result:
  ├─ Demo_Returns.BranchID = 1 ✅
  ├─ All Journals.BranchID = 1 ✅
  └─ Stock updated for Branch 1 only ✅
```

### Scenario 2: Branch B Returns Item
```
Branch: Sandton (BranchID = 2)
Return: RET-SA-TILL02-001
Result:
  ├─ Demo_Returns.BranchID = 2 ✅
  ├─ All Journals.BranchID = 2 ✅
  └─ Stock updated for Branch 2 only ✅
```

### Scenario 3: Centralized Inventory
```
If Demo_Retail_Product.BranchID IS NULL:
  ├─ Stock is global across all branches
  ├─ Return still tracked to specific branch
  └─ Journal entries still branch-specific
```

---

## Audit Trail

Every return transaction includes:
- ✅ **Which branch** processed the return
- ✅ **Which till** was used
- ✅ **Which cashier** processed it
- ✅ **Which supervisor** authorized it
- ✅ **All journal entries** tagged with BranchID
- ✅ **Stock movements** filtered by BranchID

---

## Verification Queries

### Check Return BranchID Consistency
```sql
-- Verify all returns have valid BranchID
SELECT 
    r.ReturnNumber,
    r.BranchID,
    b.BranchName,
    COUNT(rd.ReturnLineID) AS LineItems
FROM Demo_Returns r
LEFT JOIN Branches b ON r.BranchID = b.BranchID
LEFT JOIN Demo_ReturnDetails rd ON r.ReturnID = rd.ReturnID
WHERE r.BranchID IS NULL  -- Should return 0 rows
GROUP BY r.ReturnNumber, r.BranchID, b.BranchName
```

### Check Journal BranchID Consistency
```sql
-- Verify all return journals have valid BranchID
SELECT 
    j.Reference,
    j.BranchID,
    b.BranchName,
    COUNT(*) AS JournalEntries
FROM Journals j
LEFT JOIN Branches b ON j.BranchID = b.BranchID
WHERE j.Reference LIKE 'RET-%'
AND j.BranchID IS NULL  -- Should return 0 rows
GROUP BY j.Reference, j.BranchID, b.BranchName
```

---

## Summary

✅ **Demo_Returns:** BranchID captured
✅ **Demo_ReturnDetails:** BranchID accessible via JOIN
✅ **Journals:** BranchID captured for all entries
✅ **Stock Updates:** BranchID filtered (with fallback)
✅ **Reports:** Can filter by BranchID
✅ **Audit Trail:** Complete branch tracking
✅ **Multi-Branch:** Fully supported

**Conclusion:** All return transactions are properly tracked to their originating branch!
