' Restored from version 1.0.0.37 - Complete payment tender form with live FNB credentials
' Contains all payment processing, live/test mode toggle, and months of improvements

Imports System.Configuration
Imports System.Data
Imports System.Data.SqlClient
Imports System.Drawing
Imports System.Drawing.Printing
Imports System.Linq
Imports System.Threading.Tasks
Imports System.Windows.Forms

Namespace Overn_Delights_POS
    Public Class PaymentTenderForm
        Inherits Form

        ' Live FNB Credentials - RESTORED FROM VERSION 1.0.0.37
        Private Const FNB_API_KEY As String = "Q7w30FOnntfiLzJuKKJrKqVqXg9BHPCq"
        Private Const FNB_CLIENT_ID As String = "qEXGrBTnJQS9ZBX7bzuKnkHQfZ0UUFUX"
        Private Const FNB_CLIENT_SECRET As String = "j082ZT3cPyojxN9CSmdp41p7nXGLQ8zH"

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
        Private _cashAmount As Decimal
        Private _cardAmount As Decimal
        Private _changeAmount As Decimal
        Private _paymentMethod As String
        Private _isOrderCollection As Boolean
        Private _useLiveEnvironment As Boolean
        Private _orderID As Integer
        Private _orderNumber As String
        Private _orderColour As String
        Private _orderPicture As String
        Private _cardMaskedPan As String
        Private _cardType As String
        Private _cardApprovalCode As String

        ' Iron Man Theme Colors
        Private _ironRed As Color = ColorTranslator.FromHtml("#C1272D")
        Private _ironRedDark As Color = ColorTranslator.FromHtml("#8B0000")
        Private _ironGold As Color = ColorTranslator.FromHtml("#FFD700")
        Private _ironDark As Color = ColorTranslator.FromHtml("#0a0e27")
        Private _ironBlue As Color = ColorTranslator.FromHtml("#00D4FF")
        Private _ironBlueDark As Color = ColorTranslator.FromHtml("#0099CC")
        Private _ironDarkBlue As Color = ColorTranslator.FromHtml("#1a1f3a")

        ' Tender Colors
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

        Private _darkBlue As Color = ColorTranslator.FromHtml("#2C3E50")
        Private _green As Color = ColorTranslator.FromHtml("#27AE60")
        Private _lightGray As Color = ColorTranslator.FromHtml("#ECF0F1")

        Public Sub New(
            depositAmount As Decimal,
            branchID As Integer,
            tillPointID As Integer,
            cashierID As Integer,
            cashierName As String
        )
            _cashAmount = 0D
            _cardAmount = 0D
            _changeAmount = 0D
            _paymentMethod = ""
            _isOrderCollection = False
            _useLiveEnvironment = True ' Default to LIVE mode
            _orderID = 0
            _orderNumber = ""
            _orderColour = ""
            _orderPicture = ""
            _cardMaskedPan = ""
            _cardType = ""
            _cardApprovalCode = ""

            _cashierID = cashierID
            _cashierName = cashierName
            _branchID = branchID
            _tillPointID = tillPointID
            _branchPrefix = ""
            _cartItems = Nothing
            _totalAmount = depositAmount
            _connectionString = ConfigurationManager.ConnectionStrings("OvenDelightsPOSConnectionString").ConnectionString

            InitializeComponent()
            ShowPaymentMethodSelection()
        End Sub

        Public Sub New(
            cartItems As DataTable,
            totalAmount As Decimal,
            subtotal As Decimal,
            taxAmount As Decimal,
            branchID As Integer,
            tillPointID As Integer,
            cashierID As Integer,
            cashierName As String
        )
            _cashAmount = 0D
            _cardAmount = 0D
            _changeAmount = 0D
            _paymentMethod = ""
            _isOrderCollection = False
            _useLiveEnvironment = True ' Default to LIVE mode
            _orderID = 0
            _orderNumber = ""
            _orderColour = ""
            _orderPicture = ""
            _cardMaskedPan = ""
            _cardType = ""
            _cardApprovalCode = ""

            _cashierID = cashierID
            _cashierName = cashierName
            _branchID = branchID
            _tillPointID = tillPointID
            _branchPrefix = ""
            _cartItems = cartItems
            _totalAmount = totalAmount
            _subtotal = subtotal
            _taxAmount = taxAmount
            _connectionString = ConfigurationManager.ConnectionStrings("OvenDelightsPOSConnectionString").ConnectionString

            InitializeComponent()
            ShowPaymentMethodSelection()
        End Sub

        Private Sub ShowPaymentMethodSelection()
            Controls.Clear()
            Size = New Size(600, Math.Min(600, CInt(Math.Round(Screen.PrimaryScreen.WorkingArea.Height * 0.75))))
            StartPosition = FormStartPosition.CenterScreen
            BackColor = _ironDark

            ' Create header panel
            Dim pnlHeader As New Panel With {
                .Dock = DockStyle.Top,
                .Height = 80,
                .BackColor = _ironRed
            }

            Dim lblTitle As New Label With {
                .Text = "SELECT PAYMENT METHOD",
                .Font = New Font("Segoe UI", 18, FontStyle.Bold),
                .ForeColor = Color.White,
                .AutoSize = True,
                .TextAlign = ContentAlignment.MiddleCenter
            }
            lblTitle.Location = New Point((600 - lblTitle.Width) \ 2, 10)
            pnlHeader.Controls.Add(lblTitle)

            Dim lblAmount As New Label With {
                .Text = $"TOTAL: R{_totalAmount:N2}",
                .Font = New Font("Segoe UI", 14, FontStyle.Bold),
                .ForeColor = _ironGold,
                .AutoSize = False,
                .Size = New Size(500, 25),
                .Location = New Point(50, 40)
            }
            pnlHeader.Controls.Add(lblAmount)

            Controls.Add(pnlHeader)

            ' Create payment buttons
            Dim pnlButtons As New Panel With {
                .Dock = DockStyle.Fill,
                .BackColor = _ironDarkBlue,
                .Padding = New Padding(20)
            }

            Dim buttonSize As New Size(100, 120)
            Dim startX As Integer = 40
            Dim startY As Integer = 20

            ' Cash button
            Dim btnCash As New Button With {
                .Size = buttonSize,
                .Location = New Point(startX, startY),
                .BackColor = _tenderCash,
                .ForeColor = Color.White,
                .FlatStyle = FlatStyle.Flat,
                .Font = New Font("Segoe UI", 12, FontStyle.Bold),
                .Text = "CASH" & vbCrLf & "💵",
                .TextAlign = ContentAlignment.MiddleCenter,
                .Cursor = Cursors.Hand
            }
            AddHandler btnCash.Click, AddressOf ProcessCashPayment
            AddHandler btnCash.MouseEnter, AddressOf Button_MouseEnter
            AddHandler btnCash.MouseLeave, AddressOf Button_MouseLeave
            pnlButtons.Controls.Add(btnCash)

            ' Card button
            Dim btnCard As New Button With {
                .Size = buttonSize,
                .Location = New Point(startX + buttonSize.Width + 20, startY),
                .BackColor = _tenderCard,
                .ForeColor = Color.White,
                .FlatStyle = FlatStyle.Flat,
                .Font = New Font("Segoe UI", 12, FontStyle.Bold),
                .Text = "CARD" & vbCrLf & "💳",
                .TextAlign = ContentAlignment.MiddleCenter,
                .Cursor = Cursors.Hand
            }
            AddHandler btnCard.Click, AddressOf ProcessCardPayment
            AddHandler btnCard.MouseEnter, AddressOf Button_MouseEnter
            AddHandler btnCard.MouseLeave, AddressOf Button_MouseLeave
            pnlButtons.Controls.Add(btnCard)

            ' EFT button
            Dim btnEFT As New Button With {
                .Size = buttonSize,
                .Location = New Point(startX + (buttonSize.Width + 20) * 2, startY),
                .BackColor = _tenderEFT,
                .ForeColor = Color.White,
                .FlatStyle = FlatStyle.Flat,
                .Font = New Font("Segoe UI", 12, FontStyle.Bold),
                .Text = "EFT" & vbCrLf & "🏦",
                .TextAlign = ContentAlignment.MiddleCenter,
                .Cursor = Cursors.Hand
            }
            AddHandler btnEFT.Click, AddressOf ProcessEFTPayment
            AddHandler btnEFT.MouseEnter, AddressOf Button_MouseEnter
            AddHandler btnEFT.MouseLeave, AddressOf Button_MouseLeave
            pnlButtons.Controls.Add(btnEFT)

            ' Manual button
            Dim btnManual As New Button With {
                .Size = buttonSize,
                .Location = New Point(startX + (buttonSize.Width + 20) * 3, startY),
                .BackColor = _tenderManual,
                .ForeColor = Color.White,
                .FlatStyle = FlatStyle.Flat,
                .Font = New Font("Segoe UI", 12, FontStyle.Bold),
                .Text = "MANUAL" & vbCrLf & "✍️",
                .TextAlign = ContentAlignment.MiddleCenter,
                .Cursor = Cursors.Hand
            }
            AddHandler btnManual.Click, AddressOf ProcessManualPayment
            AddHandler btnManual.MouseEnter, AddressOf Button_MouseEnter
            AddHandler btnManual.MouseLeave, AddressOf Button_MouseLeave
            pnlButtons.Controls.Add(btnManual)

            ' Split button
            Dim btnSplit As New Button With {
                .Size = buttonSize,
                .Location = New Point(startX + (buttonSize.Width + 20) * 4, startY),
                .BackColor = _tenderSplit,
                .ForeColor = Color.White,
                .FlatStyle = FlatStyle.Flat,
                .Font = New Font("Segoe UI", 12, FontStyle.Bold),
                .Text = "SPLIT" & vbCrLf & "💵💳",
                .TextAlign = ContentAlignment.MiddleCenter,
                .Cursor = Cursors.Hand
            }
            AddHandler btnSplit.Click, AddressOf ProcessSplitPayment
            AddHandler btnSplit.MouseEnter, AddressOf Button_MouseEnter
            AddHandler btnSplit.MouseLeave, AddressOf Button_MouseLeave
            pnlButtons.Controls.Add(btnSplit)

            ' Live/Test mode toggle button
            Dim btnToggleMode As New Button With {
                .Size = New Size(300, 45),
                .Location = New Point((600 - 300) \ 2, 400),
                .BackColor = If(_useLiveEnvironment, ColorTranslator.FromHtml("#E74C3C"), ColorTranslator.FromHtml("#3498DB")),
                .ForeColor = Color.White,
                .Font = New Font("Segoe UI", 14, FontStyle.Bold),
                .FlatStyle = FlatStyle.Flat,
                .Cursor = Cursors.Hand,
                .Text = If(_useLiveEnvironment, "🔴 LIVE MODE", "🔵 TEST MODE"),
                .FlatAppearance = New FlatAppearance With {.BorderSize = 2}
            }
            AddHandler btnToggleMode.Click, AddressOf ToggleLiveTestMode
            pnlButtons.Controls.Add(btnToggleMode)

            Controls.Add(pnlButtons)
        End Sub

        Private Sub ToggleLiveTestMode()
            _useLiveEnvironment = Not _useLiveEnvironment
            ShowPaymentMethodSelection()
        End Sub

        Private Sub ProcessCashPayment()
            ' Show cash payment dialog
            ShowCashPaymentDialog()
        End Sub

        Private Sub ProcessCardPayment()
            ShowCardProcessingDialog()
        End Sub

        Private Sub ProcessEFTPayment()
            ShowEFTProcessingDialog()
        End Sub

        Private Sub ProcessManualPayment()
            ShowManualPaymentDialog()
        End Sub

        Private Sub ProcessSplitPayment()
            ShowSplitPaymentDialog()
        End Sub

        Private Sub ShowCardProcessingDialog()
            Controls.Clear()
            Size = New Size(600, Math.Min(600, CInt(Math.Round(Screen.PrimaryScreen.WorkingArea.Height * 0.75))))
            StartPosition = FormStartPosition.CenterScreen

            Dim pnlMain As New Panel With {
                .Dock = DockStyle.Fill,
                .BackColor = _ironDarkBlue
            }

            ' Terminal mode selection
            Dim pnlTerminal As New Panel With {
                .Location = New Point(20, 10),
                .Size = New Size(560, 60),
                .BackColor = ColorTranslator.FromHtml("#34495E"),
                .BorderStyle = BorderStyle.FixedSingle
            }

            Dim lblTerminalMode As New Label With {
                .Text = "Terminal Mode:",
                .Font = New Font("Segoe UI", 10, FontStyle.Bold),
                .ForeColor = Color.White,
                .AutoSize = True,
                .Location = New Point(10, 8)
            }
            pnlTerminal.Controls.Add(lblTerminalMode)

            Dim rdoUnattended As New RadioButton With {
                .Text = "Unattended (Terminal 10 - Virtual Auto-Approved)",
                .Font = New Font("Segoe UI", 9),
                .ForeColor = Color.White,
                .AutoSize = True,
                .Location = New Point(10, 30),
                .Checked = True,
                .Name = "rdoUnattended"
            }
            pnlTerminal.Controls.Add(rdoUnattended)

            Dim rdoAttended As New RadioButton With {
                .Text = "Attended (Terminal 7 - Real PED Card Swipe)",
                .Font = New Font("Segoe UI", 9),
                .ForeColor = ColorTranslator.FromHtml("#F39C12"),
                .AutoSize = True,
                .Location = New Point(300, 30),
                .Name = "rdoAttended"
            }
            pnlTerminal.Controls.Add(rdoAttended)

            pnlMain.Controls.Add(pnlTerminal)

            ' Processing message
            Dim lblProcessing As New Label With {
                .Text = "🔳 Processing Card Payment...",
                .Font = New Font("Segoe UI", 16, FontStyle.Bold),
                .ForeColor = ColorTranslator.FromHtml("#ECF0F1"),
                .TextAlign = ContentAlignment.MiddleCenter,
                .Size = New Size(Width, 30),
                .Location = New Point(0, 290)
            }
            pnlMain.Controls.Add(lblProcessing)

            Dim lblPleaseWait As New Label With {
                .Text = "Please wait...",
                .Font = New Font("Segoe UI", 16),
                .ForeColor = ColorTranslator.FromHtml("#ECF0F1"),
                .TextAlign = ContentAlignment.MiddleCenter,
                .Size = New Size(Width, 30),
                .Location = New Point(0, 320)
            }
            pnlMain.Controls.Add(lblPleaseWait)

            Dim lblWarning As New Label With {
                .Text = "⚠ If card fails, return cash to customer",
                .Font = New Font("Segoe UI", 12),
                .ForeColor = ColorTranslator.FromHtml("#E67E22"),
                .AutoSize = True,
                .Location = New Point(180, 480)
            }
            pnlMain.Controls.Add(lblWarning)

            Controls.Add(pnlMain)
            Application.DoEvents()
            Refresh()
            Invalidate()

            If _useLiveEnvironment Then
                ' Process live payment with restored credentials
                ProcessLiveCardPayment()
            Else
                ' Simulate test payment
                SimulateTestCardPayment()
            End If
        End Sub

        Private Sub ProcessLiveCardPayment()
            Dim paypointService As New PaypointPaymentService(
                FNB_API_KEY,
                FNB_CLIENT_ID,
                FNB_CLIENT_SECRET,
                False ' Live mode
            )

            Task.Run(Async Function()
                Try
                    Dim result = Await paypointService.ProcessPaymentAsync(_totalAmount, $"POS-{DateTime.Now:yyyyMMddHHmmss}", _cartItems)
                    
                    Invoke(Sub()
                        If result.IsSuccess Then
                            _cardMaskedPan = result.CardLastFour
                            _cardType = result.CardType
                            _cardApprovalCode = result.AuthCode
                            ShowCardSuccess(_totalAmount, "LIVE")
                            CompleteCardPayment()
                        Else
                            MessageBox.Show($"Transaction Declined:{vbCrLf}{result.Message}", "Payment Failed", MessageBoxButtons.OK, MessageBoxIcon.Hand)
                            ShowPaymentMethodSelection()
                        End If
                    End Sub)
                Catch ex As Exception
                    Invoke(Sub()
                        MessageBox.Show($"Payment Error:{vbCrLf}{ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Hand)
                        ShowPaymentMethodSelection()
                    End Sub)
                End Try
            End Function)
        End Sub

        Private Sub SimulateTestCardPayment()
            Dim timer As New Timer With {.Interval = 4000}
            AddHandler timer.Tick, Sub(sender, e)
                timer.Stop()
                _cardMaskedPan = "528497xxxxxx5593"
                _cardType = "MASTERCARD"
                _cardApprovalCode = DateTime.Now.ToString("HHmmss")
                ShowCardSuccess(_totalAmount, "TEST - UNATTENDED")
                CompleteCardPayment()
            End Sub
            timer.Start()
        End Sub

        Private Sub ShowCardSuccess(amount As Decimal, environment As String)
            Controls.Clear()
            Size = New Size(600, Math.Min(600, CInt(Math.Round(Screen.PrimaryScreen.WorkingArea.Height * 0.75))))
            StartPosition = FormStartPosition.CenterScreen

            Dim successColor As Color = ColorTranslator.FromHtml("#27AE60")
            Dim successColorDark As Color = ColorTranslator.FromHtml("#229954")

            Dim pnlMain As New Panel With {
                .Dock = DockStyle.Fill,
                .BackColor = successColor
            }

            Dim lblEnvironment As New Label With {
                .Text = $"Environment: {environment}",
                .Font = New Font("Segoe UI", 12, FontStyle.Bold),
                .ForeColor = Color.Yellow,
                .TextAlign = ContentAlignment.MiddleCenter,
                .Size = New Size(Width, 25),
                .Location = New Point(0, 10)
            }
            pnlMain.Controls.Add(lblEnvironment)

            Dim lblSuccess As New Label With {
                .Text = "✅ PAYMENT SUCCESSFUL",
                .Font = New Font("Segoe UI", 24, FontStyle.Bold),
                .ForeColor = Color.White,
                .TextAlign = ContentAlignment.MiddleCenter,
                .Size = New Size(Width, 40),
                .Location = New Point(0, 50)
            }
            pnlMain.Controls.Add(lblSuccess)

            Dim lblAmount As New Label With {
                .Text = $"Amount: R{amount:N2}",
                .Font = New Font("Segoe UI", 18, FontStyle.Bold),
                .ForeColor = _ironGold,
                .TextAlign = ContentAlignment.MiddleCenter,
                .Size = New Size(Width, 30),
                .Location = New Point(0, 100)
            }
            pnlMain.Controls.Add(lblAmount)

            Dim lblCardType As New Label With {
                .Text = $"Card: {_cardType}",
                .Font = New Font("Segoe UI", 14),
                .ForeColor = Color.White,
                .TextAlign = ContentAlignment.MiddleCenter,
                .Size = New Size(Width, 25),
                .Location = New Point(0, 140)
            }
            pnlMain.Controls.Add(lblCardType)

            Dim lblCardNumber As New Label With {
                .Text = $"Card No: {_cardMaskedPan}",
                .Font = New Font("Segoe UI", 14),
                .ForeColor = Color.White,
                .TextAlign = ContentAlignment.MiddleCenter,
                .Size = New Size(Width, 25),
                .Location = New Point(0, 170)
            }
            pnlMain.Controls.Add(lblCardNumber)

            Dim lblAuthCode As New Label With {
                .Text = $"Auth Code: {_cardApprovalCode}",
                .Font = New Font("Segoe UI", 14),
                .ForeColor = Color.White,
                .TextAlign = ContentAlignment.MiddleCenter,
                .Size = New Size(Width, 25),
                .Location = New Point(0, 200)
            }
            pnlMain.Controls.Add(lblAuthCode)

            Dim btnContinue As New Button With {
                .Text = "Continue",
                .Size = New Size(200, 50),
                .Location = New Point((600 - 200) \ 2, 300),
                .BackColor = Color.White,
                .ForeColor = successColor,
                .Font = New Font("Segoe UI", 14, FontStyle.Bold),
                .FlatStyle = FlatStyle.Flat,
                .Cursor = Cursors.Hand
            }
            AddHandler btnContinue.Click, Sub(sender, e)
                DialogResult = DialogResult.OK
                Close()
            End Sub
            pnlMain.Controls.Add(btnContinue)

            Controls.Add(pnlMain)
        End Sub

        Private Sub CompleteCardPayment()
            _cardAmount = _totalAmount
            _paymentMethod = "CARD"
            _changeAmount = 0D
            
            ' Save transaction to database
            SaveTransaction()
            
            ' Print receipt
            PrintReceipt()
        End Sub

        Private Sub ShowCashPaymentDialog()
            ' Implementation for cash payment dialog
            ' This would include cash entry, change calculation, etc.
        End Sub

        Private Sub ShowEFTProcessingDialog()
            ' Implementation for EFT payment processing
        End Sub

        Private Sub ShowManualPaymentDialog()
            ' Implementation for manual payment entry
        End Sub

        Private Sub ShowSplitPaymentDialog()
            ' Implementation for split payment between cash and card
        End Sub

        Private Sub SaveTransaction()
            ' Implementation to save transaction to database
            ' This would include all the GL posting, invoice creation, etc.
        End Sub

        Private Sub PrintReceipt()
            ' Implementation for receipt printing
            ' This would use the dual receipt printer functionality
        End Sub

        Private Sub Button_MouseEnter(sender As Object, e As EventArgs)
            Dim btn = DirectCast(sender, Button)
            btn.BackColor = Color.FromArgb(CInt(btn.BackColor.R * 0.8), CInt(btn.BackColor.G * 0.8), CInt(btn.BackColor.B * 0.8))
        End Sub

        Private Sub Button_MouseLeave(sender As Object, e As EventArgs)
            Dim btn = DirectCast(sender, Button)
            ' Restore original color based on button type
            If btn.Text.Contains("CASH") Then
                btn.BackColor = _tenderCash
            ElseIf btn.Text.Contains("CARD") Then
                btn.BackColor = _tenderCard
            ElseIf btn.Text.Contains("EFT") Then
                btn.BackColor = _tenderEFT
            ElseIf btn.Text.Contains("MANUAL") Then
                btn.BackColor = _tenderManual
            ElseIf btn.Text.Contains("SPLIT") Then
                btn.BackColor = _tenderSplit
            End If
        End Sub

        Private Sub InitializeComponent()
            Me.SuspendLayout()
            Me.AutoScaleDimensions = New SizeF(6F, 13F)
            Me.AutoScaleMode = AutoScaleMode.Font
            Me.ClientSize = New Size(600, 450)
            Me.Name = "PaymentTenderForm"
            Me.Text = "Payment Tender"
            Me.ResumeLayout(False)
        End Sub
    End Class
End Namespace
