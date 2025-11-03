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
    Private flpProducts As FlowLayoutPanel
    Private dgvCart As DataGridView
    Private lblTotal As Label
    Private lblSubtotal As Label
    Private lblTax As Label
    Private txtSearch As TextBox
    Private txtSearchByName As TextBox
    Private txtBarcodeScanner As TextBox
    Private _onScreenKeyboard As OnScreenKeyboard

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
        
        ' Initialize screen dimensions and scaling
        InitializeScreenScaling()
        
        SetupModernUI()
        InitializeCart()
        SetupIdleScreen()

        ' Handle resize for different screen sizes
        AddHandler Me.Resize, Sub() HandleFormResize()
        AddHandler Me.Load, Sub() HandleFormResize()
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
            .Font = New Font("Segoe UI", 16, FontStyle.Bold),
            .ForeColor = Color.White,
            .AutoSize = True,
            .TextAlign = ContentAlignment.MiddleCenter,
            .Anchor = AnchorStyles.Top,
            .BackColor = Color.Transparent
        }
        ' Position after pnlTop is added to form
        lblCashier.Location = New Point(450, 20)

        pnlTop.Controls.AddRange({lblTitle, txtBarcodeScanner, lblCashier, btnCashUp, btnLogout})
        lblCashier.BringToFront()

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

        ' Search panel with barcode scanner button
        Dim pnlSearchBar As New Panel With {
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
                                      txtBarcodeScanner.Focus()
                                  End Sub

        ' Search textbox (accepts keyboard and touch input) - smaller width
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
        
        ' Clear placeholder on focus OR when user starts typing
        AddHandler txtSearch.Enter, Sub()
            If txtSearch.Text.Contains("Search by code") Then
                txtSearch.Text = ""
                txtSearch.ForeColor = Color.Black
            End If
        End Sub
        
        ' Also clear placeholder on first keypress
        AddHandler txtSearch.KeyDown, Sub(sender, e)
            If txtSearch.Text.Contains("Search by code") Then
                txtSearch.Text = ""
                txtSearch.ForeColor = Color.Black
            End If
        End Sub
        
        ' Don't restore placeholder on Leave - keep search results visible
        ' User can manually clear if needed
        
        ' Search as user types
        AddHandler txtSearch.TextChanged, Sub()
                                              If Not txtSearch.Text.Contains("Search by code") AndAlso txtSearch.Text.Length >= 2 Then
                                                  SearchProducts(txtSearch.Text)
                                              End If
                                          End Sub

        ' Search by Name textbox (accepts keyboard and touch input) - adjusted position
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
        
        ' Clear placeholder on focus OR when user starts typing
        AddHandler txtSearchByName.Enter, Sub()
            If txtSearchByName.Text.Contains("Search by name") Then
                txtSearchByName.Text = ""
                txtSearchByName.ForeColor = Color.Black
            End If
        End Sub
        
        ' Also clear placeholder on first keypress
        AddHandler txtSearchByName.KeyDown, Sub(sender, e)
            If txtSearchByName.Text.Contains("Search by name") Then
                txtSearchByName.Text = ""
                txtSearchByName.ForeColor = Color.Black
            End If
        End Sub
        
        ' Don't restore placeholder on Leave - keep search results visible
        ' User can manually clear if needed
        
        ' Search as user types
        AddHandler txtSearchByName.TextChanged, Sub()
            If Not txtSearchByName.Text.Contains("Search by name") AndAlso txtSearchByName.Text.Length >= 2 Then
                SearchProducts(txtSearchByName.Text)
            End If
        End Sub

        ' Refresh Products button - adjusted position
        Dim btnRefresh As New Button With {
            .Text = "🔄 REFRESH",
            .Font = New Font("Segoe UI", 10, FontStyle.Bold),
            .Size = New Size(110, 44),
            .Location = New Point(740, 8),
            .BackColor = _green,
            .ForeColor = Color.White,
            .FlatStyle = FlatStyle.Flat,
            .Cursor = Cursors.Hand
        }
        btnRefresh.FlatAppearance.BorderSize = 0
        AddHandler btnRefresh.Click, Sub() RefreshProductsCache()

        ' Hidden barcode scanner input
        txtBarcodeScanner = New TextBox With {
            .Location = New Point(-100, -100),
            .Size = New Size(1, 1)
        }
        AddHandler txtBarcodeScanner.KeyDown, AddressOf BarcodeScanner_KeyDown

        pnlSearchBar.Controls.AddRange({btnScan, txtSearch, txtSearchByName, btnRefresh, txtBarcodeScanner})

        flpProducts = New FlowLayoutPanel With {
            .Dock = DockStyle.Fill,
            .AutoScroll = True,
            .FlowDirection = FlowDirection.LeftToRight,
            .WrapContents = True,
            .Padding = New Padding(10),
            .BackColor = _lightGray
        }

        pnlProducts.Controls.AddRange({flpProducts, pnlSearchBar})

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
            .Height = 240,
            .BackColor = _darkBlue,
            .Padding = New Padding(20)
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

    Private Sub InitializeScreenScaling()
        ' Get current screen dimensions
        _screenWidth = Screen.PrimaryScreen.Bounds.Width
        _screenHeight = Screen.PrimaryScreen.Bounds.Height
        
        ' Calculate scale factor based on screen size
        Dim widthScale As Single = CSng(_screenWidth) / CSng(_baseWidth)
        Dim heightScale As Single = CSng(_screenHeight) / CSng(_baseHeight)
        
        ' Use the smaller scale to maintain aspect ratio
        _scaleFactor = Math.Min(widthScale, heightScale)
        
        ' Ensure minimum scale factor
        If _scaleFactor < 0.5F Then _scaleFactor = 0.5F
        
        Debug.WriteLine($"[SCREEN SCALING] Screen: {_screenWidth}x{_screenHeight}, Scale Factor: {_scaleFactor:F2}")
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
                flpProducts.Controls.Add(card)
                productCount += 1
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

        ' Responsive sizing - minimum 200x140, scales up for larger screens
        Dim cardWidth = Math.Max(200, ScaleSize(200))
        Dim cardHeight = Math.Max(140, ScaleSize(140))
        
        ' Ensure minimum touch target size (40x40 pixels)
        If cardWidth < 40 Then cardWidth = 40
        If cardHeight < 40 Then cardHeight = 40

        Dim card As New Panel With {
            .Size = New Size(cardWidth, cardHeight),
            .BackColor = Color.White,
            .BorderStyle = BorderStyle.FixedSingle,
            .Cursor = Cursors.Hand,
            .Margin = New Padding(ScaleSize(8)),
            .Tag = New With {productID, itemCode, productName, price, stock}
        }

        ' Scale font sizes responsively
        Dim codeFontSize = Math.Max(9, ScaleFont(9))
        Dim nameFontSize = Math.Max(11, ScaleFont(11))
        Dim priceFontSize = Math.Max(14, ScaleFont(14))
        Dim stockFontSize = Math.Max(8, ScaleFont(8))

        Dim lblItemCode As New Label With {
            .Text = itemCode,
            .Font = New Font("Segoe UI", codeFontSize, FontStyle.Bold),
            .ForeColor = _lightBlue,
            .Location = New Point(ScaleSize(8), ScaleSize(8)),
            .AutoSize = True
        }

        Dim lblName As New Label With {
            .Text = productName,
            .Font = New Font("Segoe UI", nameFontSize, FontStyle.Bold),
            .Location = New Point(ScaleSize(8), ScaleSize(30)),
            .Size = New Size(cardWidth - ScaleSize(16), ScaleSize(50)),
            .AutoEllipsis = True
        }

        Dim lblPrice As New Label With {
            .Text = price.ToString("C2"),
            .Font = New Font("Segoe UI", priceFontSize, FontStyle.Bold),
            .ForeColor = _green,
            .Location = New Point(ScaleSize(8), cardHeight - ScaleSize(55)),
            .AutoSize = True
        }

        ' Stock label - RED if at or below reorder level
        Dim stockColor = If(isLowStock, Color.Red, _darkGray)
        Dim stockText = If(isLowStock, $"Stock: {stock} ⚠ LOW!", $"Stock: {stock}")

        Dim lblStock As New Label With {
            .Text = stockText,
            .Font = New Font("Segoe UI", stockFontSize, If(isLowStock, FontStyle.Bold, FontStyle.Regular)),
            .ForeColor = stockColor,
            .Location = New Point(ScaleSize(8), cardHeight - ScaleSize(25)),
            .AutoSize = True
        }

        card.Controls.AddRange({lblItemCode, lblName, lblPrice, lblStock})

        ' Make entire card clickable - not just specific areas
        AddHandler card.Click, Sub() AddProductToCart(productID, itemCode, productName, price)
        AddHandler lblItemCode.Click, Sub() AddProductToCart(productID, itemCode, productName, price)
        AddHandler lblName.Click, Sub() AddProductToCart(productID, itemCode, productName, price)
        AddHandler lblPrice.Click, Sub() AddProductToCart(productID, itemCode, productName, price)
        AddHandler lblStock.Click, Sub() AddProductToCart(productID, itemCode, productName, price)
        
        AddHandler card.MouseEnter, Sub() card.BackColor = _lightGray
        AddHandler card.MouseLeave, Sub() card.BackColor = Color.White

        Return card
    End Function

    Private Sub AddProductToCart(productID As Integer, itemCode As String, productName As String, price As Decimal)
        ' Don't hide keyboards or clear search - keep search results visible
        ' User can manually close keyboard/numpad or clear search if needed
        
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
            Tuple.Create("F1", "👁️ View", CType(Sub() ViewOrders(), Action)),
            Tuple.Create("F2", "⏸️ Hold", CType(Sub() HoldSale(), Action)),
            Tuple.Create("F3", "🔍 Code", CType(Sub() ShowNumpad(), Action)),
            Tuple.Create("F4", "⌨️ Name", CType(Sub() ToggleKeyboard(), Action)),
            Tuple.Create("F5", "📋 Recall", CType(Sub() RecallSale(), Action)),
            Tuple.Create("F6", "🔢 Qty", CType(Sub() ChangeQuantity(), Action)),
            Tuple.Create("F7", "💰 Disc", CType(Sub() ApplyDiscount(), Action)),
            Tuple.Create("F8", "🗑️ Remove", CType(Sub() RemoveItem(), Action)),
            Tuple.Create("F9", "↩️ Return", CType(Sub() ProcessReturn(), Action)),
            Tuple.Create("F10", "🎂 Cake", CType(Sub() CakeOrder(), Action)),
            Tuple.Create("F11", "📝 Order", CType(Sub() CreateOrder(), Action)),
            Tuple.Create("F12", "📦 Collect", CType(Sub() OrderCollection(), Action))
        }

        Dim visibleCount = 12
        Dim screenWidth = Me.ClientSize.Width
        Dim availableWidth = screenWidth - 20
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
            Case Keys.F1 : ViewOrders() : Return True
            Case Keys.F2 : HoldSale() : Return True
            Case Keys.F3 : ShowNumpad() : Return True
            Case Keys.F4 : ToggleKeyboard() : Return True
            Case Keys.F5 : RecallSale() : Return True
            Case Keys.F6 : ChangeQuantity() : Return True
            Case Keys.F7 : ApplyDiscount() : Return True
            Case Keys.F8 : RemoveItem() : Return True
            Case Keys.F9 : ProcessReturn() : Return True
            Case Keys.F10 : CakeOrder() : Return True
            Case Keys.F11 : CreateOrder() : Return True
            Case Keys.F12 : OrderCollection() : Return True
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

    Private Sub CakeOrder()
        Try
            Debug.WriteLine("CakeOrder method called")
            Debug.WriteLine($"BranchID: {_branchID}, TillPointID: {_tillPointID}, CashierID: {_cashierID}, CashierName: {_cashierName}")

            Dim cakeForm As New CakeOrderForm(_branchID, _tillPointID, _cashierID, _cashierName)
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
        Using paymentForm As New PaymentTenderForm(_cashierID, _branchID, _tillPointID, branchPrefix, _cartItems, subtotal, tax, total, _isOrderCollectionMode, _collectionOrderID, _collectionOrderNumber)
            If paymentForm.ShowDialog() = DialogResult.OK Then
                ' If order collection, mark as delivered
                If _isOrderCollectionMode Then
                    MarkOrderAsDelivered()
                    _isOrderCollectionMode = False
                    _collectionOrderID = 0
                    _collectionOrderNumber = ""
                End If
                
                ' Clear cart and show idle screen
                _cartItems.Rows.Clear()
                CalculateTotals()
                ShowIdleScreen()
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
                        ISNULL((SELECT SUM(TotalAmount) FROM Demo_Returns WHERE CashierID = @CashierID AND CAST(ReturnDate AS DATE) = CAST(GETDATE() AS DATE)), 0) AS TotalCash,
                        ISNULL((SELECT SUM(CardAmount) FROM Demo_Sales WHERE CashierID = @CashierID AND BranchID = @BranchID AND TillPointID = @TillPointID AND CAST(SaleDate AS DATE) = CAST(GETDATE() AS DATE) AND SaleType = 'Sale'), 0) AS TotalCard,
                        ISNULL((SELECT COUNT(*) FROM Demo_Returns WHERE CashierID = @CashierID AND CAST(ReturnDate AS DATE) = CAST(GETDATE() AS DATE)), 0) AS TotalReturns,
                        ISNULL((SELECT SUM(TotalAmount) FROM Demo_Returns WHERE CashierID = @CashierID AND CAST(ReturnDate AS DATE) = CAST(GETDATE() AS DATE)), 0) AS TotalReturnAmount,
                        -- Cake Orders (OrderType='Cake')
                        ISNULL((SELECT COUNT(*) FROM Demo_Sales s INNER JOIN POS_CustomOrders o ON s.ReferenceNumber = o.OrderNumber WHERE s.CashierID = @CashierID AND s.BranchID = @BranchID AND s.TillPointID = @TillPointID AND CAST(s.SaleDate AS DATE) = CAST(GETDATE() AS DATE) AND s.SaleType IN ('OrderDeposit', 'OrderCollection') AND o.OrderType = 'Cake'), 0) AS CakeOrderTransactions,
                        ISNULL((SELECT SUM(s.TotalAmount) FROM Demo_Sales s INNER JOIN POS_CustomOrders o ON s.ReferenceNumber = o.OrderNumber WHERE s.CashierID = @CashierID AND s.BranchID = @BranchID AND s.TillPointID = @TillPointID AND CAST(s.SaleDate AS DATE) = CAST(GETDATE() AS DATE) AND s.SaleType = 'OrderDeposit' AND o.OrderType = 'Cake'), 0) AS CakeOrderDeposits,
                        ISNULL((SELECT SUM(s.TotalAmount) FROM Demo_Sales s INNER JOIN POS_CustomOrders o ON s.ReferenceNumber = o.OrderNumber WHERE s.CashierID = @CashierID AND s.BranchID = @BranchID AND s.TillPointID = @TillPointID AND CAST(s.SaleDate AS DATE) = CAST(GETDATE() AS DATE) AND s.SaleType = 'OrderCollection' AND o.OrderType = 'Cake'), 0) AS CakeOrderCollections,
                        -- General Orders (OrderType='Order')
                        ISNULL((SELECT COUNT(*) FROM Demo_Sales s INNER JOIN POS_CustomOrders o ON s.ReferenceNumber = o.OrderNumber WHERE s.CashierID = @CashierID AND s.BranchID = @BranchID AND s.TillPointID = @TillPointID AND CAST(s.SaleDate AS DATE) = CAST(GETDATE() AS DATE) AND s.SaleType IN ('OrderDeposit', 'OrderCollection') AND o.OrderType = 'Order'), 0) AS GeneralOrderTransactions,
                        ISNULL((SELECT SUM(s.TotalAmount) FROM Demo_Sales s INNER JOIN POS_CustomOrders o ON s.ReferenceNumber = o.OrderNumber WHERE s.CashierID = @CashierID AND s.BranchID = @BranchID AND s.TillPointID = @TillPointID AND CAST(s.SaleDate AS DATE) = CAST(GETDATE() AS DATE) AND s.SaleType = 'OrderDeposit' AND o.OrderType = 'Order'), 0) AS GeneralOrderDeposits,
                        ISNULL((SELECT SUM(s.TotalAmount) FROM Demo_Sales s INNER JOIN POS_CustomOrders o ON s.ReferenceNumber = o.OrderNumber WHERE s.CashierID = @CashierID AND s.BranchID = @BranchID AND s.TillPointID = @TillPointID AND CAST(s.SaleDate AS DATE) = CAST(GETDATE() AS DATE) AND s.SaleType = 'OrderCollection' AND o.OrderType = 'Order'), 0) AS GeneralOrderCollections"

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

        ' Print button
        Dim btnPrint As New Button With {
            .Text = "🖨️ PRINT",
            .Font = New Font("Segoe UI", 12, FontStyle.Bold),
            .Size = New Size(150, 50),
            .Location = New Point(30, 15),
            .BackColor = _lightBlue,
            .ForeColor = Color.White,
            .FlatStyle = FlatStyle.Flat,
            .Cursor = Cursors.Hand
        }
        btnPrint.FlatAppearance.BorderSize = 0
        AddHandler btnPrint.Click, Sub()
                                       PrintCashUpReport(transactions, subtotal, tax, total, totalCash, totalCard, totalReturns, totalReturnAmount, firstSale, lastSale)
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
                                        Me.DialogResult = DialogResult.OK
                                        Me.Close()
                                    End Sub

        pnlButtons.Controls.AddRange({btnPrint, btnClose, btnLogout})

        cashUpForm.Controls.AddRange({pnlHeader, pnlContent, pnlButtons})
        cashUpForm.ShowDialog()
    End Sub

    Private Sub PrintCashUpReport(transactions As Integer, subtotal As Decimal, tax As Decimal, total As Decimal,
                                   totalCash As Decimal, totalCard As Decimal, totalReturns As Integer,
                                   totalReturnAmount As Decimal, firstSale As DateTime, lastSale As DateTime)
        Try
            ' Build receipt text
            Dim receipt As New System.Text.StringBuilder()

            receipt.AppendLine("        CASH UP REPORT")
            receipt.AppendLine("================================")
            receipt.AppendLine()
            receipt.AppendLine($"Cashier: {_cashierName}")
            receipt.AppendLine($"Till Point: {GetTillNumber()}")
            receipt.AppendLine($"Date: {DateTime.Now:dd/MM/yyyy}")
            receipt.AppendLine($"Time: {firstSale:HH:mm} - {lastSale:HH:mm}")
            receipt.AppendLine()
            receipt.AppendLine("================================")
            receipt.AppendLine()
            receipt.AppendLine($"Total Transactions: {transactions}")
            receipt.AppendLine()
            receipt.AppendLine($"Subtotal: R{subtotal:N2}")
            receipt.AppendLine($"VAT (15%): R{tax:N2}")
            receipt.AppendLine()
            receipt.AppendLine("RETURNS:")
            receipt.AppendLine($"  Total Returns: {totalReturns}")
            receipt.AppendLine($"  Return Amount: -R{totalReturnAmount:N2}")
            receipt.AppendLine()
            receipt.AppendLine("================================")
            receipt.AppendLine()
            receipt.AppendLine($"TOTAL SALES: R{total:N2}")
            receipt.AppendLine()
            receipt.AppendLine("================================")
            receipt.AppendLine()
            receipt.AppendLine($"Cash: R{totalCash:N2}")
            receipt.AppendLine($"Card: R{totalCard:N2}")
            receipt.AppendLine()
            receipt.AppendLine("================================")
            receipt.AppendLine($"Printed: {DateTime.Now:dd/MM/yyyy HH:mm:ss}")
            receipt.AppendLine()
            receipt.AppendLine()

            ' Print to default printer (receipt printer)
            Dim printDoc As New System.Drawing.Printing.PrintDocument()
            Dim receiptText As String = receipt.ToString()

            AddHandler printDoc.PrintPage, Sub(sender, e)
                                               Dim font As New Font("Courier New", 10)
                                               e.Graphics.DrawString(receiptText, font, Brushes.Black, 10, 10)
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

            ' Filter cached products by ItemCode OR ProductName - INSTANT!
            Dim allMatches = _allProducts.AsEnumerable().
                Where(Function(row)
                          Dim itemCode = row("ItemCode").ToString()
                          Dim productName = row("ProductName").ToString()
                          Return itemCode.IndexOf(searchText, StringComparison.OrdinalIgnoreCase) >= 0 OrElse
                                 productName.IndexOf(searchText, StringComparison.OrdinalIgnoreCase) >= 0
                      End Function).
                OrderBy(Function(row)
                            Dim itemCode = row("ItemCode").ToString()
                            Dim productName = row("ProductName").ToString()
                            ' Sort by: 1) Code starts with search, 2) Name starts with search, 3) Contains search, 4) Alphabetical
                            If itemCode.StartsWith(searchText, StringComparison.OrdinalIgnoreCase) Then
                                Return 0 ' Highest priority - exact code match
                            ElseIf productName.StartsWith(searchText, StringComparison.OrdinalIgnoreCase) Then
                                Return 1 ' Second priority - name starts with search
                            Else
                                Return 2 ' Lower priority - contains search
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

    ' F11 - Create Order (from cart items)
    Private Sub CreateOrder()
        Try
            ' Check if cart has items
            If _cartItems.Rows.Count = 0 Then
                MessageBox.Show("Cart is empty! Add items first, then press F11 to create order.", "Create Order", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                Return
            End If

            ' Calculate totals
            Dim subtotal As Decimal = 0
            For Each row As DataRow In _cartItems.Rows
                subtotal += CDec(row("Total"))
            Next
            Dim tax = subtotal * 0.15D
            Dim total = subtotal + tax

            ' Show order dialog to get customer details
            Using orderDialog As New CustomerOrderDialog(_branchID, _tillPointID, _cashierID, _cashierName, _cartItems, subtotal, tax, total)
                If orderDialog.ShowDialog() = DialogResult.OK Then
                    ' Get order data
                    Dim orderData = orderDialog.GetOrderData()
                    Dim depositAmount = orderData.DepositAmount
                    
                    ' Open payment tender for deposit
                    Using paymentForm As New PaymentTenderForm(depositAmount, _branchID, _tillPointID, _cashierID)
                        If paymentForm.ShowDialog() = DialogResult.OK Then
                            ' Payment successful, now create the order in database
                            Dim orderNumber = CreateOrderInDatabase(orderData.CustomerName, orderData.CustomerSurname, orderData.CustomerPhone, orderData.ReadyDate, orderData.ReadyTime, depositAmount, total)
                            
                            If Not String.IsNullOrEmpty(orderNumber) Then
                                ' Print order receipt
                                PrintOrderReceipt(orderNumber, orderData.CustomerName, orderData.CustomerSurname, orderData.CustomerPhone, orderData.ReadyDate, orderData.ReadyTime, depositAmount, total)
                                
                                ' Clear cart
                                _cartItems.Clear()
                                CalculateTotals()
                            End If
                        End If
                    End Using
                End If
            End Using

        Catch ex As Exception
            MessageBox.Show($"Error creating order: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    Private Function CreateOrderInDatabase(customerName As String, customerSurname As String, customerPhone As String, readyDate As DateTime, readyTime As TimeSpan, depositAmount As Decimal, totalAmount As Decimal) As String
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
                                TotalAmount, DepositPaid, BalanceDue,
                                OrderStatus, CreatedBy
                            ) VALUES (
                                @OrderNumber, @BranchID, 'Order',
                                @CustomerName, @CustomerSurname, @CustomerPhone,
                                GETDATE(), @ReadyDate, @ReadyTime,
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

    Private Sub PrintOrderReceipt(orderNumber As String, customerName As String, customerSurname As String, customerPhone As String, readyDate As DateTime, readyTime As TimeSpan, depositPaid As Decimal, totalAmount As Decimal)
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
            receipt.AppendLine()
            receipt.AppendLine("╔══════════════════════════════════════╗")
            receipt.AppendLine("║   *** PICKUP LOCATION ***            ║")
            receipt.AppendLine($"║   {branchInfo.Name.ToUpper().PadRight(34)} ║")
            receipt.AppendLine($"║   {branchInfo.Address.PadRight(34)} ║")
            receipt.AppendLine($"║   Tel: {branchInfo.Phone.PadRight(28)} ║")
            receipt.AppendLine("╚══════════════════════════════════════╝")
            receipt.AppendLine()
            receipt.AppendLine("CUSTOMER DETAILS:")
            receipt.AppendLine($"Name: {customerName} {customerSurname}")
            receipt.AppendLine($"Phone: {customerPhone}")
            receipt.AppendLine()
            receipt.AppendLine("READY FOR COLLECTION:")
            receipt.AppendLine($"Date: {readyDate:dd/MM/yyyy}")
            receipt.AppendLine($"Time: {readyTime:hh\:mm}")
            receipt.AppendLine()
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
            receipt.AppendLine($"Total Amount:        R{totalAmount:N2}")
            receipt.AppendLine($"Deposit Paid:        R{depositPaid:N2}")
            receipt.AppendLine($"Balance Due:         R{(totalAmount - depositPaid):N2}")
            receipt.AppendLine()
            receipt.AppendLine("========================================")
            receipt.AppendLine("   PLEASE BRING THIS RECEIPT WHEN")
            receipt.AppendLine("       COLLECTING YOUR ORDER")
            receipt.AppendLine("========================================")
            
            ' Show receipt in message box (in production, send to printer)
            MessageBox.Show(receipt.ToString(), "Order Receipt", MessageBoxButtons.OK, MessageBoxIcon.Information)
            
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
                Dim cmd As New SqlCommand("SELECT BranchName, BranchAddress, BranchPhone FROM Branches WHERE BranchID = @branchID", conn)
                cmd.Parameters.AddWithValue("@branchID", _branchID)
                Using reader = cmd.ExecuteReader()
                    If reader.Read() Then
                        Dim name = If(reader("BranchName") IsNot DBNull.Value, reader("BranchName").ToString(), "BRANCH")
                        Dim address = If(reader("BranchAddress") IsNot DBNull.Value, reader("BranchAddress").ToString(), "Address not available")
                        Dim phone = If(reader("BranchPhone") IsNot DBNull.Value, reader("BranchPhone").ToString(), "Phone not available")
                        Return (name, address, phone)
                    End If
                End Using
            End Using
        Catch
        End Try
        Return ("BRANCH", "Address not available", "Phone not available")
    End Function

    Private Function GenerateOrderNumber(conn As SqlConnection, transaction As SqlTransaction, branchPrefix As String, Optional orderType As String = "CAKE") As String
        ' Get next sequential number with table lock to prevent duplicates
        Dim pattern As String
        Dim prefix As String
        Dim sql As String
        
        If orderType = "CAKE" Then
            pattern = $"O-{branchPrefix}-CCAKE-%"
            prefix = $"O-{branchPrefix}-CCAKE-"
            sql = "
                SELECT ISNULL(MAX(CAST(RIGHT(OrderNumber, 6) AS INT)), 0) + 1 
                FROM POS_CustomOrders WITH (TABLOCKX)
                WHERE OrderNumber LIKE @pattern"
        Else
            ' General orders - exclude CCAKE orders
            prefix = $"O-{branchPrefix}-"
            sql = "
                SELECT ISNULL(MAX(CAST(RIGHT(OrderNumber, 6) AS INT)), 0) + 1 
                FROM POS_CustomOrders WITH (TABLOCKX)
                WHERE OrderNumber LIKE @pattern AND OrderNumber NOT LIKE '%-CCAKE-%'"
            pattern = $"O-{branchPrefix}-%"
        End If
        
        Using cmd As New SqlCommand(sql, conn, transaction)
            cmd.Parameters.AddWithValue("@pattern", pattern)
            Dim nextNumber As Integer = Convert.ToInt32(cmd.ExecuteScalar())
            Return $"{prefix}{nextNumber.ToString().PadLeft(6, "0"c)}"
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

            ' Show order collection dialog
            Using dialog As New OrderCollectionDialog()
                If dialog.ShowDialog() = DialogResult.OK Then
                    LoadOrderForCollection(dialog.OrderNumber)
                End If
            End Using

        Catch ex As Exception
            ShowError("Error", ex.Message)
        End Try
    End Sub

    ' Order collection variables
    Private _isOrderCollectionMode As Boolean = False
    Private _collectionOrderID As Integer = 0
    Private _collectionOrderNumber As String = ""
    Private _collectionDepositPaid As Decimal = 0
    Private _collectionTotalAmount As Decimal = 0

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
                    Dim balanceDue As Decimal = Convert.ToDecimal(reader("BalanceDue"))
                    Dim customerName As String = $"{reader("CustomerName")} {reader("CustomerSurname")}"
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
            Using paymentForm As New PaymentTenderForm(balanceDue, _branchID, _tillPointID, _cashierID)
                If paymentForm.ShowDialog() = DialogResult.OK Then
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
                    PrintCollectionReceipt(_collectionOrderNumber, balanceDue, _collectionDepositPaid, _collectionTotalAmount, paymentMethod, cashAmount, cardAmount, paymentForm.ChangeAmount)
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
    
    Private Sub PrintCollectionReceipt(orderNumber As String, balancePaid As Decimal, depositPaid As Decimal, totalAmount As Decimal, paymentMethod As String, cashAmount As Decimal, cardAmount As Decimal, changeAmount As Decimal)
        Try
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
            receipt.AppendLine($"Date: {DateTime.Now:dd/MM/yyyy HH:mm}")
            receipt.AppendLine($"Cashier: {_cashierName}")
            receipt.AppendLine()
            receipt.AppendLine("PAYMENT SUMMARY:")
            receipt.AppendLine($"Order Total: R{totalAmount:N2}")
            receipt.AppendLine($"Deposit Paid: R{depositPaid:N2}")
            receipt.AppendLine($"Balance Paid: R{balancePaid:N2}")
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
End Class
