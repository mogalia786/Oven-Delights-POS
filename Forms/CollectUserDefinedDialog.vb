Imports System.Configuration
Imports System.Data
Imports System.Data.SqlClient
Imports System.Drawing
Imports System.Text
Imports System.Windows.Forms

Public Class CollectUserDefinedDialog
    Inherits Form

    Private _connectionString As String
    Private _branchID As Integer
    Private _cashierName As String
    Private _currentOrder As DataRow = Nothing

    ' UI Controls
    Private txtBarcode As TextBox
    Private txtCellNumber As TextBox
    Private btnSearchBarcode As Button
    Private btnSearchCell As Button
    Private pnlOrderDetails As Panel
    Private lblOrderInfo As Label
    Private dgvItems As DataGridView
    Private btnProcessPickup As Button
    Private btnClose As Button

    Public Sub New(connectionString As String, branchID As Integer, cashierName As String)
        MyBase.New()
        _connectionString = connectionString
        _branchID = branchID
        _cashierName = cashierName

        InitializeComponent()
    End Sub

    Private Sub InitializeComponent()
        Me.Text = "Collect User Defined Order"
        Me.Size = New Size(800, 700)
        Me.StartPosition = FormStartPosition.CenterScreen
        Me.FormBorderStyle = FormBorderStyle.FixedDialog
        Me.MaximizeBox = False
        Me.MinimizeBox = False
        Me.BackColor = Color.White

        Dim yPos As Integer = 20

        ' Header
        Dim lblHeader As New Label With {
            .Text = "COLLECT USER DEFINED ORDER",
            .Font = New Font("Segoe UI", 16, FontStyle.Bold),
            .ForeColor = ColorTranslator.FromHtml("#E67E22"),
            .Location = New Point(20, yPos),
            .Size = New Size(750, 35),
            .TextAlign = ContentAlignment.MiddleCenter
        }
        Me.Controls.Add(lblHeader)
        yPos += 50

        ' Barcode search
        Dim lblBarcode As New Label With {
            .Text = "Scan Barcode:",
            .Font = New Font("Segoe UI", 10, FontStyle.Bold),
            .Location = New Point(20, yPos),
            .Size = New Size(150, 25)
        }
        txtBarcode = New TextBox With {
            .Font = New Font("Segoe UI", 10),
            .Location = New Point(180, yPos),
            .Size = New Size(400, 25)
        }
        AddHandler txtBarcode.KeyDown, AddressOf TxtBarcode_KeyDown

        btnSearchBarcode = New Button With {
            .Text = "Search",
            .Font = New Font("Segoe UI", 10, FontStyle.Bold),
            .Size = New Size(100, 30),
            .Location = New Point(590, yPos - 2),
            .BackColor = ColorTranslator.FromHtml("#3498DB"),
            .ForeColor = Color.White,
            .FlatStyle = FlatStyle.Flat,
            .Cursor = Cursors.Hand
        }
        btnSearchBarcode.FlatAppearance.BorderSize = 0
        AddHandler btnSearchBarcode.Click, AddressOf BtnSearchBarcode_Click

        Me.Controls.AddRange({lblBarcode, txtBarcode, btnSearchBarcode})
        yPos += 45

        ' Cell number search
        Dim lblCell As New Label With {
            .Text = "Enter Cell Number:",
            .Font = New Font("Segoe UI", 10, FontStyle.Bold),
            .Location = New Point(20, yPos),
            .Size = New Size(150, 25)
        }
        txtCellNumber = New TextBox With {
            .Font = New Font("Segoe UI", 10),
            .Location = New Point(180, yPos),
            .Size = New Size(400, 25)
        }
        AddHandler txtCellNumber.KeyDown, AddressOf TxtCellNumber_KeyDown

        btnSearchCell = New Button With {
            .Text = "Search",
            .Font = New Font("Segoe UI", 10, FontStyle.Bold),
            .Size = New Size(100, 30),
            .Location = New Point(590, yPos - 2),
            .BackColor = ColorTranslator.FromHtml("#3498DB"),
            .ForeColor = Color.White,
            .FlatStyle = FlatStyle.Flat,
            .Cursor = Cursors.Hand
        }
        btnSearchCell.FlatAppearance.BorderSize = 0
        AddHandler btnSearchCell.Click, AddressOf BtnSearchCell_Click

        Me.Controls.AddRange({lblCell, txtCellNumber, btnSearchCell})
        yPos += 50

        ' Order details panel
        pnlOrderDetails = New Panel With {
            .Location = New Point(20, yPos),
            .Size = New Size(750, 400),
            .BorderStyle = BorderStyle.FixedSingle,
            .BackColor = Color.White,
            .Visible = False
        }

        lblOrderInfo = New Label With {
            .Location = New Point(10, 10),
            .Size = New Size(720, 150),
            .Font = New Font("Segoe UI", 9),
            .BackColor = Color.White
        }

        dgvItems = New DataGridView With {
            .Location = New Point(10, 170),
            .Size = New Size(720, 220),
            .ReadOnly = True,
            .AllowUserToAddRows = False,
            .AllowUserToDeleteRows = False,
            .SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            .AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            .BackgroundColor = Color.White,
            .BorderStyle = BorderStyle.Fixed3D
        }

        pnlOrderDetails.Controls.AddRange({lblOrderInfo, dgvItems})
        Me.Controls.Add(pnlOrderDetails)
        yPos += 410

        ' Buttons
        btnProcessPickup = New Button With {
            .Text = "Process Pickup",
            .Font = New Font("Segoe UI", 12, FontStyle.Bold),
            .Size = New Size(200, 45),
            .Location = New Point(200, yPos),
            .BackColor = ColorTranslator.FromHtml("#27AE60"),
            .ForeColor = Color.White,
            .FlatStyle = FlatStyle.Flat,
            .Cursor = Cursors.Hand,
            .Visible = False
        }
        btnProcessPickup.FlatAppearance.BorderSize = 0
        AddHandler btnProcessPickup.Click, AddressOf BtnProcessPickup_Click

        btnClose = New Button With {
            .Text = "Close",
            .Font = New Font("Segoe UI", 12, FontStyle.Bold),
            .Size = New Size(200, 45),
            .Location = New Point(420, yPos),
            .BackColor = ColorTranslator.FromHtml("#E74C3C"),
            .ForeColor = Color.White,
            .FlatStyle = FlatStyle.Flat,
            .Cursor = Cursors.Hand
        }
        btnClose.FlatAppearance.BorderSize = 0
        AddHandler btnClose.Click, AddressOf BtnClose_Click

        Me.Controls.AddRange({btnProcessPickup, btnClose})

        ' Set focus to barcode textbox
        txtBarcode.Focus()
    End Sub

    Private Sub TxtBarcode_KeyDown(sender As Object, e As KeyEventArgs)
        If e.KeyCode = Keys.Enter Then
            e.SuppressKeyPress = True
            BtnSearchBarcode_Click(Nothing, Nothing)
        End If
    End Sub

    Private Sub TxtCellNumber_KeyDown(sender As Object, e As KeyEventArgs)
        If e.KeyCode = Keys.Enter Then
            e.SuppressKeyPress = True
            BtnSearchCell_Click(Nothing, Nothing)
        End If
    End Sub

    Private Sub BtnSearchBarcode_Click(sender As Object, e As EventArgs)
        Dim barcode = txtBarcode.Text.Trim()
        If String.IsNullOrEmpty(barcode) Then
            MessageBox.Show("Please scan or enter a barcode.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            Return
        End If

        SearchOrder(barcode, "barcode")
    End Sub

    Private Sub BtnSearchCell_Click(sender As Object, e As EventArgs)
        Dim cellNumber = txtCellNumber.Text.Trim()
        If String.IsNullOrEmpty(cellNumber) Then
            MessageBox.Show("Please enter a cell number.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            Return
        End If

        SearchOrder(cellNumber, "cell")
    End Sub

    Private Sub SearchOrder(searchValue As String, searchType As String)
        Try
            Using conn As New SqlConnection(_connectionString)
                conn.Open()

                Dim sql As String
                If searchType = "barcode" Then
                    sql = "SELECT * FROM POS_UserDefinedOrders WHERE OrderNumber = @SearchValue AND BranchID = @BranchID"
                Else
                    sql = "SELECT * FROM POS_UserDefinedOrders WHERE CustomerCellNumber = @SearchValue AND BranchID = @BranchID ORDER BY OrderDate DESC"
                End If

                Using cmd As New SqlCommand(sql, conn)
                    cmd.Parameters.AddWithValue("@SearchValue", searchValue)
                    cmd.Parameters.AddWithValue("@BranchID", _branchID)

                    Dim adapter As New SqlDataAdapter(cmd)
                    Dim dt As New DataTable()
                    adapter.Fill(dt)

                    If dt.Rows.Count = 0 Then
                        MessageBox.Show("No order found.", "Not Found", MessageBoxButtons.OK, MessageBoxIcon.Information)
                        pnlOrderDetails.Visible = False
                        btnProcessPickup.Visible = False
                        Return
                    End If

                    ' If multiple orders found by cell number, take the most recent
                    _currentOrder = dt.Rows(0)
                    DisplayOrderDetails()
                End Using
            End Using

        Catch ex As Exception
            MessageBox.Show($"Error searching for order: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    Private Sub DisplayOrderDetails()
        If _currentOrder Is Nothing Then Return

        Dim orderNumber = _currentOrder("OrderNumber").ToString()
        Dim customerName = $"{_currentOrder("CustomerName")} {If(IsDBNull(_currentOrder("CustomerSurname")), "", _currentOrder("CustomerSurname"))}".Trim()
        Dim customerCell = _currentOrder("CustomerCellNumber").ToString()
        Dim collectionDate = CDate(_currentOrder("CollectionDate"))
        Dim collectionTime = CType(_currentOrder("CollectionTime"), TimeSpan)
        Dim status = _currentOrder("Status").ToString()
        Dim totalAmount = CDec(_currentOrder("TotalAmount"))

        ' Build order info text
        Dim orderInfo As New StringBuilder()
        orderInfo.AppendLine($"Order Number: {orderNumber}")
        orderInfo.AppendLine($"Customer: {customerName}")
        orderInfo.AppendLine($"Phone: {customerCell}")
        orderInfo.AppendLine($"Collection Date: {collectionDate:dd/MM/yyyy}")
        orderInfo.AppendLine($"Collection Time: {collectionTime:hh\:mm}")
        orderInfo.AppendLine($"Status: {status}")
        orderInfo.AppendLine($"Total Amount: R {totalAmount:N2}")

        If Not IsDBNull(_currentOrder("CakeColour")) Then
            orderInfo.AppendLine($"Cake Colour: {_currentOrder("CakeColour")}")
        End If

        If Not IsDBNull(_currentOrder("SpecialRequest")) Then
            orderInfo.AppendLine($"Special Request: {_currentOrder("SpecialRequest")}")
        End If

        lblOrderInfo.Text = orderInfo.ToString()

        ' Load order items
        LoadOrderItems(CInt(_currentOrder("UserDefinedOrderID")))

        ' Show panel
        pnlOrderDetails.Visible = True

        ' Show/hide Process Pickup button based on status
        If status = "Completed" Then
            btnProcessPickup.Visible = True
            btnProcessPickup.Enabled = True
        ElseIf status = "Created" Then
            btnProcessPickup.Visible = True
            btnProcessPickup.Enabled = False
            MessageBox.Show("Order is not ready yet. Status: Created", "Not Ready", MessageBoxButtons.OK, MessageBoxIcon.Information)
        ElseIf status = "PickedUp" Then
            btnProcessPickup.Visible = False
            Dim pickedUpDate = If(IsDBNull(_currentOrder("PickedUpDateTime")), DateTime.Now, CDate(_currentOrder("PickedUpDateTime")))
            MessageBox.Show($"Order already collected on {pickedUpDate:dd/MM/yyyy HH:mm:ss}", "Already Collected", MessageBoxButtons.OK, MessageBoxIcon.Information)
        End If
    End Sub

    Private Sub LoadOrderItems(orderID As Integer)
        Try
            Using conn As New SqlConnection(_connectionString)
                conn.Open()
                Dim sql = "SELECT ProductName AS Product, Quantity AS Qty, UnitPrice AS Price, LineTotal AS Total FROM POS_UserDefinedOrderItems WHERE UserDefinedOrderID = @OrderID"
                Using cmd As New SqlCommand(sql, conn)
                    cmd.Parameters.AddWithValue("@OrderID", orderID)
                    Dim adapter As New SqlDataAdapter(cmd)
                    Dim dt As New DataTable()
                    adapter.Fill(dt)
                    dgvItems.DataSource = dt
                End Using
            End Using
        Catch ex As Exception
            MessageBox.Show($"Error loading order items: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    Private Sub BtnProcessPickup_Click(sender As Object, e As EventArgs)
        Try
            If _currentOrder Is Nothing Then Return

            Dim orderNumber = _currentOrder("OrderNumber").ToString()
            Dim orderID = CInt(_currentOrder("UserDefinedOrderID"))

            ' Confirm pickup
            Dim result = MessageBox.Show($"Process pickup for order {orderNumber}?", "Confirm Pickup", MessageBoxButtons.YesNo, MessageBoxIcon.Question)
            If result = DialogResult.No Then Return

            ' Update status to PickedUp
            Using conn As New SqlConnection(_connectionString)
                conn.Open()
                Dim sql = "UPDATE POS_UserDefinedOrders SET Status = 'PickedUp', PickedUpDate = @PickupDate, PickedUpTime = @PickupTime, PickedUpDateTime = GETDATE(), PickedUpBy = @PickedUpBy WHERE UserDefinedOrderID = @OrderID"
                Using cmd As New SqlCommand(sql, conn)
                    cmd.Parameters.AddWithValue("@PickupDate", DateTime.Today)
                    cmd.Parameters.AddWithValue("@PickupTime", DateTime.Now.TimeOfDay)
                    cmd.Parameters.AddWithValue("@PickedUpBy", _cashierName)
                    cmd.Parameters.AddWithValue("@OrderID", orderID)
                    cmd.ExecuteNonQuery()
                End Using
            End Using

            ' Print pickup slip
            PrintPickupSlip()

            ' Show success message
            MessageBox.Show($"Order {orderNumber} picked up successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information)

            ' Close dialog
            Me.Close()

        Catch ex As Exception
            MessageBox.Show($"Error processing pickup: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    Private Sub PrintPickupSlip()
        Try
            ' Recreate order data from current order
            Dim orderData As New UserDefinedOrderData With {
                .CustomerCellNumber = _currentOrder("CustomerCellNumber").ToString(),
                .CustomerName = _currentOrder("CustomerName").ToString(),
                .CustomerSurname = If(IsDBNull(_currentOrder("CustomerSurname")), "", _currentOrder("CustomerSurname").ToString()),
                .CakeColour = If(IsDBNull(_currentOrder("CakeColour")), "", _currentOrder("CakeColour").ToString()),
                .CakeImage = If(IsDBNull(_currentOrder("CakeImage")), "", _currentOrder("CakeImage").ToString()),
                .SpecialRequest = If(IsDBNull(_currentOrder("SpecialRequest")), "", _currentOrder("SpecialRequest").ToString()),
                .CollectionDate = CDate(_currentOrder("CollectionDate")),
                .CollectionTime = CType(_currentOrder("CollectionTime"), TimeSpan),
                .CollectionDay = If(IsDBNull(_currentOrder("CollectionDay")), "", _currentOrder("CollectionDay").ToString())
            }

            ' Get items
            Dim itemsTable = CType(dgvItems.DataSource, DataTable)

            ' Print
            Dim printer As New UserDefinedOrderPrinter(_connectionString, _branchID)
            printer.PrintPickupSlip(_currentOrder("OrderNumber").ToString(), orderData, itemsTable, CDec(_currentOrder("TotalAmount")), _cashierName)

        Catch ex As Exception
            MessageBox.Show($"Error printing pickup slip: {ex.Message}", "Print Error", MessageBoxButtons.OK, MessageBoxIcon.Warning)
        End Try
    End Sub

    Private Sub BtnClose_Click(sender As Object, e As EventArgs)
        Me.Close()
    End Sub
End Class
