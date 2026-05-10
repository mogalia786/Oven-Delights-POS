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
                Dim balance = _total - deposit
                lblBalanceDue.Text = $"Balance Due: R{balance:N2}"
                lblBalanceDue.ForeColor = If(balance > 0, Color.Red, Color.Green)
            End If
        Catch
            ' Ignore parse errors
        End Try
    End Sub

    Private Sub OnDateChanged(sender As Object, e As EventArgs)
        UpdateCollectionDay()
    End Sub

    Private Sub UpdateCollectionDay()
        ' Display full day name (e.g., "Saturday")
        txtCollectionDay.Text = dtpReadyDate.Value.ToString("dddd")
    End Sub

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

            If depositAmount > _total Then
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

    Public Function GetOrderData() As (CustomerName As String, CustomerSurname As String, CustomerPhone As String, ReadyDate As DateTime, ReadyTime As TimeSpan, DepositAmount As Decimal, CollectionDay As String, SpecialInstructions As String)
        Return (txtCustomerName.Text.Trim(), txtCustomerSurname.Text.Trim(), txtCellNumber.Text.Trim(), dtpReadyDate.Value.Date, dtpReadyTime.Value.TimeOfDay, CDec(Me.Tag), txtCollectionDay.Text, txtSpecialInstructions.Text.Trim())
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
