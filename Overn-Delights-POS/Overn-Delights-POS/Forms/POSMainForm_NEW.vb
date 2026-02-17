Imports System.Configuration
Imports System.Data.SqlClient
Imports System.Drawing
Imports System.Windows.Forms

Public Class POSMainForm
    ' Core properties
    Private _connectionString As String
    Private _cashierID As Integer
    Private _cashierName As String
    Private _branchID As Integer
    Private _cartItems As New DataTable()
    
    ' UI Controls
    Private pnlTop As Panel
    Private pnlCategories As Panel
    Private pnlProducts As Panel
    Private pnlCart As Panel
    Private txtBarcodeScanner As TextBox
    Private flpCategories As FlowLayoutPanel
    Private flpProducts As FlowLayoutPanel
    Private dgvCart As DataGridView
    Private lblTotal As Label
    Private lblSubtotal As Label
    Private lblTax As Label
    
    ' Colors
    Private _primaryColor As Color = ColorTranslator.FromHtml("#D2691E")
    Private _accentColor As Color = ColorTranslator.FromHtml("#FFD700")
    
    Public Sub New(cashierID As Integer, cashierName As String, branchID As Integer)
        InitializeComponent()
        
        _cashierID = cashierID
        _cashierName = cashierName
        _branchID = branchID
        _connectionString = ConfigurationManager.ConnectionStrings("OvenDelightsERPConnectionString").ConnectionString
        
        Me.KeyPreview = True
        SetupUI()
        InitializeCart()
    End Sub
    
    Private Sub SetupUI()
        Me.Text = "Oven Delights POS"
        Me.WindowState = FormWindowState.Maximized
        Me.BackColor = Color.FromArgb(245, 245, 245)
        
        ' TOP PANEL
        pnlTop = New Panel With {
            .Dock = DockStyle.Top,
            .Height = 60,
            .BackColor = _primaryColor
        }
        
        Dim lblTitle As New Label With {
            .Text = "OVEN DELIGHTS POS",
            .Font = New Font("Segoe UI", 18, FontStyle.Bold),
            .ForeColor = Color.White,
            .Location = New Point(20, 15),
            .AutoSize = True
        }
        
        ' BARCODE SCANNER
        txtBarcodeScanner = New TextBox With {
            .Font = New Font("Segoe UI", 14),
            .Width = 300,
            .Height = 35,
            .Location = New Point(350, 12),
            .Text = "🔍 Scan ItemCode..."
        }
        AddHandler txtBarcodeScanner.KeyDown, AddressOf BarcodeScanner_KeyDown
        
        Dim lblCashier As New Label With {
            .Text = $"Cashier: {_cashierName}",
            .Font = New Font("Segoe UI", 12),
            .ForeColor = Color.White,
            .AutoSize = True
        }
        lblCashier.Location = New Point(Me.Width - lblCashier.Width - 150, 20)
        lblCashier.Anchor = AnchorStyles.Top Or AnchorStyles.Right
        
        Dim btnLogout As New Button With {
            .Text = "Logout",
            .Font = New Font("Segoe UI", 10, FontStyle.Bold),
            .Size = New Size(80, 35),
            .BackColor = Color.FromArgb(220, 53, 69),
            .ForeColor = Color.White,
            .FlatStyle = FlatStyle.Flat,
            .Cursor = Cursors.Hand
        }
        btnLogout.Location = New Point(Me.Width - 100, 12)
        btnLogout.Anchor = AnchorStyles.Top Or AnchorStyles.Right
        btnLogout.FlatAppearance.BorderSize = 0
        AddHandler btnLogout.Click, Sub() Me.Close()
        
        pnlTop.Controls.AddRange({lblTitle, txtBarcodeScanner, lblCashier, btnLogout})
        
        ' LEFT PANEL - Categories
        pnlCategories = New Panel With {
            .Dock = DockStyle.Left,
            .Width = CInt(Me.Width * 0.18),
            .BackColor = Color.White,
            .Padding = New Padding(10)
        }
        
        Dim lblCategoriesHeader As New Label With {
            .Text = "CATEGORIES",
            .Font = New Font("Segoe UI", 14, FontStyle.Bold),
            .ForeColor = _primaryColor,
            .Dock = DockStyle.Top,
            .Height = 40,
            .TextAlign = ContentAlignment.MiddleLeft
        }
        
        flpCategories = New FlowLayoutPanel With {
            .Dock = DockStyle.Fill,
            .AutoScroll = True,
            .FlowDirection = FlowDirection.TopDown,
            .WrapContents = False,
            .Padding = New Padding(0, 5, 0, 5)
        }
        
        pnlCategories.Controls.AddRange({flpCategories, lblCategoriesHeader})
        
        ' CENTER PANEL - Products (Touch scrollable)
        pnlProducts = New Panel With {
            .Dock = DockStyle.Fill,
            .BackColor = Color.FromArgb(245, 245, 245),
            .Padding = New Padding(10)
        }
        
        flpProducts = New FlowLayoutPanel With {
            .Dock = DockStyle.Fill,
            .AutoScroll = True,
            .FlowDirection = FlowDirection.LeftToRight,
            .WrapContents = True,
            .Padding = New Padding(5),
            .BackColor = Color.White
        }
        
        pnlProducts.Controls.Add(flpProducts)
        
        ' RIGHT PANEL - Cart
        pnlCart = New Panel With {
            .Dock = DockStyle.Right,
            .Width = CInt(Me.Width * 0.3),
            .BackColor = Color.White,
            .Padding = New Padding(10)
        }
        
        Dim lblCartHeader As New Label With {
            .Text = "CURRENT SALE",
            .Font = New Font("Segoe UI", 14, FontStyle.Bold),
            .ForeColor = _primaryColor,
            .Dock = DockStyle.Top,
            .Height = 40,
            .TextAlign = ContentAlignment.MiddleLeft
        }
        
        dgvCart = New DataGridView With {
            .Dock = DockStyle.Fill,
            .BackgroundColor = Color.White,
            .BorderStyle = BorderStyle.None,
            .AllowUserToAddRows = False,
            .ReadOnly = False,
            .SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            .RowHeadersVisible = False,
            .AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            .Font = New Font("Segoe UI", 11)
        }
        
        Dim pnlTotals As New Panel With {
            .Dock = DockStyle.Bottom,
            .Height = 180,
            .BackColor = _primaryColor,
            .Padding = New Padding(15)
        }
        
        lblSubtotal = New Label With {
            .Text = "Subtotal: R 0.00",
            .Font = New Font("Segoe UI", 12),
            .ForeColor = Color.White,
            .Location = New Point(15, 15),
            .AutoSize = True
        }
        
        lblTax = New Label With {
            .Text = "VAT (15%): R 0.00",
            .Font = New Font("Segoe UI", 12),
            .ForeColor = Color.White,
            .Location = New Point(15, 40),
            .AutoSize = True
        }
        
        lblTotal = New Label With {
            .Text = "R 0.00",
            .Font = New Font("Segoe UI", 28, FontStyle.Bold),
            .ForeColor = _accentColor,
            .Location = New Point(15, 70),
            .AutoSize = True
        }
        
        Dim btnPay As New Button With {
            .Text = "PAY (F12)",
            .Font = New Font("Segoe UI", 16, FontStyle.Bold),
            .Size = New Size(pnlTotals.Width - 30, 60),
            .Location = New Point(15, 110),
            .BackColor = Color.FromArgb(40, 167, 69),
            .ForeColor = Color.White,
            .FlatStyle = FlatStyle.Flat,
            .Cursor = Cursors.Hand
        }
        btnPay.FlatAppearance.BorderSize = 0
        
        pnlTotals.Controls.AddRange({lblSubtotal, lblTax, lblTotal, btnPay})
        pnlCart.Controls.AddRange({dgvCart, pnlTotals, lblCartHeader})
        
        ' Add all panels to form
        Me.Controls.AddRange({pnlProducts, pnlCart, pnlCategories, pnlTop})
    End Sub
    
    Private Sub InitializeCart()
        _cartItems.Columns.Add("ProductID", GetType(Integer))
        _cartItems.Columns.Add("ItemCode", GetType(String))
        _cartItems.Columns.Add("Product", GetType(String))
        _cartItems.Columns.Add("Qty", GetType(Decimal))
        _cartItems.Columns.Add("Price", GetType(Decimal))
        _cartItems.Columns.Add("Total", GetType(Decimal))
        
        dgvCart.DataSource = _cartItems
        dgvCart.Columns("ProductID").Visible = False
        dgvCart.Columns("ItemCode").Width = 80
        dgvCart.Columns("Qty").Width = 60
        dgvCart.Columns("Qty").ReadOnly = False
        dgvCart.Columns("Price").DefaultCellStyle.Format = "C2"
        dgvCart.Columns("Total").DefaultCellStyle.Format = "C2"
    End Sub
    
    Protected Overrides Sub OnLoad(e As EventArgs)
        MyBase.OnLoad(e)
        
        Try
            LoadCategories()
            LoadProducts()
            txtBarcodeScanner.Focus()
        Catch ex As Exception
            MessageBox.Show($"Error loading data: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub
    
    Private Sub LoadCategories()
        flpCategories.Controls.Clear()
        
        Dim sql = "SELECT CategoryID, CategoryCode, CategoryName FROM ProductCategories WHERE IsActive = 1 ORDER BY CategoryName"
        
        Using conn As New SqlConnection(_connectionString)
            conn.Open()
            Using cmd As New SqlCommand(sql, conn)
                Using reader = cmd.ExecuteReader()
                    ' Add "All Products" button
                    Dim btnAll = CreateCategoryButton(Nothing, "ALL PRODUCTS", "📦")
                    flpCategories.Controls.Add(btnAll)
                    
                    While reader.Read()
                        Dim categoryID = reader.GetInt32(0)
                        Dim categoryCode = reader.GetString(1)
                        Dim categoryName = reader.GetString(2)
                        Dim icon = GetCategoryIcon(categoryCode)
                        Dim btn = CreateCategoryButton(categoryID, categoryName, icon)
                        flpCategories.Controls.Add(btn)
                    End While
                End Using
            End Using
        End Using
    End Sub
    
    Private Function CreateCategoryButton(categoryID As Integer?, categoryName As String, icon As String) As Button
        Dim btn As New Button With {
            .Text = $"{icon} {categoryName}",
            .Size = New Size(flpCategories.Width - 30, 70),
            .BackColor = Color.White,
            .ForeColor = _primaryColor,
            .Font = New Font("Segoe UI", 12, FontStyle.Bold),
            .FlatStyle = FlatStyle.Flat,
            .Cursor = Cursors.Hand,
            .TextAlign = ContentAlignment.MiddleLeft,
            .Padding = New Padding(15, 0, 0, 0),
            .Tag = categoryID
        }
        btn.FlatAppearance.BorderColor = Color.FromArgb(200, 200, 200)
        
        AddHandler btn.Click, Sub()
                                  LoadProducts(categoryID)
                                  txtBarcodeScanner.Focus()
                              End Sub
        AddHandler btn.MouseEnter, Sub() btn.BackColor = Color.FromArgb(240, 240, 240)
        AddHandler btn.MouseLeave, Sub() btn.BackColor = Color.White
        
        Return btn
    End Function
    
    Private Function GetCategoryIcon(categoryCode As String) As String
        Select Case categoryCode.ToUpper()
            Case "BREAD", "BRD"
                Return "🍞"
            Case "CAKE", "CKE"
                Return "🎂"
            Case "PASTRY", "PST"
                Return "🥐"
            Case "COOKIE", "COK"
                Return "🍪"
            Case "CUPCAKE", "CPC"
                Return "🧁"
            Case "DONUT", "DNT"
                Return "🍩"
            Case "PIE"
                Return "🥧"
            Case "BEVERAGE", "BEV"
                Return "☕"
            Case Else
                Return "📦"
        End Select
    End Function
    
    Private Sub LoadProducts(Optional categoryID As Integer? = Nothing)
        flpProducts.SuspendLayout()
        flpProducts.Controls.Clear()
        
        Try
            Dim sql = "
                SELECT 
                    p.ProductID,
                    p.ProductCode AS ItemCode,
                    p.ProductName,
                    p.SellingPrice,
                    ISNULL(rs.QtyOnHand, 0) AS QtyOnHand,
                    pc.CategoryName
                FROM Products p
                INNER JOIN ProductCategories pc ON p.CategoryID = pc.CategoryID
                LEFT JOIN Demo_Retail_Stock rs ON p.ProductID = rs.ProductID AND rs.BranchID = @BranchID
                WHERE p.IsActive = 1
                  AND p.ItemType = 'Finished'
                  AND ISNULL(rs.QtyOnHand, 0) > 0"
            
            If categoryID.HasValue Then
                sql &= " AND p.CategoryID = @CategoryID"
            End If
            
            sql &= " ORDER BY p.ProductCode"
            
            Using conn As New SqlConnection(_connectionString)
                conn.Open()
                Using cmd As New SqlCommand(sql, conn)
                    cmd.Parameters.AddWithValue("@BranchID", _branchID)
                    If categoryID.HasValue Then
                        cmd.Parameters.AddWithValue("@CategoryID", categoryID.Value)
                    End If
                    
                    Using reader = cmd.ExecuteReader()
                        While reader.Read()
                            Dim card = CreateProductCard(
                                reader.GetInt32(0),
                                reader.GetString(1),
                                reader.GetString(2),
                                reader.GetDecimal(3),
                                reader.GetDecimal(4)
                            )
                            flpProducts.Controls.Add(card)
                        End While
                    End Using
                End Using
            End Using
            
        Finally
            flpProducts.ResumeLayout()
        End Try
    End Sub
    
    Private Function CreateProductCard(productID As Integer, itemCode As String, productName As String, price As Decimal, stock As Decimal) As Panel
        Dim card As New Panel With {
            .Size = New Size(180, 120),
            .BackColor = Color.White,
            .BorderStyle = BorderStyle.FixedSingle,
            .Cursor = Cursors.Hand,
            .Margin = New Padding(5),
            .Tag = New With {productID, itemCode, productName, price, stock}
        }
        
        Dim lblItemCode As New Label With {
            .Text = itemCode,
            .Font = New Font("Segoe UI", 9, FontStyle.Bold),
            .ForeColor = _primaryColor,
            .Location = New Point(5, 5),
            .AutoSize = True
        }
        
        Dim lblName As New Label With {
            .Text = productName,
            .Font = New Font("Segoe UI", 10),
            .Location = New Point(5, 25),
            .Size = New Size(170, 40),
            .AutoEllipsis = True
        }
        
        Dim lblPrice As New Label With {
            .Text = price.ToString("C2"),
            .Font = New Font("Segoe UI", 12, FontStyle.Bold),
            .ForeColor = Color.FromArgb(40, 167, 69),
            .Location = New Point(5, 70),
            .AutoSize = True
        }
        
        Dim lblStock As New Label With {
            .Text = $"Stock: {stock}",
            .Font = New Font("Segoe UI", 8),
            .ForeColor = Color.Gray,
            .Location = New Point(5, 95),
            .AutoSize = True
        }
        
        card.Controls.AddRange({lblItemCode, lblName, lblPrice, lblStock})
        
        AddHandler card.Click, Sub() AddProductToCart(productID, itemCode, productName, price)
        AddHandler card.MouseEnter, Sub() card.BackColor = Color.FromArgb(240, 248, 255)
        AddHandler card.MouseLeave, Sub() card.BackColor = Color.White
        
        Return card
    End Function
    
    Private Sub BarcodeScanner_KeyDown(sender As Object, e As KeyEventArgs)
        If e.KeyCode = Keys.Enter Then
            e.SuppressKeyPress = True
            Dim itemCode = txtBarcodeScanner.Text.Trim()
            If Not String.IsNullOrWhiteSpace(itemCode) AndAlso itemCode <> "🔍 Scan ItemCode..." Then
                ProcessBarcodeScan(itemCode)
            End If
        End If
    End Sub
    
    Private Sub ProcessBarcodeScan(itemCode As String)
        Try
            Dim sql = "
                SELECT TOP 1
                    p.ProductID,
                    p.ProductCode AS ItemCode,
                    p.ProductName,
                    p.SellingPrice,
                    ISNULL(rs.QtyOnHand, 0) AS QtyOnHand
                FROM Products p
                LEFT JOIN Demo_Retail_Stock rs ON p.ProductID = rs.ProductID AND rs.BranchID = @BranchID
                WHERE p.ProductCode = @ItemCode
                  AND p.IsActive = 1
                  AND p.ItemType = 'Finished'
                  AND ISNULL(rs.QtyOnHand, 0) > 0"
            
            Using conn As New SqlConnection(_connectionString)
                conn.Open()
                Using cmd As New SqlCommand(sql, conn)
                    cmd.Parameters.AddWithValue("@ItemCode", itemCode)
                    cmd.Parameters.AddWithValue("@BranchID", _branchID)
                    
                    Using reader = cmd.ExecuteReader()
                        If reader.Read() Then
                            AddProductToCart(
                                reader.GetInt32(0),
                                reader.GetString(1),
                                reader.GetString(2),
                                reader.GetDecimal(3)
                            )
                            txtBarcodeScanner.BackColor = Color.LightGreen
                            Task.Delay(200).ContinueWith(Sub()
                                Me.Invoke(Sub()
                                    txtBarcodeScanner.Clear()
                                    txtBarcodeScanner.BackColor = Color.White
                                    txtBarcodeScanner.Focus()
                                End Sub)
                            End Sub)
                        Else
                            txtBarcodeScanner.BackColor = Color.LightCoral
                            MessageBox.Show($"Product not found: {itemCode}", "Scan Error", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                            txtBarcodeScanner.SelectAll()
                        End If
                    End Using
                End Using
            End Using
        Catch ex As Exception
            MessageBox.Show($"Scan error: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub
    
    Private Sub AddProductToCart(productID As Integer, itemCode As String, productName As String, price As Decimal)
        ' Check if already in cart
        Dim existingRow = _cartItems.Select($"ProductID = {productID}")
        If existingRow.Length > 0 Then
            existingRow(0)("Qty") = CDec(existingRow(0)("Qty")) + 1
            existingRow(0)("Total") = CDec(existingRow(0)("Qty")) * CDec(existingRow(0)("Price"))
        Else
            _cartItems.Rows.Add(productID, itemCode, productName, 1, price, price)
        End If
        
        CalculateTotals()
        txtBarcodeScanner.Focus()
    End Sub
    
    Private Sub CalculateTotals()
        Dim subtotal As Decimal = 0
        For Each row As DataRow In _cartItems.Rows
            subtotal += CDec(row("Total"))
        Next
        
        Dim tax = subtotal * 0.15D
        Dim total = subtotal + tax
        
        lblSubtotal.Text = $"Subtotal: {subtotal.ToString("C2")}"
        lblTax.Text = $"VAT (15%): {tax.ToString("C2")}"
        lblTotal.Text = total.ToString("C2")
    End Sub
    
    Protected Overrides Function ProcessCmdKey(ByRef msg As Message, keyData As Keys) As Boolean
        Select Case keyData
            Case Keys.F1
                NewSale()
                Return True
            Case Keys.F12
                ProcessPayment()
                Return True
        End Select
        
        Return MyBase.ProcessCmdKey(msg, keyData)
    End Function
    
    Private Sub NewSale()
        _cartItems.Clear()
        CalculateTotals()
        txtBarcodeScanner.Focus()
    End Sub
    
    Private Sub ProcessPayment()
        MessageBox.Show("Payment processing - To be implemented", "Payment", MessageBoxButtons.OK, MessageBoxIcon.Information)
    End Sub
End Class
