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
    
    Private _darkBlue As Color = ColorTranslator.FromHtml("#2C3E50")
    Private _green As Color = ColorTranslator.FromHtml("#27AE60")
    Private _orange As Color = ColorTranslator.FromHtml("#E67E22")
    Private _purple As Color = ColorTranslator.FromHtml("#9B59B6")
    Private _lightGray As Color = ColorTranslator.FromHtml("#ECF0F1")
    
    ' Constructor for regular sales and order collection
    Public Sub New(cashierID As Integer, cashierName As String, branchID As Integer, tillPointID As Integer, branchPrefix As String, cartItems As DataTable, subtotal As Decimal, taxAmount As Decimal, totalAmount As Decimal, Optional isOrderCollection As Boolean = False, Optional orderID As Integer = 0, Optional orderNumber As String = "")
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
        _connectionString = ConfigurationManager.ConnectionStrings("OvenDelightsERPConnectionString").ConnectionString
        
        InitializeComponent()
        ShowPaymentMethodSelection()
    End Sub
    
    ' Constructor for order deposits (simplified - no cart items needed, no sale recording)
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
        _isOrderCollection = False ' This is a deposit, not a collection
        _orderID = 0
        _orderNumber = "DEPOSIT" ' Flag to indicate this is just payment collection, not a sale
        _connectionString = ConfigurationManager.ConnectionStrings("OvenDelightsERPConnectionString").ConnectionString
        
        InitializeComponent()
        ShowPaymentMethodSelection()
    End Sub
    
    Private Sub InitializeComponent()
        Me.Text = "Payment Tender"
        Me.BackColor = _lightGray
        Me.FormBorderStyle = FormBorderStyle.Sizable
        Me.MaximizeBox = False
        Me.MinimizeBox = False
        Me.TopMost = True
        Me.WindowState = FormWindowState.Normal
        Me.ShowInTaskbar = True
        Me.ControlBox = True
        
        ' Force minimum size
        Me.MinimumSize = New Size(600, 500)
    End Sub
    
    Private Sub ShowPaymentMethodSelection()
        Me.Controls.Clear()
        
        ' Use fixed size that fits the screen
        Me.Size = New Size(1100, 650)
        Me.StartPosition = FormStartPosition.CenterScreen
        
        Dim pnlHeader As New Panel With {.Dock = DockStyle.Top, .Height = 100, .BackColor = _darkBlue}
        Dim lblTitle As New Label With {.Text = "💳 SELECT PAYMENT METHOD", .Font = New Font("Segoe UI", 24, FontStyle.Bold), .ForeColor = Color.White, .AutoSize = True, .Location = New Point(30, 30)}
        Dim lblAmount As New Label With {.Text = $"Total: {_totalAmount.ToString("C2")}", .Font = New Font("Segoe UI", 20, FontStyle.Bold), .ForeColor = ColorTranslator.FromHtml("#F39C12"), .AutoSize = True, .Location = New Point(800, 35)}
        pnlHeader.Controls.AddRange({lblTitle, lblAmount})
        
        Dim pnlButtons As New Panel With {.Location = New Point(50, 130), .Size = New Size(1000, 400)}
        
        ' All buttons same size: 220x350 (tall vertical rectangle) in single row
        Dim buttonSize As New Size(220, 350)
        Dim spacing As Integer = 250 ' 220 + 30 gap
        
        Dim btnCash As New Button With {.Size = buttonSize, .Location = New Point(0, 0), .BackColor = _green, .ForeColor = Color.White, .FlatStyle = FlatStyle.Flat, .Cursor = Cursors.Hand}
        btnCash.FlatAppearance.BorderSize = 0
        Dim lblCashIcon As New Label With {.Text = "💵", .Font = New Font("Segoe UI", 90), .AutoSize = True, .Location = New Point(60, 50), .BackColor = Color.Transparent}
        lblCashIcon.Enabled = False ' Allow clicks to pass through
        Dim lblCashText As New Label With {.Text = "CASH", .Font = New Font("Segoe UI", 26, FontStyle.Bold), .ForeColor = Color.White, .AutoSize = True, .Location = New Point(55, 200), .BackColor = Color.Transparent}
        lblCashText.Enabled = False ' Allow clicks to pass through
        Dim lblCashSub As New Label With {.Text = "Pay with Cash", .Font = New Font("Segoe UI", 13), .ForeColor = Color.White, .AutoSize = True, .Location = New Point(45, 250), .BackColor = Color.Transparent}
        lblCashSub.Enabled = False ' Allow clicks to pass through
        btnCash.Controls.AddRange({lblCashIcon, lblCashText, lblCashSub})
        AddHandler btnCash.Click, Sub() ProcessCashPayment()
        
        Dim btnCard As New Button With {.Size = buttonSize, .Location = New Point(spacing, 0), .BackColor = _purple, .ForeColor = Color.White, .FlatStyle = FlatStyle.Flat, .Cursor = Cursors.Hand}
        btnCard.FlatAppearance.BorderSize = 0
        Dim lblCardIcon As New Label With {.Text = "💳", .Font = New Font("Segoe UI", 90), .AutoSize = True, .Location = New Point(60, 50), .BackColor = Color.Transparent}
        lblCardIcon.Enabled = False
        Dim lblCardText As New Label With {.Text = "CARD", .Font = New Font("Segoe UI", 26, FontStyle.Bold), .ForeColor = Color.White, .AutoSize = True, .Location = New Point(50, 200), .BackColor = Color.Transparent}
        lblCardText.Enabled = False
        Dim lblCardSub As New Label With {.Text = "Credit/Debit Card", .Font = New Font("Segoe UI", 12), .ForeColor = Color.White, .AutoSize = True, .Location = New Point(25, 250), .BackColor = Color.Transparent}
        lblCardSub.Enabled = False
        btnCard.Controls.AddRange({lblCardIcon, lblCardText, lblCardSub})
        AddHandler btnCard.Click, Sub() ProcessCardPayment()
        
        Dim btnEFT As New Button With {.Size = buttonSize, .Location = New Point(spacing * 2, 0), .BackColor = ColorTranslator.FromHtml("#3498DB"), .ForeColor = Color.White, .FlatStyle = FlatStyle.Flat, .Cursor = Cursors.Hand}
        btnEFT.FlatAppearance.BorderSize = 0
        Dim lblEFTIcon As New Label With {.Text = "🏦", .Font = New Font("Segoe UI", 90), .AutoSize = True, .Location = New Point(60, 50), .BackColor = Color.Transparent}
        lblEFTIcon.Enabled = False
        Dim lblEFTText As New Label With {.Text = "EFT", .Font = New Font("Segoe UI", 26, FontStyle.Bold), .ForeColor = Color.White, .AutoSize = True, .Location = New Point(75, 200), .BackColor = Color.Transparent}
        lblEFTText.Enabled = False
        Dim lblEFTSub As New Label With {.Text = "Bank Transfer", .Font = New Font("Segoe UI", 13), .ForeColor = Color.White, .AutoSize = True, .Location = New Point(45, 250), .BackColor = Color.Transparent}
        lblEFTSub.Enabled = False
        btnEFT.Controls.AddRange({lblEFTIcon, lblEFTText, lblEFTSub})
        AddHandler btnEFT.Click, Sub() ProcessEFTPayment()
        
        Dim btnSplit As New Button With {.Size = buttonSize, .Location = New Point(spacing * 3, 0), .BackColor = _orange, .ForeColor = Color.White, .FlatStyle = FlatStyle.Flat, .Cursor = Cursors.Hand}
        btnSplit.FlatAppearance.BorderSize = 0
        Dim lblSplitIcon As New Label With {.Text = "💵💳", .Font = New Font("Segoe UI", 75), .AutoSize = True, .Location = New Point(35, 50), .BackColor = Color.Transparent}
        lblSplitIcon.Enabled = False
        Dim lblSplitText As New Label With {.Text = "SPLIT", .Font = New Font("Segoe UI", 26, FontStyle.Bold), .ForeColor = Color.White, .AutoSize = True, .Location = New Point(55, 200), .BackColor = Color.Transparent}
        lblSplitText.Enabled = False
        Dim lblSplitSub As New Label With {.Text = "Cash + Card", .Font = New Font("Segoe UI", 13), .ForeColor = Color.White, .AutoSize = True, .Location = New Point(45, 250), .BackColor = Color.Transparent}
        lblSplitSub.Enabled = False
        btnSplit.Controls.AddRange({lblSplitIcon, lblSplitText, lblSplitSub})
        AddHandler btnSplit.Click, Sub() ProcessSplitPayment()
        
        pnlButtons.Controls.AddRange({btnCash, btnCard, btnEFT, btnSplit})
        
        Dim pnlBottom As New Panel With {.Dock = DockStyle.Bottom, .Height = 80, .BackColor = _lightGray}
        Dim btnCancel As New Button With {.Text = "✖ CANCEL", .Size = New Size(250, 60), .Location = New Point(425, 10), .BackColor = ColorTranslator.FromHtml("#E74C3C"), .ForeColor = Color.White, .Font = New Font("Segoe UI", 14, FontStyle.Bold), .FlatStyle = FlatStyle.Flat, .Cursor = Cursors.Hand}
        btnCancel.FlatAppearance.BorderSize = 0
        AddHandler btnCancel.Click, Sub() Me.DialogResult = DialogResult.Cancel
        pnlBottom.Controls.Add(btnCancel)
        
        Me.Controls.AddRange({pnlHeader, pnlButtons, pnlBottom})
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
    
    Private Sub ShowEFTSlip()
        Me.Controls.Clear()
        ResponsiveHelper.ScaleForm(Me, 600, 800)
        
        ' Header
        Dim pnlHeader As New Panel With {.Dock = DockStyle.Top, .Height = ResponsiveHelper.ScaleSize(80), .BackColor = ColorTranslator.FromHtml("#3498DB")}
        Dim lblHeader As New Label With {
            .Text = "🏦 EFT PAYMENT SLIP",
            .Font = ResponsiveHelper.CreateScaledFont("Segoe UI", 24, FontStyle.Bold),
            .ForeColor = Color.White,
            .TextAlign = ContentAlignment.MiddleCenter,
            .Dock = DockStyle.Fill
        }
        pnlHeader.Controls.Add(lblHeader)
        
        ' Slip content
        Dim pnlSlip As New Panel With {
            .Location = ResponsiveHelper.ScalePoint(New Point(50, 100)),
            .Size = ResponsiveHelper.ScaleSize(New Size(500, 550)),
            .BackColor = Color.White,
            .BorderStyle = BorderStyle.FixedSingle
        }
        
        Dim yPos = 20
        
        ' Bank details
        Dim lblBankHeader As New Label With {
            .Text = "BANK DETAILS",
            .Font = New Font("Courier New", 14, FontStyle.Bold),
            .Location = New Point(150, yPos),
            .AutoSize = True
        }
        pnlSlip.Controls.Add(lblBankHeader)
        yPos += 40
        
        Dim lblSeparator1 As New Label With {
            .Text = "========================================",
            .Font = New Font("Courier New", 10),
            .Location = New Point(50, yPos),
            .AutoSize = True
        }
        pnlSlip.Controls.Add(lblSeparator1)
        yPos += 30
        
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
                .Font = New Font("Courier New", 11),
                .Location = New Point(80, yPos),
                .AutoSize = True
            }
            pnlSlip.Controls.Add(lblInfo)
            yPos += 30
        Next
        
        yPos += 10
        Dim lblSeparator2 As New Label With {
            .Text = "========================================",
            .Font = New Font("Courier New", 10),
            .Location = New Point(50, yPos),
            .AutoSize = True
        }
        pnlSlip.Controls.Add(lblSeparator2)
        yPos += 40
        
        ' Payment details
        Dim lblPaymentHeader As New Label With {
            .Text = "PAYMENT DETAILS",
            .Font = New Font("Courier New", 14, FontStyle.Bold),
            .Location = New Point(140, yPos),
            .AutoSize = True
        }
        pnlSlip.Controls.Add(lblPaymentHeader)
        yPos += 40
        
        Dim lblAmount As New Label With {
            .Text = $"Amount Due: R{_totalAmount:N2}",
            .Font = New Font("Courier New", 14, FontStyle.Bold),
            .ForeColor = ColorTranslator.FromHtml("#E67E22"),
            .Location = New Point(120, yPos),
            .AutoSize = True
        }
        pnlSlip.Controls.Add(lblAmount)
        yPos += 40
        
        Dim lblReference As New Label With {
            .Text = $"Reference: INV-{DateTime.Now:yyyyMMddHHmmss}",
            .Font = New Font("Courier New", 11),
            .Location = New Point(80, yPos),
            .AutoSize = True
        }
        pnlSlip.Controls.Add(lblReference)
        yPos += 40
        
        Dim lblInstructions As New Label With {
            .Text = "Use reference number for payment",
            .Font = New Font("Courier New", 10),
            .ForeColor = ColorTranslator.FromHtml("#E74C3C"),
            .Location = New Point(80, yPos),
            .AutoSize = True
        }
        pnlSlip.Controls.Add(lblInstructions)
        
        ' Buttons
        Dim pnlButtons As New Panel With {.Dock = DockStyle.Bottom, .Height = ResponsiveHelper.ScaleSize(80), .BackColor = _lightGray}
        
        Dim btnConfirm As New Button With {
            .Text = "✓ CONFIRM PAYMENT",
            .Size = ResponsiveHelper.EnsureTouchTarget(ResponsiveHelper.ScaleSize(New Size(250, 60))),
            .Location = ResponsiveHelper.ScalePoint(New Point(50, 10)),
            .BackColor = ColorTranslator.FromHtml("#27AE60"),
            .ForeColor = Color.White,
            .Font = ResponsiveHelper.CreateScaledFont("Segoe UI", 14, FontStyle.Bold),
            .FlatStyle = FlatStyle.Flat,
            .Cursor = Cursors.Hand
        }
        btnConfirm.FlatAppearance.BorderSize = 0
        AddHandler btnConfirm.Click, Sub() CompleteTransactionAndShowReceipt()
        
        Dim btnBack As New Button With {
            .Text = "← BACK",
            .Size = ResponsiveHelper.EnsureTouchTarget(ResponsiveHelper.ScaleSize(New Size(250, 60))),
            .Location = ResponsiveHelper.ScalePoint(New Point(320, 10)),
            .BackColor = ColorTranslator.FromHtml("#E74C3C"),
            .ForeColor = Color.White,
            .Font = ResponsiveHelper.CreateScaledFont("Segoe UI", 14, FontStyle.Bold),
            .FlatStyle = FlatStyle.Flat,
            .Cursor = Cursors.Hand
        }
        btnBack.FlatAppearance.BorderSize = 0
        AddHandler btnBack.Click, Sub() ShowPaymentMethodSelection()
        
        pnlButtons.Controls.AddRange({btnConfirm, btnBack})
        
        Me.Controls.AddRange({pnlHeader, pnlSlip, pnlButtons})
    End Sub
    
    Private Sub ShowCashKeypad(amountDue As Decimal, title As String)
        Me.Controls.Clear()
        Me.Size = New Size(600, 750)
        Me.StartPosition = FormStartPosition.CenterScreen
        
        ' Adjust header height for split payments
        Dim headerHeight = If(_paymentMethod = "SPLIT", 150, 120)
        Dim pnlHeader As New Panel With {.Dock = DockStyle.Top, .Height = headerHeight, .BackColor = _green}
        
        ' Add CASH DUE label for split payments (centered at top, no amount)
        If _paymentMethod = "SPLIT" Then
            Dim lblCashDue As New Label With {
                .Text = "CASH DUE",
                .Font = New Font("Segoe UI", 24, FontStyle.Bold),
                .ForeColor = Color.Yellow,
                .AutoSize = True,
                .TextAlign = ContentAlignment.MiddleCenter
            }
            ' Center the label
            lblCashDue.Location = New Point((600 - lblCashDue.Width) \ 2, 10)
            pnlHeader.Controls.Add(lblCashDue)
        End If
        
        Dim lblAmountDue As New Label With {.Text = $"AMOUNT DUE: R{amountDue:N2}", .Font = New Font("Segoe UI", 18, FontStyle.Bold), .ForeColor = Color.White, .AutoSize = True, .Location = New Point(20, If(_paymentMethod = "SPLIT", 50, 15))}
        Dim lblTendered As New Label With {.Text = "TENDERED: R0.00", .Font = New Font("Segoe UI", 16), .ForeColor = Color.White, .AutoSize = True, .Location = New Point(20, If(_paymentMethod = "SPLIT", 80, 50)), .Name = "lblTendered"}
        Dim lblChange As New Label With {.Text = "CHANGE: R0.00", .Font = New Font("Segoe UI", 20, FontStyle.Bold), .ForeColor = Color.Yellow, .AutoSize = True, .Location = New Point(20, If(_paymentMethod = "SPLIT", 110, 80)), .Name = "lblChange"}
        
        pnlHeader.Controls.AddRange({lblAmountDue, lblTendered, lblChange})
        
        ' Make text box accept keyboard input
        Dim txtAmount As New TextBox With {.Font = New Font("Segoe UI", 48, FontStyle.Bold), .TextAlign = HorizontalAlignment.Right, .Location = New Point(50, 140), .Size = New Size(500, 80), .Text = "0.00", .ReadOnly = False, .BackColor = Color.White, .ForeColor = _darkBlue, .Name = "txtAmount"}
        
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
        
        Dim pnlKeypad As New Panel With {.Location = New Point(100, 240), .Size = New Size(400, 350)}
        Dim buttonSize As New Size(120, 70)
        Dim buttons(,) As String = {{"7", "8", "9"}, {"4", "5", "6"}, {"1", "2", "3"}, {".", "0", "⌫"}}
        
        Dim keySpacingX = 130
        Dim keySpacingY = 80
        
        For row = 0 To 3
            For col = 0 To 2
                Dim btnText = buttons(row, col)
                Dim btn As New Button With {.Text = btnText, .Size = buttonSize, .Location = New Point(col * keySpacingX, row * keySpacingY), .Font = New Font("Segoe UI", 24, FontStyle.Bold), .BackColor = Color.White, .ForeColor = _darkBlue, .FlatStyle = FlatStyle.Flat, .Cursor = Cursors.Hand}
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
        
        Dim pnlButtons As New Panel With {.Dock = DockStyle.Bottom, .Height = 90, .BackColor = _lightGray}
        
        Dim btnConfirm As New Button With {.Text = "✓ CONFIRM", .Size = New Size(250, 60), .Location = New Point(50, 15), .BackColor = _green, .ForeColor = Color.White, .Font = New Font("Segoe UI", 16, FontStyle.Bold), .FlatStyle = FlatStyle.Flat, .Cursor = Cursors.Hand}
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
        
        Dim btnBack As New Button With {.Text = "← BACK", .Size = New Size(250, 60), .Location = New Point(320, 15), .BackColor = ColorTranslator.FromHtml("#E74C3C"), .ForeColor = Color.White, .Font = New Font("Segoe UI", 16, FontStyle.Bold), .FlatStyle = FlatStyle.Flat, .Cursor = Cursors.Hand}
        btnBack.FlatAppearance.BorderSize = 0
        AddHandler btnBack.Click, Sub() ShowPaymentMethodSelection()
        
        pnlButtons.Controls.AddRange({btnConfirm, btnBack})
        
        Me.Controls.AddRange({pnlHeader, txtAmount, pnlKeypad, pnlButtons})
    End Sub
    
    Private Sub ProcessCardTransaction(amount As Decimal)
        Me.Controls.Clear()
        ResponsiveHelper.ScaleForm(Me, 700, 600)
        
        Dim pnlMain As New Panel With {.Dock = DockStyle.Fill, .BackColor = _darkBlue}
        
        Dim lblIcon As New Label With {.Text = "💳", .Font = ResponsiveHelper.CreateScaledFont("Segoe UI", 100), .ForeColor = Color.White, .AutoSize = True, .Location = ResponsiveHelper.ScalePoint(New Point(280, 30))}
        
        ' Show appropriate label based on payment type
        Dim amountLabel = If(_paymentMethod = "SPLIT", "Balance Outstanding:", "Amount Due:")
        Dim lblAmountLabel As New Label With {
            .Text = amountLabel,
            .Font = ResponsiveHelper.CreateScaledFont("Segoe UI", 18, FontStyle.Bold),
            .ForeColor = Color.White,
            .AutoSize = True,
            .Location = ResponsiveHelper.ScalePoint(New Point(230, 150))
        }
        
        Dim lblAmount As New Label With {.Text = amount.ToString("C2"), .Font = ResponsiveHelper.CreateScaledFont("Segoe UI", 48, FontStyle.Bold), .ForeColor = ColorTranslator.FromHtml("#F39C12"), .AutoSize = True, .Location = ResponsiveHelper.ScalePoint(New Point(240, 190))}
        
        Dim lblInstruction As New Label With {
            .Text = "INSERT OR TAP CARD",
            .Font = ResponsiveHelper.CreateScaledFont("Segoe UI", 28, FontStyle.Bold),
            .ForeColor = Color.White,
            .AutoSize = True,
            .Location = ResponsiveHelper.ScalePoint(New Point(150, 280))
        }
        
        Dim lblWaiting As New Label With {
            .Text = "Waiting for customer...",
            .Font = ResponsiveHelper.CreateScaledFont("Segoe UI", 16),
            .ForeColor = ColorTranslator.FromHtml("#BDC3C7"),
            .AutoSize = True,
            .Location = ResponsiveHelper.ScalePoint(New Point(220, 340))
        }
        
        pnlMain.Controls.AddRange({lblIcon, lblAmountLabel, lblAmount, lblInstruction, lblWaiting})
        
        ' Show cash tendered for split payment
        If _paymentMethod = "SPLIT" Then
            Dim lblCashTendered As New Label With {
                .Text = $"Cash Tendered: {_cashAmount.ToString("C2")}",
                .Font = ResponsiveHelper.CreateScaledFont("Segoe UI", 16, FontStyle.Bold),
                .ForeColor = _green,
                .AutoSize = True,
                .Location = ResponsiveHelper.ScalePoint(New Point(220, 390))
            }
            pnlMain.Controls.Add(lblCashTendered)
            
            Dim lblWarning As New Label With {
                .Text = "⚠ If card fails, return cash to customer",
                .Font = ResponsiveHelper.CreateScaledFont("Segoe UI", 12),
                .ForeColor = ColorTranslator.FromHtml("#E67E22"),
                .AutoSize = True,
                .Location = ResponsiveHelper.ScalePoint(New Point(180, 430))
            }
            pnlMain.Controls.Add(lblWarning)
        End If
        
        Me.Controls.Add(pnlMain)
        
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
        ResponsiveHelper.ScaleForm(Me, 700, 500)
        
        Dim pnlMain As New Panel With {.Dock = DockStyle.Fill, .BackColor = ColorTranslator.FromHtml("#3498DB")} ' Blue
        
        ' Processing icon - centered
        Dim formWidth = Me.Width
        Dim lblIcon As New Label With {
            .Text = "⏳",
            .Font = ResponsiveHelper.CreateScaledFont("Segoe UI", 80),
            .ForeColor = Color.White,
            .TextAlign = ContentAlignment.MiddleCenter,
            .Size = New Size(formWidth, ResponsiveHelper.ScaleSize(100)),
            .Location = New Point(0, ResponsiveHelper.ScaleSize(40))
        }
        
        Dim lblProcessing As New Label With {
            .Text = "PROCESSING AUTHORIZATION",
            .Font = ResponsiveHelper.CreateScaledFont("Segoe UI", 22, FontStyle.Bold),
            .ForeColor = Color.White,
            .TextAlign = ContentAlignment.MiddleCenter,
            .Size = New Size(formWidth, ResponsiveHelper.ScaleSize(40)),
            .Location = New Point(0, ResponsiveHelper.ScaleSize(160))
        }
        
        Dim lblAmount As New Label With {
            .Text = amount.ToString("C2"),
            .Font = ResponsiveHelper.CreateScaledFont("Segoe UI", 36, FontStyle.Bold),
            .ForeColor = ColorTranslator.FromHtml("#F39C12"),
            .TextAlign = ContentAlignment.MiddleCenter,
            .Size = New Size(formWidth, ResponsiveHelper.ScaleSize(50)),
            .Location = New Point(0, ResponsiveHelper.ScaleSize(220))
        }
        
        Dim lblWait As New Label With {
            .Text = "Please wait...",
            .Font = ResponsiveHelper.CreateScaledFont("Segoe UI", 16),
            .ForeColor = ColorTranslator.FromHtml("#ECF0F1"),
            .TextAlign = ContentAlignment.MiddleCenter,
            .Size = New Size(formWidth, ResponsiveHelper.ScaleSize(30)),
            .Location = New Point(0, ResponsiveHelper.ScaleSize(290))
        }
        
        pnlMain.Controls.AddRange({lblIcon, lblProcessing, lblAmount, lblWait})
        Me.Controls.Add(pnlMain)
        
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
        ResponsiveHelper.ScaleForm(Me, 700, 700)
        
        ' Gradient background colors
        Dim colorTop = ColorTranslator.FromHtml("#27AE60") ' Green
        Dim colorBottom = ColorTranslator.FromHtml("#229954") ' Darker green
        
        Dim pnlMain As New Panel With {.Dock = DockStyle.Fill, .BackColor = colorTop}
        
        ' Success icon - centered
        Dim formWidth = Me.Width
        Dim lblIcon As New Label With {
            .Text = "✓",
            .Font = ResponsiveHelper.CreateScaledFont("Segoe UI", 80, FontStyle.Bold),
            .ForeColor = Color.White,
            .TextAlign = ContentAlignment.MiddleCenter,
            .Size = New Size(formWidth, ResponsiveHelper.ScaleSize(100)),
            .Location = New Point(0, ResponsiveHelper.ScaleSize(40))
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
                        PostToJournalsAndLedgers(conn, transaction, salesID, invoiceNumber)
                        
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
        
        ' Now show the receipt with actual invoice number
        ' Wider for better readability on customer display
        Me.Controls.Clear()
        Me.Size = New Size(500, 700)
        Me.StartPosition = FormStartPosition.CenterScreen
        Me.MinimumSize = New Size(450, 600)
        
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
        
        ' Receipt panel - wider for better readability
        Dim pnlReceipt As New Panel With {.Location = New Point(30, 85), .Size = New Size(440, 520), .BackColor = Color.White, .BorderStyle = BorderStyle.FixedSingle, .AutoScroll = True}
        
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
        
        Dim btnComplete As New Button With {
            .Text = "✓ NEXT SALE",
            .Size = New Size(400, 60),
            .Location = New Point(50, 15),
            .BackColor = _green,
            .ForeColor = Color.White,
            .Font = New Font("Segoe UI", 16, FontStyle.Bold),
            .FlatStyle = FlatStyle.Flat,
            .Cursor = Cursors.Hand
        }
        btnComplete.FlatAppearance.BorderSize = 0
        AddHandler btnComplete.Click, Sub()
            Me.DialogResult = DialogResult.OK
            Me.Close()
        End Sub
        
        pnlButtons.Controls.Add(btnComplete)
        
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
        ' Get till number (just the number, not "TILL1" prefix)
        Dim tillNumber As String = "01"
        Try
            Dim sqlTill = "SELECT TillNumber FROM TillPoints WHERE TillPointID = @TillPointID"
            Using cmdTill As New SqlCommand(sqlTill, conn, transaction)
                cmdTill.Parameters.AddWithValue("@TillPointID", _tillPointID)
                Dim result = cmdTill.ExecuteScalar()
                If result IsNot Nothing Then
                    Dim fullTillNumber = result.ToString()
                    ' Extract just the number part (e.g., "PH-TILL-01" becomes "01")
                    If fullTillNumber.Contains("-") Then
                        Dim parts = fullTillNumber.Split("-"c)
                        tillNumber = parts(parts.Length - 1) ' Get last part
                    Else
                        tillNumber = fullTillNumber
                    End If
                End If
            End Using
        Catch
            tillNumber = "01"
        End Try
        
        ' Format: INV-BranchCode-TILL-TillNumber-000001
        ' Example: INV-PH-TILL-01-000001
        Dim pattern = $"INV-{_branchPrefix}-TILL-{tillNumber}-%"
        Dim sql = "SELECT ISNULL(MAX(CAST(RIGHT(InvoiceNumber, 6) AS INT)), 0) + 1 FROM Demo_Sales WHERE InvoiceNumber LIKE @Pattern"
        Using cmd As New SqlCommand(sql, conn, transaction)
            cmd.Parameters.AddWithValue("@Pattern", pattern)
            Dim nextNumber = CInt(cmd.ExecuteScalar())
            Return $"INV-{_branchPrefix}-TILL-{tillNumber}-{nextNumber.ToString("D6")}"
        End Using
    End Function
    
    Private Function InsertSale(conn As SqlConnection, transaction As SqlTransaction, invoiceNumber As String) As Integer
        ' Skip sale recording if this is just a deposit payment (order will record it)
        If _orderNumber = "DEPOSIT" Then
            Return 0 ' No sale ID needed for deposits
        End If
        
        ' Determine sale type
        Dim saleType As String = "Sale"
        Dim referenceNumber As String = invoiceNumber
        
        If _isOrderCollection Then
            saleType = "OrderCollection"
            referenceNumber = _orderNumber
        End If
        
        Dim insertSql = "INSERT INTO Demo_Sales (SaleNumber, InvoiceNumber, SaleDate, CashierID, BranchID, TillPointID, Subtotal, TaxAmount, TotalAmount, PaymentMethod, CashAmount, CardAmount, SaleType, ReferenceNumber) 
                                VALUES (@SaleNumber, @InvoiceNumber, @SaleDate, @CashierID, @BranchID, @TillPointID, @Subtotal, @TaxAmount, @TotalAmount, @PaymentMethod, @CashAmount, @CardAmount, @SaleType, @ReferenceNumber);
                                SELECT CAST(SCOPE_IDENTITY() AS INT)"
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
            Return CInt(cmd.ExecuteScalar())
        End Using
    End Function
    
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
        Dim sql = "UPDATE Demo_Retail_Stock SET QtyOnHand = QtyOnHand - @Quantity WHERE StockID = @ProductID AND BranchID = @BranchID"
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
            ' Check if GeneralJournal table exists
            Dim checkTable = "SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'GeneralJournal'"
            Using cmdCheck As New SqlCommand(checkTable, conn, transaction)
                Dim tableExists = CInt(cmdCheck.ExecuteScalar()) > 0
                If Not tableExists Then
                    ' GeneralJournal table doesn't exist, skip journal entries
                    Return
                End If
            End Using
            
            Dim cashAccountID = GetAccountID(conn, transaction, "Cash")
            Dim bankAccountID = GetAccountID(conn, transaction, "Bank")
            Dim salesAccountID = GetAccountID(conn, transaction, "Sales")
            Dim vatAccountID = GetAccountID(conn, transaction, "VAT")
            Dim cosAccountID = GetAccountID(conn, transaction, "Cost of Sales")
            Dim inventoryAccountID = GetAccountID(conn, transaction, "Inventory")
            
            If _cashAmount > 0 Then InsertJournalEntry(conn, transaction, invoiceNumber, cashAccountID, "Cash", _cashAmount, 0, "Sale - Cash")
            If _cardAmount > 0 Then InsertJournalEntry(conn, transaction, invoiceNumber, bankAccountID, "Bank", _cardAmount, 0, "Sale - Card")
            InsertJournalEntry(conn, transaction, invoiceNumber, salesAccountID, "Sales", 0, _subtotal, "Sale")
            InsertJournalEntry(conn, transaction, invoiceNumber, vatAccountID, "VAT", 0, _taxAmount, "VAT")
            
            Dim totalCost = CalculateTotalCost(conn, transaction)
            InsertJournalEntry(conn, transaction, invoiceNumber, cosAccountID, "Cost of Sales", totalCost, 0, "COGS")
            InsertJournalEntry(conn, transaction, invoiceNumber, inventoryAccountID, "Inventory", 0, totalCost, "Inventory")
        Catch ex As Exception
            ' If journal entries fail, continue anyway (sale is more important)
            ' Log error but don't throw
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
            
            If totalCost > 0 Then
                InsertJournalEntry(conn, transaction, journalDate, "Sales Journal", invoiceNumber, costOfSalesLedgerID, totalCost, 0, $"COGS for {row("ProductName")}")
                InsertJournalEntry(conn, transaction, journalDate, "Sales Journal", invoiceNumber, inventoryLedgerID, 0, totalCost, $"Inventory reduction for {row("ProductName")}")
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
    
    Private Function GetAverageCost(conn As SqlConnection, transaction As SqlTransaction, productID As Integer, branchID As Integer) As Decimal
        Try
            ' Join through ProductVariants since Retail_Stock uses VariantID
            Dim sql = "SELECT TOP 1 ISNULL(rs.AverageCost, 0) FROM Retail_Stock rs " &
                      "INNER JOIN ProductVariants pv ON rs.VariantID = pv.VariantID " &
                      "WHERE pv.ProductID = @ProductID AND rs.BranchID = @BranchID"
            Using cmd As New SqlCommand(sql, conn, transaction)
                cmd.Parameters.AddWithValue("@ProductID", productID)
                cmd.Parameters.AddWithValue("@BranchID", branchID)
                Dim result = cmd.ExecuteScalar()
                Return If(result IsNot Nothing AndAlso Not IsDBNull(result), CDec(result), 0D)
            End Using
        Catch ex As Exception
            ' Log error and return 0 if cost cannot be determined
            System.Diagnostics.Debug.WriteLine($"Error getting average cost for ProductID {productID}: {ex.Message}")
            Return 0D
        End Try
    End Function
End Class
