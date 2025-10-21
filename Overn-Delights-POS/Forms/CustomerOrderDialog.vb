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
        _subtotal = subtotal
        _tax = tax
        _total = total
        _connString = ConfigurationManager.ConnectionStrings("OvenDelightsERPConnectionString")?.ConnectionString
    End Sub

    Private Sub CustomerOrderDialog_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        ' Load order items into grid
        dgvItems.DataSource = _cartItems.Copy()
        
        ' Display totals
        lblSubtotal.Text = $"Subtotal: R{_subtotal:N2}"
        lblTax.Text = $"VAT (15%): R{_tax:N2}"
        lblTotal.Text = $"TOTAL: R{_total:N2}"
        
        ' Set default ready date/time (tomorrow, 10 AM)
        dtpReadyDate.Value = DateTime.Now.AddDays(1)
        dtpReadyTime.Value = New DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, 10, 0, 0)
        
        ' Focus on customer name
        txtCustomerName.Focus()
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

            If String.IsNullOrWhiteSpace(txtCustomerPhone.Text) Then
                MessageBox.Show("Please enter customer phone", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                txtCustomerPhone.Focus()
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

            ' Store deposit amount and close dialog to proceed to payment
            Me.Tag = depositAmount
            Me.DialogResult = DialogResult.OK
            Me.Close()

        Catch ex As Exception
            MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    Public Function GetOrderData() As (CustomerName As String, CustomerSurname As String, CustomerPhone As String, ReadyDate As DateTime, ReadyTime As TimeSpan, DepositAmount As Decimal)
        Return (txtCustomerName.Text.Trim(), txtCustomerSurname.Text.Trim(), txtCustomerPhone.Text.Trim(), dtpReadyDate.Value.Date, dtpReadyTime.Value.TimeOfDay, CDec(Me.Tag))
    End Function

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
                            cmd.Parameters.AddWithValue("@CustomerPhone", txtCustomerPhone.Text.Trim())
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
                                      $"Phone: {txtCustomerPhone.Text}" & vbCrLf & vbCrLf &
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
