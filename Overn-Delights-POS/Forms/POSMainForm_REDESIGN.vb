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
    Private pnlNumpad As Panel
    Private pnlSearchBar As Panel
    Private flpProducts As FlowLayoutPanel
    Private dgvCart As DataGridView
    Private lblTotal As Label
    Private lblSubtotal As Label
    Private lblTax As Label
    Private txtSearch As TextBox
    Private txtSearchByName As TextBox
    Private txtBarcodeScanner As TextBox
    Private btnRefresh As Button
    Private btnModifyQty As Button
    Private _onScreenKeyboard As OnScreenKeyboard
    
    ' Category Navigation State
    Private _currentView As String = "categories"  ' categories | subcategories | products
    Private _currentCategoryId As Integer = 0
    Private _currentCategoryName As String = ""
    Private _currentSubCategoryId As Integer = 0
    Private _currentSubCategoryName As String = ""
    Private _categoryService As New CategoryNavigationService()
    Private lblBreadcrumb As Label
    
    ' Search debouncing
    Private _searchTimer As Timer
    Private _pendingSearchText As String = ""

    ' Idle screen
    Private _idleTimer As Timer
    Private _idleOverlay As Panel
    Private _messageTimer As Timer
    Private _currentMessageIndex As Integer = 0
    Private _lblRotatingMessage As Label
    Private Const IDLE_TIMEOUT_MS As Integer = 300000 ' 300 seconds = 5 minutes

    ' Tile sizing - responsive to screen
    Private _tileWidth As Integer = 100
    Private _tileHeight As Integer = 70
    Private _tilesPerRow As Integer = 6
    
    ' Iron Man Theme Color Palette (from pos_styles.css)
    Private _ironRed As Color = ColorTranslator.FromHtml("#C1272D")
    Private _ironRedDark As Color = ColorTranslator.FromHtml("#8B0000")     ' Dark red gradient
    Private _ironGold As Color = ColorTranslator.FromHtml("#FFD700")
    Private _ironGoldDark As Color = ColorTranslator.FromHtml("#DAA520")    ' Goldenrod
    Private _ironDark As Color = ColorTranslator.FromHtml("#0a0e27")
    Private _ironBlue As Color = ColorTranslator.FromHtml("#00D4FF")        ' Cyan blue
    Private _ironBlueDark As Color = ColorTranslator.FromHtml("#0099CC")    ' Dark cyan
    Private _ironDarkBlue As Color = ColorTranslator.FromHtml("#1a1f3a")
    Private _ironSilver As Color = ColorTranslator.FromHtml("#C0C0C0")
    Private _ironGlow As Color = ColorTranslator.FromHtml("#00F5FF")        ' Bright glow
    
    ' Legacy colors (keep for compatibility)
    Private _darkBlue As Color = ColorTranslator.FromHtml("#2C3E50")
    Private _lightBlue As Color = ColorTranslator.FromHtml("#3498DB")
    Private _green As Color = ColorTranslator.FromHtml("#27AE60")
    Private _orange As Color = ColorTranslator.FromHtml("#E67E22")
    Private _red As Color = ColorTranslator.FromHtml("#E74C3C")
    Private _purple As Color = ColorTranslator.FromHtml("#9B59B6")
    Private _yellow As Color = ColorTranslator.FromHtml("#F39C12")
    Private _lightGray As Color = ColorTranslator.FromHtml("#ECF0F1")
    Private _darkGray As Color = ColorTranslator.FromHtml("#7F8C8D")

    ' Screen scaling properties
    Private _screenWidth As Integer
    Private _screenHeight As Integer
    Private _scaleFactor As Single = 1.0F
    Private _baseWidth As Integer = 1920 ' Base design width
    Private _baseHeight As Integer = 1080 ' Base design height

    Public Sub New(cashierID As Integer, cashierName As String, branchID As Integer, tillPointID As Integer)
        MyBase.New()

        _cashierID = cashierID
        _cashierName = cashierName
        _branchID = branchID
        _tillPointID = tillPointID
        _connectionString = ConfigurationManager.ConnectionStrings("OvenDelightsERPConnectionString").ConnectionString

        Me.KeyPreview = True

        SetupModernUI()

        ' Initialize screen dimensions and scaling AFTER form is positioned
        InitializeScreenScaling()

        InitializeCart()
        SetupIdleScreen()
        InitializeSearchTimer()
        InitializeIdleTimer()

        ' Handle resize for different screen sizes
        AddHandler Me.Resize, Sub() HandleFormResize()
        AddHandler Me.Shown, AddressOf OnFormShown
    End Sub

    Private Sub OnFormShown(sender As Object, e As EventArgs)
        ' Properly maximize to screen using native Windows behavior
        Try
            ' Simply maximize - let Windows handle it
            Me.WindowState = FormWindowState.Maximized

            ' Force the scaling system to recalculate based on actual screen
            InitializeScreenScaling()
            HandleFormResize()
            
            ' Focus barcode scanner for immediate scanning
            If txtBarcodeScanner IsNot Nothing Then
                txtBarcodeScanner.Focus()
            End If
        Catch ex As Exception
            MessageBox.Show($"Screen detection error: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    Private Sub InitializeSearchTimer()
        _searchTimer = New Timer()
        _searchTimer.Interval = 300 ' 300ms delay
        AddHandler _searchTimer.Tick, Sub()
                                          _searchTimer.Stop()
                                          SearchProducts(_pendingSearchText)
                                      End Sub
    End Sub

    Private Sub DebouncedSearch(searchText As String)
        _pendingSearchText = searchText
        _searchTimer.Stop()
        _searchTimer.Start()
    End Sub

    Private Sub InitializeIdleTimer()
        _idleTimer = New Timer()
        _idleTimer.Interval = IDLE_TIMEOUT_MS
        AddHandler _idleTimer.Tick, Sub()
                                        _idleTimer.Stop()
                                        ShowIdleScreen()
                                    End Sub
    End Sub

    Private Sub ResetIdleTimer()
        If _idleTimer IsNot Nothing Then
            _idleTimer.Stop()
            _idleTimer.Start()
        End If
    End Sub

    Private Sub SetupModernUI()
        Me.Text = "Oven Delights POS"
        Me.BackColor = _ironDark
        Me.FormBorderStyle = FormBorderStyle.None
        Me.StartPosition = FormStartPosition.WindowsDefaultLocation

        ' Don't set WindowState here - let OnFormShown handle it
        ' This prevents conflicts with screen detection

        ' TOP BAR - Iron Man Red gradient - Reduced height for 1024x768
        pnlTop = New Panel With {
            .Dock = DockStyle.Top,
            .Height = 60,
            .BackColor = _ironRed
        }

        ' Get branch name
        Dim branchName As String = GetBranchName()

        Dim lblTitle As New Label With {
            .Text = "🍰 OVEN DELIGHTS POS",
            .Font = New Font("Segoe UI", 14, FontStyle.Bold),
            .ForeColor = _ironGold,
            .Location = New Point(20, 10),
            .AutoSize = True
        }

        Dim lblBranch As New Label With {
            .Text = branchName.ToUpper(),
            .Font = New Font("Segoe UI", 20, FontStyle.Bold),
            .ForeColor = Color.White,
            .Location = New Point(20, 35),
            .AutoSize = True
        }

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
            .Text = "🚪 LOGOUT",
            .Font = New Font("Segoe UI", 10, FontStyle.Bold),
            .Size = New Size(110, 40),
            .BackColor = Color.FromArgb(220, 53, 69),
            .ForeColor = Color.White,
            .FlatStyle = FlatStyle.Flat,
            .Cursor = Cursors.Hand,
            .Anchor = AnchorStyles.Top Or AnchorStyles.Right
        }
        btnLogout.Location = New Point(pnlTop.Width - 130, 15)
        btnLogout.FlatAppearance.BorderSize = 0
        AddHandler btnLogout.Click, AddressOf PerformLogout

        Dim lblCashier As New Label With {
            .Text = $"👤 {_cashierName} | Till: {GetTillNumber()}",
            .Font = New Font("Segoe UI", 16, FontStyle.Bold),
            .ForeColor = Color.White,
            .AutoSize = True,
            .TextAlign = ContentAlignment.MiddleCenter,
            .Anchor = AnchorStyles.Top,
            .BackColor = Color.Transparent
        }

        pnlTop.Controls.AddRange({lblTitle, lblBranch, lblCashier, btnCashUp, btnLogout})

        ' Center the cashier label after adding to panel
        lblCashier.Location = New Point((pnlTop.Width - lblCashier.Width) / 2, 20)
        lblCashier.BringToFront()
        lblBranch.BringToFront()

        ' LEFT PANEL - HIDDEN (using category tiles in main area instead)
        ' Categories will now show as tiles in the main product panel
        Dim pnlCategoriesContainer As New Panel With {
            .Dock = DockStyle.Left,
            .Width = 0,
            .Visible = False
        }

        ' CENTER PANEL - Products with vibrant futuristic theme
        pnlProducts = New Panel With {
            .Dock = DockStyle.Fill,
            .BackColor = Color.FromArgb(15, 20, 35),
            .Padding = New Padding(10)
        }

        ' BREADCRUMB - Category navigation (CSS: gold with glow)
        lblBreadcrumb = New Label With {
            .Text = "Categories",
            .Font = New Font("Segoe UI", 22, FontStyle.Bold),
            .ForeColor = _ironGold,
            .Dock = DockStyle.Top,
            .Height = 50,
            .Padding = New Padding(15, 10, 0, 0),
            .BackColor = Color.FromArgb(25, 255, 215, 0),
            .Cursor = Cursors.Hand
        }
        AddHandler lblBreadcrumb.Click, AddressOf Breadcrumb_Click

        ' Search panel with barcode scanner button
        pnlSearchBar = New Panel With {
            .Dock = DockStyle.Top,
            .Height = 60,
            .BackColor = Color.White,
            .Padding = New Padding(10, 8, 10, 8)
        }

        ' Barcode scan button
        Dim btnScan As New Button With {
            .Text = "📷 SCAN",
            .Font = New Font("Segoe UI", 12, FontStyle.Bold),
            .Size = New Size(100, 44),
            .Location = New Point(10, 8),
            .BackColor = _lightBlue,
            .ForeColor = Color.White,
            .FlatStyle = FlatStyle.Flat,
            .Cursor = Cursors.Hand
        }
        btnScan.FlatAppearance.BorderSize = 0
        AddHandler btnScan.Click, Sub()
                                      txtSearch.Clear()
                                      txtSearch.Focus()
                                  End Sub

        ' Search textbox (accepts keyboard and touch input)
        txtSearch = New TextBox With {
            .Font = New Font("Segoe UI", 12),
            .Location = New Point(120, 8),
            .Width = 300,
            .Height = 44,
            .Text = "🔍 Search by code...",
            .ForeColor = _darkGray,
            .ReadOnly = False,
            .Cursor = Cursors.IBeam
        }

        AddHandler txtSearch.Enter, Sub()
                                        If txtSearch.Text.Contains("Search by code") Then
                                            txtSearch.Text = ""
                                            txtSearch.ForeColor = Color.Black
                                        End If
                                    End Sub

        AddHandler txtSearch.KeyDown, Sub(sender, e)
                                          If txtSearch.Text.Contains("Search by code") Then
                                              txtSearch.Text = ""
                                              txtSearch.ForeColor = Color.Black
                                          ElseIf e.KeyCode = Keys.Enter Then
                                              e.SuppressKeyPress = True
                                              Dim searchCode = txtSearch.Text.Trim()
                                              If Not String.IsNullOrWhiteSpace(searchCode) Then
                                                  ProcessBarcodeScan(searchCode)
                                                  txtSearch.Clear()
                                                  txtSearch.Focus()
                                              End If
                                          End If
                                      End Sub

        AddHandler txtSearch.TextChanged, Sub()
                                              If Not txtSearch.Text.Contains("Search by code") AndAlso txtSearch.Text.Length >= 2 Then
                                                  DebouncedSearch(txtSearch.Text)
                                              End If
                                          End Sub

        ' Search by Name textbox
        txtSearchByName = New TextBox With {
            .Font = New Font("Segoe UI", 12),
            .Location = New Point(430, 8),
            .Width = 300,
            .Height = 44,
            .Text = "⌨️ Search by name...",
            .ForeColor = _darkGray,
            .ReadOnly = False,
            .Cursor = Cursors.IBeam
        }

        AddHandler txtSearchByName.Enter, Sub()
                                              If txtSearchByName.Text.Contains("Search by name") Then
                                                  txtSearchByName.Text = ""
                                                  txtSearchByName.ForeColor = Color.Black
                                              End If
                                          End Sub

        AddHandler txtSearchByName.KeyDown, Sub(sender, e)
                                                If txtSearchByName.Text.Contains("Search by name") Then
                                                    txtSearchByName.Text = ""
                                                    txtSearchByName.ForeColor = Color.Black
                                                End If
                                            End Sub

        AddHandler txtSearchByName.TextChanged, Sub()
                                                    If Not txtSearchByName.Text.Contains("Search by name") AndAlso txtSearchByName.Text.Length >= 2 Then
                                                        DebouncedSearch(txtSearchByName.Text)
                                                    End If
                                                End Sub

        ' Refresh Products button - sized in RepositionControls
        btnRefresh = New Button With {
            .Text = "🔄",
            .Font = New Font("Segoe UI", 10, FontStyle.Bold),
            .BackColor = _green,
            .ForeColor = Color.White,
            .FlatStyle = FlatStyle.Flat,
            .Cursor = Cursors.Hand
        }
        btnRefresh.FlatAppearance.BorderSize = 0
        AddHandler btnRefresh.Click, Sub() RefreshProductsCache()

        ' xQty button - modify cart line item quantities - sized in RepositionControls
        btnModifyQty = New Button With {
            .Text = "✕ QTY",
            .Font = New Font("Segoe UI", 10, FontStyle.Bold),
            .BackColor = _orange,
            .ForeColor = Color.White,
            .FlatStyle = FlatStyle.Flat,
            .Cursor = Cursors.Hand
        }
        btnModifyQty.FlatAppearance.BorderSize = 0
        AddHandler btnModifyQty.Click, Sub() ShowCartQuantityEditor()

        ' Hidden barcode scanner input
        txtBarcodeScanner = New TextBox With {
            .Location = New Point(-100, -100),
            .Size = New Size(1, 1)
        }
        AddHandler txtBarcodeScanner.KeyDown, AddressOf BarcodeScanner_KeyDown

        pnlSearchBar.Controls.AddRange({btnScan, txtSearch, txtSearchByName, btnRefresh, btnModifyQty, txtBarcodeScanner})

        flpProducts = New FlowLayoutPanel With {
            .Dock = DockStyle.Fill,
            .AutoScroll = True,
            .FlowDirection = FlowDirection.LeftToRight,
            .WrapContents = True,
            .Padding = New Padding(10),
            .BackColor = Color.FromArgb(15, 20, 35)
        }

        pnlProducts.Controls.AddRange({flpProducts, lblBreadcrumb, pnlSearchBar})

        ' RIGHT PANEL - Cart - Much wider, almost to categories
        pnlCart = New Panel With {
            .Dock = DockStyle.Right,
            .Width = 450,
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
            .Height = 200,
            .BackColor = _darkBlue,
            .Padding = New Padding(15)
        }

        lblSubtotal = New Label With {
            .Text = "Subtotal: R 0.00",
            .Font = New Font("Segoe UI", 13, FontStyle.Bold),
            .ForeColor = Color.White,
            .Location = New Point(20, 10),
            .AutoSize = True
        }

        lblTax = New Label With {
            .Text = "VAT (15%): R 0.00",
            .Font = New Font("Segoe UI", 13, FontStyle.Bold),
            .ForeColor = Color.White,
            .Location = New Point(20, 38),
            .AutoSize = True
        }

        lblTotal = New Label With {
            .Text = "R 0.00",
            .Font = New Font("Segoe UI", 42, FontStyle.Bold),
            .ForeColor = _yellow,
            .Location = New Point(20, 68),
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
            .Cursor = Cursors.Hand,
            .Name = "btnPayNow"
        }
        btnPay.FlatAppearance.BorderSize = 0
        AddHandler btnPay.Click, Sub() ProcessPayment()

        pnlTotals.Controls.AddRange({lblSubtotal, lblTax, lblTotal, btnPay})
        pnlCart.Controls.AddRange({dgvCart, pnlTotals, lblCartHeader})

        ' BOTTOM PANEL - F-Key Shortcuts - Reduced height for 1024x768
        pnlShortcuts = New Panel With {
            .Dock = DockStyle.Bottom,
            .Height = 80,
            .BackColor = Color.White,
            .Padding = New Padding(5)
        }

        CreateShortcutButtons()

        ' Setup on-screen keyboard (linked to txtSearchByName)
        _onScreenKeyboard = New OnScreenKeyboard(txtSearchByName)
        AddHandler _onScreenKeyboard.TextChanged, AddressOf OnScreenKeyboard_TextChanged

        ' Add all panels to form
        Me.Controls.AddRange({pnlProducts, pnlCart, pnlCategoriesContainer, pnlShortcuts, pnlTop, _onScreenKeyboard})
    End Sub

    Private Sub OnScreenKeyboard_TextChanged(sender As Object, text As String)
        ' Filter products by name with wildcard on both sides (CACHED - fast!)
        Debug.WriteLine($"[KEYBOARD EVENT] Text: '{text}' | Cache size: {_allProducts.Rows.Count}")

        ' Ignore placeholder text
        If text = "⌨️ Touch to search by name (F4)..." OrElse text = "⌨️ Touch to search by name..." Then
            Return
        End If

        ' Require at least 2 characters to search
        If text.Length < 2 Then
            flpProducts.Controls.Clear()
            Dim lblPrompt As New Label With {
                .Text = "Type at least 2 characters to search...",
                .Font = New Font("Segoe UI", 14, FontStyle.Italic),
                .ForeColor = Color.Gray,
                .AutoSize = True,
                .Padding = New Padding(20)
            }
            flpProducts.Controls.Add(lblPrompt)
            Return
        End If

        ' Search with 2+ characters
        FilterProductsByName(text)
    End Sub

    Private Sub FilterProductsByName(searchText As String)
        If String.IsNullOrWhiteSpace(searchText) Then
            ' Don't load all products - show message instead
            flpProducts.Controls.Clear()
            Dim lblPrompt As New Label With {
                .Text = "Type to search products by name...",
                .Font = New Font("Segoe UI", 14, FontStyle.Italic),
                .ForeColor = Color.Gray,
                .AutoSize = True,
                .Padding = New Padding(20)
            }
            flpProducts.Controls.Add(lblPrompt)
            Return
        End If

        Try
            Dim startTime = DateTime.Now
            Debug.WriteLine($"[CACHED NAME SEARCH] Searching: '{searchText}' in {_allProducts.Rows.Count} products")

            flpProducts.SuspendLayout()
            flpProducts.Controls.Clear()

            ' Filter cached products by ProductName - INSTANT!
            Dim allMatches = _allProducts.AsEnumerable().
                Where(Function(row)
                          Dim productName = row("ProductName").ToString()
                          Return productName.IndexOf(searchText, StringComparison.OrdinalIgnoreCase) >= 0
                      End Function).
                OrderBy(Function(row)
                            Dim productName = row("ProductName").ToString()
                            ' Sort by: 1) Starts with search (best), 2) Contains search, 3) Alphabetical
                            If productName.StartsWith(searchText, StringComparison.OrdinalIgnoreCase) Then
                                Return 0 ' Highest priority
                            Else
                                Return 1 ' Lower priority
                            End If
                        End Function).
                ThenBy(Function(row) row("ProductName").ToString()).
                ToList()

            Dim totalMatches = allMatches.Count
            Dim filteredRows = allMatches.Take(50).ToList()

            If totalMatches = 0 Then
                Dim lblNoResults As New Label With {
                    .Text = $"No products found matching '{searchText}'",
                    .Font = New Font("Segoe UI", 14, FontStyle.Italic),
                    .ForeColor = Color.Gray,
                    .AutoSize = True,
                    .Padding = New Padding(20)
                }
                flpProducts.Controls.Add(lblNoResults)
                flpProducts.ResumeLayout()
                Return
            End If

            ' Show message if results are limited
            If totalMatches > 50 Then
                Dim lblMoreResults As New Label With {
                    .Text = $"Showing 50 of {totalMatches} results. Type more characters to narrow search.",
                    .Font = New Font("Segoe UI", 11, FontStyle.Bold),
                    .ForeColor = _orange,
                    .AutoSize = True,
                    .Padding = New Padding(10),
                    .BackColor = Color.LightYellow
                }
                flpProducts.Controls.Add(lblMoreResults)
            End If

            ' Display filtered products (max 50)
            For Each row As DataRow In filteredRows
                Dim productID = CInt(row("ProductID"))
                Dim itemCode = row("ItemCode").ToString()
                Dim productName = row("ProductName").ToString()
                Dim price = If(IsDBNull(row("SellingPrice")), 0D, CDec(row("SellingPrice")))
                Dim stock = If(IsDBNull(row("QtyOnHand")), 0D, CDec(row("QtyOnHand")))
                Dim reorderLevel = If(IsDBNull(row("ReorderLevel")), 0D, CDec(row("ReorderLevel")))

                Dim card = CreateProductCard(productID, itemCode, productName, price, stock, reorderLevel)
                flpProducts.Controls.Add(card)
            Next

            flpProducts.ResumeLayout()

            Dim elapsed = (DateTime.Now - startTime).TotalMilliseconds
            Debug.WriteLine($"[CACHED NAME SEARCH] Found {filteredRows.Count} products in {elapsed:F0}ms")

        Catch ex As Exception
            flpProducts.ResumeLayout()
            MessageBox.Show($"Error filtering products: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
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
                dgvCart.Columns("ItemCode").ReadOnly = True
            End If
            If dgvCart.Columns.Contains("Product") Then
                dgvCart.Columns("Product").HeaderText = "Item"
                dgvCart.Columns("Product").ReadOnly = True
            End If
            If dgvCart.Columns.Contains("Qty") Then
                dgvCart.Columns("Qty").Width = 50
                dgvCart.Columns("Qty").ReadOnly = True
            End If
            If dgvCart.Columns.Contains("Price") Then
                dgvCart.Columns("Price").DefaultCellStyle.Format = "C2"
                dgvCart.Columns("Price").Width = 80
                dgvCart.Columns("Price").ReadOnly = True
            End If
            If dgvCart.Columns.Contains("Total") Then
                dgvCart.Columns("Total").DefaultCellStyle.Format = "C2"
                dgvCart.Columns("Total").Width = 90
                dgvCart.Columns("Total").ReadOnly = True
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
            ' ShowCategories() will be called after idle screen

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

    Private Sub RefreshProductsCache()
        ' Show confirmation dialog
        Dim result = MessageBox.Show(
            "Refresh product data from database?" & vbCrLf & vbCrLf &
            "This will update prices and stock levels." & vbCrLf &
            "Takes 2-5 seconds depending on product count.",
            "Refresh Products",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question)

        If result = DialogResult.No Then Return

        ' Show progress form
        Dim progressForm As New Form With {
            .Text = "Refreshing Products...",
            .Size = New Size(400, 150),
            .StartPosition = FormStartPosition.CenterParent,
            .FormBorderStyle = FormBorderStyle.FixedDialog,
            .MaximizeBox = False,
            .MinimizeBox = False,
            .ControlBox = False
        }

        Dim lblStatus As New Label With {
            .Text = "Loading products from database...",
            .Location = New Point(20, 20),
            .Size = New Size(360, 30),
            .Font = New Font("Segoe UI", 11),
            .TextAlign = ContentAlignment.MiddleCenter
        }

        Dim progressBar As New ProgressBar With {
            .Location = New Point(20, 60),
            .Size = New Size(360, 30),
            .Style = ProgressBarStyle.Marquee,
            .MarqueeAnimationSpeed = 30
        }

        progressForm.Controls.AddRange({lblStatus, progressBar})

        ' Run refresh in background
        Dim refreshTask = Task.Run(Sub()
                                       Try
                                           ' CRITICAL: Refresh the ProductCacheService from database first!
                                           ProductCacheService.Instance.RefreshCache()

                                           ' Then reload local cache from refreshed service
                                           LoadAllProductsToCache()

                                           ' Update UI on main thread
                                           Me.Invoke(Sub()
                                                         lblStatus.Text = "Products refreshed successfully!"
                                                         lblStatus.ForeColor = _green
                                                         progressBar.Style = ProgressBarStyle.Continuous
                                                         progressBar.Value = 100

                                                         ' Reload current view
                                                         LoadProducts()

                                                         ' Close after 1 second
                                                         Task.Delay(1000).ContinueWith(Sub()
                                                                                           Me.Invoke(Sub() progressForm.Close())
                                                                                       End Sub)
                                                     End Sub)
                                       Catch ex As Exception
                                           Me.Invoke(Sub()
                                                         progressForm.Close()
                                                         MessageBox.Show($"Error refreshing products: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
                                                     End Sub)
                                       End Try
                                   End Sub)

        progressForm.ShowDialog(Me)
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
            ' Use ProductCacheService for instant performance - NO DATABASE QUERY!
            Dim cachedProducts = ProductCacheService.Instance.GetAllProducts()

            _allProducts.Clear()
            _allProducts.Columns.Clear()
            _allProducts.Columns.Add("ProductID", GetType(Integer))
            _allProducts.Columns.Add("ItemCode", GetType(String))
            _allProducts.Columns.Add("Barcode", GetType(String))
            _allProducts.Columns.Add("ProductName", GetType(String))
            _allProducts.Columns.Add("SellingPrice", GetType(Decimal))
            _allProducts.Columns.Add("QtyOnHand", GetType(Decimal))
            _allProducts.Columns.Add("ReorderLevel", GetType(Decimal))
            _allProducts.Columns.Add("Category", GetType(String))

            ' Copy from cache to local DataTable (instant - no DB query!)
            For Each product In cachedProducts
                _allProducts.Rows.Add(
                    product.ProductID,
                    product.SKU,
                    product.Barcode,
                    product.Name,
                    product.SellingPrice,
                    product.QtyOnHand,
                    5, ' Default reorder level
                    product.CategoryName
                )
            Next

            Debug.WriteLine($"[CACHE] Loaded {_allProducts.Rows.Count} products from cache (instant)")
        Catch ex As Exception
            MessageBox.Show($"Error loading products from cache: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    Private Sub InitializeScreenScaling()
        ' Get current screen dimensions - use actual form size when maximized
        ' This ensures we scale based on what Windows actually gives us
        If Me.WindowState = FormWindowState.Maximized Then
            _screenWidth = Me.ClientSize.Width
            _screenHeight = Me.ClientSize.Height
        Else
            Dim currentScreen As Screen = Screen.FromControl(Me)
            If currentScreen Is Nothing Then
                currentScreen = Screen.PrimaryScreen
            End If
            _screenWidth = currentScreen.WorkingArea.Width
            _screenHeight = currentScreen.WorkingArea.Height
        End If

        ' Calculate scale factor based on screen size
        Dim widthScale As Single = CSng(_screenWidth) / CSng(_baseWidth)
        Dim heightScale As Single = CSng(_screenHeight) / CSng(_baseHeight)

        ' Use the smaller scale to maintain aspect ratio
        _scaleFactor = Math.Min(widthScale, heightScale)

        ' Ensure minimum scale factor
        If _scaleFactor < 0.5F Then _scaleFactor = 0.5F

        ' Calculate tile sizes - wider tiles for better visibility
        ' Cart is now 450px, so products area calculation updated
        Dim availableWidth = _screenWidth - 450 - 40 ' Subtract cart width and margins
        Dim margin = 10 ' 5px margin on each side
        _tileWidth = CInt((availableWidth - (_tilesPerRow * margin * 2)) / _tilesPerRow)
        _tileHeight = CInt(_tileWidth * 0.7) ' Maintain aspect ratio

        ' Ensure minimum size and maximum for readability
        If _tileWidth < 120 Then _tileWidth = 120
        If _tileHeight < 85 Then _tileHeight = 85
        If _tileWidth > 200 Then _tileWidth = 200
        If _tileHeight > 140 Then _tileHeight = 140

        Debug.WriteLine($"[SCREEN SCALING] Screen: {_screenWidth}x{_screenHeight}, Scale Factor: {_scaleFactor:F2}, Tile: {_tileWidth}x{_tileHeight}")
    End Sub

    Private Function ScaleSize(baseSize As Integer) As Integer
        Return CInt(baseSize * _scaleFactor)
    End Function

    Private Function ScaleFont(baseSize As Single) As Single
        Return baseSize * _scaleFactor
    End Function

    Private Sub HandleFormResize()
        ' Recalculate scaling when form is resized
        InitializeScreenScaling()
        RepositionControls()

        ' Resize keyboard if visible
        If _onScreenKeyboard IsNot Nothing Then
            _onScreenKeyboard.UpdateKeyboardSize(_scaleFactor)
        End If
    End Sub

    Private Sub RepositionControls()
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

            ' Reposition search bar controls dynamically
            If pnlSearchBar IsNot Nothing Then
                Dim buttonHeight = 44
                Dim spacing = 6
                Dim xPos = 10
                
                ' Fixed widths
                Dim scanBtnWidth = 80
                Dim searchCodeWidth = 150
                Dim searchNameWidth = 150
                Dim refreshBtnWidth = 55
                Dim qtyBtnWidth = 55
                
                ' Scan button
                Dim btnScan = pnlSearchBar.Controls.OfType(Of Button)().FirstOrDefault(Function(b) b.Text.Contains("📷"))
                If btnScan IsNot Nothing Then
                    btnScan.Location = New Point(xPos, 8)
                    btnScan.Size = New Size(scanBtnWidth, buttonHeight)
                    xPos += scanBtnWidth + spacing
                End If
                
                ' Search by code textbox
                If txtSearch IsNot Nothing Then
                    txtSearch.Location = New Point(xPos, 8)
                    txtSearch.Size = New Size(searchCodeWidth, buttonHeight)
                    xPos += searchCodeWidth + spacing
                End If
                
                ' Search by name textbox
                If txtSearchByName IsNot Nothing Then
                    txtSearchByName.Location = New Point(xPos, 8)
                    txtSearchByName.Size = New Size(searchNameWidth, buttonHeight)
                    xPos += searchNameWidth + spacing
                End If
                
                ' Refresh button - directly access class field
                If btnRefresh IsNot Nothing Then
                    btnRefresh.Location = New Point(xPos, 8)
                    btnRefresh.Size = New Size(refreshBtnWidth, buttonHeight)
                    btnRefresh.Visible = True
                    xPos += refreshBtnWidth + spacing
                End If
                
                ' Qty button - directly access class field
                If btnModifyQty IsNot Nothing Then
                    btnModifyQty.Location = New Point(xPos, 8)
                    btnModifyQty.Size = New Size(qtyBtnWidth, buttonHeight)
                    btnModifyQty.Visible = True
                End If
            End If

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

        Catch ex As Exception
            ' Ignore layout errors during resize
            MessageBox.Show($"RepositionControls ERROR: {ex.Message}{vbCrLf}{ex.StackTrace}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        Finally
            Me.ResumeLayout()
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
        ' Check if panel is initialized
        If pnlCategories Is Nothing Then
            ' Show categories as tiles in main product area instead
            ShowCategories()
            ResetIdleTimer() ' Start idle timer after showing categories
            Return
        End If

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
            ' Filter cached products in memory - INSTANT!
            Dim filteredRows As List(Of DataRow)

            If String.IsNullOrEmpty(category) Then
                ' Show first 50 products only
                filteredRows = _allProducts.AsEnumerable().Take(50).ToList()
            Else
                ' Filter by category and limit to 100 products
                filteredRows = _allProducts.AsEnumerable().
                    Where(Function(row)
                              If IsDBNull(row("Category")) Then Return False
                              Dim cat = row("Category").ToString()
                              Return cat.Equals(category, StringComparison.OrdinalIgnoreCase) OrElse
                               cat.IndexOf(category, StringComparison.OrdinalIgnoreCase) >= 0
                          End Function).
                    Take(100).
                    ToList()
            End If

            Dim productCount = 0
            Dim cardIndex = 0
            For Each row As DataRow In filteredRows
                Dim price = If(IsDBNull(row("SellingPrice")), 0D, CDec(row("SellingPrice")))
                Dim stock = If(IsDBNull(row("QtyOnHand")), 0D, CDec(row("QtyOnHand")))
                Dim reorderLevel = If(IsDBNull(row("ReorderLevel")), 0D, CDec(row("ReorderLevel")))

                Dim card = CreateProductCard(
                    CInt(row("ProductID")),
                    row("ItemCode").ToString(),
                    row("ProductName").ToString(),
                    price,
                    stock,
                    reorderLevel
                )

                ' Start invisible for smooth fade-in
                card.Visible = False
                flpProducts.Controls.Add(card)

                ' Animate fade-in with staggered delay
                Dim currentIndex = cardIndex
                Dim timer As New Timer With {.Interval = 20 + (currentIndex * 15), .Tag = card}
                AddHandler timer.Tick, Sub(s, ev)
                                           Dim t = CType(s, Timer)
                                           Dim c = CType(t.Tag, Panel)
                                           c.Visible = True
                                           AnimateFadeIn(c)
                                           t.Stop()
                                           t.Dispose()
                                       End Sub
                timer.Start()

                productCount += 1
                cardIndex += 1
            Next

            If productCount = 0 Then
                Dim lblNoProducts As New Label With {
                    .Text = If(String.IsNullOrEmpty(category),
                               "No products available. Use search (F3/F4) to find products.",
                               $"No products found in category: {category}"),
                    .Font = New Font("Segoe UI", 14, FontStyle.Italic),
                    .ForeColor = Color.Gray,
                    .AutoSize = True,
                    .Padding = New Padding(20)
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

        ' Fixed compact sizing - no scaling to prevent size changes
        Dim cardWidth As Integer = 160
        Dim cardHeight As Integer = 100

        Dim card As New Panel With {
            .Size = New Size(cardWidth, cardHeight),
            .BackColor = Color.White,
            .BorderStyle = BorderStyle.FixedSingle,
            .Cursor = Cursors.Hand,
            .Margin = New Padding(4),
            .Tag = New With {productID, itemCode, productName, price, stock}
        }

        ' Fixed compact font sizes - smaller item code, larger name area
        Dim codeFontSize As Single = 7
        Dim nameFontSize As Single = 9
        Dim priceFontSize As Single = 11
        Dim stockFontSize As Single = 7

        Dim lblItemCode As New Label With {
            .Text = itemCode,
            .Font = New Font("Segoe UI", codeFontSize, FontStyle.Bold),
            .ForeColor = _darkBlue,
            .Location = New Point(4, 4),
            .Size = New Size(50, 14),
            .AutoEllipsis = True
        }

        Dim lblName As New Label With {
            .Text = productName,
            .Font = New Font("Segoe UI", nameFontSize, FontStyle.Bold),
            .ForeColor = _darkBlue,
            .Location = New Point(4, 20),
            .Size = New Size(cardWidth - 8, 35),
            .AutoEllipsis = True
        }

        Dim lblPrice As New Label With {
            .Text = price.ToString("C2"),
            .Font = New Font("Segoe UI", priceFontSize, FontStyle.Bold),
            .ForeColor = _green,
            .Location = New Point(4, cardHeight - 35),
            .AutoSize = True
        }

        ' Stock label - RED if at or below reorder level
        Dim stockColor = If(isLowStock, Color.Red, _darkGray)
        Dim stockText = If(isLowStock, $"Stock: {stock} ⚠", $"Stock: {stock}")

        Dim lblStock As New Label With {
            .Text = stockText,
            .Font = New Font("Segoe UI", stockFontSize, If(isLowStock, FontStyle.Bold, FontStyle.Regular)),
            .ForeColor = stockColor,
            .Location = New Point(4, cardHeight - 16),
            .AutoSize = True
        }

        card.Controls.AddRange({lblItemCode, lblName, lblPrice, lblStock})

        ' Make entire card clickable - add 1 qty directly to cart
        AddHandler card.Click, Sub() AddProductToCart(productID, itemCode, productName, price)
        AddHandler lblItemCode.Click, Sub() AddProductToCart(productID, itemCode, productName, price)
        AddHandler lblName.Click, Sub() AddProductToCart(productID, itemCode, productName, price)
        AddHandler lblPrice.Click, Sub() AddProductToCart(productID, itemCode, productName, price)
        AddHandler lblStock.Click, Sub() AddProductToCart(productID, itemCode, productName, price)

        AddHandler card.MouseEnter, Sub() card.BackColor = ColorTranslator.FromHtml("#E3F2FD")
        AddHandler card.MouseLeave, Sub() card.BackColor = Color.White

        Return card
    End Function

    Private Sub AddProductToCart(productID As Integer, itemCode As String, productName As String, price As Decimal)
        ' Don't hide keyboards or clear search - keep search results visible
        ' User can manually close keyboard/numpad or clear search if needed

        Dim rowIndex As Integer = -1
        Dim existingRow = _cartItems.Select($"ProductID = {productID}")
        If existingRow.Length > 0 Then
            existingRow(0)("Qty") = CDec(existingRow(0)("Qty")) + 1
            existingRow(0)("Total") = CDec(existingRow(0)("Qty")) * CDec(existingRow(0)("Price"))
            rowIndex = _cartItems.Rows.IndexOf(existingRow(0))
        Else
            _cartItems.Rows.Add(productID, itemCode, productName, 1, price, price)
            rowIndex = _cartItems.Rows.Count - 1
        End If

        CalculateTotals()

        ' Highlight the last added/updated item
        If rowIndex >= 0 AndAlso dgvCart.Rows.Count > rowIndex Then
            dgvCart.ClearSelection()
            dgvCart.Rows(rowIndex).Selected = True
            dgvCart.FirstDisplayedScrollingRowIndex = rowIndex
        End If

        ' Auto-focus barcode scanner for next scan
        If txtBarcodeScanner IsNot Nothing Then
            txtBarcodeScanner.Focus()
        End If
    End Sub

    Private Sub CalculateTotals()
        ' Cart prices are VAT INCLUSIVE
        ' Calculate totals: Total Inc VAT, then work backwards to get Ex VAT and VAT amount
        Dim totalInclVAT As Decimal = 0
        For Each row As DataRow In _cartItems.Rows
            totalInclVAT += CDec(row("Total"))
        Next

        ' Calculate Ex VAT and VAT amount from inclusive price
        Dim totalExVAT = Math.Round(totalInclVAT / 1.15D, 2)
        Dim vatAmount = totalInclVAT - totalExVAT

        lblSubtotal.Text = $"Subtotal (Ex VAT): {totalExVAT.ToString("C2")}"
        lblTax.Text = $"VAT (15%): {vatAmount.ToString("C2")}"
        lblTotal.Text = totalInclVAT.ToString("C2")
    End Sub

    Private Sub CreateShortcutButtons()
        pnlShortcuts.Controls.Clear()

        Dim shortcuts As New List(Of Tuple(Of String, String, Action)) From {
            Tuple.Create("F1", "💰 Sale", CType(Sub() SaleMode(), Action)),
            Tuple.Create("F2", "⏸️ Hold", CType(Sub() HoldSale(), Action)),
            Tuple.Create("F3", "🔍 Code", CType(Sub() ShowNumpad(), Action)),
            Tuple.Create("F4", "⌨️ Name", CType(Sub() ToggleKeyboard(), Action)),
            Tuple.Create("F5", "📋 Recall", CType(Sub() RecallSale(), Action)),
            Tuple.Create("F6", "🔢 Qty", CType(Sub() ChangeQuantity(), Action)),
            Tuple.Create("F7", "💰 Disc", CType(Sub() ApplyDiscount(), Action)),
            Tuple.Create("F8", "🗑️ Remove", CType(Sub() RemoveItem(), Action)),
            Tuple.Create("F9", "↩️ Return", CType(Sub() ProcessReturn(), Action)),
            Tuple.Create("F10", "❌ Void", CType(Sub() VoidSale(), Action)),
            Tuple.Create("F11", "📝 Order", CType(Sub() CreateOrder(), Action)),
            Tuple.Create("F12", "📦 Collect", CType(Sub() OrderCollection(), Action)),
            Tuple.Create("", "🎂 User Defined", CType(Sub() StartUserDefinedOrder(), Action)),
            Tuple.Create("", "📦 Collect UD", CType(Sub() CollectUserDefinedOrder(), Action)),
            Tuple.Create("", "✏️ Edit Order", CType(Sub() EditCakeOrder(), Action)),
            Tuple.Create("", "📦 Box Items", CType(Sub() CreateBoxItems(), Action)),
            Tuple.Create("", "⚙️ Set Priority", CType(Sub() SetItemPriority(), Action))
        }

        Dim visibleCount = 17 ' 12 F-keys + 5 additional buttons
        ' Use actual form width for button sizing - optimized for 1024x768
        Dim screenWidth = Me.ClientSize.Width
        Dim leftMargin = 5
        Dim rightMargin = 5
        Dim availableWidth = screenWidth - leftMargin - rightMargin
        Dim spacing = 3 ' Reduced spacing for more room
        Dim buttonWidth = ((availableWidth - (spacing * (visibleCount - 1))) \ visibleCount)

        ' Ensure buttons fit on screen - adjust if too wide
        If buttonWidth < 60 Then 
            buttonWidth = 60
        ElseIf buttonWidth > 80 Then
            buttonWidth = 80
        End If
        
        ' Calculate font size based on button width
        Dim fontSize As Single = If(buttonWidth < 65, 7, 8)

        For i = 0 To visibleCount - 1
            Dim shortcut = shortcuts(i)
            Dim btn As New Button With {
                .Text = $"{shortcut.Item1}{vbCrLf}{shortcut.Item2}",
                .Size = New Size(buttonWidth, 60),
                .Location = New Point(leftMargin + (i * (buttonWidth + spacing)), 10),
                .BackColor = _darkBlue,
                .ForeColor = Color.White,
                .Font = New Font("Segoe UI", fontSize, FontStyle.Bold),
                .FlatStyle = FlatStyle.Flat,
                .Cursor = Cursors.Hand,
                .Tag = shortcut.Item3
            }
            btn.FlatAppearance.BorderSize = 0
            btn.FlatAppearance.BorderColor = _lightBlue
            AddHandler btn.Click, Sub(s, e) CType(CType(s, Button).Tag, Action).Invoke()
            AddHandler btn.MouseEnter, Sub(s, e) btn.BackColor = _lightBlue
            AddHandler btn.MouseLeave, Sub(s, e) btn.BackColor = _darkBlue
            pnlShortcuts.Controls.Add(btn)
        Next
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
            ' Check if this is a box barcode (format: 6-digit numeric code, e.g., 600001)
            ' Box barcodes are exactly 6 digits: BranchDigit (1) + Sequence (5)
            If itemCode.Length = 6 AndAlso IsNumeric(itemCode) Then
                ' Try to load as box barcode first
                If LoadBoxItems(itemCode) Then
                    Return
                End If
                ' If not found as box, continue to regular product search below
            End If

            ' Query Demo_Retail_Product and get VAT-inclusive price from Demo_Retail_Price
            ' Use the scanned/typed itemCode as the ItemCode to prevent duplicates
            ' Match product card behavior: only search products at current branch
            ' Use LIKE with wildcard to match partial barcodes (e.g., scan "12007" matches barcode "20012007")
            Dim sql = "
                SELECT TOP 1
                    p.ProductID,
                    p.Name AS ProductName,
                    ISNULL(price.SellingPrice, 0) AS SellingPrice
                FROM Demo_Retail_Product p
                LEFT JOIN Demo_Retail_Price price ON p.ProductID = price.ProductID AND price.BranchID = @BranchID
                WHERE (p.SKU LIKE '%' + @ItemCode + '%' OR p.Barcode LIKE '%' + @ItemCode + '%')
                  AND p.BranchID = @BranchID
                  AND p.IsActive = 1
                  AND ISNULL(price.SellingPrice, 0) > 0"

            Using conn As New SqlConnection(_connectionString)
                conn.Open()
                Using cmd As New SqlCommand(sql, conn)
                    cmd.Parameters.AddWithValue("@ItemCode", itemCode)
                    cmd.Parameters.AddWithValue("@BranchID", _branchID)

                    Using reader = cmd.ExecuteReader()
                        If reader.Read() Then
                            ' Use the scanned itemCode directly to ensure consistency
                            AddProductToCart(
                                reader.GetInt32(0),
                                itemCode,
                                reader.GetString(1),
                                reader.GetDecimal(2)
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
                            txtBarcodeScanner.Focus()
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
            Case Keys.F1 : SaleMode() : Return True
            Case Keys.F2 : HoldSale() : Return True
            Case Keys.F3 : ShowNumpad() : Return True
            Case Keys.F4 : ToggleKeyboard() : Return True
            Case Keys.F5 : RecallSale() : Return True
            Case Keys.F6 : ChangeQuantity() : Return True
            Case Keys.F7 : ApplyDiscount() : Return True
            Case Keys.F8 : RemoveItem() : Return True
            Case Keys.F9 : ProcessReturn() : Return True
            Case Keys.F10 : VoidSale() : Return True
            Case Keys.F11 : CreateOrder() : Return True
            Case Keys.F12 : OrderCollection() : Return True
            Case Keys.Shift Or Keys.F11 : EditCakeOrder() : Return True
        End Select
        Return MyBase.ProcessCmdKey(msg, keyData)
    End Function

    ' Shortcut Functions
    Private Sub ToggleKeyboard()
        If _onScreenKeyboard.IsKeyboardVisible Then
            _onScreenKeyboard.HideKeyboard()
            ' Don't restore placeholder - keep search results visible
        Else
            ' Hide numpad if visible
            HideNumpad()

            ' Clear placeholder if present
            If txtSearchByName.Text.Contains("Search by name") Then
                txtSearchByName.Text = ""
                txtSearchByName.ForeColor = Color.Black
            End If
            _onScreenKeyboard.ShowKeyboard()
        End If
    End Sub

    Private Sub ShowNumpad()
        ' Hide keyboard if visible
        If _onScreenKeyboard.IsKeyboardVisible Then
            _onScreenKeyboard.HideKeyboard()
        End If

        ' Create or show numpad
        If pnlNumpad Is Nothing Then
            pnlNumpad = New Panel With {
                .Size = New Size(400, 500),
                .Location = New Point((Me.ClientSize.Width - 400) \ 2, (Me.ClientSize.Height - 500) \ 2),
                .BackColor = Color.White,
                .BorderStyle = BorderStyle.FixedSingle,
                .Visible = False
            }

            ' Header with close button
            Dim pnlHeader As New Panel With {
                .Dock = DockStyle.Top,
                .Height = 50,
                .BackColor = _darkBlue
            }

            Dim lblTitle As New Label With {
                .Text = "🔢 SEARCH BY CODE",
                .Font = New Font("Segoe UI", 16, FontStyle.Bold),
                .ForeColor = Color.White,
                .Location = New Point(15, 12),
                .AutoSize = True
            }

            Dim btnClose As New Button With {
                .Text = "✖",
                .Size = New Size(40, 40),
                .Location = New Point(350, 5),
                .BackColor = ColorTranslator.FromHtml("#E74C3C"),
                .ForeColor = Color.White,
                .Font = New Font("Segoe UI", 16, FontStyle.Bold),
                .FlatStyle = FlatStyle.Flat,
                .Cursor = Cursors.Hand
            }
            btnClose.FlatAppearance.BorderSize = 0
            AddHandler btnClose.Click, Sub() HideNumpad()

            pnlHeader.Controls.AddRange({lblTitle, btnClose})

            ' Search textbox - accepts keyboard input with placeholder
            Dim txtCode As New TextBox With {
                .Font = New Font("Segoe UI", 24, FontStyle.Bold),
                .Location = New Point(20, 70),
                .Size = New Size(360, 50),
                .TextAlign = HorizontalAlignment.Center,
                .ReadOnly = False,
                .Text = "Type code...",
                .ForeColor = Color.Gray
            }

            ' Clear placeholder when user starts typing
            AddHandler txtCode.Enter, Sub()
                                          If txtCode.Text = "Type code..." Then
                                              txtCode.Text = ""
                                              txtCode.ForeColor = Color.Black
                                          End If
                                      End Sub

            ' Also clear on first keypress
            AddHandler txtCode.KeyDown, Sub(sender, e)
                                            If txtCode.Text = "Type code..." Then
                                                txtCode.Text = ""
                                                txtCode.ForeColor = Color.Black
                                            End If
                                        End Sub

            ' Handle keyboard input - search directly
            AddHandler txtCode.TextChanged, Sub()
                                                If txtCode.Text <> "Type code..." AndAlso Not String.IsNullOrWhiteSpace(txtCode.Text) AndAlso txtCode.Text.Length >= 2 Then
                                                    SearchProducts(txtCode.Text)
                                                End If
                                            End Sub

            ' Allow only numbers and backspace - prevent alphabets
            AddHandler txtCode.KeyPress, Sub(sender, e)
                                             ' Allow: digits (0-9), backspace, delete
                                             If Not Char.IsDigit(e.KeyChar) AndAlso
                   e.KeyChar <> ChrW(Keys.Back) AndAlso
                   e.KeyChar <> ChrW(Keys.Delete) Then
                                                 e.Handled = True ' Block the character
                                             End If
                                         End Sub

            ' Focus the textbox so keyboard works immediately
            txtCode.Focus()
            txtCode.SelectionStart = txtCode.Text.Length

            ' Numpad buttons
            Dim pnlButtons As New Panel With {
                .Location = New Point(50, 140),
                .Size = New Size(300, 320)
            }

            Dim buttonSize As New Size(90, 70)
            Dim buttons(,) As String = {{"7", "8", "9"}, {"4", "5", "6"}, {"1", "2", "3"}, {"CLR", "0", "⌫"}}
            Dim numpadButtons As New List(Of Button) ' Store buttons for visual feedback

            For row = 0 To 3
                For col = 0 To 2
                    Dim btnText = buttons(row, col)
                    Dim btn As New Button With {
                        .Text = btnText,
                        .Size = buttonSize,
                        .Location = New Point(col * 100, row * 80),
                        .Font = New Font("Segoe UI", 20, FontStyle.Bold),
                        .BackColor = If(btnText = "CLR", ColorTranslator.FromHtml("#E74C3C"), _lightBlue),
                        .ForeColor = Color.White,
                        .FlatStyle = FlatStyle.Flat,
                        .Cursor = Cursors.Hand
                    }
                    btn.FlatAppearance.BorderSize = 0
                    btn.Tag = btnText ' Store button text for matching
                    AddHandler btn.Click, Sub(s, e)
                                              Dim clickedBtn = CType(s, Button)

                                              ' Clear placeholder on first click
                                              If txtCode.Text = "Type code..." Then
                                                  txtCode.Text = ""
                                                  txtCode.ForeColor = Color.Black
                                              End If

                                              If clickedBtn.Text = "⌫" Then
                                                  If txtCode.Text.Length > 0 Then txtCode.Text = txtCode.Text.Substring(0, txtCode.Text.Length - 1)
                                              ElseIf clickedBtn.Text = "CLR" Then
                                                  txtCode.Text = ""
                                              Else
                                                  txtCode.Text &= clickedBtn.Text
                                              End If

                                              ' TextChanged event will trigger the search
                                          End Sub
                    pnlButtons.Controls.Add(btn)
                    numpadButtons.Add(btn) ' Add to list for visual feedback
                Next
            Next

            ' Add visual feedback for physical keyboard on numpad
            AddHandler txtCode.KeyDown, Sub(sender, e)
                                            ' Find matching button and flash it
                                            Dim keyChar = e.KeyCode.ToString()
                                            If e.KeyCode >= Keys.D0 AndAlso e.KeyCode <= Keys.D9 Then
                                                keyChar = keyChar.Replace("D", "") ' Remove "D" prefix from D0-D9
                                            ElseIf e.KeyCode >= Keys.NumPad0 AndAlso e.KeyCode <= Keys.NumPad9 Then
                                                keyChar = keyChar.Replace("NumPad", "") ' Remove "NumPad" prefix
                                            ElseIf e.KeyCode = Keys.Back Then
                                                keyChar = "⌫"
                                            End If

                                            For Each btn As Button In numpadButtons
                                                If btn.Tag IsNot Nothing AndAlso btn.Tag.ToString() = keyChar Then
                                                    FlashNumpadButton(btn)
                                                    Exit For
                                                End If
                                            Next
                                        End Sub

            pnlNumpad.Controls.AddRange({pnlHeader, txtCode, pnlButtons})
            Me.Controls.Add(pnlNumpad)
            pnlNumpad.BringToFront()
        End If

        pnlNumpad.Visible = True
        pnlNumpad.BringToFront()

        ' Focus the textbox so physical keyboard works
        Dim numpadTextBox = CType(pnlNumpad.Controls.OfType(Of TextBox)().FirstOrDefault(), TextBox)
        If numpadTextBox IsNot Nothing Then
            numpadTextBox.Focus()
            If numpadTextBox.Text = "Type code..." Then
                numpadTextBox.SelectionStart = 0
            Else
                numpadTextBox.SelectionStart = numpadTextBox.Text.Length
            End If
        End If
    End Sub

    Private Sub HideNumpad()
        If pnlNumpad IsNot Nothing Then
            pnlNumpad.Visible = False
        End If
    End Sub

    Private Async Sub FlashNumpadButton(btn As Button)
        ' Highlight the button briefly when physical key is pressed
        Dim originalColor = btn.BackColor

        ' Flash with darker color
        btn.BackColor = Color.FromArgb(Math.Max(0, originalColor.R - 50), Math.Max(0, originalColor.G - 50), Math.Max(0, originalColor.B - 50))

        ' Wait 150ms
        Await Task.Delay(150)

        ' Restore original color
        btn.BackColor = originalColor
    End Sub

    Private Sub FilterProductsByCode(code As String)
        ' Filter products by code using the existing search functionality
        txtSearch.Text = code
        ' The txtSearch TextChanged event will trigger the search
    End Sub

    Private Sub NewSale()
        ' Clear cart and show idle screen with categories/products
        If _cartItems.Rows.Count > 0 Then
            Dim result = MessageBox.Show("Clear current sale and start new?", "New Sale", MessageBoxButtons.YesNo, MessageBoxIcon.Question)
            If result = DialogResult.No Then
                Return
            End If
        End If

        _cartItems.Clear()
        CalculateTotals()
        txtBarcodeScanner.Clear()
        txtSearch.Clear()
        txtSearchByName.Text = "⌨️ Touch to search by name (F4)..."
        txtSearchByName.ForeColor = _darkGray

        ' Hide keyboard if visible
        If _onScreenKeyboard.IsKeyboardVisible Then
            _onScreenKeyboard.HideKeyboard()
        End If

        ' Show idle screen (blank screen with cached categories/products)
        ShowIdleScreen()

        UpdateStatusBar("New sale started - Touch screen to begin")
        
        ' Focus barcode scanner for immediate scanning
        If txtBarcodeScanner IsNot Nothing Then
            txtBarcodeScanner.Focus()
        End If
    End Sub

    Private Sub HoldSale()
        If _cartItems.Rows.Count = 0 Then
            MessageBox.Show("No items in cart to hold.", "Hold Sale", MessageBoxButtons.OK, MessageBoxIcon.Information)
            Return
        End If

        Try
            Dim holdNumber = $"HOLD{_branchID}{_tillPointID}{DateTime.Now:yyyyMMddHHmmss}"

            Using conn As New SqlConnection(_connectionString)
                conn.Open()
                Using transaction = conn.BeginTransaction()
                    Try
                        ' Insert held sale header
                        Dim sqlHeader = "
                            INSERT INTO Demo_HeldSales (HoldNumber, CashierID, BranchID, TillPointID, HoldDate)
                            VALUES (@HoldNumber, @CashierID, @BranchID, @TillPointID, GETDATE());
                            SELECT SCOPE_IDENTITY();"

                        Dim heldSaleID As Integer
                        Using cmd As New SqlCommand(sqlHeader, conn, transaction)
                            cmd.Parameters.AddWithValue("@HoldNumber", holdNumber)
                            cmd.Parameters.AddWithValue("@CashierID", _cashierID)
                            cmd.Parameters.AddWithValue("@BranchID", _branchID)
                            cmd.Parameters.AddWithValue("@TillPointID", _tillPointID)
                            heldSaleID = Convert.ToInt32(cmd.ExecuteScalar())
                        End Using

                        ' Insert line items
                        Dim sqlItems = "
                            INSERT INTO Demo_HeldSaleItems (HeldSaleID, ProductID, ItemCode, ProductName, Quantity, UnitPrice, DiscountPercent, LineTotal)
                            VALUES (@HeldSaleID, @ProductID, @ItemCode, @ProductName, @Quantity, @UnitPrice, @DiscountPercent, @LineTotal)"

                        For Each row As DataRow In _cartItems.Rows
                            Using cmd As New SqlCommand(sqlItems, conn, transaction)
                                cmd.Parameters.AddWithValue("@HeldSaleID", heldSaleID)
                                cmd.Parameters.AddWithValue("@ProductID", row("ProductID"))
                                cmd.Parameters.AddWithValue("@ItemCode", row("ItemCode"))
                                cmd.Parameters.AddWithValue("@ProductName", row("Product"))
                                cmd.Parameters.AddWithValue("@Quantity", row("Qty"))
                                cmd.Parameters.AddWithValue("@UnitPrice", row("Price"))
                                cmd.Parameters.AddWithValue("@DiscountPercent", If(row.Table.Columns.Contains("DiscountPercent"), row("DiscountPercent"), 0))
                                cmd.Parameters.AddWithValue("@LineTotal", row("Total"))
                                cmd.ExecuteNonQuery()
                            End Using
                        Next

                        transaction.Commit()

                        ' Clear cart
                        _cartItems.Clear()
                        CalculateTotals()

                        MessageBox.Show($"Sale placed on hold.{vbCrLf}Hold Number: {holdNumber}", "Sale On Hold", MessageBoxButtons.OK, MessageBoxIcon.Information)
                        UpdateStatusBar($"Sale held: {holdNumber}")

                    Catch ex As Exception
                        transaction.Rollback()
                        Throw
                    End Try
                End Using
            End Using

        Catch ex As Exception
            MessageBox.Show($"Error holding sale: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    Private Sub RecallSale()
        Try
            ' Get list of held sales for this cashier/till
            Dim heldSales As New DataTable()

            Using conn As New SqlConnection(_connectionString)
                conn.Open()

                Dim sql = "
                    SELECT HeldSaleID, HoldNumber, HoldDate, 
                           (SELECT COUNT(*) FROM Demo_HeldSaleItems WHERE HeldSaleID = hs.HeldSaleID) AS ItemCount,
                           (SELECT SUM(LineTotal) FROM Demo_HeldSaleItems WHERE HeldSaleID = hs.HeldSaleID) AS Total
                    FROM Demo_HeldSales hs
                    WHERE CashierID = @CashierID 
                      AND BranchID = @BranchID 
                      AND TillPointID = @TillPointID
                      AND IsRecalled = 0
                    ORDER BY HoldDate DESC"

                Using cmd As New SqlCommand(sql, conn)
                    cmd.Parameters.AddWithValue("@CashierID", _cashierID)
                    cmd.Parameters.AddWithValue("@BranchID", _branchID)
                    cmd.Parameters.AddWithValue("@TillPointID", _tillPointID)

                    Using adapter As New SqlDataAdapter(cmd)
                        adapter.Fill(heldSales)
                    End Using
                End Using
            End Using

            If heldSales.Rows.Count = 0 Then
                MessageBox.Show("No held sales found.", "Recall Sale", MessageBoxButtons.OK, MessageBoxIcon.Information)
                Return
            End If

            ' Show selection form
            ShowHeldSalesSelection(heldSales)

        Catch ex As Exception
            MessageBox.Show($"Error recalling sale: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    Private Sub ShowHeldSalesSelection(heldSales As DataTable)
        Dim selectForm As New Form With {
            .Text = "Recall Held Sale",
            .Size = New Size(700, 500),
            .StartPosition = FormStartPosition.CenterScreen,
            .FormBorderStyle = FormBorderStyle.FixedDialog,
            .MaximizeBox = False,
            .MinimizeBox = False
        }

        ' Header
        Dim lblHeader As New Label With {
            .Text = "📋 SELECT HELD SALE TO RECALL",
            .Font = New Font("Segoe UI", 16, FontStyle.Bold),
            .Dock = DockStyle.Top,
            .Height = 60,
            .TextAlign = ContentAlignment.MiddleCenter,
            .BackColor = _darkBlue,
            .ForeColor = Color.White
        }

        ' Grid
        Dim dgvHeld As New DataGridView With {
            .Location = New Point(20, 80),
            .Size = New Size(640, 300),
            .DataSource = heldSales,
            .ReadOnly = True,
            .SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            .MultiSelect = False,
            .AllowUserToAddRows = False,
            .AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
        }

        If dgvHeld.Columns.Contains("HeldSaleID") Then dgvHeld.Columns("HeldSaleID").Visible = False
        If dgvHeld.Columns.Contains("HoldNumber") Then dgvHeld.Columns("HoldNumber").HeaderText = "Hold Number"
        If dgvHeld.Columns.Contains("HoldDate") Then
            dgvHeld.Columns("HoldDate").HeaderText = "Date/Time"
            dgvHeld.Columns("HoldDate").DefaultCellStyle.Format = "dd/MM/yyyy HH:mm"
        End If
        If dgvHeld.Columns.Contains("ItemCount") Then dgvHeld.Columns("ItemCount").HeaderText = "Items"
        If dgvHeld.Columns.Contains("Total") Then
            dgvHeld.Columns("Total").HeaderText = "Total"
            dgvHeld.Columns("Total").DefaultCellStyle.Format = "C2"
        End If

        ' Buttons
        Dim btnRecall As New Button With {
            .Text = "✓ RECALL",
            .Size = New Size(150, 50),
            .Location = New Point(200, 400),
            .BackColor = _green,
            .ForeColor = Color.White,
            .Font = New Font("Segoe UI", 12, FontStyle.Bold),
            .FlatStyle = FlatStyle.Flat
        }
        btnRecall.FlatAppearance.BorderSize = 0

        Dim btnCancel As New Button With {
            .Text = "✖ CANCEL",
            .Size = New Size(150, 50),
            .Location = New Point(370, 400),
            .BackColor = ColorTranslator.FromHtml("#E74C3C"),
            .ForeColor = Color.White,
            .Font = New Font("Segoe UI", 12, FontStyle.Bold),
            .FlatStyle = FlatStyle.Flat
        }
        btnCancel.FlatAppearance.BorderSize = 0

        AddHandler btnRecall.Click, Sub()
                                        If dgvHeld.SelectedRows.Count > 0 Then
                                            Dim heldSaleID = CInt(dgvHeld.SelectedRows(0).Cells("HeldSaleID").Value)
                                            RecallHeldSale(heldSaleID)
                                            selectForm.Close()
                                        End If
                                    End Sub

        AddHandler btnCancel.Click, Sub() selectForm.Close()

        selectForm.Controls.AddRange({lblHeader, dgvHeld, btnRecall, btnCancel})
        selectForm.ShowDialog()
    End Sub

    Private Sub RecallHeldSale(heldSaleID As Integer)
        Try
            ' Check if cart has items
            If _cartItems.Rows.Count > 0 Then
                Dim result = MessageBox.Show("Current cart has items. Clear and recall held sale?", "Recall Sale", MessageBoxButtons.YesNo, MessageBoxIcon.Question)
                If result = DialogResult.No Then
                    Return
                End If
            End If

            Using conn As New SqlConnection(_connectionString)
                conn.Open()
                Using transaction = conn.BeginTransaction()
                    Try
                        ' Get held sale items
                        Dim sql = "
                            SELECT ProductID, ItemCode, ProductName, Quantity, UnitPrice, DiscountPercent, LineTotal
                            FROM Demo_HeldSaleItems
                            WHERE HeldSaleID = @HeldSaleID"

                        Dim items As New DataTable()
                        Using cmd As New SqlCommand(sql, conn, transaction)
                            cmd.Parameters.AddWithValue("@HeldSaleID", heldSaleID)
                            Using adapter As New SqlDataAdapter(cmd)
                                adapter.Fill(items)
                            End Using
                        End Using

                        ' Clear current cart
                        _cartItems.Clear()

                        ' Add items to cart
                        For Each row As DataRow In items.Rows
                            Dim newRow = _cartItems.NewRow()
                            newRow("ProductID") = row("ProductID")
                            newRow("ItemCode") = row("ItemCode")
                            newRow("Product") = row("ProductName")
                            newRow("Qty") = row("Quantity")
                            newRow("Price") = row("UnitPrice")
                            If _cartItems.Columns.Contains("DiscountPercent") Then
                                newRow("DiscountPercent") = row("DiscountPercent")
                            End If
                            newRow("Total") = row("LineTotal")
                            _cartItems.Rows.Add(newRow)
                        Next

                        ' Mark as recalled
                        Dim sqlUpdate = "UPDATE Demo_HeldSales SET IsRecalled = 1, RecalledDate = GETDATE() WHERE HeldSaleID = @HeldSaleID"
                        Using cmd As New SqlCommand(sqlUpdate, conn, transaction)
                            cmd.Parameters.AddWithValue("@HeldSaleID", heldSaleID)
                            cmd.ExecuteNonQuery()
                        End Using

                        transaction.Commit()

                        CalculateTotals()
                        UpdateStatusBar($"Sale recalled - {items.Rows.Count} items")
                        MessageBox.Show("Held sale recalled successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information)

                    Catch ex As Exception
                        transaction.Rollback()
                        Throw
                    End Try
                End Using
            End Using

        Catch ex As Exception
            MessageBox.Show($"Error recalling held sale: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    Private Sub ChangeQuantity()
        ' F5 - Product Lookup
        Try
            Using lookupForm As New ProductLookupForm(_branchID)
                If lookupForm.ShowDialog() = DialogResult.OK Then
                    ' Add product to cart
                    Dim newRow = _cartItems.NewRow()
                    newRow("ProductID") = lookupForm.SelectedProductID
                    newRow("ItemCode") = lookupForm.SelectedItemCode
                    newRow("Product") = lookupForm.SelectedProductName
                    newRow("Qty") = 1
                    newRow("Price") = lookupForm.SelectedPrice
                    newRow("Total") = lookupForm.SelectedPrice
                    _cartItems.Rows.Add(newRow)

                    CalculateTotals()
                    UpdateStatusBar($"Added: {lookupForm.SelectedProductName}")
                End If
            End Using
        Catch ex As Exception
            MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    Private Sub ApplyDiscount()
        ' F6 - Apply percentage discount to selected line item (requires supervisor authorization)
        If dgvCart.SelectedRows.Count = 0 Then
            MessageBox.Show("Please select an item to discount.", "Apply Discount", MessageBoxButtons.OK, MessageBoxIcon.Information)
            Return
        End If

        ' Supervisor Authorization
        Dim supervisorUsername = InputBox("Enter Retail Supervisor Username:", "Discount Authorization")
        If String.IsNullOrWhiteSpace(supervisorUsername) Then Return

        Dim supervisorPassword As String = ""
        Using pwdForm As New PasswordInputForm("Enter Retail Supervisor Password:", "Discount Authorization")
            If pwdForm.ShowDialog() <> DialogResult.OK Then Return
            supervisorPassword = pwdForm.Password
        End Using

        If String.IsNullOrWhiteSpace(supervisorPassword) Then Return

        ' Validate supervisor credentials
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
            End Using
        Catch ex As Exception
            MessageBox.Show($"Authorization error: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
            Return
        End Try

        Dim selectedRow = dgvCart.SelectedRows(0)
        Dim productName = selectedRow.Cells("Product").Value.ToString()
        Dim currentPrice = CDec(selectedRow.Cells("Price").Value)
        Dim currentQty = CDec(selectedRow.Cells("Qty").Value)

        ' Show discount dialog
        Dim discountForm As New Form With {
            .Text = "Apply Discount",
            .Size = New Size(400, 250),
            .StartPosition = FormStartPosition.CenterScreen,
            .FormBorderStyle = FormBorderStyle.FixedDialog,
            .MaximizeBox = False,
            .MinimizeBox = False,
            .BackColor = Color.White
        }

        Dim lblProduct As New Label With {
            .Text = $"Product: {productName}",
            .Font = New Font("Segoe UI", 11, FontStyle.Bold),
            .Location = New Point(20, 20),
            .AutoSize = True
        }

        Dim lblOriginal As New Label With {
            .Text = $"Original Price: {currentPrice:C2}",
            .Font = New Font("Segoe UI", 10),
            .Location = New Point(20, 50),
            .AutoSize = True
        }

        Dim lblDiscount As New Label With {
            .Text = "Discount %:",
            .Font = New Font("Segoe UI", 10, FontStyle.Bold),
            .Location = New Point(20, 90),
            .AutoSize = True
        }

        Dim txtDiscount As New NumericUpDown With {
            .Font = New Font("Segoe UI", 14),
            .Location = New Point(130, 85),
            .Size = New Size(100, 30),
            .Minimum = 0,
            .Maximum = 100,
            .DecimalPlaces = 2,
            .Value = 0
        }

        Dim lblNewPrice As New Label With {
            .Text = $"New Price: {currentPrice:C2}",
            .Font = New Font("Segoe UI", 10, FontStyle.Bold),
            .ForeColor = ColorTranslator.FromHtml("#27AE60"),
            .Location = New Point(20, 130),
            .AutoSize = True
        }

        AddHandler txtDiscount.ValueChanged, Sub()
                                                 Dim discountPercent = txtDiscount.Value
                                                 Dim newPrice = currentPrice * (1 - (discountPercent / 100))
                                                 lblNewPrice.Text = $"New Price: {newPrice:C2}"
                                             End Sub

        Dim btnApply As New Button With {
            .Text = "✓ APPLY",
            .Size = New Size(120, 40),
            .Location = New Point(80, 170),
            .BackColor = ColorTranslator.FromHtml("#27AE60"),
            .ForeColor = Color.White,
            .Font = New Font("Segoe UI", 11, FontStyle.Bold),
            .FlatStyle = FlatStyle.Flat
        }
        btnApply.FlatAppearance.BorderSize = 0

        Dim btnCancel As New Button With {
            .Text = "✖ CANCEL",
            .Size = New Size(120, 40),
            .Location = New Point(210, 170),
            .BackColor = ColorTranslator.FromHtml("#E74C3C"),
            .ForeColor = Color.White,
            .Font = New Font("Segoe UI", 11, FontStyle.Bold),
            .FlatStyle = FlatStyle.Flat
        }
        btnCancel.FlatAppearance.BorderSize = 0

        AddHandler btnApply.Click, Sub()
                                       Dim discountPercent = txtDiscount.Value
                                       Dim newPrice = currentPrice * (1 - (discountPercent / 100))

                                       selectedRow.Cells("Price").Value = newPrice
                                       selectedRow.Cells("Total").Value = newPrice * currentQty

                                       ' Store discount percent if column exists
                                       If dgvCart.Columns.Contains("DiscountPercent") Then
                                           selectedRow.Cells("DiscountPercent").Value = discountPercent
                                       End If

                                       CalculateTotals()
                                       UpdateStatusBar($"Discount applied: {discountPercent}% on {productName}")
                                       discountForm.Close()
                                   End Sub

        AddHandler btnCancel.Click, Sub() discountForm.Close()

        discountForm.Controls.AddRange({lblProduct, lblOriginal, lblDiscount, txtDiscount, lblNewPrice, btnApply, btnCancel})
        discountForm.ShowDialog()
    End Sub

    Private Sub RemoveItem()
        ' F7 - Remove item (requires supervisor authorization)
        If dgvCart.SelectedRows.Count = 0 Then
            MessageBox.Show("Please select an item to remove.", "Remove Item", MessageBoxButtons.OK, MessageBoxIcon.Information)
            Return
        End If

        Dim selectedRow = dgvCart.SelectedRows(0)
        Dim productName = selectedRow.Cells("Product").Value.ToString()

        ' Confirm removal
        Dim result = MessageBox.Show($"Remove '{productName}' from cart?", "Remove Item", MessageBoxButtons.YesNo, MessageBoxIcon.Question)
        If result <> DialogResult.Yes Then Return

        ' Supervisor Authorization
        Dim supervisorUsername = InputBox("Enter Retail Supervisor Username:", "Remove Item Authorization")
        If String.IsNullOrWhiteSpace(supervisorUsername) Then Return

        Dim supervisorPassword As String = ""
        Using pwdForm As New PasswordInputForm("Enter Retail Supervisor Password:", "Remove Item Authorization")
            If pwdForm.ShowDialog() <> DialogResult.OK Then Return
            supervisorPassword = pwdForm.Password
        End Using

        If String.IsNullOrWhiteSpace(supervisorPassword) Then Return

        ' Validate supervisor credentials
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
            End Using

            ' Authorization successful - remove item
            dgvCart.Rows.Remove(selectedRow)
            CalculateTotals()
            UpdateStatusBar($"Removed: {productName} (Authorized by: {supervisorUsername})")

        Catch ex As Exception
            MessageBox.Show($"Authorization error: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    Private Sub ProcessReturn()
        ' Step 1: Show choice dialog - With Receipt or No Receipt
        Dim choiceForm As New Form With {
            .Text = "Return Method",
            .Size = New Size(500, 300),
            .StartPosition = FormStartPosition.CenterParent,
            .FormBorderStyle = FormBorderStyle.FixedDialog,
            .MaximizeBox = False,
            .MinimizeBox = False,
            .BackColor = _ironDark
        }

        Dim lblTitle As New Label With {
            .Text = "How would you like to process the return?",
            .Font = New Font("Segoe UI", 14, FontStyle.Bold),
            .ForeColor = _ironGold,
            .Location = New Point(20, 20),
            .Size = New Size(460, 40),
            .TextAlign = ContentAlignment.MiddleCenter
        }

        Dim btnWithReceipt As New Button With {
            .Text = "📄 WITH RECEIPT" & vbCrLf & "(Scan Invoice Barcode)",
            .Font = New Font("Segoe UI", 12, FontStyle.Bold),
            .Size = New Size(200, 100),
            .Location = New Point(30, 80),
            .BackColor = _green,
            .ForeColor = Color.White,
            .FlatStyle = FlatStyle.Flat,
            .Cursor = Cursors.Hand
        }
        btnWithReceipt.FlatAppearance.BorderSize = 0

        Dim btnNoReceipt As New Button With {
            .Text = "🔍 NO RECEIPT" & vbCrLf & "(Scan Item Barcodes)",
            .Font = New Font("Segoe UI", 12, FontStyle.Bold),
            .Size = New Size(200, 100),
            .Location = New Point(250, 80),
            .BackColor = _orange,
            .ForeColor = Color.White,
            .FlatStyle = FlatStyle.Flat,
            .Cursor = Cursors.Hand
        }
        btnNoReceipt.FlatAppearance.BorderSize = 0

        Dim btnCancel As New Button With {
            .Text = "Cancel",
            .Font = New Font("Segoe UI", 10, FontStyle.Bold),
            .Size = New Size(100, 40),
            .Location = New Point(190, 200),
            .BackColor = Color.Gray,
            .ForeColor = Color.White,
            .FlatStyle = FlatStyle.Flat,
            .Cursor = Cursors.Hand
        }
        btnCancel.FlatAppearance.BorderSize = 0

        Dim selectedOption As String = ""

        AddHandler btnWithReceipt.Click, Sub()
                                             selectedOption = "withreceipt"
                                             choiceForm.DialogResult = DialogResult.OK
                                             choiceForm.Close()
                                         End Sub

        AddHandler btnNoReceipt.Click, Sub()
                                           selectedOption = "noreceipt"
                                           choiceForm.DialogResult = DialogResult.OK
                                           choiceForm.Close()
                                       End Sub

        AddHandler btnCancel.Click, Sub()
                                        choiceForm.DialogResult = DialogResult.Cancel
                                        choiceForm.Close()
                                    End Sub

        choiceForm.Controls.AddRange({lblTitle, btnWithReceipt, btnNoReceipt, btnCancel})

        If choiceForm.ShowDialog() <> DialogResult.OK Then Return

        ' Route to appropriate workflow
        If selectedOption = "withreceipt" Then
            ProcessReturnWithReceipt()
        ElseIf selectedOption = "noreceipt" Then
            ProcessReturnNoReceipt()
        End If
    End Sub

    Private Sub ProcessReturnWithReceipt()
        ' Scan receipt barcode using colorful barcode scanner dialog
        Try
            ' Get supervisor authorization first
            Dim supervisorID As Integer = 0
            Dim supervisorUsername = InputBox("Enter Supervisor Username:", "Authorization Required")
            If String.IsNullOrWhiteSpace(supervisorUsername) Then Return

            Dim supervisorPassword As String = ""
            Using pwdForm As New PasswordInputForm("Enter Supervisor Password:", "Authorization Required")
                If pwdForm.ShowDialog() <> DialogResult.OK Then Return
                supervisorPassword = pwdForm.Password
            End Using

            If String.IsNullOrWhiteSpace(supervisorPassword) Then Return

            ' Validate supervisor credentials
            Using conn As New SqlConnection(_connectionString)
                conn.Open()
                Dim sql = "SELECT UserID FROM Users u INNER JOIN Roles r ON u.RoleID = r.RoleID WHERE u.Username = @Username AND u.Password = @Password AND r.RoleName IN ('Retail Supervisor', 'Manager', 'Admin') AND u.IsActive = 1"
                Using cmd As New SqlCommand(sql, conn)
                    cmd.Parameters.AddWithValue("@Username", supervisorUsername)
                    cmd.Parameters.AddWithValue("@Password", supervisorPassword)
                    Dim result = cmd.ExecuteScalar()
                    If result Is Nothing Then
                        MessageBox.Show("Invalid supervisor credentials!", "Authorization Failed", MessageBoxButtons.OK, MessageBoxIcon.Error)
                        Return
                    End If
                    supervisorID = CInt(result)
                End Using
            End Using

            ' Scan invoice/order barcode using colorful barcode scanner dialog
            Dim scannedNumber As String = ""
            Using barcodeDialog As New BarcodeScannerDialog("SCAN RECEIPT BARCODE", "Scan the barcode from the sales receipt or order slip")
                If barcodeDialog.ShowDialog() <> DialogResult.OK Then Return
                scannedNumber = barcodeDialog.ScannedBarcode
            End Using

            If String.IsNullOrWhiteSpace(scannedNumber) Then Return

            ' Check what type of receipt was scanned: Regular Sale, Cake Order, or User-Defined Order
            Dim orderType As String = ""
            Dim orderFound As Boolean = False
            
            Using conn As New SqlConnection(_connectionString)
                conn.Open()
                
                ' 1. Check if it's a regular sale invoice
                Dim sqlSale = "SELECT SaleID, InvoiceNumber, TotalAmount FROM Demo_Sales WHERE InvoiceNumber = @Number"
                Using cmd As New SqlCommand(sqlSale, conn)
                    cmd.Parameters.AddWithValue("@Number", scannedNumber.Trim())
                    Using reader = cmd.ExecuteReader()
                        If reader.Read() Then
                            orderFound = True
                            orderType = "RegularSale"
                        End If
                    End Using
                End Using
                
                ' 2. Check if it's a cake order
                If Not orderFound Then
                    Dim sqlCake = "SELECT OrderID, OrderNumber FROM POS_CustomOrders WHERE OrderNumber = @Number AND OrderStatus = 'Delivered'"
                    Using cmd As New SqlCommand(sqlCake, conn)
                        cmd.Parameters.AddWithValue("@Number", scannedNumber.Trim())
                        Using reader = cmd.ExecuteReader()
                            If reader.Read() Then
                                orderFound = True
                                orderType = "CakeOrder"
                            End If
                        End Using
                    End Using
                End If
                
                ' 3. Check if it's a user-defined order
                If Not orderFound Then
                    Dim sqlUserDefined = "SELECT UserDefinedOrderID, OrderNumber FROM POS_UserDefinedOrders WHERE OrderNumber = @Number AND Status = 'PickedUp'"
                    Using cmd As New SqlCommand(sqlUserDefined, conn)
                        cmd.Parameters.AddWithValue("@Number", scannedNumber.Trim())
                        Using reader = cmd.ExecuteReader()
                            If reader.Read() Then
                                orderFound = True
                                orderType = "UserDefinedOrder"
                            End If
                        End Using
                    End Using
                End If
            End Using
            
            If Not orderFound Then
                MessageBox.Show("Order/Invoice not found. Please check the barcode and try again.", "Invalid Receipt", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                Return
            End If

            ' Open ReturnLineItemsForm with the scanned number and order type
            Using returnForm As New ReturnLineItemsForm(scannedNumber.Trim(), _branchID, _tillPointID, _cashierID, supervisorID, orderType)
                returnForm.ShowDialog()
            End Using
        Catch ex As Exception
            MessageBox.Show($"Error processing return: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    Private Sub ProcessReturnNoReceipt()
        ' New workflow - No receipt, scan individual item barcodes
        Try
            Using returnForm As New NoReceiptReturnForm(_branchID, _tillPointID, _cashierID, _cashierName, _connectionString)
                returnForm.ShowDialog()
            End Using
        Catch ex As Exception
            MessageBox.Show($"Error processing return: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    Private Sub CakeOrder()
        Try
            Debug.WriteLine("CakeOrder method called")
            Debug.WriteLine($"BranchID: {_branchID}, TillPointID: {_tillPointID}, CashierID: {_cashierID}, CashierName: {_cashierName}")

            ' Get branch details
            Dim branchDetails = GetBranchDetails()

            Dim cakeForm As New CakeOrderFormNew(_branchID, _tillPointID, _cashierID, _cashierName,
                                                 branchDetails.Name, branchDetails.Address, branchDetails.Phone)
            cakeForm.ShowDialog()
        Catch ex As Exception
            MessageBox.Show($"Error opening cake order: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
            Debug.WriteLine($"Error: {ex.Message}")
            Debug.WriteLine($"Stack: {ex.StackTrace}")
        End Try
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

        ' Calculate totals - prices are VAT INCLUSIVE
        CalculateTotals()
        Dim totalInclVAT As Decimal = 0
        For Each row As DataRow In _cartItems.Rows
            totalInclVAT += CDec(row("Total"))
        Next

        ' Work backwards from inclusive price to get Ex VAT and VAT amount
        Dim totalExVAT = Math.Round(totalInclVAT / 1.15D, 2)
        Dim vatAmount = totalInclVAT - totalExVAT

        ' Get branch prefix
        Dim branchPrefix = GetBranchPrefix()

        ' Show payment tender form (subtotal = Ex VAT, tax = VAT amount, total = Incl VAT)
        Using paymentForm As New PaymentTenderForm(_cashierID, _cashierName, _branchID, _tillPointID, branchPrefix, _cartItems, totalExVAT, vatAmount, totalInclVAT, _isOrderCollectionMode, _collectionOrderID, _collectionOrderNumber, "", "")
            If paymentForm.ShowDialog(Me) = DialogResult.OK Then
                ' If order collection, mark as delivered
                If _isOrderCollectionMode Then
                    MarkOrderAsDelivered()
                    _isOrderCollectionMode = False
                    _collectionOrderID = 0
                    _collectionOrderNumber = ""
                End If

                ' Clear cart and show categories
                _cartItems.Rows.Clear()
                CalculateTotals()
                LoadCategories()
            End If
        End Using
    End Sub

    Private Sub MarkOrderAsDelivered()
        Try
            Using conn As New SqlConnection(_connectionString)
                conn.Open()
                Dim cmd As New SqlCommand("UPDATE POS_CustomOrders SET OrderStatus = 'Delivered', DeliveredDate = GETDATE() WHERE OrderID = @orderID", conn)
                cmd.Parameters.AddWithValue("@orderID", _collectionOrderID)
                cmd.ExecuteNonQuery()
            End Using
        Catch ex As Exception
            MessageBox.Show($"Error marking order as delivered: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
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

        ' Reset idle timer when user interacts
        ResetIdleTimer()

        ' Show categories when idle screen is dismissed
        If _currentView = "categories" AndAlso flpProducts.Controls.Count = 0 Then
            ShowCategories()
        End If

        ' Auto-focus barcode scanner for immediate scanning
        If txtBarcodeScanner IsNot Nothing Then
            txtBarcodeScanner.Focus()
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

    Private Sub PerformLogout(sender As Object, e As EventArgs)
        Try
            ' Confirm logout
            Dim result = MessageBox.Show("Are you sure you want to logout?", "Confirm Logout", MessageBoxButtons.YesNo, MessageBoxIcon.Question)
            
            If result = DialogResult.Yes Then
                ' Hide this form
                Me.Hide()
                
                ' Show login form
                Dim loginForm As New LoginForm()
                loginForm.ShowDialog()
                
                ' Close this form after login form is closed
                Me.Close()
            End If
        Catch ex As Exception
            MessageBox.Show($"Error during logout: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
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


                ' Get sales, returns, and order deposits/collections for this cashier and till for TODAY ONLY
                Dim sql = "
                    SELECT 
                        (SELECT COUNT(*) FROM Demo_Sales WHERE CashierID = @CashierID AND BranchID = @BranchID AND TillPointID = @TillPointID AND CAST(SaleDate AS DATE) = CAST(GETDATE() AS DATE) AND SaleType = 'Sale') AS TotalTransactions,
                        ISNULL((SELECT SUM(Subtotal) FROM Demo_Sales WHERE CashierID = @CashierID AND BranchID = @BranchID AND TillPointID = @TillPointID AND CAST(SaleDate AS DATE) = CAST(GETDATE() AS DATE) AND SaleType = 'Sale'), 0) AS TotalSubtotal,
                        ISNULL((SELECT SUM(TaxAmount) FROM Demo_Sales WHERE CashierID = @CashierID AND BranchID = @BranchID AND TillPointID = @TillPointID AND CAST(SaleDate AS DATE) = CAST(GETDATE() AS DATE) AND SaleType = 'Sale'), 0) AS TotalTax,
                        ISNULL((SELECT SUM(TotalAmount) FROM Demo_Sales WHERE CashierID = @CashierID AND BranchID = @BranchID AND TillPointID = @TillPointID AND CAST(SaleDate AS DATE) = CAST(GETDATE() AS DATE) AND SaleType = 'Sale'), 0) - 
                        ISNULL((SELECT SUM(TotalAmount) FROM Demo_Returns WHERE CashierID = @CashierID AND CAST(ReturnDate AS DATE) = CAST(GETDATE() AS DATE)), 0) AS TotalSales,
                        (SELECT MIN(SaleDate) FROM Demo_Sales WHERE CashierID = @CashierID AND BranchID = @BranchID AND TillPointID = @TillPointID AND CAST(SaleDate AS DATE) = CAST(GETDATE() AS DATE) AND SaleType = 'Sale') AS FirstSale,
                        (SELECT MAX(SaleDate) FROM Demo_Sales WHERE CashierID = @CashierID AND BranchID = @BranchID AND TillPointID = @TillPointID AND CAST(SaleDate AS DATE) = CAST(GETDATE() AS DATE) AND SaleType = 'Sale') AS LastSale,
                        ISNULL((SELECT SUM(CashAmount) FROM Demo_Sales WHERE CashierID = @CashierID AND BranchID = @BranchID AND TillPointID = @TillPointID AND CAST(SaleDate AS DATE) = CAST(GETDATE() AS DATE) AND SaleType = 'Sale'), 0) - 
                        ISNULL((SELECT SUM(CashAmount) FROM POS_Returns WHERE CashierID = @CashierID AND BranchID = @BranchID AND TillPointID = @TillPointID AND CAST(ReturnDate AS DATE) = CAST(GETDATE() AS DATE)), 0) AS TotalCash,
                        ISNULL((SELECT SUM(CardAmount) FROM Demo_Sales WHERE CashierID = @CashierID AND BranchID = @BranchID AND TillPointID = @TillPointID AND CAST(SaleDate AS DATE) = CAST(GETDATE() AS DATE) AND SaleType = 'Sale'), 0) - 
                        ISNULL((SELECT SUM(CardAmount) FROM POS_Returns WHERE CashierID = @CashierID AND BranchID = @BranchID AND TillPointID = @TillPointID AND CAST(ReturnDate AS DATE) = CAST(GETDATE() AS DATE)), 0) AS TotalCard,
                        ISNULL((SELECT COUNT(*) FROM POS_Returns WHERE CashierID = @CashierID AND BranchID = @BranchID AND TillPointID = @TillPointID AND CAST(ReturnDate AS DATE) = CAST(GETDATE() AS DATE)), 0) AS TotalReturns,
                        ISNULL((SELECT SUM(TotalAmount) FROM POS_Returns WHERE CashierID = @CashierID AND BranchID = @BranchID AND TillPointID = @TillPointID AND CAST(ReturnDate AS DATE) = CAST(GETDATE() AS DATE)), 0) AS TotalReturnAmount,
                        -- Cake Orders (OrderType='Cake')
                        ISNULL((SELECT COUNT(*) FROM Demo_Sales s INNER JOIN POS_CustomOrders o ON s.ReferenceNumber = o.OrderNumber WHERE s.CashierID = @CashierID AND s.BranchID = @BranchID AND s.TillPointID = @TillPointID AND CAST(s.SaleDate AS DATE) = CAST(GETDATE() AS DATE) AND s.SaleType IN ('OrderDeposit', 'OrderCollection') AND o.OrderType = 'Cake'), 0) AS CakeOrderTransactions,
                        ISNULL((SELECT SUM(s.TotalAmount) FROM Demo_Sales s INNER JOIN POS_CustomOrders o ON s.ReferenceNumber = o.OrderNumber WHERE s.CashierID = @CashierID AND s.BranchID = @BranchID AND s.TillPointID = @TillPointID AND CAST(s.SaleDate AS DATE) = CAST(GETDATE() AS DATE) AND s.SaleType = 'OrderDeposit' AND o.OrderType = 'Cake'), 0) AS CakeOrderDeposits,
                        ISNULL((SELECT SUM(s.TotalAmount) FROM Demo_Sales s INNER JOIN POS_CustomOrders o ON s.ReferenceNumber = o.OrderNumber WHERE s.CashierID = @CashierID AND s.BranchID = @BranchID AND s.TillPointID = @TillPointID AND CAST(s.SaleDate AS DATE) = CAST(GETDATE() AS DATE) AND s.SaleType = 'OrderCollection' AND o.OrderType = 'Cake'), 0) AS CakeOrderCollections,
                        -- General Orders (OrderType='Order')
                        ISNULL((SELECT COUNT(*) FROM Demo_Sales s INNER JOIN POS_CustomOrders o ON s.ReferenceNumber = o.OrderNumber WHERE s.CashierID = @CashierID AND s.BranchID = @BranchID AND s.TillPointID = @TillPointID AND CAST(s.SaleDate AS DATE) = CAST(GETDATE() AS DATE) AND s.SaleType IN ('OrderDeposit', 'OrderCollection') AND o.OrderType = 'Order'), 0) AS GeneralOrderTransactions,
                        ISNULL((SELECT SUM(s.TotalAmount) FROM Demo_Sales s INNER JOIN POS_CustomOrders o ON s.ReferenceNumber = o.OrderNumber WHERE s.CashierID = @CashierID AND s.BranchID = @BranchID AND s.TillPointID = @TillPointID AND CAST(s.SaleDate AS DATE) = CAST(GETDATE() AS DATE) AND s.SaleType = 'OrderDeposit' AND o.OrderType = 'Order'), 0) AS GeneralOrderDeposits,
                        ISNULL((SELECT SUM(s.TotalAmount) FROM Demo_Sales s INNER JOIN POS_CustomOrders o ON s.ReferenceNumber = o.OrderNumber WHERE s.CashierID = @CashierID AND s.BranchID = @BranchID AND s.TillPointID = @TillPointID AND CAST(s.SaleDate AS DATE) = CAST(GETDATE() AS DATE) AND s.SaleType = 'OrderCollection' AND o.OrderType = 'Order'), 0) AS GeneralOrderCollections,
                        -- Cash amounts for orders (for Total Cash in Till calculation)
                        ISNULL((SELECT SUM(s.CashAmount) FROM Demo_Sales s INNER JOIN POS_CustomOrders o ON s.ReferenceNumber = o.OrderNumber WHERE s.CashierID = @CashierID AND s.BranchID = @BranchID AND s.TillPointID = @TillPointID AND CAST(s.SaleDate AS DATE) = CAST(GETDATE() AS DATE) AND s.SaleType IN ('OrderDeposit', 'OrderCollection') AND o.OrderType = 'Cake'), 0) AS CakeOrderCash,
                        ISNULL((SELECT SUM(s.CashAmount) FROM Demo_Sales s INNER JOIN POS_CustomOrders o ON s.ReferenceNumber = o.OrderNumber WHERE s.CashierID = @CashierID AND s.BranchID = @BranchID AND s.TillPointID = @TillPointID AND CAST(s.SaleDate AS DATE) = CAST(GETDATE() AS DATE) AND s.SaleType IN ('OrderDeposit', 'OrderCollection') AND o.OrderType = 'Order'), 0) AS GeneralOrderCash,
                        -- Card amounts for orders
                        ISNULL((SELECT SUM(s.CardAmount) FROM Demo_Sales s INNER JOIN POS_CustomOrders o ON s.ReferenceNumber = o.OrderNumber WHERE s.CashierID = @CashierID AND s.BranchID = @BranchID AND s.TillPointID = @TillPointID AND CAST(s.SaleDate AS DATE) = CAST(GETDATE() AS DATE) AND s.SaleType IN ('OrderDeposit', 'OrderCollection') AND o.OrderType = 'Cake'), 0) AS CakeOrderCard,
                        ISNULL((SELECT SUM(s.CardAmount) FROM Demo_Sales s INNER JOIN POS_CustomOrders o ON s.ReferenceNumber = o.OrderNumber WHERE s.CashierID = @CashierID AND s.BranchID = @BranchID AND s.TillPointID = @TillPointID AND CAST(s.SaleDate AS DATE) = CAST(GETDATE() AS DATE) AND s.SaleType IN ('OrderDeposit', 'OrderCollection') AND o.OrderType = 'Order'), 0) AS GeneralOrderCard"

                Using cmd As New SqlCommand(sql, conn)
                    cmd.Parameters.AddWithValue("@CashierID", _cashierID)
                    cmd.Parameters.AddWithValue("@BranchID", _branchID)
                    cmd.Parameters.AddWithValue("@TillPointID", _tillPointID)

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

        ' Cake order data
        Dim cakeOrderTransactions = If(IsDBNull(row("CakeOrderTransactions")), 0, CInt(row("CakeOrderTransactions")))
        Dim cakeOrderDeposits = If(IsDBNull(row("CakeOrderDeposits")), 0D, CDec(row("CakeOrderDeposits")))
        Dim cakeOrderCollections = If(IsDBNull(row("CakeOrderCollections")), 0D, CDec(row("CakeOrderCollections")))

        ' General order data
        Dim generalOrderTransactions = If(IsDBNull(row("GeneralOrderTransactions")), 0, CInt(row("GeneralOrderTransactions")))
        Dim generalOrderDeposits = If(IsDBNull(row("GeneralOrderDeposits")), 0D, CDec(row("GeneralOrderDeposits")))
        Dim generalOrderCollections = If(IsDBNull(row("GeneralOrderCollections")), 0D, CDec(row("GeneralOrderCollections")))

        ' Order cash amounts (for Total Cash in Till)
        Dim cakeOrderCash = If(IsDBNull(row("CakeOrderCash")), 0D, CDec(row("CakeOrderCash")))
        Dim generalOrderCash = If(IsDBNull(row("GeneralOrderCash")), 0D, CDec(row("GeneralOrderCash")))
        Dim totalOrderCash = cakeOrderCash + generalOrderCash

        ' Order card amounts
        Dim cakeOrderCard = If(IsDBNull(row("CakeOrderCard")), 0D, CDec(row("CakeOrderCard")))
        Dim generalOrderCard = If(IsDBNull(row("GeneralOrderCard")), 0D, CDec(row("GeneralOrderCard")))
        Dim totalOrderCard = cakeOrderCard + generalOrderCard

        ' Get Cash Float from TillFloatConfig
        Dim cashFloat As Decimal = 0
        Try
            Using conn As New SqlConnection(_connectionString)
                conn.Open()
                Dim sql = "SELECT FloatAmount FROM TillFloatConfig WHERE BranchID = @BranchID AND TillPointID = @TillPointID AND IsActive = 1"
                Using cmd As New SqlCommand(sql, conn)
                    cmd.Parameters.AddWithValue("@BranchID", _branchID)
                    cmd.Parameters.AddWithValue("@TillPointID", _tillPointID)
                    Dim result = cmd.ExecuteScalar()
                    If result IsNot Nothing Then
                        cashFloat = CDec(result)
                    End If
                End Using
            End Using
        Catch ex As Exception
            ' Default to 0 if not configured
            cashFloat = 0
        End Try

        ' Calculate Total Cash in Till = Cash Float + Cash Sales + Order Cash
        Dim totalCashInTill = cashFloat + totalCash + totalOrderCash

        ' Create cash up form
        Dim cashUpForm As New Form With {
            .Text = "Cash Up Report",
            .Size = New Size(600, 850),
            .StartPosition = FormStartPosition.CenterScreen,
            .BackColor = Color.White,
            .FormBorderStyle = FormBorderStyle.Sizable,
            .MaximizeBox = False,
            .MinimizeBox = False,
            .MinimumSize = New Size(600, 700)
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

        ' Report content - use AutoScroll for long content
        Dim pnlContent As New Panel With {
            .Location = New Point(50, 100),
            .Size = New Size(500, 600),
            .BackColor = Color.White,
            .AutoScroll = True
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

        ' Cake Order Transactions section
        If cakeOrderTransactions > 0 Then
            Dim lblCakeOrdersHeader As New Label With {
                .Text = "CAKE ORDERS:",
                .Font = New Font("Segoe UI", 12, FontStyle.Bold),
                .ForeColor = _orange,
                .Location = New Point(20, yPos),
                .AutoSize = True
            }
            pnlContent.Controls.Add(lblCakeOrdersHeader)
            yPos += 30

            Dim lblCakeOrdersCount As New Label With {
                .Text = $"Order Transactions: {cakeOrderTransactions}",
                .Font = New Font("Segoe UI", 11),
                .ForeColor = _orange,
                .Location = New Point(40, yPos),
                .AutoSize = True
            }
            pnlContent.Controls.Add(lblCakeOrdersCount)
            yPos += 25

            If cakeOrderDeposits > 0 Then
                Dim lblCakeDeposits As New Label With {
                    .Text = $"Deposits Received: R{cakeOrderDeposits:N2}",
                    .Font = New Font("Segoe UI", 11),
                    .ForeColor = _orange,
                    .Location = New Point(40, yPos),
                    .AutoSize = True
                }
                pnlContent.Controls.Add(lblCakeDeposits)
                yPos += 25
            End If

            If cakeOrderCollections > 0 Then
                Dim lblCakeCollections As New Label With {
                    .Text = $"Balance Collected: R{cakeOrderCollections:N2}",
                    .Font = New Font("Segoe UI", 11),
                    .ForeColor = _orange,
                    .Location = New Point(40, yPos),
                    .AutoSize = True
                }
                pnlContent.Controls.Add(lblCakeCollections)
                yPos += 25
            End If

            yPos += 10
        End If

        ' General Order Transactions section
        If generalOrderTransactions > 0 Then
            Dim lblGeneralOrdersHeader As New Label With {
                .Text = "GENERAL ORDERS:",
                .Font = New Font("Segoe UI", 12, FontStyle.Bold),
                .ForeColor = _lightBlue,
                .Location = New Point(20, yPos),
                .AutoSize = True
            }
            pnlContent.Controls.Add(lblGeneralOrdersHeader)
            yPos += 30

            Dim lblGeneralOrdersCount As New Label With {
                .Text = $"Order Transactions: {generalOrderTransactions}",
                .Font = New Font("Segoe UI", 11),
                .ForeColor = _lightBlue,
                .Location = New Point(40, yPos),
                .AutoSize = True
            }
            pnlContent.Controls.Add(lblGeneralOrdersCount)
            yPos += 25

            If generalOrderDeposits > 0 Then
                Dim lblGeneralDeposits As New Label With {
                    .Text = $"Deposits Received: R{generalOrderDeposits:N2}",
                    .Font = New Font("Segoe UI", 11),
                    .ForeColor = _lightBlue,
                    .Location = New Point(40, yPos),
                    .AutoSize = True
                }
                pnlContent.Controls.Add(lblGeneralDeposits)
                yPos += 25
            End If

            If generalOrderCollections > 0 Then
                Dim lblGeneralCollections As New Label With {
                    .Text = $"Balance Collected: R{generalOrderCollections:N2}",
                    .Font = New Font("Segoe UI", 11),
                    .ForeColor = _lightBlue,
                    .Location = New Point(40, yPos),
                    .AutoSize = True
                }
                pnlContent.Controls.Add(lblGeneralCollections)
                yPos += 25
            End If

            yPos += 10
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

        ' PAYMENT BREAKDOWN SECTION
        Dim lblPaymentHeader As New Label With {
            .Text = "PAYMENT BREAKDOWN:",
            .Font = New Font("Segoe UI", 13, FontStyle.Bold),
            .Location = New Point(20, yPos),
            .AutoSize = True
        }
        pnlContent.Controls.Add(lblPaymentHeader)
        yPos += 35

        ' Cash Sales
        Dim lblCash As New Label With {
            .Text = $"💵 Cash (Sales): {totalCash.ToString("C2")}",
            .Font = New Font("Segoe UI", 12),
            .Location = New Point(40, yPos),
            .AutoSize = True
        }
        pnlContent.Controls.Add(lblCash)
        yPos += 30

        ' Cash Orders (if any)
        If totalOrderCash > 0 Then
            Dim lblOrderCashPayment As New Label With {
                .Text = $"💵 Cash (Orders): {totalOrderCash.ToString("C2")}",
                .Font = New Font("Segoe UI", 12),
                .Location = New Point(40, yPos),
                .AutoSize = True
            }
            pnlContent.Controls.Add(lblOrderCashPayment)
            yPos += 30
        End If

        ' Card Sales
        Dim lblCard As New Label With {
            .Text = $"💳 Card (Sales): {totalCard.ToString("C2")}",
            .Font = New Font("Segoe UI", 12),
            .Location = New Point(40, yPos),
            .AutoSize = True
        }
        pnlContent.Controls.Add(lblCard)
        yPos += 30

        ' Card Orders (if any)
        If totalOrderCard > 0 Then
            Dim lblOrderCardPayment As New Label With {
                .Text = $"💳 Card (Orders): {totalOrderCard.ToString("C2")}",
                .Font = New Font("Segoe UI", 12),
                .Location = New Point(40, yPos),
                .AutoSize = True
            }
            pnlContent.Controls.Add(lblOrderCardPayment)
            yPos += 30
        End If

        yPos += 10

        ' Separator before total
        Dim lblSep3 As New Label With {
            .Text = "═══════════════════════════════════",
            .Font = New Font("Courier New", 12),
            .Location = New Point(20, yPos),
            .AutoSize = True
        }
        pnlContent.Controls.Add(lblSep3)
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
        yPos += 50

        ' Separator
        Dim lblSep4 As New Label With {
            .Text = "═══════════════════════════════════",
            .Font = New Font("Courier New", 12),
            .Location = New Point(20, yPos),
            .AutoSize = True
        }
        pnlContent.Controls.Add(lblSep4)
        yPos += 30

        ' CASH FLOAT SECTION
        Dim lblFloatHeader As New Label With {
            .Text = "CASH FLOAT:",
            .Font = New Font("Segoe UI", 13, FontStyle.Bold),
            .ForeColor = Color.DarkBlue,
            .Location = New Point(20, yPos),
            .AutoSize = True
        }
        pnlContent.Controls.Add(lblFloatHeader)
        yPos += 35

        ' Cash Float Amount
        Dim lblCashFloat As New Label With {
            .Text = $"Opening Float: {cashFloat.ToString("C2")}",
            .Font = New Font("Segoe UI", 12),
            .Location = New Point(40, yPos),
            .AutoSize = True
        }
        pnlContent.Controls.Add(lblCashFloat)
        yPos += 30

        ' Cash Sales (repeated for clarity)
        Dim lblCashSalesInTill As New Label With {
            .Text = $"Cash Sales: {totalCash.ToString("C2")}",
            .Font = New Font("Segoe UI", 12),
            .Location = New Point(40, yPos),
            .AutoSize = True
        }
        pnlContent.Controls.Add(lblCashSalesInTill)
        yPos += 30

        ' Order Cash (if any)
        If totalOrderCash > 0 Then
            Dim lblOrderCash As New Label With {
                .Text = $"Order Cash: {totalOrderCash.ToString("C2")}",
                .Font = New Font("Segoe UI", 12),
                .Location = New Point(40, yPos),
                .AutoSize = True
            }
            pnlContent.Controls.Add(lblOrderCash)
            yPos += 30
        Else
            yPos += 10
        End If

        ' Total Cash in Till (BOLD and highlighted)
        Dim lblTotalCashInTill As New Label With {
            .Text = $"💰 TOTAL CASH IN TILL: {totalCashInTill.ToString("C2")}",
            .Font = New Font("Segoe UI", 16, FontStyle.Bold),
            .ForeColor = Color.DarkGreen,
            .Location = New Point(20, yPos),
            .AutoSize = True
        }
        pnlContent.Controls.Add(lblTotalCashInTill)

        ' Button panel
        Dim pnlButtons As New Panel With {
            .Dock = DockStyle.Bottom,
            .Height = 80,
            .BackColor = Color.White
        }

        ' Print Cash-Up Report button
        Dim btnPrint As New Button With {
            .Text = "🖨️ PRINT REPORT",
            .Font = New Font("Segoe UI", 12, FontStyle.Bold),
            .Size = New Size(150, 50),
            .Location = New Point(30, 15),
            .BackColor = _green,
            .ForeColor = Color.White,
            .FlatStyle = FlatStyle.Flat,
            .Cursor = Cursors.Hand
        }
        btnPrint.FlatAppearance.BorderSize = 0
        AddHandler btnPrint.Click, Sub()
                                        PrintCashUpReport(totalCashInTill, cashFloat, totalCash, totalOrderCash, totalCard, totalOrderCard, total, transactions, totalReturns, totalReturnAmount, firstSale, lastSale, cakeOrderTransactions, cakeOrderDeposits, cakeOrderCollections, generalOrderTransactions, generalOrderDeposits, generalOrderCollections)
                                    End Sub

        ' Close button
        Dim btnClose As New Button With {
            .Text = "CLOSE",
            .Font = New Font("Segoe UI", 12, FontStyle.Bold),
            .Size = New Size(150, 50),
            .Location = New Point(190, 15),
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
            .Location = New Point(350, 15),
            .BackColor = _red,
            .ForeColor = Color.White,
            .FlatStyle = FlatStyle.Flat,
            .Cursor = Cursors.Hand
        }
        btnLogout.FlatAppearance.BorderSize = 0
        AddHandler btnLogout.Click, Sub()
                                        cashUpForm.Close()
                                        PerformLogout(Nothing, Nothing)
                                    End Sub

        pnlButtons.Controls.AddRange({btnPrint, btnClose, btnLogout})

        cashUpForm.Controls.AddRange({pnlHeader, pnlContent, pnlButtons})
        cashUpForm.ShowDialog()
    End Sub

    Private Sub PrintCashUpReport(totalCashInTill As Decimal, cashFloat As Decimal, totalCash As Decimal, totalOrderCash As Decimal,
                                   totalCard As Decimal, totalOrderCard As Decimal, total As Decimal, transactions As Integer,
                                   totalReturns As Integer, totalReturnAmount As Decimal, firstSale As DateTime, lastSale As DateTime,
                                   cakeOrderTransactions As Integer, cakeOrderDeposits As Decimal, cakeOrderCollections As Decimal,
                                   generalOrderTransactions As Integer, generalOrderDeposits As Decimal, generalOrderCollections As Decimal)
        Try
            ' Build receipt text for 80mm thermal printer (42 characters wide)
            Dim receipt As New System.Text.StringBuilder()

            receipt.AppendLine("========================================")
            receipt.AppendLine("       OVEN DELIGHTS")
            receipt.AppendLine("       CASH UP REPORT")
            receipt.AppendLine("========================================")
            receipt.AppendLine()
            receipt.AppendLine($"Cashier: {_cashierName}")
            receipt.AppendLine($"Till: {GetTillNumber()}")
            receipt.AppendLine($"Date: {DateTime.Now:dd/MM/yyyy HH:mm}")
            receipt.AppendLine($"Period: {firstSale:HH:mm} - {lastSale:HH:mm}")
            receipt.AppendLine()
            receipt.AppendLine("========================================")
            receipt.AppendLine("SALES SUMMARY")
            receipt.AppendLine("========================================")
            receipt.AppendLine()
            receipt.AppendLine($"Total Transactions:          {transactions,10}")
            receipt.AppendLine()
            
            ' Calculate subtotal and tax from total
            Dim subtotal As Decimal = total / 1.15D
            Dim tax As Decimal = total - subtotal
            
            receipt.AppendLine($"Subtotal:              R{subtotal,14:N2}")
            receipt.AppendLine($"VAT (15%):             R{tax,14:N2}")
            receipt.AppendLine()

            If totalReturns > 0 Then
                receipt.AppendLine("RETURNS:")
                receipt.AppendLine($"  Count:                      {totalReturns,10}")
                receipt.AppendLine($"  Amount:               -R{totalReturnAmount,14:N2}")
                receipt.AppendLine()
            End If

            receipt.AppendLine("----------------------------------------")
            receipt.AppendLine($"TOTAL SALES:           R{total,14:N2}")
            receipt.AppendLine("========================================")
            receipt.AppendLine("PAYMENT BREAKDOWN")
            receipt.AppendLine("========================================")
            receipt.AppendLine()
            receipt.AppendLine($"Cash (Sales):          R{totalCash,14:N2}")
            If totalOrderCash > 0 Then
                receipt.AppendLine($"Cash (Orders):         R{totalOrderCash,14:N2}")
            End If
            receipt.AppendLine($"Card (Sales):          R{totalCard,14:N2}")
            If totalOrderCard > 0 Then
                receipt.AppendLine($"Card (Orders):         R{totalOrderCard,14:N2}")
            End If
            receipt.AppendLine()

            ' Order Details Section
            If cakeOrderTransactions > 0 Or generalOrderTransactions > 0 Then
                receipt.AppendLine("========================================")
                receipt.AppendLine("ORDER DETAILS")
                receipt.AppendLine("========================================")
                receipt.AppendLine()
                
                If cakeOrderTransactions > 0 Then
                    receipt.AppendLine("CAKE ORDERS:")
                    receipt.AppendLine($"  Transactions:               {cakeOrderTransactions,10}")
                    receipt.AppendLine($"  Deposits:          R{cakeOrderDeposits,14:N2}")
                    receipt.AppendLine($"  Collections:       R{cakeOrderCollections,14:N2}")
                    receipt.AppendLine()
                End If
                
                If generalOrderTransactions > 0 Then
                    receipt.AppendLine("GENERAL ORDERS:")
                    receipt.AppendLine($"  Transactions:               {generalOrderTransactions,10}")
                    receipt.AppendLine($"  Deposits:          R{generalOrderDeposits,14:N2}")
                    receipt.AppendLine($"  Collections:       R{generalOrderCollections,14:N2}")
                    receipt.AppendLine()
                End If
            End If
            receipt.AppendLine("========================================")
            receipt.AppendLine("CASH FLOAT")
            receipt.AppendLine("========================================")
            receipt.AppendLine()
            receipt.AppendLine($"Opening Float:         R{cashFloat,14:N2}")
            receipt.AppendLine($"Cash Sales:            R{totalCash,14:N2}")
            If totalOrderCash > 0 Then
                receipt.AppendLine($"Order Cash:            R{totalOrderCash,14:N2}")
            End If
            receipt.AppendLine("----------------------------------------")
            receipt.AppendLine($"*** TOTAL CASH IN TILL: R{totalCashInTill,11:N2} ***")
            receipt.AppendLine()
            receipt.AppendLine("========================================")
            receipt.AppendLine($"Printed: {DateTime.Now:dd/MM/yyyy HH:mm:ss}")
            receipt.AppendLine()
            receipt.AppendLine()
            receipt.AppendLine()

            ' Print to default printer (receipt printer)
            Dim printDoc As New System.Drawing.Printing.PrintDocument()
            Dim receiptText As String = receipt.ToString()

            AddHandler printDoc.PrintPage, Sub(sender, e)
                                               ' Use monospaced font for proper alignment
                                               Dim font As New Font("Courier New", 9, FontStyle.Regular)
                                               Dim boldFont As New Font("Courier New", 9, FontStyle.Bold)

                                               ' Draw text with proper formatting
                                               Dim yPos As Single = 10
                                               Dim lineHeight As Single = font.GetHeight(e.Graphics)

                                               For Each line In receiptText.Split(New String() {Environment.NewLine}, StringSplitOptions.None)
                                                   ' Use bold for important lines
                                                   Dim useFont = font
                                                   If line.Contains("***") OrElse line.Contains("TOTAL SALES") OrElse line.Contains("CASH UP REPORT") Then
                                                       useFont = boldFont
                                                   End If

                                                   e.Graphics.DrawString(line, useFont, Brushes.Black, 10, yPos)
                                                   yPos += lineHeight
                                               Next
                                           End Sub

            printDoc.Print()

            MessageBox.Show("Cash Up Report sent to printer!", "Print", MessageBoxButtons.OK, MessageBoxIcon.Information)

        Catch ex As Exception
            MessageBox.Show($"Print error: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub


    Private Sub SearchProducts(searchText As String)
        Try
            Dim startTime = DateTime.Now
            Debug.WriteLine($"[CACHED CODE SEARCH] Searching: '{searchText}' in {_allProducts.Rows.Count} products")

            flpProducts.SuspendLayout()
            flpProducts.Controls.Clear()

            ' Filter cached products by ItemCode OR Barcode OR ProductName - INSTANT!
            Dim allMatches = _allProducts.AsEnumerable().
                Where(Function(row)
                          Dim itemCode = row("ItemCode").ToString()
                          Dim barcode = row("Barcode").ToString()
                          Dim productName = row("ProductName").ToString()
                          Return itemCode.IndexOf(searchText, StringComparison.OrdinalIgnoreCase) >= 0 OrElse
                                 barcode.IndexOf(searchText, StringComparison.OrdinalIgnoreCase) >= 0 OrElse
                                 productName.IndexOf(searchText, StringComparison.OrdinalIgnoreCase) >= 0
                      End Function).
                OrderBy(Function(row)
                            Dim itemCode = row("ItemCode").ToString()
                            Dim barcode = row("Barcode").ToString()
                            Dim productName = row("ProductName").ToString()
                            ' Sort by: 1) Barcode exact match, 2) Code starts with search, 3) Name starts with search, 4) Contains search
                            If barcode.Equals(searchText, StringComparison.OrdinalIgnoreCase) Then
                                Return 0 ' Highest priority - exact barcode match
                            ElseIf itemCode.StartsWith(searchText, StringComparison.OrdinalIgnoreCase) Then
                                Return 1 ' Second priority - code starts with search
                            ElseIf productName.StartsWith(searchText, StringComparison.OrdinalIgnoreCase) Then
                                Return 2 ' Third priority - name starts with search
                            Else
                                Return 3 ' Lower priority - contains search
                            End If
                        End Function).
                ThenBy(Function(row) row("ProductName").ToString()).
                ToList()

            Dim totalMatches = allMatches.Count
            Dim filteredRows = allMatches.Take(50).ToList()

            If totalMatches = 0 Then
                Dim lblNoResults As New Label With {
                    .Text = $"No products found matching '{searchText}'",
                    .Font = New Font("Segoe UI", 14),
                    .ForeColor = _darkGray,
                    .AutoSize = True,
                    .Padding = New Padding(20)
                }
                flpProducts.Controls.Add(lblNoResults)
                flpProducts.ResumeLayout()
                Return
            End If

            ' Show message if results are limited
            If totalMatches > 50 Then
                Dim lblMoreResults As New Label With {
                    .Text = $"Showing 50 of {totalMatches} results. Type more digits to narrow search.",
                    .Font = New Font("Segoe UI", 11, FontStyle.Bold),
                    .ForeColor = _orange,
                    .AutoSize = True,
                    .Padding = New Padding(10),
                    .BackColor = Color.LightYellow
                }
                flpProducts.Controls.Add(lblMoreResults)
            End If

            ' Display filtered products (max 50)
            For Each row As DataRow In filteredRows
                Dim productID = CInt(row("ProductID"))
                Dim itemCode = row("ItemCode").ToString()
                Dim productName = row("ProductName").ToString()
                Dim price = If(IsDBNull(row("SellingPrice")), 0D, CDec(row("SellingPrice")))
                Dim stock = If(IsDBNull(row("QtyOnHand")), 0D, CDec(row("QtyOnHand")))
                Dim reorderLevel = If(IsDBNull(row("ReorderLevel")), 0D, CDec(row("ReorderLevel")))

                Dim card = CreateProductCard(productID, itemCode, productName, price, stock, reorderLevel)
                flpProducts.Controls.Add(card)
            Next

            flpProducts.ResumeLayout()

            Dim elapsed = (DateTime.Now - startTime).TotalMilliseconds
            Debug.WriteLine($"[CACHED CODE SEARCH] Found {filteredRows.Count} products in {elapsed:F0}ms")

        Catch ex As Exception
            flpProducts.ResumeLayout()
            Debug.WriteLine($"Search error: {ex.Message}")
            MessageBox.Show($"Search error: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    Private Sub AddProductByBarcode(barcode As String)
        Try
            Using conn As New SqlConnection(_connectionString)
                conn.Open()

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
                    WHERE drp.SKU = @Barcode
                      AND drp.IsActive = 1
                      AND ISNULL(stock.QtyOnHand, 0) > 0
                      AND ISNULL(price.SellingPrice, 0) > 0"

                Using cmd As New SqlCommand(sql, conn)
                    cmd.Parameters.AddWithValue("@Barcode", barcode)
                    cmd.Parameters.AddWithValue("@BranchID", _branchID)

                    Using reader = cmd.ExecuteReader()
                        If reader.Read() Then
                            Dim productID = CInt(reader("ProductID"))
                            Dim itemCode = reader("ItemCode").ToString()
                            Dim productName = reader("ProductName").ToString()
                            Dim price = CDec(reader("SellingPrice"))

                            ' Add to cart directly
                            AddProductToCart(productID, itemCode, productName, price)
                        Else
                            MessageBox.Show($"Product not found: {barcode}", "Not Found", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                        End If
                    End Using
                End Using
            End Using

        Catch ex As Exception
            MessageBox.Show($"Barcode scan error: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    Private Sub UpdateStatusBar(message As String)
        ' Status bar update - can be enhanced with actual status strip later
        Debug.WriteLine($"[POS Status] {message}")
        ' TODO: Add StatusStrip control and update label when UI is enhanced
    End Sub

    ' F10 - Void Sale (Clear cart and return to categories)
    Private Sub VoidSale()
        Try
            ' Check if there's anything to void
            If _cartItems.Rows.Count = 0 Then
                MessageBox.Show("No items in cart to void.", "Void Sale", MessageBoxButtons.OK, MessageBoxIcon.Information)
                Return
            End If

            ' Require retail manager authentication
            If Not AuthenticateRetailManager() Then
                MessageBox.Show("Void cancelled - Manager authentication required.", "Authentication Failed", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                Return
            End If

            ' Confirm void action
            Dim result = MessageBox.Show($"Void entire sale with {_cartItems.Rows.Count} item(s)?{vbCrLf}{vbCrLf}This action cannot be undone.", "Void Sale", MessageBoxButtons.YesNo, MessageBoxIcon.Warning)

            If result = DialogResult.Yes Then
                ' Clear cart
                _cartItems.Clear()
                CalculateTotals()

                ' Exit order mode if active
                If _isOrderMode Then
                    ExitOrderMode()
                End If

                ' Return to categories
                LoadCategories()

                ' Update breadcrumb
                lblBreadcrumb.Text = "Sale voided - Select category to start new sale"
                lblBreadcrumb.ForeColor = Color.Red

                MessageBox.Show("Sale voided successfully.", "Void Sale", MessageBoxButtons.OK, MessageBoxIcon.Information)
            End If

        Catch ex As Exception
            MessageBox.Show($"Error voiding sale: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    ' F11 - Create Order (NEW FLOW: Categories first, then order info)
    Private _isOrderMode As Boolean = False
    Private _btnAddOrderInfo As Button
    
    ' User Defined Order Mode
    Private _isUserDefinedMode As Boolean = False
    Private _userDefinedOrderData As UserDefinedOrderData
    Private _btnCompleteUserDefined As Button

    Private Sub CreateOrder()
        Try
            ' Enter ORDER MODE - show categories to add items
            _isOrderMode = True

            ' Clear cart if needed
            If _cartItems.Rows.Count > 0 Then
                Dim result = MessageBox.Show("Clear current cart and start new order?", "New Order", MessageBoxButtons.YesNo, MessageBoxIcon.Question)
                If result = DialogResult.No Then
                    Return
                End If
                _cartItems.Clear()
                CalculateTotals()
            End If

            ' Show categories
            ShowCategories()

            ' Update breadcrumb to show order mode
            lblBreadcrumb.Text = "📝 ORDER MODE - Select items and add to basket"
            lblBreadcrumb.ForeColor = _orange

            ' Add "ADD ORDER INFO" button at bottom of cart panel
            ShowAddOrderInfoButton()

        Catch ex As Exception
            MessageBox.Show($"Error entering order mode: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    Private Sub ShowAddOrderInfoButton()
        ' Remove existing button if present
        If _btnAddOrderInfo IsNot Nothing AndAlso pnlCart.Controls.Contains(_btnAddOrderInfo) Then
            pnlCart.Controls.Remove(_btnAddOrderInfo)
        End If

        ' Hide Pay Now button in order mode
        Dim btnPayNow = pnlCart.Controls.Find("btnPayNow", True).FirstOrDefault()
        If btnPayNow IsNot Nothing Then
            btnPayNow.Visible = False
        End If

        ' Create big bold "ADD ORDER INFO" button at bottom of cart
        _btnAddOrderInfo = New Button With {
            .Text = "📝 ADD ORDER INFO",
            .Dock = DockStyle.Bottom,
            .Height = 80,
            .BackColor = _orange,
            .ForeColor = Color.White,
            .Font = New Font("Segoe UI", 24, FontStyle.Bold),
            .FlatStyle = FlatStyle.Flat,
            .Cursor = Cursors.Hand
        }
        _btnAddOrderInfo.FlatAppearance.BorderSize = 0
        AddHandler _btnAddOrderInfo.Click, Sub() ProcessOrderInfo()

        pnlCart.Controls.Add(_btnAddOrderInfo)
        _btnAddOrderInfo.BringToFront()
    End Sub

    Private Sub ProcessOrderInfo()
        Try
            ' Get branch details
            Dim branchDetails = GetBranchDetails()

            ' Open new CakeOrderFormNew with cart items pre-populated
            Using cakeForm As New CakeOrderFormNew(_branchID, _tillPointID, _cashierID, _cashierName,
                                                   branchDetails.Name, branchDetails.Address, branchDetails.Phone, _cartItems)
                If cakeForm.ShowDialog() = DialogResult.OK Then
                    ' Order created successfully, clear cart and exit order mode
                    _cartItems.Clear()
                    CalculateTotals()
                    ExitOrderMode()
                End If
            End Using

        Catch ex As Exception
            MessageBox.Show($"Error processing order info: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    Private Sub ExitOrderMode()
        _isOrderMode = False

        ' Remove order info button if it exists
        If _btnAddOrderInfo IsNot Nothing AndAlso pnlCart IsNot Nothing AndAlso pnlCart.Controls.Contains(_btnAddOrderInfo) Then
            pnlCart.Controls.Remove(_btnAddOrderInfo)
        End If

        ' Show Pay Now button again
        If pnlCart IsNot Nothing Then
            Dim btnPayNow = pnlCart.Controls.Find("btnPayNow", True).FirstOrDefault()
            If btnPayNow IsNot Nothing Then
                btnPayNow.Visible = True
            End If
        End If

        ' Reset breadcrumb color
        If lblBreadcrumb IsNot Nothing Then
            lblBreadcrumb.ForeColor = _ironGold
        End If
    End Sub

    ' F1 - Sale Mode (exit order mode and show categories)
    Private Sub SaleMode()
        Try
            ' Exit order mode if active
            If _isOrderMode Then
                ExitOrderMode()
            End If

            ' Clear cart
            _cartItems.Clear()
            CalculateTotals()

            ' Show categories screen
            ShowCategories()

            ' Update breadcrumb
            lblBreadcrumb.Text = "Categories"
            lblBreadcrumb.ForeColor = _ironGold
            
            ' Focus barcode scanner for immediate scanning
            If txtBarcodeScanner IsNot Nothing Then
                txtBarcodeScanner.Focus()
            End If

        Catch ex As Exception
            MessageBox.Show($"Error entering sale mode: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    Private Function CreateOrderInDatabase(customerName As String, customerSurname As String, customerPhone As String, readyDate As DateTime, readyTime As TimeSpan, specialInstructions As String, depositAmount As Decimal, totalAmount As Decimal, Optional colour As String = "", Optional picture As String = "") As String
        Try
            Using conn As New SqlConnection(_connectionString)
                conn.Open()
                Using transaction = conn.BeginTransaction()
                    Try
                        ' Generate order number (GENERAL order for F11)
                        Dim branchPrefix As String = GetBranchPrefix()
                        Dim orderNumber As String = GenerateOrderNumber(conn, transaction, branchPrefix, "ORDER")

                        ' Insert order header
                        Dim sqlOrder As String = "
                            INSERT INTO POS_CustomOrders (
                                OrderNumber, BranchID, OrderType,
                                CustomerName, CustomerSurname, CustomerPhone,
                                OrderDate, ReadyDate, ReadyTime,
                                SpecialInstructions,
                                TotalAmount, DepositPaid, BalanceDue,
                                OrderStatus, CreatedBy
                            ) VALUES (
                                @OrderNumber, @BranchID, 'Order',
                                @CustomerName, @CustomerSurname, @CustomerPhone,
                                GETDATE(), @ReadyDate, @ReadyTime,
                                @SpecialInstructions,
                                @TotalAmount, @DepositPaid, @BalanceDue,
                                'New', @CreatedBy
                            );
                            SELECT SCOPE_IDENTITY();"

                        Dim orderID As Integer
                        Using cmd As New SqlCommand(sqlOrder, conn, transaction)
                            cmd.Parameters.AddWithValue("@OrderNumber", orderNumber)
                            cmd.Parameters.AddWithValue("@BranchID", _branchID)
                            cmd.Parameters.AddWithValue("@CustomerName", customerName)
                            cmd.Parameters.AddWithValue("@CustomerSurname", customerSurname)
                            cmd.Parameters.AddWithValue("@CustomerPhone", customerPhone)
                            cmd.Parameters.AddWithValue("@ReadyDate", readyDate)
                            cmd.Parameters.AddWithValue("@ReadyTime", readyTime)
                            cmd.Parameters.AddWithValue("@SpecialInstructions", If(String.IsNullOrWhiteSpace(specialInstructions), DBNull.Value, specialInstructions))
                            cmd.Parameters.AddWithValue("@TotalAmount", totalAmount)
                            cmd.Parameters.AddWithValue("@DepositPaid", depositAmount)
                            cmd.Parameters.AddWithValue("@BalanceDue", totalAmount - depositAmount)
                            cmd.Parameters.AddWithValue("@CreatedBy", _cashierName)

                            orderID = Convert.ToInt32(cmd.ExecuteScalar())
                        End Using

                        ' Insert order items
                        Dim sqlItems As String = "
                            INSERT INTO POS_CustomOrderItems (
                                OrderID, ProductID, ProductName, Quantity, UnitPrice, LineTotal
                            ) VALUES (
                                @OrderID, @ProductID, @ProductName, @Quantity, @UnitPrice, @LineTotal
                            )"

                        ' Insert order items (no manufacturing specs for general orders)
                        For Each row As DataRow In _cartItems.Rows
                            Using cmd As New SqlCommand(sqlItems, conn, transaction)
                                cmd.Parameters.AddWithValue("@OrderID", orderID)
                                cmd.Parameters.AddWithValue("@ProductID", row("ProductID"))
                                cmd.Parameters.AddWithValue("@ProductName", row("Product"))
                                cmd.Parameters.AddWithValue("@Quantity", row("Qty"))
                                cmd.Parameters.AddWithValue("@UnitPrice", row("Price"))
                                cmd.Parameters.AddWithValue("@LineTotal", row("Total"))
                                cmd.ExecuteNonQuery()
                            End Using
                        Next

                        ' No manufacturing instructions for general orders (OrderType='Order')
                        ' Manufacturing instructions only for custom cakes (OrderType='Cake')

                        ' Record deposit payment as OrderDeposit in Demo_Sales
                        Dim sqlDeposit As String = "
                            INSERT INTO Demo_Sales (
                                SaleNumber, InvoiceNumber, BranchID, TillPointID, CashierID, SaleDate,
                                Subtotal, TaxAmount, TotalAmount, PaymentMethod, CashAmount, CardAmount, SaleType, ReferenceNumber
                            ) VALUES (
                                @OrderNumber, @OrderNumber, @BranchID, @TillPointID, @CashierID, GETDATE(),
                                @Subtotal, @TaxAmount, @TotalAmount, 'Cash', @TotalAmount, 0, 'OrderDeposit', @ReferenceNumber
                            )"

                        Using cmd As New SqlCommand(sqlDeposit, conn, transaction)
                            cmd.Parameters.AddWithValue("@OrderNumber", orderNumber)
                            cmd.Parameters.AddWithValue("@BranchID", _branchID)
                            cmd.Parameters.AddWithValue("@TillPointID", _tillPointID)
                            cmd.Parameters.AddWithValue("@CashierID", _cashierID)
                            cmd.Parameters.AddWithValue("@Subtotal", depositAmount / 1.15D)
                            cmd.Parameters.AddWithValue("@TaxAmount", depositAmount - (depositAmount / 1.15D))
                            cmd.Parameters.AddWithValue("@TotalAmount", depositAmount)
                            cmd.Parameters.AddWithValue("@ReferenceNumber", orderNumber)
                            cmd.ExecuteNonQuery()
                        End Using

                        transaction.Commit()
                        Return orderNumber

                    Catch ex As Exception
                        transaction.Rollback()
                        Throw
                    End Try
                End Using
            End Using
        Catch ex As Exception
            MessageBox.Show($"Error saving order: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
            Return Nothing
        End Try
    End Function

    Private Sub PrintOrderReceipt(orderNumber As String, customerName As String, customerSurname As String, customerPhone As String, readyDate As DateTime, readyTime As TimeSpan, collectionDay As String, specialInstructions As String, depositPaid As Decimal, totalAmount As Decimal, Optional colour As String = "", Optional picture As String = "")
        Try
            ' Get branch details
            Dim branchInfo = GetBranchDetails()

            Dim receipt As New System.Text.StringBuilder()
            receipt.AppendLine("========================================")
            receipt.AppendLine("       OVEN DELIGHTS - ORDER RECEIPT")
            receipt.AppendLine("========================================")
            receipt.AppendLine()
            receipt.AppendLine($"Order Number: {orderNumber}")
            receipt.AppendLine($"Date: {DateTime.Now:dd MMM yyyy HH:mm}")
            receipt.AppendLine($"Till: {_tillPointID}  |  Cashier: {_cashierName}")
            receipt.AppendLine()
            receipt.AppendLine("========================================")
            receipt.AppendLine("*** PICKUP LOCATION ***")
            receipt.AppendLine($"{branchInfo.Name}")
            receipt.AppendLine($"{branchInfo.Address}")
            receipt.AppendLine($"Tel: {branchInfo.Phone}")
            receipt.AppendLine("========================================")
            receipt.AppendLine()
            receipt.AppendLine("CUSTOMER DETAILS:")
            receipt.AppendLine($"Name: {customerName} {customerSurname}")
            receipt.AppendLine($"Phone: {customerPhone}")
            receipt.AppendLine()
            receipt.AppendLine("READY FOR COLLECTION:")
            receipt.AppendLine($"Date: {readyDate:dd/MM/yyyy}")
            receipt.AppendLine($"Time: {readyTime:hh\:mm}")
            receipt.AppendLine($"*** {collectionDay.ToUpper()} ***")  ' BOLD DAY OF WEEK
            receipt.AppendLine()

            ' Colour and Picture
            If Not String.IsNullOrWhiteSpace(colour) Then
                receipt.AppendLine("COLOUR:")
                receipt.AppendLine($"  {colour}")
                receipt.AppendLine()
            End If

            If Not String.IsNullOrWhiteSpace(picture) Then
                receipt.AppendLine("PICTURE/DESIGN:")
                receipt.AppendLine($"  {picture}")
                receipt.AppendLine()
            End If

            ' Special Instructions
            If Not String.IsNullOrWhiteSpace(specialInstructions) Then
                receipt.AppendLine("SPECIAL INSTRUCTIONS:")
                receipt.AppendLine($"  {specialInstructions}")
                receipt.AppendLine()
            End If
            receipt.AppendLine("ORDER ITEMS:")
            receipt.AppendLine("----------------------------------------")

            For Each row As DataRow In _cartItems.Rows
                Dim qty = CDec(row("Qty"))
                Dim product = row("Product").ToString()
                Dim price = CDec(row("Price"))
                Dim total = CDec(row("Total"))
                receipt.AppendLine($"{qty:0.00} x {product}")
                receipt.AppendLine($"    @ R{price:N2} = R{total:N2}")
            Next

            receipt.AppendLine("----------------------------------------")
            ' Calculate VAT breakdown (prices are VAT-inclusive)
            Dim subtotalExclVAT = Math.Round(totalAmount / 1.15D, 2)
            Dim vatAmount = Math.Round(totalAmount - subtotalExclVAT, 2)

            receipt.AppendLine($"Subtotal (excl VAT): R{subtotalExclVAT:N2}")
            receipt.AppendLine($"VAT (15%):           R{vatAmount:N2}")
            receipt.AppendLine($"Total Amount:        R{totalAmount:N2}")
            receipt.AppendLine()
            receipt.AppendLine($"Deposit Paid:        R{depositPaid:N2}")
            receipt.AppendLine($"Balance Due:         R{(totalAmount - depositPaid):N2}")
            receipt.AppendLine()
            receipt.AppendLine("========================================")
            receipt.AppendLine("   PLEASE BRING THIS RECEIPT WHEN")
            receipt.AppendLine("       COLLECTING YOUR ORDER")
            receipt.AppendLine("========================================")

            ' Print to thermal slip printer (default POS printer) first
            Try
                PrintOrderReceiptToThermalPrinter(orderNumber, customerName, customerSurname, customerPhone, readyDate, readyTime, collectionDay, specialInstructions, depositPaid, totalAmount, colour, picture)
            Catch ex As Exception
                MessageBox.Show($"Thermal printer error: {ex.Message}", "Print Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            End Try

            ' Then print to continuous feeder printer
            Try
                Dim printer As New POSReceiptPrinter()
                printer.PrintOrderReceipt(orderNumber, customerName, customerSurname, customerPhone, readyDate, readyTime, collectionDay, specialInstructions, depositPaid, totalAmount, colour, picture, _cartItems, _branchID, _cashierName)
            Catch ex As Exception
                MessageBox.Show($"Continuous printer error: {ex.Message}", "Print Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            End Try

            MessageBox.Show("Order receipt printed!", "Print Complete", MessageBoxButtons.OK, MessageBoxIcon.Information)

        Catch ex As Exception
            MessageBox.Show($"Error printing receipt: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    Private Function GetBranchName() As String
        Try
            Using conn As New SqlConnection(_connectionString)
                conn.Open()
                Dim cmd As New SqlCommand("SELECT BranchName FROM Branches WHERE BranchID = @branchID", conn)
                cmd.Parameters.AddWithValue("@branchID", _branchID)
                Dim result = cmd.ExecuteScalar()
                Return If(result IsNot Nothing, result.ToString(), "BRANCH")
            End Using
        Catch
            Return "BRANCH"
        End Try
    End Function

    Private Function GetBranchDetails() As (Name As String, Address As String, Phone As String)
        Try
            Using conn As New SqlConnection(_connectionString)
                conn.Open()
                Dim cmd As New SqlCommand("SELECT BranchName, Address, Phone FROM Branches WHERE BranchID = @branchID", conn)
                cmd.Parameters.AddWithValue("@branchID", _branchID)
                Using reader = cmd.ExecuteReader()
                    If reader.Read() Then
                        Dim name = If(reader("BranchName") IsNot DBNull.Value, reader("BranchName").ToString().Trim(), "BRANCH")
                        Dim address = If(reader("Address") IsNot DBNull.Value, reader("Address").ToString().Trim(), "Address not available")
                        Dim phone = If(reader("Phone") IsNot DBNull.Value, reader("Phone").ToString().Trim(), "Phone not available")
                        Return (name, address, phone)
                    Else
                        System.Diagnostics.Debug.WriteLine($"No branch found for BranchID: {_branchID}")
                    End If
                End Using
            End Using
        Catch ex As Exception
            System.Diagnostics.Debug.WriteLine($"Error getting branch details: {ex.Message}")
        End Try
        Return ("BRANCH", "Address not available", "Phone not available")
    End Function

    Private Function GenerateOrderNumber(conn As SqlConnection, transaction As SqlTransaction, branchPrefix As String, Optional orderType As String = "CAKE") As String
        ' Generate numeric-only order number: BranchID + 4 + 5-digit sequence
        ' Example: Branch 6, sequence 1 -> "640001"
        ' Transaction type codes: 4=Order, (Sales use branchID+sequence, Returns use branchID+4+sequence)
        ' Format: BranchID (1 digit) + Type (1 digit) + Sequence (4 digits) = 6 digits total
        ' Numeric only for better barcode scanning with Free 3 of 9 font

        Dim sql As String = "
            SELECT ISNULL(MAX(CAST(RIGHT(OrderNumber, 4) AS INT)), 0) + 1 
            FROM POS_CustomOrders WITH (TABLOCKX)
            WHERE OrderNumber LIKE @pattern AND LEN(OrderNumber) = 6"

        Dim pattern As String = $"{_branchID}4%"

        Using cmd As New SqlCommand(sql, conn, transaction)
            cmd.Parameters.AddWithValue("@pattern", pattern)
            Dim nextNumber As Integer = Convert.ToInt32(cmd.ExecuteScalar())

            ' Format: BranchID + 4 (Order) + Sequence (4 digits) = 6 digits total
            ' Example: Branch 6, order 1 -> "640001"
            Return $"{_branchID}4{nextNumber.ToString().PadLeft(4, "0"c)}"
        End Using
    End Function

    ' F1 - View Orders
    Private Sub ViewOrders()
        Try
            ' TODO: Add ViewOrdersForm to project file in Visual Studio
            ' Right-click Forms folder → Add → Existing Item → Select ViewOrdersForm.vb and ViewOrdersForm.Designer.vb
            MessageBox.Show("View Orders feature available." & vbCrLf & vbCrLf & "To enable: Add ViewOrdersForm.vb to the project in Visual Studio.", "View Orders", MessageBoxButtons.OK, MessageBoxIcon.Information)

            ' Uncomment when ViewOrdersForm is added to project:
            'Dim frm As New ViewOrdersForm(_branchID)
            'frm.ShowDialog()
        Catch ex As Exception
            MessageBox.Show($"Error opening orders: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    ' F12 - Order Collection
    Private Sub OrderCollection()
        Try
            ' Clear cart if has items
            If _cartItems.Rows.Count > 0 Then
                _cartItems.Clear()
                CalculateTotals()
            End If

            ' Step 1: Show option dialog - Barcode or Cell Number
            Dim optionForm As New Form With {
                .Text = "Order Collection",
                .Size = New Size(500, 300),
                .StartPosition = FormStartPosition.CenterParent,
                .FormBorderStyle = FormBorderStyle.FixedDialog,
                .MaximizeBox = False,
                .MinimizeBox = False,
                .BackColor = _ironDark
            }

            Dim lblTitle As New Label With {
                .Text = "How would you like to collect the order?",
                .Font = New Font("Segoe UI", 14, FontStyle.Bold),
                .ForeColor = _ironGold,
                .Location = New Point(20, 20),
                .Size = New Size(460, 40),
                .TextAlign = ContentAlignment.MiddleCenter
            }

            Dim btnBarcode As New Button With {
                .Text = "📱 SCAN BARCODE" & vbCrLf & "(From Receipt)",
                .Font = New Font("Segoe UI", 12, FontStyle.Bold),
                .Size = New Size(200, 100),
                .Location = New Point(30, 80),
                .BackColor = _green,
                .ForeColor = Color.White,
                .FlatStyle = FlatStyle.Flat,
                .Cursor = Cursors.Hand
            }
            btnBarcode.FlatAppearance.BorderSize = 0

            Dim btnCellNumber As New Button With {
                .Text = "📞 CELL NUMBER" & vbCrLf & "(No Receipt)",
                .Font = New Font("Segoe UI", 12, FontStyle.Bold),
                .Size = New Size(200, 100),
                .Location = New Point(250, 80),
                .BackColor = _orange,
                .ForeColor = Color.White,
                .FlatStyle = FlatStyle.Flat,
                .Cursor = Cursors.Hand
            }
            btnCellNumber.FlatAppearance.BorderSize = 0

            Dim btnCancel As New Button With {
                .Text = "Cancel",
                .Font = New Font("Segoe UI", 10, FontStyle.Bold),
                .Size = New Size(100, 40),
                .Location = New Point(190, 200),
                .BackColor = Color.Gray,
                .ForeColor = Color.White,
                .FlatStyle = FlatStyle.Flat,
                .Cursor = Cursors.Hand
            }
            btnCancel.FlatAppearance.BorderSize = 0

            Dim selectedOption As String = ""

            AddHandler btnBarcode.Click, Sub()
                                             selectedOption = "barcode"
                                             optionForm.DialogResult = DialogResult.OK
                                             optionForm.Close()
                                         End Sub

            AddHandler btnCellNumber.Click, Sub()
                                                selectedOption = "cellnumber"
                                                optionForm.DialogResult = DialogResult.OK
                                                optionForm.Close()
                                            End Sub

            AddHandler btnCancel.Click, Sub()
                                            optionForm.DialogResult = DialogResult.Cancel
                                            optionForm.Close()
                                        End Sub

            optionForm.Controls.AddRange({lblTitle, btnBarcode, btnCellNumber, btnCancel})

            If optionForm.ShowDialog() <> DialogResult.OK Then Return

            ' Step 2: Get order identifier based on selected option
            Dim orderIdentifier As String = ""

            If selectedOption = "barcode" Then
                ' Scan barcode
                Using barcodeDialog As New BarcodeScannerDialog("SCAN ORDER BARCODE", "Scan the barcode from the order receipt")
                    If barcodeDialog.ShowDialog() <> DialogResult.OK Then Return
                    orderIdentifier = barcodeDialog.ScannedBarcode
                End Using

                If String.IsNullOrWhiteSpace(orderIdentifier) Then Return
                LoadOrderForCollection(orderIdentifier)

            ElseIf selectedOption = "cellnumber" Then
                ' Enter cell number
                orderIdentifier = ShowCellNumberInput()
                If String.IsNullOrWhiteSpace(orderIdentifier) Then Return
                LoadOrderForCollectionByCellNumber(orderIdentifier)
            End If

        Catch ex As Exception
            ShowError("Error", ex.Message)
        End Try
    End Sub

    Private Function ShowCellNumberInput() As String
        ' Create touch-friendly cell number input dialog
        Dim inputForm As New Form With {
            .Text = "",
            .Size = New Size(600, 500),
            .StartPosition = FormStartPosition.CenterParent,
            .FormBorderStyle = FormBorderStyle.None,
            .BackColor = _ironDark,
            .ShowInTaskbar = False
        }

        Dim lblTitle As New Label With {
            .Text = "Enter Customer Cell Number",
            .Font = New Font("Segoe UI", 20, FontStyle.Bold),
            .ForeColor = _ironGold,
            .Location = New Point(20, 20),
            .Size = New Size(560, 40),
            .TextAlign = ContentAlignment.MiddleCenter
        }

        Dim txtCellNumber As New TextBox With {
            .Text = "",
            .Font = New Font("Segoe UI", 36, FontStyle.Bold),
            .TextAlign = HorizontalAlignment.Center,
            .Location = New Point(100, 80),
            .Size = New Size(400, 60),
            .BackColor = Color.FromArgb(50, 50, 70),
            .ForeColor = _ironGold,
            .BorderStyle = BorderStyle.FixedSingle,
            .ReadOnly = True
        }

        ' Numpad panel
        Dim pnlNumpad As New Panel With {
            .Location = New Point(100, 160),
            .Size = New Size(400, 220),
            .BackColor = _ironDark
        }

        ' Create numpad buttons (3x4 grid)
        Dim numbers() As String = {"1", "2", "3", "4", "5", "6", "7", "8", "9", "C", "0", "⌫"}
        Dim btnIndex = 0

        For row = 0 To 3
            For col = 0 To 2
                Dim btnText = numbers(btnIndex)
                Dim btn As New Button With {
                    .Text = btnText,
                    .Size = New Size(120, 50),
                    .Location = New Point(col * 130, row * 55),
                    .Font = New Font("Segoe UI", 20, FontStyle.Bold),
                    .BackColor = _ironBlue,
                    .ForeColor = Color.White,
                    .FlatStyle = FlatStyle.Flat,
                    .Cursor = Cursors.Hand,
                    .Tag = btnText
                }
                btn.FlatAppearance.BorderSize = 1
                btn.FlatAppearance.BorderColor = _ironGlow

                AddHandler btn.Click, Sub(s, ev)
                                          Dim clickedBtn = CType(s, Button)
                                          Dim value = clickedBtn.Tag.ToString()

                                          If value = "C" Then
                                              txtCellNumber.Text = ""
                                          ElseIf value = "⌫" Then
                                              If txtCellNumber.Text.Length > 0 Then
                                                  txtCellNumber.Text = txtCellNumber.Text.Substring(0, txtCellNumber.Text.Length - 1)
                                              End If
                                          Else
                                              ' Limit to 10 digits
                                              If txtCellNumber.Text.Length < 10 Then
                                                  txtCellNumber.Text &= value
                                              End If
                                          End If
                                      End Sub

                AddHandler btn.MouseEnter, Sub(s, ev)
                                               Dim hoverBtn = CType(s, Button)
                                               hoverBtn.BackColor = _ironBlueDark
                                               hoverBtn.FlatAppearance.BorderSize = 2
                                               hoverBtn.FlatAppearance.BorderColor = _ironGold
                                           End Sub

                AddHandler btn.MouseLeave, Sub(s, ev)
                                               Dim hoverBtn = CType(s, Button)
                                               hoverBtn.BackColor = _ironBlue
                                               hoverBtn.FlatAppearance.BorderSize = 1
                                               hoverBtn.FlatAppearance.BorderColor = _ironGlow
                                           End Sub

                pnlNumpad.Controls.Add(btn)
                btnIndex += 1
            Next
        Next

        ' OK button
        Dim btnOK As New Button With {
            .Text = "OK",
            .Size = New Size(180, 50),
            .Location = New Point(150, 400),
            .Font = New Font("Segoe UI", 16, FontStyle.Bold),
            .BackColor = _green,
            .ForeColor = Color.White,
            .FlatStyle = FlatStyle.Flat,
            .Cursor = Cursors.Hand
        }
        btnOK.FlatAppearance.BorderSize = 0

        ' Cancel button
        Dim btnCancel As New Button With {
            .Text = "Cancel",
            .Size = New Size(120, 50),
            .Location = New Point(350, 400),
            .Font = New Font("Segoe UI", 16, FontStyle.Bold),
            .BackColor = Color.Gray,
            .ForeColor = Color.White,
            .FlatStyle = FlatStyle.Flat,
            .Cursor = Cursors.Hand
        }
        btnCancel.FlatAppearance.BorderSize = 0

        Dim resultCellNumber As String = ""

        AddHandler btnOK.Click, Sub()
                                    If txtCellNumber.Text.Length >= 10 Then
                                        resultCellNumber = txtCellNumber.Text
                                        inputForm.DialogResult = DialogResult.OK
                                        inputForm.Close()
                                    Else
                                        MessageBox.Show("Please enter a valid 10-digit cell number.", "Invalid Cell Number", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                                    End If
                                End Sub

        AddHandler btnCancel.Click, Sub()
                                        inputForm.DialogResult = DialogResult.Cancel
                                        inputForm.Close()
                                    End Sub

        AddHandler btnOK.MouseEnter, Sub()
                                         btnOK.BackColor = Color.FromArgb(0, 180, 0)
                                     End Sub

        AddHandler btnOK.MouseLeave, Sub()
                                         btnOK.BackColor = _green
                                     End Sub

        AddHandler btnCancel.MouseEnter, Sub()
                                             btnCancel.BackColor = Color.DarkGray
                                         End Sub

        AddHandler btnCancel.MouseLeave, Sub()
                                             btnCancel.BackColor = Color.Gray
                                         End Sub

        inputForm.Controls.AddRange({lblTitle, txtCellNumber, pnlNumpad, btnOK, btnCancel})

        If inputForm.ShowDialog() = DialogResult.OK Then
            Return resultCellNumber
        Else
            Return ""
        End If
    End Function

    Private Sub LoadOrderForCollectionByCellNumber(cellNumber As String)
        Try
            Using conn As New SqlConnection(_connectionString)
                conn.Open()

                ' Find orders by cell number (New or Ready status)
                Dim cmdOrders As New SqlCommand("
                    SELECT OrderID, OrderNumber, CustomerName, CustomerSurname, CustomerPhone,
                           TotalAmount, DepositPaid, BalanceDue, OrderStatus, OrderDate
                    FROM POS_CustomOrders
                    WHERE CustomerPhone = @cellNumber 
                      AND BranchID = @branchId 
                      AND OrderStatus IN ('New', 'Ready')
                    ORDER BY OrderDate DESC", conn)

                cmdOrders.Parameters.AddWithValue("@cellNumber", cellNumber)
                cmdOrders.Parameters.AddWithValue("@branchId", _branchID)

                Dim orders As New DataTable()
                Using adapter As New SqlDataAdapter(cmdOrders)
                    adapter.Fill(orders)
                End Using

                If orders.Rows.Count = 0 Then
                    ShowError("No Orders Found", $"No ready orders found for cell number: {cellNumber}")
                    Return
                ElseIf orders.Rows.Count = 1 Then
                    ' Single order - load it directly
                    Dim orderNumber As String = orders.Rows(0)("OrderNumber").ToString()
                    LoadOrderForCollection(orderNumber)
                Else
                    ' Multiple orders - show selection dialog
                    Dim selectedOrderNumber As String = ShowOrderSelectionDialog(orders)
                    If Not String.IsNullOrWhiteSpace(selectedOrderNumber) Then
                        LoadOrderForCollection(selectedOrderNumber)
                    End If
                End If
            End Using

        Catch ex As Exception
            ShowError("Error", ex.Message)
        End Try
    End Sub

    Private Function ShowOrderSelectionDialog(orders As DataTable) As String
        ' Create dialog to select from multiple orders
        Dim selectForm As New Form With {
            .Text = "Select Order",
            .Size = New Size(600, 500),
            .StartPosition = FormStartPosition.CenterParent,
            .FormBorderStyle = FormBorderStyle.FixedDialog,
            .MaximizeBox = False,
            .MinimizeBox = False,
            .BackColor = _ironDark
        }

        Dim lblTitle As New Label With {
            .Text = "Multiple orders found. Select one:",
            .Font = New Font("Segoe UI", 14, FontStyle.Bold),
            .ForeColor = _ironGold,
            .Location = New Point(20, 20),
            .Size = New Size(560, 30)
        }

        Dim dgvOrders As New DataGridView With {
            .Location = New Point(20, 60),
            .Size = New Size(560, 340),
            .BackgroundColor = Color.FromArgb(30, 35, 50),
            .ForeColor = Color.White,
            .GridColor = Color.FromArgb(60, 70, 90),
            .BorderStyle = BorderStyle.None,
            .AllowUserToAddRows = False,
            .AllowUserToDeleteRows = False,
            .SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            .MultiSelect = False,
            .ReadOnly = True,
            .AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
        }

        ' Add columns
        dgvOrders.Columns.Add("OrderNumber", "Order #")
        dgvOrders.Columns.Add("CustomerName", "Customer")
        dgvOrders.Columns.Add("OrderDate", "Date")
        dgvOrders.Columns.Add("TotalAmount", "Total")

        ' Populate orders
        For Each row As DataRow In orders.Rows
            dgvOrders.Rows.Add(
                row("OrderNumber").ToString(),
                $"{row("CustomerName")} {row("CustomerSurname")}",
                Convert.ToDateTime(row("OrderDate")).ToString("dd/MM/yyyy"),
                CDec(row("TotalAmount")).ToString("C2")
            )
        Next

        Dim btnSelect As New Button With {
            .Text = "Select Order",
            .Location = New Point(350, 420),
            .Size = New Size(120, 40),
            .BackColor = _green,
            .ForeColor = Color.White,
            .FlatStyle = FlatStyle.Flat,
            .Cursor = Cursors.Hand
        }
        btnSelect.FlatAppearance.BorderSize = 0

        Dim btnCancel As New Button With {
            .Text = "Cancel",
            .Location = New Point(480, 420),
            .Size = New Size(100, 40),
            .BackColor = Color.Gray,
            .ForeColor = Color.White,
            .FlatStyle = FlatStyle.Flat,
            .Cursor = Cursors.Hand
        }
        btnCancel.FlatAppearance.BorderSize = 0

        Dim selectedOrderNumber As String = ""

        AddHandler btnSelect.Click, Sub()
                                        If dgvOrders.SelectedRows.Count > 0 Then
                                            selectedOrderNumber = dgvOrders.SelectedRows(0).Cells("OrderNumber").Value.ToString()
                                            selectForm.DialogResult = DialogResult.OK
                                            selectForm.Close()
                                        Else
                                            MessageBox.Show("Please select an order.", "No Selection", MessageBoxButtons.OK, MessageBoxIcon.Information)
                                        End If
                                    End Sub

        AddHandler btnCancel.Click, Sub()
                                        selectForm.DialogResult = DialogResult.Cancel
                                        selectForm.Close()
                                    End Sub

        selectForm.Controls.AddRange({lblTitle, dgvOrders, btnSelect, btnCancel})

        If selectForm.ShowDialog() = DialogResult.OK Then
            Return selectedOrderNumber
        Else
            Return ""
        End If
    End Function

    ' Order collection variables
    Private _isOrderCollectionMode As Boolean = False
    Private _collectionOrderID As Integer = 0
    Private _collectionOrderNumber As String = ""
    Private _collectionDepositPaid As Decimal = 0
    Private _collectionTotalAmount As Decimal = 0
    Private _collectionCustomerName As String = ""

    Private Sub LoadOrderForCollection(orderNumber As String)
        Try
            Using conn As New SqlConnection(_connectionString)
                conn.Open()

                ' Load order
                Dim cmdOrder As New SqlCommand("
                    SELECT OrderID, OrderNumber, CustomerName, CustomerSurname, CustomerPhone,
                           TotalAmount, DepositPaid, BalanceDue, OrderStatus
                    FROM POS_CustomOrders
                    WHERE OrderNumber = @orderNumber AND BranchID = @branchId", conn)

                cmdOrder.Parameters.AddWithValue("@orderNumber", orderNumber)
                cmdOrder.Parameters.AddWithValue("@branchId", _branchID)

                Using reader = cmdOrder.ExecuteReader()
                    If Not reader.Read() Then
                        ShowError("Order Not Found", $"Order {orderNumber} not found at this branch")
                        Return
                    End If

                    Dim orderStatus As String = reader("OrderStatus").ToString()

                    ' Check if order is ready
                    If orderStatus = "New" Then
                        ShowError("Order Not Ready", $"Order {orderNumber} is still in production!" & vbCrLf & vbCrLf & "Please check with manufacturer.")
                        Return
                    ElseIf orderStatus = "Delivered" Then
                        ShowError("Already Collected", $"Order {orderNumber} has already been collected!")
                        Return
                    ElseIf orderStatus = "Cancelled" Then
                        ShowError("Order Cancelled", $"Order {orderNumber} has been cancelled")
                        Return
                    End If

                    ' Order is Ready - proceed
                    _collectionOrderID = Convert.ToInt32(reader("OrderID"))
                    _collectionOrderNumber = reader("OrderNumber").ToString()
                    _collectionTotalAmount = Convert.ToDecimal(reader("TotalAmount"))
                    _collectionDepositPaid = Convert.ToDecimal(reader("DepositPaid"))
                    _collectionCustomerName = $"{reader("CustomerName")} {reader("CustomerSurname")}"
                    Dim balanceDue As Decimal = Convert.ToDecimal(reader("BalanceDue"))
                    Dim customerName As String = _collectionCustomerName
                    Dim customerPhone As String = reader("CustomerPhone").ToString()

                    reader.Close()

                    ' Load order items
                    Dim cmdItems As New SqlCommand("
                        SELECT ProductID, ProductName, Quantity, UnitPrice, LineTotal
                        FROM POS_CustomOrderItems
                        WHERE OrderID = @orderId", conn)

                    cmdItems.Parameters.AddWithValue("@orderId", _collectionOrderID)

                    _cartItems.Clear()

                    Using itemReader = cmdItems.ExecuteReader()
                        While itemReader.Read()
                            Dim newRow = _cartItems.NewRow()
                            newRow("ProductID") = Convert.ToInt32(itemReader("ProductID"))
                            newRow("ItemCode") = ""
                            newRow("Product") = itemReader("ProductName").ToString()
                            newRow("Qty") = Convert.ToDecimal(itemReader("Quantity"))
                            newRow("Price") = Convert.ToDecimal(itemReader("UnitPrice"))
                            newRow("Total") = Convert.ToDecimal(itemReader("LineTotal"))
                            _cartItems.Rows.Add(newRow)
                        End While
                    End Using

                    ' Enable collection mode
                    _isOrderCollectionMode = True

                    ' Update display
                    CalculateTotals()

                    ' Automatically open payment tender for balance (no confirmation dialog needed)
                    If balanceDue > 0 Then
                        ProcessOrderCollectionPayment(balanceDue)
                    Else
                        ' No balance - mark as delivered immediately
                        CompleteOrderCollection()
                    End If
                End Using
            End Using

        Catch ex As Exception
            MessageBox.Show($"Error loading order: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
            _isOrderCollectionMode = False
        End Try
    End Sub

    Private Sub ProcessOrderCollectionPayment(balanceDue As Decimal)
        Try
            ' Open payment tender for balance
            Using paymentForm As New PaymentTenderForm(balanceDue, _branchID, _tillPointID, _cashierID, _cashierName)
                If paymentForm.ShowDialog(Me) = DialogResult.OK Then
                    ' Payment successful - record and complete
                    Dim paymentMethod = paymentForm.PaymentMethod
                    Dim cashAmount = paymentForm.CashAmount
                    Dim cardAmount = paymentForm.CardAmount

                    ' Record balance payment in Demo_Sales
                    Using conn As New SqlConnection(_connectionString)
                        conn.Open()

                        ' Generate unique sale number for balance payment (append -BAL to avoid duplicate)
                        Dim saleNumber As String = _collectionOrderNumber & "-BAL"

                        Dim sqlPayment = "
                            INSERT INTO Demo_Sales (
                                SaleNumber, InvoiceNumber, BranchID, TillPointID, CashierID, SaleDate,
                                Subtotal, TaxAmount, TotalAmount, PaymentMethod, CashAmount, CardAmount, SaleType, ReferenceNumber
                            ) VALUES (
                                @SaleNumber, @OrderNumber, @BranchID, @TillPointID, @CashierID, GETDATE(),
                                @Subtotal, @TaxAmount, @TotalAmount, @PaymentMethod, @CashAmount, @CardAmount, 'OrderCollection', @ReferenceNumber
                            )"

                        Using cmd As New SqlCommand(sqlPayment, conn)
                            cmd.Parameters.AddWithValue("@SaleNumber", saleNumber)
                            cmd.Parameters.AddWithValue("@OrderNumber", _collectionOrderNumber)
                            cmd.Parameters.AddWithValue("@BranchID", _branchID)
                            cmd.Parameters.AddWithValue("@TillPointID", _tillPointID)
                            cmd.Parameters.AddWithValue("@CashierID", _cashierID)
                            cmd.Parameters.AddWithValue("@Subtotal", balanceDue / 1.15D)
                            cmd.Parameters.AddWithValue("@TaxAmount", balanceDue - (balanceDue / 1.15D))
                            cmd.Parameters.AddWithValue("@TotalAmount", balanceDue)
                            cmd.Parameters.AddWithValue("@PaymentMethod", paymentMethod)
                            cmd.Parameters.AddWithValue("@CashAmount", cashAmount)
                            cmd.Parameters.AddWithValue("@CardAmount", cardAmount)
                            cmd.Parameters.AddWithValue("@ReferenceNumber", _collectionOrderNumber)
                            cmd.ExecuteNonQuery()
                        End Using
                    End Using

                    ' Complete the order collection
                    CompleteOrderCollection()

                    ' Print receipt (no extra confirmation needed)
                    PrintCollectionReceipt(_collectionOrderNumber, _collectionCustomerName, balanceDue, _collectionDepositPaid, _collectionTotalAmount, paymentMethod, cashAmount, cardAmount, paymentForm.ChangeAmount)
                Else
                    ' Payment cancelled - silently reset
                    _isOrderCollectionMode = False
                    _cartItems.Clear()
                    CalculateTotals()
                End If
            End Using

        Catch ex As Exception
            MessageBox.Show($"Error processing payment: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
            _isOrderCollectionMode = False
        End Try
    End Sub

    Private Sub CompleteOrderCollection()
        Try
            ' Update order status to Delivered
            Using conn As New SqlConnection(_connectionString)
                conn.Open()

                Dim sqlUpdate = "
                    UPDATE POS_CustomOrders
                    SET OrderStatus = 'Delivered'
                    WHERE OrderID = @orderId"

                Using cmd As New SqlCommand(sqlUpdate, conn)
                    cmd.Parameters.AddWithValue("@orderId", _collectionOrderID)
                    cmd.ExecuteNonQuery()
                End Using
            End Using

            ' Reset collection mode
            _isOrderCollectionMode = False
            _collectionOrderID = 0
            _collectionOrderNumber = ""
            _collectionDepositPaid = 0
            _collectionTotalAmount = 0

            ' Clear cart
            _cartItems.Clear()
            CalculateTotals()

        Catch ex As Exception
            MessageBox.Show($"Error completing collection: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    Private Sub PrintCollectionReceipt(orderNumber As String, customerName As String, balancePaid As Decimal, depositPaid As Decimal, totalAmount As Decimal, paymentMethod As String, cashAmount As Decimal, cardAmount As Decimal, changeAmount As Decimal)
        Try
            ' PRINT TO THERMAL PRINTER FIRST
            PrintCollectionReceiptToThermalPrinter(orderNumber, customerName, balancePaid, depositPaid, totalAmount, paymentMethod, cashAmount, cardAmount, changeAmount)

            ' THEN PRINT TO CONTINUOUS PRINTER (with XY coordinates from database)
            Try
                MessageBox.Show("DEBUG: About to print to continuous printer", "DEBUG", MessageBoxButtons.OK, MessageBoxIcon.Information)
                Dim dualPrinter As New DualReceiptPrinter(_connectionString, _branchID)
                Dim receiptData As New Dictionary(Of String, Object) From {
                    {"InvoiceNumber", orderNumber},
                    {"SaleDateTime", DateTime.Now},
                    {"ChangeAmount", changeAmount},
                    {"BranchName", GetBranchName()},
                    {"TillNumber", "N/A"},
                    {"CashierName", _cashierName},
                    {"PaymentMethod", paymentMethod},
                    {"CashAmount", cashAmount},
                    {"CardAmount", cardAmount},
                    {"Subtotal", totalAmount - (totalAmount * 0.15D)},
                    {"TaxAmount", totalAmount * 0.15D},
                    {"TotalAmount", totalAmount}
                }
                ' Create empty cart items for collection (no items to show)
                Dim emptyCart As New DataTable()
                dualPrinter.PrintToContinuousPrinter(receiptData, emptyCart)
                MessageBox.Show("DEBUG: Continuous printer call completed", "DEBUG", MessageBoxButtons.OK, MessageBoxIcon.Information)
            Catch ex As Exception
                ' Don't block collection if continuous printer fails
                MessageBox.Show($"Continuous printer error: {ex.Message}", "Continuous Printer Error", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            End Try

            ' Create modern receipt display
            Dim receiptForm As New Form With {
                .Text = "Collection Receipt",
                .Size = New Size(500, 650),
                .StartPosition = FormStartPosition.CenterScreen,
                .FormBorderStyle = FormBorderStyle.None,
                .BackColor = Color.White
            }

            ' Header
            Dim pnlHeader As New Panel With {
                .Dock = DockStyle.Top,
                .Height = 80,
                .BackColor = _green
            }

            Dim lblHeader As New Label With {
                .Text = "✓ ORDER COLLECTED",
                .Font = New Font("Segoe UI", 24, FontStyle.Bold),
                .ForeColor = Color.White,
                .TextAlign = ContentAlignment.MiddleCenter,
                .Dock = DockStyle.Fill
            }
            pnlHeader.Controls.Add(lblHeader)

            ' Receipt content
            Dim txtReceipt As New TextBox With {
                .Multiline = True,
                .ReadOnly = True,
                .Font = New Font("Courier New", 12),
                .BorderStyle = BorderStyle.None,
                .Location = New Point(30, 100),
                .Size = New Size(440, 450),
                .BackColor = Color.White
            }

            Dim receipt As New System.Text.StringBuilder()
            receipt.AppendLine("================================")
            receipt.AppendLine("   ORDER COLLECTION RECEIPT")
            receipt.AppendLine("================================")
            receipt.AppendLine()
            receipt.AppendLine($"Order #: {orderNumber}")
            receipt.AppendLine($"Customer: {customerName}")
            receipt.AppendLine($"Date Collected: {DateTime.Now:dd/MM/yyyy HH:mm}")
            receipt.AppendLine($"Cashier: {_cashierName}")
            receipt.AppendLine()
            receipt.AppendLine("PAYMENT SUMMARY:")
            receipt.AppendLine($"Order Total:    R{totalAmount:N2}")
            receipt.AppendLine($"Deposit Paid:   R{depositPaid:N2}")
            receipt.AppendLine($"Balance Paid:   R{balancePaid:N2}")
            receipt.AppendLine()
            receipt.AppendLine("PAYMENT METHOD:")
            receipt.AppendLine($"  {paymentMethod}")
            If cashAmount > 0 Then
                receipt.AppendLine($"  Cash: R{cashAmount:N2}")
            End If
            If cardAmount > 0 Then
                receipt.AppendLine($"  Card: R{cardAmount:N2}")
            End If
            If changeAmount > 0 Then
                receipt.AppendLine($"  Change: R{changeAmount:N2}")
            End If
            receipt.AppendLine()
            receipt.AppendLine("================================")
            receipt.AppendLine("      Thank you!")
            receipt.AppendLine("================================")

            txtReceipt.Text = receipt.ToString()

            ' Close button
            Dim btnClose As New Button With {
                .Text = "✓ CLOSE",
                .Font = New Font("Segoe UI", 14, FontStyle.Bold),
                .Size = New Size(200, 60),
                .Location = New Point(150, 570),
                .BackColor = _green,
                .ForeColor = Color.White,
                .FlatStyle = FlatStyle.Flat,
                .Cursor = Cursors.Hand
            }
            btnClose.FlatAppearance.BorderSize = 0
            AddHandler btnClose.Click, Sub() receiptForm.Close()

            receiptForm.Controls.AddRange({pnlHeader, txtReceipt, btnClose})
            receiptForm.ShowDialog()

        Catch ex As Exception
            ShowError("Error", $"Error printing receipt: {ex.Message}")
        End Try
    End Sub

    Private Sub ShowError(title As String, message As String)
        ' Create modern error dialog
        Dim errorForm As New Form With {
            .Text = title,
            .Size = New Size(500, 300),
            .StartPosition = FormStartPosition.CenterScreen,
            .FormBorderStyle = FormBorderStyle.None,
            .BackColor = Color.White
        }

        ' Header
        Dim pnlHeader As New Panel With {
            .Dock = DockStyle.Top,
            .Height = 80,
            .BackColor = _red
        }

        Dim lblHeader As New Label With {
            .Text = "⚠ " & title.ToUpper(),
            .Font = New Font("Segoe UI", 24, FontStyle.Bold),
            .ForeColor = Color.White,
            .TextAlign = ContentAlignment.MiddleCenter,
            .Dock = DockStyle.Fill
        }
        pnlHeader.Controls.Add(lblHeader)

        ' Message
        Dim lblMessage As New Label With {
            .Text = message,
            .Font = New Font("Segoe UI", 14),
            .ForeColor = _darkBlue,
            .Location = New Point(30, 100),
            .Size = New Size(440, 120),
            .TextAlign = ContentAlignment.MiddleCenter
        }

        ' OK button
        Dim btnOK As New Button With {
            .Text = "OK",
            .Font = New Font("Segoe UI", 14, FontStyle.Bold),
            .Size = New Size(200, 60),
            .Location = New Point(150, 230),
            .BackColor = _red,
            .ForeColor = Color.White,
            .FlatStyle = FlatStyle.Flat,
            .Cursor = Cursors.Hand
        }
        btnOK.FlatAppearance.BorderSize = 0
        AddHandler btnOK.Click, Sub() errorForm.Close()

        errorForm.Controls.AddRange({pnlHeader, lblMessage, btnOK})
        errorForm.ShowDialog()
    End Sub

    ' ============================================
    ' CATEGORY NAVIGATION METHODS - Iron Man Theme
    ' ============================================

    Private Sub Breadcrumb_Click(sender As Object, e As EventArgs)
        ' Clear any search text
        If txtSearch IsNot Nothing Then txtSearch.Text = ""
        If txtSearchByName IsNot Nothing Then txtSearchByName.Text = ""

        ' Parse breadcrumb to determine navigation
        Dim breadcrumbText = lblBreadcrumb.Text

        ' Always go to categories when clicking breadcrumb
        ShowCategories()
    End Sub

    Private Sub ShowCategories()
        ' Clear search boxes when returning to categories
        If txtSearch IsNot Nothing Then txtSearch.Text = ""
        If txtSearchByName IsNot Nothing Then txtSearchByName.Text = ""

        ' Reset idle timer on user activity
        ResetIdleTimer()

        _currentView = "categories"
        _currentCategoryId = 0
        _currentCategoryName = ""
        lblBreadcrumb.Text = "Categories"

        flpProducts.SuspendLayout()
        flpProducts.Controls.Clear()

        Try
            Dim categories = _categoryService.LoadCategories()

            For Each row As DataRow In categories.Rows
                Dim categoryId = CInt(row("CategoryID"))
                Dim categoryName = row("CategoryName").ToString()
                Dim productCount = CInt(row("ProductCount"))

                Dim btn = CreateCategoryTile(categoryId, categoryName, productCount)
                flpProducts.Controls.Add(btn)
            Next

        Catch ex As Exception
            MessageBox.Show($"Error loading categories: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        Finally
            flpProducts.ResumeLayout()
        End Try
    End Sub

    Private Sub ShowSubCategories(categoryId As Integer, categoryName As String)
        ResetIdleTimer() ' Reset on user activity

        _currentView = "subcategories"
        _currentCategoryId = categoryId
        _currentCategoryName = categoryName
        lblBreadcrumb.Text = $"Categories > {categoryName}"

        flpProducts.SuspendLayout()
        flpProducts.Controls.Clear()

        Try
            Dim subcategories = _categoryService.LoadSubCategories(categoryId)

            If subcategories.Rows.Count = 0 Then
                Dim lblNoSubs As New Label With {
                    .Text = $"No subcategories found for {categoryName}",
                    .Font = New Font("Segoe UI", 16, FontStyle.Italic),
                    .ForeColor = _ironGold,
                    .AutoSize = True,
                    .Padding = New Padding(20)
                }
                flpProducts.Controls.Add(lblNoSubs)
            Else
                For Each row As DataRow In subcategories.Rows
                    Dim subCategoryId = CInt(row("SubCategoryID"))
                    Dim subCategoryName = row("SubCategoryName").ToString()
                    Dim productCount = CInt(row("ProductCount"))

                    Dim btn = CreateSubCategoryTile(subCategoryId, subCategoryName, productCount)
                    flpProducts.Controls.Add(btn)
                Next
            End If

        Catch ex As Exception
            MessageBox.Show($"Error loading subcategories: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        Finally
            flpProducts.ResumeLayout()
        End Try
    End Sub

    Private Sub ShowProductsForSubCategory(subCategoryId As Integer, subCategoryName As String)
        ResetIdleTimer() ' Reset on user activity

        _currentView = "products"
        _currentSubCategoryId = subCategoryId
        _currentSubCategoryName = subCategoryName
        lblBreadcrumb.Text = $"Categories > {_currentCategoryName} > {subCategoryName}"

        flpProducts.SuspendLayout()
        flpProducts.Controls.Clear()

        Try
            Dim products = _categoryService.LoadProducts(_currentCategoryId, subCategoryId, _branchID)

            If products.Rows.Count = 0 Then
                Dim lblNoProducts As New Label With {
                    .Text = $"No products found in {subCategoryName}",
                    .Font = New Font("Segoe UI", 16, FontStyle.Italic),
                    .ForeColor = _ironGold,
                    .AutoSize = True,
                    .Padding = New Padding(20)
                }
                flpProducts.Controls.Add(lblNoProducts)
            Else
                For Each row As DataRow In products.Rows
                    Dim productId = CInt(row("ProductID"))
                    Dim productName = row("ProductName").ToString()
                    Dim price = If(IsDBNull(row("SellingPrice")), 0D, CDec(row("SellingPrice")))
                    Dim stock = If(IsDBNull(row("QtyOnHand")), 0D, CDec(row("QtyOnHand")))
                    Dim productCode = If(IsDBNull(row("ItemCode")), row("SKU").ToString(), row("ItemCode").ToString())
                    Dim barcode = If(IsDBNull(row("Barcode")), productCode, row("Barcode").ToString())

                    Dim btn = CreateProductTileNew(productId, productCode, productName, price, stock, barcode)
                    flpProducts.Controls.Add(btn)
                Next
            End If

        Catch ex As Exception
            MessageBox.Show($"Error loading products: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        Finally
            flpProducts.ResumeLayout()
        End Try
    End Sub

    Private Function CreateCategoryTile(categoryId As Integer, categoryName As String, productCount As Integer) As Button
        ' Larger category tile with smaller font to prevent word wrapping
        ' Responsive font size based on text length
        Dim fontSize As Single = If(categoryName.Length > 15, 8, If(categoryName.Length > 10, 9, 10))

        Dim btn As New Button With {
            .Text = categoryName.ToUpper(),
            .Size = New Size(_tileWidth, _tileHeight),
            .Font = New Font("Segoe UI", fontSize, FontStyle.Bold),
            .ForeColor = Color.White,
            .BackColor = Color.FromArgb(180, 193, 39, 45),
            .FlatStyle = FlatStyle.Flat,
            .Cursor = Cursors.Hand,
            .Tag = categoryId,
            .Margin = New Padding(10),
            .TextAlign = ContentAlignment.MiddleCenter,
            .AutoSize = False
        }
        btn.FlatAppearance.BorderSize = 2
        btn.FlatAppearance.BorderColor = Color.FromArgb(255, 215, 0)

        AddHandler btn.Click, Sub() ShowSubCategories(categoryId, categoryName)
        AddHandler btn.MouseEnter, Sub()
                                       btn.BackColor = Color.FromArgb(220, 193, 39, 45)
                                       btn.FlatAppearance.BorderSize = 3
                                       btn.FlatAppearance.BorderColor = _ironGold
                                   End Sub
        AddHandler btn.MouseLeave, Sub()
                                       btn.BackColor = Color.FromArgb(180, 193, 39, 45)
                                       btn.FlatAppearance.BorderSize = 2
                                       btn.FlatAppearance.BorderColor = Color.FromArgb(255, 215, 0)
                                   End Sub

        Return btn
    End Function

    Private Function CreateSubCategoryTile(subCategoryId As Integer, subCategoryName As String, productCount As Integer) As Button
        ' Larger subcategory tile with smaller font to prevent word wrapping
        ' Responsive font size based on text length
        Dim fontSize As Single = If(subCategoryName.Length > 15, 7, If(subCategoryName.Length > 10, 8, 9))

        Dim btn As New Button With {
            .Text = subCategoryName.ToUpper(),
            .Size = New Size(_tileWidth, _tileHeight),
            .Font = New Font("Segoe UI", fontSize, FontStyle.Bold),
            .ForeColor = Color.White,
            .BackColor = Color.FromArgb(180, 0, 150, 255),
            .FlatStyle = FlatStyle.Flat,
            .Cursor = Cursors.Hand,
            .Tag = subCategoryId,
            .Margin = New Padding(10),
            .TextAlign = ContentAlignment.MiddleCenter,
            .AutoSize = False
        }
        btn.FlatAppearance.BorderSize = 2
        btn.FlatAppearance.BorderColor = Color.FromArgb(100, 200, 255)

        AddHandler btn.Click, Sub() ShowProductsForSubCategory(subCategoryId, subCategoryName)
        AddHandler btn.MouseEnter, Sub()
                                       btn.BackColor = Color.FromArgb(220, 0, 150, 255)
                                       btn.FlatAppearance.BorderSize = 3
                                       btn.FlatAppearance.BorderColor = Color.FromArgb(0, 212, 255)
                                   End Sub
        AddHandler btn.MouseLeave, Sub()
                                       btn.BackColor = Color.FromArgb(180, 0, 150, 255)
                                       btn.FlatAppearance.BorderSize = 2
                                       btn.FlatAppearance.BorderColor = Color.FromArgb(100, 200, 255)
                                   End Sub

        Return btn
    End Function

    Private Function CreateProductTileNew(productId As Integer, productCode As String, productName As String, price As Decimal, stock As Decimal, Optional barcode As String = "") As Panel
        ' Compact product tile - fixed size
        Dim pnl As New Panel With {
            .Size = New Size(140, 100),
            .BackColor = Color.White,
            .Cursor = Cursors.Hand,
            .Tag = New With {productId, productCode, productName, price, stock},
            .Margin = New Padding(4),
            .BorderStyle = BorderStyle.FixedSingle
        }

        ' Product Code (top left, small)
        Dim lblCode As New Label With {
            .Text = productCode,
            .Font = New Font("Segoe UI", 8, FontStyle.Bold),
            .ForeColor = _darkBlue,
            .Location = New Point(4, 4),
            .AutoSize = True,
            .BackColor = Color.White
        }

        ' Product Name (center, wrapped)
        Dim lblName As New Label With {
            .Text = productName,
            .Font = New Font("Segoe UI", 9, FontStyle.Bold),
            .ForeColor = _darkBlue,
            .Location = New Point(4, 20),
            .Size = New Size(132, 35),
            .AutoEllipsis = True,
            .BackColor = Color.White
        }

        ' Price (bottom left, green color)
        Dim lblPrice As New Label With {
            .Text = $"R {price:N2}",
            .Font = New Font("Segoe UI", 11, FontStyle.Bold),
            .ForeColor = _green,
            .Location = New Point(4, 65),
            .AutoSize = True,
            .BackColor = Color.White
        }

        ' Stock (bottom right, small)
        Dim lblStock As New Label With {
            .Text = $"Stock: {stock:N0}",
            .Font = New Font("Segoe UI", 7, FontStyle.Regular),
            .ForeColor = If(stock > 0, _darkBlue, Color.Red),
            .Location = New Point(4, 84),
            .AutoSize = True,
            .BackColor = Color.White
        }

        ' Add labels to panel
        pnl.Controls.AddRange({lblCode, lblName, lblPrice, lblStock})

        ' Click handlers - add 1 qty directly to cart
        AddHandler pnl.Click, Sub() AddProductToCart(productId, productCode, productName, price)
        AddHandler lblCode.Click, Sub() AddProductToCart(productId, productCode, productName, price)
        AddHandler lblName.Click, Sub() AddProductToCart(productId, productCode, productName, price)
        AddHandler lblPrice.Click, Sub() AddProductToCart(productId, productCode, productName, price)
        AddHandler lblStock.Click, Sub() AddProductToCart(productId, productCode, productName, price)

        ' Hover effects (light blue on hover)
        AddHandler pnl.MouseEnter, Sub()
                                       Dim hoverColor = ColorTranslator.FromHtml("#E3F2FD")
                                       pnl.BackColor = hoverColor
                                       lblCode.BackColor = hoverColor
                                       lblName.BackColor = hoverColor
                                       lblPrice.BackColor = hoverColor
                                       lblStock.BackColor = hoverColor
                                   End Sub
        AddHandler pnl.MouseLeave, Sub()
                                       pnl.BackColor = Color.White
                                       lblCode.BackColor = Color.White
                                       lblName.BackColor = Color.White
                                       lblPrice.BackColor = Color.White
                                       lblStock.BackColor = Color.White
                                   End Sub

        Return pnl
    End Function

    Private Sub ShowCartQuantityEditor()
        ' Show numpad dialog to modify quantity of selected cart item
        If _cartItems.Rows.Count = 0 Then
            MessageBox.Show("Cart is empty. Add items first.", "Empty Cart", MessageBoxButtons.OK, MessageBoxIcon.Information)
            Return
        End If

        ' Check if a cart item is selected
        If dgvCart.SelectedRows.Count = 0 Then
            MessageBox.Show("Please select a cart item first.", "No Item Selected", MessageBoxButtons.OK, MessageBoxIcon.Information)
            Return
        End If

        ' Get selected cart item details
        Dim selectedRow = dgvCart.SelectedRows(0)
        Dim rowIndex = selectedRow.Index
        Dim productID = CInt(selectedRow.Cells("ProductID").Value)
        Dim productName = selectedRow.Cells("Product").Value.ToString()
        Dim currentQty = CDec(selectedRow.Cells("Qty").Value)
        Dim price = CDec(selectedRow.Cells("Price").Value)

        ' Create modal form with numpad
        Dim modalForm As New Form With {
            .Text = "",
            .Size = New Size(600, 500),
            .StartPosition = FormStartPosition.CenterParent,
            .FormBorderStyle = FormBorderStyle.None,
            .BackColor = _ironDark,
            .ShowInTaskbar = False
        }

        ' Title
        Dim lblTitle As New Label With {
            .Text = productName,
            .Font = New Font("Segoe UI", 24, FontStyle.Bold),
            .ForeColor = _ironGold,
            .Location = New Point(20, 20),
            .Size = New Size(560, 50),
            .TextAlign = ContentAlignment.MiddleCenter,
            .BackColor = _ironDark
        }

        ' Quantity input with current qty
        Dim isInitialValue As Boolean = True
        Dim txtQuantity As New TextBox With {
            .Text = currentQty.ToString(),
            .Font = New Font("Segoe UI", 48, FontStyle.Bold),
            .TextAlign = HorizontalAlignment.Center,
            .Location = New Point(150, 90),
            .Size = New Size(300, 80),
            .BackColor = Color.FromArgb(50, 50, 70),
            .ForeColor = _ironGold,
            .BorderStyle = BorderStyle.FixedSingle,
            .ReadOnly = True
        }

        ' Numpad panel
        Dim pnlNumpad As New Panel With {
            .Location = New Point(100, 190),
            .Size = New Size(400, 220),
            .BackColor = _ironDark
        }

        ' Create numpad buttons (3x4 grid)
        Dim numbers() As String = {"1", "2", "3", "4", "5", "6", "7", "8", "9", "C", "0", "⌫"}
        Dim btnIndex = 0

        For row = 0 To 3
            For col = 0 To 2
                Dim btnText = numbers(btnIndex)
                Dim btn As New Button With {
                    .Text = btnText,
                    .Size = New Size(120, 50),
                    .Location = New Point(col * 130, row * 55),
                    .Font = New Font("Segoe UI", 20, FontStyle.Bold),
                    .BackColor = _ironBlue,
                    .ForeColor = Color.White,
                    .FlatStyle = FlatStyle.Flat,
                    .Cursor = Cursors.Hand,
                    .Tag = btnText
                }
                btn.FlatAppearance.BorderSize = 1
                btn.FlatAppearance.BorderColor = _ironGlow

                AddHandler btn.Click, Sub(s, ev)
                                          Dim clickedBtn = CType(s, Button)
                                          Dim value = clickedBtn.Tag.ToString()

                                          If value = "C" Then
                                              txtQuantity.Text = "1"
                                              isInitialValue = True
                                          ElseIf value = "⌫" Then
                                              If txtQuantity.Text.Length > 1 Then
                                                  txtQuantity.Text = txtQuantity.Text.Substring(0, txtQuantity.Text.Length - 1)
                                              Else
                                                  txtQuantity.Text = "1"
                                                  isInitialValue = True
                                              End If
                                          Else
                                              ' If it's the initial value, replace it with the first digit
                                              If isInitialValue Then
                                                  txtQuantity.Text = value
                                                  isInitialValue = False
                                              Else
                                                  ' Otherwise append the digit
                                                  txtQuantity.Text &= value
                                              End If
                                          End If
                                      End Sub

                AddHandler btn.MouseEnter, Sub(s, ev)
                                               Dim hoverBtn = CType(s, Button)
                                               hoverBtn.BackColor = _ironBlueDark
                                               hoverBtn.FlatAppearance.BorderSize = 2
                                               hoverBtn.FlatAppearance.BorderColor = _ironGold
                                           End Sub

                AddHandler btn.MouseLeave, Sub(s, ev)
                                               Dim hoverBtn = CType(s, Button)
                                               hoverBtn.BackColor = _ironBlue
                                               hoverBtn.FlatAppearance.BorderSize = 1
                                               hoverBtn.FlatAppearance.BorderColor = _ironGlow
                                           End Sub

                pnlNumpad.Controls.Add(btn)
                btnIndex += 1
            Next
        Next

        ' OK button
        Dim btnOK As New Button With {
            .Text = "OK",
            .Size = New Size(200, 50),
            .Location = New Point(200, 420),
            .Font = New Font("Segoe UI", 16, FontStyle.Bold),
            .BackColor = _green,
            .ForeColor = Color.White,
            .FlatStyle = FlatStyle.Flat,
            .Cursor = Cursors.Hand
        }
        btnOK.FlatAppearance.BorderSize = 0

        AddHandler btnOK.Click, Sub()
                                    Dim newQty As Decimal
                                    If Decimal.TryParse(txtQuantity.Text, newQty) AndAlso newQty > 0 Then
                                        ' Find the correct DataTable row using ProductID (water-tight approach)
                                        Dim foundRow As DataRow = Nothing
                                        For Each row As DataRow In _cartItems.Rows
                                            If CInt(row("ProductID")) = productID Then
                                                foundRow = row
                                                Exit For
                                            End If
                                        Next
                                        
                                        If foundRow IsNot Nothing Then
                                            ' Update cart item quantity using ProductID match
                                            foundRow("Qty") = newQty
                                            foundRow("Total") = price * newQty
                                            CalculateTotals()
                                            modalForm.DialogResult = DialogResult.OK
                                            modalForm.Close()
                                        Else
                                            MessageBox.Show("Cart item not found.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
                                        End If
                                    Else
                                        MessageBox.Show("Please enter a valid quantity.", "Invalid Quantity", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                                    End If
                                End Sub

        AddHandler btnOK.MouseEnter, Sub()
                                         btnOK.BackColor = Color.FromArgb(0, 180, 0)
                                     End Sub

        AddHandler btnOK.MouseLeave, Sub()
                                         btnOK.BackColor = _green
                                     End Sub

        modalForm.Controls.AddRange({lblTitle, txtQuantity, pnlNumpad, btnOK})
        modalForm.ShowDialog(Me)
    End Sub

    Private Sub ShowQuantityModal(productId As Integer, productCode As String, productName As String, price As Decimal, stock As Decimal)
        ' Create modal form
        Dim modalForm As New Form With {
            .Text = "",
            .Size = New Size(600, 500),
            .StartPosition = FormStartPosition.CenterParent,
            .FormBorderStyle = FormBorderStyle.None,
            .BackColor = _ironDark,
            .ShowInTaskbar = False
        }

        ' Title
        Dim lblTitle As New Label With {
            .Text = productName,
            .Font = New Font("Segoe UI", 24, FontStyle.Bold),
            .ForeColor = _ironGold,
            .Location = New Point(20, 20),
            .Size = New Size(560, 50),
            .TextAlign = ContentAlignment.MiddleCenter,
            .BackColor = _ironDark
        }

        ' Quantity input with flag to track if it's the initial value
        Dim isInitialValue As Boolean = True
        Dim txtQuantity As New TextBox With {
            .Text = "1",
            .Font = New Font("Segoe UI", 48, FontStyle.Bold),
            .TextAlign = HorizontalAlignment.Center,
            .Location = New Point(150, 90),
            .Size = New Size(300, 80),
            .BackColor = Color.FromArgb(50, 50, 70),
            .ForeColor = _ironGold,
            .BorderStyle = BorderStyle.FixedSingle,
            .ReadOnly = True
        }

        ' Numpad panel
        Dim pnlNumpad As New Panel With {
            .Location = New Point(100, 190),
            .Size = New Size(400, 220),
            .BackColor = _ironDark
        }

        ' Create numpad buttons (3x4 grid)
        Dim numbers() As String = {"1", "2", "3", "4", "5", "6", "7", "8", "9", "C", "0", "⌫"}
        Dim btnIndex = 0

        For row = 0 To 3
            For col = 0 To 2
                Dim btnText = numbers(btnIndex)
                Dim btn As New Button With {
                    .Text = btnText,
                    .Size = New Size(120, 50),
                    .Location = New Point(col * 130, row * 55),
                    .Font = New Font("Segoe UI", 20, FontStyle.Bold),
                    .BackColor = _ironBlue,
                    .ForeColor = Color.White,
                    .FlatStyle = FlatStyle.Flat,
                    .Cursor = Cursors.Hand,
                    .Tag = btnText
                }
                btn.FlatAppearance.BorderSize = 1
                btn.FlatAppearance.BorderColor = _ironGlow

                AddHandler btn.Click, Sub(s, ev)
                                          Dim clickedBtn = CType(s, Button)
                                          Dim value = clickedBtn.Tag.ToString()

                                          If value = "C" Then
                                              txtQuantity.Text = "1"
                                              isInitialValue = True
                                          ElseIf value = "⌫" Then
                                              If txtQuantity.Text.Length > 1 Then
                                                  txtQuantity.Text = txtQuantity.Text.Substring(0, txtQuantity.Text.Length - 1)
                                              Else
                                                  txtQuantity.Text = "1"
                                                  isInitialValue = True
                                              End If
                                          Else
                                              ' If it's the initial "1", replace it with the first digit
                                              If isInitialValue Then
                                                  txtQuantity.Text = value
                                                  isInitialValue = False
                                              Else
                                                  ' Otherwise append the digit
                                                  txtQuantity.Text &= value
                                              End If
                                          End If
                                      End Sub

                AddHandler btn.MouseEnter, Sub(s, ev)
                                               Dim hoverBtn = CType(s, Button)
                                               hoverBtn.BackColor = _ironBlueDark
                                               hoverBtn.FlatAppearance.BorderSize = 2
                                               hoverBtn.FlatAppearance.BorderColor = _ironGold
                                           End Sub

                AddHandler btn.MouseLeave, Sub(s, ev)
                                               Dim hoverBtn = CType(s, Button)
                                               hoverBtn.BackColor = _ironBlue
                                               hoverBtn.FlatAppearance.BorderSize = 1
                                               hoverBtn.FlatAppearance.BorderColor = _ironGlow
                                           End Sub

                pnlNumpad.Controls.Add(btn)
                btnIndex += 1
            Next
        Next

        ' Action buttons
        Dim btnNo As New Button With {
            .Text = "NO",
            .Size = New Size(250, 60),
            .Location = New Point(50, 420),
            .Font = New Font("Segoe UI", 20, FontStyle.Bold),
            .BackColor = _ironRed,
            .ForeColor = Color.White,
            .FlatStyle = FlatStyle.Flat,
            .Cursor = Cursors.Hand
        }
        btnNo.FlatAppearance.BorderSize = 0
        AddHandler btnNo.Click, Sub() modalForm.Close()

        Dim btnYes As New Button With {
            .Text = "YES",
            .Size = New Size(250, 60),
            .Location = New Point(310, 420),
            .Font = New Font("Segoe UI", 20, FontStyle.Bold),
            .BackColor = _green,
            .ForeColor = Color.White,
            .FlatStyle = FlatStyle.Flat,
            .Cursor = Cursors.Hand
        }
        btnYes.FlatAppearance.BorderSize = 0
        AddHandler btnYes.Click, Sub()
                                     Dim quantity = CInt(txtQuantity.Text)
                                     If quantity > 0 Then
                                         ' Add to cart multiple times for the quantity (disregard stock)
                                         For i = 1 To quantity
                                             AddProductToCart(productId, productCode, productName, price)
                                         Next
                                         modalForm.Close()
                                     End If
                                 End Sub

        ' Add all controls
        modalForm.Controls.AddRange({lblTitle, txtQuantity, pnlNumpad, btnNo, btnYes})

        ' Show modal
        modalForm.ShowDialog(Me)
    End Sub

    ''' <summary>
    ''' Smooth fade-in animation for product cards
    ''' </summary>
    Private Sub AnimateFadeIn(control As Control)
        ' Start with low opacity
        Dim opacity As Double = 0.0
        Dim fadeTimer As New Timer With {.Interval = 15}

        AddHandler fadeTimer.Tick, Sub(s, ev)
                                       opacity += 0.15
                                       If opacity >= 1.0 Then
                                           opacity = 1.0
                                           fadeTimer.Stop()
                                           fadeTimer.Dispose()
                                       End If

                                       ' Simulate opacity by adjusting control visibility
                                       ' WinForms doesn't support true opacity on controls, so we use a quick fade effect
                                       If opacity < 1.0 Then
                                           control.BackColor = Color.FromArgb(CInt(255 * opacity), control.BackColor)
                                       End If
                                   End Sub

        fadeTimer.Start()
    End Sub

    ''' <summary>
    ''' Print order receipt to thermal printer (80mm)
    ''' </summary>
    Private Sub PrintOrderReceiptToThermalPrinter(orderNumber As String, customerName As String, customerSurname As String, customerPhone As String, readyDate As DateTime, readyTime As TimeSpan, collectionDay As String, specialInstructions As String, depositPaid As Decimal, totalAmount As Decimal, Optional colour As String = "", Optional picture As String = "", Optional notes As String = "")
        Try
            Dim printDoc As New Printing.PrintDocument()

            AddHandler printDoc.PrintPage, Sub(sender, e)
                                               ' ALL FONTS BOLD FOR BETTER VISIBILITY
                                               Dim fontBold As New Font("Courier New", 8, FontStyle.Bold)
                                               Dim fontLarge As New Font("Courier New", 11, FontStyle.Bold)
                                               Dim yPos As Single = 5
                                               Dim leftMargin As Single = 5

                                               ' Store header - centered
                                               Dim headerText = "OVEN DELIGHTS"
                                               Dim headerSize = e.Graphics.MeasureString(headerText, fontLarge)
                                               e.Graphics.DrawString(headerText, fontLarge, Brushes.Black, (302 - headerSize.Width) / 2, yPos)
                                               yPos += 22

                                               ' Branch info with full address
                                               Dim branchDetails = GetBranchDetails()
                                               e.Graphics.DrawString(branchDetails.Name, fontBold, Brushes.Black, leftMargin, yPos)
                                               yPos += 14
                                               e.Graphics.DrawString(branchDetails.Address, fontBold, Brushes.Black, leftMargin, yPos)
                                               yPos += 14
                                               e.Graphics.DrawString($"Tel: {branchDetails.Phone}", fontBold, Brushes.Black, leftMargin, yPos)
                                               yPos += 15

                                               ' Date and time
                                               e.Graphics.DrawString(DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss"), fontBold, Brushes.Black, leftMargin, yPos)
                                               yPos += 15

                                               ' Order number
                                               e.Graphics.DrawString($"Order #: {orderNumber}", fontBold, Brushes.Black, leftMargin, yPos)
                                               yPos += 15

                                               ' Cashier
                                               e.Graphics.DrawString($"Cashier: {_cashierName}", fontBold, Brushes.Black, leftMargin, yPos)
                                               yPos += 18

                                               ' Separator
                                               e.Graphics.DrawString("======================================", fontBold, Brushes.Black, leftMargin, yPos)
                                               yPos += 15

                                               ' Order header - centered
                                               Dim orderText = "ORDER RECEIPT"
                                               Dim orderSize = e.Graphics.MeasureString(orderText, fontBold)
                                               e.Graphics.DrawString(orderText, fontBold, Brushes.Black, (302 - orderSize.Width) / 2, yPos)
                                               yPos += 18

                                               ' Separator
                                               e.Graphics.DrawString("======================================", fontBold, Brushes.Black, leftMargin, yPos)
                                               yPos += 15

                                               ' Customer details
                                               e.Graphics.DrawString("CUSTOMER:", fontBold, Brushes.Black, leftMargin, yPos)
                                               yPos += 14
                                               e.Graphics.DrawString($"{customerName} {customerSurname}", fontBold, Brushes.Black, leftMargin, yPos)
                                               yPos += 14
                                               e.Graphics.DrawString($"Phone: {customerPhone}", fontBold, Brushes.Black, leftMargin, yPos)
                                               yPos += 18

                                               ' Collection details
                                               e.Graphics.DrawString("READY FOR COLLECTION:", fontBold, Brushes.Black, leftMargin, yPos)
                                               yPos += 14
                                               e.Graphics.DrawString($"Date: {readyDate:dd/MM/yyyy}", fontBold, Brushes.Black, leftMargin, yPos)
                                               yPos += 14
                                               e.Graphics.DrawString($"Time: {readyTime:hh\:mm}", fontBold, Brushes.Black, leftMargin, yPos)
                                               yPos += 14
                                               e.Graphics.DrawString($"*** {collectionDay.ToUpper()} ***", fontBold, Brushes.Black, leftMargin, yPos)
                                               yPos += 18

                                               ' Special instructions
                                               If Not String.IsNullOrWhiteSpace(specialInstructions) Then
                                                   e.Graphics.DrawString("SPECIAL INSTRUCTIONS:", fontBold, Brushes.Black, leftMargin, yPos)
                                                   yPos += 14
                                                   e.Graphics.DrawString(specialInstructions, fontBold, Brushes.Black, leftMargin, yPos)
                                                   yPos += 18
                                               End If

                                               ' Notes
                                               If Not String.IsNullOrWhiteSpace(notes) Then
                                                   e.Graphics.DrawString("NOTES:", fontBold, Brushes.Black, leftMargin, yPos)
                                                   yPos += 14
                                                   e.Graphics.DrawString(notes, fontBold, Brushes.Black, leftMargin, yPos)
                                                   yPos += 18
                                               End If

                                               ' Separator
                                               e.Graphics.DrawString("======================================", fontBold, Brushes.Black, leftMargin, yPos)
                                               yPos += 15

                                               ' Items
                                               For Each row As DataRow In _cartItems.Rows
                                                   Dim product = row("Product").ToString()
                                                   Dim qty = CDec(row("Qty"))
                                                   Dim price = CDec(row("Price"))
                                                   Dim total = CDec(row("Total"))

                                                   e.Graphics.DrawString($"{qty:0.00} x {product}", fontBold, Brushes.Black, leftMargin, yPos)
                                                   yPos += 14
                                                   e.Graphics.DrawString($"    @ R{price:N2} = R{total:N2}", fontBold, Brushes.Black, leftMargin, yPos)
                                                   yPos += 14
                                               Next

                                               yPos += 5
                                               e.Graphics.DrawString("--------------------------------------", fontBold, Brushes.Black, leftMargin, yPos)
                                               yPos += 15

                                               ' Calculate VAT breakdown (prices are VAT-inclusive)
                                               Dim subtotalExclVAT = Math.Round(totalAmount / 1.15D, 2)
                                               Dim vatAmount = Math.Round(totalAmount - subtotalExclVAT, 2)

                                               e.Graphics.DrawString($"Subtotal (excl VAT):  R {subtotalExclVAT:N2}", fontBold, Brushes.Black, leftMargin, yPos)
                                               yPos += 14
                                               e.Graphics.DrawString($"VAT (15%):            R {vatAmount:N2}", fontBold, Brushes.Black, leftMargin, yPos)
                                               yPos += 14
                                               e.Graphics.DrawString($"Total Amount:         R {totalAmount:N2}", fontBold, Brushes.Black, leftMargin, yPos)
                                               yPos += 14
                                               e.Graphics.DrawString($"Deposit Paid:         R {depositPaid:N2}", fontBold, Brushes.Black, leftMargin, yPos)
                                               yPos += 14
                                               e.Graphics.DrawString($"Balance Due:          R {(totalAmount - depositPaid):N2}", fontBold, Brushes.Black, leftMargin, yPos)
                                               yPos += 18

                                               e.Graphics.DrawString("======================================", fontBold, Brushes.Black, leftMargin, yPos)
                                               yPos += 15

                                               ' Barcode for order collection (7-digit format, research-based settings)
                                               Try
                                                   Dim barcodeImage = BarcodeGenerator.GenerateCode39Barcode(orderNumber, 180, 60)
                                                   e.Graphics.DrawImage(barcodeImage, CInt((302 - 180) / 2), CInt(yPos))
                                                   yPos += 65

                                                   barcodeImage.Dispose()
                                               Catch ex As Exception
                                                   Dim orderNumFont As New Font("Arial", 20, FontStyle.Bold)
                                                   Dim orderNumSize = e.Graphics.MeasureString(orderNumber, orderNumFont)
                                                   e.Graphics.DrawString(orderNumber, orderNumFont, Brushes.Black, (302 - orderNumSize.Width) / 2, yPos)
                                                   yPos += 28
                                               End Try

                                               ' Footer - centered
                                               Dim footer1 = "SCAN BARCODE TO COLLECT"
                                               Dim footer1Size = e.Graphics.MeasureString(footer1, fontBold)
                                               e.Graphics.DrawString(footer1, fontBold, Brushes.Black, (302 - footer1Size.Width) / 2, yPos)
                                               yPos += 14

                                               Dim footer2 = "PLEASE BRING THIS RECEIPT"
                                               Dim footer2Size = e.Graphics.MeasureString(footer2, fontBold)
                                               e.Graphics.DrawString(footer2, fontBold, Brushes.Black, (302 - footer2Size.Width) / 2, yPos)
                                           End Sub

            ' Print to default thermal printer
            printDoc.Print()

        Catch ex As Exception
            Throw New Exception($"Thermal printer error: {ex.Message}")
        End Try
    End Sub

    ''' <summary>
    ''' Print order collection receipt to thermal printer (80mm)
    ''' </summary>
    Private Sub PrintCollectionReceiptToThermalPrinter(orderNumber As String, customerName As String, balancePaid As Decimal, depositPaid As Decimal, totalAmount As Decimal, paymentMethod As String, cashAmount As Decimal, cardAmount As Decimal, changeAmount As Decimal)
        Try
            Dim printDoc As New Printing.PrintDocument()

            ' Use default printer settings - don't force custom paper size

            AddHandler printDoc.PrintPage, Sub(sender, e)
                                               Dim font As New Font("Courier New", 8)
                                               Dim fontBold As New Font("Courier New", 8, FontStyle.Bold)
                                               Dim fontLarge As New Font("Courier New", 11, FontStyle.Bold)
                                               Dim yPos As Single = 5
                                               Dim leftMargin As Single = 5

                                               ' Store header - centered
                                               Dim headerText = "OVEN DELIGHTS"
                                               Dim headerSize = e.Graphics.MeasureString(headerText, fontLarge)
                                               e.Graphics.DrawString(headerText, fontLarge, Brushes.Black, (302 - headerSize.Width) / 2, yPos)
                                               yPos += 22

                                               ' Branch info
                                               e.Graphics.DrawString(GetBranchName(), font, Brushes.Black, leftMargin, yPos)
                                               yPos += 15

                                               ' Date collected
                                               e.Graphics.DrawString($"Date Collected: {DateTime.Now:dd/MM/yyyy HH:mm}", fontBold, Brushes.Black, leftMargin, yPos)
                                               yPos += 15

                                               ' Order number
                                               e.Graphics.DrawString($"Order #: {orderNumber}", fontBold, Brushes.Black, leftMargin, yPos)
                                               yPos += 15

                                               ' Customer name
                                               e.Graphics.DrawString($"Customer: {customerName}", font, Brushes.Black, leftMargin, yPos)
                                               yPos += 15

                                               ' Cashier
                                               e.Graphics.DrawString($"Cashier: {_cashierName}", font, Brushes.Black, leftMargin, yPos)
                                               yPos += 18

                                               ' Separator
                                               e.Graphics.DrawString("======================================", font, Brushes.Black, leftMargin, yPos)
                                               yPos += 15

                                               ' Collection header - centered
                                               Dim collectionText = "ORDER COLLECTION"
                                               Dim collectionSize = e.Graphics.MeasureString(collectionText, fontBold)
                                               e.Graphics.DrawString(collectionText, fontBold, Brushes.Black, (302 - collectionSize.Width) / 2, yPos)
                                               yPos += 18

                                               ' Separator
                                               e.Graphics.DrawString("======================================", font, Brushes.Black, leftMargin, yPos)
                                               yPos += 15

                                               ' Payment summary
                                               e.Graphics.DrawString("PAYMENT SUMMARY:", fontBold, Brushes.Black, leftMargin, yPos)
                                               yPos += 15

                                               e.Graphics.DrawString($"Order Total:              R {totalAmount:N2}", font, Brushes.Black, leftMargin, yPos)
                                               yPos += 14
                                               e.Graphics.DrawString($"Deposit Paid:             R {depositPaid:N2}", font, Brushes.Black, leftMargin, yPos)
                                               yPos += 14
                                               e.Graphics.DrawString($"Balance Paid:             R {balancePaid:N2}", fontBold, Brushes.Black, leftMargin, yPos)
                                               yPos += 18

                                               ' Payment method
                                               e.Graphics.DrawString($"Payment: {paymentMethod}", font, Brushes.Black, leftMargin, yPos)
                                               yPos += 14

                                               If cashAmount > 0 Then
                                                   e.Graphics.DrawString($"Cash:                     R {cashAmount:N2}", font, Brushes.Black, leftMargin, yPos)
                                                   yPos += 14
                                               End If

                                               If cardAmount > 0 Then
                                                   e.Graphics.DrawString($"Card:                     R {cardAmount:N2}", font, Brushes.Black, leftMargin, yPos)
                                                   yPos += 14
                                               End If

                                               If changeAmount > 0 Then
                                                   e.Graphics.DrawString($"CHANGE:                   R {changeAmount:N2}", fontBold, Brushes.Black, leftMargin, yPos)
                                                   yPos += 14
                                               End If

                                               yPos += 10
                                               e.Graphics.DrawString("======================================", font, Brushes.Black, leftMargin, yPos)
                                               yPos += 15

                                               ' Footer - centered
                                               Dim footer1 = "Thank you for your order!"
                                               Dim footer1Size = e.Graphics.MeasureString(footer1, font)
                                               e.Graphics.DrawString(footer1, font, Brushes.Black, (302 - footer1Size.Width) / 2, yPos)
                                               yPos += 14

                                               Dim footer2 = "Please come again!"
                                               Dim footer2Size = e.Graphics.MeasureString(footer2, font)
                                               e.Graphics.DrawString(footer2, font, Brushes.Black, (302 - footer2Size.Width) / 2, yPos)
                                           End Sub

            ' Print to default thermal printer
            printDoc.Print()

        Catch ex As Exception
            ' Don't block the collection if printing fails
            Console.WriteLine($"Thermal printer error: {ex.Message}")
        End Try
    End Sub

    ''' <summary>
    ''' Public method to perform day-end from cash-up dialog
    ''' </summary>
    Public Sub PerformDayEnd(actualCash As Decimal)
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
                    cmd.Parameters.AddWithValue("@TillPointID", _tillPointID)

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

            Dim cashVariance = actualCash - totalCash

            ' Optional notes
            Dim notes = InputBox("Enter any notes (optional):", "Day End Notes", "")

            ' Print day-end report to slip printer
            PrintDayEndReport(totalSales, totalCash, totalCard, totalAccount, totalRefunds, totalCash, actualCash, cashVariance, notes)

            ' Complete day-end in database
            Dim dayEndService As New DayEndService()
            dayEndService.CompleteDayEnd(
                _tillPointID,
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
                                               PrintLeft($"Till: Till {_tillPointID}", font)
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

    ' ========================================
    ' USER DEFINED ORDER METHODS
    ' ========================================
    
    Private Sub StartUserDefinedOrder()
        Try
            ' Clear cart if needed
            If _cartItems.Rows.Count > 0 Then
                Dim result = MessageBox.Show("Clear current cart and start User Defined Order?", "User Defined Order", MessageBoxButtons.YesNo, MessageBoxIcon.Question)
                If result = DialogResult.No Then
                    Return
                End If
                _cartItems.Clear()
                CalculateTotals()
            End If

            ' Show User Defined Order Dialog to capture header fields
            Dim dialog As New UserDefinedOrderDialog(_branchID, GetBranchName())
            If dialog.ShowDialog() = DialogResult.OK Then
                ' Save order data
                _userDefinedOrderData = New UserDefinedOrderData With {
                    .CustomerCellNumber = dialog.CustomerCellNumber,
                    .CustomerName = dialog.CustomerName,
                    .CustomerSurname = dialog.CustomerSurname,
                    .CakeColour = dialog.CakeColour,
                    .SpecialRequest = dialog.SpecialRequest,
                    .CakeImage = dialog.CakeImage,
                    .CollectionDate = dialog.CollectionDate,
                    .CollectionTime = dialog.CollectionTime,
                    .CollectionDay = dialog.CollectionDay
                }

                ' Enter User Defined Mode
                _isUserDefinedMode = True
                _isOrderMode = False

                ' Show categories for item selection
                ShowCategories()

                ' Update breadcrumb
                lblBreadcrumb.Text = $"🎂 USER DEFINED ORDER - {_userDefinedOrderData.CustomerName} - Collect: {_userDefinedOrderData.CollectionDate:dd/MM/yyyy}"
                lblBreadcrumb.ForeColor = ColorTranslator.FromHtml("#E67E22")

                ' Show Complete User Defined Order button
                ShowCompleteUserDefinedButton()
            End If

        Catch ex As Exception
            MessageBox.Show($"Error starting User Defined Order: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    Private Sub ShowCompleteUserDefinedButton()
        ' Remove existing button if present
        If _btnCompleteUserDefined IsNot Nothing AndAlso pnlCart.Controls.Contains(_btnCompleteUserDefined) Then
            pnlCart.Controls.Remove(_btnCompleteUserDefined)
        End If

        ' Hide Pay Now button in User Defined mode
        Dim btnPayNow = pnlCart.Controls.Find("btnPayNow", True).FirstOrDefault()
        If btnPayNow IsNot Nothing Then
            btnPayNow.Visible = False
        End If

        ' Create "COMPLETE USER DEFINED ORDER" button at bottom of cart
        _btnCompleteUserDefined = New Button With {
            .Text = "✓ COMPLETE USER DEFINED ORDER",
            .Font = New Font("Segoe UI", 14, FontStyle.Bold),
            .Size = New Size(pnlCart.Width - 40, 60),
            .Location = New Point(20, pnlCart.Height - 80),
            .BackColor = ColorTranslator.FromHtml("#27AE60"),
            .ForeColor = Color.White,
            .FlatStyle = FlatStyle.Flat,
            .Cursor = Cursors.Hand,
            .Anchor = AnchorStyles.Bottom Or AnchorStyles.Left Or AnchorStyles.Right
        }
        _btnCompleteUserDefined.FlatAppearance.BorderSize = 0
        AddHandler _btnCompleteUserDefined.Click, AddressOf CompleteUserDefinedOrder
        pnlCart.Controls.Add(_btnCompleteUserDefined)
        _btnCompleteUserDefined.BringToFront()
    End Sub

    Private Sub CompleteUserDefinedOrder()
        Try
            ' Validate cart has items
            If _cartItems.Rows.Count = 0 Then
                MessageBox.Show("Please add items to the cart before completing the order.", "No Items", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                Return
            End If

            ' Calculate totals
            Dim subtotal = CalculateSubtotal()
            Dim taxAmount = CalculateTax(subtotal)
            Dim totalAmount = subtotal + taxAmount

            ' Open payment tender
            Dim tenderForm As New PaymentTenderForm(_cashierID, _cashierName, _branchID, _tillPointID, GetBranchPrefix(), _cartItems, subtotal, taxAmount, totalAmount)
            If tenderForm.ShowDialog() = DialogResult.OK Then
                ' Payment successful - save User Defined Order
                Dim orderNumber = SaveUserDefinedOrder(tenderForm.PaymentMethod, tenderForm.CashAmount, tenderForm.CardAmount, totalAmount)

                ' Print dual till slip
                PrintUserDefinedOrderSlip(orderNumber, totalAmount, tenderForm.PaymentMethod, tenderForm.CashAmount, tenderForm.CardAmount)

                ' Show success message
                MessageBox.Show($"User Defined Order {orderNumber} created successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information)

                ' Reset to Sale Mode
                ResetToSaleMode()
            End If

        Catch ex As Exception
            MessageBox.Show($"Error completing User Defined Order: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    Private Function SaveUserDefinedOrder(paymentMethod As String, cashAmount As Decimal, cardAmount As Decimal, totalAmount As Decimal) As String
        Dim orderNumber As String = ""
        Dim saleID As Integer = 0
        Dim invoiceNumber As String = ""

        Using conn As New SqlConnection(_connectionString)
            conn.Open()
            Using transaction = conn.BeginTransaction()
                Try
                    ' Generate order number: BranchID + 6 + sequence
                    orderNumber = GenerateUserDefinedOrderNumber(conn, transaction)

                    ' 1. Write to Sales table
                    saleID = WriteSaleToDatabase(conn, transaction, paymentMethod, cashAmount, cardAmount, totalAmount)

                    ' 2. Generate invoice number
                    invoiceNumber = GenerateInvoiceNumber(conn, transaction)

                    ' 3. Write to Invoices table
                    WriteInvoiceItems(conn, transaction, invoiceNumber, saleID)

                    ' 4. Write to POS_UserDefinedOrders table
                    Dim insertOrderSql = "
                        INSERT INTO POS_UserDefinedOrders (
                            OrderNumber, BranchID, BranchName, CashierID, CashierName, TillPointID,
                            CustomerCellNumber, CustomerName, CustomerSurname,
                            CakeColour, CakeImage, SpecialRequest,
                            CollectionDate, CollectionTime, CollectionDay,
                            OrderDate, OrderTime, OrderDateTime,
                            TotalAmount, AmountPaid, PaymentMethod, CashAmount, CardAmount,
                            Status, SaleID, InvoiceNumber
                        ) VALUES (
                            @OrderNumber, @BranchID, @BranchName, @CashierID, @CashierName, @TillPointID,
                            @CustomerCellNumber, @CustomerName, @CustomerSurname,
                            @CakeColour, @CakeImage, @SpecialRequest,
                            @CollectionDate, @CollectionTime, @CollectionDay,
                            @OrderDate, @OrderTime, GETDATE(),
                            @TotalAmount, @AmountPaid, @PaymentMethod, @CashAmount, @CardAmount,
                            'Created', @SaleID, @InvoiceNumber
                        )"

                    Using cmd As New SqlCommand(insertOrderSql, conn, transaction)
                        cmd.Parameters.AddWithValue("@OrderNumber", orderNumber)
                        cmd.Parameters.AddWithValue("@BranchID", _branchID)
                        cmd.Parameters.AddWithValue("@BranchName", GetBranchName())
                        cmd.Parameters.AddWithValue("@CashierID", _cashierID)
                        cmd.Parameters.AddWithValue("@CashierName", _cashierName)
                        cmd.Parameters.AddWithValue("@TillPointID", _tillPointID)
                        cmd.Parameters.AddWithValue("@CustomerCellNumber", _userDefinedOrderData.CustomerCellNumber)
                        cmd.Parameters.AddWithValue("@CustomerName", _userDefinedOrderData.CustomerName)
                        cmd.Parameters.AddWithValue("@CustomerSurname", If(String.IsNullOrEmpty(_userDefinedOrderData.CustomerSurname), DBNull.Value, _userDefinedOrderData.CustomerSurname))
                        cmd.Parameters.AddWithValue("@CakeColour", If(String.IsNullOrEmpty(_userDefinedOrderData.CakeColour), DBNull.Value, _userDefinedOrderData.CakeColour))
                        cmd.Parameters.AddWithValue("@CakeImage", If(String.IsNullOrEmpty(_userDefinedOrderData.CakeImage), DBNull.Value, _userDefinedOrderData.CakeImage))
                        cmd.Parameters.AddWithValue("@SpecialRequest", If(String.IsNullOrEmpty(_userDefinedOrderData.SpecialRequest), DBNull.Value, _userDefinedOrderData.SpecialRequest))
                        cmd.Parameters.AddWithValue("@CollectionDate", _userDefinedOrderData.CollectionDate)
                        cmd.Parameters.AddWithValue("@CollectionTime", _userDefinedOrderData.CollectionTime)
                        cmd.Parameters.AddWithValue("@CollectionDay", _userDefinedOrderData.CollectionDay)
                        cmd.Parameters.AddWithValue("@OrderDate", DateTime.Today)
                        cmd.Parameters.AddWithValue("@OrderTime", DateTime.Now.TimeOfDay)
                        cmd.Parameters.AddWithValue("@TotalAmount", totalAmount)
                        cmd.Parameters.AddWithValue("@AmountPaid", totalAmount)
                        cmd.Parameters.AddWithValue("@PaymentMethod", paymentMethod)
                        cmd.Parameters.AddWithValue("@CashAmount", If(cashAmount > 0, cashAmount, DBNull.Value))
                        cmd.Parameters.AddWithValue("@CardAmount", If(cardAmount > 0, cardAmount, DBNull.Value))
                        cmd.Parameters.AddWithValue("@SaleID", saleID)
                        cmd.Parameters.AddWithValue("@InvoiceNumber", invoiceNumber)
                        cmd.ExecuteNonQuery()
                    End Using

                    ' 5. Write order items
                    Try
                        Dim orderID = GetUserDefinedOrderID(conn, transaction, orderNumber)
                        For Each row As DataRow In _cartItems.Rows
                            Dim insertItemSql = "
                                INSERT INTO POS_UserDefinedOrderItems (
                                    UserDefinedOrderID, ProductID, ProductName, ProductCode, Quantity, UnitPrice, LineTotal
                                ) VALUES (
                                    @OrderID, @ProductID, @ProductName, @ProductCode, @Quantity, @UnitPrice, @LineTotal
                                )"
                            Using cmdItem As New SqlCommand(insertItemSql, conn, transaction)
                                cmdItem.Parameters.AddWithValue("@OrderID", orderID)
                                cmdItem.Parameters.AddWithValue("@ProductID", CInt(row("ProductID")))
                                cmdItem.Parameters.AddWithValue("@ProductName", row("Product").ToString())
                                cmdItem.Parameters.AddWithValue("@ProductCode", If(IsDBNull(row("ItemCode")), DBNull.Value, row("ItemCode")))
                                cmdItem.Parameters.AddWithValue("@Quantity", CDec(row("Qty")))
                                cmdItem.Parameters.AddWithValue("@UnitPrice", CDec(row("Price")))
                                cmdItem.Parameters.AddWithValue("@LineTotal", CDec(row("Total")))
                                cmdItem.ExecuteNonQuery()
                            End Using
                        Next
                    Catch ex As SqlException
                        Throw New Exception($"Error writing order items. SQL Error: {ex.Message}. Line: {ex.LineNumber}. Procedure: {ex.Procedure}", ex)
                    End Try

                    ' 6. Update stock
                    UpdateStock(conn, transaction)

                    ' 7. Create journal entries for sales and cost of sales
                    CreateJournalEntries(conn, transaction, invoiceNumber, paymentMethod, cashAmount, cardAmount, totalAmount)

                    transaction.Commit()
                    Return orderNumber

                Catch ex As Exception
                    transaction.Rollback()
                    ' Get inner exception details
                    Dim innerMsg = If(ex.InnerException IsNot Nothing, $" Inner: {ex.InnerException.Message}", "")
                    Throw New Exception($"Failed to save User Defined Order: {ex.Message}{innerMsg}", ex)
                End Try
            End Using
        End Using
    End Function

    Private Function GenerateUserDefinedOrderNumber(conn As SqlConnection, transaction As SqlTransaction) As String
        ' Format: BranchID + 6 + 5-digit sequence
        ' Example: Branch 6 → "6600001"
        Dim sql = "
            SELECT ISNULL(MAX(CAST(RIGHT(OrderNumber, 5) AS INT)), 0) + 1
            FROM POS_UserDefinedOrders WITH (TABLOCKX)
            WHERE OrderNumber LIKE @pattern AND LEN(OrderNumber) = 7"

        Dim pattern As String = $"{_branchID}6%"

        Using cmd As New SqlCommand(sql, conn, transaction)
            cmd.Parameters.AddWithValue("@pattern", pattern)
            Dim nextNumber As Integer = Convert.ToInt32(cmd.ExecuteScalar())
            Return $"{_branchID}6{nextNumber.ToString().PadLeft(5, "0"c)}"
        End Using
    End Function

    Private Function GetUserDefinedOrderID(conn As SqlConnection, transaction As SqlTransaction, orderNumber As String) As Integer
        Dim sql = "SELECT UserDefinedOrderID FROM POS_UserDefinedOrders WHERE OrderNumber = @OrderNumber"
        Using cmd As New SqlCommand(sql, conn, transaction)
            cmd.Parameters.AddWithValue("@OrderNumber", orderNumber)
            Return CInt(cmd.ExecuteScalar())
        End Using
    End Function

    Private Sub PrintUserDefinedOrderSlip(orderNumber As String, totalAmount As Decimal, paymentMethod As String, cashAmount As Decimal, cardAmount As Decimal)
        Try
            Dim printer As New UserDefinedOrderPrinter(_connectionString, _branchID)
            printer.PrintCreationSlip(orderNumber, _userDefinedOrderData, _cartItems, totalAmount, paymentMethod, cashAmount, cardAmount, _cashierName)
        Catch ex As Exception
            MessageBox.Show($"Error printing User Defined Order slip: {ex.Message}", "Print Error", MessageBoxButtons.OK, MessageBoxIcon.Warning)
        End Try
    End Sub

    Private Sub CollectUserDefinedOrder()
        Try
            ' Open collection dialog
            Dim dialog As New CollectUserDefinedDialog(_connectionString, _branchID, _cashierName)
            dialog.ShowDialog()
        Catch ex As Exception
            MessageBox.Show($"Error collecting User Defined Order: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    Private Sub ResetToSaleMode()
        ' Reset mode flags
        _isUserDefinedMode = False
        _isOrderMode = False
        _userDefinedOrderData = Nothing

        ' Remove User Defined button
        If _btnCompleteUserDefined IsNot Nothing AndAlso pnlCart.Controls.Contains(_btnCompleteUserDefined) Then
            pnlCart.Controls.Remove(_btnCompleteUserDefined)
            _btnCompleteUserDefined = Nothing
        End If

        ' Show Pay Now button
        Dim btnPayNow = pnlCart.Controls.Find("btnPayNow", True).FirstOrDefault()
        If btnPayNow IsNot Nothing Then
            btnPayNow.Visible = True
        End If

        ' Clear cart
        _cartItems.Clear()
        CalculateTotals()

        ' Reset breadcrumb
        lblBreadcrumb.Text = "🏠 Home > Categories"
        lblBreadcrumb.ForeColor = _ironGold

        ' Show categories
        ShowCategories()
        
        ' Focus barcode scanner for immediate scanning
        If txtBarcodeScanner IsNot Nothing Then
            txtBarcodeScanner.Focus()
        End If
    End Sub

    ' Helper methods for User Defined Orders
    Private Function CalculateSubtotal() As Decimal
        Dim totalInclVAT As Decimal = 0
        For Each row As DataRow In _cartItems.Rows
            totalInclVAT += CDec(row("Total"))
        Next
        Return Math.Round(totalInclVAT / 1.15D, 2)
    End Function

    Private Function CalculateTax(subtotal As Decimal) As Decimal
        Return Math.Round(subtotal * 0.15D, 2)
    End Function

    Private Function WriteSaleToDatabase(conn As SqlConnection, transaction As SqlTransaction, paymentMethod As String, cashAmount As Decimal, cardAmount As Decimal, totalAmount As Decimal) As Integer
        Dim subtotal = CalculateSubtotal()
        Dim taxAmount = CalculateTax(subtotal)
        
        Dim sql = "INSERT INTO Demo_Sales (SaleNumber, InvoiceNumber, BranchID, TillPointID, CashierID, SaleDate, Subtotal, TaxAmount, TotalAmount, PaymentMethod, CashAmount, CardAmount, SaleType, ReferenceNumber) 
                   VALUES (@SaleNumber, @InvoiceNumber, @BranchID, @TillPointID, @CashierID, @SaleDate, @Subtotal, @TaxAmount, @TotalAmount, @PaymentMethod, @CashAmount, @CardAmount, 'UserDefined', @ReferenceNumber);
                   SELECT CAST(SCOPE_IDENTITY() AS INT)"
        Using cmd As New SqlCommand(sql, conn, transaction)
            cmd.Parameters.AddWithValue("@SaleNumber", "UD-" & DateTime.Now.ToString("yyyyMMddHHmmss"))
            cmd.Parameters.AddWithValue("@InvoiceNumber", DBNull.Value)
            cmd.Parameters.AddWithValue("@BranchID", _branchID)
            cmd.Parameters.AddWithValue("@TillPointID", _tillPointID)
            cmd.Parameters.AddWithValue("@CashierID", _cashierID)
            cmd.Parameters.AddWithValue("@SaleDate", DateTime.Today)
            cmd.Parameters.AddWithValue("@Subtotal", subtotal)
            cmd.Parameters.AddWithValue("@TaxAmount", taxAmount)
            cmd.Parameters.AddWithValue("@TotalAmount", totalAmount)
            cmd.Parameters.AddWithValue("@PaymentMethod", paymentMethod)
            cmd.Parameters.AddWithValue("@CashAmount", If(cashAmount > 0, cashAmount, DBNull.Value))
            cmd.Parameters.AddWithValue("@CardAmount", If(cardAmount > 0, cardAmount, DBNull.Value))
            cmd.Parameters.AddWithValue("@ReferenceNumber", DBNull.Value)
            Return CInt(cmd.ExecuteScalar())
        End Using
    End Function

    Private Function GenerateInvoiceNumber(conn As SqlConnection, transaction As SqlTransaction) As String
        Dim sql = "SELECT ISNULL(MAX(CAST(SUBSTRING(InvoiceNumber, LEN(InvoiceNumber) - 5, 6) AS INT)), 0) + 1 
                   FROM POS_InvoiceLines 
                   WHERE InvoiceNumber LIKE @Prefix + '%'"
        Using cmd As New SqlCommand(sql, conn, transaction)
            cmd.Parameters.AddWithValue("@Prefix", GetBranchPrefix())
            Dim nextNumber = CInt(cmd.ExecuteScalar())
            Return $"{GetBranchPrefix()}{nextNumber.ToString("D6")}"
        End Using
    End Function

    Private Sub WriteInvoiceItems(conn As SqlConnection, transaction As SqlTransaction, invoiceNumber As String, saleID As Integer)
        ' Write to POS_InvoiceLines
        Dim sqlPOS = "INSERT INTO POS_InvoiceLines (InvoiceNumber, SalesID, BranchID, ProductID, ItemCode, ProductName, Quantity, UnitPrice, LineTotal, SaleDate, CashierID, CreatedDate) 
                      VALUES (@InvoiceNumber, @SaleID, @BranchID, @ProductID, @ItemCode, @ProductName, @Quantity, @UnitPrice, @LineTotal, GETDATE(), @CashierID, GETDATE())"
        
        ' Also write to Invoices table (for ERP integration)
        Dim sqlERP = "INSERT INTO Invoices (InvoiceNumber, SalesID, BranchID, ProductID, ItemCode, ProductName, Quantity, UnitPrice, LineTotal, SaleDate, CashierID, CreatedDate) 
                      VALUES (@InvoiceNumber, @SaleID, @BranchID, @ProductID, @ItemCode, @ProductName, @Quantity, @UnitPrice, @LineTotal, GETDATE(), @CashierID, GETDATE())"
        
        For Each row As DataRow In _cartItems.Rows
            ' Insert into POS_InvoiceLines
            Using cmd As New SqlCommand(sqlPOS, conn, transaction)
                cmd.Parameters.AddWithValue("@InvoiceNumber", invoiceNumber)
                cmd.Parameters.AddWithValue("@SaleID", saleID)
                cmd.Parameters.AddWithValue("@BranchID", _branchID)
                cmd.Parameters.AddWithValue("@ProductID", row("ProductID"))
                cmd.Parameters.AddWithValue("@ItemCode", If(IsDBNull(row("ItemCode")), DBNull.Value, row("ItemCode")))
                cmd.Parameters.AddWithValue("@ProductName", row("Product"))
                cmd.Parameters.AddWithValue("@Quantity", row("Qty"))
                cmd.Parameters.AddWithValue("@UnitPrice", row("Price"))
                cmd.Parameters.AddWithValue("@LineTotal", row("Total"))
                cmd.Parameters.AddWithValue("@CashierID", _cashierID)
                cmd.ExecuteNonQuery()
            End Using
            
            ' Insert into Invoices (ERP table)
            Using cmd As New SqlCommand(sqlERP, conn, transaction)
                cmd.Parameters.AddWithValue("@InvoiceNumber", invoiceNumber)
                cmd.Parameters.AddWithValue("@SaleID", saleID)
                cmd.Parameters.AddWithValue("@BranchID", _branchID)
                cmd.Parameters.AddWithValue("@ProductID", row("ProductID"))
                cmd.Parameters.AddWithValue("@ItemCode", If(IsDBNull(row("ItemCode")), DBNull.Value, row("ItemCode")))
                cmd.Parameters.AddWithValue("@ProductName", row("Product"))
                cmd.Parameters.AddWithValue("@Quantity", row("Qty"))
                cmd.Parameters.AddWithValue("@UnitPrice", row("Price"))
                cmd.Parameters.AddWithValue("@LineTotal", row("Total"))
                cmd.Parameters.AddWithValue("@CashierID", _cashierID)
                cmd.ExecuteNonQuery()
            End Using
        Next
    End Sub

    Private Sub UpdateStock(conn As SqlConnection, transaction As SqlTransaction)
        Dim sql = "UPDATE Demo_Retail_Product SET CurrentStock = CurrentStock - @Quantity WHERE ProductID = @ProductID AND BranchID = @BranchID"
        For Each row As DataRow In _cartItems.Rows
            Using cmd As New SqlCommand(sql, conn, transaction)
                cmd.Parameters.AddWithValue("@Quantity", row("Qty"))
                cmd.Parameters.AddWithValue("@ProductID", row("ProductID"))
                cmd.Parameters.AddWithValue("@BranchID", _branchID)
                cmd.ExecuteNonQuery()
            End Using
        Next
    End Sub

    Private Sub CreateJournalEntries(conn As SqlConnection, transaction As SqlTransaction, invoiceNumber As String, paymentMethod As String, cashAmount As Decimal, cardAmount As Decimal, totalAmount As Decimal)
        Dim journalDate = DateTime.Now
        Dim subtotal = CalculateSubtotal()
        Dim vatAmount = CalculateTax(subtotal)

        ' Get ledger IDs for journal entries
        Dim cashLedgerID = GetLedgerID(conn, transaction, "Cash")
        Dim bankLedgerID = GetLedgerID(conn, transaction, "Bank")
        Dim salesRevenueLedgerID = GetLedgerID(conn, transaction, "Sales Revenue")
        Dim vatOutputLedgerID = GetLedgerID(conn, transaction, "VAT Output")
        Dim costOfSalesLedgerID = GetLedgerID(conn, transaction, "Cost of Sales")
        Dim inventoryLedgerID = GetLedgerID(conn, transaction, "Inventory")

        ' 1. DEBIT: Cash/Bank (Asset)
        If cashAmount > 0 Then
            InsertJournalEntry(conn, transaction, journalDate, "Sales Journal", invoiceNumber, cashLedgerID, cashAmount, 0, $"User Defined Order {invoiceNumber} - Cash")
        End If
        If cardAmount > 0 Then
            InsertJournalEntry(conn, transaction, journalDate, "Sales Journal", invoiceNumber, bankLedgerID, cardAmount, 0, $"User Defined Order {invoiceNumber} - Card")
        End If

        ' 2. CREDIT: Sales Revenue
        InsertJournalEntry(conn, transaction, journalDate, "Sales Journal", invoiceNumber, salesRevenueLedgerID, 0, subtotal, $"User Defined Order {invoiceNumber}")

        ' 3. CREDIT: VAT Output
        InsertJournalEntry(conn, transaction, journalDate, "Sales Journal", invoiceNumber, vatOutputLedgerID, 0, vatAmount, $"VAT on User Defined Order {invoiceNumber}")

        ' 4. DEBIT: Cost of Sales & CREDIT: Inventory (per product)
        For Each row As DataRow In _cartItems.Rows
            Dim qty = CDec(row("Qty"))
            Dim productID = CInt(row("ProductID"))
            Dim productName = row("Product").ToString()
            Dim avgCost = GetAverageCost(conn, transaction, productID, _branchID)
            Dim totalCost = qty * avgCost
            
            If totalCost > 0 Then
                InsertJournalEntry(conn, transaction, journalDate, "Sales Journal", invoiceNumber, costOfSalesLedgerID, totalCost, 0, $"COGS for {productName}")
                InsertJournalEntry(conn, transaction, journalDate, "Sales Journal", invoiceNumber, inventoryLedgerID, 0, totalCost, $"Inventory reduction for {productName}")
            End If
        Next
    End Sub

    Private Function GetLedgerID(conn As SqlConnection, transaction As SqlTransaction, ledgerName As String) As Integer
        Dim sql = "SELECT TOP 1 LedgerID FROM Ledgers WHERE LedgerName = @LedgerName"
        Using cmd As New SqlCommand(sql, conn, transaction)
            cmd.Parameters.AddWithValue("@LedgerName", ledgerName)
            Dim result = cmd.ExecuteScalar()
            If result IsNot Nothing Then
                Return CInt(result)
            Else
                ' Create ledger if it doesn't exist
                Dim insertSql = "INSERT INTO Ledgers (LedgerName, LedgerType, IsActive) VALUES (@LedgerName, 'Asset', 1); SELECT CAST(SCOPE_IDENTITY() AS INT)"
                Using cmdInsert As New SqlCommand(insertSql, conn, transaction)
                    cmdInsert.Parameters.AddWithValue("@LedgerName", ledgerName)
                    Return CInt(cmdInsert.ExecuteScalar())
                End Using
            End If
        End Using
    End Function

    Private Function GetAverageCost(conn As SqlConnection, transaction As SqlTransaction, productID As Integer, branchID As Integer) As Decimal
        Try
            Dim sql = "SELECT TOP 1 ISNULL(CostPrice, 0) FROM Demo_Retail_Price WHERE ProductID = @ProductID AND (BranchID = @BranchID OR BranchID = 0) ORDER BY BranchID DESC"
            Using cmd As New SqlCommand(sql, conn, transaction)
                cmd.Parameters.AddWithValue("@ProductID", productID)
                cmd.Parameters.AddWithValue("@BranchID", branchID)
                Dim result = cmd.ExecuteScalar()
                Return If(result IsNot Nothing AndAlso Not IsDBNull(result), CDec(result), 0D)
            End Using
        Catch
            Return 0D
        End Try
    End Function

    Private Sub InsertJournalEntry(conn As SqlConnection, transaction As SqlTransaction, journalDate As DateTime, journalType As String, reference As String, ledgerID As Integer, debit As Decimal, credit As Decimal, description As String)
        Dim sql = "INSERT INTO GeneralJournal (TransactionDate, JournalType, Reference, LedgerID, Debit, Credit, Description, BranchID, CreatedBy, CreatedDate) VALUES (@Date, @Type, @Ref, @LedgerID, @Debit, @Credit, @Desc, @BranchID, @CreatedBy, GETDATE())"
        Using cmd As New SqlCommand(sql, conn, transaction)
            cmd.Parameters.AddWithValue("@Date", journalDate)
            cmd.Parameters.AddWithValue("@Type", journalType)
            cmd.Parameters.AddWithValue("@Ref", reference)
            cmd.Parameters.AddWithValue("@LedgerID", ledgerID)
            cmd.Parameters.AddWithValue("@Debit", debit)
            cmd.Parameters.AddWithValue("@Credit", credit)
            cmd.Parameters.AddWithValue("@Desc", description)
            cmd.Parameters.AddWithValue("@BranchID", _branchID)
            cmd.Parameters.AddWithValue("@CreatedBy", _cashierName)
            cmd.ExecuteNonQuery()
        End Using
    End Sub

    ''' <summary>
    ''' Opens priority management dialog for current subcategory
    ''' Requires supervisor authentication
    ''' </summary>
    Private Sub SetItemPriority()
        Try
            ' Check if we're viewing products (not categories or subcategories)
            If _currentView <> "products" OrElse _currentSubCategoryId = 0 Then
                MessageBox.Show("Please select a subcategory first to manage item priorities.", "No Subcategory Selected", MessageBoxButtons.OK, MessageBoxIcon.Information)
                Return
            End If

            ' Authenticate supervisor
            Dim authDialog As New SupervisorAuthDialog(_connectionString)
            If authDialog.ShowDialog(Me) <> DialogResult.OK Then
                Return
            End If

            ' Open priority management dialog
            Dim priorityDialog As New ItemPriorityManagementDialog(
                _connectionString,
                _branchID,
                _currentSubCategoryId,
                _currentSubCategoryName,
                authDialog.AuthenticatedUsername
            )

            If priorityDialog.ShowDialog(Me) = DialogResult.OK Then
                ' Refresh product display to show new priority order
                ShowProductsForSubCategory(_currentSubCategoryId, _currentSubCategoryName)
                MessageBox.Show("Item priorities updated successfully! Products are now displayed in priority order.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information)
            End If

        Catch ex As Exception
            MessageBox.Show($"Error managing item priorities: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    ''' <summary>
    ''' Opens box items dialog to create a box with multiple items
    ''' </summary>
    Private Sub CreateBoxItems()
        Try
            Dim boxDialog As New BoxItemsDialog(_connectionString, _branchID, _cashierName)
            boxDialog.ShowDialog(Me)
        Catch ex As Exception
            MessageBox.Show($"Error creating box items: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    ''' <summary>
    ''' Loads all items from a box barcode into the cart
    ''' Returns True if box was found and loaded, False otherwise
    ''' </summary>
    Private Function LoadBoxItems(boxBarcode As String) As Boolean
        Try
            Using conn As New SqlConnection(_connectionString)
                conn.Open()

                ' Get all items in the box
                Dim sql = "
                    SELECT 
                        ItemBarcode,
                        ProductName,
                        Quantity,
                        Price
                    FROM BoxedItems
                    WHERE BoxBarcode = @BoxBarcode
                      AND BranchID = @BranchID
                      AND IsActive = 1
                    ORDER BY BoxItemID"

                Using cmd As New SqlCommand(sql, conn)
                    cmd.Parameters.AddWithValue("@BoxBarcode", boxBarcode)
                    cmd.Parameters.AddWithValue("@BranchID", _branchID)

                    Dim itemCount As Integer = 0
                    Using reader = cmd.ExecuteReader()
                        While reader.Read()
                            Dim itemBarcode = reader("ItemBarcode").ToString()
                            Dim productName = reader("ProductName").ToString()
                            Dim quantity = CDec(reader("Quantity"))
                            Dim price = CDec(reader("Price"))

                            ' Get ProductID from barcode
                            Dim productID = GetProductIDFromBarcode(itemBarcode)
                            If productID > 0 Then
                                ' Add each item to cart with its quantity
                                For i = 1 To CInt(quantity)
                                    AddProductToCart(productID, itemBarcode, productName, price)
                                Next
                                itemCount += 1
                            End If
                        End While
                    End Using

                    If itemCount > 0 Then
                        txtBarcodeScanner.BackColor = _green
                        MessageBox.Show($"Box loaded successfully! {itemCount} item(s) added to cart.", "Box Loaded", MessageBoxButtons.OK, MessageBoxIcon.Information)
                        Task.Delay(200).ContinueWith(Sub()
                                                         Me.Invoke(Sub()
                                                                       txtBarcodeScanner.Clear()
                                                                       txtBarcodeScanner.BackColor = Color.White
                                                                       txtBarcodeScanner.Focus()
                                                                   End Sub)
                                                     End Sub)
                        Return True
                    Else
                        Return False
                    End If
                End Using
            End Using

        Catch ex As Exception
            MessageBox.Show($"Error loading box items: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
            txtBarcodeScanner.Clear()
            txtBarcodeScanner.Focus()
            Return False
        End Try
    End Function

    ''' <summary>
    ''' Gets ProductID from barcode
    ''' </summary>
    Private Function GetProductIDFromBarcode(barcode As String) As Integer
        Try
            Using conn As New SqlConnection(_connectionString)
                conn.Open()
                Dim sql = "
                    SELECT TOP 1 ProductID
                    FROM Demo_Retail_Product
                    WHERE (Barcode = @Barcode OR SKU = @Barcode)
                      AND BranchID = @BranchID
                      AND IsActive = 1"

                Using cmd As New SqlCommand(sql, conn)
                    cmd.Parameters.AddWithValue("@Barcode", barcode)
                    cmd.Parameters.AddWithValue("@BranchID", _branchID)

                    Dim result = cmd.ExecuteScalar()
                    If result IsNot Nothing Then
                        Return CInt(result)
                    End If
                End Using
            End Using
        Catch ex As Exception
            ' Return 0 if error
        End Try
        Return 0
    End Function
    
    Private Function AuthenticateRetailManager() As Boolean
        ' Create authentication dialog
        Dim authForm As New Form With {
            .Text = "Manager Authentication Required",
            .Size = New Size(400, 220),
            .StartPosition = FormStartPosition.CenterParent,
            .FormBorderStyle = FormBorderStyle.FixedDialog,
            .MaximizeBox = False,
            .MinimizeBox = False,
            .BackColor = Color.White
        }
        
        Dim lblTitle As New Label With {
            .Text = "Retail Manager Authentication",
            .Font = New Font("Segoe UI", 12, FontStyle.Bold),
            .Location = New Point(20, 20),
            .AutoSize = True
        }
        
        Dim lblUsername As New Label With {
            .Text = "Username:",
            .Location = New Point(20, 60),
            .AutoSize = True
        }
        
        Dim txtUsername As New TextBox With {
            .Location = New Point(120, 57),
            .Width = 240,
            .Font = New Font("Segoe UI", 10)
        }
        
        Dim lblPassword As New Label With {
            .Text = "Password:",
            .Location = New Point(20, 95),
            .AutoSize = True
        }
        
        Dim txtPassword As New TextBox With {
            .Location = New Point(120, 92),
            .Width = 240,
            .Font = New Font("Segoe UI", 10),
            .UseSystemPasswordChar = True
        }
        
        Dim btnOK As New Button With {
            .Text = "Authenticate",
            .Location = New Point(120, 135),
            .Size = New Size(110, 35),
            .BackColor = _darkBlue,
            .ForeColor = Color.White,
            .FlatStyle = FlatStyle.Flat,
            .Font = New Font("Segoe UI", 10, FontStyle.Bold)
        }
        btnOK.FlatAppearance.BorderSize = 0
        
        Dim btnCancel As New Button With {
            .Text = "Cancel",
            .Location = New Point(250, 135),
            .Size = New Size(110, 35),
            .BackColor = Color.Gray,
            .ForeColor = Color.White,
            .FlatStyle = FlatStyle.Flat,
            .Font = New Font("Segoe UI", 10, FontStyle.Bold)
        }
        btnCancel.FlatAppearance.BorderSize = 0
        
        AddHandler btnOK.Click, Sub()
            authForm.DialogResult = DialogResult.OK
            authForm.Close()
        End Sub
        
        AddHandler btnCancel.Click, Sub()
            authForm.DialogResult = DialogResult.Cancel
            authForm.Close()
        End Sub
        
        authForm.Controls.AddRange({lblTitle, lblUsername, txtUsername, lblPassword, txtPassword, btnOK, btnCancel})
        authForm.AcceptButton = btnOK
        authForm.CancelButton = btnCancel
        
        If authForm.ShowDialog() = DialogResult.OK Then
            ' Validate credentials against database
            Try
                Using conn As New SqlConnection(_connectionString)
                    conn.Open()
                    ' Check credentials and role - no branch restriction for managers
                    Dim sql = "SELECT u.UserID, r.RoleName
                              FROM Users u
                              LEFT JOIN Roles r ON u.RoleID = r.RoleID
                              WHERE u.Username = @Username 
                              AND u.Password = @Password 
                              AND u.IsActive = 1"
                    
                    Using cmd As New SqlCommand(sql, conn)
                        cmd.Parameters.AddWithValue("@Username", txtUsername.Text.Trim())
                        cmd.Parameters.AddWithValue("@Password", txtPassword.Text.Trim())
                        
                        Using reader = cmd.ExecuteReader()
                            If reader.Read() Then
                                Dim roleName = If(reader("RoleName") IsNot DBNull.Value, reader("RoleName").ToString().Trim(), "")
                                
                                ' Check if user has manager-level role
                                If roleName.IndexOf("Manager", StringComparison.OrdinalIgnoreCase) >= 0 OrElse
                                   roleName.IndexOf("Administrator", StringComparison.OrdinalIgnoreCase) >= 0 OrElse
                                   roleName.IndexOf("Supervisor", StringComparison.OrdinalIgnoreCase) >= 0 Then
                                    Return True
                                Else
                                    MessageBox.Show($"Insufficient permissions. Your role is '{roleName}'." & vbCrLf & "Only managers/supervisors can void sales.", "Access Denied", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                                    Return False
                                End If
                            Else
                                ' Invalid credentials
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
    End Function
    
    ''' <summary>
    ''' Opens the Edit Cake Order workflow (Shift+F11)
    ''' Requires Retail Manager authentication
    ''' </summary>
    Private Sub EditCakeOrder()
        Try
            ' Get branch details from database
            Dim branchName As String = ""
            Dim branchAddress As String = ""
            Dim branchPhone As String = ""
            
            Using conn As New SqlConnection(_connectionString)
                conn.Open()
                Dim sql = "SELECT BranchName, Address, Phone FROM Branches WHERE BranchID = @BranchID"
                Using cmd As New SqlCommand(sql, conn)
                    cmd.Parameters.AddWithValue("@BranchID", _branchID)
                    Using reader = cmd.ExecuteReader()
                        If reader.Read() Then
                            branchName = If(IsDBNull(reader("BranchName")), "", reader("BranchName").ToString())
                            branchAddress = If(IsDBNull(reader("Address")), "", reader("Address").ToString())
                            branchPhone = If(IsDBNull(reader("Phone")), "", reader("Phone").ToString())
                        End If
                    End Using
                End Using
            End Using
            
            ' Create and start edit workflow
            Dim editService As New CakeOrderEditService(
                _branchID, 
                _tillPointID, 
                _cashierID, 
                _cashierName,
                branchName, 
                branchAddress, 
                branchPhone
            )
            
            editService.StartEditWorkflow()
            
        Catch ex As Exception
            MessageBox.Show($"Error starting edit workflow: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub
End Class
