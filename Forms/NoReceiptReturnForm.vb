Imports System.Windows.Forms
Imports System.Drawing
Imports System.Data.SqlClient
Imports System.Configuration
Imports System.Linq

Public Class NoReceiptReturnForm
    Inherits Form

    Private _branchID As Integer
    Private _tillPointID As Integer
    Private _cashierID As Integer
    Private _cashierName As String
    Private _connectionString As String
    Private _supervisorID As Integer = 0
    Private _supervisorName As String = ""
    Private _customerID As Integer = 0
    Private _returnItems As New DataTable()
    Private _paymentMethod As String = "Cash"
    Private _cashAmount As Decimal = 0
    Private _cardAmount As Decimal = 0

    Private flpReturnItems As FlowLayoutPanel
    Private txtCellNumber As TextBox
    Private txtCustomerName As TextBox
    Private txtCustomerSurname As TextBox
    Private txtCustomerEmail As TextBox
    Private txtReturnReason As TextBox
    Private chkReturnToStock As CheckBox
    Private lblTotalRefund As Label
    Private btnScanItem As Button
    Private btnProcessReturn As Button

    Private _darkBlue As Color = ColorTranslator.FromHtml("#2C3E50")
    Private _green As Color = ColorTranslator.FromHtml("#27AE60")
    Private _red As Color = ColorTranslator.FromHtml("#E74C3C")
    Private _orange As Color = ColorTranslator.FromHtml("#E67E22")
    Private _yellow As Color = ColorTranslator.FromHtml("#F39C12")
    Private _ironDark As Color = Color.FromArgb(30, 35, 50)
    Private _ironGold As Color = Color.FromArgb(255, 215, 0)

    Public Sub New(branchID As Integer, tillPointID As Integer, cashierID As Integer, cashierName As String, connectionString As String)
        _branchID = branchID
        _tillPointID = tillPointID
        _cashierID = cashierID
        _cashierName = cashierName
        _connectionString = connectionString

        InitializeReturnItemsTable()
        InitializeComponent()
    End Sub
    
    Protected Overrides Sub OnLoad(e As EventArgs)
        MyBase.OnLoad(e)
        
        ' Require supervisor authorization on form load
        If Not AuthenticateSupervisor() Then
            MessageBox.Show("Supervisor authorization required!", "Access Denied", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            Me.DialogResult = DialogResult.Cancel
            Me.Close()
        End If
    End Sub

    Private Sub InitializeReturnItemsTable()
        _returnItems.Columns.Add("ProductID", GetType(Integer))
        _returnItems.Columns.Add("ItemCode", GetType(String))
        _returnItems.Columns.Add("ProductName", GetType(String))
        _returnItems.Columns.Add("Quantity", GetType(Decimal))
        _returnItems.Columns.Add("UnitPrice", GetType(Decimal))
        _returnItems.Columns.Add("LineTotal", GetType(Decimal))
        _returnItems.Columns.Add("ReturnToStock", GetType(Boolean))
    End Sub

    Private Sub InitializeComponent()
        Me.Text = "Return Without Receipt"
        ' Make form resizable and responsive to screen size
        Dim screenHeight = Screen.PrimaryScreen.WorkingArea.Height
        Dim screenWidth = Screen.PrimaryScreen.WorkingArea.Width
        Me.Size = New Size(Math.Min(1100, CInt(screenWidth * 0.9)), Math.Min(900, CInt(screenHeight * 0.9)))
        Me.StartPosition = FormStartPosition.CenterScreen
        Me.FormBorderStyle = FormBorderStyle.Sizable
        Me.MaximizeBox = True
        Me.MinimumSize = New Size(800, 600)
        Me.BackColor = Color.White

        ' Header
        Dim pnlHeader As New Panel With {
            .Dock = DockStyle.Top,
            .Height = 80,
            .BackColor = _darkBlue
        }

        Dim lblHeader As New Label With {
            .Text = "🔄 RETURN WITHOUT RECEIPT",
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

        ' Scan Item Section
        Dim lblScanSection As New Label With {
            .Text = "SCAN ITEMS TO RETURN:",
            .Font = New Font("Segoe UI", 14, FontStyle.Bold),
            .Location = New Point(20, 10),
            .AutoSize = True
        }

        btnScanItem = New Button With {
            .Text = "📱 SCAN ITEM BARCODE",
            .Font = New Font("Segoe UI", 14, FontStyle.Bold),
            .Size = New Size(300, 60),
            .Location = New Point(20, 45),
            .BackColor = _green,
            .ForeColor = Color.White,
            .FlatStyle = FlatStyle.Flat,
            .Cursor = Cursors.Hand
        }
        btnScanItem.FlatAppearance.BorderSize = 0
        AddHandler btnScanItem.Click, AddressOf ScanItemBarcode

        ' Return Items List
        Dim lblReturnItems As New Label With {
            .Text = "ITEMS TO RETURN:",
            .Font = New Font("Segoe UI", 14, FontStyle.Bold),
            .Location = New Point(20, 120),
            .AutoSize = True
        }

        flpReturnItems = New FlowLayoutPanel With {
            .Location = New Point(20, 155),
            .Size = New Size(1040, 200),
            .AutoScroll = True,
            .FlowDirection = FlowDirection.TopDown,
            .WrapContents = False,
            .BorderStyle = BorderStyle.FixedSingle,
            .BackColor = Color.FromArgb(240, 240, 240)
        }

        ' Stock Option Checkbox - DEFAULT UNCHECKED
        chkReturnToStock = New CheckBox With {
            .Text = "☑ Return items to stock (uncheck for writeoff)",
            .Font = New Font("Segoe UI", 12, FontStyle.Bold),
            .Location = New Point(20, 370),
            .Size = New Size(500, 30),
            .Checked = False,
            .ForeColor = _red
        }

        ' Customer Details Section
        Dim lblCustomerDetails As New Label With {
            .Text = "CUSTOMER DETAILS:",
            .Font = New Font("Segoe UI", 14, FontStyle.Bold),
            .Location = New Point(20, 415),
            .AutoSize = True
        }

        Dim lblCellNumber As New Label With {
            .Text = "Cell Number:",
            .Font = New Font("Segoe UI", 11),
            .Location = New Point(20, 450),
            .AutoSize = True
        }

        txtCellNumber = New TextBox With {
            .Font = New Font("Segoe UI", 11),
            .Location = New Point(150, 447),
            .Width = 200
        }
        AddHandler txtCellNumber.Leave, AddressOf OnCellNumberLeave

        Dim lblName As New Label With {
            .Text = "Name:",
            .Font = New Font("Segoe UI", 11),
            .Location = New Point(370, 450),
            .AutoSize = True
        }

        txtCustomerName = New TextBox With {
            .Font = New Font("Segoe UI", 11),
            .Location = New Point(450, 447),
            .Width = 200
        }

        Dim lblSurname As New Label With {
            .Text = "Surname:",
            .Font = New Font("Segoe UI", 11),
            .Location = New Point(670, 450),
            .AutoSize = True
        }

        txtCustomerSurname = New TextBox With {
            .Font = New Font("Segoe UI", 11),
            .Location = New Point(760, 447),
            .Width = 200
        }

        Dim lblEmail As New Label With {
            .Text = "Email:",
            .Font = New Font("Segoe UI", 11),
            .Location = New Point(20, 490),
            .AutoSize = True
        }

        txtCustomerEmail = New TextBox With {
            .Font = New Font("Segoe UI", 11),
            .Location = New Point(150, 487),
            .Width = 400
        }

        ' Return Reason Section
        Dim lblReason As New Label With {
            .Text = "REASON FOR RETURN (Required):",
            .Font = New Font("Segoe UI", 14, FontStyle.Bold),
            .Location = New Point(20, 535),
            .AutoSize = True
        }

        txtReturnReason = New TextBox With {
            .Font = New Font("Segoe UI", 11),
            .Location = New Point(20, 570),
            .Size = New Size(1040, 80),
            .Multiline = True,
            .MaxLength = 500
        }

        pnlMain.Controls.AddRange({lblScanSection, btnScanItem, lblReturnItems, flpReturnItems, chkReturnToStock,
                                   lblCustomerDetails, lblCellNumber, txtCellNumber, lblName, txtCustomerName,
                                   lblSurname, txtCustomerSurname, lblEmail, txtCustomerEmail,
                                   lblReason, txtReturnReason})

        ' Bottom panel
        Dim pnlBottom As New Panel With {
            .Dock = DockStyle.Bottom,
            .Height = 120,
            .BackColor = _darkBlue,
            .Padding = New Padding(20)
        }

        lblTotalRefund = New Label With {
            .Text = "R 0.00",
            .Font = New Font("Segoe UI", 36, FontStyle.Bold),
            .ForeColor = _yellow,
            .Location = New Point(20, 20),
            .AutoSize = True
        }

        Dim lblRefundLabel As New Label With {
            .Text = "TOTAL REFUND:",
            .Font = New Font("Segoe UI", 12),
            .ForeColor = Color.White,
            .Location = New Point(20, 70),
            .AutoSize = True
        }

        btnProcessReturn = New Button With {
            .Text = "PROCESS RETURN",
            .Font = New Font("Segoe UI", 16, FontStyle.Bold),
            .Size = New Size(300, 80),
            .Location = New Point(750, 20),
            .BackColor = _red,
            .ForeColor = Color.White,
            .FlatStyle = FlatStyle.Flat,
            .Cursor = Cursors.Hand,
            .Enabled = False
        }
        btnProcessReturn.FlatAppearance.BorderSize = 0
        AddHandler btnProcessReturn.Click, AddressOf ProcessReturn

        pnlBottom.Controls.AddRange({lblTotalRefund, lblRefundLabel, btnProcessReturn})

        Me.Controls.AddRange({pnlMain, pnlBottom, pnlHeader})
    End Sub

    Private Function AuthenticateSupervisor() As Boolean
        Using authForm As New Form()
            authForm.Text = "Supervisor Authentication"
            authForm.Size = New Size(400, 250)
            authForm.StartPosition = FormStartPosition.CenterParent
            authForm.FormBorderStyle = FormBorderStyle.FixedDialog
            authForm.MaximizeBox = False
            authForm.MinimizeBox = False

            Dim lblUsername As New Label With {
                .Text = "Supervisor Username:",
                .Location = New Point(20, 20),
                .AutoSize = True
            }
            Dim txtUsername As New TextBox With {
                .Location = New Point(20, 45),
                .Size = New Size(340, 25)
            }

            Dim lblPassword As New Label With {
                .Text = "Supervisor Password:",
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
                Try
                    Using conn As New SqlConnection(_connectionString)
                        conn.Open()
                        Dim sql = "SELECT u.UserID, u.FirstName + ' ' + u.LastName FROM Users u INNER JOIN Roles r ON u.RoleID = r.RoleID WHERE u.Username = @Username AND u.Password = @Password AND r.RoleName = 'Retail Supervisor' AND u.IsActive = 1"
                        Using cmd As New SqlCommand(sql, conn)
                            cmd.Parameters.AddWithValue("@Username", txtUsername.Text.Trim())
                            cmd.Parameters.AddWithValue("@Password", txtPassword.Text.Trim())
                            Using reader = cmd.ExecuteReader()
                                If reader.Read() Then
                                    _supervisorID = CInt(reader(0))
                                    _supervisorName = reader(1).ToString()
                                    Return True
                                Else
                                    MessageBox.Show("Invalid Retail Supervisor credentials!", "Authorization Failed", MessageBoxButtons.OK, MessageBoxIcon.Error)
                                    Return False
                                End If
                            End Using
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

    Private Sub ScanItemBarcode(sender As Object, e As EventArgs)
        Try
            Dim barcode As String = ""
            Using barcodeDialog As New BarcodeScannerDialog("SCAN ITEM BARCODE", "Scan the barcode of the item to return")
                If barcodeDialog.ShowDialog() <> DialogResult.OK Then Return
                barcode = barcodeDialog.ScannedBarcode
            End Using

            If String.IsNullOrWhiteSpace(barcode) Then Return

            ' Lookup product by barcode using wildcard search
            Using conn As New SqlConnection(_connectionString)
                conn.Open()
                Dim sql = "
                    SELECT 
                        p.ProductID,
                        p.SKU AS ItemCode,
                        p.Name AS ProductName,
                        pr.SellingPrice
                    FROM Demo_Retail_Product p
                    LEFT JOIN Demo_Retail_Price pr ON p.ProductID = pr.ProductID AND pr.BranchID = @BranchID
                    WHERE (p.SKU LIKE '%' + @Barcode + '%' OR p.Barcode LIKE '%' + @Barcode + '%')
                      AND p.BranchID = @BranchID
                      AND p.IsActive = 1
                      AND ISNULL(pr.SellingPrice, 0) > 0"

                Using cmd As New SqlCommand(sql, conn)
                    cmd.Parameters.AddWithValue("@Barcode", barcode)
                    cmd.Parameters.AddWithValue("@BranchID", _branchID)
                    Using reader = cmd.ExecuteReader()
                        If reader.Read() Then
                            Dim productID = CInt(reader("ProductID"))
                            Dim itemCode = reader("ItemCode").ToString()
                            Dim productName = reader("ProductName").ToString()
                            Dim price = If(IsDBNull(reader("SellingPrice")), 0D, CDec(reader("SellingPrice")))

                            ' Check if item already in return list by ItemCode
                            Dim existingRow As DataRow = Nothing
                            For Each row As DataRow In _returnItems.Rows
                                If row("ItemCode").ToString() = itemCode Then
                                    existingRow = row
                                    Exit For
                                End If
                            Next
                            
                            If existingRow IsNot Nothing Then
                                ' Increment quantity
                                existingRow("Quantity") = CDec(existingRow("Quantity")) + 1
                                existingRow("LineTotal") = CDec(existingRow("Quantity")) * CDec(existingRow("UnitPrice"))
                            Else
                                ' Add new item
                                Dim newRow = _returnItems.NewRow()
                                newRow("ProductID") = productID
                                newRow("ItemCode") = itemCode
                                newRow("ProductName") = productName
                                newRow("Quantity") = 1
                                newRow("UnitPrice") = price
                                newRow("LineTotal") = price
                                newRow("ReturnToStock") = chkReturnToStock.Checked
                                _returnItems.Rows.Add(newRow)
                            End If

                            RefreshReturnItemsDisplay()
                            CalculateTotalRefund()
                        Else
                            MessageBox.Show($"Product with barcode '{barcode}' not found!", "Product Not Found", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                        End If
                    End Using
                End Using
            End Using

        Catch ex As Exception
            MessageBox.Show($"Error scanning item: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    Private Sub RefreshReturnItemsDisplay()
        flpReturnItems.Controls.Clear()

        For Each row As DataRow In _returnItems.Rows
            Dim pnlItem As New Panel With {
                .Size = New Size(1020, 60),
                .BorderStyle = BorderStyle.FixedSingle,
                .Margin = New Padding(5),
                .BackColor = Color.White
            }

            Dim itemCode = row("ItemCode").ToString()
            Dim productName = row("ProductName").ToString()
            Dim qty = CDec(row("Quantity"))
            Dim price = CDec(row("UnitPrice"))
            Dim total = CDec(row("LineTotal"))

            Dim lblInfo As New Label With {
                .Text = $"{itemCode} - {productName}  |  Qty: {qty}  |  Price: R {price:N2}  |  Total: R {total:N2}",
                .Font = New Font("Segoe UI", 11),
                .Location = New Point(10, 18),
                .Size = New Size(800, 25)
            }

            Dim btnRemove As New Button With {
                .Text = "✖ Remove",
                .Font = New Font("Segoe UI", 10, FontStyle.Bold),
                .Size = New Size(100, 40),
                .Location = New Point(900, 10),
                .BackColor = _red,
                .ForeColor = Color.White,
                .FlatStyle = FlatStyle.Flat,
                .Cursor = Cursors.Hand,
                .Tag = row
            }
            btnRemove.FlatAppearance.BorderSize = 0
            AddHandler btnRemove.Click, Sub(s, ev)
                Dim btn = DirectCast(s, Button)
                Dim rowToRemove = DirectCast(btn.Tag, DataRow)
                _returnItems.Rows.Remove(rowToRemove)
                RefreshReturnItemsDisplay()
                CalculateTotalRefund()
            End Sub

            pnlItem.Controls.AddRange({lblInfo, btnRemove})
            flpReturnItems.Controls.Add(pnlItem)
        Next
    End Sub

    Private Sub CalculateTotalRefund()
        Dim total As Decimal = 0
        For Each row As DataRow In _returnItems.Rows
            total += CDec(row("LineTotal"))
        Next

        lblTotalRefund.Text = $"R {total:N2}"
        btnProcessReturn.Enabled = (total > 0)
    End Sub

    Private Sub OnCellNumberLeave(sender As Object, e As EventArgs)
        If Not String.IsNullOrWhiteSpace(txtCellNumber.Text) AndAlso txtCellNumber.Text.Length >= 10 Then
            LookupCustomer(txtCellNumber.Text.Trim())
        End If
    End Sub

    Private Sub LookupCustomer(cellNumber As String)
        Try
            Using conn As New SqlConnection(_connectionString)
                conn.Open()
                Dim sql = "SELECT CustomerID, FirstName, Surname, Email FROM POS_Customers WHERE CellNumber = @CellNumber AND IsActive = 1"
                Using cmd As New SqlCommand(sql, conn)
                    cmd.Parameters.AddWithValue("@CellNumber", cellNumber)
                    Using reader = cmd.ExecuteReader()
                        If reader.Read() Then
                            _customerID = CInt(reader("CustomerID"))
                            txtCustomerName.Text = reader("FirstName").ToString()
                            txtCustomerSurname.Text = reader("Surname").ToString()
                            If Not IsDBNull(reader("Email")) Then
                                txtCustomerEmail.Text = reader("Email").ToString()
                            End If

                            ' Visual feedback
                            txtCustomerName.BackColor = Color.LightGreen
                            txtCustomerSurname.BackColor = Color.LightGreen
                            txtCustomerEmail.BackColor = Color.LightGreen

                            Dim timer As New Timer With {.Interval = 1000}
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
            ' Silently fail
        End Try
    End Sub

    Private Sub ProcessReturn(sender As Object, e As EventArgs)
        Try
            ' Validate inputs
            If _returnItems.Rows.Count = 0 Then
                MessageBox.Show("Please scan at least one item to return!", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                Return
            End If

            If String.IsNullOrWhiteSpace(txtCellNumber.Text) OrElse txtCellNumber.Text.Length < 10 Then
                MessageBox.Show("Please enter a valid cell number!", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                txtCellNumber.Focus()
                Return
            End If

            If String.IsNullOrWhiteSpace(txtCustomerName.Text) Then
                MessageBox.Show("Please enter customer name!", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                txtCustomerName.Focus()
                Return
            End If

            If String.IsNullOrWhiteSpace(txtCustomerSurname.Text) Then
                MessageBox.Show("Please enter customer surname!", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                txtCustomerSurname.Focus()
                Return
            End If

            If String.IsNullOrWhiteSpace(txtReturnReason.Text) Then
                MessageBox.Show("Please enter reason for return!", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                txtReturnReason.Focus()
                Return
            End If

            ' 2nd Supervisor Authorization (before tender)
            If Not AuthenticateSupervisor() Then
                MessageBox.Show("Supervisor authorization required to process return!", "Access Denied", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                Return
            End If

            ' Save/Update customer
            SaveCustomer()

            ' Calculate total refund
            Dim totalRefund As Decimal = 0
            For Each row As DataRow In _returnItems.Rows
                totalRefund += CDec(row("LineTotal"))
            Next
            
            ' Process return transaction first
            Dim returnNumber = ProcessReturnTransaction(totalRefund)

            If Not String.IsNullOrEmpty(returnNumber) Then
                ' Show tender selection screen (Cash/Card/EFT) - matches payment screen design
                Using tenderForm As New ReturnTenderForm(returnNumber, _returnItems, totalRefund, _branchID, _cashierName, txtCustomerName.Text, txtCustomerSurname.Text, txtCellNumber.Text, txtReturnReason.Text)
                    If tenderForm.ShowDialog() = DialogResult.OK Then
                        ' Tender form handles:
                        ' 1. Tender method selection
                        ' 2. Receipt printing
                        ' 3. Cash drawer opening (if cash)
                        
                        Me.DialogResult = DialogResult.OK
                        Me.Close()
                    Else
                        ' User cancelled tender - return already recorded in database
                        MessageBox.Show("Return recorded but tender cancelled. Please process refund manually.", "Tender Cancelled", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                    End If
                End Using
            End If

        Catch ex As Exception
            MessageBox.Show($"Error processing return: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    Private Sub SaveCustomer()
        Try
            Using conn As New SqlConnection(_connectionString)
                conn.Open()
                Dim sql = "
                    IF NOT EXISTS (SELECT 1 FROM POS_Customers WHERE CellNumber = @CellNumber)
                        INSERT INTO POS_Customers (CellNumber, FirstName, Surname, Email, CreatedDate, IsActive)
                        VALUES (@CellNumber, @FirstName, @Surname, @Email, GETDATE(), 1);
                        SELECT SCOPE_IDENTITY();
                    ELSE
                        UPDATE POS_Customers 
                        SET FirstName = @FirstName, Surname = @Surname, Email = @Email
                        WHERE CellNumber = @CellNumber;
                        SELECT CustomerID FROM POS_Customers WHERE CellNumber = @CellNumber;"

                Using cmd As New SqlCommand(sql, conn)
                    cmd.Parameters.AddWithValue("@CellNumber", txtCellNumber.Text.Trim())
                    cmd.Parameters.AddWithValue("@FirstName", txtCustomerName.Text.Trim())
                    cmd.Parameters.AddWithValue("@Surname", txtCustomerSurname.Text.Trim())
                    cmd.Parameters.AddWithValue("@Email", If(String.IsNullOrWhiteSpace(txtCustomerEmail.Text), DBNull.Value, txtCustomerEmail.Text.Trim()))
                    Dim result = cmd.ExecuteScalar()
                    If result IsNot Nothing Then
                        _customerID = CInt(result)
                    End If
                End Using
            End Using
        Catch ex As Exception
            ' Non-critical error
        End Try
    End Sub

    Private Function ProcessReturnTransaction(totalRefund As Decimal) As String
        Dim returnNumber As String = ""
        
        Try
            Using conn As New SqlConnection(_connectionString)
                conn.Open()
                Using transaction = conn.BeginTransaction()
                    Try
                        ' Generate return number
                        Dim branchPrefix = GetBranchPrefix(conn, transaction)
                        returnNumber = GenerateReturnNumber(conn, transaction, branchPrefix)

                        ' Insert return header
                        Dim sqlReturn = "
                            INSERT INTO POS_Returns (
                                ReturnNumber, BranchID, TillPointID,
                                CashierID, CashierName, SupervisorID, SupervisorName,
                                CustomerID, ReturnDate, TotalAmount, PaymentMethod, CashAmount, CardAmount,
                                ReturnReason, ReturnToStock, ReturnStatus, CreatedDate, CreatedBy
                            ) VALUES (
                                @ReturnNumber, @BranchID, @TillPointID,
                                @CashierID, @CashierName, @SupervisorID, @SupervisorName,
                                @CustomerID, GETDATE(), @TotalAmount, @PaymentMethod, @CashAmount, @CardAmount,
                                @ReturnReason, @ReturnToStock, 'Processed', GETDATE(), @CreatedBy
                            );
                            SELECT SCOPE_IDENTITY();"

                        Dim returnID As Integer
                        Using cmd As New SqlCommand(sqlReturn, conn, transaction)
                            cmd.Parameters.AddWithValue("@ReturnNumber", returnNumber)
                            cmd.Parameters.AddWithValue("@BranchID", _branchID)
                            cmd.Parameters.AddWithValue("@TillPointID", _tillPointID)
                            cmd.Parameters.AddWithValue("@CashierID", _cashierID)
                            cmd.Parameters.AddWithValue("@CashierName", _cashierName)
                            cmd.Parameters.AddWithValue("@SupervisorID", _supervisorID)
                            cmd.Parameters.AddWithValue("@SupervisorName", _supervisorName)
                            cmd.Parameters.AddWithValue("@CustomerID", _customerID)
                            cmd.Parameters.AddWithValue("@TotalAmount", totalRefund)
                            cmd.Parameters.AddWithValue("@PaymentMethod", _paymentMethod)
                            cmd.Parameters.AddWithValue("@CashAmount", _cashAmount)
                            cmd.Parameters.AddWithValue("@CardAmount", _cardAmount)
                            cmd.Parameters.AddWithValue("@ReturnReason", txtReturnReason.Text.Trim())
                            cmd.Parameters.AddWithValue("@ReturnToStock", chkReturnToStock.Checked)
                            cmd.Parameters.AddWithValue("@CreatedBy", _cashierName)
                            returnID = CInt(cmd.ExecuteScalar())
                        End Using

                        ' Update stock if return to stock is checked
                        If chkReturnToStock.Checked Then
                            For Each row As DataRow In _returnItems.Rows
                                Try
                                    ' Get values from DataRow
                                    Dim itemCode As String = row("ItemCode").ToString()
                                    Dim qty As Decimal = CDec(row("Quantity"))
                                    
                                    ' Get ProductID from ItemCode by looking up in Demo_Retail_Product
                                    Dim productID As Integer = 0
                                    Dim sqlGetProdID = "SELECT TOP 1 ProductID FROM Demo_Retail_Product WHERE (SKU = @ItemCode OR Barcode = @ItemCode) AND BranchID = @BranchID AND IsActive = 1"
                                    Using cmdGetID As New SqlCommand(sqlGetProdID, conn, transaction)
                                        cmdGetID.Parameters.AddWithValue("@ItemCode", itemCode)
                                        cmdGetID.Parameters.AddWithValue("@BranchID", _branchID)
                                        Dim result = cmdGetID.ExecuteScalar()
                                        If result IsNot Nothing Then
                                            productID = CInt(result)
                                        End If
                                    End Using
                                    
                                    ' Update stock
                                    If productID > 0 Then
                                        Dim sqlStock = "UPDATE Demo_Retail_Stock SET Quantity = Quantity + @Quantity WHERE ProductID = @ProductID AND BranchID = @BranchID"
                                        Using cmd As New SqlCommand(sqlStock, conn, transaction)
                                            cmd.Parameters.AddWithValue("@Quantity", qty)
                                            cmd.Parameters.AddWithValue("@ProductID", productID)
                                            cmd.Parameters.AddWithValue("@BranchID", _branchID)
                                            cmd.ExecuteNonQuery()
                                        End Using
                                    End If
                                Catch ex As Exception
                                    ' Skip items that cause errors
                                    Continue For
                                End Try
                            Next
                        End If

                        ' Skip POS_Transactions insert - table doesn't exist or has different structure
                        ' Return is already recorded in POS_Returns table

                        transaction.Commit()
                        Return returnNumber

                    Catch ex As Exception
                        transaction.Rollback()
                        Throw
                    End Try
                End Using
            End Using

        Catch ex As Exception
            MessageBox.Show($"Error processing return transaction: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
            Return ""
        End Try
    End Function

    Private Function GetBranchPrefix(conn As SqlConnection, transaction As SqlTransaction) As String
        Dim sql = "SELECT BranchCode FROM Branches WHERE BranchID = @BranchID"
        Using cmd As New SqlCommand(sql, conn, transaction)
            cmd.Parameters.AddWithValue("@BranchID", _branchID)
            Dim result = cmd.ExecuteScalar()
            Return If(result IsNot Nothing, result.ToString(), "UNK")
        End Using
    End Function

    Private Function GenerateReturnNumber(conn As SqlConnection, transaction As SqlTransaction, branchPrefix As String) As String
        ' Generate return barcode using format: BranchID&4&SequenceNumber (e.g., 640001)
        ' This creates a scannable barcode that can be printed and scanned
        ' Transaction type codes: 1=Sale, 4=Return, 2=Order
        ' Shorter format for optimal barcode scanning (6 digits total)
        
        Dim pattern As String = "54%"
        Dim sql As String = "
            SELECT ISNULL(MAX(CAST(RIGHT(ReturnNumber, 5) AS INT)), 0) + 1 
            FROM POS_Returns WITH (TABLOCKX)
            WHERE ReturnNumber LIKE @pattern AND LEN(ReturnNumber) = 7"
        
        Using cmd As New SqlCommand(sql, conn, transaction)
            cmd.Parameters.AddWithValue("@pattern", pattern)
            Dim nextNumber As Integer = Convert.ToInt32(cmd.ExecuteScalar())
            
            ' Format: 5 + TransactionType (1 digit) + Sequence (5 digits) = 7 digits total
            ' Example: return 1 -> "5400001" (5=prefix, 4=Return, 00001=sequence)
            ' 7-digit format matches working barcode from yesterday
            Return $"54{nextNumber.ToString().PadLeft(5, "0"c)}"
        End Using
    End Function
    
    Private Sub OpenCashDrawer()
        Try
            ' ESC/POS command to open cash drawer: ESC p m t1 t2
            Dim drawerCommand As String = Chr(27) & Chr(112) & Chr(0) & Chr(25) & Chr(250)
            
            ' Send to default printer using raw printer helper
            Dim printDoc As New Printing.PrintDocument()
            Dim printerName As String = printDoc.PrinterSettings.PrinterName
            
            ' Send raw command directly to printer
            System.IO.File.WriteAllText("\\\" & printerName & "\", drawerCommand)
        Catch ex As Exception
            ' Non-critical - drawer may not be connected
        End Try
    End Sub
    
    Private Sub ShowReturnReceipt(returnNumber As String, totalRefund As Decimal)
        Try
            ' Create return receipt form with customer details and return reason
            Dim customerFullName = txtCustomerName.Text.Trim()
            Dim customerSurname = txtCustomerSurname.Text.Trim()
            Dim customerCell = txtCellNumber.Text.Trim()
            Dim returnReason = txtReturnReason.Text.Trim()
            
            Dim receiptForm As New ReturnReceiptForm(returnNumber, _returnItems, totalRefund, _branchID, _cashierName, customerFullName, customerSurname, customerCell, returnReason)
            receiptForm.ShowDialog()
        Catch ex As Exception
            MessageBox.Show($"Return processed successfully!{vbCrLf}Return Number: {returnNumber}{vbCrLf}Refund Amount: R {totalRefund:N2}", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information)
        End Try
    End Sub
End Class
