Imports System.Configuration
Imports System.Drawing
Imports System.Windows.Forms

Public Class POSMainForm
    Private _primaryColor As Color
    Private _accentColor As Color
    Private _dataService As POSDataService
    Private _cashierID As Integer
    Private _cashierName As String
    Private _branchID As Integer
    Private _cartItems As New DataTable()
    Private _currentSubtotal As Decimal = 0
    Private _currentTax As Decimal = 0
    Private _currentTotal As Decimal = 0
    Private _idleScreen As IdleScreen
    Private _idleTimer As Timer
    Private Const IDLE_TIMEOUT_MS As Integer = 60000 ' 1 minute

    ' UI Controls
    Private pnlCategories As Panel
    Private pnlProducts As Panel
    Private pnlCart As Panel
    Private pnlShortcuts As Panel
    Private dgvProducts As DataGridView
    Private dgvCart As DataGridView
    Private txtSearch As TextBox
    Private lblTotal As Label
    Private btnMoreShortcuts As Button
    Private shortcutsExpanded As Boolean = False

    Public Sub New(cashierID As Integer, cashierName As String, branchID As Integer)
        InitializeComponent()
        
        _cashierID = cashierID
        _cashierName = cashierName
        _branchID = branchID
        _dataService = New POSDataService()
        
        LoadColors()
        SetupUI()
        InitializeCart()
        LoadCategories()
        LoadProducts()
        SetupIdleScreen()
        SetupShortcutKeys()
    End Sub

    Private Sub LoadColors()
        Dim primaryHex = ConfigurationManager.AppSettings("PrimaryColor") ?? "#D2691E"
        Dim accentHex = ConfigurationManager.AppSettings("AccentColor") ?? "#FFD700"
        _primaryColor = ColorTranslator.FromHtml(primaryHex)
        _accentColor = ColorTranslator.FromHtml(accentHex)
    End Sub

    Private Sub SetupUI()
        ' Form settings
        Me.FormBorderStyle = FormBorderStyle.None
        Me.WindowState = FormWindowState.Maximized
        Me.BackColor = Color.FromArgb(240, 240, 240)
        Me.Text = "Oven Delights POS"

        ' Top bar
        Dim pnlTop As New Panel With {
            .Dock = DockStyle.Top,
            .Height = 60,
            .BackColor = _primaryColor
        }

        Dim lblTitle As New Label With {
            .Text = ConfigurationManager.AppSettings("CompanyName") ?? "Oven Delights",
            .Font = New Font("Segoe UI", 20, FontStyle.Bold),
            .ForeColor = Color.White,
            .Location = New Point(20, 15),
            .AutoSize = True
        }

        Dim lblCashier As New Label With {
            .Text = $"Cashier: {_cashierName}",
            .Font = New Font("Segoe UI", 12),
            .ForeColor = Color.White,
            .Location = New Point(Me.Width - 250, 20),
            .AutoSize = True
        }
        lblCashier.Anchor = AnchorStyles.Top Or AnchorStyles.Right

        Dim btnLogout As New Button With {
            .Text = "Logout",
            .Font = New Font("Segoe UI", 10, FontStyle.Bold),
            .Size = New Size(80, 35),
            .Location = New Point(Me.Width - 100, 12),
            .BackColor = Color.FromArgb(220, 53, 69),
            .ForeColor = Color.White,
            .FlatStyle = FlatStyle.Flat,
            .Cursor = Cursors.Hand
        }
        btnLogout.Anchor = AnchorStyles.Top Or AnchorStyles.Right
        btnLogout.FlatAppearance.BorderSize = 0
        AddHandler btnLogout.Click, AddressOf btnLogout_Click

        pnlTop.Controls.AddRange({lblTitle, lblCashier, btnLogout})

        ' LEFT PANEL - Categories (20% width)
        pnlCategories = New Panel With {
            .Dock = DockStyle.Left,
            .Width = CInt(Me.Width * 0.2),
            .BackColor = Color.White,
            .Padding = New Padding(10)
        }

        Dim lblCategories As New Label With {
            .Text = "CATEGORIES",
            .Font = New Font("Segoe UI", 14, FontStyle.Bold),
            .ForeColor = _primaryColor,
            .Dock = DockStyle.Top,
            .Height = 40,
            .TextAlign = ContentAlignment.MiddleLeft
        }
        pnlCategories.Controls.Add(lblCategories)

        ' CENTER PANEL - Products (50% width)
        pnlProducts = New Panel With {
            .Dock = DockStyle.Fill,
            .BackColor = Color.FromArgb(245, 245, 245),
            .Padding = New Padding(10)
        }

        ' Search box
        txtSearch = New TextBox With {
            .Font = New Font("Segoe UI", 14),
            .Dock = DockStyle.Top,
            .Height = 40,
            .PlaceholderText = "Search products (F3)..."
        }
        AddHandler txtSearch.TextChanged, AddressOf txtSearch_TextChanged

        ' Products grid
        dgvProducts = New DataGridView With {
            .Dock = DockStyle.Fill,
            .BackgroundColor = Color.White,
            .BorderStyle = BorderStyle.None,
            .AllowUserToAddRows = False,
            .AllowUserToDeleteRows = False,
            .ReadOnly = True,
            .SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            .MultiSelect = False,
            .RowHeadersVisible = False,
            .AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            .RowTemplate = New DataGridViewRow With {.Height = 60},
            .Font = New Font("Segoe UI", 12)
        }
        AddHandler dgvProducts.CellDoubleClick, AddressOf dgvProducts_CellDoubleClick
        AddHandler dgvProducts.KeyDown, AddressOf dgvProducts_KeyDown

        pnlProducts.Controls.AddRange({dgvProducts, txtSearch})

        ' RIGHT PANEL - Cart (30% width)
        pnlCart = New Panel With {
            .Dock = DockStyle.Right,
            .Width = CInt(Me.Width * 0.3),
            .BackColor = Color.White,
            .Padding = New Padding(10)
        }

        Dim lblCart As New Label With {
            .Text = "CURRENT SALE",
            .Font = New Font("Segoe UI", 14, FontStyle.Bold),
            .ForeColor = _primaryColor,
            .Dock = DockStyle.Top,
            .Height = 40,
            .TextAlign = ContentAlignment.MiddleLeft
        }

        ' Cart grid
        dgvCart = New DataGridView With {
            .Dock = DockStyle.Fill,
            .BackgroundColor = Color.White,
            .BorderStyle = BorderStyle.None,
            .AllowUserToAddRows = False,
            .ReadOnly = False,
            .SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            .RowHeadersVisible = False,
            .AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            .RowTemplate = New DataGridViewRow With {.Height = 50},
            .Font = New Font("Segoe UI", 11)
        }
        AddHandler dgvCart.CellValueChanged, AddressOf dgvCart_CellValueChanged
        AddHandler dgvCart.KeyDown, AddressOf dgvCart_KeyDown

        ' Total panel
        Dim pnlTotal As New Panel With {
            .Dock = DockStyle.Bottom,
            .Height = 180,
            .BackColor = _primaryColor,
            .Padding = New Padding(15)
        }

        Dim lblSubtotalLabel As New Label With {
            .Text = "Subtotal:",
            .Font = New Font("Segoe UI", 14),
            .ForeColor = Color.White,
            .Location = New Point(15, 15),
            .AutoSize = True
        }

        Dim lblSubtotalValue As New Label With {
            .Name = "lblSubtotalValue",
            .Text = "R 0.00",
            .Font = New Font("Segoe UI", 14, FontStyle.Bold),
            .ForeColor = Color.White,
            .Location = New Point(pnlTotal.Width - 150, 15),
            .AutoSize = True
        }
        lblSubtotalValue.Anchor = AnchorStyles.Top Or AnchorStyles.Right

        Dim lblTaxLabel As New Label With {
            .Text = "VAT (15%):",
            .Font = New Font("Segoe UI", 14),
            .ForeColor = Color.White,
            .Location = New Point(15, 50),
            .AutoSize = True
        }

        Dim lblTaxValue As New Label With {
            .Name = "lblTaxValue",
            .Text = "R 0.00",
            .Font = New Font("Segoe UI", 14, FontStyle.Bold),
            .ForeColor = Color.White,
            .Location = New Point(pnlTotal.Width - 150, 50),
            .AutoSize = True
        }
        lblTaxValue.Anchor = AnchorStyles.Top Or AnchorStyles.Right

        lblTotal = New Label With {
            .Name = "lblTotalValue",
            .Text = "R 0.00",
            .Font = New Font("Segoe UI", 28, FontStyle.Bold),
            .ForeColor = _accentColor,
            .Location = New Point(15, 90),
            .AutoSize = True
        }

        Dim btnPay As New Button With {
            .Text = "PAY (F12)",
            .Font = New Font("Segoe UI", 16, FontStyle.Bold),
            .Size = New Size(pnlTotal.Width - 30, 60),
            .Location = New Point(15, 140),
            .BackColor = Color.FromArgb(40, 167, 69),
            .ForeColor = Color.White,
            .FlatStyle = FlatStyle.Flat,
            .Cursor = Cursors.Hand
        }
        btnPay.Anchor = AnchorStyles.Bottom Or AnchorStyles.Left Or AnchorStyles.Right
        btnPay.FlatAppearance.BorderSize = 0
        AddHandler btnPay.Click, AddressOf btnPay_Click

        pnlTotal.Controls.AddRange({lblSubtotalLabel, lblSubtotalValue, lblTaxLabel, lblTaxValue, lblTotal, btnPay})
        pnlCart.Controls.AddRange({pnlTotal, dgvCart, lblCart})

        ' BOTTOM PANEL - Shortcuts
        pnlShortcuts = New Panel With {
            .Dock = DockStyle.Bottom,
            .Height = 80,
            .BackColor = Color.FromArgb(250, 250, 250),
            .Padding = New Padding(10, 5, 10, 5)
        }

        CreateShortcutButtons()

        ' Add all panels to form
        Me.Controls.AddRange({pnlProducts, pnlCart, pnlCategories, pnlShortcuts, pnlTop})
    End Sub

    Private Sub CreateShortcutButtons()
        pnlShortcuts.Controls.Clear()

        Dim shortcuts = New List(Of Tuple(Of String, String, Action)) From {
            Tuple.Create("F1", "New Sale", Sub() NewSale()),
            Tuple.Create("F2", "Hold", Sub() HoldSale()),
            Tuple.Create("F3", "Search", Sub() txtSearch.Focus()),
            Tuple.Create("F4", "Recall", Sub() RecallSale()),
            Tuple.Create("F5", "Qty", Sub() ChangeQuantity()),
            Tuple.Create("F6", "Discount", Sub() ApplyDiscount()),
            Tuple.Create("F7", "Remove", Sub() RemoveItem()),
            Tuple.Create("F8", "Returns", Sub() ProcessReturn()),
            Tuple.Create("F9", "Reports", Sub() ShowReports()),
            Tuple.Create("F10", "Cash Drawer", Sub() OpenCashDrawer()),
            Tuple.Create("F11", "Manager", Sub() ManagerFunctions()),
            Tuple.Create("F12", "Pay", Sub() btnPay_Click(Nothing, Nothing))
        }

        Dim visibleCount = If(shortcutsExpanded, shortcuts.Count, 8)
        Dim buttonWidth = (pnlShortcuts.Width - 30) \ visibleCount - 5

        For i = 0 To visibleCount - 1
            Dim shortcut = shortcuts(i)
            Dim btn As New Button With {
                .Text = $"{shortcut.Item1}{vbCrLf}{shortcut.Item2}",
                .Size = New Size(buttonWidth, 65),
                .Location = New Point(10 + (i * (buttonWidth + 5)), 5),
                .BackColor = Color.White,
                .ForeColor = _primaryColor,
                .Font = New Font("Segoe UI", 9, FontStyle.Bold),
                .FlatStyle = FlatStyle.Flat,
                .Cursor = Cursors.Hand,
                .Tag = shortcut.Item3
            }
            btn.FlatAppearance.BorderColor = _primaryColor
            btn.FlatAppearance.BorderSize = 2
            AddHandler btn.Click, Sub(s, e) CType(CType(s, Button).Tag, Action).Invoke()
            pnlShortcuts.Controls.Add(btn)
        Next

        ' More button if not expanded
        If Not shortcutsExpanded AndAlso shortcuts.Count > 8 Then
            btnMoreShortcuts = New Button With {
                .Text = "▼ More",
                .Size = New Size(80, 65),
                .Location = New Point(pnlShortcuts.Width - 95, 5),
                .BackColor = _accentColor,
                .ForeColor = Color.Black,
                .Font = New Font("Segoe UI", 9, FontStyle.Bold),
                .FlatStyle = FlatStyle.Flat,
                .Cursor = Cursors.Hand
            }
            btnMoreShortcuts.Anchor = AnchorStyles.Bottom Or AnchorStyles.Right
            btnMoreShortcuts.FlatAppearance.BorderSize = 0
            AddHandler btnMoreShortcuts.Click, Sub()
                                                   shortcutsExpanded = True
                                                   pnlShortcuts.Height = 150
                                                   CreateShortcutButtons()
                                               End Sub
            pnlShortcuts.Controls.Add(btnMoreShortcuts)
        ElseIf shortcutsExpanded Then
            btnMoreShortcuts = New Button With {
                .Text = "▲ Less",
                .Size = New Size(80, 65),
                .Location = New Point(pnlShortcuts.Width - 95, 80),
                .BackColor = _accentColor,
                .ForeColor = Color.Black,
                .Font = New Font("Segoe UI", 9, FontStyle.Bold),
                .FlatStyle = FlatStyle.Flat,
                .Cursor = Cursors.Hand
            }
            btnMoreShortcuts.Anchor = AnchorStyles.Bottom Or AnchorStyles.Right
            btnMoreShortcuts.FlatAppearance.BorderSize = 0
            AddHandler btnMoreShortcuts.Click, Sub()
                                                   shortcutsExpanded = False
                                                   pnlShortcuts.Height = 80
                                                   CreateShortcutButtons()
                                               End Sub
            pnlShortcuts.Controls.Add(btnMoreShortcuts)
        End If
    End Sub

    Private Sub InitializeCart()
        _cartItems.Columns.Add("VariantID", GetType(Integer))
        _cartItems.Columns.Add("SKU", GetType(String))
        _cartItems.Columns.Add("Product", GetType(String))
        _cartItems.Columns.Add("Qty", GetType(Decimal))
        _cartItems.Columns.Add("Price", GetType(Decimal))
        _cartItems.Columns.Add("Total", GetType(Decimal))
        
        dgvCart.DataSource = _cartItems
        dgvCart.Columns("VariantID").Visible = False
        dgvCart.Columns("SKU").Width = 80
        dgvCart.Columns("Qty").Width = 60
        dgvCart.Columns("Qty").ReadOnly = False
        dgvCart.Columns("Price").DefaultCellStyle.Format = "C2"
        dgvCart.Columns("Total").DefaultCellStyle.Format = "C2"
    End Sub

    Private Sub LoadCategories()
        Try
            Dim categories = _dataService.GetCategories()
            
            Dim yPos = 50
            For Each category In categories
                Dim btn As New Button With {
                    .Text = category,
                    .Size = New Size(pnlCategories.Width - 30, 60),
                    .Location = New Point(10, yPos),
                    .BackColor = Color.White,
                    .ForeColor = _primaryColor,
                    .Font = New Font("Segoe UI", 12, FontStyle.Bold),
                    .FlatStyle = FlatStyle.Flat,
                    .Cursor = Cursors.Hand,
                    .TextAlign = ContentAlignment.MiddleLeft,
                    .Padding = New Padding(15, 0, 0, 0),
                    .Tag = category
                }
                btn.FlatAppearance.BorderColor = Color.FromArgb(200, 200, 200)
                AddHandler btn.Click, AddressOf CategoryButton_Click
                AddHandler btn.MouseEnter, Sub(s, e) btn.BackColor = Color.FromArgb(240, 240, 240)
                AddHandler btn.MouseLeave, Sub(s, e) btn.BackColor = Color.White
                
                pnlCategories.Controls.Add(btn)
                yPos += 65
            Next
            
        Catch ex As Exception
            MessageBox.Show($"Error loading categories: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    Private Sub LoadProducts(Optional category As String = Nothing)
        Try
            Dim products = _dataService.GetProductsWithStock(_branchID)
            
            If Not String.IsNullOrEmpty(category) Then
                products = products.Select($"Category = '{category}'").CopyToDataTable()
            End If
            
            dgvProducts.DataSource = products
            dgvProducts.Columns("ProductID").Visible = False
            dgvProducts.Columns("VariantID").Visible = False
            dgvProducts.Columns("Barcode").Visible = False
            dgvProducts.Columns("CostPrice").Visible = False
            dgvProducts.Columns("ReorderPoint").Visible = False
            
            dgvProducts.Columns("SKU").HeaderText = "Code"
            dgvProducts.Columns("ProductName").HeaderText = "Product"
            dgvProducts.Columns("SellingPrice").HeaderText = "Price"
            dgvProducts.Columns("QtyOnHand").HeaderText = "Stock"
            dgvProducts.Columns("SellingPrice").DefaultCellStyle.Format = "C2"
            
        Catch ex As Exception
            MessageBox.Show($"Error loading products: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    Private Sub CategoryButton_Click(sender As Object, e As EventArgs)
        Dim btn = CType(sender, Button)
        Dim category = btn.Tag.ToString()
        LoadProducts(category)
        ResetIdleTimer()
    End Sub

    Private Sub txtSearch_TextChanged(sender As Object, e As EventArgs)
        If String.IsNullOrWhiteSpace(txtSearch.Text) Then
            LoadProducts()
        Else
            Try
                Dim results = _dataService.SearchProducts(txtSearch.Text, _branchID)
                dgvProducts.DataSource = results
            Catch ex As Exception
                MessageBox.Show($"Search error: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
            End Try
        End If
        ResetIdleTimer()
    End Sub

    Private Sub dgvProducts_CellDoubleClick(sender As Object, e As DataGridViewCellEventArgs)
        If e.RowIndex >= 0 Then
            AddToCart()
        End If
    End Sub

    Private Sub dgvProducts_KeyDown(sender As Object, e As KeyEventArgs)
        If e.KeyCode = Keys.Enter Then
            AddToCart()
            e.Handled = True
        End If
    End Sub

    Private Sub AddToCart()
        If dgvProducts.CurrentRow Is Nothing Then Return
        
        Try
            Dim variantID = CInt(dgvProducts.CurrentRow.Cells("VariantID").Value)
            Dim sku = dgvProducts.CurrentRow.Cells("SKU").Value.ToString()
            Dim productName = dgvProducts.CurrentRow.Cells("ProductName").Value.ToString()
            Dim price = CDec(dgvProducts.CurrentRow.Cells("SellingPrice").Value)
            Dim stock = CDec(dgvProducts.CurrentRow.Cells("QtyOnHand").Value)
            
            If stock <= 0 Then
                MessageBox.Show("Product out of stock!", "Stock Error", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                Return
            End If
            
            ' Check if already in cart
            Dim existingRow = _cartItems.Select($"VariantID = {variantID}")
            If existingRow.Length > 0 Then
                existingRow(0)("Qty") = CDec(existingRow(0)("Qty")) + 1
                existingRow(0)("Total") = CDec(existingRow(0)("Qty")) * CDec(existingRow(0)("Price"))
            Else
                _cartItems.Rows.Add(variantID, sku, productName, 1, price, price)
            End If
            
            CalculateTotals()
            ResetIdleTimer()
            
        Catch ex As Exception
            MessageBox.Show($"Error adding to cart: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    Private Sub dgvCart_CellValueChanged(sender As Object, e As DataGridViewCellEventArgs)
        If e.RowIndex >= 0 AndAlso e.ColumnIndex = dgvCart.Columns("Qty").Index Then
            Try
                Dim row = dgvCart.Rows(e.RowIndex)
                Dim qty = CDec(row.Cells("Qty").Value)
                Dim price = CDec(row.Cells("Price").Value)
                row.Cells("Total").Value = qty * price
                CalculateTotals()
            Catch ex As Exception
                MessageBox.Show("Invalid quantity", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
            End Try
        End If
    End Sub

    Private Sub dgvCart_KeyDown(sender As Object, e As KeyEventArgs)
        If e.KeyCode = Keys.Delete Then
            RemoveItem()
            e.Handled = True
        End If
    End Sub

    Private Sub CalculateTotals()
        _currentSubtotal = 0
        For Each row As DataRow In _cartItems.Rows
            _currentSubtotal += CDec(row("Total"))
        Next
        
        Dim vatRate = CDec(ConfigurationManager.AppSettings("VATRate") ?? "0.15")
        _currentTax = _currentSubtotal * vatRate
        _currentTotal = _currentSubtotal + _currentTax
        
        ' Update labels
        Dim lblSubtotal = TryCast(pnlCart.Controls.Find("lblSubtotalValue", True).FirstOrDefault(), Label)
        Dim lblTax = TryCast(pnlCart.Controls.Find("lblTaxValue", True).FirstOrDefault(), Label)
        Dim lblTotal = TryCast(pnlCart.Controls.Find("lblTotalValue", True).FirstOrDefault(), Label)
        
        If lblSubtotal IsNot Nothing Then lblSubtotal.Text = _currentSubtotal.ToString("C2")
        If lblTax IsNot Nothing Then lblTax.Text = _currentTax.ToString("C2")
        If lblTotal IsNot Nothing Then lblTotal.Text = _currentTotal.ToString("C2")
    End Sub

    Private Sub btnPay_Click(sender As Object, e As EventArgs)
        If _cartItems.Rows.Count = 0 Then
            MessageBox.Show("Cart is empty!", "Payment Error", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            Return
        End If
        
        ' TODO: Open payment form
        MessageBox.Show($"Payment processing for: {_currentTotal:C2}", "Payment", MessageBoxButtons.OK, MessageBoxIcon.Information)
        ResetIdleTimer()
    End Sub

    Private Sub btnLogout_Click(sender As Object, e As EventArgs)
        If _cartItems.Rows.Count > 0 Then
            Dim result = MessageBox.Show("Current sale will be lost. Continue?", "Confirm Logout", MessageBoxButtons.YesNo, MessageBoxIcon.Question)
            If result = DialogResult.No Then Return
        End If
        
        Me.Close()
    End Sub

    ' Shortcut functions
    Private Sub NewSale()
        If _cartItems.Rows.Count > 0 Then
            Dim result = MessageBox.Show("Clear current sale?", "New Sale", MessageBoxButtons.YesNo, MessageBoxIcon.Question)
            If result = DialogResult.No Then Return
        End If
        _cartItems.Clear()
        CalculateTotals()
        txtSearch.Clear()
        txtSearch.Focus()
        ResetIdleTimer()
    End Sub

    Private Sub HoldSale()
        MessageBox.Show("Hold sale feature coming soon!", "Hold Sale", MessageBoxButtons.OK, MessageBoxIcon.Information)
    End Sub

    Private Sub RecallSale()
        MessageBox.Show("Recall sale feature coming soon!", "Recall Sale", MessageBoxButtons.OK, MessageBoxIcon.Information)
    End Sub

    Private Sub ChangeQuantity()
        If dgvCart.CurrentRow Is Nothing Then Return
        Dim currentQty = CDec(dgvCart.CurrentRow.Cells("Qty").Value)
        Dim input = InputBox("Enter new quantity:", "Change Quantity", currentQty.ToString())
        If Not String.IsNullOrEmpty(input) AndAlso IsNumeric(input) Then
            dgvCart.CurrentRow.Cells("Qty").Value = CDec(input)
            dgvCart_CellValueChanged(dgvCart, New DataGridViewCellEventArgs(dgvCart.Columns("Qty").Index, dgvCart.CurrentRow.Index))
        End If
        ResetIdleTimer()
    End Sub

    Private Sub ApplyDiscount()
        MessageBox.Show("Discount feature coming soon!", "Discount", MessageBoxButtons.OK, MessageBoxIcon.Information)
    End Sub

    Private Sub RemoveItem()
        If dgvCart.CurrentRow IsNot Nothing Then
            dgvCart.Rows.Remove(dgvCart.CurrentRow)
            CalculateTotals()
        End If
        ResetIdleTimer()
    End Sub

    Private Sub ProcessReturn()
        MessageBox.Show("Returns feature coming soon!", "Returns", MessageBoxButtons.OK, MessageBoxIcon.Information)
    End Sub

    Private Sub ShowReports()
        MessageBox.Show("Reports feature coming soon!", "Reports", MessageBoxButtons.OK, MessageBoxIcon.Information)
    End Sub

    Private Sub OpenCashDrawer()
        MessageBox.Show("Cash drawer opened!", "Cash Drawer", MessageBoxButtons.OK, MessageBoxIcon.Information)
    End Sub

    Private Sub ManagerFunctions()
        MessageBox.Show("Manager functions coming soon!", "Manager", MessageBoxButtons.OK, MessageBoxIcon.Information)
    End Sub

    ' Idle screen functionality
    Private Sub SetupIdleScreen()
        _idleScreen = New IdleScreen With {
            .Visible = False,
            .Dock = DockStyle.Fill
        }
        Me.Controls.Add(_idleScreen)
        _idleScreen.BringToFront()
        
        _idleTimer = New Timer With {.Interval = IDLE_TIMEOUT_MS}
        AddHandler _idleTimer.Tick, AddressOf IdleTimer_Tick
        _idleTimer.Start()
        
        ' Click anywhere to dismiss idle screen
        AddHandler _idleScreen.Click, AddressOf DismissIdleScreen
    End Sub

    Private Sub IdleTimer_Tick(sender As Object, e As EventArgs)
        If _cartItems.Rows.Count = 0 Then
            ShowIdleScreen()
        End If
    End Sub

    Private Sub ShowIdleScreen()
        _idleScreen.Visible = True
        _idleScreen.BringToFront()
    End Sub

    Private Sub DismissIdleScreen(sender As Object, e As EventArgs)
        _idleScreen.Visible = False
        ResetIdleTimer()
    End Sub

    Private Sub ResetIdleTimer()
        _idleTimer.Stop()
        _idleTimer.Start()
        If _idleScreen.Visible Then
            _idleScreen.Visible = False
        End If
    End Sub

    ' Keyboard shortcuts
    Private Sub SetupShortcutKeys()
        Me.KeyPreview = True
        AddHandler Me.KeyDown, AddressOf POSMainForm_KeyDown
    End Sub

    Private Sub POSMainForm_KeyDown(sender As Object, e As KeyEventArgs)
        Select Case e.KeyCode
            Case Keys.F1
                NewSale()
            Case Keys.F2
                HoldSale()
            Case Keys.F3
                txtSearch.Focus()
            Case Keys.F4
                RecallSale()
            Case Keys.F5
                ChangeQuantity()
            Case Keys.F6
                ApplyDiscount()
            Case Keys.F7
                RemoveItem()
            Case Keys.F8
                ProcessReturn()
            Case Keys.F9
                ShowReports()
            Case Keys.F10
                OpenCashDrawer()
            Case Keys.F11
                ManagerFunctions()
            Case Keys.F12
                btnPay_Click(Nothing, Nothing)
        End Select
        
        ResetIdleTimer()
    End Sub

    Protected Overrides Sub OnFormClosing(e As FormClosingEventArgs)
        _idleTimer?.Stop()
        _idleTimer?.Dispose()
        MyBase.OnFormClosing(e)
    End Sub
End Class
