# Returns with Stock Management Implementation

## Overview
Implemented comprehensive returns functionality with intelligent stock management for food retail operations.

## Features

### 1. **Put Back Into Stock Checkbox**
- Each return line item has a checkbox: "Put Back Into Stock"
- Default: **Checked** (assumes item is good for resale)
- Unchecked: Item is discarded (damaged/expired food)

### 2. **Stock Adjustment Logic**

#### When "Put Back Into Stock" is CHECKED:
- ✅ **Physical Stock Updated**: `Demo_Retail_Product.CurrentStock` increased by return quantity
- ✅ **Inventory Ledger**: DEBIT (stock value increases)
- ✅ **Cost of Sales Ledger**: CREDIT (reverses COGS)
- 📊 **Accounting Entry**:
  ```
  DR Inventory          R XXX
     CR Cost of Sales       R XXX
  ```

#### When "Put Back Into Stock" is UNCHECKED:
- ❌ **No Stock Update**: Item not added back to inventory
- ✅ **Stock Write-Off Ledger**: DEBIT (expense for discarded goods)
- 📊 **Accounting Entry**:
  ```
  DR Stock Write-Off    R XXX
     CR (no credit - loss)
  ```

### 3. **Complete Accounting Entries**

For every return transaction:

**Always Posted:**
```
1. DR Sales Returns (Contra-Revenue)    R XXX
      CR Cash (Refund)                      R XXX
```

**For Restocked Items:**
```
2. DR Inventory                          R XXX
      CR Cost of Sales                      R XXX
```

**For Discarded Items:**
```
3. DR Stock Write-Off (Expense)          R XXX
```

### 4. **Database Schema**

#### Demo_ReturnDetails Table
Added column:
- `RestockItem` (BIT, NOT NULL, DEFAULT 1)

#### Required Ledgers
Created in `Create_Stock_Adjustment_Ledgers.sql`:
- **Inventory** (Asset)
- **Cost of Sales** (Expense)
- **Stock Write-Off** (Expense)
- **Sales Returns** (Contra-Revenue)

### 5. **User Interface**

```
┌─────────────────────────────────────────────────────────────────┐
│ Item: CAKE001 - Chocolate Cake | Qty: 2 | R 50.00 | R 100.00  │
│                                                                 │
│ ☑ Put Back Into Stock    [🔄 RETURN]  [➖ MINUS]              │
└─────────────────────────────────────────────────────────────────┘
```

### 6. **Business Logic**

**Scenario 1: Good Condition Return**
- Customer returns unopened Coke
- ☑ Put Back Into Stock = CHECKED
- Result: Stock increases, inventory value restored

**Scenario 2: Damaged/Expired Return**
- Customer returns stale bread
- ☐ Put Back Into Stock = UNCHECKED
- Result: No stock increase, written off as expense

### 7. **Transaction Flow**

```
1. Cashier scans return items
2. For each item, decides: Restock or Discard?
3. Supervisor authorizes return
4. System processes:
   ├─ Refunds customer (Cash/Card)
   ├─ Updates stock (if restocking)
   ├─ Posts to journals
   └─ Updates ledgers
5. Receipt printed showing return details
```

### 8. **Reports Impact**

#### Cash Up Report
- Shows total returns deducted from sales
- Displays return count and amount

#### Inventory Report
- Restocked items appear as stock increases
- Discarded items don't affect stock levels

#### Financial Reports
- P&L shows:
  - Sales Returns (reduces revenue)
  - Stock Write-Off (expense)
  - Cost of Sales (adjusted for restocks)

### 9. **Setup Instructions**

**Step 1: Run SQL Scripts**
```sql
-- 1. Create ledgers
SQL/Create_Stock_Adjustment_Ledgers.sql

-- 2. Add RestockItem column
SQL/Add_RestockItem_Column.sql
```

**Step 2: Rebuild Application**
- All code changes are in `ReturnLineItemsForm.vb`
- Checkbox automatically added to each line item

**Step 3: Test**
1. Make a sale
2. Press F8 for returns
3. Enter invoice number
4. Toggle "Put Back Into Stock" checkbox
5. Process return
6. Verify stock levels and ledgers

### 10. **Code Components**

#### ReturnLineItem Class
```vb
Public Class ReturnLineItem
    Public Property ProductID As Integer
    Public Property ItemCode As String
    Public Property ProductName As String
    Public Property OriginalQty As Decimal
    Public Property ReturnQty As Decimal
    Public Property UnitPrice As Decimal
    Public Property LineTotal As Decimal
    Public Property RestockItem As Boolean  ' NEW
End Class
```

#### Key Methods
- `UpdateInventoryStock()` - Increases physical stock
- `PostReturnToJournalsAndLedgers()` - Posts accounting entries
- `GetLedgerID()` - Retrieves ledger IDs
- `PostToJournal()` - Creates journal entries
- `UpdateLedger()` - Updates ledger balances

### 11. **Error Handling**

- Ledger posting failures don't block returns
- Missing ledgers gracefully handled (ID = 0)
- Transaction rollback on critical errors
- Debug logging for troubleshooting

### 12. **Audit Trail**

Every return is tracked:
- Return number (RET-BRANCH-TILL-XXX)
- Original invoice number
- Customer details
- Supervisor authorization
- Restock/discard decision per item
- Journal entries with references
- Timestamp and user ID

## Benefits

✅ **Accurate Inventory**: Only good items return to stock
✅ **Proper Accounting**: Separate tracking of restocks vs write-offs
✅ **Food Safety**: Prevents spoiled items from being resold
✅ **Financial Accuracy**: True cost of returns visible
✅ **Audit Compliance**: Complete trail of all decisions
✅ **Flexibility**: Per-item decision making

## Future Enhancements

- [ ] Reason codes for discards (expired, damaged, recalled)
- [ ] Photo capture for discarded items
- [ ] Batch write-off reports
- [ ] Integration with quality control
- [ ] Supplier return tracking
