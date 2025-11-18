# POS Payment System - Database Setup Instructions

## ⚠️ IMPORTANT: Run Scripts in This Order!

### Step 1: Fix Existing Invoices Table (if it exists)
```sql
-- Run this first if you already have an Invoices table
Fix_Invoices_Table.sql
```

### Step 2: Create Sales and Invoices Tables
```sql
-- This creates both Sales and Invoices tables with correct structure
Create_Sales_Tables.sql
```

### Step 3: Verify Tables Were Created
```sql
-- Check Sales table
SELECT * FROM Sales

-- Check Invoices table structure
SELECT 
    COLUMN_NAME, 
    DATA_TYPE, 
    IS_NULLABLE
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME = 'Invoices'
ORDER BY ORDINAL_POSITION
```

## 📋 Expected Table Structures

### Sales Table (Invoice Header)
- SalesID (PK, Identity)
- InvoiceNumber (Unique, e.g., "POS-0000001")
- BranchID
- CashierID
- SaleDate
- Subtotal
- TaxAmount
- TotalAmount
- PaymentMethod (CASH/CARD/SPLIT)
- CashAmount
- CardAmount
- CreatedDate

### Invoices Table (Line Items)
- InvoiceLineID (PK, Identity)
- InvoiceNumber (Groups all items)
- SalesID (FK to Sales)
- BranchID (For branch identification)
- ProductID
- ItemCode
- ProductName
- Quantity
- UnitPrice
- LineTotal
- SaleDate (Date/time of sale)
- CashierID (Who processed it)
- CreatedDate

## ✅ What Gets Updated During a Sale

### 1. Sales Table
One record per transaction with totals and payment info

### 2. Invoices Table
One record PER LINE ITEM with full details:
- If you sell 4 different products → 4 records
- Each record has InvoiceNumber, BranchID, SaleDate, CashierID
- Allows full receipt recreation
- Enables line-item returns/amendments

### 3. Demo_Retail_Stock Table
Stock quantities reduced for each product sold

### 4. GeneralJournal Table (Ledger Entries)
**For Cash Payment:**
- DR Cash, CR Sales Revenue
- DR VAT Output
- DR Cost of Sales, CR Inventory

**For Card Payment:**
- DR Bank, CR Sales Revenue
- DR VAT Output
- DR Cost of Sales, CR Inventory

**For Split Payment:**
- DR Cash (cash portion), CR Sales Revenue
- DR Bank (card portion)
- DR VAT Output
- DR Cost of Sales, CR Inventory

## 🔍 Troubleshooting

### Error: "Invalid column name 'SalesID'"
**Solution:** Run `Fix_Invoices_Table.sql` first, then `Create_Sales_Tables.sql`

### Error: "Cannot insert NULL into BranchID/SaleDate/CashierID"
**Solution:** The Sales table doesn't exist. Run `Create_Sales_Tables.sql`

### Error: "Sales table not found"
**Solution:** Run `Create_Sales_Tables.sql` to create it

### No data in Sales table after transaction
**Check:**
1. Did the transaction complete without errors?
2. Check the error message in the POS application
3. Verify connection string is correct
4. Check if Sales table exists: `SELECT * FROM Sales`

## 📊 Test Queries

### View All Sales
```sql
SELECT 
    s.InvoiceNumber,
    s.SaleDate,
    b.BranchName,
    s.TotalAmount,
    s.PaymentMethod,
    s.CashAmount,
    s.CardAmount
FROM Sales s
INNER JOIN Branches b ON s.BranchID = b.BranchID
ORDER BY s.SaleDate DESC
```

### View Invoice Line Items
```sql
SELECT 
    i.InvoiceNumber,
    i.SaleDate,
    b.BranchName,
    i.ProductName,
    i.Quantity,
    i.UnitPrice,
    i.LineTotal
FROM Invoices i
INNER JOIN Branches b ON i.BranchID = b.BranchID
ORDER BY i.InvoiceNumber, i.InvoiceLineID
```

### Recreate a Receipt
```sql
DECLARE @InvoiceNumber NVARCHAR(50) = 'POS-0000001'

-- Header
SELECT 
    s.InvoiceNumber,
    b.BranchName,
    s.SaleDate,
    s.CashierID,
    s.Subtotal,
    s.TaxAmount,
    s.TotalAmount,
    s.PaymentMethod,
    s.CashAmount,
    s.CardAmount
FROM Sales s
INNER JOIN Branches b ON s.BranchID = b.BranchID
WHERE s.InvoiceNumber = @InvoiceNumber

-- Line Items
SELECT 
    ProductName,
    Quantity,
    UnitPrice,
    LineTotal
FROM Invoices
WHERE InvoiceNumber = @InvoiceNumber
ORDER BY InvoiceLineID
```

### View Ledger Entries for a Sale
```sql
SELECT 
    TransactionDate,
    Reference,
    AccountName,
    Debit,
    Credit,
    Description
FROM GeneralJournal
WHERE Reference LIKE 'POS-%'
ORDER BY TransactionDate DESC, JournalID
```

## ✅ Success Indicators

After a successful sale, you should see:
1. ✅ 1 record in Sales table
2. ✅ X records in Invoices table (where X = number of products sold)
3. ✅ Stock reduced in Demo_Retail_Stock
4. ✅ 6+ records in GeneralJournal (Cash/Bank, Sales, VAT, COS, Inventory)
5. ✅ Receipt displayed on screen with all details
6. ✅ Invoice number generated (e.g., POS-0000001)
