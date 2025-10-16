Imports System.Configuration
Imports System.Data.SqlClient
Imports System.Drawing
Imports System.Windows.Forms

Public Class POSMainForm_REDESIGN
    Inherits Form

    ' Core properties
    Private _cashierID As Integer
    Private _cashierName As String
    Private _branchID As Integer
    Private _tillPointID As Integer
    Private _connectionString As String
    Private _cartItems As New DataTable()
    Private _allProducts As New DataTable() ' Cache all products

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
    Private Const IDLE_TIMEOUT_MS As Integer = 5000

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

    Public Sub New(cashierID As Integer, cashierName As String, branchID As Integer, tillPointID As Integer)
        MyBase.New()

        _cashierID = cashierID
        _cashierName = cashierName
        _branchID = branchID
        _tillPointID = tillPointID
        _connectionString = ConfigurationManager.ConnectionStrings("OvenDelightsERPConnectionString").ConnectionString

        Me.KeyPreview = True
        SetupModernUI()
        InitializeCart()
        SetupIdleScreen()

        ' Handle resize for different screen sizes
        AddHandler Me.Resize, Sub() RepositionControls()
    End Sub

    Private Sub SetupModernUI()
        Me.Text = "Oven Delights POS"
        Me.WindowState = FormWindowState.Maximized
        Me.BackColor = _lightGray
        Me.FormBorderStyle = FormBorderStyle.None
        Me.StartPosition = FormStartPosition.Manual
        Me.Bounds = Screen.PrimaryScreen.Bounds

        ' TOP BAR - Modern gradient effect
        pnlTop = New Panel With {
            .Dock = DockStyle.Top,
            .Height = 70,
            .BackColor = _darkBlue
        }

        Dim lblTitle As New Label With {
            .Text = "🍰 OVEN DELIGHTS POS",
            .Font = New Font("Segoe UI", 18, FontStyle.Bold),
            .ForeColor = Color.White,
            .Location = New Point(20, 20),
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

        Dim btnCashUp As New Button With {
            .Text = "💰 CASH UP",
            .Font = New Font("Segoe UI", 10, FontStyle.Bold),
            .Size = New Size(120, 40),
            .BackColor = _green,
            .ForeColor = Color.White,
            .FlatStyle = FlatStyle.Flat,
            .Cursor = Cursors.Hand,
            .Anchor = AnchorStyles.Top Or AnchorStyles.Right
        }
        btnCashUp.Location = New Point(pnlTop.Width - 260, 15)
        btnCashUp.FlatAppearance.BorderSize = 0
        AddHandler btnCashUp.Click, AddressOf PerformCashUp

        Dim btnLogout As New Button With {
            .Text = "🚪 EXIT",
            .Font = New Font("Segoe UI", 10, FontStyle.Bold),
            .Size = New Size(110, 40),
            .BackColor = _red,
            .ForeColor = Color.White,
            .FlatStyle = FlatStyle.Flat,
            .Cursor = Cursors.Hand,
            .Anchor = AnchorStyles.Top Or AnchorStyles.Right
        }
        btnLogout.Location = New Point(pnlTop.Width - 130, 15)
        btnLogout.FlatAppearance.BorderSize = 0
        AddHandler btnLogout.Click, Sub() Me.Close()

        Dim lblCashier As New Label With {
            .Text = $"👤 {_cashierName} | Till: {GetTillNumber()}",
            .Font = New Font("Segoe UI", 11, FontStyle.Bold),
            .ForeColor = Color.White,
            .AutoSize = False,
            .TextAlign = ContentAlignment.MiddleCenter,
            .Width = 300,
            .Height = 40,
            .Anchor = AnchorStyles.Top
        }
        lblCashier.Location = New Point((pnlTop.Width \ 2) - 150, 15)

        pnlTop.Controls.AddRange({lblTitle, txtBarcodeScanner, lblCashier, btnCashUp, btnLogout})

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
                                        End If
                                    End Sub

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

        ' Add all panels to form
        Me.Controls.AddRange({pnlProducts, pnlCart, pnlCategoriesContainer, pnlShortcuts, pnlTop})
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

        ' Configure columns after binding
        AddHandler dgvCart.DataBindingComplete, AddressOf ConfigureCartColumns
    End Sub

    Private Sub ConfigureCartColumns(sender As Object, e As DataGridViewBindingCompleteEventArgs)
        ' Only configure once
        RemoveHandler dgvCart.DataBindingComplete, AddressOf ConfigureCartColumns

        If dgvCart.Columns.Count > 0 Then
            If dgvCart.Columns.Contains("ProductID") Then dgvCart.Columns("ProductID").Visible = False
            If dgvCart.Columns.Contains("ItemCode") Then
                dgvCart.Columns("ItemCode").Width = 70
                dgvCart.Columns("ItemCode").HeaderText = "Code"
            End If
            If dgvCart.Columns.Contains("Product") Then
                dgvCart.Columns("Product").HeaderText = "Item"
            End If
            If dgvCart.Columns.Contains("Qty") Then
                dgvCart.Columns("Qty").Width = 50
                dgvCart.Columns("Qty").ReadOnly = False
            End If
            If dgvCart.Columns.Contains("Price") Then
                dgvCart.Columns("Price").DefaultCellStyle.Format = "C2"
                dgvCart.Columns("Price").Width = 80
            End If
            If dgvCart.Columns.Contains("Total") Then
                dgvCart.Columns("Total").DefaultCellStyle.Format = "C2"
                dgvCart.Columns("Total").Width = 90
            End If
        End If
    End Sub

    Protected Overrides Sub OnLoad(e As EventArgs)
        MyBase.OnLoad(e)

        Try
            ' Show loading screen
            Dim loadingScreen As New LoadingScreen()
            loadingScreen.Show()
            Application.DoEvents()

            ' Load in stages with progress updates
            loadingScreen.UpdateStatus("Setting up interface...")
            loadingScreen.SetProgress(20)
            RepositionControls()

            loadingScreen.UpdateStatus("Loading categories...")
            loadingScreen.SetProgress(40)
            LoadCategories()

            loadingScreen.UpdateStatus("Loading products...")
            loadingScreen.SetProgress(60)
            LoadAllProductsToCache()

            loadingScreen.UpdateStatus("Finalizing...")
            loadingScreen.SetProgress(80)
            
            ' Force layout update before showing idle screen
            Me.PerformLayout()
            Application.DoEvents()
            
            ShowIdleScreen()

            loadingScreen.SetProgress(100)
            Threading.Thread.Sleep(300) ' Brief pause to show 100%

            ' Close loading screen
            loadingScreen.Close()
            loadingScreen.Dispose()
        Catch ex As Exception
            MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    Private Sub LoadAllProductsFromDatabase()
        ' Reload products from database to refresh stock levels
        LoadAllProductsToCache()
        ' Reload current view (thread-safe)
        If Me.InvokeRequired Then
            Me.Invoke(Sub() LoadProducts())
        Else
            LoadProducts()
        End If
    End Sub

    Private Sub LoadAllProductsToCache()
        Try
            _allProducts.Clear()
            _allProducts.Columns.Clear()
            _allProducts.Columns.Add("ProductID", GetType(Integer))
            _allProducts.Columns.Add("ItemCode", GetType(String))
            _allProducts.Columns.Add("ProductName", GetType(String))
            _allProducts.Columns.Add("SellingPrice", GetType(Decimal))
            _allProducts.Columns.Add("QtyOnHand", GetType(Decimal))
            _allProducts.Columns.Add("ReorderLevel", GetType(Decimal))
            _allProducts.Columns.Add("Category", GetType(String))

            ' Query the view - simple and fast!
            Dim sql = "
                SELECT 
                    ProductID,
                    ItemCode,
                    ProductName,
                    ISNULL(SellingPrice, 0) AS SellingPrice,
                    ISNULL(QtyOnHand, 0) AS QtyOnHand,
                    ISNULL(ReorderLevel, 5) AS ReorderLevel,
                    Category
                FROM vw_POS_Products
                WHERE BranchID = @BranchID
                ORDER BY ProductName"

            Using conn As New SqlConnection(_connectionString)
                conn.Open()
                Using cmd As New SqlCommand(sql, conn)
                    cmd.Parameters.AddWithValue("@BranchID", _branchID)
                    Using adapter As New SqlDataAdapter(cmd)
                        adapter.Fill(_allProducts)
                    End Using
                End Using
            End Using
        Catch ex As Exception
            MessageBox.Show($"Error caching products: {ex.Message}{vbCrLf}{vbCrLf}Make sure to run the SQL script: Create_POS_ProductView.sql", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    Private Sub RepositionControls()
        ' Reposition cashier label to stay centered
        For Each ctrl As Control In pnlTop.Controls
            If TypeOf ctrl Is Label AndAlso ctrl.Text.Contains("Till:") Then
                ctrl.Left = (pnlTop.Width \ 2) - (ctrl.Width \ 2)
                Exit For
            End If
        Next

        ' Reposition idle screen if visible
        If _idleOverlay IsNot Nothing AndAlso _idleOverlay.Visible Then
            ShowIdleScreen()
        End If
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
            Dim sql = "SELECT CategoryID, CategoryCode, CategoryName FROM ProductCategories WHERE IsActive = 1 ORDER BY CategoryName"

            Using conn As New SqlConnection(_connectionString)
                conn.Open()
                Using cmd As New SqlCommand(sql, conn)
                    Using reader = cmd.ExecuteReader()
                        Dim colorIndex = 0
                        Dim colors() As Color = {_lightBlue, _green, _orange, _purple, _red, _yellow}

                        While reader.Read()
                            Dim categoryCode = reader.GetString(1)
                            Dim categoryName = reader.GetString(2)
                            Dim icon = GetCategoryIcon(categoryCode)
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

    Private Sub LoadProducts(Optional category As String = Nothing)
        flpProducts.SuspendLayout()
        flpProducts.Controls.Clear()

        Try
            ' DEBUG: Show what we have
            Dim totalProducts = _allProducts.Rows.Count

            ' Filter cached products in memory - INSTANT!
            Dim filteredRows As DataRow()

            If String.IsNullOrEmpty(category) Then
                filteredRows = _allProducts.Select()
            Else
                ' Try exact match first, then LIKE
                filteredRows = _allProducts.Select($"Category = '{category}'")
                If filteredRows.Length = 0 Then
                    ' Try LIKE if exact match fails
                    filteredRows = _allProducts.Select($"Category LIKE '%{category}%'")
                End If
            End If

            Dim productCount = 0
            For Each row As DataRow In filteredRows
                Dim price = If(IsDBNull(row("SellingPrice")), 0D, CDec(row("SellingPrice")))
                Dim stock = If(IsDBNull(row("QtyOnHand")), 0D, CDec(row("QtyOnHand")))
                Dim reorderLevel = If(IsDBNull(row("ReorderLevel")), 0D, CDec(row("ReorderLevel")))
                ' Show all products
                Dim card = CreateProductCard(
                    CInt(row("ProductID")),
                    row("ItemCode").ToString(),
                    row("ProductName").ToString(),
                    price,
                    stock,
                    reorderLevel
                )
                flpProducts.Controls.Add(card)
                productCount += 1
            Next

            If productCount = 0 Then
                ' DEBUG: Show what categories exist
                Dim uniqueCategories As New HashSet(Of String)
                For Each row As DataRow In _allProducts.Rows
                    If Not IsDBNull(row("Category")) Then
                        uniqueCategories.Add(row("Category").ToString())
                    End If
                Next

                Dim lblNoProducts As New Label With {
                    .Text = $"No products in '{category}'{vbCrLf}Total cached: {totalProducts}{vbCrLf}Categories: {String.Join(", ", uniqueCategories)}",
                    .Font = New Font("Segoe UI", 12),
                    .ForeColor = _darkGray,
                    .AutoSize = True,
                    .Location = New Point(50, 100)
                }
                flpProducts.Controls.Add(lblNoProducts)
            End If
        Catch ex As Exception
            MessageBox.Show($"Error loading products: {ex.Message}{vbCrLf}{ex.StackTrace}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        Finally
            flpProducts.ResumeLayout()
        End Try
    End Sub

    Private Function CreateProductCard(productID As Integer, itemCode As String, productName As String, price As Decimal, stock As Decimal, reorderLevel As Decimal) As Panel
        ' Determine if stock is low (at or below reorder level)
        Dim isLowStock = stock <= reorderLevel

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

        ' Stock label - RED if at or below reorder level
        Dim stockColor = If(isLowStock, Color.Red, _darkGray)
        Dim stockText = If(isLowStock, $"Stock: {stock} ⚠ LOW!", $"Stock: {stock}")

        Dim lblStock As New Label With {
            .Text = stockText,
            .Font = New Font("Segoe UI", 8, If(isLowStock, FontStyle.Bold, FontStyle.Regular)),
            .ForeColor = stockColor,
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
        Dim screenWidth = Screen.PrimaryScreen.Bounds.Width
        Dim availableWidth = screenWidth - 140
        Dim spacing = 5
        Dim buttonWidth = (availableWidth \ visibleCount) - spacing

        For i = 0 To visibleCount - 1
            Dim shortcut = shortcuts(i)
            Dim btn As New Button With {
                .Text = $"{shortcut.Item1}{vbCrLf}{shortcut.Item2}",
                .Size = New Size(buttonWidth, 70),
                .Location = New Point(10 + (i * (buttonWidth + spacing)), 10),
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
            .Size = New Size(120, 70),
            .Location = New Point(Screen.PrimaryScreen.Bounds.Width - 130, 10),
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
        ' Step 1: Supervisor Authorization
        Dim supervisorUsername = InputBox("Enter Retail Supervisor Username:", "Returns Authorization")
        If String.IsNullOrWhiteSpace(supervisorUsername) Then Return

        Dim supervisorPassword As String = ""
        Using pwdForm As New PasswordInputForm("Enter Retail Supervisor Password:", "Returns Authorization")
            If pwdForm.ShowDialog() <> DialogResult.OK Then Return
            supervisorPassword = pwdForm.Password
        End Using

        If String.IsNullOrWhiteSpace(supervisorPassword) Then Return

        ' Validate supervisor credentials
        Dim supervisorID As Integer = 0
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
                    supervisorID = CInt(result)
                End Using
            End Using
        Catch ex As Exception
            MessageBox.Show($"Authorization error: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
            Return
        End Try

        ' Step 2: Get invoice number
        Using invoiceForm As New InvoiceNumberEntryForm()
            If invoiceForm.ShowDialog() <> DialogResult.OK Then Return

            Dim digits = invoiceForm.InvoiceDigits
            If String.IsNullOrWhiteSpace(digits) Then Return

            ' Generate full invoice number - Format: INV-BranchCode-TILL-TillNumber-000001
            ' Example: INV-PH-TILL-01-000003
            Dim branchPrefix = GetBranchPrefix()
            Dim tillNumber = GetTillNumber()
            Dim fullInvoiceNumber = $"INV-{branchPrefix}-TILL-{tillNumber}-{digits.PadLeft(6, "0"c)}"

            ' Verify invoice exists
            Try
                Using conn As New SqlConnection(_connectionString)
                    conn.Open()
                    Dim sql = "SELECT COUNT(*) FROM Demo_Sales WHERE InvoiceNumber = @InvoiceNumber"
                    Using cmd As New SqlCommand(sql, conn)
                        cmd.Parameters.AddWithValue("@InvoiceNumber", fullInvoiceNumber)
                        Dim count = CInt(cmd.ExecuteScalar())
                        If count = 0 Then
                            MessageBox.Show($"Invoice {fullInvoiceNumber} not found!", "Invalid Invoice", MessageBoxButtons.OK, MessageBoxIcon.Error)
                            Return
                        End If
                    End Using
                End Using
            Catch ex As Exception
                MessageBox.Show($"Error validating invoice: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
                Return
            End Try

            ' Step 3: Show return line items form
            Using returnForm As New ReturnLineItemsForm(fullInvoiceNumber, _branchID, _tillPointID, _cashierID, supervisorID)
                returnForm.ShowDialog()
            End Using
        End Using
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
        CalculateTotals()
        Dim subtotal As Decimal = 0
        For Each row As DataRow In _cartItems.Rows
            subtotal += CDec(row("Total"))
        Next
        Dim tax = subtotal * 0.15D
        Dim total = subtotal + tax

        ' Get branch prefix
        Dim branchPrefix = GetBranchPrefix()

        ' Show payment tender form
        Using paymentForm As New PaymentTenderForm(_cashierID, _branchID, _tillPointID, branchPrefix, _cartItems, subtotal, tax, total)
            If paymentForm.ShowDialog() = DialogResult.OK Then
                ' Clear cart and show idle screen
                _cartItems.Clear()
                CalculateTotals()
                ShowIdleScreen()
            End If
        End Using
    End Sub

    Private Sub UpdateCachedStock()
        ' Update stock quantities in the cached DataTable - INSTANT!
        For Each cartRow As DataRow In _cartItems.Rows
            Dim productID = CInt(cartRow("ProductID"))
            Dim qtySold = CDec(cartRow("Qty"))

            ' Find and update in cache
            Dim cachedRows = _allProducts.Select($"ProductID = {productID}")
            If cachedRows.Length > 0 Then
                Dim currentStock = CDec(cachedRows(0)("QtyOnHand"))
                cachedRows(0)("QtyOnHand") = currentStock - qtySold
            End If
        Next
    End Sub

    Private Function GetBranchPrefix() As String
        Try
            Dim sql = "SELECT BranchCode FROM Branches WHERE BranchID = @BranchID"
            Using conn As New SqlConnection(_connectionString)
                conn.Open()
                Using cmd As New SqlCommand(sql, conn)
                    cmd.Parameters.AddWithValue("@BranchID", _branchID)
                    Dim result = cmd.ExecuteScalar()
                    Return If(result IsNot Nothing, result.ToString(), "POS")
                End Using
            End Using
        Catch
            Return "POS"
        End Try
    End Function

    Private Sub SetupIdleScreen()
        _idleOverlay = New Panel With {
            .Dock = DockStyle.Fill,
            .BackColor = Color.FromArgb(180, 139, 69, 19),
            .Visible = False
        }
        
        ' Set background image
        Try
            Dim imagePath = System.IO.Path.Combine(Application.StartupPath, "idleScreen.png")
            If System.IO.File.Exists(imagePath) Then
                _idleOverlay.BackgroundImage = Image.FromFile(imagePath)
                _idleOverlay.BackgroundImageLayout = ImageLayout.Stretch
            End If
        Catch ex As Exception
            ' If image fails to load, keep the solid color background
        End Try

        Dim lblLogo As New Label With {
            .Text = "🍰",
            .Font = New Font("Segoe UI", 120, FontStyle.Bold),
            .ForeColor = ColorTranslator.FromHtml("#C84B31"),
            .TextAlign = ContentAlignment.MiddleCenter,
            .AutoSize = False,
            .Dock = DockStyle.None,
            .Height = 200,
            .BackColor = Color.Transparent
        }

        Dim lblCompany As New Label With {
            .Text = "OVEN DELIGHTS",
            .Font = New Font("Segoe UI", 72, FontStyle.Bold),
            .ForeColor = ColorTranslator.FromHtml("#D9534F"),
            .TextAlign = ContentAlignment.MiddleCenter,
            .AutoSize = False,
            .Dock = DockStyle.None,
            .Height = 100,
            .BackColor = Color.Transparent
        }

        Dim lblTagline As New Label With {
            .Text = "Freshly Baked with Love",
            .Font = New Font("Segoe UI", 32, FontStyle.Italic),
            .ForeColor = ColorTranslator.FromHtml("#E67E22"),
            .TextAlign = ContentAlignment.MiddleCenter,
            .AutoSize = False,
            .Dock = DockStyle.None,
            .Height = 60,
            .BackColor = Color.Transparent
        }

        _lblRotatingMessage = New Label With {
            .Font = New Font("Segoe UI", 42, FontStyle.Bold),
            .ForeColor = Color.White,
            .TextAlign = ContentAlignment.MiddleCenter,
            .AutoSize = False,
            .Dock = DockStyle.None,
            .Height = 80,
            .BackColor = Color.Transparent
        }

        Dim lblTouch As New Label With {
            .Text = "Touch screen to begin",
            .Font = New Font("Segoe UI", 32, FontStyle.Bold),
            .ForeColor = Color.White,
            .TextAlign = ContentAlignment.MiddleCenter,
            .AutoSize = False,
            .Dock = DockStyle.None,
            .Height = 50,
            .BackColor = Color.Transparent
        }

        _idleOverlay.Controls.AddRange({lblLogo, lblCompany, lblTagline, _lblRotatingMessage, lblTouch})
        Me.Controls.Add(_idleOverlay)

        AddHandler _idleOverlay.Click, Sub() DismissIdleScreen()
        AddHandler lblLogo.Click, Sub() DismissIdleScreen()
        AddHandler lblCompany.Click, Sub() DismissIdleScreen()
        AddHandler lblTagline.Click, Sub() DismissIdleScreen()
        AddHandler _lblRotatingMessage.Click, Sub() DismissIdleScreen()
        AddHandler lblTouch.Click, Sub() DismissIdleScreen()

        _messageTimer = New Timer With {.Interval = 3000}
        AddHandler _messageTimer.Tick, AddressOf RotateMessage
        UpdateRotatingMessage()
    End Sub

    Private Sub ShowIdleScreen()
        If _idleOverlay IsNot Nothing Then
            ' Get actual overlay dimensions
            Dim overlayWidth = _idleOverlay.Width
            Dim overlayHeight = _idleOverlay.Height
            Dim centerY = overlayHeight \ 2

            Dim labelIndex = 0
            For Each ctrl As Control In _idleOverlay.Controls
                If TypeOf ctrl Is Label Then
                    Dim lbl = CType(ctrl, Label)
                    lbl.Width = overlayWidth
                    lbl.Left = 0
                    lbl.TextAlign = ContentAlignment.MiddleCenter

                    ' Position based on order added (0=logo, 1=company, 2=tagline, 3=rotating, 4=touch)
                    Select Case labelIndex
                        Case 0 ' Logo
                            lbl.Top = centerY - 200
                        Case 1 ' OVEN DELIGHTS
                            lbl.Top = centerY - 20
                        Case 2 ' Tagline
                            lbl.Top = centerY + 90
                        Case 3 ' Rotating message
                            lbl.Top = centerY + 160
                        Case 4 ' Touch screen
                            lbl.Top = centerY + 250
                    End Select
                    
                    labelIndex += 1
                End If
            Next

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

    Protected Overrides Sub OnFormClosing(e As FormClosingEventArgs)
        _messageTimer?.Stop()
        _messageTimer?.Dispose()
        MyBase.OnFormClosing(e)
    End Sub
    
    Private Function GetTillNumber() As String
        Try
            Using conn As New SqlConnection(_connectionString)
                conn.Open()
                Dim sql = "SELECT TillNumber FROM TillPoints WHERE TillPointID = @TillPointID"
                Using cmd As New SqlCommand(sql, conn)
                    cmd.Parameters.AddWithValue("@TillPointID", _tillPointID)
                    Dim result = cmd.ExecuteScalar()
                    If result IsNot Nothing Then
                        Dim fullTillNumber = result.ToString()
                        ' Extract just the number part (e.g., "PH-TILL-01" becomes "01")
                        If fullTillNumber.Contains("-") Then
                            Dim parts = fullTillNumber.Split("-"c)
                            Return parts(parts.Length - 1) ' Get last part
                        Else
                            Return fullTillNumber
                        End If
                    Else
                        Return "01"
                    End If
                End Using
            End Using
        Catch
            Return "01"
        End Try
    End Function
    
    Private Sub PerformCashUp()
        ' Request Retail Supervisor username
        Dim supervisorUsername = InputBox("Enter Retail Supervisor Username:", "Cash Up Authorization")
        
        If String.IsNullOrWhiteSpace(supervisorUsername) Then
            Return
        End If
        
        ' Request Retail Supervisor password using secure form
        Dim supervisorPassword As String = ""
        Using pwdForm As New PasswordInputForm("Enter Retail Supervisor Password:", "Cash Up Authorization")
            If pwdForm.ShowDialog() <> DialogResult.OK Then
                Return
            End If
            supervisorPassword = pwdForm.Password
        End Using
        
        If String.IsNullOrWhiteSpace(supervisorPassword) Then
            Return
        End If
        
        ' Validate Retail Supervisor credentials
        Try
            Using conn As New SqlConnection(_connectionString)
                conn.Open()
                
                Dim sql = "SELECT COUNT(*) FROM Users u INNER JOIN Roles r ON u.RoleID = r.RoleID WHERE u.Username = @Username AND u.Password = @Password AND r.RoleName = 'Retail Supervisor' AND u.IsActive = 1"
                Using cmd As New SqlCommand(sql, conn)
                    cmd.Parameters.AddWithValue("@Username", supervisorUsername)
                    cmd.Parameters.AddWithValue("@Password", supervisorPassword)
                    
                    If CInt(cmd.ExecuteScalar()) = 0 Then
                        MessageBox.Show("Invalid Retail Supervisor credentials!", "Authorization Failed", MessageBoxButtons.OK, MessageBoxIcon.Error)
                        Return
                    End If
                End Using
                
                ' Get cash up data
                Dim cashUpData = GetCashUpData()
                
                ' Show cash up report (modal dialog)
                ShowCashUpReport(cashUpData)
            End Using
        Catch ex As Exception
            MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub
    
    Private Function GetCashUpData() As DataTable
        Dim dt As New DataTable()
        
        Try
            Using conn As New SqlConnection(_connectionString)
                conn.Open()
                
                
                ' Get sales and returns for this cashier and till
                Dim sql = "
                    SELECT 
                        (SELECT COUNT(*) FROM Demo_Sales WHERE CashierID = @CashierID AND CAST(SaleDate AS DATE) = CAST(GETDATE() AS DATE)) AS TotalTransactions,
                        ISNULL((SELECT SUM(Subtotal) FROM Demo_Sales WHERE CashierID = @CashierID AND CAST(SaleDate AS DATE) = CAST(GETDATE() AS DATE)), 0) AS TotalSubtotal,
                        ISNULL((SELECT SUM(TaxAmount) FROM Demo_Sales WHERE CashierID = @CashierID AND CAST(SaleDate AS DATE) = CAST(GETDATE() AS DATE)), 0) AS TotalTax,
                        ISNULL((SELECT SUM(TotalAmount) FROM Demo_Sales WHERE CashierID = @CashierID AND CAST(SaleDate AS DATE) = CAST(GETDATE() AS DATE)), 0) - 
                        ISNULL((SELECT SUM(TotalAmount) FROM Demo_Returns WHERE CashierID = @CashierID AND CAST(ReturnDate AS DATE) = CAST(GETDATE() AS DATE)), 0) AS TotalSales,
                        (SELECT MIN(SaleDate) FROM Demo_Sales WHERE CashierID = @CashierID AND CAST(SaleDate AS DATE) = CAST(GETDATE() AS DATE)) AS FirstSale,
                        (SELECT MAX(SaleDate) FROM Demo_Sales WHERE CashierID = @CashierID AND CAST(SaleDate AS DATE) = CAST(GETDATE() AS DATE)) AS LastSale,
                        ISNULL((SELECT SUM(CashAmount) FROM Demo_Sales WHERE CashierID = @CashierID AND CAST(SaleDate AS DATE) = CAST(GETDATE() AS DATE)), 0) - 
                        ISNULL((SELECT SUM(TotalAmount) FROM Demo_Returns WHERE CashierID = @CashierID AND CAST(ReturnDate AS DATE) = CAST(GETDATE() AS DATE)), 0) AS TotalCash,
                        ISNULL((SELECT SUM(CardAmount) FROM Demo_Sales WHERE CashierID = @CashierID AND CAST(SaleDate AS DATE) = CAST(GETDATE() AS DATE)), 0) AS TotalCard,
                        ISNULL((SELECT COUNT(*) FROM Demo_Returns WHERE CashierID = @CashierID AND CAST(ReturnDate AS DATE) = CAST(GETDATE() AS DATE)), 0) AS TotalReturns,
                        ISNULL((SELECT SUM(TotalAmount) FROM Demo_Returns WHERE CashierID = @CashierID AND CAST(ReturnDate AS DATE) = CAST(GETDATE() AS DATE)), 0) AS TotalReturnAmount"
                
                Using cmd As New SqlCommand(sql, conn)
                    cmd.Parameters.AddWithValue("@CashierID", _cashierID)
                    
                    Using adapter As New SqlDataAdapter(cmd)
                        adapter.Fill(dt)
                    End Using
                End Using
            End Using
        Catch ex As Exception
            MessageBox.Show($"Error getting cash up data: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
        
        Return dt
    End Function
    
    Private Sub ShowCashUpReport(data As DataTable)
        If data.Rows.Count = 0 Then
            MessageBox.Show("No sales data found for today!", "Cash Up", MessageBoxButtons.OK, MessageBoxIcon.Information)
            Return
        End If
        
        Dim row = data.Rows(0)
        Dim transactions = If(IsDBNull(row("TotalTransactions")), 0, CInt(row("TotalTransactions")))
        Dim subtotal = If(IsDBNull(row("TotalSubtotal")), 0D, CDec(row("TotalSubtotal")))
        Dim tax = If(IsDBNull(row("TotalTax")), 0D, CDec(row("TotalTax")))
        Dim total = If(IsDBNull(row("TotalSales")), 0D, CDec(row("TotalSales")))
        Dim totalCash = If(IsDBNull(row("TotalCash")), 0D, CDec(row("TotalCash")))
        Dim totalCard = If(IsDBNull(row("TotalCard")), 0D, CDec(row("TotalCard")))
        Dim totalReturns = If(IsDBNull(row("TotalReturns")), 0, CInt(row("TotalReturns")))
        Dim totalReturnAmount = If(IsDBNull(row("TotalReturnAmount")), 0D, CDec(row("TotalReturnAmount")))
        Dim firstSale = If(IsDBNull(row("FirstSale")), DateTime.Now, CDate(row("FirstSale")))
        Dim lastSale = If(IsDBNull(row("LastSale")), DateTime.Now, CDate(row("LastSale")))
        
        ' Create cash up form
        Dim cashUpForm As New Form With {
            .Text = "Cash Up Report",
            .Size = New Size(600, 700),
            .StartPosition = FormStartPosition.CenterScreen,
            .BackColor = Color.White,
            .FormBorderStyle = FormBorderStyle.FixedDialog,
            .MaximizeBox = False,
            .MinimizeBox = False
        }
        
        ' Header
        Dim pnlHeader As New Panel With {
            .Dock = DockStyle.Top,
            .Height = 80,
            .BackColor = _green
        }
        
        Dim lblHeader As New Label With {
            .Text = "💰 CASH UP REPORT",
            .Font = New Font("Segoe UI", 24, FontStyle.Bold),
            .ForeColor = Color.White,
            .TextAlign = ContentAlignment.MiddleCenter,
            .Dock = DockStyle.Fill
        }
        pnlHeader.Controls.Add(lblHeader)
        
        ' Report content
        Dim pnlContent As New Panel With {
            .Location = New Point(50, 100),
            .Size = New Size(500, 500),
            .BackColor = Color.White
        }
        
        Dim yPos = 20
        
        ' Cashier info
        Dim lblCashier As New Label With {
            .Text = $"Cashier: {_cashierName}",
            .Font = New Font("Segoe UI", 14, FontStyle.Bold),
            .Location = New Point(20, yPos),
            .AutoSize = True
        }
        pnlContent.Controls.Add(lblCashier)
        yPos += 35
        
        ' Till info
        Dim lblTill As New Label With {
            .Text = $"Till Point: {GetTillNumber()}",
            .Font = New Font("Segoe UI", 12),
            .Location = New Point(20, yPos),
            .AutoSize = True
        }
        pnlContent.Controls.Add(lblTill)
        yPos += 30
        
        ' Date
        Dim lblDate As New Label With {
            .Text = $"Date: {DateTime.Now:dd/MM/yyyy}",
            .Font = New Font("Segoe UI", 12),
            .Location = New Point(20, yPos),
            .AutoSize = True
        }
        pnlContent.Controls.Add(lblDate)
        yPos += 30
        
        ' Time range
        Dim lblTime As New Label With {
            .Text = $"Time: {firstSale:HH:mm} - {lastSale:HH:mm}",
            .Font = New Font("Segoe UI", 12),
            .Location = New Point(20, yPos),
            .AutoSize = True
        }
        pnlContent.Controls.Add(lblTime)
        yPos += 50
        
        ' Separator
        Dim lblSep1 As New Label With {
            .Text = "═══════════════════════════════════",
            .Font = New Font("Courier New", 12),
            .Location = New Point(20, yPos),
            .AutoSize = True
        }
        pnlContent.Controls.Add(lblSep1)
        yPos += 30
        
        ' Transactions
        Dim lblTransactions As New Label With {
            .Text = $"Total Transactions: {transactions}",
            .Font = New Font("Segoe UI", 13, FontStyle.Bold),
            .Location = New Point(20, yPos),
            .AutoSize = True
        }
        pnlContent.Controls.Add(lblTransactions)
        yPos += 40
        
        ' Subtotal
        Dim lblSubtotal As New Label With {
            .Text = $"Subtotal: {subtotal.ToString("C2")}",
            .Font = New Font("Segoe UI", 12),
            .Location = New Point(20, yPos),
            .AutoSize = True
        }
        pnlContent.Controls.Add(lblSubtotal)
        yPos += 30
        
        ' Tax
        Dim lblTax As New Label With {
            .Text = $"VAT (15%): {tax.ToString("C2")}",
            .Font = New Font("Segoe UI", 12),
            .Location = New Point(20, yPos),
            .AutoSize = True
        }
        pnlContent.Controls.Add(lblTax)
        yPos += 40
        
        ' Returns section
        If totalReturns > 0 Then
            Dim lblReturnsHeader As New Label With {
                .Text = "RETURNS:",
                .Font = New Font("Segoe UI", 12, FontStyle.Bold),
                .ForeColor = Color.Red,
                .Location = New Point(20, yPos),
                .AutoSize = True
            }
            pnlContent.Controls.Add(lblReturnsHeader)
            yPos += 30
            
            Dim lblReturnsCount As New Label With {
                .Text = $"Total Returns: {totalReturns}",
                .Font = New Font("Segoe UI", 11),
                .ForeColor = Color.Red,
                .Location = New Point(40, yPos),
                .AutoSize = True
            }
            pnlContent.Controls.Add(lblReturnsCount)
            yPos += 25
            
            Dim lblReturnsAmount As New Label With {
                .Text = $"Return Amount: -{totalReturnAmount.ToString("C2")}",
                .Font = New Font("Segoe UI", 11),
                .ForeColor = Color.Red,
                .Location = New Point(40, yPos),
                .AutoSize = True
            }
            pnlContent.Controls.Add(lblReturnsAmount)
            yPos += 35
        End If
        
        ' Separator
        Dim lblSep2 As New Label With {
            .Text = "═══════════════════════════════════",
            .Font = New Font("Courier New", 12),
            .Location = New Point(20, yPos),
            .AutoSize = True
        }
        pnlContent.Controls.Add(lblSep2)
        yPos += 30
        
        ' Total
        Dim lblTotal As New Label With {
            .Text = $"TOTAL SALES: {total.ToString("C2")}",
            .Font = New Font("Segoe UI", 16, FontStyle.Bold),
            .ForeColor = _green,
            .Location = New Point(20, yPos),
            .AutoSize = True
        }
        pnlContent.Controls.Add(lblTotal)
        
        ' Button panel
        Dim pnlButtons As New Panel With {
            .Dock = DockStyle.Bottom,
            .Height = 80,
            .BackColor = Color.White
        }
        
        ' Close button
        Dim btnClose As New Button With {
            .Text = "CLOSE",
            .Font = New Font("Segoe UI", 12, FontStyle.Bold),
            .Size = New Size(150, 50),
            .Location = New Point(150, 15),
            .BackColor = _darkGray,
            .ForeColor = Color.White,
            .FlatStyle = FlatStyle.Flat,
            .Cursor = Cursors.Hand
        }
        btnClose.FlatAppearance.BorderSize = 0
        AddHandler btnClose.Click, Sub() cashUpForm.Close()
        
        ' Logout button
        Dim btnLogout As New Button With {
            .Text = "LOGOUT",
            .Font = New Font("Segoe UI", 12, FontStyle.Bold),
            .Size = New Size(150, 50),
            .Location = New Point(310, 15),
            .BackColor = _red,
            .ForeColor = Color.White,
            .FlatStyle = FlatStyle.Flat,
            .Cursor = Cursors.Hand
        }
        btnLogout.FlatAppearance.BorderSize = 0
        AddHandler btnLogout.Click, Sub()
            cashUpForm.Close()
            Me.DialogResult = DialogResult.OK
            Me.Close()
        End Sub
        
        pnlButtons.Controls.AddRange({btnClose, btnLogout})
        
        cashUpForm.Controls.AddRange({pnlHeader, pnlContent, pnlButtons})
        cashUpForm.ShowDialog()
    End Sub
End Class
