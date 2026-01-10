# Cost of Sales Integration - POS System

## Overview
The POS system must track cost of sales for accounting purposes WITHOUT displaying cost prices to cashiers/users.

---

## Database Changes Required

### 1. Run SQL Script
**File:** `ADD_COST_OF_SALES_TO_POS.sql`

**Creates:**
- `Demo_Sales.CostOfSales` column
- `POS_InvoiceLines.UnitCost` and `LineCost` columns
- `Invoices.UnitCost` and `LineCost` columns
- `sp_GetProductCostForPOS` stored procedure
- `sp_PostSaleAccountingEntries` stored procedure

---

## VB.NET Code Changes Required

### PaymentTenderForm.vb Updates

#### 1. Add Cost Tracking Variables
```vb
Private _totalCostOfSales As Decimal = 0
```

#### 2. Update InsertSale Method
```vb
Private Function InsertSale(conn As SqlConnection, transaction As SqlTransaction, invoiceNumber As String) As Integer
    ' ... existing code ...
    
    Dim insertSql = "INSERT INTO Demo_Sales (SaleNumber, InvoiceNumber, SaleDate, CashierID, BranchID, TillPointID, Subtotal, TaxAmount, TotalAmount, CostOfSales, PaymentMethod, CashAmount, CardAmount, SaleType, ReferenceNumber) 
                            VALUES (@SaleNumber, @InvoiceNumber, @SaleDate, @CashierID, @BranchID, @TillPointID, @Subtotal, @TaxAmount, @TotalAmount, @CostOfSales, @PaymentMethod, @CashAmount, @CardAmount, @SaleType, @ReferenceNumber);
                            SELECT CAST(SCOPE_IDENTITY() AS INT)"
    
    Using cmd As New SqlCommand(insertSql, conn, transaction)
        ' ... existing parameters ...
        cmd.Parameters.AddWithValue("@CostOfSales", _totalCostOfSales)
        ' ... rest of code ...
    End Using
End Function
```

#### 3. Update InsertInvoiceLineItems Method
```vb
Private Sub InsertInvoiceLineItems(conn As SqlConnection, transaction As SqlTransaction, salesID As Integer, invoiceNumber As String)
    Dim sqlPOS = "INSERT INTO POS_InvoiceLines (InvoiceNumber, SalesID, BranchID, ProductID, ItemCode, ProductName, Quantity, UnitPrice, UnitCost, LineTotal, LineCost, SaleDate, CashierID, CreatedDate) 
                  VALUES (@InvoiceNumber, @SaleID, @BranchID, @ProductID, @ItemCode, @ProductName, @Quantity, @UnitPrice, @UnitCost, @LineTotal, @LineCost, GETDATE(), @CashierID, GETDATE())"
    
    For Each row As DataRow In _cartItems.Rows
        ' Get cost price from database (NOT displayed to user)
        Dim unitCost As Decimal = GetProductCost(CInt(row("ProductID")), conn, transaction)
        Dim quantity As Decimal = CDec(row("Qty"))
        Dim lineCost As Decimal = unitCost * quantity
        
        ' Accumulate total cost of sales
        _totalCostOfSales += lineCost
        
        Using cmd As New SqlCommand(sqlPOS, conn, transaction)
            ' ... existing parameters ...
            cmd.Parameters.AddWithValue("@UnitCost", unitCost)
            cmd.Parameters.AddWithValue("@LineCost", lineCost)
            cmd.ExecuteNonQuery()
        End Using
    Next
End Sub
```

#### 4. Add GetProductCost Method
```vb
Private Function GetProductCost(productID As Integer, conn As SqlConnection, transaction As SqlTransaction) As Decimal
    Try
        Using cmd As New SqlCommand("sp_GetProductCostForPOS", conn, transaction)
            cmd.CommandType = CommandType.StoredProcedure
            cmd.Parameters.AddWithValue("@ProductID", productID)
            cmd.Parameters.AddWithValue("@BranchID", _branchID)
            
            Dim result = cmd.ExecuteScalar()
            Return If(result IsNot Nothing AndAlso Not IsDBNull(result), CDec(result), 0)
        End Using
    Catch ex As Exception
        ' Log error but return 0 to not break sale
        System.Diagnostics.Debug.WriteLine($"Error getting product cost: {ex.Message}")
        Return 0
    End Try
End Function
```

#### 5. Add PostAccountingEntries Method
```vb
Private Sub PostAccountingEntries(conn As SqlConnection, transaction As SqlTransaction, salesID As Integer, invoiceNumber As String)
    Try
        Using cmd As New SqlCommand("sp_PostSaleAccountingEntries", conn, transaction)
            cmd.CommandType = CommandType.StoredProcedure
            cmd.Parameters.AddWithValue("@SalesID", salesID)
            cmd.Parameters.AddWithValue("@InvoiceNumber", invoiceNumber)
            cmd.Parameters.AddWithValue("@BranchID", _branchID)
            cmd.Parameters.AddWithValue("@SaleDate", DateTime.Now)
            cmd.Parameters.AddWithValue("@TotalAmount", _totalAmount)
            cmd.Parameters.AddWithValue("@CostOfSales", _totalCostOfSales)
            cmd.Parameters.AddWithValue("@PaymentMethod", _paymentMethod)
            cmd.ExecuteNonQuery()
        End Using
    Catch ex As Exception
        ' Log error but don't fail sale
        System.Diagnostics.Debug.WriteLine($"Error posting accounting entries: {ex.Message}")
    End Try
End Sub
```

#### 6. Update ProcessPayment Method
```vb
Private Sub ProcessPayment()
    Try
        Using conn As New SqlConnection(_connectionString)
            conn.Open()
            Using transaction = conn.BeginTransaction()
                Try
                    ' Reset cost tracking
                    _totalCostOfSales = 0
                    
                    Dim invoiceNumber = GenerateInvoiceNumber(conn, transaction)
                    
                    ' Insert line items (calculates _totalCostOfSales)
                    InsertInvoiceLineItems(conn, transaction, salesID, invoiceNumber)
                    
                    ' Insert sale header (includes _totalCostOfSales)
                    Dim salesID = InsertSale(conn, transaction, invoiceNumber)
                    
                    ' Post accounting entries to ledgers/journals
                    PostAccountingEntries(conn, transaction, salesID, invoiceNumber)
                    
                    ' Update stock
                    UpdateStock(conn, transaction)
                    
                    transaction.Commit()
                    ' ... rest of code ...
                Catch ex As Exception
                    transaction.Rollback()
                    Throw
                End Try
            End Using
        End Using
    Catch ex As Exception
        MessageBox.Show($"Payment processing error: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
    End Try
End Sub
```

---

## Accounting Entries Posted

### For Each Sale (Example: R100 sale, R63.33 cost)

```
DR Cash/Bank/AR        R100.00  (Asset increase)
CR Sales Revenue       R100.00  (Revenue)
DR Cost of Sales       R63.33   (Expense)
CR Inventory           R63.33   (Asset decrease)
```

### Profit Calculation
```
Gross Profit = Sales Revenue - Cost of Sales
Example: R100.00 - R63.33 = R36.67
```

---

## Data Flow

1. **Manufacturing Completes Production**
   - `sp_CompleteReOrderProduct` calculates cost from BOM
   - Updates `Demo_Retail_Price.CostPrice` = R6.33 per unit
   - Adds to retail stock

2. **POS Sale**
   - Cashier scans product (sees selling price only)
   - POS retrieves cost from `Demo_Retail_Price.CostPrice` (hidden)
   - Calculates: 1 unit × R6.33 = R6.33 line cost
   - Accumulates total cost of sales

3. **Payment Processing**
   - Saves sale with cost of sales
   - Posts accounting entries to ledgers/journals
   - Updates stock

4. **Reporting**
   - Sales reports show revenue
   - Profit reports show: Revenue - Cost of Sales
   - Inventory reports show cost basis

---

## Important Notes

✅ **Cost is NEVER displayed in POS UI**
- Cashiers/users only see selling prices
- Cost is retrieved silently from database
- Used only for accounting entries

✅ **Cost comes from ERP system**
- Manufactured products: Cost from BOM (sp_CompleteReOrderProduct)
- External products: Cost from purchase orders
- Stored in Demo_Retail_Price.CostPrice per branch

✅ **Accounting is automatic**
- Every sale posts to ledgers/journals
- No manual entries needed
- Full audit trail maintained

---

## Testing Checklist

- [ ] Run ADD_COST_OF_SALES_TO_POS.sql
- [ ] Update PaymentTenderForm.vb with code changes
- [ ] Test sale of manufactured product (has BOM cost)
- [ ] Test sale of external product (has purchase cost)
- [ ] Verify Demo_Sales.CostOfSales is populated
- [ ] Verify JournalEntries table has 4 entries per sale
- [ ] Verify cost is NOT visible in POS UI
- [ ] Verify profit reports calculate correctly
