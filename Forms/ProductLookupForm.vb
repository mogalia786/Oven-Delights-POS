Imports System.Configuration
Imports System.Data.SqlClient
Imports System.Drawing
Imports System.Windows.Forms

Public Class ProductLookupForm
    Inherits Form

    Private _connectionString As String
    Private _branchID As Integer
    Private _allProducts As DataTable
    Private _filteredProducts As DataTable

    Private txtSearch As TextBox
    Private dgvProducts As DataGridView
    Private pnlNumpad As Panel
    Private btnSelect As Button
    Private btnCancel As Button

    Public Property SelectedProductID As Integer = 0
    Public Property SelectedItemCode As String = ""
    Public Property SelectedProductName As String = ""
    Public Property SelectedPrice As Decimal = 0
    Public Property SelectedStock As Decimal = 0

    Public Sub New(branchID As Integer)
        MyBase.New()
        _branchID = branchID
        _connectionString = ConfigurationManager.ConnectionStrings("OvenDelightsERPConnectionString").ConnectionString

        InitializeComponent()

        ' Load products AFTER form is shown
        AddHandler Me.Shown, Sub()
                                 LoadProducts()
                             End Sub
    End Sub

    Private Sub InitializeComponent()
        Me.Text = "Product Lookup"
        Me.Size = New Size(1000, 700)
        Me.StartPosition = FormStartPosition.CenterScreen
        Me.FormBorderStyle = FormBorderStyle.FixedDialog
        Me.MaximizeBox = False
        Me.MinimizeBox = False
        Me.BackColor = Color.White

        ' Header
        Dim lblHeader As New Label With {
            .Text = "🔍 PRODUCT LOOKUP",
            .Font = New Font("Segoe UI", 20, FontStyle.Bold),
            .ForeColor = Color.White,
            .BackColor = ColorTranslator.FromHtml("#2C3E50"),
            .Dock = DockStyle.Top,
            .Height = 60,
            .TextAlign = ContentAlignment.MiddleCenter
        }

        ' Search box
        Dim pnlSearch As New Panel With {
            .Location = New Point(20, 80),
            .Size = New Size(600, 50),
            .BackColor = Color.White
        }

        Dim lblSearch As New Label With {
            .Text = "Search:",
            .Font = New Font("Segoe UI", 12, FontStyle.Bold),
            .Location = New Point(0, 15),
            .AutoSize = True
        }

        txtSearch = New TextBox With {
            .Font = New Font("Segoe UI", 14),
            .Location = New Point(80, 10),
            .Size = New Size(500, 30)
        }
        AddHandler txtSearch.TextChanged, AddressOf TxtSearch_TextChanged
        AddHandler txtSearch.KeyDown, AddressOf TxtSearch_KeyDown

        pnlSearch.Controls.AddRange({lblSearch, txtSearch})

        ' Products grid
        dgvProducts = New DataGridView With {
            .Location = New Point(20, 150),
            .Size = New Size(600, 450),
            .ReadOnly = True,
            .SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            .MultiSelect = False,
            .AllowUserToAddRows = False,
            .AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            .BackgroundColor = Color.White,
            .BorderStyle = BorderStyle.FixedSingle
        }
        AddHandler dgvProducts.CellDoubleClick, AddressOf DgvProducts_CellDoubleClick

        ' Numpad
        pnlNumpad = CreateNumpad()
        pnlNumpad.Location = New Point(640, 150)

        ' Buttons
        btnSelect = New Button With {
            .Text = "✓ SELECT",
            .Size = New Size(150, 50),
            .Location = New Point(640, 550),
            .BackColor = ColorTranslator.FromHtml("#27AE60"),
            .ForeColor = Color.White,
            .Font = New Font("Segoe UI", 12, FontStyle.Bold),
            .FlatStyle = FlatStyle.Flat
        }
        btnSelect.FlatAppearance.BorderSize = 0
        AddHandler btnSelect.Click, AddressOf BtnSelect_Click

        btnCancel = New Button With {
            .Text = "✖ CANCEL",
            .Size = New Size(150, 50),
            .Location = New Point(810, 550),
            .BackColor = ColorTranslator.FromHtml("#E74C3C"),
            .ForeColor = Color.White,
            .Font = New Font("Segoe UI", 12, FontStyle.Bold),
            .FlatStyle = FlatStyle.Flat
        }
        btnCancel.FlatAppearance.BorderSize = 0
        AddHandler btnCancel.Click, Sub() Me.DialogResult = DialogResult.Cancel

        Me.Controls.AddRange({lblHeader, pnlSearch, dgvProducts, pnlNumpad, btnSelect, btnCancel})
    End Sub

    Private Function CreateNumpad() As Panel
        Dim pnl As New Panel With {
            .Size = New Size(320, 380),
            .BackColor = ColorTranslator.FromHtml("#ECF0F1")
        }

        Dim buttons = {"7", "8", "9", "4", "5", "6", "1", "2", "3", "0", ".", "←"}
        Dim x = 0, y = 0

        For i = 0 To buttons.Length - 1
            Dim btn As New Button With {
                .Text = buttons(i),
                .Size = New Size(100, 90),
                .Location = New Point(x * 105 + 5, y * 95 + 5),
                .Font = New Font("Segoe UI", 20, FontStyle.Bold),
                .BackColor = Color.White,
                .FlatStyle = FlatStyle.Flat
            }
            btn.FlatAppearance.BorderSize = 1

            Dim btnText = buttons(i)
            AddHandler btn.Click, Sub() NumpadClick(btnText)

            pnl.Controls.Add(btn)

            x += 1
            If x = 3 Then
                x = 0
                y += 1
            End If
        Next

        Return pnl
    End Function

    Private Sub NumpadClick(value As String)
        If value = "←" Then
            If txtSearch.Text.Length > 0 Then
                txtSearch.Text = txtSearch.Text.Substring(0, txtSearch.Text.Length - 1)
            End If
        Else
            txtSearch.Text &= value
        End If
        txtSearch.Focus()
        txtSearch.SelectionStart = txtSearch.Text.Length
    End Sub

    Private Sub LoadProducts()
        Try
            _allProducts = New DataTable()

            Using conn As New SqlConnection(_connectionString)
                conn.Open()

                Dim sql = "
                    SELECT 
                        ProductID,
                        ItemCode,
                        ProductName,
                        ISNULL(SellingPrice, 0) AS Price,
                        ISNULL(QtyOnHand, 0) AS Stock
                    FROM vw_POS_Products
                    WHERE BranchID = @BranchID
                    ORDER BY ProductName"

                Using cmd As New SqlCommand(sql, conn)
                    cmd.Parameters.AddWithValue("@BranchID", _branchID)

                    Using adapter As New SqlDataAdapter(cmd)
                        adapter.Fill(_allProducts)
                    End Using
                End Using
            End Using

            If _allProducts.Rows.Count = 0 Then
                MessageBox.Show("No products found. Please check the vw_POS_Products view exists.", "No Products", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                Return
            End If

            _filteredProducts = _allProducts.Copy()
            dgvProducts.DataSource = Nothing
            dgvProducts.DataSource = _filteredProducts
            dgvProducts.Refresh()

            ' Format columns after a brief delay to ensure grid is ready
            Dim timer As New Timer With {.Interval = 100}
            AddHandler timer.Tick, Sub()
                                       timer.Stop()
                                       FormatColumns()
                                   End Sub
            timer.Start()

        Catch ex As Exception
            MessageBox.Show($"Error loading products: {ex.Message}{vbCrLf}{vbCrLf}Stack: {ex.StackTrace}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    Private Sub FormatColumns()
        Try
            ' Exit silently if grid not ready
            If dgvProducts Is Nothing Then Exit Sub
            If dgvProducts.Columns Is Nothing Then Exit Sub
            If dgvProducts.Columns.Count = 0 Then Exit Sub

            ' Format each column safely
            Try
                If dgvProducts.Columns.Contains("ProductID") Then
                    Dim col = dgvProducts.Columns("ProductID")
                    If col IsNot Nothing Then
                        col.Visible = False
                    End If
                End If
            Catch
                ' Skip this column
            End Try

            Try
                If dgvProducts.Columns.Contains("ItemCode") Then
                    Dim col = dgvProducts.Columns("ItemCode")
                    If col IsNot Nothing Then
                        col.HeaderText = "Code"
                        col.Width = 150
                    End If
                End If
            Catch
                ' Skip this column
            End Try

            Try
                If dgvProducts.Columns.Contains("ProductName") Then
                    Dim col = dgvProducts.Columns("ProductName")
                    If col IsNot Nothing Then
                        col.HeaderText = "Product"
                    End If
                End If
            Catch
                ' Skip this column
            End Try

            Try
                If dgvProducts.Columns.Contains("Price") Then
                    Dim col = dgvProducts.Columns("Price")
                    If col IsNot Nothing Then
                        col.HeaderText = "Price"
                        col.DefaultCellStyle.Format = "C2"
                        col.Width = 100
                    End If
                End If
            Catch
                ' Skip this column
            End Try

            Try
                If dgvProducts.Columns.Contains("Stock") Then
                    Dim col = dgvProducts.Columns("Stock")
                    If col IsNot Nothing Then
                        col.HeaderText = "Stock"
                        col.DefaultCellStyle.Format = "N2"
                        col.Width = 80
                    End If
                End If
            Catch
                ' Skip this column
            End Try

        Catch ex As Exception
            ' Silently ignore all formatting errors
            Debug.WriteLine($"FormatColumns error: {ex.Message}")
        End Try
    End Sub

    Private Sub TxtSearch_TextChanged(sender As Object, e As EventArgs)
        FilterProducts()
    End Sub

    Private Sub TxtSearch_KeyDown(sender As Object, e As KeyEventArgs)
        If e.KeyCode = Keys.Enter Then
            e.SuppressKeyPress = True
            If dgvProducts.Rows.Count > 0 Then
                dgvProducts.Rows(0).Selected = True
                SelectProduct()
            End If
        ElseIf e.KeyCode = Keys.Down Then
            e.SuppressKeyPress = True
            dgvProducts.Focus()
            If dgvProducts.Rows.Count > 0 Then
                dgvProducts.Rows(0).Selected = True
            End If
        End If
    End Sub

    Private Sub FilterProducts()
        If String.IsNullOrWhiteSpace(txtSearch.Text) Then
            _filteredProducts = _allProducts.Copy()
        Else
            Dim searchText = txtSearch.Text.ToLower()
            Dim filteredRows = _allProducts.AsEnumerable().Where(Function(row)
                                                                     Dim itemCode = row("ItemCode").ToString().ToLower()
                                                                     Dim productName = row("ProductName").ToString().ToLower()
                                                                     Return itemCode.Contains(searchText) OrElse productName.Contains(searchText)
                                                                 End Function)

            If filteredRows.Any() Then
                _filteredProducts = filteredRows.CopyToDataTable()
            Else
                _filteredProducts = _allProducts.Clone()
            End If
        End If

        dgvProducts.DataSource = _filteredProducts
    End Sub

    Private Sub DgvProducts_CellDoubleClick(sender As Object, e As DataGridViewCellEventArgs)
        If e.RowIndex >= 0 Then
            SelectProduct()
        End If
    End Sub

    Private Sub BtnSelect_Click(sender As Object, e As EventArgs)
        SelectProduct()
    End Sub

    Private Sub SelectProduct()
        If dgvProducts.SelectedRows.Count = 0 Then
            MessageBox.Show("Please select a product.", "Select Product", MessageBoxButtons.OK, MessageBoxIcon.Information)
            Return
        End If

        Dim row = dgvProducts.SelectedRows(0)
        SelectedProductID = CInt(row.Cells("ProductID").Value)
        SelectedItemCode = row.Cells("ItemCode").Value.ToString()
        SelectedProductName = row.Cells("ProductName").Value.ToString()
        SelectedPrice = CDec(row.Cells("Price").Value)
        SelectedStock = CDec(row.Cells("Stock").Value)

        Me.DialogResult = DialogResult.OK
        Me.Close()
    End Sub
End Class
