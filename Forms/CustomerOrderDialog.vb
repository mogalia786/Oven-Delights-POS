Imports System.Data.SqlClient
Imports System.Configuration

Public Class CustomerOrderDialog
    Private ReadOnly _branchID As Integer
    Private ReadOnly _tillPointID As Integer
    Private ReadOnly _cashierID As Integer
    Private ReadOnly _cashierName As String
    Private ReadOnly _cartItems As DataTable
    Private ReadOnly _subtotal As Decimal
    Private ReadOnly _tax As Decimal
    Private ReadOnly _total As Decimal
    Private ReadOnly _connString As String
    Private _amendedTotal As Decimal = 0

    Public Sub New(branchID As Integer, tillPointID As Integer, cashierID As Integer, cashierName As String,
                   cartItems As DataTable, subtotal As Decimal, tax As Decimal, total As Decimal)
        InitializeComponent()
        
        _branchID = branchID
        _tillPointID = tillPointID
        _cashierID = cashierID
        _cashierName = cashierName
        _cartItems = cartItems
        
        ' POSMainForm passes: subtotal (cart total), tax (15% added), total (subtotal + tax)
        ' But cart prices are VAT-INCLUSIVE, so work backward from the cart subtotal
        ' Cart Subtotal is actually the VAT-inclusive amount
        _total = subtotal  ' This is the real total (VAT-inclusive from cart)
        _subtotal = Math.Round(subtotal / 1.15D, 2)  ' Work backward to get VAT-exclusive
        _tax = Math.Round(_total - _subtotal, 2)  ' Calculate actual VAT
        
        _connString = ConfigurationManager.ConnectionStrings("OvenDelightsERPConnectionString")?.ConnectionString
    End Sub

    Private Sub CustomerOrderDialog_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        Try
            ' Load order items into grid
            dgvItems.DataSource = _cartItems.Copy()
            
            ' Display totals (VAT-exclusive breakdown)
            lblSubtotal.Text = $"Subtotal (excl VAT): R{_subtotal:N2}"
            lblTax.Text = $"VAT (15%): R{_tax:N2}"
            lblTotal.Text = $"TOTAL: R{_total:N2}"
            
            ' Set default ready date/time (tomorrow, 10 AM)
            dtpReadyDate.Value = DateTime.Now.AddDays(1)
            dtpReadyTime.Value = New DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, 10, 0, 0)
            
            ' Wire up event handlers BEFORE calling UpdateCollectionDay
            AddHandler dtpReadyDate.ValueChanged, AddressOf OnDateChanged
            AddHandler txtCellNumber.Leave, AddressOf OnCellNumberLeave
            
            ' Auto-fill Collection Day (must be called AFTER controls are initialized)
            UpdateCollectionDay()
            
            ' Set default deposit to 50% of total
            txtDepositAmount.Text = Math.Round(_total * 0.5D, 2).ToString("0.00")
            UpdateBalanceDue()
            
            ' Wire up deposit amount change
            AddHandler txtDepositAmount.TextChanged, AddressOf OnDepositChanged
            
            ' Wire up Amend Total button
            AddHandler btnAmendTotal.Click, AddressOf OnAmendTotalClick
            
            ' Focus on cell number (first field)
            txtCellNumber.Focus()
        Catch ex As Exception
            MessageBox.Show($"Error loading form: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    Private Sub OnDepositChanged(sender As Object, e As EventArgs)
        UpdateBalanceDue()
    End Sub

    Private Sub UpdateBalanceDue()
        Try
            Dim deposit As Decimal = 0
            If Decimal.TryParse(txtDepositAmount.Text, deposit) Then
                Dim currentTotal = If(_amendedTotal > 0, _amendedTotal, _total)
                Dim balance = currentTotal - deposit
                lblBalanceDue.Text = $"Balance Due: R{balance:N2}"
                lblBalanceDue.ForeColor = If(balance > 0, Color.Red, Color.Green)
            End If
        Catch
            ' Ignore parse errors
        End Try
    End Sub
    
    Private Sub OnAmendTotalClick(sender As Object, e As EventArgs)
        ' Require supervisor authentication to amend total
        Try
            ' Show supervisor login dialog
            Dim supervisorAuth = AuthenticateSupervisor()
            If Not supervisorAuth Then
                MessageBox.Show("Supervisor authentication required to amend total!", "Access Denied", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                Return
            End If
            
            Dim currentTotal = If(_amendedTotal > 0, _amendedTotal, _total)
            
            ' Create numpad form
            Using numpadForm As New Form()
                numpadForm.Text = "Amend Total"
                numpadForm.Size = New Size(400, 750)
                numpadForm.StartPosition = FormStartPosition.CenterParent
                numpadForm.FormBorderStyle = FormBorderStyle.FixedDialog
                numpadForm.MaximizeBox = False
                numpadForm.MinimizeBox = False
                
                ' Display label
                Dim lblDisplay As New Label With {
                    .Text = currentTotal.ToString("0.00"),
                    .Font = New Font("Segoe UI", 32, FontStyle.Bold),
                    .TextAlign = ContentAlignment.MiddleRight,
                    .Dock = DockStyle.Top,
                    .Height = 80,
                    .BackColor = Color.Black,
                    .ForeColor = Color.Lime,
                    .Padding = New Padding(10, 15, 20, 15),
                    .AutoSize = False
                }
                numpadForm.Controls.Add(lblDisplay)
                
                ' Flag to track if user has started typing
                Dim isFirstDigit As Boolean = True
                
                ' Numpad panel
                Dim pnlNumpad As New Panel With {.Dock = DockStyle.Fill, .Padding = New Padding(20, 40, 20, 20)}
                
                Dim buttons() As String = {"7", "8", "9", "4", "5", "6", "1", "2", "3", "C", "0", "."}
                Dim btnSize As New Size(100, 80)
                Dim gap As Integer = 10
                
                For i = 0 To buttons.Length - 1
                    Dim row = i \ 3
                    Dim col = i Mod 3
                    Dim btnText = buttons(i)
                    
                    Dim btn As New Button With {
                        .Text = btnText,
                        .Size = btnSize,
                        .Location = New Point(20 + (col * (btnSize.Width + gap)), 70 + (row * (btnSize.Height + gap))),
                        .Font = New Font("Segoe UI", 24, FontStyle.Bold),
                        .BackColor = If(btnText = "C", Color.Red, Color.FromArgb(52, 73, 94)),
                        .ForeColor = Color.White,
                        .FlatStyle = FlatStyle.Flat,
                        .Cursor = Cursors.Hand
                    }
                    btn.FlatAppearance.BorderSize = 0
                    
                    AddHandler btn.Click, Sub(s, ev)
                        If btnText = "C" Then
                            lblDisplay.Text = "0.00"
                            isFirstDigit = True
                        ElseIf btnText = "." Then
                            If isFirstDigit Then
                                lblDisplay.Text = "0."
                                isFirstDigit = False
                            ElseIf Not lblDisplay.Text.Contains(".") Then
                                lblDisplay.Text &= "."
                            End If
                        Else
                            ' First digit replaces the entire display (override behavior)
                            If isFirstDigit Then
                                lblDisplay.Text = btnText
                                isFirstDigit = False
                            Else
                                lblDisplay.Text &= btnText
                            End If
                        End If
                    End Sub
                    
                    pnlNumpad.Controls.Add(btn)
                Next
                
                numpadForm.Controls.Add(pnlNumpad)
                
                ' Confirm button
                Dim btnConfirm As New Button With {
                    .Text = "✓ CONFIRM",
                    .Dock = DockStyle.Bottom,
                    .Height = 60,
                    .BackColor = Color.Green,
                    .ForeColor = Color.White,
                    .Font = New Font("Segoe UI", 18, FontStyle.Bold),
                    .FlatStyle = FlatStyle.Flat
                }
                btnConfirm.FlatAppearance.BorderSize = 0
                AddHandler btnConfirm.Click, Sub()
                    Dim newTotal As Decimal
                    If Decimal.TryParse(lblDisplay.Text, newTotal) AndAlso newTotal > 0 Then
                        _amendedTotal = newTotal
                        ' Recalculate subtotal and VAT based on new total (VAT-inclusive)
                        Dim newSubtotal = Math.Round(newTotal / 1.15D, 2)
                        Dim newTax = Math.Round(newTotal - newSubtotal, 2)
                        
                        ' Update all labels
                        lblSubtotal.Text = $"Subtotal (excl VAT): R{newSubtotal:N2}"
                        lblTax.Text = $"VAT (15%): R{newTax:N2}"
                        lblTotal.Text = $"TOTAL: R{_amendedTotal:N2}"
                        lblTotal.ForeColor = Color.Orange ' Show it's been amended
                        UpdateBalanceDue()
                        numpadForm.DialogResult = DialogResult.OK
                        numpadForm.Close()
                    Else
                        MessageBox.Show("Please enter a valid amount", "Invalid Amount", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                    End If
                End Sub
                numpadForm.Controls.Add(btnConfirm)
                
                numpadForm.ShowDialog()
            End Using
            
        Catch ex As Exception
            MessageBox.Show($"Error amending total: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    Private Sub OnDateChanged(sender As Object, e As EventArgs)
        UpdateCollectionDay()
    End Sub

    Private Sub UpdateCollectionDay()
        ' Display full day name (e.g., "Saturday")
        txtCollectionDay.Text = dtpReadyDate.Value.ToString("dddd")
    End Sub
    
    Private Function AuthenticateSupervisor() As Boolean
        ' Simple supervisor authentication dialog
        Using authForm As New Form()
            authForm.Text = "Supervisor Authentication"
            authForm.Size = New Size(400, 250)
            authForm.StartPosition = FormStartPosition.CenterParent
            authForm.FormBorderStyle = FormBorderStyle.FixedDialog
            authForm.MaximizeBox = False
            authForm.MinimizeBox = False
            
            Dim lblUsername As New Label With {
                .Text = "Username:",
                .Location = New Point(20, 20),
                .AutoSize = True
            }
            Dim txtUsername As New TextBox With {
                .Location = New Point(20, 45),
                .Size = New Size(340, 25)
            }
            
            Dim lblPassword As New Label With {
                .Text = "Password:",
                .Location = New Point(20, 80),
                .AutoSize = True
            }
            Dim txtPassword As New TextBox With {
                .Location = New Point(20, 105),
                .Size = New Size(340, 25),
                .UseSystemPasswordChar = True
            }
            
            Dim btnOK As New Button With {
                .Text = "OK",
                .Location = New Point(180, 150),
                .Size = New Size(80, 35),
                .DialogResult = DialogResult.OK
            }
            Dim btnCancel As New Button With {
                .Text = "Cancel",
                .Location = New Point(280, 150),
                .Size = New Size(80, 35),
                .DialogResult = DialogResult.Cancel
            }
            
            authForm.Controls.AddRange({lblUsername, txtUsername, lblPassword, txtPassword, btnOK, btnCancel})
            authForm.AcceptButton = btnOK
            authForm.CancelButton = btnCancel
            
            If authForm.ShowDialog() = DialogResult.OK Then
                ' Check credentials against database - RoleID 10 is Branch Supervisor
                ' Supervisor must belong to the same branch
                Try
                    Using conn As New SqlConnection(_connString)
                        conn.Open()
                        Dim sql = "SELECT COUNT(*) FROM Users WHERE Username = @Username AND Password = @Password AND RoleID = 10 AND BranchID = @BranchID AND IsActive = 1"
                        Using cmd As New SqlCommand(sql, conn)
                            cmd.Parameters.AddWithValue("@Username", txtUsername.Text.Trim())
                            cmd.Parameters.AddWithValue("@Password", txtPassword.Text.Trim())
                            cmd.Parameters.AddWithValue("@BranchID", _branchID)
                            Dim count = Convert.ToInt32(cmd.ExecuteScalar())
                            Return count > 0
                        End Using
                    End Using
                Catch ex As Exception
                    MessageBox.Show($"Authentication error: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
                    Return False
                End Try
            End If
            
            Return False
        End Using
    End Function

    Private Sub OnCellNumberLeave(sender As Object, e As EventArgs)
        ' Auto-lookup customer when leaving cell number field
        If Not String.IsNullOrWhiteSpace(txtCellNumber.Text) Then
            LookupCustomer(txtCellNumber.Text.Trim())
        End If
    End Sub

    Private Sub LookupCustomer(cellNumber As String)
        Try
            ' Only lookup if cell number is valid (at least 10 digits)
            If cellNumber.Length < 10 Then
                Return
            End If
            
            Using conn As New SqlConnection(_connString)
                conn.Open()
                Dim sql = "SELECT FirstName, Surname, Email FROM POS_Customers WHERE CellNumber = @CellNumber AND IsActive = 1"
                Using cmd As New SqlCommand(sql, conn)
                    cmd.Parameters.AddWithValue("@CellNumber", cellNumber)
                    Using reader = cmd.ExecuteReader()
                        If reader.Read() Then
                            ' Customer found - auto-populate with visual feedback
                            txtCustomerName.Text = reader("FirstName").ToString()
                            txtCustomerSurname.Text = reader("Surname").ToString()
                            If Not IsDBNull(reader("Email")) Then
                                txtCustomerEmail.Text = reader("Email").ToString()
                            End If
                            
                            ' Visual feedback - flash the fields green briefly
                            txtCustomerName.BackColor = Color.LightGreen
                            txtCustomerSurname.BackColor = Color.LightGreen
                            txtCustomerEmail.BackColor = Color.LightGreen
                            
                            ' Reset colors after 1 second
                            Dim timer As New Timer()
                            timer.Interval = 1000
                            AddHandler timer.Tick, Sub()
                                txtCustomerName.BackColor = Color.White
                                txtCustomerSurname.BackColor = Color.White
                                txtCustomerEmail.BackColor = Color.White
                                timer.Stop()
                                timer.Dispose()
                            End Sub
                            timer.Start()
                        End If
                    End Using
                End Using
            End Using
        Catch ex As Exception
            ' Silently fail - customer not found is OK
            System.Diagnostics.Debug.WriteLine($"Customer lookup error: {ex.Message}")
        End Try
    End Sub

    Private Sub btnCreateOrder_Click(sender As Object, e As EventArgs) Handles btnCreateOrder.Click
        Try
            ' Validate inputs
            If String.IsNullOrWhiteSpace(txtCustomerName.Text) Then
                MessageBox.Show("Please enter customer name", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                txtCustomerName.Focus()
                Return
            End If

            If String.IsNullOrWhiteSpace(txtCustomerSurname.Text) Then
                MessageBox.Show("Please enter customer surname", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                txtCustomerSurname.Focus()
                Return
            End If

            If String.IsNullOrWhiteSpace(txtCellNumber.Text) Then
                MessageBox.Show("Please enter customer phone", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                txtCellNumber.Focus()
                Return
            End If

            Dim depositAmount As Decimal = 0
            If Not Decimal.TryParse(txtDepositAmount.Text, depositAmount) OrElse depositAmount <= 0 Then
                MessageBox.Show("Please enter valid deposit amount", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                txtDepositAmount.Focus()
                Return
            End If

            Dim finalTotal = If(_amendedTotal > 0, _amendedTotal, _total)
            If depositAmount > finalTotal Then
                MessageBox.Show("Deposit cannot exceed total amount", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                txtDepositAmount.Focus()
                Return
            End If

            ' Save customer to database
            SaveCustomerToDatabase()
            
            ' Store deposit amount and close dialog to proceed to payment
            Me.Tag = depositAmount
            Me.DialogResult = DialogResult.OK
            Me.Close()

        Catch ex As Exception
            MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    Public Function GetOrderData() As (CustomerName As String, CustomerSurname As String, CustomerPhone As String, ReadyDate As DateTime, ReadyTime As TimeSpan, DepositAmount As Decimal, CollectionDay As String, SpecialInstructions As String, Colour As String, Picture As String, AmendedTotal As Decimal)
        Return (txtCustomerName.Text.Trim(), txtCustomerSurname.Text.Trim(), txtCellNumber.Text.Trim(), dtpReadyDate.Value.Date, dtpReadyTime.Value.TimeOfDay, CDec(Me.Tag), txtCollectionDay.Text, txtSpecialInstructions.Text.Trim(), txtColour.Text.Trim(), txtPicture.Text.Trim(), _amendedTotal)
    End Function

    Private Sub SaveCustomerToDatabase()
        ' Save new customer to POS_Customers table
        Try
            Using conn As New SqlConnection(_connString)
                conn.Open()
                Dim sql = "IF NOT EXISTS (SELECT 1 FROM POS_Customers WHERE CellNumber = @CellNumber) " &
                         "INSERT INTO POS_Customers (CellNumber, FirstName, Surname, Email, LastOrderDate, TotalOrders) " &
                         "VALUES (@CellNumber, @FirstName, @Surname, @Email, GETDATE(), 1) " &
                         "ELSE " &
                         "UPDATE POS_Customers SET LastOrderDate = GETDATE(), TotalOrders = TotalOrders + 1 WHERE CellNumber = @CellNumber"
                Using cmd As New SqlCommand(sql, conn)
                    cmd.Parameters.AddWithValue("@CellNumber", txtCellNumber.Text.Trim())
                    cmd.Parameters.AddWithValue("@FirstName", txtCustomerName.Text.Trim())
                    cmd.Parameters.AddWithValue("@Surname", txtCustomerSurname.Text.Trim())
                    cmd.Parameters.AddWithValue("@Email", If(String.IsNullOrWhiteSpace(txtCustomerEmail.Text), DBNull.Value, txtCustomerEmail.Text.Trim()))
                    cmd.ExecuteNonQuery()
                End Using
            End Using
        Catch ex As Exception
            ' Silently fail - not critical
        End Try
    End Sub

    Private Sub CreateCustomOrder(depositAmount As Decimal)
        Try
            Using conn As New SqlConnection(_connString)
                conn.Open()
                Using transaction = conn.BeginTransaction()
                    Try
                        ' Generate order number
                        Dim branchPrefix As String = GetBranchPrefix(conn, transaction)
                        Dim orderNumber As String = GenerateOrderNumber(conn, transaction, branchPrefix)

                        ' Insert order header
                        Dim sqlOrder As String = "
                            INSERT INTO POS_CustomOrders (
                                OrderNumber, BranchID, OrderType,
                                CustomerName, CustomerSurname, CustomerPhone,
                                OrderDate, ReadyDate, ReadyTime,
                                TotalAmount, DepositPaid, BalanceDue,
                                OrderStatus, CreatedBy
                            ) VALUES (
                                @OrderNumber, @BranchID, 'Order',
                                @CustomerName, @CustomerSurname, @CustomerPhone,
                                GETDATE(), @ReadyDate, @ReadyTime,
                                @TotalAmount, @DepositPaid, @BalanceDue,
                                'New', @CreatedBy
                            );
                            SELECT SCOPE_IDENTITY();"

                        Dim orderID As Integer
                        Using cmd As New SqlCommand(sqlOrder, conn, transaction)
                            cmd.Parameters.AddWithValue("@OrderNumber", orderNumber)
                            cmd.Parameters.AddWithValue("@BranchID", _branchID)
                            cmd.Parameters.AddWithValue("@CustomerName", txtCustomerName.Text.Trim())
                            cmd.Parameters.AddWithValue("@CustomerSurname", txtCustomerSurname.Text.Trim())
                            cmd.Parameters.AddWithValue("@CustomerPhone", txtCellNumber.Text.Trim())
                            cmd.Parameters.AddWithValue("@ReadyDate", dtpReadyDate.Value.Date)
                            cmd.Parameters.AddWithValue("@ReadyTime", dtpReadyTime.Value.TimeOfDay)
                            cmd.Parameters.AddWithValue("@TotalAmount", _total)
                            cmd.Parameters.AddWithValue("@DepositPaid", depositAmount)
                            cmd.Parameters.AddWithValue("@BalanceDue", _total - depositAmount)
                            cmd.Parameters.AddWithValue("@CreatedBy", _cashierName)
                            
                            orderID = Convert.ToInt32(cmd.ExecuteScalar())
                        End Using

                        ' Insert order items
                        Dim sqlItems As String = "
                            INSERT INTO POS_CustomOrderItems (
                                OrderID, ProductID, ProductName, Quantity, UnitPrice, LineTotal
                            ) VALUES (
                                @OrderID, @ProductID, @ProductName, @Quantity, @UnitPrice, @LineTotal
                            )"

                        For Each row As DataRow In _cartItems.Rows
                            Using cmd As New SqlCommand(sqlItems, conn, transaction)
                                cmd.Parameters.AddWithValue("@OrderID", orderID)
                                cmd.Parameters.AddWithValue("@ProductID", row("ProductID"))
                                cmd.Parameters.AddWithValue("@ProductName", row("Product"))
                                cmd.Parameters.AddWithValue("@Quantity", row("Qty"))
                                cmd.Parameters.AddWithValue("@UnitPrice", row("Price"))
                                cmd.Parameters.AddWithValue("@LineTotal", row("Total"))
                                cmd.ExecuteNonQuery()
                            End Using
                        Next

                        ' Record deposit payment as order transaction (NOT regular sale)
                        Dim sqlPayment As String = "
                            INSERT INTO Demo_Sales (
                                SaleNumber, InvoiceNumber, BranchID, TillPointID, CashierID, SaleDate,
                                Subtotal, TaxAmount, TotalAmount, PaymentMethod, CashAmount, CardAmount, SaleType, ReferenceNumber
                            ) VALUES (
                                @OrderNumber, @OrderNumber, @BranchID, @TillPointID, @CashierID, GETDATE(),
                                @Subtotal, @TaxAmount, @TotalAmount, 'Cash', @TotalAmount, 0, 'OrderDeposit', @ReferenceNumber
                            )"

                        Using cmd As New SqlCommand(sqlPayment, conn, transaction)
                            cmd.Parameters.AddWithValue("@OrderNumber", orderNumber)
                            cmd.Parameters.AddWithValue("@BranchID", _branchID)
                            cmd.Parameters.AddWithValue("@TillPointID", _tillPointID)
                            cmd.Parameters.AddWithValue("@CashierID", _cashierID)
                            cmd.Parameters.AddWithValue("@Subtotal", depositAmount / 1.15D)
                            cmd.Parameters.AddWithValue("@TaxAmount", depositAmount - (depositAmount / 1.15D))
                            cmd.Parameters.AddWithValue("@TotalAmount", depositAmount)
                            cmd.Parameters.AddWithValue("@ReferenceNumber", orderNumber)
                            cmd.ExecuteNonQuery()
                        End Using

                        transaction.Commit()

                        ' Print order receipt to continuous printer
                        Try
                            Dim printer As New POSReceiptPrinter()
                            Dim orderData As New Dictionary(Of String, Object) From {
                                {"OrderNumber", orderNumber},
                                {"CustomerName", $"{txtCustomerName.Text} {txtCustomerSurname.Text}"},
                                {"Telephone", If(txtCustomerEmail IsNot Nothing, txtCustomerEmail.Text, "")},
                                {"CellNumber", txtCellNumber.Text},
                                {"CakeColour", ""},
                                {"CollectionDate", dtpReadyDate.Value.ToString("dd MMM yyyy")},
                                {"CollectionTime", dtpReadyTime.Value.ToString("HH:mm")},
                                {"SpecialRequest", If(txtSpecialInstructions IsNot Nothing, txtSpecialInstructions.Text, "")},
                                {"OrderDetails", BuildOrderDetailsString()},
                                {"InvoiceTotal", _total.ToString("N2")},
                                {"DepositPaid", depositAmount.ToString("N2")},
                                {"BalanceOwing", (_total - depositAmount).ToString("N2")}
                            }
                            printer.PrintCustomOrderReceipt(_branchID, orderNumber, orderData)
                        Catch printEx As Exception
                            MessageBox.Show($"Print error: {printEx.Message}", "Print Error", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                        End Try
                        
                        ' Show success message with order details
                        Dim readyDateTime As String = dtpReadyDate.Value.ToString("dd MMM yyyy") & " at " & dtpReadyTime.Value.ToString("HH:mm")
                        MessageBox.Show($"ORDER CREATED SUCCESSFULLY!" & vbCrLf & vbCrLf &
                                      $"Order Number: {orderNumber}" & vbCrLf &
                                      $"Customer: {txtCustomerName.Text} {txtCustomerSurname.Text}" & vbCrLf &
                                      $"Phone: {txtCellNumber.Text}" & vbCrLf & vbCrLf &
                                      $"Ready: {readyDateTime}" & vbCrLf & vbCrLf &
                                      $"Total: R{_total:N2}" & vbCrLf &
                                      $"Deposit Paid: R{depositAmount:N2}" & vbCrLf &
                                      $"Balance Due: R{(_total - depositAmount):N2}" & vbCrLf & vbCrLf &
                                      "Order sent to manufacturing.",
                                      "Order Created", MessageBoxButtons.OK, MessageBoxIcon.Information)

                        Me.DialogResult = DialogResult.OK
                        Me.Close()

                    Catch ex As Exception
                        transaction.Rollback()
                        Throw
                    End Try
                End Using
            End Using

        Catch ex As Exception
            MessageBox.Show($"Error creating order: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    Private Function GetBranchPrefix(conn As SqlConnection, transaction As SqlTransaction) As String
        Try
            Dim cmd As New SqlCommand("SELECT BranchCode FROM Branches WHERE BranchID = @bid", conn, transaction)
            cmd.Parameters.AddWithValue("@bid", _branchID)
            Dim result = cmd.ExecuteScalar()
            Return If(result IsNot Nothing, result.ToString(), "BR")
        Catch
            Return "BR"
        End Try
    End Function

    Private Function GenerateOrderNumber(conn As SqlConnection, transaction As SqlTransaction, branchPrefix As String) As String
        Dim cmd As New SqlCommand("SELECT ISNULL(MAX(CAST(SUBSTRING(OrderNumber, LEN(@prefix) + 3, 6) AS INT)), 0) + 1 FROM POS_CustomOrders WHERE OrderNumber LIKE @pattern", conn, transaction)
        cmd.Parameters.AddWithValue("@prefix", branchPrefix)
        cmd.Parameters.AddWithValue("@pattern", $"O-{branchPrefix}-%")
        Dim nextNumber As Integer = Convert.ToInt32(cmd.ExecuteScalar())
        Return $"O-{branchPrefix}-{nextNumber.ToString().PadLeft(6, "0"c)}"
    End Function

    Private Function BuildOrderDetailsString() As String
        Dim details As New System.Text.StringBuilder()
        For Each row As DataRow In _cartItems.Rows
            Dim itemName As String = row("Product").ToString()
            If itemName.Length > 18 Then itemName = itemName.Substring(0, 18)
            Dim qty As Decimal = CDec(row("Qty"))
            Dim price As Decimal = CDec(row("Price"))
            Dim total As Decimal = CDec(row("Total"))
            details.AppendLine($"{itemName,-18} {qty,4:0.00} {price,7:N2} {total,8:N2}")
        Next
        Return details.ToString()
    End Function

    Private Sub btnCancel_Click(sender As Object, e As EventArgs) Handles btnCancel.Click
        Me.DialogResult = DialogResult.Cancel
        Me.Close()
    End Sub

    Private Sub txtDepositAmount_TextChanged(sender As Object, e As EventArgs) Handles txtDepositAmount.TextChanged
        Dim depositAmount As Decimal = 0
        If Decimal.TryParse(txtDepositAmount.Text, depositAmount) Then
            Dim balance As Decimal = _total - depositAmount
            lblBalanceDue.Text = $"Balance Due: R{balance:N2}"
        Else
            lblBalanceDue.Text = "Balance Due: R0.00"
        End If
    End Sub
End Class
