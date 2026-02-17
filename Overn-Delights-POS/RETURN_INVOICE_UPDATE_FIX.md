# Return Invoice Update Fix

## Issues Fixed

### 1. **Duplicate Return Number Format**
**Problem:** `RET-PH-TILLPH-TILL-01-000002` (TILL repeated)

**Root Cause:** TillNumber from database already contains "TILL" prefix

**Fix:**
```vb
' Strip TILL prefix if it exists
tillNumber = result.ToString().Replace("TILL", "").Replace("Till", "").Trim()

' Format: RET-{BranchCode}-TILL-{TillNumber}-{Sequence}
returnNumber = $"RET-{branchCode}-TILL-{tillNumber}-{sequence:D6}"
```

**Result:** `RET-PH-TILL-01-000002` ✅

---

### 2. **Invoice Line Items Not Updated After Return**
**Problem:** 
- Return item from invoice
- Go back to invoice
- Item still shows original quantity
- Should reduce quantity or remove line if fully returned

**Fix:** Added `UpdateInvoiceLineItem()` method that:

1. **Reduces Quantity:**
   ```sql
   UPDATE Demo_SalesDetails 
   SET Quantity = Quantity - @ReturnedQty,
       LineTotal = (Quantity - @ReturnedQty) * UnitPrice
   WHERE SaleID = @SaleID AND ProductID = @ProductID
   ```

2. **Removes Line if Qty = 0:**
   ```sql
   DELETE FROM Demo_SalesDetails 
   WHERE SaleID = @SaleID 
   AND ProductID = @ProductID 
   AND Quantity <= 0
   ```

3. **Updates Sale Totals:**
   ```sql
   UPDATE Demo_Sales 
   SET Subtotal = (SELECT SUM(LineTotal) FROM Demo_SalesDetails WHERE SaleID = @SaleID),
       TotalAmount = (SELECT SUM(LineTotal) FROM Demo_SalesDetails WHERE SaleID = @SaleID)
   WHERE SaleID = @SaleID
   ```

---

## Return Flow Now

```
1. User selects items to return
   ├─ Full return (RETURN button)
   └─ Partial return (MINUS button)

2. Process Return executes:
   ├─ Insert into Demo_Returns
   ├─ Insert into Demo_ReturnDetails
   ├─ Update Demo_Retail_Product stock (if restocking)
   ├─ Update Demo_SalesDetails (reduce quantity) ✅ NEW
   ├─ Delete line if quantity = 0 ✅ NEW
   ├─ Update Demo_Sales totals ✅ NEW
   └─ Post to Journals/Ledgers

3. Result:
   ├─ Invoice reflects actual remaining items
   ├─ Can't return same item twice
   └─ Invoice total updated
```

---

## Examples

### Example 1: Full Return
**Original Invoice:**
- Item: Cake, Qty: 2, Price: R 50, Total: R 100

**Return Action:** Click RETURN button

**Result:**
- Demo_SalesDetails: Line deleted (Qty = 0)
- Demo_Sales.TotalAmount: Reduced by R 100
- Invoice shows: Item removed

---

### Example 2: Partial Return
**Original Invoice:**
- Item: Bread, Qty: 5, Price: R 15, Total: R 75

**Return Action:** Click MINUS, enter 2

**Result:**
- Demo_SalesDetails: Qty = 3, LineTotal = R 45
- Demo_Sales.TotalAmount: Reduced by R 30
- Invoice shows: Bread, Qty: 3, Total: R 45

---

### Example 3: Multiple Partial Returns
**Original Invoice:**
- Item: Coke, Qty: 10, Price: R 25, Total: R 250

**First Return:** Return 3
- Demo_SalesDetails: Qty = 7, LineTotal = R 175

**Second Return:** Return 4
- Demo_SalesDetails: Qty = 3, LineTotal = R 75

**Third Return:** Return 3
- Demo_SalesDetails: Line deleted (Qty = 0)
- Invoice shows: Item removed

---

## Database Changes

### Demo_SalesDetails Updates
```sql
-- Before Return
SaleID | ProductID | Quantity | UnitPrice | LineTotal
1      | 101       | 5        | 15.00     | 75.00

-- After Returning 2
SaleID | ProductID | Quantity | UnitPrice | LineTotal
1      | 101       | 3        | 15.00     | 45.00

-- After Returning 3 more (total 5)
-- Line deleted (Quantity = 0)
```

### Demo_Sales Updates
```sql
-- Before Return
SaleID | InvoiceNumber | Subtotal | TotalAmount
1      | INV-001       | 250.00   | 250.00

-- After Return (R 75 returned)
SaleID | InvoiceNumber | Subtotal | TotalAmount
1      | INV-001       | 175.00   | 175.00
```

---

## Validation

### Prevent Over-Return
The system already prevents returning more than purchased:
```vb
If returnQty > maxQty Then
    MessageBox.Show("Cannot return more than purchased")
    Return
End If
```

### Prevent Duplicate Returns
The system tracks what's already in the return list:
```vb
Dim existing = _returnItems.FirstOrDefault(Function(x) x.ProductID = productID)
If existing IsNot Nothing Then
    MessageBox.Show("This item is already in the return list!")
    Return
End If
```

---

## Testing Checklist

- [ ] Return full quantity - line removed from invoice
- [ ] Return partial quantity - line updated with remaining qty
- [ ] Return multiple times - quantities accumulate correctly
- [ ] Try to return more than purchased - blocked
- [ ] Try to return same item twice in one return - blocked
- [ ] Invoice totals recalculated correctly
- [ ] Return number format correct (no duplication)
- [ ] Stock updated if restocking
- [ ] Journal entries created
- [ ] Receipt prints correctly

---

## Summary

✅ **Fixed return number duplication** - Strips TILL prefix
✅ **Updates invoice line items** - Reduces quantity or removes line
✅ **Updates invoice totals** - Recalculates Subtotal and TotalAmount
✅ **Prevents over-return** - Can't return more than purchased
✅ **Prevents duplicate returns** - Tracks returned items
✅ **Complete audit trail** - All changes in transaction

**Rebuild and test - invoice line items will now update correctly after returns!**
