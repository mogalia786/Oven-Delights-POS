Imports System.Windows.Forms
Imports System.Drawing
Imports System.Data.SqlClient
Imports System.Configuration

Public Class ReturnLineItemsForm
    Inherits Form

    Private _invoiceNumber As String
    Private _branchID As Integer
    Private _tillPointID As Integer
    Private _cashierID As Integer
    Private _supervisorID As Integer
    Private _connectionString As String
    Private _invoiceLines As New DataTable()
    Private _returnItems As New List(Of ReturnLineItem)

    Private flpLineItems As FlowLayoutPanel
    Private txtCustomerName As TextBox
    Private txtPhone As TextBox
    Private txtAddress As TextBox
    Private txtReason As TextBox
    Private lblTotalReturn As Label
    Private btnProcess As Button

    Private _darkBlue As Color = ColorTranslator.FromHtml("#2C3E50")
    Private _green As Color = ColorTranslator.FromHtml("#27AE60")
    Private _red As Color = ColorTranslator.FromHtml("#E74C3C")
    Private _orange As Color = ColorTranslator.FromHtml("#E67E22")
    Private _yellow As Color = ColorTranslator.FromHtml("#F39C12")

    Public Class ReturnLineItem
        Public Property ProductID As Integer
        Public Property ItemCode As String
        Public Property ProductName As String
        Public Property OriginalQty As Decimal
        Public Property ReturnQty As Decimal
        Public Property UnitPrice As Decimal
        Public Property LineTotal As Decimal
    End Class

    Public Sub New(invoiceNumber As String, branchID As Integer, tillPointID As Integer, cashierID As Integer, supervisorID As Integer)
        _invoiceNumber = invoiceNumber
        _branchID = branchID
        _tillPointID = tillPointID
        _cashierID = cashierID
        _supervisorID = supervisorID
        _connectionString = ConfigurationManager.ConnectionStrings("OvenDelightsERPConnectionString").ConnectionString

        InitializeComponent()
        LoadInvoiceLines()
    End Sub

    Private Sub InitializeComponent()
        Me.Text = $"Process Return - {_invoiceNumber}"
        Me.Size = New Size(1000, 800)
        Me.StartPosition = FormStartPosition.CenterScreen
        Me.FormBorderStyle = FormBorderStyle.FixedDialog
        Me.MaximizeBox = False
        Me.BackColor = Color.White

        ' Header
        Dim pnlHeader As New Panel With {
            .Dock = DockStyle.Top,
            .Height = 80,
            .BackColor = _darkBlue
        }

        Dim lblHeader As New Label With {
            .Text = $"🔄 RETURN - {_invoiceNumber}",
            .Font = New Font("Segoe UI", 20, FontStyle.Bold),
            .ForeColor = Color.White,
            .TextAlign = ContentAlignment.MiddleCenter,
            .Dock = DockStyle.Fill
        }
        pnlHeader.Controls.Add(lblHeader)

        ' Main content panel
        Dim pnlMain As New Panel With {
            .Dock = DockStyle.Fill,
            .AutoScroll = True,
            .Padding = New Padding(20)
        }

        ' Line items section
        Dim lblLineItems As New Label With {
            .Text = "SELECT ITEMS TO RETURN:",
            .Font = New Font("Segoe UI", 14, FontStyle.Bold),
            .Location = New Point(20, 10),
            .AutoSize = True
        }

        flpLineItems = New FlowLayoutPanel With {
            .Location = New Point(20, 45),
            .Size = New Size(940, 300),
            .AutoScroll = True,
            .FlowDirection = FlowDirection.TopDown,
            .WrapContents = False,
            .BorderStyle = BorderStyle.FixedSingle
        }

        ' Customer details section
        Dim lblCustomerDetails As New Label With {
            .Text = "CUSTOMER DETAILS:",
            .Font = New Font("Segoe UI", 14, FontStyle.Bold),
            .Location = New Point(20, 360),
            .AutoSize = True
        }

        Dim lblName As New Label With {
            .Text = "Customer Name:",
            .Font = New Font("Segoe UI", 11),
            .Location = New Point(20, 395),
            .AutoSize = True
        }

        txtCustomerName = New TextBox With {
            .Font = New Font("Segoe UI", 11),
            .Location = New Point(180, 392),
            .Width = 300
        }

        Dim lblPhone As New Label With {
            .Text = "Phone Number:",
            .Font = New Font("Segoe UI", 11),
            .Location = New Point(500, 395),
            .AutoSize = True
        }

        txtPhone = New TextBox With {
            .Font = New Font("Segoe UI", 11),
            .Location = New Point(640, 392),
            .Width = 200
        }

        Dim lblAddress As New Label With {
            .Text = "Address:",
            .Font = New Font("Segoe UI", 11),
            .Location = New Point(20, 435),
            .AutoSize = True
        }

        txtAddress = New TextBox With {
            .Font = New Font("Segoe UI", 11),
            .Location = New Point(180, 432),
            .Width = 660,
            .Multiline = True,
            .Height = 60
        }

        Dim lblReason As New Label With {
            .Text = "Reason for Return:",
            .Font = New Font("Segoe UI", 11),
            .Location = New Point(20, 510),
            .AutoSize = True
        }

        txtReason = New TextBox With {
            .Font = New Font("Segoe UI", 11),
            .Location = New Point(180, 507),
            .Width = 660,
            .Multiline = True,
            .Height = 60
        }

        pnlMain.Controls.AddRange({lblLineItems, flpLineItems, lblCustomerDetails, lblName, txtCustomerName, lblPhone, txtPhone, lblAddress, txtAddress, lblReason, txtReason})

        ' Bottom panel
        Dim pnlBottom As New Panel With {
            .Dock = DockStyle.Bottom,
            .Height = 150,
            .BackColor = _darkBlue,
            .Padding = New Padding(20)
        }

        lblTotalReturn = New Label With {
            .Text = "R 0.00",
            .Font = New Font("Segoe UI", 36, FontStyle.Bold),
            .ForeColor = _yellow,
            .Location = New Point(20, 20),
            .AutoSize = True
        }

        Dim lblReturnLabel As New Label With {
            .Text = "AMOUNT TO RETURN:",
            .Font = New Font("Segoe UI", 12),
            .ForeColor = Color.White,
            .Location = New Point(20, 80),
            .AutoSize = True
        }

        btnProcess = New Button With {
            .Text = "PROCESS RETURN",
            .Font = New Font("Segoe UI", 16, FontStyle.Bold),
            .Size = New Size(300, 80),
            .Location = New Point(650, 35),
            .BackColor = _red,
            .ForeColor = Color.White,
            .FlatStyle = FlatStyle.Flat,
            .Cursor = Cursors.Hand,
            .Enabled = False
        }
        btnProcess.FlatAppearance.BorderSize = 0
        AddHandler btnProcess.Click, AddressOf ProcessReturn

        pnlBottom.Controls.AddRange({lblTotalReturn, lblReturnLabel, btnProcess})

        Me.Controls.AddRange({pnlMain, pnlBottom, pnlHeader})
    End Sub

    Private Sub LoadInvoiceLines()
        Try
            Using conn As New SqlConnection(_connectionString)
                conn.Open()

                Dim sql = "
                    SELECT 
                        il.ProductID,
                        il.ItemCode,
                        il.ProductName,
                        il.Quantity,
                        il.UnitPrice,
                        il.LineTotal
                    FROM POS_InvoiceLines il
                    WHERE il.InvoiceNumber = @InvoiceNumber
                    ORDER BY il.ProductID"

                Using cmd As New SqlCommand(sql, conn)
                    cmd.Parameters.AddWithValue("@InvoiceNumber", _invoiceNumber)
                    Using adapter As New SqlDataAdapter(cmd)
                        adapter.Fill(_invoiceLines)
                    End Using
                End Using
            End Using

            If _invoiceLines.Rows.Count = 0 Then
                MessageBox.Show("No line items found for this invoice!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
                Me.Close()
                Return
            End If

            DisplayLineItems()

        Catch ex As Exception
            MessageBox.Show($"Error loading invoice: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
            Me.Close()
        End Try
    End Sub

    Private Sub DisplayLineItems()
        flpLineItems.Controls.Clear()

        For Each row As DataRow In _invoiceLines.Rows
            Dim pnlLine As New Panel With {
                .Size = New Size(900, 60),
                .BorderStyle = BorderStyle.FixedSingle,
                .Margin = New Padding(5),
                .BackColor = Color.White
            }

            Dim productID = CInt(row("ProductID"))
            Dim itemCode = row("ItemCode").ToString()
            Dim productName = row("ProductName").ToString()
            Dim qty = CDec(row("Quantity"))
            Dim price = CDec(row("UnitPrice"))
            Dim total = CDec(row("LineTotal"))

            Dim lblInfo As New Label With {
                .Text = $"{itemCode} - {productName}  |  Qty: {qty}  |  Price: R {price:N2}  |  Total: R {total:N2}",
                .Font = New Font("Segoe UI", 11),
                .Location = New Point(10, 18),
                .AutoSize = True
            }

            Dim btnReturn As New Button With {
                .Text = "🔄 RETURN",
                .Font = New Font("Segoe UI", 10, FontStyle.Bold),
                .Size = New Size(120, 40),
                .Location = New Point(650, 10),
                .BackColor = _green,
                .ForeColor = Color.White,
                .FlatStyle = FlatStyle.Flat,
                .Cursor = Cursors.Hand,
                .Tag = New With {productID, itemCode, productName, qty, price}
            }
            btnReturn.FlatAppearance.BorderSize = 0
            AddHandler btnReturn.Click, Sub() ReturnFullLine(productID, itemCode, productName, qty, price)

            Dim btnMinus As New Button With {
                .Text = "➖ MINUS",
                .Font = New Font("Segoe UI", 10, FontStyle.Bold),
                .Size = New Size(120, 40),
                .Location = New Point(780, 10),
                .BackColor = _orange,
                .ForeColor = Color.White,
                .FlatStyle = FlatStyle.Flat,
                .Cursor = Cursors.Hand,
                .Tag = New With {productID, itemCode, productName, qty, price}
            }
            btnMinus.FlatAppearance.BorderSize = 0
            AddHandler btnMinus.Click, Sub() ReduceQuantity(productID, itemCode, productName, qty, price)

            pnlLine.Controls.AddRange({lblInfo, btnReturn, btnMinus})
            flpLineItems.Controls.Add(pnlLine)
        Next
    End Sub

    Private Sub ReturnFullLine(productID As Integer, itemCode As String, productName As String, qty As Decimal, price As Decimal)
        ' Check if already in return list
        Dim existing = _returnItems.FirstOrDefault(Function(x) x.ProductID = productID)
        If existing IsNot Nothing Then
            MessageBox.Show("This item is already in the return list!", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information)
            Return
        End If

        _returnItems.Add(New ReturnLineItem With {
            .ProductID = productID,
            .ItemCode = itemCode,
            .ProductName = productName,
            .OriginalQty = qty,
            .ReturnQty = qty,
            .UnitPrice = price,
            .LineTotal = qty * price
        })

        UpdateTotal()
    End Sub

    Private Sub ReduceQuantity(productID As Integer, itemCode As String, productName As String, maxQty As Decimal, price As Decimal)
        Dim input = InputBox($"Enter quantity to return (Max: {maxQty}):", "Reduce Quantity", "1")

        If String.IsNullOrEmpty(input) Then Return

        Dim returnQty As Decimal
        If Not Decimal.TryParse(input, returnQty) OrElse returnQty <= 0 OrElse returnQty > maxQty Then
            MessageBox.Show($"Please enter a valid quantity between 1 and {maxQty}", "Invalid Quantity", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            Return
        End If

        ' Check if already in return list
        Dim existing = _returnItems.FirstOrDefault(Function(x) x.ProductID = productID)
        If existing IsNot Nothing Then
            MessageBox.Show("This item is already in the return list!", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information)
            Return
        End If

        _returnItems.Add(New ReturnLineItem With {
            .ProductID = productID,
            .ItemCode = itemCode,
            .ProductName = productName,
            .OriginalQty = maxQty,
            .ReturnQty = returnQty,
            .UnitPrice = price,
            .LineTotal = returnQty * price
        })

        UpdateTotal()
    End Sub

    Private Sub UpdateTotal()
        Dim total = _returnItems.Sum(Function(x) x.LineTotal)
        lblTotalReturn.Text = $"R {total:N2}"
        btnProcess.Enabled = _returnItems.Count > 0
    End Sub

    Private Sub ProcessReturn()
        If _returnItems.Count = 0 Then
            MessageBox.Show("Please select items to return!", "No Items", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            Return
        End If

        ' Validate customer details
        If String.IsNullOrWhiteSpace(txtCustomerName.Text) Then
            MessageBox.Show("Please enter customer name!", "Required", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            txtCustomerName.Focus()
            Return
        End If

        If String.IsNullOrWhiteSpace(txtReason.Text) Then
            MessageBox.Show("Please enter reason for return!", "Required", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            txtReason.Focus()
            Return
        End If

        ' SUPERVISOR AUTHORIZATION BEFORE WRITING
        Dim supervisorUsername = InputBox("Enter Retail Supervisor Username:", "Authorization Required")
        If String.IsNullOrWhiteSpace(supervisorUsername) Then Return

        Dim supervisorPassword As String = ""
        Using pwdForm As New PasswordInputForm("Enter Retail Supervisor Password:", "Authorization Required")
            If pwdForm.ShowDialog() <> DialogResult.OK Then Return
            supervisorPassword = pwdForm.Password
        End Using

        If String.IsNullOrWhiteSpace(supervisorPassword) Then Return

        ' Validate supervisor
        Dim authorizedSupervisorID As Integer = 0
        Try
            Using conn As New SqlConnection(_connectionString)
                conn.Open()
                Dim sql = "SELECT u.UserID FROM Users u INNER JOIN Roles r ON u.RoleID = r.RoleID WHERE u.Username = @Username AND u.Password = @Password AND r.RoleName = 'Retail Supervisor' AND u.IsActive = 1"
                Using cmd As New SqlCommand(sql, conn)
                    cmd.Parameters.AddWithValue("@Username", supervisorUsername)
                    cmd.Parameters.AddWithValue("@Password", supervisorPassword)
                    Dim result = cmd.ExecuteScalar()
                    If result Is Nothing Then
                        MessageBox.Show("Invalid Retail Supervisor credentials!", "Authorization Failed", MessageBoxButtons.OK, MessageBoxIcon.Error)
                        Return
                    End If
                    authorizedSupervisorID = CInt(result)
                End Using
            End Using
        Catch ex As Exception
            MessageBox.Show($"Authorization error: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
            Return
        End Try

        ' Process the return
        Try
            Using conn As New SqlConnection(_connectionString)
                conn.Open()
                Using transaction = conn.BeginTransaction()
                    Try
                        ' Generate return number
                        Dim returnNumber = GenerateReturnNumber(conn, transaction)

                        ' Calculate totals
                        Dim totalReturn = _returnItems.Sum(Function(x) x.LineTotal)
                        Dim totalTax = totalReturn * 0.15D / 1.15D ' Extract VAT

                        ' Insert into Returns table
                        Dim returnID = InsertReturn(conn, transaction, returnNumber, authorizedSupervisorID, totalReturn, totalTax)

                        ' Insert return line items
                        InsertReturnLineItems(conn, transaction, returnID)

                        ' Post to journals and ledgers
                        PostReturnToJournalsAndLedgers(conn, transaction, returnID, returnNumber, totalReturn, totalTax)

                        transaction.Commit()

                        ' Convert return items to receipt format
                        Dim receiptItems As New List(Of ReturnItem)
                        For Each item In _returnItems
                            receiptItems.Add(New ReturnItem With {
                                .ProductID = item.ProductID,
                                .ItemCode = item.ItemCode,
                                .ProductName = item.ProductName,
                                .QtyReturned = item.ReturnQty,
                                .UnitPrice = item.UnitPrice,
                                .LineTotal = item.LineTotal
                            })
                        Next

                        ' Show return receipt
                        Using receiptForm As New ReturnReceiptForm(returnNumber, DateTime.Now, txtCustomerName.Text, _invoiceNumber, receiptItems, totalReturn, totalTax, txtReason.Text)
                            receiptForm.ShowDialog()
                        End Using

                        ' TODO: Open cash drawer

                        Me.DialogResult = DialogResult.OK
                        Me.Close()

                    Catch ex As Exception
                        transaction.Rollback()
                        Throw
                    End Try
                End Using
            End Using
        Catch ex As Exception
            MessageBox.Show($"Error processing return: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    Private Function GenerateReturnNumber(conn As SqlConnection, transaction As SqlTransaction) As String
        ' Format: RET-[BranchCode]-TILL[TillNumber]-[SequenceNumber]
        Dim branchCode As String = "POS"
        Dim tillNumber As String = "1"

        ' Get branch code
        Dim sqlBranch = "SELECT BranchCode FROM Branches WHERE BranchID = @BranchID"
        Using cmd As New SqlCommand(sqlBranch, conn, transaction)
            cmd.Parameters.AddWithValue("@BranchID", _branchID)
            Dim result = cmd.ExecuteScalar()
            If result IsNot Nothing Then branchCode = result.ToString()
        End Using

        ' Get till number
        Dim sqlTill = "SELECT TillNumber FROM TillPoints WHERE TillPointID = @TillPointID"
        Using cmd As New SqlCommand(sqlTill, conn, transaction)
            cmd.Parameters.AddWithValue("@TillPointID", _tillPointID)
            Dim result = cmd.ExecuteScalar()
            If result IsNot Nothing Then tillNumber = result.ToString()
        End Using

        ' Get next sequence
        Dim sequence As Integer = 1
        Dim sqlSeq = "SELECT ISNULL(MAX(CAST(RIGHT(ReturnNumber, CHARINDEX('-', REVERSE(ReturnNumber)) - 1) AS INT)), 0) + 1 FROM Returns WHERE ReturnNumber LIKE @Pattern"
        Using cmd As New SqlCommand(sqlSeq, conn, transaction)
            cmd.Parameters.AddWithValue("@Pattern", $"RET-{branchCode}-TILL{tillNumber}-%")
            Dim result = cmd.ExecuteScalar()
            If result IsNot Nothing AndAlso Not IsDBNull(result) Then sequence = CInt(result)
        End Using

        Return $"RET-{branchCode}-TILL{tillNumber}-{sequence:D3}"
    End Function

    Private Function InsertReturn(conn As SqlConnection, transaction As SqlTransaction, returnNumber As String, supervisorID As Integer, totalReturn As Decimal, totalTax As Decimal) As Integer
        Dim sql = "
            INSERT INTO Demo_Returns (ReturnNumber, OriginalInvoiceNumber, BranchID, TillPointID, CashierID, SupervisorID, CustomerName, CustomerPhone, CustomerAddress, ReturnDate, TotalAmount, Reason)
            VALUES (@ReturnNumber, @OriginalInvoice, @BranchID, @TillPointID, @CashierID, @SupervisorID, @CustomerName, @Phone, @Address, GETDATE(), @TotalAmount, @Reason);
            SELECT CAST(SCOPE_IDENTITY() AS INT)"

        Using cmd As New SqlCommand(sql, conn, transaction)
            cmd.Parameters.AddWithValue("@ReturnNumber", returnNumber)
            cmd.Parameters.AddWithValue("@OriginalInvoice", _invoiceNumber)
            cmd.Parameters.AddWithValue("@BranchID", _branchID)
            cmd.Parameters.AddWithValue("@TillPointID", _tillPointID)
            cmd.Parameters.AddWithValue("@CashierID", _cashierID)
            cmd.Parameters.AddWithValue("@SupervisorID", supervisorID)
            cmd.Parameters.AddWithValue("@CustomerName", txtCustomerName.Text)
            cmd.Parameters.AddWithValue("@Phone", If(String.IsNullOrWhiteSpace(txtPhone.Text), DBNull.Value, CObj(txtPhone.Text)))
            cmd.Parameters.AddWithValue("@Address", If(String.IsNullOrWhiteSpace(txtAddress.Text), DBNull.Value, CObj(txtAddress.Text)))
            cmd.Parameters.AddWithValue("@TotalAmount", totalReturn)
            cmd.Parameters.AddWithValue("@Reason", txtReason.Text)

            Return CInt(cmd.ExecuteScalar())
        End Using
    End Function

    Private Sub InsertReturnLineItems(conn As SqlConnection, transaction As SqlTransaction, returnID As Integer)
        For Each item In _returnItems
            Dim sql = "
                INSERT INTO Demo_ReturnDetails (ReturnID, VariantID, ProductName, Quantity, UnitPrice, LineTotal)
                VALUES (@ReturnID, @VariantID, @ProductName, @Quantity, @UnitPrice, @LineTotal)"

            Using cmd As New SqlCommand(sql, conn, transaction)
                cmd.Parameters.AddWithValue("@ReturnID", returnID)
                cmd.Parameters.AddWithValue("@VariantID", item.ProductID)
                cmd.Parameters.AddWithValue("@ProductName", item.ProductName)
                cmd.Parameters.AddWithValue("@Quantity", item.ReturnQty)
                cmd.Parameters.AddWithValue("@UnitPrice", item.UnitPrice)
                cmd.Parameters.AddWithValue("@LineTotal", item.LineTotal)
                cmd.ExecuteNonQuery()
            End Using
        Next
    End Sub

    Private Sub PostReturnToJournalsAndLedgers(conn As SqlConnection, transaction As SqlTransaction, returnID As Integer, returnNumber As String, totalReturn As Decimal, totalTax As Decimal)
        ' Skip for now - journals table structure needs fixing
        ' TODO: Implement journal entries:
        ' 1. DEBIT: Sales Returns (contra-revenue)
        ' 2. CREDIT: Cash/Bank (refund)
        ' 3. DEBIT: Inventory (stock back)
        ' 4. CREDIT: Cost of Sales (reverse COGS)
    End Sub
End Class
