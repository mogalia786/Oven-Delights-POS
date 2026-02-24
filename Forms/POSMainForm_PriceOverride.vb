' Price Override Extension for POSMainForm
' Add these methods and modifications to POSMainForm.vb

' ===== ADD TO CLASS VARIABLES SECTION =====
Private _priceOverrides As New Dictionary(Of Integer, PriceOverride)

' ===== MODIFY InitializeCart METHOD =====
Private Sub InitializeCart()
    _cartItems.Columns.Add("ProductID", GetType(Integer))
    _cartItems.Columns.Add("ItemCode", GetType(String))
    _cartItems.Columns.Add("Product", GetType(String))
    _cartItems.Columns.Add("Qty", GetType(Decimal))
    _cartItems.Columns.Add("Price", GetType(Decimal))
    _cartItems.Columns.Add("Total", GetType(Decimal))
    _cartItems.Columns.Add("PriceOverridden", GetType(Boolean)) ' NEW COLUMN

    dgvCart.AutoGenerateColumns = False ' CHANGED TO FALSE
    dgvCart.DataSource = _cartItems

    ' Manually add columns
    dgvCart.Columns.Add(New DataGridViewTextBoxColumn With {
        .Name = "ProductID",
        .DataPropertyName = "ProductID",
        .Visible = False
    })
    
    dgvCart.Columns.Add(New DataGridViewTextBoxColumn With {
        .Name = "ItemCode",
        .DataPropertyName = "ItemCode",
        .HeaderText = "Code",
        .Width = 70
    })
    
    dgvCart.Columns.Add(New DataGridViewTextBoxColumn With {
        .Name = "Product",
        .DataPropertyName = "Product",
        .HeaderText = "Item",
        .AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill
    })
    
    dgvCart.Columns.Add(New DataGridViewTextBoxColumn With {
        .Name = "Qty",
        .DataPropertyName = "Qty",
        .HeaderText = "Qty",
        .Width = 50,
        .ReadOnly = False
    })
    
    ' Add multiply button column for quantity
    Dim btnMultiplyCol As New DataGridViewButtonColumn With {
        .Name = "MultiplyBtn",
        .HeaderText = "×",
        .Text = "×",
        .UseColumnTextForButtonValue = True,
        .Width = 30
    })
    dgvCart.Columns.Add(btnMultiplyCol)
    
    dgvCart.Columns.Add(New DataGridViewTextBoxColumn With {
        .Name = "Price",
        .DataPropertyName = "Price",
        .HeaderText = "Price",
        .Width = 80,
        .DefaultCellStyle = New DataGridViewCellStyle With {.Format = "C4"}
    })
    
    ' Add price override button column
    Dim btnPriceCol As New DataGridViewButtonColumn With {
        .Name = "PriceBtn",
        .HeaderText = "R",
        .Text = "R",
        .UseColumnTextForButtonValue = True,
        .Width = 30,
        .DefaultCellStyle = New DataGridViewCellStyle With {
            .BackColor = Color.FromArgb(255, 193, 7),
            .ForeColor = Color.Black,
            .Font = New Font("Segoe UI", 10, FontStyle.Bold)
        }
    })
    dgvCart.Columns.Add(btnPriceCol)
    
    dgvCart.Columns.Add(New DataGridViewTextBoxColumn With {
        .Name = "Total",
        .DataPropertyName = "Total",
        .HeaderText = "Total",
        .Width = 90,
        .DefaultCellStyle = New DataGridViewCellStyle With {.Format = "C2"}
    })
    
    dgvCart.Columns.Add(New DataGridViewTextBoxColumn With {
        .Name = "PriceOverridden",
        .DataPropertyName = "PriceOverridden",
        .Visible = False
    })

    ' Handle button clicks
    AddHandler dgvCart.CellContentClick, AddressOf dgvCart_CellContentClick
    AddHandler dgvCart.CellFormatting, AddressOf dgvCart_CellFormatting
End Sub

' ===== ADD NEW METHOD: Handle Button Clicks =====
Private Sub dgvCart_CellContentClick(sender As Object, e As DataGridViewCellEventArgs)
    If e.RowIndex < 0 Then Return
    
    Dim colName = dgvCart.Columns(e.ColumnIndex).Name
    
    Select Case colName
        Case "MultiplyBtn"
            ChangeQuantity()
            
        Case "PriceBtn"
            OverridePrice(e.RowIndex)
    End Select
End Sub

' ===== ADD NEW METHOD: Cell Formatting for Visual Indicators =====
Private Sub dgvCart_CellFormatting(sender As Object, e As DataGridViewCellFormattingEventArgs)
    If e.RowIndex < 0 Then Return
    
    ' Check if price was overridden
    If dgvCart.Rows(e.RowIndex).Cells("PriceOverridden").Value IsNot Nothing AndAlso
       CBool(dgvCart.Rows(e.RowIndex).Cells("PriceOverridden").Value) Then
        
        ' Apply gold background to price cells
        If dgvCart.Columns(e.ColumnIndex).Name = "Price" OrElse
           dgvCart.Columns(e.ColumnIndex).Name = "Total" Then
            e.CellStyle.BackColor = Color.FromArgb(255, 248, 220) ' Light gold
            e.CellStyle.Font = New Font(e.CellStyle.Font, FontStyle.Bold)
        End If
    End If
End Sub

' ===== ADD NEW METHOD: Override Price =====
Private Sub OverridePrice(rowIndex As Integer)
    Try
        ' Get current row data
        Dim row = dgvCart.Rows(rowIndex)
        Dim productName = row.Cells("Product").Value.ToString()
        Dim originalPrice = CDec(row.Cells("Price").Value)
        Dim quantity = CDec(row.Cells("Qty").Value)
        
        ' Authenticate supervisor
        Dim authDialog As New RetailManagerAuthDialog()
        If authDialog.ShowDialog(Me) <> DialogResult.OK Then
            Return
        End If
        
        ' Show price override dialog
        Dim priceDialog As New PriceOverrideDialog(productName, originalPrice)
        If priceDialog.ShowDialog(Me) = DialogResult.OK Then
            Dim newPrice = priceDialog.NewPrice
            
            ' Update the row
            row.Cells("Price").Value = newPrice
            row.Cells("Total").Value = newPrice * quantity
            row.Cells("PriceOverridden").Value = True
            
            ' Store override info
            _priceOverrides(rowIndex) = New PriceOverride With {
                .NewPrice = newPrice,
                .SupervisorUsername = authDialog.AuthenticatedUsername,
                .OverrideDate = DateTime.Now
            }
            
            ' Recalculate totals
            CalculateTotals()
            
            ' Refresh the row to show visual indicator
            dgvCart.InvalidateRow(rowIndex)
            
            MessageBox.Show($"Price updated to R {newPrice:N4}", "Price Override", MessageBoxButtons.OK, MessageBoxIcon.Information)
        End If
        
    Catch ex As Exception
        MessageBox.Show($"Error overriding price: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
    End Try
End Sub

' ===== MODIFY AddProductToCart METHOD =====
Private Sub AddProductToCart(productID As Integer, itemCode As String, productName As String, price As Decimal)
    ' Check if product already exists in cart
    Dim existingRow = _cartItems.Select($"ProductID = {productID}")
    
    If existingRow.Length > 0 Then
        existingRow(0)("Qty") = CDec(existingRow(0)("Qty")) + 1
        existingRow(0)("Total") = CDec(existingRow(0)("Qty")) * CDec(existingRow(0)("Price"))
    Else
        _cartItems.Rows.Add(productID, itemCode, productName, 1, price, price, False) ' Added False for PriceOverridden
    End If

    CalculateTotals()
End Sub

' ===== MODIFY ChangeQuantity METHOD =====
Private Sub ChangeQuantity()
    If dgvCart.CurrentRow IsNot Nothing Then
        Dim currentQty = CDec(dgvCart.CurrentRow.Cells("Qty").Value)
        Dim input = InputBox("Enter new quantity:", "Change Quantity", currentQty.ToString())
        If Not String.IsNullOrEmpty(input) AndAlso IsNumeric(input) Then
            Dim newQty = CDec(input)
            dgvCart.CurrentRow.Cells("Qty").Value = newQty
            dgvCart.CurrentRow.Cells("Total").Value = newQty * CDec(dgvCart.CurrentRow.Cells("Price").Value)
            CalculateTotals()
        End If
    End If
End Sub

' ===== ADD TO ProcessPayment METHOD (when saving to database) =====
' When saving sale items to POS_SaleItems table, add this logic:
'
' For i = 0 To dgvCart.Rows.Count - 1
'     Dim row = dgvCart.Rows(i)
'     
'     ' ... existing code to insert sale item ...
'     
'     ' Add price override data if applicable
'     If _priceOverrides.ContainsKey(i) Then
'         Dim override = _priceOverrides(i)
'         cmd.Parameters.AddWithValue("@OverriddenPrice", override.NewPrice)
'         cmd.Parameters.AddWithValue("@PriceOverrideBy", override.SupervisorUsername)
'         cmd.Parameters.AddWithValue("@PriceOverrideDate", override.OverrideDate)
'     Else
'         cmd.Parameters.AddWithValue("@OverriddenPrice", DBNull.Value)
'         cmd.Parameters.AddWithValue("@PriceOverrideBy", DBNull.Value)
'         cmd.Parameters.AddWithValue("@PriceOverrideDate", DBNull.Value)
'     End If
' Next

' ===== ADD TO NewSale METHOD =====
Private Sub NewSale()
    _cartItems.Clear()
    _priceOverrides.Clear() ' Clear price overrides
    CalculateTotals()
    txtBarcodeScanner.Focus()
End Sub
