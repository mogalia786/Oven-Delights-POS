Imports System.Configuration
Imports System.Data.SqlClient
Imports System.Drawing
Imports System.Windows.Forms

Public Class POSMainForm_REDESIGN
    Inherits Form

    ' Core properties
    Private _connectionString As String
    Private _cashierID As Integer
    Private _cashierName As String
    Private _branchID As Integer
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

    Public Sub New(cashierID As Integer, cashierName As String, branchID As Integer)
        MyBase.New()

        _cashierID = cashierID
        _cashierName = cashierName
        _branchID = branchID
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
            .AutoSize = True,
            .Anchor = AnchorStyles.Top Or AnchorStyles.Right
        }
        lblCashier.Location = New Point(Screen.PrimaryScreen.Bounds.Width - 250, 25)

        Dim btnLogout As New Button With {
            .Text = "🚪 EXIT",
            .Font = New Font("Segoe UI", 11, FontStyle.Bold),
            .Size = New Size(120, 40),
            .BackColor = _red,
            .ForeColor = Color.White,
            .FlatStyle = FlatStyle.Flat,
            .Cursor = Cursors.Hand,
            .Anchor = AnchorStyles.Top Or AnchorStyles.Right
        }
        btnLogout.Location = New Point(Screen.PrimaryScreen.Bounds.Width - 130, 15)
        btnLogout.FlatAppearance.BorderSize = 0
        AddHandler btnLogout.Click, Sub() Me.Close()

        pnlTop.Controls.AddRange({lblTitle, txtBarcodeScanner, lblCashier, btnLogout})

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
            ' Reposition controls now that form is loaded
            RepositionControls()
            LoadCategories()
            LoadAllProductsToCache() ' Load products once
            ShowIdleScreen()
        Catch ex As Exception
            MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
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
            _allProducts.Columns.Add("Category", GetType(String))

            ' SIMPLE: Query the view that has everything mapped correctly
            Dim sql = "
                SELECT 
                    ProductID,
                    ItemCode,
                    ProductName,
                    ISNULL(SellingPrice, 0) AS SellingPrice,
                    ISNULL(QtyOnHand, 0) AS QtyOnHand,
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
        ' Fix top bar controls
        For Each ctrl As Control In pnlTop.Controls
            If TypeOf ctrl Is Label AndAlso ctrl.Text.Contains("👤") Then
                ctrl.Location = New Point(Me.Width - 250, 25)
            ElseIf TypeOf ctrl Is Button AndAlso ctrl.Text.Contains("EXIT") Then
                ctrl.Location = New Point(Me.Width - 130, 15)
            End If
        Next

        ' Fix idle screen labels to use actual form size
        If _idleOverlay IsNot Nothing Then
            For Each ctrl As Control In _idleOverlay.Controls
                If TypeOf ctrl Is Label Then
                    Dim lbl = CType(ctrl, Label)
                    lbl.Width = Me.Width

                    ' Reposition vertically centered
                    If lbl.Text.Contains("🍰") Then
                        lbl.Location = New Point(0, (Me.Height \ 2) - 300)
                    ElseIf lbl.Text = "OVEN DELIGHTS" Then
                        lbl.Location = New Point(0, (Me.Height \ 2) - 80)
                    ElseIf lbl.Text.Contains("Freshly Baked") Then
                        lbl.Location = New Point(0, (Me.Height \ 2) + 40)
                    ElseIf lbl.Text.Contains("🍞") Or lbl.Text.Contains("🎂") Or lbl.Text.Contains("🥐") Then
                        lbl.Location = New Point(0, (Me.Height \ 2) + 120)
                    ElseIf lbl.Text.Contains("Touch screen") Then
                        lbl.Location = New Point(0, (Me.Height \ 2) + 220)
                    End If
                End If
            Next
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
                ' Show all products
                Dim card = CreateProductCard(
                    CInt(row("ProductID")),
                    row("ItemCode").ToString(),
                    row("ProductName").ToString(),
                    price,
                    If(IsDBNull(row("QtyOnHand")), 0D, CDec(row("QtyOnHand")))
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
        MessageBox.Show("Payment processing - To be implemented", "Payment", MessageBoxButtons.OK, MessageBoxIcon.Information)
        ShowIdleScreen()
    End Sub

    Private Sub SetupIdleScreen()
        _idleOverlay = New Panel With {
            .Dock = DockStyle.Fill,
            .BackColor = ColorTranslator.FromHtml("#D2691E"),
            .Visible = False
        }

        Dim lblLogo As New Label With {
            .Text = "🍰",
            .Font = New Font("Segoe UI", 120, FontStyle.Bold),
            .ForeColor = Color.White,
            .TextAlign = ContentAlignment.MiddleCenter,
            .AutoSize = False,
            .Dock = DockStyle.None,
            .Anchor = AnchorStyles.None,
            .BackColor = Color.Transparent
        }

        Dim lblCompany As New Label With {
            .Text = "OVEN DELIGHTS",
            .Font = New Font("Segoe UI", 72, FontStyle.Bold),
            .ForeColor = Color.White,
            .TextAlign = ContentAlignment.MiddleCenter,
            .AutoSize = False,
            .Dock = DockStyle.None,
            .Anchor = AnchorStyles.None,
            .BackColor = Color.Transparent
        }

        Dim lblTagline As New Label With {
            .Text = "Freshly Baked with Love",
            .Font = New Font("Segoe UI", 32, FontStyle.Italic),
            .ForeColor = ColorTranslator.FromHtml("#FFD700"),
            .TextAlign = ContentAlignment.MiddleCenter,
            .AutoSize = False,
            .Dock = DockStyle.None,
            .Anchor = AnchorStyles.None,
            .BackColor = Color.Transparent
        }

        _lblRotatingMessage = New Label With {
            .Font = New Font("Segoe UI", 36, FontStyle.Bold),
            .ForeColor = ColorTranslator.FromHtml("#FFD700"),
            .TextAlign = ContentAlignment.MiddleCenter,
            .AutoSize = False,
            .Dock = DockStyle.None,
            .Anchor = AnchorStyles.None,
            .BackColor = Color.Transparent
        }

        Dim lblTouch As New Label With {
            .Text = "Touch screen to begin",
            .Font = New Font("Segoe UI", 28),
            .ForeColor = Color.White,
            .TextAlign = ContentAlignment.MiddleCenter,
            .AutoSize = False,
            .Dock = DockStyle.None,
            .Anchor = AnchorStyles.None,
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
            ' Reposition idle screen content before showing
            Dim screenWidth = Me.ClientSize.Width
            Dim screenHeight = Me.ClientSize.Height

            For Each ctrl As Control In _idleOverlay.Controls
                If TypeOf ctrl Is Label Then
                    Dim lbl = CType(ctrl, Label)
                    lbl.Width = screenWidth
                    lbl.Height = 100

                    ' Center vertically based on actual form size
                    If lbl.Text.Contains("🍰") Then
                        lbl.Height = 200
                        lbl.Location = New Point(0, (screenHeight \ 2) - 300)
                    ElseIf lbl.Text = "OVEN DELIGHTS" Then
                        lbl.Location = New Point(0, (screenHeight \ 2) - 80)
                    ElseIf lbl.Text.Contains("Freshly Baked") Then
                        lbl.Height = 60
                        lbl.Location = New Point(0, (screenHeight \ 2) + 40)
                    ElseIf lbl.Text.Contains("🍞") Or lbl.Text.Contains("🎂") Or lbl.Text.Contains("🥐") Or lbl.Text.Contains("🧁") Or lbl.Text.Contains("☕") Or lbl.Text.Contains("🍪") Then
                        lbl.Height = 80
                        lbl.Location = New Point(0, (screenHeight \ 2) + 120)
                    ElseIf lbl.Text.Contains("Touch screen") Then
                        lbl.Height = 50
                        lbl.Location = New Point(0, (screenHeight \ 2) + 220)
                    End If
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
End Class
