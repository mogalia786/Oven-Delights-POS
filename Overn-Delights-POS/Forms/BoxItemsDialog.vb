Imports System.Configuration
Imports System.Data
Imports System.Data.SqlClient
Imports System.Drawing
Imports System.Windows.Forms

Public Class BoxItemsDialog
    Inherits Form

    Private _connectionString As String
    Private _branchID As Integer
    Private _cashierName As String
    Private _boxBarcode As String
    Private _boxItems As New DataTable()

    ' UI Controls
    Private lblHeader As Label
    Private lblBoxBarcode As Label
    Private txtBarcode As TextBox
    Private dgvItems As DataGridView
    Private btnComplete As Button
    Private btnCancel As Button
    Private btnRemove As Button
    Private lblTotal As Label

    Public Sub New(connectionString As String, branchID As Integer, cashierName As String)
        MyBase.New()
        _connectionString = connectionString
        _branchID = branchID
        _cashierName = cashierName
        _boxBarcode = GenerateBoxBarcode()

        InitializeComponent()
        InitializeDataTable()
    End Sub

    Private Sub InitializeComponent()
        Me.Text = "Box Items - Create Box Barcode"
        Me.Size = New Size(900, 700)
        Me.StartPosition = FormStartPosition.CenterScreen
        Me.FormBorderStyle = FormBorderStyle.FixedDialog
        Me.MaximizeBox = False
        Me.MinimizeBox = False
        Me.BackColor = Color.White

        Dim yPos As Integer = 20

        ' Header
        lblHeader = New Label With {
            .Text = "BOX ITEMS - SCAN TO ADD",
            .Font = New Font("Segoe UI", 16, FontStyle.Bold),
            .ForeColor = ColorTranslator.FromHtml("#E67E22"),
            .Location = New Point(20, yPos),
            .Size = New Size(850, 35),
            .TextAlign = ContentAlignment.MiddleCenter
        }
        Me.Controls.Add(lblHeader)
        yPos += 45

        ' Box Barcode Display
        lblBoxBarcode = New Label With {
            .Text = $"BOX BARCODE: {_boxBarcode}",
            .Font = New Font("Segoe UI", 14, FontStyle.Bold),
            .ForeColor = ColorTranslator.FromHtml("#2C3E50"),
            .Location = New Point(20, yPos),
            .Size = New Size(850, 30),
            .TextAlign = ContentAlignment.MiddleCenter
        }
        Me.Controls.Add(lblBoxBarcode)
        yPos += 40

        ' Barcode input
        Dim lblScan As New Label With {
            .Text = "Scan Item Barcode:",
            .Font = New Font("Segoe UI", 11, FontStyle.Bold),
            .Location = New Point(20, yPos),
            .Size = New Size(200, 25)
        }
        Me.Controls.Add(lblScan)

        txtBarcode = New TextBox With {
            .Location = New Point(230, yPos),
            .Size = New Size(640, 30),
            .Font = New Font("Segoe UI", 12)
        }
        AddHandler txtBarcode.KeyDown, AddressOf TxtBarcode_KeyDown
        Me.Controls.Add(txtBarcode)
        yPos += 40

        ' DataGridView
        dgvItems = New DataGridView With {
            .Location = New Point(20, yPos),
            .Size = New Size(850, 400),
            .AllowUserToAddRows = False,
            .AllowUserToDeleteRows = True,
            .SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            .MultiSelect = False,
            .AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None,
            .RowHeadersVisible = False,
            .BackgroundColor = Color.White,
            .BorderStyle = BorderStyle.FixedSingle,
            .Font = New Font("Segoe UI", 10, FontStyle.Bold)
        }
        AddHandler dgvItems.DataBindingComplete, AddressOf DgvItems_DataBindingComplete
        Me.Controls.Add(dgvItems)
        yPos += 410

        ' Total
        lblTotal = New Label With {
            .Text = "TOTAL: R 0.00",
            .Font = New Font("Segoe UI", 14, FontStyle.Bold),
            .ForeColor = ColorTranslator.FromHtml("#27AE60"),
            .Location = New Point(20, yPos),
            .Size = New Size(850, 30),
            .TextAlign = ContentAlignment.MiddleRight
        }
        Me.Controls.Add(lblTotal)
        yPos += 40

        ' Buttons
        btnRemove = New Button With {
            .Text = "Remove Selected",
            .Font = New Font("Segoe UI", 11, FontStyle.Bold),
            .Size = New Size(180, 45),
            .Location = New Point(100, yPos),
            .BackColor = ColorTranslator.FromHtml("#E67E22"),
            .ForeColor = Color.White,
            .FlatStyle = FlatStyle.Flat,
            .Cursor = Cursors.Hand
        }
        btnRemove.FlatAppearance.BorderSize = 0
        AddHandler btnRemove.Click, AddressOf BtnRemove_Click

        btnComplete = New Button With {
            .Text = "Complete & Print",
            .Font = New Font("Segoe UI", 11, FontStyle.Bold),
            .Size = New Size(200, 45),
            .Location = New Point(300, yPos),
            .BackColor = ColorTranslator.FromHtml("#27AE60"),
            .ForeColor = Color.White,
            .FlatStyle = FlatStyle.Flat,
            .Cursor = Cursors.Hand
        }
        btnComplete.FlatAppearance.BorderSize = 0
        AddHandler btnComplete.Click, AddressOf BtnComplete_Click

        btnCancel = New Button With {
            .Text = "Cancel",
            .Font = New Font("Segoe UI", 11, FontStyle.Bold),
            .Size = New Size(180, 45),
            .Location = New Point(520, yPos),
            .BackColor = ColorTranslator.FromHtml("#E74C3C"),
            .ForeColor = Color.White,
            .FlatStyle = FlatStyle.Flat,
            .Cursor = Cursors.Hand
        }
        btnCancel.FlatAppearance.BorderSize = 0
        AddHandler btnCancel.Click, AddressOf BtnCancel_Click

        Me.Controls.AddRange({btnRemove, btnComplete, btnCancel})
    End Sub

    Private Sub InitializeDataTable()
        _boxItems.Columns.Add("ItemBarcode", GetType(String))
        _boxItems.Columns.Add("ProductName", GetType(String))
        _boxItems.Columns.Add("Quantity", GetType(Decimal))
        _boxItems.Columns.Add("Price", GetType(Decimal))
        _boxItems.Columns.Add("Total", GetType(Decimal))

        dgvItems.DataSource = _boxItems
        AddHandler dgvItems.CellValueChanged, AddressOf DgvItems_CellValueChanged
    End Sub

    Private Sub DgvItems_DataBindingComplete(sender As Object, e As DataGridViewBindingCompleteEventArgs)
        Try
            ' Configure columns after data binding is complete
            If dgvItems.Columns.Contains("ItemBarcode") Then
                dgvItems.Columns("ItemBarcode").HeaderText = "Barcode"
                dgvItems.Columns("ItemBarcode").Width = 150
                dgvItems.Columns("ItemBarcode").ReadOnly = True
            End If

            If dgvItems.Columns.Contains("ProductName") Then
                dgvItems.Columns("ProductName").HeaderText = "Product Name"
                dgvItems.Columns("ProductName").Width = 350
                dgvItems.Columns("ProductName").ReadOnly = True
            End If

            If dgvItems.Columns.Contains("Quantity") Then
                dgvItems.Columns("Quantity").HeaderText = "Qty"
                dgvItems.Columns("Quantity").Width = 80
                dgvItems.Columns("Quantity").DefaultCellStyle.Format = "N2"
            End If

            If dgvItems.Columns.Contains("Price") Then
                dgvItems.Columns("Price").HeaderText = "Price"
                dgvItems.Columns("Price").Width = 100
                dgvItems.Columns("Price").DefaultCellStyle.Format = "C2"
                dgvItems.Columns("Price").ReadOnly = True
            End If

            If dgvItems.Columns.Contains("Total") Then
                dgvItems.Columns("Total").HeaderText = "Total"
                dgvItems.Columns("Total").Width = 120
                dgvItems.Columns("Total").DefaultCellStyle.Format = "C2"
                dgvItems.Columns("Total").ReadOnly = True
            End If
        Catch ex As Exception
            ' Ignore configuration errors
        End Try
    End Sub

    Private Function GenerateBoxBarcode() As String
        ' Generate 6-digit numeric box barcode: BranchID (1 digit) + Sequence (5 digits)
        ' Format: {BranchID}{Sequence} e.g., 600001
        Try
            Using conn As New SqlConnection(_connectionString)
                conn.Open()

                ' Get next sequence number for this branch
                Dim sql = "
                    SELECT ISNULL(MAX(CAST(BoxBarcode AS INT)), 0) + 1 AS NextSeq
                    FROM BoxedItems
                    WHERE BranchID = @BranchID
                      AND LEN(BoxBarcode) = 6
                      AND BoxBarcode NOT LIKE '%[^0-9]%'"

                Dim sequence As Integer = 1
                Using cmd As New SqlCommand(sql, conn)
                    cmd.Parameters.AddWithValue("@BranchID", _branchID)
                    Dim result = cmd.ExecuteScalar()
                    If result IsNot Nothing AndAlso Not IsDBNull(result) Then
                        sequence = CInt(result)
                    End If
                End Using

                ' Build 6-digit barcode: BranchID (1 digit) + Sequence (5 digits)
                Dim branchDigit = _branchID Mod 10 ' Use last digit of branch ID
                Dim seqPart = (sequence Mod 100000).ToString("D5") ' 5-digit sequence
                Return $"{branchDigit}{seqPart}"
            End Using
        Catch ex As Exception
            ' Fallback to simple sequence if database fails
            Dim branchDigit = _branchID Mod 10
            Return $"{branchDigit}00001"
        End Try
    End Function

    Private Sub TxtBarcode_KeyDown(sender As Object, e As KeyEventArgs)
        If e.KeyCode = Keys.Enter Then
            e.SuppressKeyPress = True
            AddItemByBarcode(txtBarcode.Text.Trim())
            txtBarcode.Clear()
            txtBarcode.Focus()
        End If
    End Sub

    Private Sub AddItemByBarcode(barcode As String)
        If String.IsNullOrWhiteSpace(barcode) Then Return

        Try
            Using conn As New SqlConnection(_connectionString)
                conn.Open()

                Dim sql = "
                    SELECT TOP 1
                        p.ProductID,
                        p.Name AS ProductName,
                        ISNULL(p.Barcode, p.SKU) AS Barcode,
                        ISNULL(price.SellingPrice, 0) AS Price
                    FROM Demo_Retail_Product p
                    LEFT JOIN Demo_Retail_Price price ON price.ProductID = p.ProductID AND price.BranchID = @BranchID
                    WHERE (p.Barcode LIKE '%' + @Barcode + '%' OR p.SKU LIKE '%' + @Barcode + '%')
                      AND p.BranchID = @BranchID
                      AND p.IsActive = 1"

                Using cmd As New SqlCommand(sql, conn)
                    cmd.Parameters.AddWithValue("@Barcode", barcode)
                    cmd.Parameters.AddWithValue("@BranchID", _branchID)

                    Using reader = cmd.ExecuteReader()
                        If reader.Read() Then
                            Dim productName = reader("ProductName").ToString()
                            Dim itemBarcode = reader("Barcode").ToString()
                            Dim price = CDec(reader("Price"))

                            ' Check if item already exists in grid
                            Dim existingRow = _boxItems.AsEnumerable().FirstOrDefault(Function(r) r.Field(Of String)("ItemBarcode") = itemBarcode)
                            If existingRow IsNot Nothing Then
                                ' Increment quantity
                                Dim currentQty = CDec(existingRow("Quantity"))
                                existingRow("Quantity") = currentQty + 1
                                existingRow("Total") = (currentQty + 1) * price
                            Else
                                ' Add new row
                                _boxItems.Rows.Add(itemBarcode, productName, 1, price, price)
                            End If

                            UpdateTotal()
                        Else
                            MessageBox.Show($"Product not found: {barcode}", "Not Found", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                        End If
                    End Using
                End Using
            End Using
        Catch ex As Exception
            MessageBox.Show($"Error adding item: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    Private Sub DgvItems_CellValueChanged(sender As Object, e As DataGridViewCellEventArgs)
        If e.RowIndex >= 0 AndAlso e.ColumnIndex = _boxItems.Columns("Quantity").Ordinal Then
            Dim row = dgvItems.Rows(e.RowIndex)
            Dim qty = CDec(row.Cells("Quantity").Value)
            Dim price = CDec(row.Cells("Price").Value)
            row.Cells("Total").Value = qty * price
            UpdateTotal()
        End If
    End Sub

    Private Sub UpdateTotal()
        Dim total As Decimal = 0
        For Each row As DataRow In _boxItems.Rows
            total += CDec(row("Total"))
        Next
        lblTotal.Text = $"TOTAL: {total:C2}"
    End Sub

    Private Sub BtnComplete_Click(sender As Object, e As EventArgs)
        If _boxItems.Rows.Count = 0 Then
            MessageBox.Show("Please add at least one item to the box.", "No Items", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            Return
        End If

        Try
            ' Save box items to database
            Using conn As New SqlConnection(_connectionString)
                conn.Open()

                For Each row As DataRow In _boxItems.Rows
                    Dim sql = "
                        INSERT INTO BoxedItems (BoxBarcode, ItemBarcode, ProductName, Quantity, Price, BranchID, CreatedBy, CreatedDate, IsActive)
                        VALUES (@BoxBarcode, @ItemBarcode, @ProductName, @Quantity, @Price, @BranchID, @CreatedBy, GETDATE(), 1)"

                    Using cmd As New SqlCommand(sql, conn)
                        cmd.Parameters.AddWithValue("@BoxBarcode", _boxBarcode)
                        cmd.Parameters.AddWithValue("@ItemBarcode", row("ItemBarcode"))
                        cmd.Parameters.AddWithValue("@ProductName", row("ProductName"))
                        cmd.Parameters.AddWithValue("@Quantity", row("Quantity"))
                        cmd.Parameters.AddWithValue("@Price", row("Price"))
                        cmd.Parameters.AddWithValue("@BranchID", _branchID)
                        cmd.Parameters.AddWithValue("@CreatedBy", _cashierName)
                        cmd.ExecuteNonQuery()
                    End Using
                Next
            End Using

            ' Print box slip and barcode sticker
            Dim printer As New BoxItemsPrinter(_boxBarcode, _boxItems, _cashierName)
            printer.PrintBoxSlip()
            printer.PrintBarcodeSticker()

            MessageBox.Show("Box created successfully! Slip and sticker printed.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information)
            Me.DialogResult = DialogResult.OK
            Me.Close()

        Catch ex As Exception
            MessageBox.Show($"Error saving box: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    Private Sub BtnRemove_Click(sender As Object, e As EventArgs)
        If dgvItems.SelectedRows.Count = 0 Then
            MessageBox.Show("Please select an item to remove.", "No Selection", MessageBoxButtons.OK, MessageBoxIcon.Information)
            Return
        End If

        Try
            ' Remove selected row from DataTable
            Dim selectedIndex = dgvItems.SelectedRows(0).Index
            _boxItems.Rows(selectedIndex).Delete()
            _boxItems.AcceptChanges()
            UpdateTotal()
        Catch ex As Exception
            MessageBox.Show($"Error removing item: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    Private Sub BtnCancel_Click(sender As Object, e As EventArgs)
        Me.DialogResult = DialogResult.Cancel
        Me.Close()
    End Sub
End Class
