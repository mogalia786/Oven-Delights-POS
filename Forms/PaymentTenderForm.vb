Imports System.Configuration
Imports System.Data.SqlClient
Imports System.Drawing
Imports System.Windows.Forms

Public Class PaymentTenderForm
    Inherits Form

    Private _connectionString As String
    Private _cashierID As Integer
    Private _cashierName As String
    Private _branchID As Integer
    Private _tillPointID As Integer
    Private _branchPrefix As String
    Private _cartItems As DataTable
    Private _totalAmount As Decimal
    Private _subtotal As Decimal
    Private _taxAmount As Decimal
    Private _cashAmount As Decimal = 0
    Private _cardAmount As Decimal = 0
    Private _changeAmount As Decimal = 0
    Private _paymentMethod As String = ""
    Private _isOrderCollection As Boolean = False
    Private _orderID As Integer = 0
    Private _orderNumber As String = ""
    Private _orderColour As String = ""
    Private _orderPicture As String = ""
    
    ' Public properties to expose payment details
    Public ReadOnly Property PaymentMethod As String
        Get
            Return _paymentMethod
        End Get
    End Property
    
    Public ReadOnly Property CashAmount As Decimal
        Get
            Return _cashAmount
        End Get
    End Property
    
    Public ReadOnly Property CardAmount As Decimal
        Get
            Return _cardAmount
        End Get
    End Property
    
    Public ReadOnly Property ChangeAmount As Decimal
        Get
            Return _changeAmount
        End Get
    End Property
    
    ' Iron Man Theme Color Palette (from pos_styles.css)
    Private _ironRed As Color = ColorTranslator.FromHtml("#C1272D")
    Private _ironRedDark As Color = ColorTranslator.FromHtml("#8B0000")
    Private _ironGold As Color = ColorTranslator.FromHtml("#FFD700")
    Private _ironDark As Color = ColorTranslator.FromHtml("#0a0e27")
    Private _ironBlue As Color = ColorTranslator.FromHtml("#00D4FF")
    Private _ironBlueDark As Color = ColorTranslator.FromHtml("#0099CC")
    Private _ironDarkBlue As Color = ColorTranslator.FromHtml("#1a1f3a")
    
    ' Tender button colors from CSS
    Private _tenderCash As Color = ColorTranslator.FromHtml("#27AE60")
    Private _tenderCashDark As Color = ColorTranslator.FromHtml("#1E8449")
    Private _tenderCard As Color = ColorTranslator.FromHtml("#9B59B6")
    Private _tenderCardDark As Color = ColorTranslator.FromHtml("#7D3C98")
    Private _tenderEFT As Color = ColorTranslator.FromHtml("#00D4FF")
    Private _tenderEFTDark As Color = ColorTranslator.FromHtml("#0099CC")
    Private _tenderManual As Color = ColorTranslator.FromHtml("#E67E22")
    Private _tenderManualDark As Color = ColorTranslator.FromHtml("#CA6F1E")
    Private _tenderSplit As Color = ColorTranslator.FromHtml("#F39C12")
    Private _tenderSplitDark As Color = ColorTranslator.FromHtml("#D68910")
    
    ' Legacy colors for compatibility with existing code
    Private _darkBlue As Color = ColorTranslator.FromHtml("#2C3E50")
    Private _green As Color = ColorTranslator.FromHtml("#27AE60")
    Private _lightGray As Color = ColorTranslator.FromHtml("#ECF0F1")
    
    ' Constructor for regular sales and order collection
    Public Sub New(cashierID As Integer, cashierName As String, branchID As Integer, tillPointID As Integer, branchPrefix As String, cartItems As DataTable, subtotal As Decimal, taxAmount As Decimal, totalAmount As Decimal, Optional isOrderCollection As Boolean = False, Optional orderID As Integer = 0, Optional orderNumber As String = "", Optional orderColour As String = "", Optional orderPicture As String = "")
        MyBase.New()
        _cashierID = cashierID
        _cashierName = cashierName
        _branchID = branchID
        _tillPointID = tillPointID
        _branchPrefix = branchPrefix
        _cartItems = cartItems.Copy()
        _subtotal = subtotal
        _taxAmount = taxAmount
        _totalAmount = totalAmount
        _isOrderCollection = isOrderCollection
        _orderID = orderID
        _orderNumber = orderNumber
        _orderColour = orderColour
        _orderPicture = orderPicture
        _connectionString = ConfigurationManager.ConnectionStrings("OvenDelightsERPConnectionString").ConnectionString
        
        InitializeComponent()
        ShowPaymentMethodSelection()
    End Sub
    
    ' Constructor for order deposits (simplified - no cart items needed, records as ORDER not SALE)
    Public Sub New(depositAmount As Decimal, branchID As Integer, tillPointID As Integer, cashierID As Integer, cashierName As String)
        MyBase.New()
        _cashierID = cashierID
        _cashierName = cashierName
        _branchID = branchID
        _tillPointID = tillPointID
        _branchPrefix = ""
        _cartItems = Nothing
        _subtotal = depositAmount
        _taxAmount = 0
        _totalAmount = depositAmount
        _isOrderCollection = True ' Mark as order-related to use 'ORDER' SaleType
        _orderID = 0
        _orderNumber = "DEPOSIT" ' Flag to indicate this is deposit payment
        _connectionString = ConfigurationManager.ConnectionStrings("OvenDelightsERPConnectionString").ConnectionString
        
        InitializeComponent()
        ShowPaymentMethodSelection()
    End Sub
    
    Private Sub InitializeComponent()
        Me.Text = "Payment Tender"
        Me.BackColor = _ironDark
        Me.FormBorderStyle = FormBorderStyle.FixedDialog
        Me.MaximizeBox = False
        Me.MinimizeBox = False
        Me.TopMost = True
        Me.WindowState = FormWindowState.Normal
        Me.ShowInTaskbar = True
        Me.ControlBox = True
        
        ' Fixed size for consistent display on all screens
        Me.Size = New Size(600, 650)
        Me.StartPosition = FormStartPosition.CenterScreen
    End Sub
    
    Private Sub ShowPaymentMethodSelection()
        Me.Controls.Clear()
        
        ' Scale based on screen size
        Dim screenWidth = Screen.PrimaryScreen.WorkingArea.Width
        Dim screenHeight = Screen.PrimaryScreen.WorkingArea.Height
        Dim formWidth = Math.Min(1000, CInt(screenWidth * 0.8))
        Dim formHeight = Math.Min(600, CInt(screenHeight * 0.75))
        
        Me.WindowState = FormWindowState.Normal
        Me.Size = New Size(formWidth, formHeight)
        Me.StartPosition = FormStartPosition.CenterScreen
        
        ' Header with Iron Man theme - centered and compact
        Dim pnlHeader As New Panel With {
            .Dock = DockStyle.Top,
            .Height = 100,
            .BackColor = Color.Transparent,
            .Padding = New Padding(20, 10, 20, 10)
        }
        
        Dim lblTitle As New Label With {
            .Text = "💳 SELECT PAYMENT METHOD",
            .Font = New Font("Segoe UI", 24, FontStyle.Bold),
            .ForeColor = _ironGold,
            .AutoSize = False,
            .Width = formWidth - 40,
            .Height = 40,
            .TextAlign = ContentAlignment.MiddleCenter
        }
        lblTitle.Location = New Point(20, 10)
        
        Dim lblAmount As New Label With {
            .Text = $"Total: R {_totalAmount:N2}",
            .Font = New Font("Segoe UI", 24, FontStyle.Bold),
            .ForeColor = _ironRed,
            .AutoSize = False,
            .Width = formWidth - 40,
            .Height = 40,
            .TextAlign = ContentAlignment.MiddleCenter
        }
        lblAmount.Location = New Point(20, 55)
        pnlHeader.Controls.AddRange({lblTitle, lblAmount})
        
        ' Grid layout for tender buttons (5 horizontal rectangles in 1 row)
        Dim pnlButtons As New Panel With {
            .Location = New Point(40, 110),
            .Size = New Size(formWidth - 80, 280),
            .BackColor = Color.Transparent
        }
        
        Dim gap As Integer = 15
        Dim totalGaps = gap * 4 ' 4 gaps between 5 buttons
        Dim availableWidth = formWidth - 80 - totalGaps ' Subtract padding and gaps
        Dim buttonWidth = availableWidth \ 5
        Dim buttonHeight = 280 ' Reduced height to fit cancel button
        Dim buttonSize As New Size(buttonWidth, buttonHeight)
        
        ' Helper function to create professional tender button
        Dim CreateTenderButton = Function(text As String, icon As String, bgColor As Color, bgColorDark As Color, xPos As Integer, clickHandler As Action) As Button
            Dim btn As New Button With {
                .Size = buttonSize,
                .Location = New Point(xPos, 0),
                .BackColor = bgColor,
                .ForeColor = Color.White,
                .FlatStyle = FlatStyle.Flat,
                .Cursor = Cursors.Hand,
                .Font = New Font("Segoe UI", 24, FontStyle.Bold)
            }
            btn.FlatAppearance.BorderSize = 2
            btn.FlatAppearance.BorderColor = Color.White
            
            ' Add icon label - centered in upper portion
            Dim lblIcon As New Label With {
                .Text = icon,
                .Font = New Font("Segoe UI", 60),
                .ForeColor = Color.White,
                .BackColor = Color.Transparent,
                .AutoSize = True,
                .TextAlign = ContentAlignment.MiddleCenter
            }
            lblIcon.Location = New Point((buttonSize.Width - lblIcon.PreferredWidth) \ 2, 50)
            btn.Controls.Add(lblIcon)
            
            ' Add text label - centered below icon
            Dim lblText As New Label With {
                .Text = text,
                .Font = New Font("Segoe UI", 18, FontStyle.Bold),
                .ForeColor = Color.White,
                .BackColor = Color.Transparent,
                .AutoSize = True,
                .TextAlign = ContentAlignment.MiddleCenter
            }
            lblText.Location = New Point((buttonSize.Width - lblText.PreferredWidth) \ 2, 180)
            btn.Controls.Add(lblText)
            
            AddHandler btn.Click, Sub() clickHandler()
            AddHandler btn.MouseEnter, Sub()
                btn.BackColor = bgColorDark
                btn.FlatAppearance.BorderColor = _ironGold
                btn.FlatAppearance.BorderSize = 4
            End Sub
            AddHandler btn.MouseLeave, Sub()
                btn.BackColor = bgColor
                btn.FlatAppearance.BorderColor = Color.White
                btn.FlatAppearance.BorderSize = 2
            End Sub
            
            Return btn
        End Function
        
        ' Create 5 professional buttons in a row
        Dim btnCash = CreateTenderButton("CASH", "💵", _tenderCash, _tenderCashDark, 0, Sub() ProcessCashPayment())
        Dim btnCard = CreateTenderButton("CARD", "💳", _tenderCard, _tenderCardDark, buttonSize.Width + gap, Sub() ProcessCardPayment())
        Dim btnEFT = CreateTenderButton("EFT", "🏦", _tenderEFT, _tenderEFTDark, (buttonSize.Width + gap) * 2, Sub() ProcessEFTPayment())
        Dim btnManual = CreateTenderButton("MANUAL", "✍️", _tenderManual, _tenderManualDark, (buttonSize.Width + gap) * 3, Sub() ProcessManualPayment())
        Dim btnSplit = CreateTenderButton("SPLIT", "💵💳", _tenderSplit, _tenderSplitDark, (buttonSize.Width + gap) * 4, Sub() ProcessSplitPayment())
        
        pnlButtons.Controls.AddRange({btnCash, btnCard, btnEFT, btnManual, btnSplit})
        
        ' Cancel button at bottom - full width, more compact
        Dim pnlBottom As New Panel With {.Dock = DockStyle.Bottom, .Height = 70, .BackColor = Color.Transparent, .Padding = New Padding(40, 5, 40, 5)}
        Dim btnCancel As New Button With {
            .Text = "✖ CANCEL",
            .Dock = DockStyle.Fill,
            .BackColor = _ironRed,
            .ForeColor = Color.White,
            .Font = New Font("Segoe UI", 20, FontStyle.Bold),
            .FlatStyle = FlatStyle.Flat,
            .Cursor = Cursors.Hand
        }
        btnCancel.FlatAppearance.BorderSize = 2
        btnCancel.FlatAppearance.BorderColor = Color.White
        AddHandler btnCancel.Click, Sub() Me.DialogResult = DialogResult.Cancel
        AddHandler btnCancel.MouseEnter, Sub()
            btnCancel.BackColor = _ironRedDark
            btnCancel.FlatAppearance.BorderColor = _ironGold
            btnCancel.FlatAppearance.BorderSize = 5
        End Sub
        AddHandler btnCancel.MouseLeave, Sub()
            btnCancel.BackColor = _ironRed
            btnCancel.FlatAppearance.BorderColor = Color.White
            btnCancel.FlatAppearance.BorderSize = 2
        End Sub
        pnlBottom.Controls.Add(btnCancel)
        
        Me.Controls.AddRange({pnlHeader, pnlButtons, pnlBottom})
        Application.DoEvents()
        Me.Refresh()
        Me.Invalidate()
    End Sub
    
    Private Sub ProcessCashPayment()
        _paymentMethod = "CASH"
        ShowCashKeypad(_totalAmount, "CASH PAYMENT")
    End Sub
    
    Private Sub ProcessCardPayment()
        _paymentMethod = "CARD"
        _cardAmount = _totalAmount
        ProcessCardTransaction(_totalAmount)
    End Sub
    
    Private Sub ProcessSplitPayment()
        _paymentMethod = "SPLIT"
        ShowCashKeypad(_totalAmount, "CASH PORTION - SPLIT PAYMENT")
    End Sub
    
    Private Sub ProcessEFTPayment()
        _paymentMethod = "EFT"
        _cardAmount = _totalAmount ' EFT counts as electronic payment
        ShowEFTSlip()
    End Sub
    
    Private Sub ProcessManualPayment()
        _paymentMethod = "MANUAL"
        _cashAmount = _totalAmount ' Manual payment treated as cash
        
        ' Show confirmation dialog
        Dim result = MessageBox.Show($"Confirm manual payment of R {_totalAmount:N2}?{vbCrLf}{vbCrLf}Record payment details manually.", "Manual Payment", MessageBoxButtons.YesNo, MessageBoxIcon.Question)
        
        If result = DialogResult.Yes Then
            ' Complete the transaction (same as other payment methods)
            CompleteTransactionAndShowReceipt()
        End If
    End Sub
    
    Private Sub ShowEFTSlip()
        Me.Controls.Clear()
        ' Fixed size for 1024x768 POS screen
        Me.Size = New Size(600, 700)
        Me.StartPosition = FormStartPosition.CenterScreen
        
        ' Header
        Dim pnlHeader As New Panel With {.Dock = DockStyle.Top, .Height = 60, .BackColor = ColorTranslator.FromHtml("#3498DB")}
        Dim lblHeader As New Label With {
            .Text = "🏦 EFT PAYMENT SLIP",
            .Font = New Font("Segoe UI", 20, FontStyle.Bold),
            .ForeColor = Color.White,
            .TextAlign = ContentAlignment.MiddleCenter,
            .Dock = DockStyle.Fill
        }
        pnlHeader.Controls.Add(lblHeader)
        
        ' Slip content - reduced size to fit screen with buttons visible
        Dim pnlSlip As New Panel With {
            .Location = New Point(50, 75),
            .Size = New Size(500, 450),
            .BackColor = Color.White,
            .BorderStyle = BorderStyle.FixedSingle,
            .AutoScroll = True
        }
        
        Dim yPos = 15
        
        ' Bank details
        Dim lblBankHeader As New Label With {
            .Text = "BANK DETAILS",
            .Font = New Font("Courier New", 12, FontStyle.Bold),
            .Location = New Point(160, yPos),
            .AutoSize = True
        }
        pnlSlip.Controls.Add(lblBankHeader)
        yPos += 30
        
        Dim lblSeparator1 As New Label With {
            .Text = "========================================",
            .Font = New Font("Courier New", 9),
            .Location = New Point(50, yPos),
            .AutoSize = True
        }
        pnlSlip.Controls.Add(lblSeparator1)
        yPos += 25
        
        ' Bank info
        Dim bankInfo() As String = {
            "Bank: ABSA Bank",
            "Account Name: Oven Delights (Pty) Ltd",
            "Account Number: 4012345678",
            "Branch Code: 632005",
            "Account Type: Business Cheque"
        }
        
        For Each info As String In bankInfo
            Dim lblInfo As New Label With {
                .Text = info,
                .Font = New Font("Courier New", 10),
                .Location = New Point(70, yPos),
                .AutoSize = True
            }
            pnlSlip.Controls.Add(lblInfo)
            yPos += 25
        Next
        
        yPos += 10
        Dim lblSeparator2 As New Label With {
            .Text = "========================================",
            .Font = New Font("Courier New", 9),
            .Location = New Point(50, yPos),
            .AutoSize = True
        }
        pnlSlip.Controls.Add(lblSeparator2)
        yPos += 30
        
        ' Payment details
        Dim lblPaymentHeader As New Label With {
            .Text = "PAYMENT DETAILS",
            .Font = New Font("Courier New", 12, FontStyle.Bold),
            .Location = New Point(150, yPos),
            .AutoSize = True
        }
        pnlSlip.Controls.Add(lblPaymentHeader)
        yPos += 35
        
        Dim lblAmount As New Label With {
            .Text = $"Amount Due: R{_totalAmount:N2}",
            .Font = New Font("Courier New", 12, FontStyle.Bold),
            .ForeColor = ColorTranslator.FromHtml("#E67E22"),
            .Location = New Point(110, yPos),
            .AutoSize = True
        }
        pnlSlip.Controls.Add(lblAmount)
        yPos += 35
        
        Dim lblReference As New Label With {
            .Text = $"Reference: INV-{DateTime.Now:yyyyMMddHHmmss}",
            .Font = New Font("Courier New", 10),
            .Location = New Point(70, yPos),
            .AutoSize = True
        }
        pnlSlip.Controls.Add(lblReference)
        yPos += 35
        
        Dim lblInstructions As New Label With {
            .Text = "Use reference number for payment",
            .Font = New Font("Courier New", 9),
            .ForeColor = ColorTranslator.FromHtml("#E74C3C"),
            .Location = New Point(70, yPos),
            .AutoSize = True
        }
        pnlSlip.Controls.Add(lblInstructions)
        
        ' Buttons
        Dim pnlButtons As New Panel With {.Dock = DockStyle.Bottom, .Height = 80, .BackColor = _lightGray}
        
        Dim btnPrint As New Button With {
            .Text = "🖨️ PRINT SLIP",
            .Size = New Size(160, 60),
            .Location = New Point(20, 10),
            .BackColor = ColorTranslator.FromHtml("#3498DB"),
            .ForeColor = Color.White,
            .Font = New Font("Segoe UI", 12, FontStyle.Bold),
            .FlatStyle = FlatStyle.Flat,
            .Cursor = Cursors.Hand
        }
        btnPrint.FlatAppearance.BorderSize = 0
        AddHandler btnPrint.Click, Sub() PrintEFTSlip()
        
        Dim btnConfirm As New Button With {
            .Text = "✓ CONFIRM PAYMENT",
            .Size = New Size(180, 60),
            .Location = New Point(190, 10),
            .BackColor = ColorTranslator.FromHtml("#27AE60"),
            .ForeColor = Color.White,
            .Font = New Font("Segoe UI", 12, FontStyle.Bold),
            .FlatStyle = FlatStyle.Flat,
            .Cursor = Cursors.Hand
        }
        btnConfirm.FlatAppearance.BorderSize = 0
        AddHandler btnConfirm.Click, Sub() CompleteTransactionAndShowReceipt()
        
        Dim btnBack As New Button With {
            .Text = "← BACK",
            .Size = New Size(160, 60),
            .Location = New Point(380, 10),
            .BackColor = ColorTranslator.FromHtml("#E74C3C"),
            .ForeColor = Color.White,
            .Font = New Font("Segoe UI", 12, FontStyle.Bold),
            .FlatStyle = FlatStyle.Flat,
            .Cursor = Cursors.Hand
        }
        btnBack.FlatAppearance.BorderSize = 0
        AddHandler btnBack.Click, Sub() ShowPaymentMethodSelection()
        
        pnlButtons.Controls.AddRange({btnPrint, btnConfirm, btnBack})
        
        Me.Controls.AddRange({pnlHeader, pnlSlip, pnlButtons})
        Application.DoEvents()
        Me.Refresh()
        Me.Invalidate()
    End Sub
    
    Private Sub ShowCashKeypad(amountDue As Decimal, title As String)
        Me.Controls.Clear()
        ' Fixed size to ensure all controls are visible
        Me.Size = New Size(550, 600)
        Me.StartPosition = FormStartPosition.CenterScreen
        
        ' Adjust header height for split payments
        Dim headerHeight = If(_paymentMethod = "SPLIT", 120, 100)
        Dim pnlHeader As New Panel With {.Dock = DockStyle.Top, .Height = headerHeight, .BackColor = _green}
        
        ' Add CASH DUE label for split payments (centered at top, no amount)
        If _paymentMethod = "SPLIT" Then
            Dim lblCashDue As New Label With {
                .Text = "CASH DUE",
                .Font = New Font("Segoe UI", 18, FontStyle.Bold),
                .ForeColor = Color.Yellow,
                .AutoSize = True,
                .TextAlign = ContentAlignment.MiddleCenter
            }
            ' Center the label
            lblCashDue.Location = New Point((550 - lblCashDue.Width) \ 2, 5)
            pnlHeader.Controls.Add(lblCashDue)
        End If
        
        Dim lblAmountDue As New Label With {
            .Text = $"AMOUNT DUE: R{amountDue:N2}",
            .Font = New Font("Segoe UI", 14, FontStyle.Bold),
            .ForeColor = Color.White,
            .AutoSize = False,
            .Size = New Size(500, 25),
            .Location = New Point(20, If(_paymentMethod = "SPLIT", 35, 10))
        }
        Dim lblTendered As New Label With {
            .Text = "TENDERED: R0.00",
            .Font = New Font("Segoe UI", 12),
            .ForeColor = Color.White,
            .AutoSize = False,
            .Size = New Size(500, 25),
            .Location = New Point(20, If(_paymentMethod = "SPLIT", 60, 35)),
            .Name = "lblTendered"
        }
        Dim lblChange As New Label With {
            .Text = "CHANGE: R0.00",
            .Font = New Font("Segoe UI", 16, FontStyle.Bold),
            .ForeColor = Color.Yellow,
            .AutoSize = False,
            .Size = New Size(500, 30),
            .Location = New Point(20, If(_paymentMethod = "SPLIT", 85, 60)),
            .Name = "lblChange"
        }
        
        pnlHeader.Controls.AddRange({lblAmountDue, lblTendered, lblChange})
        
        ' Make text box accept keyboard input - position below header
        Dim txtAmount As New TextBox With {.Font = New Font("Segoe UI", 36, FontStyle.Bold), .TextAlign = HorizontalAlignment.Right, .Location = New Point(25, headerHeight + 5), .Size = New Size(500, 60), .Text = "0.00", .ReadOnly = False, .BackColor = Color.White, .ForeColor = _darkBlue, .Name = "txtAmount"}
        
        ' Handle keyboard input and update change calculation
        AddHandler txtAmount.TextChanged, Sub()
            Dim tendered As Decimal = 0
            If Decimal.TryParse(txtAmount.Text, tendered) Then
                Dim change = Math.Max(0, tendered - amountDue)
                CType(pnlHeader.Controls("lblTendered"), Label).Text = $"TENDERED: R{tendered:N2}"
                CType(pnlHeader.Controls("lblChange"), Label).Text = $"CHANGE: R{change:N2}"
            End If
        End Sub
        
        ' Allow only numbers, decimal point, and backspace
        AddHandler txtAmount.KeyPress, Sub(sender, e)
            If Not Char.IsDigit(e.KeyChar) AndAlso e.KeyChar <> "."c AndAlso e.KeyChar <> ChrW(Keys.Back) Then
                e.Handled = True
            End If
            ' Only allow one decimal point
            If e.KeyChar = "."c AndAlso txtAmount.Text.Contains(".") Then
                e.Handled = True
            End If
        End Sub
        
        ' Clear "0.00" when user starts typing
        AddHandler txtAmount.Enter, Sub()
            If txtAmount.Text = "0.00" Then
                txtAmount.Text = ""
            End If
        End Sub
        
        txtAmount.Focus() ' Set focus so keyboard works immediately
        
        ' Calculate numpad position to avoid overlap with bottom buttons
        Dim numpadTop = headerHeight + 75 ' Below text box
        Dim numpadHeight = 280 ' Reduced height for numpad
        Dim pnlKeypad As New Panel With {.Location = New Point(75, numpadTop), .Size = New Size(400, numpadHeight)}
        Dim buttonSize As New Size(110, 60)
        Dim buttons(,) As String = {{"7", "8", "9"}, {"4", "5", "6"}, {"1", "2", "3"}, {".", "0", "⌫"}}
        
        Dim keySpacingX = 120
        Dim keySpacingY = 70
        
        For row = 0 To 3
            For col = 0 To 2
                Dim btnText = buttons(row, col)
                Dim btn As New Button With {.Text = btnText, .Size = buttonSize, .Location = New Point(col * keySpacingX, row * keySpacingY), .Font = New Font("Segoe UI", 20, FontStyle.Bold), .BackColor = Color.White, .ForeColor = _darkBlue, .FlatStyle = FlatStyle.Flat, .Cursor = Cursors.Hand}
                btn.FlatAppearance.BorderColor = _lightGray
                AddHandler btn.Click, Sub(s, e)
                    Dim clickedBtn = CType(s, Button)
                    If clickedBtn.Text = "⌫" Then
                        If txtAmount.Text.Length > 0 Then txtAmount.Text = txtAmount.Text.Substring(0, txtAmount.Text.Length - 1)
                        If txtAmount.Text = "" Then txtAmount.Text = "0.00"
                    ElseIf clickedBtn.Text = "." Then
                        If Not txtAmount.Text.Contains(".") Then txtAmount.Text &= "."
                    Else
                        If txtAmount.Text = "0.00" Then txtAmount.Text = ""
                        txtAmount.Text &= clickedBtn.Text
                    End If
                    
                    ' Update tendered and change labels
                    Dim tendered As Decimal = 0
                    Decimal.TryParse(txtAmount.Text, tendered)
                    Dim change = Math.Max(0, tendered - amountDue)
                    CType(pnlHeader.Controls("lblTendered"), Label).Text = $"TENDERED: R{tendered:N2}"
                    CType(pnlHeader.Controls("lblChange"), Label).Text = $"CHANGE: R{change:N2}"
                End Sub
                pnlKeypad.Controls.Add(btn)
            Next
        Next
        
        Dim pnlButtons As New Panel With {.Dock = DockStyle.Bottom, .Height = 70, .BackColor = _lightGray}
        
        Dim btnConfirm As New Button With {.Text = "✓ CONFIRM", .Size = New Size(230, 50), .Location = New Point(30, 10), .BackColor = _green, .ForeColor = Color.White, .Font = New Font("Segoe UI", 14, FontStyle.Bold), .FlatStyle = FlatStyle.Flat, .Cursor = Cursors.Hand}
        btnConfirm.FlatAppearance.BorderSize = 0
        AddHandler btnConfirm.Click, Sub()
            Dim amount As Decimal
            If Decimal.TryParse(txtAmount.Text, amount) Then
                If _paymentMethod = "CASH" Then
                    _cashAmount = amount
                    If amount >= _totalAmount Then
                        ' Cash payment complete - write to database and show receipt
                        CompleteTransactionAndShowReceipt()
                    Else
                        MessageBox.Show("Insufficient amount!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                    End If
                ElseIf _paymentMethod = "SPLIT" Then
                    _cashAmount = amount
                    _cardAmount = _totalAmount - amount
                    If _cardAmount > 0 Then
                        ' Process card for remaining balance - DON'T write to database yet
                        ProcessCardTransaction(_cardAmount)
                    Else
                        ' All cash, no card needed - write to database
                        CompleteTransactionAndShowReceipt()
                    End If
                End If
            End If
        End Sub
        
        Dim btnBack As New Button With {.Text = "← BACK", .Size = New Size(230, 50), .Location = New Point(270, 10), .BackColor = ColorTranslator.FromHtml("#E74C3C"), .ForeColor = Color.White, .Font = New Font("Segoe UI", 14, FontStyle.Bold), .FlatStyle = FlatStyle.Flat, .Cursor = Cursors.Hand}
        btnBack.FlatAppearance.BorderSize = 0
        AddHandler btnBack.Click, Sub() ShowPaymentMethodSelection()
        
        pnlButtons.Controls.AddRange({btnConfirm, btnBack})
        
        Me.Controls.AddRange({pnlHeader, txtAmount, pnlKeypad, pnlButtons})
        Application.DoEvents()
        Me.Refresh()
        Me.Invalidate()
    End Sub
    
    Private Sub ProcessCardTransaction(amount As Decimal)
        Me.Controls.Clear()
        Dim screenHeight = Screen.PrimaryScreen.WorkingArea.Height
        Dim formHeight = Math.Min(550, CInt(screenHeight * 0.7))
        Me.Size = New Size(600, formHeight)
        Me.StartPosition = FormStartPosition.CenterScreen
        
        Dim pnlMain As New Panel With {.Dock = DockStyle.Fill, .BackColor = _darkBlue}
        
        Dim lblIcon As New Label With {.Text = "💳", .Font = New Font("Segoe UI", 100), .ForeColor = Color.White, .AutoSize = True, .Location = New Point(280, 30)}
        
        ' Show appropriate label based on payment type
        Dim amountLabel = If(_paymentMethod = "SPLIT", "Balance Outstanding:", "Amount Due:")
        Dim lblAmountLabel As New Label With {
            .Text = amountLabel,
            .Font = New Font("Segoe UI", 18, FontStyle.Bold),
            .ForeColor = Color.White,
            .AutoSize = True,
            .Location = New Point(230, 150)
        }
        
        Dim lblAmount As New Label With {.Text = amount.ToString("C2"), .Font = New Font("Segoe UI", 48, FontStyle.Bold), .ForeColor = ColorTranslator.FromHtml("#F39C12"), .AutoSize = True, .Location = New Point(240, 190)}
        
        Dim lblInstruction As New Label With {
            .Text = "INSERT OR TAP CARD",
            .Font = New Font("Segoe UI", 28, FontStyle.Bold),
            .ForeColor = Color.White,
            .AutoSize = True,
            .Location = New Point(150, 280)
        }
        
        Dim lblWaiting As New Label With {
            .Text = "Waiting for customer...",
            .Font = New Font("Segoe UI", 16),
            .ForeColor = ColorTranslator.FromHtml("#BDC3C7"),
            .AutoSize = True,
            .Location = New Point(220, 340)
        }
        
        pnlMain.Controls.AddRange({lblIcon, lblAmountLabel, lblAmount, lblInstruction, lblWaiting})
        
        ' Show cash tendered for split payment
        If _paymentMethod = "SPLIT" Then
            Dim lblCashTendered As New Label With {
                .Text = $"Cash Tendered: {_cashAmount.ToString("C2")}",
                .Font = New Font("Segoe UI", 16, FontStyle.Bold),
                .ForeColor = _green,
                .AutoSize = True,
                .Location = New Point(220, 390)
            }
            pnlMain.Controls.Add(lblCashTendered)
            
            Dim lblWarning As New Label With {
                .Text = "⚠ If card fails, return cash to customer",
                .Font = New Font("Segoe UI", 12),
                .ForeColor = ColorTranslator.FromHtml("#E67E22"),
                .AutoSize = True,
                .Location = New Point(180, 430)
            }
            pnlMain.Controls.Add(lblWarning)
        End If
        
        Me.Controls.Add(pnlMain)
        Application.DoEvents()
        Me.Refresh()
        Me.Invalidate()
        
        ' Simulate PayPoint - show processing then success
        Dim timer As New Timer With {.Interval = 3000}
        AddHandler timer.Tick, Sub()
            timer.Stop()
            ' TODO: Replace with actual PayPoint integration
            ShowCardProcessing(amount)
        End Sub
        timer.Start()
    End Sub
    
    Private Sub ShowCardProcessing(amount As Decimal)
        Me.Controls.Clear()
        Dim screenHeight = Screen.PrimaryScreen.WorkingArea.Height
        Dim formHeight = Math.Min(500, CInt(screenHeight * 0.6))
        Me.Size = New Size(600, formHeight)
        Me.StartPosition = FormStartPosition.CenterScreen
        
        Dim pnlMain As New Panel With {.Dock = DockStyle.Fill, .BackColor = ColorTranslator.FromHtml("#3498DB")} ' Blue
        
        ' Processing icon - centered
        Dim formWidth = Me.Width
        Dim lblIcon As New Label With {
            .Text = "⏳",
            .Font = New Font("Segoe UI", 80),
            .ForeColor = Color.White,
            .TextAlign = ContentAlignment.MiddleCenter,
            .Size = New Size(formWidth, 100),
            .Location = New Point(0, 40)
        }
        
        Dim lblProcessing As New Label With {
            .Text = "PROCESSING AUTHORIZATION",
            .Font = New Font("Segoe UI", 22, FontStyle.Bold),
            .ForeColor = Color.White,
            .TextAlign = ContentAlignment.MiddleCenter,
            .Size = New Size(formWidth, 40),
            .Location = New Point(0, 160)
        }
        
        Dim lblAmount As New Label With {
            .Text = amount.ToString("C2"),
            .Font = New Font("Segoe UI", 36, FontStyle.Bold),
            .ForeColor = ColorTranslator.FromHtml("#F39C12"),
            .TextAlign = ContentAlignment.MiddleCenter,
            .Size = New Size(formWidth, 50),
            .Location = New Point(0, 220)
        }
        
        Dim lblWait As New Label With {
            .Text = "Please wait...",
            .Font = New Font("Segoe UI", 16),
            .ForeColor = ColorTranslator.FromHtml("#ECF0F1"),
            .TextAlign = ContentAlignment.MiddleCenter,
            .Size = New Size(formWidth, 30),
            .Location = New Point(0, 290)
        }
        
        pnlMain.Controls.AddRange({lblIcon, lblProcessing, lblAmount, lblWait})
        Me.Controls.Add(pnlMain)
        Application.DoEvents()
        Me.Refresh()
        Me.Invalidate()
        
        ' Show success after processing
        Dim timer As New Timer With {.Interval = 4000}
        AddHandler timer.Tick, Sub()
            timer.Stop()
            ShowCardSuccess(amount)
        End Sub
        timer.Start()
    End Sub
    
    Private Sub ShowCardSuccess(amount As Decimal)
        Me.Controls.Clear()
        Dim screenHeight = Screen.PrimaryScreen.WorkingArea.Height
        Dim formHeight = Math.Min(600, CInt(screenHeight * 0.75))
        Me.Size = New Size(600, formHeight)
        Me.StartPosition = FormStartPosition.CenterScreen
        
        ' Gradient background colors
        Dim colorTop = ColorTranslator.FromHtml("#27AE60") ' Green
        Dim colorBottom = ColorTranslator.FromHtml("#229954") ' Darker green
        
        Dim pnlMain As New Panel With {.Dock = DockStyle.Fill, .BackColor = colorTop}
        
        ' Success icon - centered
        Dim formWidth = Me.Width
        Dim lblIcon As New Label With {
            .Text = "✓",
            .Font = New Font("Segoe UI", 80, FontStyle.Bold),
            .ForeColor = Color.White,
            .TextAlign = ContentAlignment.MiddleCenter,
            .Size = New Size(formWidth, 100),
            .Location = New Point(0, 40)
        }
        
        ' Success message - centered
        Dim lblSuccess As New Label With {
            .Text = "PAYMENT APPROVED",
            .Font = New Font("Segoe UI", 26, FontStyle.Bold),
            .ForeColor = Color.White,
            .TextAlign = ContentAlignment.MiddleCenter,
            .Size = New Size(700, 40),
            .Location = New Point(0, 150)
        }
        
        ' Amount - centered
        Dim lblAmount As New Label With {
            .Text = amount.ToString("C2"),
            .Font = New Font("Segoe UI", 42, FontStyle.Bold),
            .ForeColor = ColorTranslator.FromHtml("#F1C40F"),
            .TextAlign = ContentAlignment.MiddleCenter,
            .Size = New Size(700, 60),
            .Location = New Point(0, 210)
        }
        
        ' Payment method info panel
        Dim pnlInfo As New Panel With {
            .BackColor = ColorTranslator.FromHtml("#1E8449"),
            .Location = New Point(100, 300),
            .Size = New Size(500, 180)
        }
        
        Dim yPos = 20
        
        ' Payment method
        Dim paymentMethodText = If(_paymentMethod = "SPLIT", "SPLIT PAYMENT", _paymentMethod & " PAYMENT")
        Dim lblPaymentMethod As New Label With {
            .Text = $"Payment Method: {paymentMethodText}",
            .Font = New Font("Segoe UI", 14, FontStyle.Bold),
            .ForeColor = Color.White,
            .AutoSize = True,
            .Location = New Point(20, yPos)
        }
        pnlInfo.Controls.Add(lblPaymentMethod)
        yPos += 35
        
        ' Show breakdown for split
        If _paymentMethod = "SPLIT" Then
            Dim lblCash As New Label With {
                .Text = $"Cash: {_cashAmount.ToString("C2")}",
                .Font = New Font("Segoe UI", 13),
                .ForeColor = ColorTranslator.FromHtml("#F1C40F"),
                .AutoSize = True,
                .Location = New Point(40, yPos)
            }
            pnlInfo.Controls.Add(lblCash)
            yPos += 30
            
            Dim lblCard As New Label With {
                .Text = $"Card: {_cardAmount.ToString("C2")}",
                .Font = New Font("Segoe UI", 13),
                .ForeColor = ColorTranslator.FromHtml("#F1C40F"),
                .AutoSize = True,
                .Location = New Point(40, yPos)
            }
            pnlInfo.Controls.Add(lblCard)
            yPos += 35
        End If
        
        ' Total
        Dim lblTotal As New Label With {
            .Text = $"Total: {_totalAmount.ToString("C2")}",
            .Font = New Font("Segoe UI", 14, FontStyle.Bold),
            .ForeColor = Color.White,
            .AutoSize = True,
            .Location = New Point(20, yPos)
        }
        pnlInfo.Controls.Add(lblTotal)
        
        pnlMain.Controls.AddRange({lblIcon, lblSuccess, lblAmount, pnlInfo})
        
        ' Button panel at bottom
        Dim pnlButtons As New Panel With {.Dock = DockStyle.Bottom, .Height = 100, .BackColor = colorBottom}
        
        Dim btnContinue As New Button With {
            .Text = "✓ COMPLETE & SHOW RECEIPT",
            .Size = New Size(500, 70),
            .Location = New Point(100, 15),
            .BackColor = Color.White,
            .ForeColor = colorTop,
            .Font = New Font("Segoe UI", 18, FontStyle.Bold),
            .FlatStyle = FlatStyle.Flat,
            .Cursor = Cursors.Hand
        }
        btnContinue.FlatAppearance.BorderSize = 0
        AddHandler btnContinue.Click, Sub()
            ' Card approved - NOW complete the transaction and write to database
            CompleteTransactionAndShowReceipt()
        End Sub
        
        pnlButtons.Controls.Add(btnContinue)
        
        Me.Controls.AddRange({pnlMain, pnlButtons})
        Application.DoEvents()
        Me.Refresh()
        Me.Invalidate()
    End Sub
    
    Private Sub CompleteTransactionAndShowReceipt()
        ' Calculate change for cash payments
        Dim changeAmount As Decimal = 0
        If _paymentMethod = "CASH" Then
            changeAmount = _cashAmount - _totalAmount
        ElseIf _paymentMethod = "SPLIT" Then
            changeAmount = _cashAmount - (_totalAmount - _cardAmount)
        End If
        
        ' Store change amount in private field for external access
        _changeAmount = changeAmount
        
        ' For cake deposits (no cart items), just close with success
        If _cartItems Is Nothing Then
            Me.DialogResult = DialogResult.OK
            Me.Close()
            Return
        End If
        
        ' NOW complete the transaction and write to database
        Dim invoiceNumber As String = ""
        Dim saleDateTime As DateTime = DateTime.Now
        
        Try
            Using conn As New SqlConnection(_connectionString)
                conn.Open()
                Using transaction = conn.BeginTransaction()
                    Try
                        invoiceNumber = GenerateInvoiceNumber(conn, transaction)
                        Dim salesID = InsertSale(conn, transaction, invoiceNumber)
                        InsertInvoiceLineItems(conn, transaction, salesID, invoiceNumber)
                        UpdateStock(conn, transaction)
                        
                        ' For EFT payments, record as Pending (do NOT post to journals/ledgers yet)
                        If _paymentMethod = "EFT" Then
                            RecordEFTPayment(conn, transaction, salesID, invoiceNumber)
                        Else
                            ' For other payment methods, post to journals/ledgers immediately
                            PostToJournalsAndLedgers(conn, transaction, salesID, invoiceNumber)
                        End If
                        
                        ' If this is an order collection, update order status to Delivered
                        If _isOrderCollection AndAlso _orderID > 0 Then
                            Dim updateOrderSql = "UPDATE POS_CustomOrders SET OrderStatus = 'Delivered', CollectionDate = GETDATE() WHERE OrderID = @OrderID"
                            Using cmdUpdate As New SqlCommand(updateOrderSql, conn, transaction)
                                cmdUpdate.Parameters.AddWithValue("@OrderID", _orderID)
                                cmdUpdate.ExecuteNonQuery()
                            End Using
                        End If
                        
                        transaction.Commit()
                    Catch ex As Exception
                        transaction.Rollback()
                        MessageBox.Show($"Transaction Error: {ex.Message}{vbCrLf}{vbCrLf}Stack: {ex.StackTrace}", "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
                        Me.DialogResult = DialogResult.Cancel
                        Me.Close()
                        Return
                    End Try
                End Using
            End Using
        Catch ex As Exception
            MessageBox.Show($"Connection Error: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
            Me.DialogResult = DialogResult.Cancel
            Me.Close()
            Return
        End Try
        
        ' PRINT TO BOTH THERMAL AND CONTINUOUS PRINTERS BEFORE SHOWING RECEIPT
        Try
            PrintReceiptDual(invoiceNumber, saleDateTime, changeAmount)
        Catch ex As Exception
            ' Don't block the sale if printing fails
            MessageBox.Show($"Print error: {ex.Message}{vbCrLf}Receipt will be displayed on screen.", "Print Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning)
        End Try
        
        ' Now show the receipt with actual invoice number
        ' Large centered window for better readability
        Me.Controls.Clear()
        Me.WindowState = FormWindowState.Normal
        Dim screenWidth = Screen.PrimaryScreen.WorkingArea.Width
        Dim screenHeight = Screen.PrimaryScreen.WorkingArea.Height
        Dim formWidth = Math.Min(700, CInt(screenWidth * 0.7))
        Dim formHeight = Math.Min(650, CInt(screenHeight * 0.85))
        Me.Size = New Size(formWidth, formHeight)
        Me.StartPosition = FormStartPosition.CenterScreen
        Me.FormBorderStyle = FormBorderStyle.FixedDialog
        
        ' Header
        Dim pnlHeader As New Panel With {.Dock = DockStyle.Top, .Height = 70, .BackColor = _green}
        Dim lblHeaderText As New Label With {
            .Text = If(changeAmount > 0, $"CHANGE DUE: {changeAmount.ToString("C2")}", "RECEIPT"),
            .Font = New Font("Segoe UI", 20, FontStyle.Bold),
            .ForeColor = Color.White,
            .AutoSize = True,
            .Location = New Point(120, 20)
        }
        pnlHeader.Controls.Add(lblHeaderText)
        
        ' Receipt panel - responsive sizing
        Dim receiptWidth = Math.Min(800, formWidth - 100)
        Dim receiptHeight = Math.Min(600, formHeight - 200)
        Dim pnlReceipt As New Panel With {
            .Location = New Point((formWidth - receiptWidth) / 2, 85),
            .Size = New Size(receiptWidth, receiptHeight),
            .BackColor = Color.White,
            .BorderStyle = BorderStyle.FixedSingle,
            .AutoScroll = True
        }
        
        Dim yPos = 10
        
        ' Store header
        Dim lblStoreName As New Label With {.Text = "OVEN DELIGHTS", .Font = New Font("Courier New", 14, FontStyle.Bold), .AutoSize = True, .Location = New Point(120, yPos)}
        pnlReceipt.Controls.Add(lblStoreName)
        yPos += 30
        
        ' Branch info
        Dim branchName = GetBranchName()
        Dim lblBranch As New Label With {.Text = branchName, .Font = New Font("Courier New", 10), .AutoSize = True, .Location = New Point(10, yPos)}
        pnlReceipt.Controls.Add(lblBranch)
        yPos += 25
        
        ' Date and time
        Dim lblDateTime As New Label With {.Text = saleDateTime.ToString("dd/MM/yyyy HH:mm:ss"), .Font = New Font("Courier New", 10), .AutoSize = True, .Location = New Point(10, yPos)}
        pnlReceipt.Controls.Add(lblDateTime)
        yPos += 25
        
        ' Invoice number
        Dim lblInvoice As New Label With {.Text = $"Invoice: {invoiceNumber}", .Font = New Font("Courier New", 10), .AutoSize = True, .Location = New Point(10, yPos)}
        pnlReceipt.Controls.Add(lblInvoice)
        yPos += 25
        
        ' Till Point
        Dim tillNumber = GetTillNumber()
        Dim lblTill As New Label With {.Text = $"Till: {tillNumber}", .Font = New Font("Courier New", 10), .AutoSize = True, .Location = New Point(10, yPos)}
        pnlReceipt.Controls.Add(lblTill)
        yPos += 25
        
        ' Cashier
        Dim cashierName = GetCashierName()
        Dim lblCashier As New Label With {.Text = $"Cashier: {cashierName}", .Font = New Font("Courier New", 10), .AutoSize = True, .Location = New Point(10, yPos)}
        pnlReceipt.Controls.Add(lblCashier)
        yPos += 30
        
        ' Separator
        Dim lblSep1 As New Label With {.Text = "========================================", .Font = New Font("Courier New", 8), .AutoSize = True, .Location = New Point(10, yPos)}
        pnlReceipt.Controls.Add(lblSep1)
        yPos += 20
        
        ' Column headers
        Dim lblHeaders As New Label With {.Text = "Item                 Qty   Price   Total", .Font = New Font("Courier New", 9, FontStyle.Bold), .AutoSize = True, .Location = New Point(10, yPos)}
        pnlReceipt.Controls.Add(lblHeaders)
        yPos += 20
        
        ' Line items
        For Each row As DataRow In _cartItems.Rows
            Dim productName = row("Product").ToString()
            If productName.Length > 20 Then productName = productName.Substring(0, 17) & "..."
            
            Dim qty = CDec(row("Qty")).ToString("0.##")
            Dim price = CDec(row("Price")).ToString("C2")
            Dim total = CDec(row("Total")).ToString("C2")
            
            Dim lblItem As New Label With {
                .Text = productName.PadRight(20) & qty.PadLeft(4) & price.PadLeft(8) & total.PadLeft(8),
                .Font = New Font("Courier New", 9),
                .AutoSize = True,
                .Location = New Point(10, yPos)
            }
            pnlReceipt.Controls.Add(lblItem)
            yPos += 20
        Next
        
        ' Separator
        Dim lblSep2 As New Label With {.Text = "========================================", .Font = New Font("Courier New", 8), .AutoSize = True, .Location = New Point(10, yPos)}
        pnlReceipt.Controls.Add(lblSep2)
        yPos += 20
        
        ' Totals - right aligned
        Dim lblSubtotal As New Label With {
            .Text = "Subtotal:",
            .Font = New Font("Courier New", 10),
            .AutoSize = True,
            .Location = New Point(10, yPos)
        }
        Dim lblSubtotalAmt As New Label With {
            .Text = _subtotal.ToString("C2"),
            .Font = New Font("Courier New", 10),
            .AutoSize = True,
            .Location = New Point(300, yPos)
        }
        pnlReceipt.Controls.AddRange({lblSubtotal, lblSubtotalAmt})
        yPos += 20
        
        Dim lblTax As New Label With {
            .Text = "VAT (15%):",
            .Font = New Font("Courier New", 10),
            .AutoSize = True,
            .Location = New Point(10, yPos)
        }
        Dim lblTaxAmt As New Label With {
            .Text = _taxAmount.ToString("C2"),
            .Font = New Font("Courier New", 10),
            .AutoSize = True,
            .Location = New Point(300, yPos)
        }
        pnlReceipt.Controls.AddRange({lblTax, lblTaxAmt})
        yPos += 20
        
        Dim lblTotal As New Label With {
            .Text = "TOTAL:",
            .Font = New Font("Courier New", 11, FontStyle.Bold),
            .AutoSize = True,
            .Location = New Point(10, yPos)
        }
        Dim lblTotalAmt As New Label With {
            .Text = _totalAmount.ToString("C2"),
            .Font = New Font("Courier New", 11, FontStyle.Bold),
            .AutoSize = True,
            .Location = New Point(300, yPos)
        }
        pnlReceipt.Controls.AddRange({lblTotal, lblTotalAmt})
        yPos += 30
        
        ' Payment method
        Dim lblPayment As New Label With {.Text = $"Payment: {_paymentMethod}", .Font = New Font("Courier New", 10), .AutoSize = True, .Location = New Point(10, yPos)}
        pnlReceipt.Controls.Add(lblPayment)
        yPos += 20
        
        If _cashAmount > 0 Then
            Dim lblCash As New Label With {
                .Text = "Cash:",
                .Font = New Font("Courier New", 10),
                .AutoSize = True,
                .Location = New Point(10, yPos)
            }
            Dim lblCashAmt As New Label With {
                .Text = _cashAmount.ToString("C2"),
                .Font = New Font("Courier New", 10),
                .AutoSize = True,
                .Location = New Point(300, yPos)
            }
            pnlReceipt.Controls.AddRange({lblCash, lblCashAmt})
            yPos += 20
        End If
        
        If _cardAmount > 0 Then
            Dim lblCard As New Label With {
                .Text = "Card:",
                .Font = New Font("Courier New", 10),
                .AutoSize = True,
                .Location = New Point(10, yPos)
            }
            Dim lblCardAmt As New Label With {
                .Text = _cardAmount.ToString("C2"),
                .Font = New Font("Courier New", 10),
                .AutoSize = True,
                .Location = New Point(300, yPos)
            }
            pnlReceipt.Controls.AddRange({lblCard, lblCardAmt})
            yPos += 20
        End If
        
        yPos += 10
        
        If changeAmount > 0 Then
            Dim lblChange As New Label With {
                .Text = "CHANGE:",
                .Font = New Font("Courier New", 11, FontStyle.Bold),
                .ForeColor = _green,
                .AutoSize = True,
                .Location = New Point(10, yPos)
            }
            Dim lblChangeAmt As New Label With {
                .Text = changeAmount.ToString("C2"),
                .Font = New Font("Courier New", 11, FontStyle.Bold),
                .ForeColor = _green,
                .AutoSize = True,
                .Location = New Point(300, yPos)
            }
            pnlReceipt.Controls.AddRange({lblChange, lblChangeAmt})
            yPos += 30
        End If
        
        ' Thank you
        Dim lblThankYou As New Label With {.Text = "Thank you for your purchase!", .Font = New Font("Courier New", 10, FontStyle.Bold), .AutoSize = True, .Location = New Point(70, yPos)}
        pnlReceipt.Controls.Add(lblThankYou)
        
        ' Buttons at bottom
        Dim pnlButtons As New Panel With {.Dock = DockStyle.Bottom, .Height = 90, .BackColor = _lightGray}
        
        Dim btnPrint As New Button With {.Text = "🖨 PRINT", .Size = New Size(220, 60), .Location = New Point(25, 15), .BackColor = _ironBlue, .ForeColor = Color.White, .Font = New Font("Segoe UI", 18, FontStyle.Bold), .FlatStyle = FlatStyle.Flat, .Cursor = Cursors.Hand}
        btnPrint.FlatAppearance.BorderSize = 0
        AddHandler btnPrint.Click, Sub()
            ' Print receipt to continuous printer
            Try
                Dim printer As New POSReceiptPrinter()
                Dim success = printer.PrintSaleReceipt(_branchID, invoiceNumber, _cartItems, _totalAmount, _paymentMethod, _cashierName)
                If success Then
                    MessageBox.Show("Receipt printed successfully!", "Print", MessageBoxButtons.OK, MessageBoxIcon.Information)
                End If
            Catch ex As Exception
                MessageBox.Show($"Print error: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
            End Try
        End Sub
        
        Dim btnComplete As New Button With {.Text = "✓ COMPLETE", .Size = New Size(220, 60), .Location = New Point(255, 15), .BackColor = _green, .ForeColor = Color.White, .Font = New Font("Segoe UI", 18, FontStyle.Bold), .FlatStyle = FlatStyle.Flat, .Cursor = Cursors.Hand}
        btnComplete.FlatAppearance.BorderSize = 0
        AddHandler btnComplete.Click, Sub()
            Me.DialogResult = DialogResult.OK
            Me.Close()
        End Sub
        
        pnlButtons.Controls.AddRange({btnPrint, btnComplete})
        
        Me.Controls.AddRange({pnlHeader, pnlReceipt, pnlButtons})
    End Sub
    
    Private Function GetTillNumber() As String
        Try
            Using conn As New SqlConnection(_connectionString)
                conn.Open()
                Dim sql = "SELECT TillNumber FROM TillPoints WHERE TillPointID = @TillPointID"
                Using cmd As New SqlCommand(sql, conn)
                    cmd.Parameters.AddWithValue("@TillPointID", _tillPointID)
                    Dim result = cmd.ExecuteScalar()
                    Return If(result IsNot Nothing, result.ToString(), "N/A")
                End Using
            End Using
        Catch
            Return "N/A"
        End Try
    End Function
    
    Private Function GetCashierName() As String
        Try
            Dim sql = "SELECT COALESCE(NULLIF(TRIM(FirstName + ' ' + LastName), ''), Username, 'Cashier') FROM Users WHERE UserID = @UserID"
            Using conn As New SqlConnection(_connectionString)
                conn.Open()
                Using cmd As New SqlCommand(sql, conn)
                    cmd.Parameters.AddWithValue("@UserID", _cashierID)
                    Dim result = cmd.ExecuteScalar()
                    If result IsNot Nothing Then
                        Dim name = result.ToString().Trim()
                        ' Remove duplicate words (e.g., "Mogalia Mogalia" becomes "Mogalia")
                        Dim words = name.Split(" "c).Where(Function(w) Not String.IsNullOrWhiteSpace(w)).ToArray()
                        If words.Length >= 2 Then
                            ' Check if any consecutive words are duplicates
                            Dim uniqueWords As New List(Of String)
                            uniqueWords.Add(words(0))
                            For i = 1 To words.Length - 1
                                If words(i) <> words(i - 1) Then
                                    uniqueWords.Add(words(i))
                                End If
                            Next
                            Return String.Join(" ", uniqueWords)
                        End If
                        Return name
                    End If
                    Return $"Cashier #{_cashierID}"
                End Using
            End Using
        Catch
            Return $"Cashier #{_cashierID}"
        End Try
    End Function
    
    Private Function GetBranchName() As String
        Try
            Dim sql = "SELECT BranchName FROM Branches WHERE BranchID = @BranchID"
            Using conn As New SqlConnection(_connectionString)
                conn.Open()
                Using cmd As New SqlCommand(sql, conn)
                    cmd.Parameters.AddWithValue("@BranchID", _branchID)
                    Dim result = cmd.ExecuteScalar()
                    Return If(result IsNot Nothing, result.ToString(), "Branch")
                End Using
            End Using
        Catch
            Return "Branch"
        End Try
    End Function
    
    Private Function GenerateInvoiceNumber(conn As SqlConnection, transaction As SqlTransaction) As String
        ' Generate numeric-only invoice number: BranchID + 5-digit sequence (6 digits total)
        ' Example: Branch 6, invoice 1 -> "600001"
        ' Numeric only for better barcode scanning with Free 3 of 9 font
        
        Dim sql As String = "
            SELECT ISNULL(MAX(CAST(RIGHT(InvoiceNumber, 5) AS INT)), 0) + 1 
            FROM Demo_Sales WITH (TABLOCKX)
            WHERE InvoiceNumber LIKE @pattern AND LEN(InvoiceNumber) = 6"
        
        Dim pattern As String = $"{_branchID}%"
        
        Using cmd As New SqlCommand(sql, conn, transaction)
            cmd.Parameters.AddWithValue("@pattern", pattern)
            Dim nextNumber As Integer = Convert.ToInt32(cmd.ExecuteScalar())
            Return $"{_branchID}{nextNumber.ToString().PadLeft(5, "0"c)}"
        End Using
    End Function
    
    Private Function InsertSale(conn As SqlConnection, transaction As SqlTransaction, invoiceNumber As String) As Integer
        ' Determine sale type
        Dim saleType As String = "SALE"
        Dim referenceNumber As String = invoiceNumber
        
        If _isOrderCollection Then
            ' Check if this is a deposit (orderNumber = "DEPOSIT") or order collection
            If _orderNumber = "DEPOSIT" Then
                saleType = "ORDER" ' Deposit payment - not a sale
                ' Record deposit in DailySales for cashup tracking
                InsertDailySale(conn, transaction, invoiceNumber, saleType)
                Return 0 ' No sale ID needed for deposits (skip Demo_Sales)
            Else
                saleType = "OrderCollection" ' Order pickup/collection
            End If
            referenceNumber = _orderNumber
        End If
        
        Dim insertSql = "INSERT INTO Demo_Sales (SaleNumber, InvoiceNumber, SaleDate, CashierID, BranchID, TillPointID, Subtotal, TaxAmount, TotalAmount, PaymentMethod, CashAmount, CardAmount, SaleType, ReferenceNumber) 
                                VALUES (@SaleNumber, @InvoiceNumber, @SaleDate, @CashierID, @BranchID, @TillPointID, @Subtotal, @TaxAmount, @TotalAmount, @PaymentMethod, @CashAmount, @CardAmount, @SaleType, @ReferenceNumber);
                                SELECT CAST(SCOPE_IDENTITY() AS INT)"
        Dim salesID As Integer
        Using cmd As New SqlCommand(insertSql, conn, transaction)
            cmd.Parameters.AddWithValue("@SaleNumber", invoiceNumber)
            cmd.Parameters.AddWithValue("@InvoiceNumber", invoiceNumber)
            cmd.Parameters.AddWithValue("@SaleDate", DateTime.Now)
            cmd.Parameters.AddWithValue("@CashierID", _cashierID)
            cmd.Parameters.AddWithValue("@BranchID", _branchID)
            cmd.Parameters.AddWithValue("@TillPointID", _tillPointID)
            cmd.Parameters.AddWithValue("@Subtotal", _subtotal)
            cmd.Parameters.AddWithValue("@TaxAmount", _taxAmount)
            cmd.Parameters.AddWithValue("@TotalAmount", _totalAmount)
            cmd.Parameters.AddWithValue("@PaymentMethod", _paymentMethod)
            cmd.Parameters.AddWithValue("@CashAmount", _cashAmount)
            cmd.Parameters.AddWithValue("@CardAmount", _cardAmount)
            cmd.Parameters.AddWithValue("@SaleType", saleType)
            cmd.Parameters.AddWithValue("@ReferenceNumber", referenceNumber)
            salesID = CInt(cmd.ExecuteScalar())
        End Using
        
        ' Track in DailySales for reporting
        InsertDailySale(conn, transaction, invoiceNumber, saleType)
        
        Return salesID
    End Function
    
    Private Sub InsertDailySale(conn As SqlConnection, transaction As SqlTransaction, invoiceNumber As String, saleType As String)
        Try
            Dim tillNumber As String = GetTillNumber()
            ' Handle null cart items for deposits
            Dim itemCount As Integer = If(_cartItems IsNot Nothing, _cartItems.Rows.Count, 0)
            
            Dim sql = "INSERT INTO DailySales (SaleDate, BranchID, TillNumber, CashierID, CashierName, InvoiceNumber, SaleType, TotalAmount, PaymentMethod, ItemCount) 
                      VALUES (CAST(GETDATE() AS DATE), @BranchID, @TillNumber, @CashierID, @CashierName, @InvoiceNumber, @SaleType, @TotalAmount, @PaymentMethod, @ItemCount)"
            
            Using cmd As New SqlCommand(sql, conn, transaction)
                cmd.Parameters.AddWithValue("@BranchID", _branchID)
                cmd.Parameters.AddWithValue("@TillNumber", tillNumber)
                cmd.Parameters.AddWithValue("@CashierID", _cashierID)
                cmd.Parameters.AddWithValue("@CashierName", _cashierName)
                cmd.Parameters.AddWithValue("@InvoiceNumber", invoiceNumber)
                cmd.Parameters.AddWithValue("@SaleType", saleType)
                cmd.Parameters.AddWithValue("@TotalAmount", _totalAmount)
                cmd.Parameters.AddWithValue("@PaymentMethod", _paymentMethod)
                cmd.Parameters.AddWithValue("@ItemCount", itemCount)
                cmd.ExecuteNonQuery()
            End Using
        Catch ex As Exception
            ' Log error for debugging
            System.Diagnostics.Debug.WriteLine($"InsertDailySale error: {ex.Message}")
        End Try
    End Sub
    
    Private Sub InsertInvoiceLineItems(conn As SqlConnection, transaction As SqlTransaction, salesID As Integer, invoiceNumber As String)
        ' Write to POS_InvoiceLines
        Dim sqlPOS = "INSERT INTO POS_InvoiceLines (InvoiceNumber, SalesID, BranchID, ProductID, ItemCode, ProductName, Quantity, UnitPrice, LineTotal, SaleDate, CashierID, CreatedDate) VALUES (@InvoiceNumber, @SaleID, @BranchID, @ProductID, @ItemCode, @ProductName, @Quantity, @UnitPrice, @LineTotal, GETDATE(), @CashierID, GETDATE())"
        
        ' Also write to Invoices table (for ERP integration)
        Dim sqlERP = "INSERT INTO Invoices (InvoiceNumber, SalesID, BranchID, ProductID, ItemCode, ProductName, Quantity, UnitPrice, LineTotal, SaleDate, CashierID, CreatedDate) VALUES (@InvoiceNumber, @SaleID, @BranchID, @ProductID, @ItemCode, @ProductName, @Quantity, @UnitPrice, @LineTotal, GETDATE(), @CashierID, GETDATE())"
        
        For Each row As DataRow In _cartItems.Rows
            ' Insert into POS_InvoiceLines
            Using cmd As New SqlCommand(sqlPOS, conn, transaction)
                cmd.Parameters.AddWithValue("@InvoiceNumber", invoiceNumber)
                cmd.Parameters.AddWithValue("@SaleID", salesID)
                cmd.Parameters.AddWithValue("@BranchID", _branchID)
                cmd.Parameters.AddWithValue("@ProductID", row("ProductID"))
                cmd.Parameters.AddWithValue("@ItemCode", row("ItemCode"))
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
                cmd.Parameters.AddWithValue("@SaleID", salesID)
                cmd.Parameters.AddWithValue("@BranchID", _branchID)
                cmd.Parameters.AddWithValue("@ProductID", row("ProductID"))
                cmd.Parameters.AddWithValue("@ItemCode", row("ItemCode"))
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
    
    Private Sub CreateJournalEntries(conn As SqlConnection, transaction As SqlTransaction, invoiceNumber As String)
        Try
            ' Calculate total cost of items sold
            Dim totalCost = CalculateTotalCost(conn, transaction)
            
            ' Call the new GL posting stored procedure
            Using cmd As New SqlCommand("sp_POS_PostSaleToGL", conn, transaction)
                cmd.CommandType = CommandType.StoredProcedure
                cmd.Parameters.AddWithValue("@InvoiceNumber", invoiceNumber)
                cmd.Parameters.AddWithValue("@SaleDate", DateTime.Today)
                cmd.Parameters.AddWithValue("@BranchID", _branchID)
                cmd.Parameters.AddWithValue("@CashierID", _cashierID)
                cmd.Parameters.AddWithValue("@Subtotal", _subtotal)
                cmd.Parameters.AddWithValue("@TaxAmount", _taxAmount)
                cmd.Parameters.AddWithValue("@TotalAmount", _totalAmount)
                cmd.Parameters.AddWithValue("@CashAmount", _cashAmount)
                cmd.Parameters.AddWithValue("@CardAmount", _cardAmount)
                cmd.Parameters.AddWithValue("@TotalCost", totalCost)
                cmd.Parameters.AddWithValue("@CreatedBy", $"Cashier_{_cashierID}")
                
                cmd.ExecuteNonQuery()
            End Using
        Catch ex As Exception
            ' Log error but don't throw - sale is more important than GL posting
            System.Diagnostics.Debug.WriteLine($"GL Posting Error: {ex.Message}")
        End Try
    End Sub
    
    Private Function GetAccountID(conn As SqlConnection, transaction As SqlTransaction, accountName As String) As Integer
        Dim sql = "SELECT TOP 1 AccountID FROM ChartOfAccounts WHERE AccountName LIKE '%' + @Name + '%'"
        Using cmd As New SqlCommand(sql, conn, transaction)
            cmd.Parameters.AddWithValue("@Name", accountName)
            Dim result = cmd.ExecuteScalar()
            Return If(result IsNot Nothing, CInt(result), 0)
        End Using
    End Function
    
    Private Sub InsertJournalEntry(conn As SqlConnection, transaction As SqlTransaction, reference As String, accountID As Integer, accountName As String, debit As Decimal, credit As Decimal, description As String)
        Dim sql = "INSERT INTO GeneralJournal (TransactionDate, Reference, AccountID, AccountName, Debit, Credit, Description, BranchID, CreatedBy, CreatedDate) VALUES (GETDATE(), @Reference, @AccountID, @AccountName, @Debit, @Credit, @Description, @BranchID, @CreatedBy, GETDATE())"
        Using cmd As New SqlCommand(sql, conn, transaction)
            cmd.Parameters.AddWithValue("@Reference", reference)
            cmd.Parameters.AddWithValue("@AccountID", accountID)
            cmd.Parameters.AddWithValue("@AccountName", accountName)
            cmd.Parameters.AddWithValue("@Debit", debit)
            cmd.Parameters.AddWithValue("@Credit", credit)
            cmd.Parameters.AddWithValue("@Description", description)
            cmd.Parameters.AddWithValue("@BranchID", _branchID)
            cmd.Parameters.AddWithValue("@CreatedBy", _cashierID)
            cmd.ExecuteNonQuery()
        End Using
    End Sub
    
    Private Function CalculateTotalCost(conn As SqlConnection, transaction As SqlTransaction) As Decimal
        Dim totalCost As Decimal = 0
        Dim sql = "SELECT ISNULL(pr.CostPrice, 0) FROM Demo_Retail_Product p LEFT JOIN Demo_Retail_Stock s ON p.ProductID = s.StockID LEFT JOIN Demo_Retail_Price pr ON s.StockID = pr.ProductID WHERE p.ProductID = @ProductID"
        For Each row As DataRow In _cartItems.Rows
            Using cmd As New SqlCommand(sql, conn, transaction)
                cmd.Parameters.AddWithValue("@ProductID", row("ProductID"))
                Dim costPrice = CDec(cmd.ExecuteScalar())
                totalCost += costPrice * CDec(row("Qty"))
            End Using
        Next
        Return totalCost
    End Function
    
    Private Sub PostToJournalsAndLedgers(conn As SqlConnection, transaction As SqlTransaction, salesID As Integer, invoiceNumber As String)
        Dim journalDate = DateTime.Now
        
        ' 1. DEBIT: Cash/Bank
        Dim cashLedgerID = GetLedgerID(conn, transaction, "Cash")
        InsertJournalEntry(conn, transaction, journalDate, "Sales Journal", invoiceNumber, cashLedgerID, _totalAmount, 0, $"Sale {invoiceNumber}")
        
        ' 2. CREDIT: Sales Revenue
        Dim salesRevenueLedgerID = GetLedgerID(conn, transaction, "Sales Revenue")
        InsertJournalEntry(conn, transaction, journalDate, "Sales Journal", invoiceNumber, salesRevenueLedgerID, 0, _subtotal, $"Sale {invoiceNumber}")
        
        ' 3. CREDIT: VAT Output
        Dim vatOutputLedgerID = GetLedgerID(conn, transaction, "VAT Output")
        InsertJournalEntry(conn, transaction, journalDate, "Sales Journal", invoiceNumber, vatOutputLedgerID, 0, _taxAmount, $"VAT on Sale {invoiceNumber}")
        
        ' 4. DEBIT: Cost of Sales & CREDIT: Inventory
        Dim costOfSalesLedgerID = GetLedgerID(conn, transaction, "Cost of Sales")
        Dim inventoryLedgerID = GetLedgerID(conn, transaction, "Inventory")
        
        For Each row As DataRow In _cartItems.Rows
            Dim qty = CDec(row("Qty"))
            Dim productID = CInt(row("ProductID"))
            Dim avgCost = GetAverageCost(conn, transaction, productID, _branchID)
            Dim totalCost = qty * avgCost
            
            ' Get product name safely - check if column exists
            Dim productName As String = "Product"
            If _cartItems.Columns.Contains("ProductName") Then
                productName = row("ProductName").ToString()
            ElseIf _cartItems.Columns.Contains("Product") Then
                productName = row("Product").ToString()
            End If
            
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
                Dim insertSql = "INSERT INTO Ledgers (LedgerName, LedgerType, IsActive) VALUES (@LedgerName, 'Asset', 1); SELECT CAST(SCOPE_IDENTITY() AS INT)"
                Using cmdInsert As New SqlCommand(insertSql, conn, transaction)
                    cmdInsert.Parameters.AddWithValue("@LedgerName", ledgerName)
                    Return CInt(cmdInsert.ExecuteScalar())
                End Using
            End If
        End Using
    End Function
    
    Private Sub InsertJournalEntry(conn As SqlConnection, transaction As SqlTransaction, journalDate As DateTime, journalType As String, reference As String, ledgerID As Integer, debit As Decimal, credit As Decimal, description As String)
        Dim sql = "INSERT INTO GeneralJournal (TransactionDate, JournalType, Reference, LedgerID, Debit, Credit, Description, BranchID, CreatedBy, CreatedDate) " &
                  "VALUES (@Date, @Type, @Ref, @LedgerID, @Debit, @Credit, @Desc, @BranchID, @CreatedBy, GETDATE())"
        
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
    
    Private Sub RecordEFTPayment(conn As SqlConnection, transaction As SqlTransaction, salesID As Integer, invoiceNumber As String)
        Try
            ' Generate payment reference
            Dim paymentReference As String = $"INV-{DateTime.Now:yyyyMMddHHmmss}"
            
            ' Determine transaction type
            Dim transactionType As String = "Sale"
            If _isOrderCollection Then
                transactionType = "CakeOrder"
            End If
            
            ' Get branch name
            Dim branchName As String = ""
            Dim branchSql = "SELECT BranchName FROM Branches WHERE BranchID = @BranchID"
            Using cmdBranch As New SqlCommand(branchSql, conn, transaction)
                cmdBranch.Parameters.AddWithValue("@BranchID", _branchID)
                Dim result = cmdBranch.ExecuteScalar()
                If result IsNot Nothing Then branchName = result.ToString()
            End Using
            
            ' Record EFT payment as Pending
            Using cmd As New SqlCommand("sp_RecordEFTPayment", conn, transaction)
                cmd.CommandType = CommandType.StoredProcedure
                cmd.Parameters.AddWithValue("@PaymentReference", paymentReference)
                cmd.Parameters.AddWithValue("@TransactionType", transactionType)
                cmd.Parameters.AddWithValue("@TransactionID", salesID)
                cmd.Parameters.AddWithValue("@InvoiceNumber", invoiceNumber)
                cmd.Parameters.AddWithValue("@OrderNumber", If(_isOrderCollection, _orderNumber, DBNull.Value))
                cmd.Parameters.AddWithValue("@BranchID", _branchID)
                cmd.Parameters.AddWithValue("@BranchName", branchName)
                cmd.Parameters.AddWithValue("@TillPointID", _tillPointID)
                cmd.Parameters.AddWithValue("@CashierID", _cashierID)
                cmd.Parameters.AddWithValue("@CashierName", _cashierName)
                cmd.Parameters.AddWithValue("@Amount", _totalAmount)
                cmd.Parameters.AddWithValue("@CustomerName", DBNull.Value)
                cmd.Parameters.AddWithValue("@CustomerSurname", DBNull.Value)
                cmd.Parameters.AddWithValue("@CustomerCell", DBNull.Value)
                cmd.Parameters.AddWithValue("@Notes", "EFT payment - awaiting bank confirmation")
                cmd.ExecuteNonQuery()
            End Using
        Catch ex As Exception
            ' Log error but don't block the sale
            System.Diagnostics.Debug.WriteLine($"Error recording EFT payment: {ex.Message}")
        End Try
    End Sub
    
    Private Function GetAverageCost(conn As SqlConnection, transaction As SqlTransaction, productID As Integer, branchID As Integer) As Decimal
        Try
            ' Get cost price from Demo_Retail_Price table
            Dim sql = "SELECT TOP 1 ISNULL(CostPrice, 0) FROM Demo_Retail_Price " &
                      "WHERE ProductID = @ProductID AND (BranchID = @BranchID OR BranchID = 0) " &
                      "ORDER BY BranchID DESC"
            Using cmd As New SqlCommand(sql, conn, transaction)
                cmd.Parameters.AddWithValue("@ProductID", productID)
                cmd.Parameters.AddWithValue("@BranchID", branchID)
                Dim result = cmd.ExecuteScalar()
                If result IsNot Nothing AndAlso Not IsDBNull(result) Then
                    Return CDec(result)
                Else
                    Return 0D
                End If
            End Using
        Catch ex As Exception
            ' Log error and return 0 if cost cannot be determined
            System.Diagnostics.Debug.WriteLine($"Error getting average cost for ProductID {productID}: {ex.Message}")
            Return 0D
        End Try
    End Function
    
    ''' <summary>
    ''' Print EFT payment slip to thermal printer
    ''' </summary>
    Private Sub PrintEFTSlip()
        Try
            Dim printDoc As New Printing.PrintDocument()
            printDoc.DefaultPageSettings.PaperSize = New Printing.PaperSize("80mm", 302, 1200)
            
            AddHandler printDoc.PrintPage, Sub(sender, e)
                Dim fontBold As New Font("Courier New", 8, FontStyle.Bold)
                Dim fontLarge As New Font("Courier New", 11, FontStyle.Bold)
                Dim yPos As Single = 5
                Dim leftMargin As Single = 5
                
                ' Header
                Dim headerText = "OVEN DELIGHTS"
                Dim headerSize = e.Graphics.MeasureString(headerText, fontLarge)
                e.Graphics.DrawString(headerText, fontLarge, Brushes.Black, (302 - headerSize.Width) / 2, yPos)
                yPos += 22
                
                Dim eftTitle = "EFT PAYMENT SLIP"
                Dim eftTitleSize = e.Graphics.MeasureString(eftTitle, fontLarge)
                e.Graphics.DrawString(eftTitle, fontLarge, Brushes.Black, (302 - eftTitleSize.Width) / 2, yPos)
                yPos += 22
                
                e.Graphics.DrawString("======================================", fontBold, Brushes.Black, leftMargin, yPos)
                yPos += 15
                
                ' Bank details
                e.Graphics.DrawString("BANK DETAILS:", fontBold, Brushes.Black, leftMargin, yPos)
                yPos += 15
                e.Graphics.DrawString("Bank: ABSA Bank", fontBold, Brushes.Black, leftMargin, yPos)
                yPos += 14
                e.Graphics.DrawString("Account Name: Oven Delights (Pty) Ltd", fontBold, Brushes.Black, leftMargin, yPos)
                yPos += 14
                e.Graphics.DrawString("Account Number: 4012345678", fontBold, Brushes.Black, leftMargin, yPos)
                yPos += 14
                e.Graphics.DrawString("Branch Code: 632005", fontBold, Brushes.Black, leftMargin, yPos)
                yPos += 14
                e.Graphics.DrawString("Account Type: Business Cheque", fontBold, Brushes.Black, leftMargin, yPos)
                yPos += 18
                
                e.Graphics.DrawString("======================================", fontBold, Brushes.Black, leftMargin, yPos)
                yPos += 15
                
                ' Payment details
                e.Graphics.DrawString("PAYMENT DETAILS:", fontBold, Brushes.Black, leftMargin, yPos)
                yPos += 15
                e.Graphics.DrawString($"Amount Due: R {_totalAmount:N2}", fontLarge, Brushes.Black, leftMargin, yPos)
                yPos += 18
                e.Graphics.DrawString($"Reference: INV-{DateTime.Now:yyyyMMddHHmmss}", fontBold, Brushes.Black, leftMargin, yPos)
                yPos += 18
                
                e.Graphics.DrawString("======================================", fontBold, Brushes.Black, leftMargin, yPos)
                yPos += 15
                
                e.Graphics.DrawString("Use reference number for payment", fontBold, Brushes.Black, leftMargin, yPos)
                yPos += 14
                e.Graphics.DrawString($"Date: {DateTime.Now:dd/MM/yyyy HH:mm}", fontBold, Brushes.Black, leftMargin, yPos)
                yPos += 14
                e.Graphics.DrawString($"Cashier: {_cashierName}", fontBold, Brushes.Black, leftMargin, yPos)
                yPos += 18
                
                e.Graphics.DrawString("======================================", fontBold, Brushes.Black, leftMargin, yPos)
                yPos += 15
                
                Dim footer = "Thank you!"
                Dim footerSize = e.Graphics.MeasureString(footer, fontBold)
                e.Graphics.DrawString(footer, fontBold, Brushes.Black, (302 - footerSize.Width) / 2, yPos)
            End Sub
            
            printDoc.Print()
            MessageBox.Show("EFT payment slip printed successfully!", "Print Complete", MessageBoxButtons.OK, MessageBoxIcon.Information)
            
        Catch ex As Exception
            MessageBox.Show($"Print error: {ex.Message}", "Print Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub
    
    ''' <summary>
    ''' Print receipt to thermal slip printer ONLY (default printer)
    ''' </summary>
    Private Sub PrintReceiptDual(invoiceNumber As String, saleDateTime As DateTime, changeAmount As Decimal)
        ' Print to thermal slip printer only
        Dim dualPrinter As New DualReceiptPrinter(_connectionString, _branchID)
        Dim receiptData As New Dictionary(Of String, Object) From {
            {"InvoiceNumber", invoiceNumber},
            {"SaleDateTime", saleDateTime},
            {"ChangeAmount", changeAmount},
            {"BranchName", GetBranchName()},
            {"TillNumber", GetTillNumber()},
            {"CashierName", GetCashierName()},
            {"PaymentMethod", _paymentMethod},
            {"CashAmount", _cashAmount},
            {"CardAmount", _cardAmount},
            {"Subtotal", _subtotal},
            {"TaxAmount", _taxAmount},
            {"TotalAmount", _totalAmount}
        }
        
        ' Print ONLY to thermal printer (not continuous)
        Try
            dualPrinter.PrintToThermalPrinter(receiptData, _cartItems)
        Catch ex As Exception
            MessageBox.Show($"Thermal printer error: {ex.Message}", "Printer Error", MessageBoxButtons.OK, MessageBoxIcon.Warning)
        End Try
    End Sub
    
    ''' <summary>
    ''' LEGACY: Print receipt to default printer (Epson thermal slip printer - 80mm)
    ''' This method is kept for reference but replaced by PrintReceiptDual
    ''' </summary>
    Private Sub PrintReceiptToDefaultPrinter_OLD(invoiceNumber As String, saleDateTime As DateTime, changeAmount As Decimal)
        Dim printDoc As New Printing.PrintDocument()
        
        ' Configure for 80mm thermal printer (Epson)
        printDoc.DefaultPageSettings.PaperSize = New Printing.PaperSize("80mm", 302, 3000) ' 80mm width, variable height
        
        ' Store receipt data for printing
        Dim receiptData As New Dictionary(Of String, Object) From {
            {"InvoiceNumber", invoiceNumber},
            {"SaleDateTime", saleDateTime},
            {"ChangeAmount", changeAmount},
            {"BranchName", GetBranchName()},
            {"TillNumber", GetTillNumber()},
            {"CashierName", GetCashierName()}
        }
        
        AddHandler printDoc.PrintPage, Sub(sender, e)
            ' Fonts optimized for 80mm thermal printer
            Dim font As New Font("Courier New", 8)
            Dim fontBold As New Font("Courier New", 8, FontStyle.Bold)
            Dim fontLarge As New Font("Courier New", 11, FontStyle.Bold)
            Dim yPos As Single = 5
            Dim leftMargin As Single = 5
            Dim centerPos As Single = 140 ' Center for 80mm (302 pixels / 2)
            
            ' Store header - centered
            Dim headerText = "OVEN DELIGHTS"
            Dim headerSize = e.Graphics.MeasureString(headerText, fontLarge)
            e.Graphics.DrawString(headerText, fontLarge, Brushes.Black, (302 - headerSize.Width) / 2, yPos)
            yPos += 22
            
            ' Branch info
            e.Graphics.DrawString(receiptData("BranchName").ToString(), font, Brushes.Black, leftMargin, yPos)
            yPos += 15
            
            ' Date and time
            e.Graphics.DrawString(CType(receiptData("SaleDateTime"), DateTime).ToString("dd/MM/yyyy HH:mm:ss"), font, Brushes.Black, leftMargin, yPos)
            yPos += 15
            
            ' Invoice number
            e.Graphics.DrawString($"Invoice: {receiptData("InvoiceNumber")}", font, Brushes.Black, leftMargin, yPos)
            yPos += 15
            
            ' Till and Cashier
            e.Graphics.DrawString($"Till: {receiptData("TillNumber")}", font, Brushes.Black, leftMargin, yPos)
            yPos += 15
            e.Graphics.DrawString($"Cashier: {receiptData("CashierName")}", font, Brushes.Black, leftMargin, yPos)
            yPos += 18
            
            ' Separator
            e.Graphics.DrawString("======================================", font, Brushes.Black, leftMargin, yPos)
            yPos += 15
            
            ' Column headers
            e.Graphics.DrawString("Item              Qty  Price  Total", fontBold, Brushes.Black, leftMargin, yPos)
            yPos += 15
            
            ' Line items
            For Each row As DataRow In _cartItems.Rows
                Dim itemName = row("Product").ToString()
                If itemName.Length > 17 Then itemName = itemName.Substring(0, 14) & "..."
                
                Dim qty = CDec(row("Qty"))
                Dim price = CDec(row("Price"))
                Dim lineTotal = CDec(row("Total"))
                
                Dim line = String.Format("{0,-17} {1,3} {2,5:N2} {3,6:N2}", itemName, qty, price, lineTotal)
                e.Graphics.DrawString(line, font, Brushes.Black, leftMargin, yPos)
                yPos += 14
            Next
            
            ' Separator
            e.Graphics.DrawString("======================================", font, Brushes.Black, leftMargin, yPos)
            yPos += 15
            
            ' Totals
            e.Graphics.DrawString($"Subtotal:                 R {_subtotal:N2}", font, Brushes.Black, leftMargin, yPos)
            yPos += 14
            e.Graphics.DrawString($"Tax (15%):                R {_taxAmount:N2}", font, Brushes.Black, leftMargin, yPos)
            yPos += 14
            e.Graphics.DrawString($"TOTAL:                    R {_totalAmount:N2}", fontBold, Brushes.Black, leftMargin, yPos)
            yPos += 18
            
            ' Payment info
            e.Graphics.DrawString($"Payment: {_paymentMethod}", font, Brushes.Black, leftMargin, yPos)
            yPos += 14
            
            If _paymentMethod = "CASH" OrElse _paymentMethod = "SPLIT" Then
                e.Graphics.DrawString($"Cash Tendered:            R {_cashAmount:N2}", font, Brushes.Black, leftMargin, yPos)
                yPos += 14
                If changeAmount > 0 Then
                    e.Graphics.DrawString($"CHANGE:                   R {changeAmount:N2}", fontBold, Brushes.Black, leftMargin, yPos)
                    yPos += 14
                End If
            End If
            
            If _paymentMethod = "SPLIT" Then
                e.Graphics.DrawString($"Card Amount:              R {_cardAmount:N2}", font, Brushes.Black, leftMargin, yPos)
                yPos += 14
            End If
            
            yPos += 10
            e.Graphics.DrawString("======================================", font, Brushes.Black, leftMargin, yPos)
            yPos += 15
            
            ' Footer - centered
            Dim footer1 = "Thank you for your purchase!"
            Dim footer1Size = e.Graphics.MeasureString(footer1, font)
            e.Graphics.DrawString(footer1, font, Brushes.Black, (302 - footer1Size.Width) / 2, yPos)
            yPos += 14
            
            Dim footer2 = "Please come again!"
            Dim footer2Size = e.Graphics.MeasureString(footer2, font)
            e.Graphics.DrawString(footer2, font, Brushes.Black, (302 - footer2Size.Width) / 2, yPos)
            yPos += 20
        End Sub
        
        ' Print to default printer
        printDoc.Print()
    End Sub
End Class
