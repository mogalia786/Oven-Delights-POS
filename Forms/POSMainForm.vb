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
    Private pnlCategories As FlowLayoutPanel
    Private pnlProducts As Panel
    Private pnlCart As Panel
    Private pnlShortcuts As Panel
    Private flpProducts As FlowLayoutPanel
    Private dgvCart As DataGridView
    Private lblTotal As Label
    Private lblSubtotal As Label
    Private lblTax As Label
    Private txtSearch As TextBox
    Private txtBarcodeScanner As TextBox
    Private btnShowMore As Button
    Private _shortcutsExpanded As Boolean = False

    ' Idle screen
    Private _idleTimer As Timer
    Private _idleOverlay As Panel
    Private _messageTimer As Timer
    Private _currentMessageIndex As Integer = 0
    Private _lblRotatingMessage As Label
    Private Const IDLE_TIMEOUT_MS As Integer = 5000 ' 5 seconds for testing

    ' Modern Color Palette
    Private _darkBlue As Color = ColorTranslator.FromHtml("#2C3E50")
    Private _lightBlue As Color = ColorTranslator.FromHtml("#3498DB")
    Private _green As Color = ColorTranslator.FromHtml("#27AE60")
    Private _orange As Color = ColorTranslator.FromHtml("#E67E22")
    Private _red As Color = ColorTranslator.FromHtml("#E74C3C")
    Private _purple As Color = ColorTranslator.FromHtml("#9B59B6")
    Private _yellow As Color = ColorTranslator.FromHtml("#F39C12")
    Private _lightGray As Color = ColorTranslator.FromHtml("#ECF0F1")
    Private _darkGray As Color = ColorTranslator.FromHtml("#7F8C8D")

    Public Sub New(cashierID As Integer, cashierName As String, branchID As Integer)
        InitializeComponent()

        _cashierID = cashierID
        _cashierName = cashierName
        _branchID = branchID
        _connectionString = ConfigurationManager.ConnectionStrings("OvenDelightsERPConnectionString").ConnectionString

        Me.KeyPreview = True
        SetupModernUI()
        InitializeCart()
        SetupIdleScreen()
        
        ' Add resize handler
        AddHandler Me.Resize, AddressOf POSMainForm_Resize
        AddHandler Me.Load, AddressOf POSMainForm_Load
    End Sub

    Private Sub SetupModernUI()
        ' Clear any designer-created controls
        Me.SuspendLayout()
        Me.Controls.Clear()

        Me.Text = "Oven Delights POS - MODERN VERSION"
        Me.WindowState = FormWindowState.Maximized
        Me.BackColor = _lightGray
        Me.FormBorderStyle = FormBorderStyle.None

        ' TOP BAR - Modern gradient effect
        pnlTop = New Panel With {
            .Dock = DockStyle.Top,
            .Height = 70,
            .BackColor = _darkBlue
        }

        Dim lblTitle As New Label With {
            .Text = "🍰 OVEN DELIGHTS POS",
            .Font = New Font("Segoe UI", 22, FontStyle.Bold),
            .ForeColor = Color.White,
            .Location = New Point(25, 18),
            .AutoSize = True
        }

        ' BARCODE SCANNER - Always focused
        txtBarcodeScanner = New TextBox With {
            .Font = New Font("Segoe UI", 14),
            .Width = 350,
            .Height = 40,
            .Location = New Point(400, 15),
            .Text = "🔍 Scan barcode or type ItemCode...",
            .ForeColor = _darkGray
        }
        AddHandler txtBarcodeScanner.KeyDown, AddressOf BarcodeScanner_KeyDown
        AddHandler txtBarcodeScanner.Enter, Sub()
                                                If txtBarcodeScanner.ForeColor = _darkGray Then
                                                    txtBarcodeScanner.Text = ""
                                                    txtBarcodeScanner.ForeColor = Color.Black
                                                End If
                                            End Sub
        AddHandler txtBarcodeScanner.Leave, Sub()
                                                If String.IsNullOrWhiteSpace(txtBarcodeScanner.Text) Then
                                                    txtBarcodeScanner.Text = "🔍 Scan barcode or type ItemCode..."
                                                    txtBarcodeScanner.ForeColor = _darkGray
                                                End If
                                            End Sub

        Dim lblCashier As New Label With {
            .Text = $"👤 {_cashierName}",
            .Font = New Font("Segoe UI", 12),
            .ForeColor = Color.White,
            .AutoSize = True
        }
        lblCashier.Location = New Point(Me.Width - lblCashier.Width - 150, 25)
        lblCashier.Anchor = AnchorStyles.Top Or AnchorStyles.Right

        Dim btnRefresh As New Button With {
            .Text = "🔄 Refresh",
            .Font = New Font("Segoe UI", 10, FontStyle.Bold),
            .Size = New Size(100, 40),
            .BackColor = _green,
            .ForeColor = Color.White,
            .FlatStyle = FlatStyle.Flat,
            .Cursor = Cursors.Hand
        }
        btnRefresh.Location = New Point(Me.Width - 360, 15)
        btnRefresh.Anchor = AnchorStyles.Top Or AnchorStyles.Right
        btnRefresh.FlatAppearance.BorderSize = 0
        AddHandler btnRefresh.Click, AddressOf BtnRefresh_Click
        
        Dim btnDayEnd As New Button With {
            .Text = "📊 Day End",
            .Font = New Font("Segoe UI", 11, FontStyle.Bold),
            .Size = New Size(120, 40),
            .BackColor = _orange,
            .ForeColor = Color.White,
            .FlatStyle = FlatStyle.Flat,
            .Cursor = Cursors.Hand
        }
        btnDayEnd.Location = New Point(Me.Width - 240, 15)
        btnDayEnd.Anchor = AnchorStyles.Top Or AnchorStyles.Right
        btnDayEnd.FlatAppearance.BorderSize = 0
        AddHandler btnDayEnd.Click, AddressOf BtnDayEnd_Click
        
        Dim btnLogout As New Button With {
            .Text = "🚪 Logout",
            .Font = New Font("Segoe UI", 11, FontStyle.Bold),
            .Size = New Size(100, 40),
            .BackColor = _red,
            .ForeColor = Color.White,
            .FlatStyle = FlatStyle.Flat,
            .Cursor = Cursors.Hand
        }
        btnLogout.Location = New Point(Me.Width - 120, 15)
        btnLogout.Anchor = AnchorStyles.Top Or AnchorStyles.Right
        btnLogout.FlatAppearance.BorderSize = 0
        AddHandler btnLogout.Click, Sub() Me.Close()

        pnlTop.Controls.AddRange({lblTitle, txtBarcodeScanner, lblCashier, btnRefresh, btnDayEnd, btnLogout})

        ' LEFT PANEL - Categories with FlowLayoutPanel
        Dim pnlCategoriesContainer As New Panel With {
            .Dock = DockStyle.Left,
            .Width = 200,
            .BackColor = Color.White,
            .Padding = New Padding(0)
        }

        Dim lblCategoriesHeader As New Label With {
            .Text = "📂 CATEGORIES",
            .Font = New Font("Segoe UI", 13, FontStyle.Bold),
            .ForeColor = _darkBlue,
            .Dock = DockStyle.Top,
            .Height = 50,
            .TextAlign = ContentAlignment.MiddleCenter,
            .BackColor = _lightGray
        }

        pnlCategories = New FlowLayoutPanel With {
            .Dock = DockStyle.Fill,
            .AutoScroll = True,
            .FlowDirection = FlowDirection.TopDown,
            .WrapContents = False,
            .Padding = New Padding(5),
            .BackColor = Color.White
        }

        pnlCategoriesContainer.Controls.AddRange({pnlCategories, lblCategoriesHeader})

        ' CENTER PANEL - Products
        pnlProducts = New Panel With {
            .Dock = DockStyle.Fill,
            .BackColor = _lightGray,
            .Padding = New Padding(10)
        }

        txtSearch = New TextBox With {
            .Font = New Font("Segoe UI", 14),
            .Dock = DockStyle.Top,
            .Height = 45,
            .Text = "🔍 Search products...",
            .ForeColor = _darkGray
        }
        AddHandler txtSearch.Enter, Sub()
                                        If txtSearch.Text = "🔍 Search products..." Then
                                            txtSearch.Text = ""
                                            txtSearch.ForeColor = Color.Black
                                        End If
                                    End Sub
        AddHandler txtSearch.Leave, Sub()
                                        If String.IsNullOrWhiteSpace(txtSearch.Text) Then
                                            txtSearch.Text = "🔍 Search products..."
                                            txtSearch.ForeColor = _darkGray
                                            ShowWelcomeMessage()
                                        End If
                                    End Sub
        AddHandler txtSearch.TextChanged, AddressOf txtSearch_TextChanged

        flpProducts = New FlowLayoutPanel With {
            .Dock = DockStyle.Fill,
            .AutoScroll = True,
            .FlowDirection = FlowDirection.LeftToRight,
            .WrapContents = True,
            .Padding = New Padding(10),
            .BackColor = _lightGray
        }

        pnlProducts.Controls.AddRange({flpProducts, txtSearch})

        ' RIGHT PANEL - Cart
        pnlCart = New Panel With {
            .Dock = DockStyle.Right,
            .Width = 380,
            .BackColor = Color.White,
            .Padding = New Padding(0)
        }

        Dim lblCartHeader As New Label With {
            .Text = "🛒 CURRENT SALE",
            .Font = New Font("Segoe UI", 14, FontStyle.Bold),
            .ForeColor = Color.White,
            .BackColor = _darkBlue,
            .Dock = DockStyle.Top,
            .Height = 50,
            .TextAlign = ContentAlignment.MiddleCenter
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
            .Font = New Font("Segoe UI", 10),
            .RowTemplate = New DataGridViewRow With {.Height = 45}
        }

        Dim pnlTotals As New Panel With {
            .Dock = DockStyle.Bottom,
            .Height = 220,
            .BackColor = _darkBlue,
            .Padding = New Padding(20)
        }

        lblSubtotal = New Label With {
            .Text = "Subtotal: R 0.00",
            .Font = New Font("Segoe UI", 12),
            .ForeColor = Color.White,
            .Location = New Point(20, 15),
            .AutoSize = True
        }

        lblTax = New Label With {
            .Text = "VAT (15%): R 0.00",
            .Font = New Font("Segoe UI", 12),
            .ForeColor = Color.White,
            .Location = New Point(20, 45),
            .AutoSize = True
        }

        lblTotal = New Label With {
            .Text = "R 0.00",
            .Font = New Font("Segoe UI", 32, FontStyle.Bold),
            .ForeColor = _yellow,
            .Location = New Point(20, 75),
            .AutoSize = True
        }

        Dim btnPay As New Button With {
            .Text = "💳 PAY NOW (F12)",
            .Font = New Font("Segoe UI", 16, FontStyle.Bold),
            .Size = New Size(340, 65),
            .Location = New Point(20, 140),
            .BackColor = _green,
            .ForeColor = Color.White,
            .FlatStyle = FlatStyle.Flat,
            .Cursor = Cursors.Hand
        }
        btnPay.FlatAppearance.BorderSize = 0
        AddHandler btnPay.Click, Sub() ProcessPayment()

        pnlTotals.Controls.AddRange({lblSubtotal, lblTax, lblTotal, btnPay})
        pnlCart.Controls.AddRange({dgvCart, pnlTotals, lblCartHeader})

        ' BOTTOM PANEL - F-Key Shortcuts
        pnlShortcuts = New Panel With {
            .Dock = DockStyle.Bottom,
            .Height = 90,
            .BackColor = Color.White,
            .Padding = New Padding(10)
        }

        CreateShortcutButtons()

        ' Add all panels to form - ORDER MATTERS!
        Me.Controls.Add(pnlTop)
        Me.Controls.Add(pnlShortcuts)
        Me.Controls.Add(pnlCategoriesContainer)
        Me.Controls.Add(pnlCart)
        Me.Controls.Add(pnlProducts) ' Fill goes last

        Me.ResumeLayout(True)
        Me.PerformLayout()
    End Sub

    Private Sub InitializeCart()
        _cartItems.Columns.Add("ProductID", GetType(Integer))
        _cartItems.Columns.Add("ItemCode", GetType(String))
        _cartItems.Columns.Add("Product", GetType(String))
        _cartItems.Columns.Add("Qty", GetType(Decimal))
        _cartItems.Columns.Add("Price", GetType(Decimal))
        _cartItems.Columns.Add("Total", GetType(Decimal))

        dgvCart.AutoGenerateColumns = True
        dgvCart.DataSource = _cartItems

        ' Configure columns after first row is added
        AddHandler dgvCart.DataBindingComplete, AddressOf ConfigureCartColumns
    End Sub

    Private Sub ConfigureCartColumns(sender As Object, e As DataGridViewBindingCompleteEventArgs)
        ' Only configure once
        RemoveHandler dgvCart.DataBindingComplete, AddressOf ConfigureCartColumns

        If dgvCart.Columns.Count > 0 Then
            For Each col As DataGridViewColumn In dgvCart.Columns
                Select Case col.Name
                    Case "ProductID"
                        col.Visible = False
                    Case "ItemCode"
                        col.Width = 70
                        col.HeaderText = "Code"
                    Case "Product"
                        col.HeaderText = "Item"
                    Case "Qty"
                        col.Width = 50
                        col.ReadOnly = False
                        col.HeaderText = "Qty"
                    Case "Price"
                        col.DefaultCellStyle.Format = "C2"
                        col.Width = 80
                    Case "Total"
                        col.DefaultCellStyle.Format = "C2"
                        col.Width = 90
                End Select
            Next
        End If
    End Sub

    Protected Overrides Sub OnLoad(e As EventArgs)
        MyBase.OnLoad(e)

        Try
            LoadCategories()
            ' Show idle screen immediately on startup
            ShowIdleScreen()
        Catch ex As Exception
            MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    Private Sub ShowWelcomeMessage()
        flpProducts.Controls.Clear()
        Dim lblWelcome As New Label With {
            .Text = "👈 Select a category to view products",
            .Font = New Font("Segoe UI", 18, FontStyle.Bold),
            .ForeColor = _darkGray,
            .AutoSize = True,
            .Location = New Point(50, 100)
        }
        flpProducts.Controls.Add(lblWelcome)
    End Sub

    Private Sub LoadCategories()
        pnlCategories.Controls.Clear()

        ' Add "All Products" button at the top
        Dim btnAll As New Button With {
            .Text = "📦 All Products",
            .Size = New Size(190, 60),
            .BackColor = _darkBlue,
            .ForeColor = Color.White,
            .Font = New Font("Segoe UI", 11, FontStyle.Bold),
            .FlatStyle = FlatStyle.Flat,
            .Cursor = Cursors.Hand,
            .TextAlign = ContentAlignment.MiddleLeft,
            .Padding = New Padding(10, 0, 0, 0),
            .Margin = New Padding(0, 0, 0, 10)
        }
        btnAll.FlatAppearance.BorderSize = 0
        AddHandler btnAll.Click, Sub() LoadProducts()
        AddHandler btnAll.MouseEnter, Sub() btnAll.BackColor = _lightBlue
        AddHandler btnAll.MouseLeave, Sub() btnAll.BackColor = _darkBlue
        pnlCategories.Controls.Add(btnAll)

        Try
            ' Load categories that actually have products in Demo_Retail_Product
            Dim sql = "
                SELECT DISTINCT drp.Category
                FROM Demo_Retail_Product drp
                WHERE drp.IsActive = 1 
                  AND drp.Category IS NOT NULL
                  AND drp.Category <> ''
                ORDER BY drp.Category"

            Using conn As New SqlConnection(_connectionString)
                conn.Open()
                Using cmd As New SqlCommand(sql, conn)
                    Using reader = cmd.ExecuteReader()
                        Dim colorIndex = 0
                        Dim colors() As Color = {_lightBlue, _green, _orange, _purple, _red, _yellow}

                        While reader.Read()
                            Dim categoryName = reader.GetString(0)
                            Dim icon = GetCategoryIcon(categoryName)
                            Dim btnColor = colors(colorIndex Mod colors.Length)

                            Dim btn As New Button With {
                                .Text = $"{icon} {categoryName}",
                                .Size = New Size(190, 60),
                                .BackColor = btnColor,
                                .ForeColor = Color.White,
                                .Font = New Font("Segoe UI", 11, FontStyle.Bold),
                                .FlatStyle = FlatStyle.Flat,
                                .Cursor = Cursors.Hand,
                                .TextAlign = ContentAlignment.MiddleLeft,
                                .Padding = New Padding(10, 0, 0, 0),
                                .Tag = categoryName,
                                .Margin = New Padding(0, 0, 0, 5)
                            }
                            btn.FlatAppearance.BorderSize = 0

                            AddHandler btn.Click, Sub(s, ev)
                                                      LoadProducts(categoryName)
                                                  End Sub
                            AddHandler btn.MouseEnter, Sub(s, ev)
                                                           btn.BackColor = ControlPaint.Light(btnColor, 0.2)
                                                       End Sub
                            AddHandler btn.MouseLeave, Sub(s, ev)
                                                           btn.BackColor = btnColor
                                                       End Sub

                            pnlCategories.Controls.Add(btn)
                            colorIndex += 1
                        End While
                    End Using
                End Using
            End Using
        Catch ex As Exception
            MessageBox.Show($"Error loading categories: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    Private Function GetCategoryIcon(categoryCode As String) As String
        Select Case categoryCode.ToUpper()
            Case "BREAD", "BRD" : Return "🍞"
            Case "CAKE", "CKE" : Return "🎂"
            Case "PASTRY", "PST" : Return "🥐"
            Case "COOKIE", "COK" : Return "🍪"
            Case "CUPCAKE", "CPC" : Return "🧁"
            Case "DONUT", "DNT" : Return "🍩"
            Case "PIE" : Return "🥧"
            Case "BEVERAGE", "BEV", "BEVERAGES" : Return "☕"
            Case "MANUFACTURED", "GOODS" : Return "🏭"
            Case "PACKAGING" : Return "📦"
            Case "RAW", "MATERIALS" : Return "🌾"
            Case Else : Return "📦"
        End Select
    End Function

    Private Sub txtSearch_TextChanged(sender As Object, e As EventArgs)
        If txtSearch.ForeColor = _darkGray Then Return ' Ignore placeholder text

        Dim searchText = txtSearch.Text.Trim()
        If String.IsNullOrWhiteSpace(searchText) Then
            ShowWelcomeMessage()
            Return
        End If

        ' Search products
        SearchProducts(searchText)
    End Sub

    Private Sub SearchProducts(searchText As String)
        ' Show loading indicator
        flpProducts.Controls.Clear()
        Dim lblSearching As New Label With {
            .Text = $"🔍 Searching for '{searchText}'...",
            .Font = New Font("Segoe UI", 14),
            .ForeColor = _lightBlue,
            .AutoSize = True,
            .Location = New Point(50, 100)
        }
        flpProducts.Controls.Add(lblSearching)
        Application.DoEvents()

        flpProducts.SuspendLayout()
        flpProducts.Controls.Clear()

        Try
            Dim sql = "
                SELECT 
                    p.ProductID,
                    p.SKU AS ItemCode,
                    p.Name AS ProductName,
                    ISNULL(p.Barcode, p.SKU) AS Barcode,
                    ISNULL(
                        (SELECT TOP 1 SellingPrice FROM Demo_Retail_Price 
                         WHERE ProductID = p.ProductID AND BranchID = @BranchID 
                         ORDER BY EffectiveFrom DESC),
                        (SELECT TOP 1 SellingPrice FROM Demo_Retail_Price 
                         WHERE ProductID = p.ProductID AND BranchID IS NULL 
                         ORDER BY EffectiveFrom DESC)
                    ) AS SellingPrice,
                    ISNULL(p.CurrentStock, 0) AS QtyOnHand
                FROM Demo_Retail_Product p
                WHERE p.IsActive = 1
                  AND p.Category NOT IN ('ingredients', 'sub recipe', 'packaging', 'consumables', 'equipment', 'miscellaneous', 'pest control')
                  AND (p.ProductType = 'External' OR p.ProductType = 'Internal')
                  AND (p.SKU LIKE '%' + @Search + '%' OR p.Name LIKE '%' + @Search + '%' OR ISNULL(p.Barcode, '') LIKE '%' + @Search + '%')
                ORDER BY p.Name"

            Using conn As New SqlConnection(_connectionString)
                conn.Open()
                Using cmd As New SqlCommand(sql, conn)
                    cmd.Parameters.AddWithValue("@BranchID", _branchID)
                    cmd.Parameters.AddWithValue("@Search", searchText)

                    Using reader = cmd.ExecuteReader()
                        Dim productCount = 0
                        While reader.Read()
                            Dim card = CreateProductCard(
                                reader.GetInt32(0),
                                reader.GetString(1),
                                reader.GetString(2),
                                reader.GetDecimal(3),
                                reader.GetDecimal(4)
                            )
                            flpProducts.Controls.Add(card)
                            productCount += 1
                        End While

                        If productCount = 0 Then
                            Dim lblNoResults As New Label With {
                                .Text = $"No products found matching '{searchText}'",
                                .Font = New Font("Segoe UI", 14),
                                .ForeColor = _darkGray,
                                .AutoSize = True,
                                .Location = New Point(50, 100)
                            }
                            flpProducts.Controls.Add(lblNoResults)
                        End If
                    End Using
                End Using
            End Using
        Catch ex As Exception
            MessageBox.Show($"Search error: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        Finally
            flpProducts.ResumeLayout()
        End Try
    End Sub

    Private Sub LoadProducts(Optional category As String = Nothing)
        ' Show loading indicator
        flpProducts.Controls.Clear()
        Dim lblLoading As New Label With {
            .Text = "⏳ Loading products...",
            .Font = New Font("Segoe UI", 16, FontStyle.Bold),
            .ForeColor = _lightBlue,
            .AutoSize = True,
            .Location = New Point(50, 100)
        }
        flpProducts.Controls.Add(lblLoading)
        Application.DoEvents()

        flpProducts.SuspendLayout()
        flpProducts.Controls.Clear()

        Try
            ' First check what categories exist in products
            Dim debugSql = "SELECT DISTINCT Category FROM Demo_Retail_Product WHERE IsActive = 1 AND Category IS NOT NULL"

            Dim sql = "
                SELECT DISTINCT
                    drp.ProductID,
                    drp.SKU AS ItemCode,
                    drp.Name AS ProductName,
                    ISNULL(price.SellingPrice, 0) AS SellingPrice,
                    ISNULL(stock.QtyOnHand, 0) AS QtyOnHand,
                    drp.Category AS CategoryName
                FROM Demo_Retail_Product drp
                LEFT JOIN Demo_Retail_Variant drv ON drp.ProductID = drv.ProductID
                LEFT JOIN Demo_Retail_Stock stock ON drv.VariantID = stock.VariantID AND (stock.BranchID = @BranchID OR stock.BranchID IS NULL)
                LEFT JOIN Demo_Retail_Price price ON drp.ProductID = price.ProductID AND (price.BranchID = @BranchID OR price.BranchID IS NULL)
                WHERE drp.IsActive = 1"

            ' Remove stock and price filters temporarily to see all products
            If Not String.IsNullOrEmpty(category) Then
                sql &= " AND drp.Category = @Category"
            End If

            sql &= " ORDER BY drp.SKU"

            Using conn As New SqlConnection(_connectionString)
                conn.Open()
                Using cmd As New SqlCommand(sql, conn)
                    cmd.Parameters.AddWithValue("@BranchID", _branchID)
                    If Not String.IsNullOrEmpty(category) Then
                        cmd.Parameters.AddWithValue("@Category", category)
                    End If

                    Using reader = cmd.ExecuteReader()
                        Dim productCount = 0
                        While reader.Read()
                            Dim card = CreateProductCard(
                                reader.GetInt32(0),
                                reader.GetString(1),
                                reader.GetString(2),
                                reader.GetDecimal(3),
                                reader.GetDecimal(4)
                            )
                            flpProducts.Controls.Add(card)
                            productCount += 1
                        End While

                        ' Show message if no products found
                        If productCount = 0 Then
                            Dim lblNoProducts As New Label With {
                                .Text = "No products found in this category",
                                .Font = New Font("Segoe UI", 14),
                                .ForeColor = _darkGray,
                                .AutoSize = True,
                                .Location = New Point(50, 100)
                            }
                            flpProducts.Controls.Add(lblNoProducts)
                        End If
                    End Using
                End Using
            End Using
        Catch ex As Exception
            MessageBox.Show($"Error loading products: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        Finally
            flpProducts.ResumeLayout()
        End Try
    End Sub

    Private Function CreateProductCard(productID As Integer, itemCode As String, productName As String, price As Decimal, stock As Decimal) As Panel
        Dim card As New Panel With {
            .Size = New Size(200, 140),
            .BackColor = Color.White,
            .BorderStyle = BorderStyle.FixedSingle,
            .Cursor = Cursors.Hand,
            .Margin = New Padding(8),
            .Tag = New With {productID, itemCode, productName, price, stock}
        }

        Dim lblItemCode As New Label With {
            .Text = itemCode,
            .Font = New Font("Segoe UI", 9, FontStyle.Bold),
            .ForeColor = _lightBlue,
            .Location = New Point(8, 8),
            .AutoSize = True
        }

        Dim lblName As New Label With {
            .Text = productName,
            .Font = New Font("Segoe UI", 11, FontStyle.Bold),
            .Location = New Point(8, 30),
            .Size = New Size(184, 50),
            .AutoEllipsis = True
        }

        Dim lblPrice As New Label With {
            .Text = price.ToString("C2"),
            .Font = New Font("Segoe UI", 14, FontStyle.Bold),
            .ForeColor = _green,
            .Location = New Point(8, 85),
            .AutoSize = True
        }

        Dim lblStock As New Label With {
            .Text = $"Stock: {stock}",
            .Font = New Font("Segoe UI", 8),
            .ForeColor = _darkGray,
            .Location = New Point(8, 115),
            .AutoSize = True
        }

        card.Controls.AddRange({lblItemCode, lblName, lblPrice, lblStock})

        AddHandler card.Click, Sub() AddProductToCart(productID, itemCode, productName, price)
        AddHandler card.MouseEnter, Sub() card.BackColor = _lightGray
        AddHandler card.MouseLeave, Sub() card.BackColor = Color.White

        Return card
    End Function

    Private Sub AddProductToCart(productID As Integer, itemCode As String, productName As String, price As Decimal)
        Dim existingRow = _cartItems.Select($"ProductID = {productID}")
        If existingRow.Length > 0 Then
            existingRow(0)("Qty") = CDec(existingRow(0)("Qty")) + 1
            existingRow(0)("Total") = CDec(existingRow(0)("Qty")) * CDec(existingRow(0)("Price"))
        Else
            _cartItems.Rows.Add(productID, itemCode, productName, 1, price, price)
        End If

        CalculateTotals()
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

    Private Sub CreateShortcutButtons()
        pnlShortcuts.Controls.Clear()

        Dim shortcuts As New List(Of Tuple(Of String, String, Action)) From {
            Tuple.Create("F1", "New Sale", CType(Sub() NewSale(), Action)),
            Tuple.Create("F2", "Hold", CType(Sub() HoldSale(), Action)),
            Tuple.Create("F3", "Search", CType(Sub() txtSearch.Focus(), Action)),
            Tuple.Create("F4", "Recall", CType(Sub() RecallSale(), Action)),
            Tuple.Create("F5", "Qty", CType(Sub() ChangeQuantity(), Action)),
            Tuple.Create("F6", "Discount", CType(Sub() ApplyDiscount(), Action)),
            Tuple.Create("F7", "Remove", CType(Sub() RemoveItem(), Action)),
            Tuple.Create("F8", "Returns", CType(Sub() ProcessReturn(), Action)),
            Tuple.Create("F9", "Reports", CType(Sub() ShowReports(), Action)),
            Tuple.Create("F10", "Drawer", CType(Sub() OpenCashDrawer(), Action)),
            Tuple.Create("F11", "Manager", CType(Sub() ManagerFunctions(), Action)),
            Tuple.Create("F12", "Pay", CType(Sub() ProcessPayment(), Action))
        }

        Dim visibleCount = If(_shortcutsExpanded, shortcuts.Count, 8)
        Dim availableWidth = pnlShortcuts.Width - 140 ' Leave space for More button
        Dim buttonWidth = (availableWidth \ visibleCount) - 8 ' 8px spacing between buttons
        Dim startX = 10

        For i = 0 To visibleCount - 1
            Dim shortcut = shortcuts(i)
            Dim btn As New Button With {
                .Text = $"{shortcut.Item1}{vbCrLf}{shortcut.Item2}",
                .Size = New Size(buttonWidth, 70),
                .Location = New Point(startX + (i * (buttonWidth + 8)), 10),
                .BackColor = _darkBlue,
                .ForeColor = Color.White,
                .Font = New Font("Segoe UI", 9, FontStyle.Bold),
                .FlatStyle = FlatStyle.Flat,
                .Cursor = Cursors.Hand,
                .Tag = shortcut.Item3
            }
            btn.FlatAppearance.BorderSize = 1
            btn.FlatAppearance.BorderColor = _lightBlue
            AddHandler btn.Click, Sub(s, e) CType(CType(s, Button).Tag, Action).Invoke()
            AddHandler btn.MouseEnter, Sub(s, e) btn.BackColor = _lightBlue
            AddHandler btn.MouseLeave, Sub(s, e) btn.BackColor = _darkBlue
            pnlShortcuts.Controls.Add(btn)
        Next

        ' Show More / Show Less button
        btnShowMore = New Button With {
            .Text = If(_shortcutsExpanded, "▲ Less", "▼ More"),
            .Size = New Size(110, 70),
            .Location = New Point(pnlShortcuts.Width - 120, 10),
            .BackColor = _yellow,
            .ForeColor = Color.Black,
            .Font = New Font("Segoe UI", 10, FontStyle.Bold),
            .FlatStyle = FlatStyle.Flat,
            .Cursor = Cursors.Hand
        }
        btnShowMore.Anchor = AnchorStyles.Bottom Or AnchorStyles.Right
        btnShowMore.FlatAppearance.BorderSize = 0
        AddHandler btnShowMore.Click, Sub()
                                          _shortcutsExpanded = Not _shortcutsExpanded
                                          pnlShortcuts.Height = If(_shortcutsExpanded, 160, 90)
                                          CreateShortcutButtons()
                                      End Sub
        pnlShortcuts.Controls.Add(btnShowMore)
    End Sub

    Private Sub BarcodeScanner_KeyDown(sender As Object, e As KeyEventArgs)
        If e.KeyCode = Keys.Enter Then
            e.SuppressKeyPress = True
            Dim itemCode = txtBarcodeScanner.Text.Trim()
            If itemCode <> "🔍 Scan barcode or type ItemCode..." AndAlso Not String.IsNullOrWhiteSpace(itemCode) Then
                ProcessBarcodeScan(itemCode)
            End If
        End If
    End Sub

    Private Sub ProcessBarcodeScan(itemCode As String)
        Try
            Dim sql = "
                SELECT TOP 1
                    drp.ProductID,
                    drp.SKU AS ItemCode,
                    drp.Name AS ProductName,
                    ISNULL(price.SellingPrice, 0) AS SellingPrice,
                    ISNULL(stock.QtyOnHand, 0) AS QtyOnHand
                FROM Demo_Retail_Product drp
                LEFT JOIN Demo_Retail_Variant drv ON drp.ProductID = drv.ProductID
                LEFT JOIN Demo_Retail_Stock stock ON drv.VariantID = stock.VariantID AND (stock.BranchID = @BranchID OR stock.BranchID IS NULL)
                LEFT JOIN Demo_Retail_Price price ON drp.ProductID = price.ProductID AND (price.BranchID = @BranchID OR price.BranchID IS NULL)
                WHERE drp.SKU = @ItemCode
                  AND drp.IsActive = 1
                  AND ISNULL(stock.QtyOnHand, 0) > 0
                  AND ISNULL(price.SellingPrice, 0) > 0"

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
                            txtBarcodeScanner.BackColor = _green
                            Task.Delay(200).ContinueWith(Sub()
                                                             Me.Invoke(Sub()
                                                                           txtBarcodeScanner.Clear()
                                                                           txtBarcodeScanner.BackColor = Color.White
                                                                           txtBarcodeScanner.Focus()
                                                                       End Sub)
                                                         End Sub)
                        Else
                            txtBarcodeScanner.BackColor = _red
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

    Protected Overrides Function ProcessCmdKey(ByRef msg As Message, keyData As Keys) As Boolean
        Select Case keyData
            Case Keys.F1 : NewSale() : Return True
            Case Keys.F2 : HoldSale() : Return True
            Case Keys.F3 : txtSearch.Focus() : Return True
            Case Keys.F4 : RecallSale() : Return True
            Case Keys.F5 : ChangeQuantity() : Return True
            Case Keys.F6 : ApplyDiscount() : Return True
            Case Keys.F7 : RemoveItem() : Return True
            Case Keys.F8 : ProcessReturn() : Return True
            Case Keys.F9 : ShowReports() : Return True
            Case Keys.F10 : OpenCashDrawer() : Return True
            Case Keys.F11 : ManagerFunctions() : Return True
            Case Keys.F12 : ProcessPayment() : Return True
        End Select
        Return MyBase.ProcessCmdKey(msg, keyData)
    End Function

    ' Shortcut Functions
    Private Sub NewSale()
        _cartItems.Clear()
        CalculateTotals()
        txtBarcodeScanner.Focus()
    End Sub

    Private Sub HoldSale()
        MessageBox.Show("Hold sale - Coming soon", "Hold", MessageBoxButtons.OK, MessageBoxIcon.Information)
    End Sub

    Private Sub RecallSale()
        MessageBox.Show("Recall sale - Coming soon", "Recall", MessageBoxButtons.OK, MessageBoxIcon.Information)
    End Sub

    Private Sub ChangeQuantity()
        If dgvCart.CurrentRow IsNot Nothing Then
            Dim currentQty = CDec(dgvCart.CurrentRow.Cells("Qty").Value)
            Dim input = InputBox("Enter new quantity:", "Change Quantity", currentQty.ToString())
            If Not String.IsNullOrEmpty(input) AndAlso IsNumeric(input) Then
                dgvCart.CurrentRow.Cells("Qty").Value = CDec(input)
                dgvCart.CurrentRow.Cells("Total").Value = CDec(input) * CDec(dgvCart.CurrentRow.Cells("Price").Value)
                CalculateTotals()
            End If
        End If
    End Sub

    Private Sub ApplyDiscount()
        MessageBox.Show("Discount - Coming soon", "Discount", MessageBoxButtons.OK, MessageBoxIcon.Information)
    End Sub

    Private Sub RemoveItem()
        If dgvCart.CurrentRow IsNot Nothing Then
            dgvCart.Rows.Remove(dgvCart.CurrentRow)
            CalculateTotals()
        End If
    End Sub

    Private Sub ProcessReturn()
        MessageBox.Show("Returns - Coming soon", "Returns", MessageBoxButtons.OK, MessageBoxIcon.Information)
    End Sub

    Private Sub ShowReports()
        MessageBox.Show("Reports - Coming soon", "Reports", MessageBoxButtons.OK, MessageBoxIcon.Information)
    End Sub

    Private Sub OpenCashDrawer()
        MessageBox.Show("Cash drawer opened!", "Cash Drawer", MessageBoxButtons.OK, MessageBoxIcon.Information)
    End Sub

    Private Sub ManagerFunctions()
        MessageBox.Show("Manager functions - Coming soon", "Manager", MessageBoxButtons.OK, MessageBoxIcon.Information)
    End Sub

    Private Sub ProcessPayment()
        If _cartItems.Rows.Count = 0 Then
            MessageBox.Show("Cart is empty!", "Payment Error", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            Return
        End If
        
        ' Calculate totals
        Dim subtotal As Decimal = 0
        For Each row As DataRow In _cartItems.Rows
            subtotal += Convert.ToDecimal(row("Total"))
        Next
        Dim tax As Decimal = subtotal * 0.15D
        Dim total As Decimal = subtotal + tax
        
        ' Simulate payment processing
        MessageBox.Show($"Payment: R{total:N2}", "Payment", MessageBoxButtons.OK, MessageBoxIcon.Information)
        
        ' Generate invoice number
        Dim invoiceNumber As String = "INV" & DateTime.Now.ToString("yyyyMMddHHmmss")
        
        ' Prepare receipt data
        Dim orderData As New Dictionary(Of String, String)
        orderData("InvoiceNumber") = invoiceNumber
        orderData("CustomerName") = "WALK-IN CUSTOMER"
        orderData("AccountNo") = ""
        orderData("Telephone") = ""
        orderData("CellNumber") = ""
        orderData("OrderDetails") = $"POS Sale    {invoiceNumber}    {DateTime.Now:yyyy/MM/dd}    {_cashierName}"
        orderData("InvoiceTotal") = total.ToString("N2")
        orderData("DepositPaid") = total.ToString("N2")
        orderData("BalanceOwing") = "0.00"
        
        ' Print receipt to continuous printer
        Try
            Dim printer As New POSReceiptPrinter()
            Dim cartDataTable As New DataTable()
            cartDataTable.Columns.Add("Product", GetType(String))
            cartDataTable.Columns.Add("Qty", GetType(Decimal))
            cartDataTable.Columns.Add("Price", GetType(Decimal))
            cartDataTable.Columns.Add("Total", GetType(Decimal))
            
            For Each row As DataRow In _cartItems.Rows
                cartDataTable.Rows.Add(row("Product"), row("Qty"), row("Price"), row("Total"))
            Next
            
            printer.PrintSaleReceipt(_branchID, invoiceNumber, cartDataTable, total, "Cash", _cashierName)
        Catch ex As Exception
            MessageBox.Show($"Print error: {ex.Message}", "Print Error", MessageBoxButtons.OK, MessageBoxIcon.Warning)
        End Try
        
        ' Clear cart and show idle screen
        _cartItems.Clear()
        ShowIdleScreen()
    End Sub

    ' Idle Screen Functions
    Private Sub SetupIdleScreen()
        ' Create beautiful branded idle overlay
        _idleOverlay = New Panel With {
            .Dock = DockStyle.Fill,
            .BackColor = ColorTranslator.FromHtml("#D2691E"),
            .Visible = False
        }

        ' Company logo/icon (large)
        Dim lblLogo As New Label With {
            .Text = "🍰",
            .Font = New Font("Segoe UI", 120, FontStyle.Bold),
            .ForeColor = Color.White,
            .TextAlign = ContentAlignment.MiddleCenter,
            .Size = New Size(Me.Width, 200),
            .Location = New Point(0, 200),
            .BackColor = Color.Transparent
        }

        ' Company name
        Dim lblCompany As New Label With {
            .Text = "OVEN DELIGHTS",
            .Font = New Font("Segoe UI", 72, FontStyle.Bold),
            .ForeColor = Color.White,
            .TextAlign = ContentAlignment.MiddleCenter,
            .Size = New Size(Me.Width, 100),
            .Location = New Point(0, 420),
            .BackColor = Color.Transparent
        }

        ' Tagline
        Dim lblTagline As New Label With {
            .Text = "Freshly Baked with Love",
            .Font = New Font("Segoe UI", 32, FontStyle.Italic),
            .ForeColor = ColorTranslator.FromHtml("#FFD700"),
            .TextAlign = ContentAlignment.MiddleCenter,
            .Size = New Size(Me.Width, 60),
            .Location = New Point(0, 540),
            .BackColor = Color.Transparent
        }

        ' Rotating messages with icons
        _lblRotatingMessage = New Label With {
            .Font = New Font("Segoe UI", 36, FontStyle.Bold),
            .ForeColor = ColorTranslator.FromHtml("#FFD700"),
            .TextAlign = ContentAlignment.MiddleCenter,
            .Size = New Size(Me.Width, 80),
            .Location = New Point(0, 640),
            .BackColor = Color.Transparent
        }

        ' Touch to continue
        Dim lblTouch As New Label With {
            .Text = "Touch screen to begin",
            .Font = New Font("Segoe UI", 28),
            .ForeColor = Color.White,
            .TextAlign = ContentAlignment.MiddleCenter,
            .Size = New Size(Me.Width, 50),
            .Location = New Point(0, 760),
            .BackColor = Color.Transparent
        }

        _idleOverlay.Controls.AddRange({lblLogo, lblCompany, lblTagline, _lblRotatingMessage, lblTouch})

        ' Add idle overlay AFTER all other controls are added
        ' This ensures it can properly cover/uncover the UI
        Me.Controls.Add(_idleOverlay)
        _idleOverlay.Visible = False ' Start hidden

        ' Click anywhere to dismiss
        AddHandler _idleOverlay.Click, Sub() DismissIdleScreen()
        AddHandler lblLogo.Click, Sub() DismissIdleScreen()
        AddHandler lblCompany.Click, Sub() DismissIdleScreen()
        AddHandler lblTagline.Click, Sub() DismissIdleScreen()
        AddHandler _lblRotatingMessage.Click, Sub() DismissIdleScreen()
        AddHandler lblTouch.Click, Sub() DismissIdleScreen()

        ' Setup message rotation timer
        _messageTimer = New Timer With {.Interval = 3000} ' Change every 3 seconds
        AddHandler _messageTimer.Tick, AddressOf RotateMessage
        UpdateRotatingMessage()

        ' Setup timer - show idle screen when cart is empty
        _idleTimer = New Timer With {.Interval = IDLE_TIMEOUT_MS}
        AddHandler _idleTimer.Tick, Sub()
                                        If _cartItems.Rows.Count = 0 Then
                                            ShowIdleScreen()
                                        Else
                                            ResetIdleTimer()
                                        End If
                                    End Sub
        _idleTimer.Start()

        ' Reset timer on any activity
        AddHandler Me.MouseMove, Sub() ResetIdleTimer()
        AddHandler Me.KeyDown, Sub() ResetIdleTimer()
        AddHandler Me.Click, Sub() ResetIdleTimer()
    End Sub

    Private Sub ShowIdleScreen()
        If _idleOverlay IsNot Nothing Then
            _idleOverlay.Visible = True
            _idleOverlay.BringToFront()
            _messageTimer?.Start()
            UpdateRotatingMessage()
        End If
    End Sub

    Private Sub DismissIdleScreen()
        If _idleOverlay IsNot Nothing Then
            _idleOverlay.Visible = False
            _idleOverlay.SendToBack()
            _messageTimer?.Stop()
            ResetIdleTimer()
            ' Show welcome message after dismissing idle screen
            If _cartItems.Rows.Count = 0 Then
                ShowWelcomeMessage()
            End If
        End If
    End Sub

    Private Sub RotateMessage(sender As Object, e As EventArgs)
        _currentMessageIndex = (_currentMessageIndex + 1) Mod 8
        UpdateRotatingMessage()
    End Sub

    Private Sub UpdateRotatingMessage()
        Dim messages() As String = {
            "🍞 Fresh Bread Daily",
            "🎂 Custom Cakes Available",
            "🥐 Artisan Pastries",
            "🍪 Homemade Cookies",
            "☕ Coffee & Treats",
            "🧁 Delicious Cupcakes",
            "🥧 Traditional Pies",
            "🍩 Gourmet Donuts"
        }

        If _lblRotatingMessage IsNot Nothing Then
            _lblRotatingMessage.Text = messages(_currentMessageIndex)
        End If
    End Sub

    Private Sub ResetIdleTimer()
        _idleTimer.Stop()
        _idleTimer.Start()
    End Sub

    Protected Overrides Sub OnFormClosing(e As FormClosingEventArgs)
        _idleTimer?.Stop()
        _idleTimer?.Dispose()
        _messageTimer?.Stop()
        _messageTimer?.Dispose()
        MyBase.OnFormClosing(e)
    End Sub

    Private Sub POSMainForm_Load(sender As Object, e As EventArgs)
        ' Initial layout adjustment
        AdjustLayout()
    End Sub
    
    Private Sub POSMainForm_Resize(sender As Object, e As EventArgs)
        ' Adjust layout when window is resized
        AdjustLayout()
    End Sub
    
    Private Sub AdjustLayout()
        If Me.WindowState = FormWindowState.Minimized Then Return
        
        Try
            Me.SuspendLayout()
            
            ' Responsive font sizes based on screen width
            Dim baseFontSize As Single = If(Me.Width > 1600, 12, If(Me.Width > 1200, 10, 9))
            
            ' Adjust category panel width (responsive)
            If pnlCategories IsNot Nothing AndAlso pnlCategories.Parent IsNot Nothing Then
                Dim categoryWidth = CInt(Me.Width * 0.15) ' 15% of screen width
                pnlCategories.Parent.Width = Math.Max(180, Math.Min(250, categoryWidth))
            End If
            
            ' Adjust cart panel width (responsive)
            If pnlCart IsNot Nothing Then
                Dim cartWidth = CInt(Me.Width * 0.25) ' 25% of screen width
                pnlCart.Width = Math.Max(300, Math.Min(450, cartWidth))
            End If
            
            ' Adjust product button sizes
            If flpProducts IsNot Nothing Then
                Dim buttonWidth = CInt((flpProducts.Width - 40) / 4) ' 4 columns with spacing
                buttonWidth = Math.Max(120, Math.Min(200, buttonWidth))
                
                For Each ctrl As Control In flpProducts.Controls
                    If TypeOf ctrl Is Button Then
                        ctrl.Size = New Size(buttonWidth, CInt(buttonWidth * 0.8))
                        ctrl.Font = New Font("Segoe UI", baseFontSize, FontStyle.Bold)
                    End If
                Next
            End If
            
            ' Adjust category button sizes
            If pnlCategories IsNot Nothing Then
                For Each ctrl As Control In pnlCategories.Controls
                    If TypeOf ctrl Is Button Then
                        ctrl.Width = pnlCategories.Width - 20
                        ctrl.Font = New Font("Segoe UI", baseFontSize, FontStyle.Bold)
                    End If
                Next
            End If
            
            ' Adjust DataGridView font
            If dgvCart IsNot Nothing Then
                dgvCart.Font = New Font("Segoe UI", baseFontSize)
                dgvCart.RowTemplate.Height = CInt(baseFontSize * 3)
            End If
            
            ' Adjust total labels
            If lblTotal IsNot Nothing Then
                lblTotal.Font = New Font("Segoe UI", baseFontSize + 6, FontStyle.Bold)
            End If
            If lblSubtotal IsNot Nothing Then
                lblSubtotal.Font = New Font("Segoe UI", baseFontSize + 2)
            End If
            If lblTax IsNot Nothing Then
                lblTax.Font = New Font("Segoe UI", baseFontSize + 2)
            End If
            
        Catch ex As Exception
            ' Ignore layout errors during resize
        Finally
            Me.ResumeLayout()
        End Try
    End Sub
    
    Private Sub BtnRefresh_Click(sender As Object, e As EventArgs)
        Try
            Me.Cursor = Cursors.WaitCursor
            
            ' Show refreshing message
            Dim originalText = DirectCast(sender, Button).Text
            DirectCast(sender, Button).Text = "⏳ Refreshing..."
            DirectCast(sender, Button).Enabled = False
            Application.DoEvents()
            
            ' Reload cache from database
            ProductCacheService.Instance.RefreshCache()
            
            ' Reload categories and products display
            LoadCategories()
            
            ' Reset button
            DirectCast(sender, Button).Text = originalText
            DirectCast(sender, Button).Enabled = True
            Me.Cursor = Cursors.Default
            
            MessageBox.Show($"Cache refreshed successfully!{vbCrLf}{ProductCacheService.Instance.ProductCount} products loaded.", "Refresh Complete", MessageBoxButtons.OK, MessageBoxIcon.Information)
            
        Catch ex As Exception
            Me.Cursor = Cursors.Default
            DirectCast(sender, Button).Text = "🔄 Refresh"
            DirectCast(sender, Button).Enabled = True
            MessageBox.Show($"Failed to refresh cache: {ex.Message}", "Refresh Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub
    
    Private Sub BtnDayEnd_Click(sender As Object, e As EventArgs)
        Try
            ' Confirm day-end action
            Dim confirmMsg = "Are you sure you want to complete Day End?" & vbCrLf & vbCrLf &
                           "This will:" & vbCrLf &
                           "1. Print day-end report to slip printer" & vbCrLf &
                           "2. Lock this till for today" & vbCrLf &
                           "3. You will NOT be able to log in again today" & vbCrLf & vbCrLf &
                           "Continue?"
            
            Dim result = MessageBox.Show(confirmMsg, "Day End Confirmation", MessageBoxButtons.YesNo, MessageBoxIcon.Question)
            
            If result <> DialogResult.Yes Then
                Return
            End If
            
            Me.Cursor = Cursors.WaitCursor
            
            ' Get today's sales totals from database
            Dim totalSales As Decimal = 0
            Dim totalCash As Decimal = 0
            Dim totalCard As Decimal = 0
            Dim totalAccount As Decimal = 0
            Dim totalRefunds As Decimal = 0
            
            Using conn As New SqlClient.SqlConnection(_connectionString)
                conn.Open()
                
                ' Get today's sales summary for this till
                Dim sql = "
                    SELECT 
                        ISNULL(SUM(TotalAmount), 0) AS TotalSales,
                        ISNULL(SUM(CASE WHEN PaymentMethod = 'Cash' THEN TotalAmount ELSE 0 END), 0) AS TotalCash,
                        ISNULL(SUM(CASE WHEN PaymentMethod = 'Card' THEN TotalAmount ELSE 0 END), 0) AS TotalCard,
                        ISNULL(SUM(CASE WHEN PaymentMethod = 'Account' THEN TotalAmount ELSE 0 END), 0) AS TotalAccount,
                        ISNULL(SUM(CASE WHEN TotalAmount < 0 THEN ABS(TotalAmount) ELSE 0 END), 0) AS TotalRefunds
                    FROM Sales
                    WHERE CAST(SaleDate AS DATE) = @Today
                    AND TillPointID = @TillPointID"
                
                Using cmd As New SqlClient.SqlCommand(sql, conn)
                    cmd.Parameters.AddWithValue("@Today", DateTime.Today)
                    cmd.Parameters.AddWithValue("@TillPointID", GetTillPointID())
                    
                    Using reader = cmd.ExecuteReader()
                        If reader.Read() Then
                            totalSales = CDec(reader("TotalSales"))
                            totalCash = CDec(reader("TotalCash"))
                            totalCard = CDec(reader("TotalCard"))
                            totalAccount = CDec(reader("TotalAccount"))
                            totalRefunds = CDec(reader("TotalRefunds"))
                        End If
                    End Using
                End Using
            End Using
            
            ' Prompt for actual cash count
            Dim actualCashInput = InputBox("Enter actual cash in drawer:", "Cash Count", totalCash.ToString("F2"))
            
            If String.IsNullOrWhiteSpace(actualCashInput) Then
                Me.Cursor = Cursors.Default
                MessageBox.Show("Day end cancelled - cash count required.", "Cancelled", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                Return
            End If
            
            Dim actualCash As Decimal
            If Not Decimal.TryParse(actualCashInput, actualCash) Then
                Me.Cursor = Cursors.Default
                MessageBox.Show("Invalid cash amount entered.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
                Return
            End If
            
            Dim cashVariance = actualCash - totalCash
            
            ' Optional notes
            Dim notes = InputBox("Enter any notes (optional):", "Day End Notes", "")
            
            ' Print day-end report to slip printer
            PrintDayEndReport(totalSales, totalCash, totalCard, totalAccount, totalRefunds, totalCash, actualCash, cashVariance, notes)
            
            ' Complete day-end in database
            Dim dayEndService As New DayEndService()
            dayEndService.CompleteDayEnd(
                GetTillPointID(),
                _cashierID,
                totalSales,
                totalCash,
                totalCard,
                totalAccount,
                totalRefunds,
                totalCash,
                actualCash,
                notes
            )
            
            Me.Cursor = Cursors.Default
            
            ' Show completion message
            Dim completionMsg = "Day End Completed Successfully!" & vbCrLf & vbCrLf &
                              $"Total Sales: R {totalSales:N2}" & vbCrLf &
                              $"Cash Variance: R {cashVariance:N2}" & vbCrLf & vbCrLf &
                              "Report printed to slip printer." & vbCrLf & vbCrLf &
                              "You cannot log in again today." & vbCrLf &
                              "Application will now close."
            
            MessageBox.Show(completionMsg, "Day End Complete", MessageBoxButtons.OK, MessageBoxIcon.Information)
            
            ' Close application
            Application.Exit()
            
        Catch ex As Exception
            Me.Cursor = Cursors.Default
            MessageBox.Show($"Day end failed: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub
    
    Private Sub PrintDayEndReport(totalSales As Decimal, totalCash As Decimal, totalCard As Decimal,
                                  totalAccount As Decimal, totalRefunds As Decimal,
                                  expectedCash As Decimal, actualCash As Decimal, cashVariance As Decimal,
                                  notes As String)
        Try
            ' Create print document for 80mm slip printer
            Dim printDoc As New Printing.PrintDocument()
            
            ' Use default printer
            ' printDoc.PrinterSettings.PrinterName = "Default Printer Name" ' Leave default
            
            AddHandler printDoc.PrintPage, Sub(sender, e)
                Dim font As New Font("Courier New", 9, FontStyle.Regular)
                Dim fontBold As New Font("Courier New", 9, FontStyle.Bold)
                Dim fontLarge As New Font("Courier New", 12, FontStyle.Bold)
                Dim y As Single = 10
                Dim lineHeight As Single = font.GetHeight(e.Graphics)
                
                ' Helper function to print centered text
                Dim PrintCentered = Sub(text As String, fnt As Font)
                    Dim textWidth = e.Graphics.MeasureString(text, fnt).Width
                    Dim x = (e.PageBounds.Width - textWidth) / 2
                    e.Graphics.DrawString(text, fnt, Brushes.Black, x, y)
                    y += fnt.GetHeight(e.Graphics)
                End Sub
                
                ' Helper function to print left-aligned text
                Dim PrintLeft = Sub(text As String, fnt As Font)
                    e.Graphics.DrawString(text, fnt, Brushes.Black, 10, y)
                    y += fnt.GetHeight(e.Graphics)
                End Sub
                
                ' Header
                PrintCentered("================================", font)
                PrintCentered("OVEN DELIGHTS", fontLarge)
                PrintCentered("DAY END REPORT", fontBold)
                PrintCentered("================================", font)
                y += lineHeight
                
                ' Till info
                PrintLeft($"Date: {DateTime.Today:dd/MM/yyyy}", font)
                PrintLeft($"Till: Till {GetTillPointID()}", font)
                PrintLeft($"Cashier: {_cashierName}", font)
                PrintLeft($"Time: {DateTime.Now:HH:mm:ss}", font)
                y += lineHeight
                
                ' Sales summary
                PrintCentered("SALES SUMMARY", fontBold)
                PrintLeft("--------------------------------", font)
                PrintLeft($"Total Sales:      R {totalSales,10:N2}", font)
                PrintLeft($"Cash Sales:       R {totalCash,10:N2}", font)
                PrintLeft($"Card Sales:       R {totalCard,10:N2}", font)
                PrintLeft($"Account Sales:    R {totalAccount,10:N2}", font)
                PrintLeft($"Refunds:          R {totalRefunds,10:N2}", font)
                y += lineHeight
                
                ' Cash drawer
                PrintCentered("CASH DRAWER", fontBold)
                PrintLeft("--------------------------------", font)
                PrintLeft($"Expected Cash:    R {expectedCash,10:N2}", font)
                PrintLeft($"Actual Cash:      R {actualCash,10:N2}", font)
                PrintLeft($"Variance:         R {cashVariance,10:N2}", fontBold)
                y += lineHeight
                
                ' Notes
                If Not String.IsNullOrWhiteSpace(notes) Then
                    PrintLeft("Notes:", fontBold)
                    PrintLeft(notes, font)
                    y += lineHeight
                End If
                
                ' Footer
                PrintCentered("================================", font)
                PrintCentered("Day End Complete", font)
                PrintCentered($"{DateTime.Now:dd/MM/yyyy HH:mm:ss}", font)
                PrintCentered("================================", font)
            End Sub
            
            ' Print
            printDoc.Print()
            
        Catch ex As Exception
            Throw New Exception("Failed to print day-end report: " & ex.Message, ex)
        End Try
    End Sub
    
    Private Function GetTillPointID() As Integer
        Try
            Using conn As New SqlClient.SqlConnection(_connectionString)
                conn.Open()
                Dim sql = "SELECT TillPointID FROM TillPoints WHERE MachineName = @MachineName AND IsActive = 1"
                Using cmd As New SqlClient.SqlCommand(sql, conn)
                    cmd.Parameters.AddWithValue("@MachineName", Environment.MachineName)
                    Dim result = cmd.ExecuteScalar()
                    If result IsNot Nothing AndAlso Not IsDBNull(result) Then
                        Return CInt(result)
                    End If
                End Using
            End Using
        Catch ex As Exception
            Debug.WriteLine($"Error getting TillPointID: {ex.Message}")
        End Try
        Return 0
    End Function
End Class
