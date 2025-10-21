# POS Orders Implementation - Frontend Solution

## Overview
This is for the **Oven-Delights-POS** solution (Frontend/Retail).

---

## Keyboard Shortcuts

| Key | Function | Description |
|-----|----------|-------------|
| **F10** | **Custom Cake Order** | Create new custom cake order |
| **F11** | **View New Orders** | View orders (read-only) |
| **F12** | **Order Collection** | Collect order and tender balance |

---

## Implementation

### Step 1: Update POSMainForm.vb Keyboard Shortcuts

```vb
Protected Overrides Function ProcessCmdKey(ByRef msg As Message, keyData As Keys) As Boolean
    ' F10 - Custom Cake Order
    If keyData = Keys.F10 Then
        OpenCustomCakeOrder()
        Return True
    End If
    
    ' F11 - View New Orders
    If keyData = Keys.F11 Then
        OpenViewOrders()
        Return True
    End If
    
    ' F12 - Order Collection
    If keyData = Keys.F12 Then
        OpenOrderCollection()
        Return True
    End If
    
    Return MyBase.ProcessCmdKey(msg, keyData)
End Function

Private Sub OpenCustomCakeOrder()
    Try
        If _cartItems.Rows.Count = 0 Then
            MessageBox.Show("Please add items to cart first", "Custom Order", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            Return
        End If
        
        ' Get branch info
        Dim branchPrefix As String = GetBranchPrefix(_currentBranchId)
        Dim branchName As String = GetBranchName(_currentBranchId)
        
        ' Convert cart to order items
        Dim orderItems As New List(Of CartItem)()
        For Each row As DataRow In _cartItems.Rows
            orderItems.Add(New CartItem With {
                .ProductID = Convert.ToInt32(row("ProductID")),
                .ProductName = row("ProductName").ToString(),
                .Quantity = Convert.ToDecimal(row("Quantity")),
                .UnitPrice = Convert.ToDecimal(row("UnitPrice")),
                .LineTotal = Convert.ToDecimal(row("LineTotal"))
            })
        Next
        
        ' Show custom order dialog
        Dim orderDialog As New CustomerOrderDialog()
        orderDialog.OrderItems = orderItems
        orderDialog.TotalAmount = _currentTotal
        orderDialog.BranchID = _currentBranchId
        orderDialog.BranchName = branchName
        orderDialog.BranchPrefix = branchPrefix
        orderDialog.CurrentUser = _currentUsername
        
        If orderDialog.ShowDialog() = DialogResult.OK Then
            _cartItems.Clear()
            UpdateCartTotals()
            MessageBox.Show($"Order {orderDialog.OrderNumber} created successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information)
        End If
        
    Catch ex As Exception
        MessageBox.Show("Error creating order: " & ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
    End Try
End Sub

Private Sub OpenViewOrders()
    Try
        Dim viewOrdersForm As New ViewOrdersForm(_currentBranchId)
        viewOrdersForm.ShowDialog()
    Catch ex As Exception
        MessageBox.Show("Error opening orders: " & ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
    End Try
End Sub

Private Sub OpenOrderCollection()
    Try
        ' Clear cart if has items
        If _cartItems.Rows.Count > 0 Then
            Dim result = MessageBox.Show(
                "Clear current cart to process order collection?",
                "Order Collection",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question)
            
            If result = DialogResult.No Then
                Return
            End If
            
            _cartItems.Clear()
            UpdateCartTotals()
        End If
        
        ' Prompt for order number
        Dim orderNumber As String = InputBox(
            "Enter Order Number:" & vbCrLf & vbCrLf &
            "Format: O-JHB-000001" & vbCrLf &
            "Or just the number: 000001",
            "Order Collection",
            "")
        
        If String.IsNullOrWhiteSpace(orderNumber) Then
            Return
        End If
        
        ' Clean up input
        orderNumber = orderNumber.Trim().ToUpper()
        
        ' Add prefix if not present
        If Not orderNumber.StartsWith("O-") Then
            orderNumber = "O-" & GetBranchPrefix(_currentBranchId) & "-" & orderNumber.PadLeft(6, "0"c)
        End If
        
        ' Load and process order
        LoadOrderForCollection(orderNumber)
        
    Catch ex As Exception
        MessageBox.Show("Error: " & ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
    End Try
End Sub

Private _isOrderCollectionMode As Boolean = False
Private _collectionOrderID As Integer = 0
Private _collectionOrderNumber As String = ""
Private _collectionDepositPaid As Decimal = 0
Private _collectionTotalAmount As Decimal = 0

Private Sub LoadOrderForCollection(orderNumber As String)
    Try
        Using conn As New SqlConnection(_connString)
            conn.Open()
            
            ' Load order
            Dim cmdOrder As New SqlCommand("
                SELECT 
                    OrderID, OrderNumber, BranchID,
                    CustomerName, CustomerSurname, CustomerPhone,
                    TotalAmount, DepositPaid, BalanceDue,
                    OrderStatus, ReadyDate, ReadyTime
                FROM POS_CustomOrders
                WHERE OrderNumber = @orderNumber
                AND BranchID = @branchId", conn)
            
            cmdOrder.Parameters.AddWithValue("@orderNumber", orderNumber)
            cmdOrder.Parameters.AddWithValue("@branchId", _currentBranchId)
            
            Using reader = cmdOrder.ExecuteReader()
                If Not reader.Read() Then
                    MessageBox.Show($"Order {orderNumber} not found", "Order Not Found", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                    Return
                End If
                
                Dim orderStatus As String = reader("OrderStatus").ToString()
                
                ' Check if order is ready
                If orderStatus = "New" Then
                    MessageBox.Show(
                        $"Order {orderNumber} is NOT READY yet!" & vbCrLf & vbCrLf &
                        "Please check with manufacturer." & vbCrLf &
                        "Status: NEW (In Production)",
                        "Order Not Ready",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning)
                    Return
                ElseIf orderStatus = "Delivered" Then
                    MessageBox.Show(
                        $"Order {orderNumber} has already been collected!",
                        "Already Collected",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information)
                    Return
                ElseIf orderStatus = "Cancelled" Then
                    MessageBox.Show(
                        $"Order {orderNumber} has been cancelled",
                        "Order Cancelled",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning)
                    Return
                End If
                
                ' Order is Ready - proceed
                _collectionOrderID = Convert.ToInt32(reader("OrderID"))
                _collectionOrderNumber = reader("OrderNumber").ToString()
                _collectionTotalAmount = Convert.ToDecimal(reader("TotalAmount"))
                _collectionDepositPaid = Convert.ToDecimal(reader("DepositPaid"))
                Dim balanceDue As Decimal = Convert.ToDecimal(reader("BalanceDue"))
                Dim customerName As String = $"{reader("CustomerName")} {reader("CustomerSurname")}"
                Dim customerPhone As String = reader("CustomerPhone").ToString()
                
                reader.Close()
                
                ' Load order items
                Dim cmdItems As New SqlCommand("
                    SELECT ProductID, ProductName, Quantity, UnitPrice, LineTotal
                    FROM POS_CustomOrderItems
                    WHERE OrderID = @orderId", conn)
                
                cmdItems.Parameters.AddWithValue("@orderId", _collectionOrderID)
                
                _cartItems.Clear()
                
                Using itemReader = cmdItems.ExecuteReader()
                    While itemReader.Read()
                        Dim newRow = _cartItems.NewRow()
                        newRow("Code") = ""
                        newRow("SKU") = ""
                        newRow("ProductName") = itemReader("ProductName").ToString()
                        newRow("Quantity") = Convert.ToDecimal(itemReader("Quantity"))
                        newRow("UnitPrice") = Convert.ToDecimal(itemReader("UnitPrice"))
                        newRow("LineTotal") = Convert.ToDecimal(itemReader("LineTotal"))
                        newRow("ProductID") = Convert.ToInt32(itemReader("ProductID"))
                        _cartItems.Rows.Add(newRow)
                    End While
                End Using
                
                ' Enable collection mode
                _isOrderCollectionMode = True
                
                ' Update display
                UpdateCartTotals()
                txtTenderAmount.Text = balanceDue.ToString("N2")
                
                ' Show banner
                ShowCollectionBanner(customerName, customerPhone, balanceDue)
                
                ' Show confirmation
                MessageBox.Show(
                    $"Order {_collectionOrderNumber} loaded!" & vbCrLf & vbCrLf &
                    $"Customer: {customerName}" & vbCrLf &
                    $"Phone: {customerPhone}" & vbCrLf & vbCrLf &
                    $"Total: R{_collectionTotalAmount:N2}" & vbCrLf &
                    $"Deposit Paid: R{_collectionDepositPaid:N2}" & vbCrLf &
                    $"BALANCE DUE: R{balanceDue:N2}" & vbCrLf & vbCrLf &
                    "Please collect balance payment and finalize sale.",
                    "Order Ready for Collection",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information)
            End Using
        End Using
        
    Catch ex As Exception
        MessageBox.Show("Error loading order: " & ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        ExitCollectionMode()
    End Try
End Sub

Private Sub ShowCollectionBanner(customerName As String, phone As String, balance As Decimal)
    If pnlCollectionBanner Is Nothing Then
        pnlCollectionBanner = New Panel()
        pnlCollectionBanner.BackColor = Color.FromArgb(40, 167, 69) ' Green
        pnlCollectionBanner.Dock = DockStyle.Top
        pnlCollectionBanner.Height = 60
        pnlCollectionBanner.BringToFront()
        
        lblCollectionBanner = New Label()
        lblCollectionBanner.Dock = DockStyle.Fill
        lblCollectionBanner.Font = New Font("Segoe UI", 12, FontStyle.Bold)
        lblCollectionBanner.ForeColor = Color.White
        lblCollectionBanner.TextAlign = ContentAlignment.MiddleCenter
        
        pnlCollectionBanner.Controls.Add(lblCollectionBanner)
        Me.Controls.Add(pnlCollectionBanner)
    End If
    
    lblCollectionBanner.Text = $"📦 ORDER COLLECTION | Order: {_collectionOrderNumber} | Customer: {customerName} | Phone: {phone} | BALANCE DUE: R{balance:N2}"
    pnlCollectionBanner.Visible = True
End Sub

Private Sub ExitCollectionMode()
    _isOrderCollectionMode = False
    _collectionOrderID = 0
    _collectionOrderNumber = ""
    _collectionDepositPaid = 0
    _collectionTotalAmount = 0
    
    If pnlCollectionBanner IsNot Nothing Then
        pnlCollectionBanner.Visible = False
    End If
End Sub

Private pnlCollectionBanner As Panel
Private lblCollectionBanner As Label
```

### Step 2: Modify ProcessSale Method

```vb
Private Sub ProcessSale(tenderAmount As Decimal)
    Using conn As New SqlConnection(_connString)
        conn.Open()
        Using trans = conn.BeginTransaction()
            Try
                Dim saleId As Integer
                
                If _isOrderCollectionMode Then
                    ' Process as order collection
                    saleId = CreateOrderCollectionSale(conn, trans, tenderAmount)
                    
                    ' Mark order as Delivered
                    Dim cmdUpdate As New SqlCommand("
                        UPDATE POS_CustomOrders 
                        SET OrderStatus = 'Delivered',
                            CollectedDate = GETDATE()
                        WHERE OrderID = @orderId", conn, trans)
                    cmdUpdate.Parameters.AddWithValue("@orderId", _collectionOrderID)
                    cmdUpdate.ExecuteNonQuery()
                Else
                    ' Regular sale
                    saleId = CreateSaleHeader(conn, trans, tenderAmount)
                End If
                
                ' Create sale lines
                CreateSaleLines(conn, trans, saleId)
                
                trans.Commit()
                
                ' Show success
                If _isOrderCollectionMode Then
                    MessageBox.Show($"Order {_collectionOrderNumber} delivered successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information)
                    PrintCollectionReceipt(saleId)
                    ExitCollectionMode()
                Else
                    MessageBox.Show("Sale completed!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information)
                    PrintReceipt(saleId)
                End If
                
                ' Clear cart
                _cartItems.Clear()
                UpdateCartTotals()
                
            Catch ex As Exception
                trans.Rollback()
                Throw
            End Try
        End Using
    End Using
End Sub

Private Function CreateOrderCollectionSale(conn As SqlConnection, trans As SqlTransaction, tenderAmount As Decimal) As Integer
    Dim cmdSale As New SqlCommand("
        INSERT INTO Sales (
            BranchID, SaleDate, SaleType, ReferenceNumber,
            Subtotal, VAT, Total, TenderAmount, ChangeAmount,
            PaymentMethod, CreatedBy
        )
        VALUES (
            @branchId, GETDATE(), 'Order Collection', @orderNumber,
            @subtotal, @vat, @total, @tenderAmount, @changeAmount,
            @paymentMethod, @createdBy
        );
        SELECT SCOPE_IDENTITY();", conn, trans)
    
    cmdSale.Parameters.AddWithValue("@branchId", _currentBranchId)
    cmdSale.Parameters.AddWithValue("@orderNumber", _collectionOrderNumber)
    cmdSale.Parameters.AddWithValue("@subtotal", _currentSubtotal)
    cmdSale.Parameters.AddWithValue("@vat", _currentVAT)
    cmdSale.Parameters.AddWithValue("@total", _collectionTotalAmount)
    cmdSale.Parameters.AddWithValue("@tenderAmount", tenderAmount)
    cmdSale.Parameters.AddWithValue("@changeAmount", tenderAmount - (_collectionTotalAmount - _collectionDepositPaid))
    cmdSale.Parameters.AddWithValue("@paymentMethod", _tenderType)
    cmdSale.Parameters.AddWithValue("@createdBy", _currentUsername)
    
    Return Convert.ToInt32(cmdSale.ExecuteScalar())
End Function
```

### Step 3: Create ViewOrdersForm.vb (Read-Only)

```vb
Imports System.Data.SqlClient
Imports System.Configuration

Public Class ViewOrdersForm
    Private ReadOnly _connString As String
    Private ReadOnly _branchId As Integer
    
    Public Sub New(branchId As Integer)
        InitializeComponent()
        _connString = ConfigurationManager.ConnectionStrings("OvenDelightsConnectionString")?.ConnectionString
        _branchId = branchId
    End Sub
    
    Private Sub ViewOrdersForm_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        LoadOrders()
    End Sub
    
    Private Sub LoadOrders()
        Try
            Using conn As New SqlConnection(_connString)
                conn.Open()
                
                Dim sql As String = "
                    SELECT 
                        OrderNumber AS [Order #],
                        CustomerName + ' ' + CustomerSurname AS Customer,
                        CustomerPhone AS Phone,
                        CONVERT(VARCHAR, ReadyDate, 106) + ' ' + CONVERT(VARCHAR, ReadyTime, 108) AS [Ready Date/Time],
                        TotalAmount AS Total,
                        DepositPaid AS Deposit,
                        BalanceDue AS Balance,
                        OrderStatus AS Status
                    FROM POS_CustomOrders
                    WHERE BranchID = @branchId
                    AND OrderStatus IN ('New', 'Ready')
                    ORDER BY ReadyDate, ReadyTime"
                
                Using da As New SqlDataAdapter(sql, conn)
                    da.SelectCommand.Parameters.AddWithValue("@branchId", _branchId)
                    Dim dt As New DataTable()
                    da.Fill(dt)
                    dgvOrders.DataSource = dt
                    
                    ' Color code by status
                    For Each row As DataGridViewRow In dgvOrders.Rows
                        Dim status As String = row.Cells("Status").Value.ToString()
                        If status = "New" Then
                            row.DefaultCellStyle.BackColor = Color.LightYellow
                            row.Cells("Status").Style.BackColor = Color.Orange
                            row.Cells("Status").Style.ForeColor = Color.White
                        ElseIf status = "Ready" Then
                            row.DefaultCellStyle.BackColor = Color.LightGreen
                            row.Cells("Status").Style.BackColor = Color.Green
                            row.Cells("Status").Style.ForeColor = Color.White
                        End If
                    Next
                    
                    lblOrderCount.Text = $"{dt.Rows.Count} orders"
                End Using
            End Using
        Catch ex As Exception
            MessageBox.Show("Error loading orders: " & ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub
    
    Private Sub btnClose_Click(sender As Object, e As EventArgs) Handles btnClose.Click
        Me.Close()
    End Sub
End Class
```

---

## Summary

### POS Solution (Frontend):

**F10 - Custom Cake Order:**
- Add items to cart
- Press F10
- Enter customer details
- Pay deposit
- Order created with status "New"

**F11 - View New Orders:**
- Read-only view
- Shows New and Ready orders
- Color-coded by status
- Cannot modify

**F12 - Order Collection:**
- Enter order number
- System checks if status = "Ready"
- If "New" → Shows "Not Ready" message
- If "Ready" → Loads items, tender balance
- Complete payment
- Order status → "Delivered"

---

## Complete Workflow

```
1. POS: Customer orders cake (F10)
   → Status: "New"
   → Manufacturer notified

2. ERP: Manufacturer sees order in Manufacturing → Orders
   → Status: "New" (Orange badge)
   → Views details, prepares order

3. ERP: Manufacturer completes order
   → Clicks "Mark as Ready"
   → Status: "Ready"

4. POS: Customer arrives to collect (F12)
   → Cashier enters order number
   → System checks status
   → If "New" → "Not Ready" message
   → If "Ready" → Load items, tender balance

5. POS: Cashier completes payment
   → Status: "Delivered"
   → Customer receives order
```

---

This is the complete two-solution implementation!
