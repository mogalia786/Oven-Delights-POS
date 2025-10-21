# ORDERS SYSTEM - CORRECT IMPLEMENTATION

## F-KEY MAPPINGS (POS)
- **F10** = General Order (from cart) → `O-BranchPrefix-000001` → OrderType='Order'
- **F11** = Custom Cake Order (with questions) → `O-BranchPrefix-CCAKE-000001` → OrderType='Cake'
- **F12** = Order Collection (requires full order number)

## DATABASE STRUCTURE

### POS_CustomOrders Table
- **OrderNumber**: VARCHAR - Format depends on OrderType
- **OrderType**: VARCHAR - 'Order' or 'Cake'
- **BranchID**: INT - Branch where order was placed
- **CustomerName**, **CustomerSurname**, **CustomerPhone**: Customer details
- **OrderDate**: DATETIME - When order was created
- **ReadyDate**: DATE - When order should be ready
- **ReadyTime**: TIME - Time order should be ready
- **TotalAmount**: DECIMAL - Full order amount
- **DepositPaid**: DECIMAL - Deposit amount paid
- **BalanceDue**: DECIMAL - Remaining balance
- **OrderStatus**: VARCHAR - 'New', 'Ready', 'Delivered', 'Cancelled'
- **CreatedBy**: VARCHAR - Cashier who created order
- **ManufacturingInstructions**: TEXT - Only for Cake orders (NULL for general orders)

### POS_CustomOrderItems Table
- **OrderID**: INT - FK to POS_CustomOrders
- **ProductID**: INT
- **ProductName**: VARCHAR
- **Quantity**: DECIMAL
- **UnitPrice**: DECIMAL
- **LineTotal**: DECIMAL

## ORDER NUMBER GENERATION

### General Orders (F10)
```
Pattern: O-{BranchPrefix}-000001
Example: O-JHB-000001, O-JHB-000002, etc.
SQL: WHERE OrderNumber LIKE 'O-JHB-%' AND OrderNumber NOT LIKE '%-CCAKE-%'
```

### Custom Cake Orders (F11)
```
Pattern: O-{BranchPrefix}-CCAKE-000001
Example: O-JHB-CCAKE-000001, O-JHB-CCAKE-000002, etc.
SQL: WHERE OrderNumber LIKE 'O-JHB-CCAKE-%'
```

## CASHUP REPORTING

### Order Deposits
- SaleType = 'OrderDeposit' in Demo_Sales
- Shows as deposit, NOT as sale
- Grouped by OrderType: 'Order' vs 'Cake'

### Order Collections (Status='Delivered')
- SaleType = 'OrderCollection' in Demo_Sales
- Shows as sale ONLY when status changes to 'Delivered'
- Grouped separately: "Order" and "Cake" categories

### Regular Sales
- SaleType = 'Sale' in Demo_Sales
- Normal POS sales

## WORKFLOW

### F10 - General Order
1. Add items to cart
2. Press F10
3. Enter customer details (name, surname, phone, ready date/time)
4. Enter deposit amount
5. Process deposit payment
6. Order created with OrderType='Order'
7. Order number: O-BranchPrefix-000001
8. ManufacturingInstructions = NULL

### F11 - Custom Cake Order
1. Press F11
2. Answer all cake questions (size, shape, flavor, etc.)
3. Select accessories/toppings
4. Enter customer details
5. Calculate total and deposit
6. Process deposit payment
7. Order created with OrderType='Cake'
8. Order number: O-BranchPrefix-CCAKE-000001
9. ManufacturingInstructions = populated with cake specs

### F12 - Order Collection
1. Press F12
2. Enter FULL order number (e.g., O-JHB-000001 or O-JHB-CCAKE-000001)
3. System validates order exists and status='Ready'
4. Load order items into cart
5. Display balance due
6. Process balance payment
7. Update OrderStatus='Delivered'
8. Record in Demo_Sales with SaleType='OrderCollection'

## ERP INTEGRATION

### Manufacturing > Orders Menu
- **General Orders**: Filters WHERE OrderType='Order'
- **Cake Orders**: Filters WHERE OrderType='Cake'
- Both show: New, Ready, All
- Mark as Ready changes status from 'New' to 'Ready'

## CRITICAL RULES
1. OrderType field is MANDATORY
2. General orders have NULL ManufacturingInstructions
3. Cake orders have populated ManufacturingInstructions
4. Order numbers MUST include branch prefix
5. Collection requires FULL order number
6. Orders only show as sales in cashup when status='Delivered'
7. Deposits and collections are separate from regular sales
