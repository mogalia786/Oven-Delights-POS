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
    Private _hasItems As Boolean = False

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
        Public Property RestockItem As Boolean
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
        Me.Size = New Size(1100, 800)
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

        ' Column headers for line items
        Dim pnlHeaders As New Panel With {
            .Location = New Point(20, 45),
            .Size = New Size(1040, 30),
            .BackColor = _darkBlue
        }

        Dim lblHeaderItem As New Label With {
            .Text = "ITEM DETAILS",
            .Font = New Font("Segoe UI", 10, FontStyle.Bold),
            .ForeColor = Color.White,
            .Location = New Point(10, 7),
            .AutoSize = True
        }

        Dim lblHeaderRestock As New Label With {
            .Text = "RESTOCK?",
            .Font = New Font("Segoe UI", 10, FontStyle.Bold),
            .ForeColor = Color.White,
            .Location = New Point(680, 7),
            .AutoSize = True
        }

        Dim lblHeaderActions As New Label With {
            .Text = "ACTIONS",
            .Font = New Font("Segoe UI", 10, FontStyle.Bold),
            .ForeColor = Color.White,
            .Location = New Point(850, 7),
            .AutoSize = True
        }

        pnlHeaders.Controls.AddRange({lblHeaderItem, lblHeaderRestock, lblHeaderActions})

        flpLineItems = New FlowLayoutPanel With {
            .Location = New Point(20, 80),
            .Size = New Size(1040, 265),
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

        pnlMain.Controls.AddRange({lblLineItems, pnlHeaders, flpLineItems, lblCustomerDetails, lblName, txtCustomerName, lblPhone, txtPhone, lblAddress, txtAddress, lblReason, txtReason})

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

                ' Load from POS_InvoiceLines - this is the source of truth
                Dim sql = "
                    SELECT 
                        ProductID,
                        ItemCode,
                        ProductName,
                        Quantity,
                        UnitPrice,
                        LineTotal
                    FROM POS_InvoiceLines
                    WHERE InvoiceNumber = @InvoiceNumber
                    AND Quantity > 0
                    ORDER BY ProductName"

                Using cmd As New SqlCommand(sql, conn)
                    cmd.Parameters.AddWithValue("@InvoiceNumber", _invoiceNumber)
                    Using adapter As New SqlDataAdapter(cmd)
                        adapter.Fill(_invoiceLines)
                    End Using
                End Using
            End Using

            If _invoiceLines.Rows.Count = 0 Then
                _hasItems = False
                Return
            End If

            _hasItems = True
            DisplayLineItems()

        Catch ex As Exception
            MessageBox.Show($"Error loading invoice: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
            _hasItems = False
        End Try
    End Sub
    
    Protected Overrides Sub OnShown(e As EventArgs)
        MyBase.OnShown(e)
        
        ' Check if we have items after loading
        If Not _hasItems Then
            MessageBox.Show("No items available to return for this invoice! All items may have already been returned.", "No Items", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            Me.DialogResult = DialogResult.Cancel
            Me.Close()
        End If
    End Sub

    Private Sub DisplayLineItems()
        flpLineItems.Controls.Clear()

        For Each row As DataRow In _invoiceLines.Rows
            Dim pnlLine As New Panel With {
                .Size = New Size(1020, 60),
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
                .Size = New Size(650, 25)
            }

            Dim btnReturn As New Button With {
                .Text = "🔄 RETURN",
                .Font = New Font("Segoe UI", 10, FontStyle.Bold),
                .Size = New Size(120, 40),
                .Location = New Point(780, 10),
                .BackColor = _green,
                .ForeColor = Color.White,
                .FlatStyle = FlatStyle.Flat,
                .Cursor = Cursors.Hand,
                .Tag = New With {productID, itemCode, productName, qty, price}
            }
            btnReturn.FlatAppearance.BorderSize = 0
            AddHandler btnReturn.Click, Sub(sender, e)
                Dim btn = DirectCast(sender, Button)
                Dim panel = DirectCast(btn.Parent, Panel)
                Dim chk = panel.Controls.OfType(Of CheckBox)().FirstOrDefault()
                ReturnFullLine(productID, itemCode, productName, qty, price, If(chk IsNot Nothing, chk.Checked, True))
            End Sub

            Dim chkRestock As New CheckBox With {
                .Text = "",
                .Font = New Font("Segoe UI", 9, FontStyle.Bold),
                .Location = New Point(695, 20),
                .Size = New Size(20, 20),
                .Checked = True
            }

            Dim btnMinus As New Button With {
                .Text = "➖ MINUS",
                .Font = New Font("Segoe UI", 10, FontStyle.Bold),
                .Size = New Size(120, 40),
                .Location = New Point(910, 10),
                .BackColor = _orange,
                .ForeColor = Color.White,
                .FlatStyle = FlatStyle.Flat,
                .Cursor = Cursors.Hand,
                .Tag = New With {productID, itemCode, productName, qty, price, chkRestock}
            }
            btnMinus.FlatAppearance.BorderSize = 0
            AddHandler btnMinus.Click, Sub(sender, e)
                Dim btn = DirectCast(sender, Button)
                Dim panel = DirectCast(btn.Parent, Panel)
                Dim chk = panel.Controls.OfType(Of CheckBox)().FirstOrDefault()
                ReduceQuantity(productID, itemCode, productName, qty, price, If(chk IsNot Nothing, chk.Checked, True))
            End Sub

            pnlLine.Controls.AddRange({lblInfo, chkRestock, btnReturn, btnMinus})
            flpLineItems.Controls.Add(pnlLine)
        Next
    End Sub

    Private Sub ReturnFullLine(productID As Integer, itemCode As String, productName As String, qty As Decimal, price As Decimal, restockItem As Boolean)
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
            .LineTotal = qty * price,
            .RestockItem = restockItem
        })

        UpdateTotal()
    End Sub

    Private Sub ReduceQuantity(productID As Integer, itemCode As String, productName As String, maxQty As Decimal, price As Decimal, restockItem As Boolean)
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
            .LineTotal = returnQty * price,
            .RestockItem = restockItem
        })

        UpdateTotal()
    End Sub

    Private Sub UpdateTotal()
        Dim total = _returnItems.Sum(Function(x) x.LineTotal)
        lblTotalReturn.Text = $"R {total:N2}"
        btnProcess.Enabled = _returnItems.Count > 0
    End Sub

    Private Sub ProcessReturn()
        Debug.WriteLine("========================================")
        Debug.WriteLine("PROCESS RETURN BUTTON CLICKED")
        Debug.WriteLine($"Invoice Number: {_invoiceNumber}")
        Debug.WriteLine($"Items to return: {_returnItems.Count}")
        Debug.WriteLine("========================================")
        
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
                        Debug.WriteLine($"Calling InsertReturnLineItems with ReturnID={returnID}")
                        InsertReturnLineItems(conn, transaction, returnID)
                        Debug.WriteLine($"Completed InsertReturnLineItems")

                        ' Post to journals and ledgers
                        Debug.WriteLine($"Calling PostReturnToJournalsAndLedgers")
                        PostReturnToJournalsAndLedgers(conn, transaction, returnID, returnNumber, totalReturn, totalTax)
                        Debug.WriteLine($"Completed PostReturnToJournalsAndLedgers")

                        Debug.WriteLine($"COMMITTING TRANSACTION...")
                        transaction.Commit()
                        Debug.WriteLine($"TRANSACTION COMMITTED SUCCESSFULLY!")

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

        ' Get till number (remove TILL prefix if it exists)
        Dim sqlTill = "SELECT TillNumber FROM TillPoints WHERE TillPointID = @TillPointID"
        Using cmd As New SqlCommand(sqlTill, conn, transaction)
            cmd.Parameters.AddWithValue("@TillPointID", _tillPointID)
            Dim result = cmd.ExecuteScalar()
            If result IsNot Nothing Then 
                tillNumber = result.ToString().Replace("TILL", "").Replace("Till", "").Trim()
            End If
        End Using

        ' Get next sequence - simpler approach using COUNT
        Dim sequence As Integer = 1
        Dim pattern = $"RET-{branchCode}-TILL-{tillNumber}-%"
        Dim sqlSeq = "SELECT COUNT(*) + 1 FROM Demo_Returns WHERE ReturnNumber LIKE @Pattern"
        Using cmd As New SqlCommand(sqlSeq, conn, transaction)
            cmd.Parameters.AddWithValue("@Pattern", pattern)
            Dim result = cmd.ExecuteScalar()
            If result IsNot Nothing AndAlso Not IsDBNull(result) Then sequence = CInt(result)
        End Using

        ' Keep trying until we find a unique number
        Dim returnNumber As String
        Dim attempts = 0
        Do
            returnNumber = $"RET-{branchCode}-TILL-{tillNumber}-{sequence:D6}"
            
            ' Check if this number already exists
            Dim sqlCheck = "SELECT COUNT(*) FROM Demo_Returns WHERE ReturnNumber = @ReturnNumber"
            Using cmdCheck As New SqlCommand(sqlCheck, conn, transaction)
                cmdCheck.Parameters.AddWithValue("@ReturnNumber", returnNumber)
                Dim exists = CInt(cmdCheck.ExecuteScalar())
                If exists = 0 Then Exit Do
            End Using
            
            sequence += 1
            attempts += 1
            If attempts > 100 Then Throw New Exception("Unable to generate unique return number")
        Loop

        Return returnNumber
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
        Debug.WriteLine($"=== InsertReturnLineItems: Processing {_returnItems.Count} items ===")
        
        For Each item In _returnItems
            Debug.WriteLine($"Processing return item: {item.ProductName}, ProductID={item.ProductID}, Qty={item.ReturnQty}")
            
            ' Insert return line item
            Dim sql = "
                INSERT INTO Demo_ReturnDetails (ReturnID, VariantID, ProductName, Quantity, UnitPrice, LineTotal, RestockItem)
                VALUES (@ReturnID, @VariantID, @ProductName, @Quantity, @UnitPrice, @LineTotal, @RestockItem)"

            Using cmd As New SqlCommand(sql, conn, transaction)
                cmd.Parameters.AddWithValue("@ReturnID", returnID)
                cmd.Parameters.AddWithValue("@VariantID", item.ProductID)
                cmd.Parameters.AddWithValue("@ProductName", item.ProductName)
                cmd.Parameters.AddWithValue("@Quantity", item.ReturnQty)
                cmd.Parameters.AddWithValue("@UnitPrice", item.UnitPrice)
                cmd.Parameters.AddWithValue("@LineTotal", item.LineTotal)
                cmd.Parameters.AddWithValue("@RestockItem", item.RestockItem)
                cmd.ExecuteNonQuery()
            End Using
            
            Debug.WriteLine($"Inserted into Demo_ReturnDetails")

            ' If restock flag is checked, update inventory
            If item.RestockItem Then
                Debug.WriteLine($"Restocking item...")
                UpdateInventoryStock(conn, transaction, item.ProductID, item.ReturnQty)
            End If
            
            ' Update the original invoice line item
            Debug.WriteLine($"About to call UpdateInvoiceLineItem for ProductID={item.ProductID}, Invoice={_invoiceNumber}")
            UpdateInvoiceLineItem(conn, transaction, item.ProductID, item.ReturnQty)
            Debug.WriteLine($"Finished UpdateInvoiceLineItem")
        Next
        
        Debug.WriteLine($"=== InsertReturnLineItems: Completed ===")
    End Sub
    
    Private Sub UpdateInvoiceLineItem(conn As SqlConnection, transaction As SqlTransaction, productID As Integer, returnedQty As Decimal)
        Try
            Debug.WriteLine($"Updating POS_InvoiceLines: Invoice={_invoiceNumber}, ProductID={productID}, ReturnQty={returnedQty}")
        
            ' Check current quantity in POS_InvoiceLines
            Dim sqlCheck = "SELECT Quantity FROM POS_InvoiceLines WHERE InvoiceNumber = @InvoiceNumber AND ProductID = @ProductID"
            Dim currentQty As Decimal = 0
            Using cmd As New SqlCommand(sqlCheck, conn, transaction)
                cmd.Parameters.AddWithValue("@InvoiceNumber", _invoiceNumber)
                cmd.Parameters.AddWithValue("@ProductID", productID)
                Dim result = cmd.ExecuteScalar()
                If result IsNot Nothing Then 
                    currentQty = CDec(result)
                Else
                    Debug.WriteLine($"WARNING: No line item found in POS_InvoiceLines for Invoice={_invoiceNumber}, ProductID={productID}")
                    Return
                End If
            End Using
            
            Debug.WriteLine($"Current quantity: {currentQty}, Returning: {returnedQty}")
        
            If currentQty <= returnedQty Then
                ' Delete the line completely (returning all)
                Debug.WriteLine($"DELETING line item - full return")
                Dim sqlDelete = "DELETE FROM POS_InvoiceLines WHERE InvoiceNumber = @InvoiceNumber AND ProductID = @ProductID"
                Using cmd As New SqlCommand(sqlDelete, conn, transaction)
                    cmd.Parameters.AddWithValue("@InvoiceNumber", _invoiceNumber)
                    cmd.Parameters.AddWithValue("@ProductID", productID)
                    Dim rowsAffected = cmd.ExecuteNonQuery()
                    Debug.WriteLine($"Deleted {rowsAffected} rows from POS_InvoiceLines")
                End Using
            Else
                ' Update the line item quantity (partial return)
                Debug.WriteLine($"UPDATING line item - partial return")
                Dim sql = "
                    UPDATE POS_InvoiceLines 
                    SET Quantity = Quantity - @ReturnedQty,
                        LineTotal = (Quantity - @ReturnedQty) * UnitPrice
                    WHERE InvoiceNumber = @InvoiceNumber 
                    AND ProductID = @ProductID"
                
                Using cmd As New SqlCommand(sql, conn, transaction)
                    cmd.Parameters.AddWithValue("@InvoiceNumber", _invoiceNumber)
                    cmd.Parameters.AddWithValue("@ProductID", productID)
                    cmd.Parameters.AddWithValue("@ReturnedQty", returnedQty)
                    Dim rowsAffected = cmd.ExecuteNonQuery()
                    Debug.WriteLine($"Updated {rowsAffected} rows in POS_InvoiceLines")
                End Using
            End If
            
            Debug.WriteLine($"Successfully updated invoice {_invoiceNumber}")
            
        Catch ex As Exception
            Debug.WriteLine($"ERROR updating invoice line item: {ex.Message}")
            Throw
        End Try
    End Sub

    Private Sub UpdateInventoryStock(conn As SqlConnection, transaction As SqlTransaction, productID As Integer, quantity As Decimal)
        ' Update Demo_Retail_Product stock for this branch
        ' Note: If Demo_Retail_Product doesn't have BranchID, stock is global across branches
        Dim sql = "
            UPDATE Demo_Retail_Product 
            SET CurrentStock = CurrentStock + @Quantity 
            WHERE ProductID = @ProductID 
            AND (BranchID = @BranchID OR BranchID IS NULL)"

        Using cmd As New SqlCommand(sql, conn, transaction)
            cmd.Parameters.AddWithValue("@ProductID", productID)
            cmd.Parameters.AddWithValue("@Quantity", quantity)
            cmd.Parameters.AddWithValue("@BranchID", _branchID)
            Dim rowsAffected = cmd.ExecuteNonQuery()
            
            ' If no rows affected with BranchID filter, try without it (global stock)
            If rowsAffected = 0 Then
                Dim sqlGlobal = "
                    UPDATE Demo_Retail_Product 
                    SET CurrentStock = CurrentStock + @Quantity 
                    WHERE ProductID = @ProductID"
                
                Using cmdGlobal As New SqlCommand(sqlGlobal, conn, transaction)
                    cmdGlobal.Parameters.AddWithValue("@ProductID", productID)
                    cmdGlobal.Parameters.AddWithValue("@Quantity", quantity)
                    cmdGlobal.ExecuteNonQuery()
                End Using
            End If
        End Using
    End Sub

    Private Sub PostReturnToJournalsAndLedgers(conn As SqlConnection, transaction As SqlTransaction, returnID As Integer, returnNumber As String, totalReturn As Decimal, totalTax As Decimal)
        Try
            ' Get ledger IDs
            Dim salesReturnsLedgerID = GetLedgerID(conn, transaction, "Sales Returns")
            Dim cashLedgerID = GetLedgerID(conn, transaction, "Cash")
            Dim inventoryLedgerID = GetLedgerID(conn, transaction, "Inventory")
            Dim costOfSalesLedgerID = GetLedgerID(conn, transaction, "Cost of Sales")
            Dim stockWriteOffLedgerID = GetLedgerID(conn, transaction, "Stock Write-Off")

            ' Calculate totals for restocked vs discarded items
            Dim restockedAmount As Decimal = 0
            Dim discardedAmount As Decimal = 0

            For Each item In _returnItems
                If item.RestockItem Then
                    restockedAmount += item.LineTotal
                Else
                    discardedAmount += item.LineTotal
                End If
            Next

            ' 1. DEBIT: Sales Returns (contra-revenue) - full amount
            PostToJournal(conn, transaction, returnNumber, salesReturnsLedgerID, "Debit", totalReturn, $"Return: {returnNumber}")

            ' 2. CREDIT: Cash (refund to customer) - full amount
            PostToJournal(conn, transaction, returnNumber, cashLedgerID, "Credit", totalReturn, $"Refund: {returnNumber}")

            ' 3. For restocked items: DEBIT Inventory, CREDIT Cost of Sales
            If restockedAmount > 0 Then
                PostToJournal(conn, transaction, returnNumber, inventoryLedgerID, "Debit", restockedAmount, $"Restock: {returnNumber}")
                PostToJournal(conn, transaction, returnNumber, costOfSalesLedgerID, "Credit", restockedAmount, $"Reverse COGS: {returnNumber}")
            End If

            ' 4. For discarded items: DEBIT Stock Write-Off
            If discardedAmount > 0 Then
                PostToJournal(conn, transaction, returnNumber, stockWriteOffLedgerID, "Debit", discardedAmount, $"Discarded: {returnNumber}")
            End If

        Catch ex As Exception
            ' Log but don't fail the return if ledger posting fails
            Debug.WriteLine($"Ledger posting error: {ex.Message}")
        End Try
    End Sub

    Private Function GetLedgerID(conn As SqlConnection, transaction As SqlTransaction, ledgerName As String) As Integer
        ' Get ledger for this specific branch
        Dim sql = "SELECT LedgerID FROM Ledgers WHERE LedgerName = @LedgerName AND (BranchID = @BranchID OR BranchID IS NULL)"
        Using cmd As New SqlCommand(sql, conn, transaction)
            cmd.Parameters.AddWithValue("@LedgerName", ledgerName)
            cmd.Parameters.AddWithValue("@BranchID", _branchID)
            Dim result = cmd.ExecuteScalar()
            If result IsNot Nothing Then
                Return CInt(result)
            End If
            
            ' If no branch-specific ledger found, try without branch filter (global ledgers)
            Dim sqlGlobal = "SELECT LedgerID FROM Ledgers WHERE LedgerName = @LedgerName AND BranchID IS NULL"
            Using cmdGlobal As New SqlCommand(sqlGlobal, conn, transaction)
                cmdGlobal.Parameters.AddWithValue("@LedgerName", ledgerName)
                Dim resultGlobal = cmdGlobal.ExecuteScalar()
                If resultGlobal IsNot Nothing Then
                    Return CInt(resultGlobal)
                End If
            End Using
            
            Return 0
        End Using
    End Function

    Private Sub PostToJournal(conn As SqlConnection, transaction As SqlTransaction, reference As String, ledgerID As Integer, entryType As String, amount As Decimal, description As String)
        If ledgerID = 0 Then Return

        ' Journals table uses Debit/Credit columns, not EntryType/Amount
        Dim debitAmount As Decimal = If(entryType = "Debit", amount, 0)
        Dim creditAmount As Decimal = If(entryType = "Credit", amount, 0)

        Dim sql = "
            INSERT INTO Journals (JournalDate, JournalType, Reference, LedgerID, Debit, Credit, Description, BranchID)
            VALUES (GETDATE(), 'Returns', @Reference, @LedgerID, @Debit, @Credit, @Description, @BranchID)"

        Using cmd As New SqlCommand(sql, conn, transaction)
            cmd.Parameters.AddWithValue("@Reference", reference)
            cmd.Parameters.AddWithValue("@LedgerID", ledgerID)
            cmd.Parameters.AddWithValue("@Debit", debitAmount)
            cmd.Parameters.AddWithValue("@Credit", creditAmount)
            cmd.Parameters.AddWithValue("@Description", description)
            cmd.Parameters.AddWithValue("@BranchID", _branchID)
            cmd.ExecuteNonQuery()
        End Using
    End Sub

    ' NOTE: Ledger balances are calculated from Journals table, not stored in Ledgers table
    ' No need to update Ledgers table directly - just post to Journals
End Class
