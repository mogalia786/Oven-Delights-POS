Imports System.Configuration
Imports System.Data.SqlClient
Imports System.Drawing
Imports System.Windows.Forms
Imports System.IO
Imports System.Net
Imports System.Text

Public Class ReturnTenderForm
    Inherits Form

    Private _connectionString As String
    Private _returnNumber As String
    Private _returnInvoiceNumber As String
    Private _returnItems As DataTable
    Private _totalRefund As Decimal
    Private _branchID As Integer
    Private _cashierName As String
    Private _customerName As String
    Private _customerSurname As String
    Private _customerCell As String
    Private _returnReason As String
    Private _tenderMethod As String = ""
    
    ' PAYMENT TERMINAL CONFIGURATION
    Private _isLiveMode As Boolean = False ' DEFAULT TO TEST MODE
    Private _paypointApiKey As String = ""
    Private _paypointSiteId As String = ""
    Private _paypointClientSecret As String = ""
    Private _paypointClientId As String = ""
    Private _cardApprovalCode As String = ""
    Private _transactionId As String = ""
    
    ' Public properties to expose tender details
    Public ReadOnly Property TenderMethod As String
        Get
            Return _tenderMethod
        End Get
    End Property
    
    ' Iron Man Theme Color Palette
    Private _ironRed As Color = ColorTranslator.FromHtml("#C1272D")
    Private _ironRedDark As Color = ColorTranslator.FromHtml("#8B0000")
    Private _ironGold As Color = ColorTranslator.FromHtml("#FFD700")
    Private _ironDark As Color = ColorTranslator.FromHtml("#0a0e27")
    
    ' Tender button colors
    Private _tenderCash As Color = ColorTranslator.FromHtml("#27AE60")
    Private _tenderCashDark As Color = ColorTranslator.FromHtml("#1E8449")
    Private _tenderCard As Color = ColorTranslator.FromHtml("#9B59B6")
    Private _tenderCardDark As Color = ColorTranslator.FromHtml("#7D3C98")
    Private _tenderEFT As Color = ColorTranslator.FromHtml("#00D4FF")
    Private _tenderEFTDark As Color = ColorTranslator.FromHtml("#0099CC")
    
    Public Sub New(returnNumber As String, returnItems As DataTable, totalRefund As Decimal, branchID As Integer, cashierName As String, customerName As String, customerSurname As String, customerCell As String, returnReason As String)
        MyBase.New()
        _returnNumber = returnNumber
        _returnInvoiceNumber = returnNumber
        _returnItems = returnItems
        _totalRefund = totalRefund
        _branchID = branchID
        _cashierName = cashierName
        _customerName = customerName
        _customerSurname = customerSurname
        _customerCell = customerCell
        _returnReason = returnReason
        _connectionString = ConfigurationManager.ConnectionStrings("OvenDelightsERPConnectionString").ConnectionString
        
        LoadPaymentConfiguration()
        InitializeComponent()
        ShowTenderSelection()
    End Sub
    
    Private Sub LoadPaymentConfiguration()
        Try
            ' ✅ USE HARDCODED CREDENTIALS BASED ON _isLiveMode TOGGLE
            If _isLiveMode Then
                ' ✅ LIVE CREDENTIALS - REFUND/REVERSAL
                _paypointApiKey = ""
                _paypointClientId = "gmfp6rxmbrjejsd8ekc1eb4zhkdgkwi38iwx017pmdxs81giwk6i0nehmilmuj"
                _paypointClientSecret = "81giwk6i0nehmilmuj"
                _paypointSiteId = "RT08"
                Debug.WriteLine("[REFUND] Using LIVE credentials - Production environment")
            Else
                ' ✅ TEST CREDENTIALS
                _paypointApiKey = "Q7w30FOnntfiLzJuKKJrKqVqXg9BHPCq"
                _paypointClientId = "MP7BQIe0TMxgxzhpGghkNF303zhmYnjA"
                _paypointClientSecret = "Tf3ac4dLR9DGmBfwipmjy6tjUmLv6tma"
                _paypointSiteId = "UT02"
                Debug.WriteLine("[REFUND] Using TEST credentials - Sandbox environment")
            End If
            
            Debug.WriteLine($"[REFUND] Loaded {_isLiveMode.ToString().ToUpper()} mode configuration")
        Catch ex As Exception
            Debug.WriteLine($"[REFUND] Error loading config: {ex.Message}")
            _isLiveMode = False
        End Try
    End Sub
    
    Private Sub InitializeComponent()
        Me.Text = "Return Tender"
        Me.BackColor = _ironDark
        Me.FormBorderStyle = FormBorderStyle.FixedDialog
        Me.MaximizeBox = False
        Me.MinimizeBox = False
        Me.TopMost = True
        Me.WindowState = FormWindowState.Normal
        Me.ShowInTaskbar = True
        Me.ControlBox = True
        Me.Size = New Size(800, 500)
        Me.StartPosition = FormStartPosition.CenterScreen
    End Sub
    
    Private Sub ShowTenderSelection()
        Me.Controls.Clear()
        
        ' Header
        Dim pnlHeader As New Panel With {
            .Dock = DockStyle.Top,
            .Height = 120,
            .BackColor = Color.Transparent,
            .Padding = New Padding(20, 10, 20, 10)
        }
        
        ' LIVE/TEST Toggle buttons (top row, highly visible)
        Dim btnTest As New Button With {
            .Text = "🧪 TEST",
            .Size = New Size(140, 45),
            .Location = New Point(20, 10),
            .BackColor = If(Not _isLiveMode, Color.FromArgb(0, 200, 0), Color.FromArgb(80, 80, 80)),
            .ForeColor = Color.White,
            .Font = New Font("Segoe UI", 12, FontStyle.Bold),
            .FlatStyle = FlatStyle.Flat,
            .Cursor = Cursors.Hand
        }
        btnTest.FlatAppearance.BorderSize = 0
        
        Dim btnLive As New Button With {
            .Text = "🔴 LIVE",
            .Size = New Size(140, 45),
            .Location = New Point(170, 10),
            .BackColor = If(_isLiveMode, Color.FromArgb(220, 20, 20), Color.FromArgb(80, 80, 80)),
            .ForeColor = Color.White,
            .Font = New Font("Segoe UI", 12, FontStyle.Bold),
            .FlatStyle = FlatStyle.Flat,
            .Cursor = Cursors.Hand
        }
        btnLive.FlatAppearance.BorderSize = 0
        
        Dim lblTitle As New Label With {
            .Text = "� SELECT REFUND METHOD",
            .Font = New Font("Segoe UI", 22, FontStyle.Bold),
            .ForeColor = _ironGold,
            .AutoSize = False,
            .Width = 500,
            .Height = 35,
            .TextAlign = ContentAlignment.MiddleLeft
        }
        lblTitle.Location = New Point(330, 15)
        
        Dim lblAmount As New Label With {
            .Text = $"Refund Amount: R {_totalRefund:N2}",
            .Font = New Font("Segoe UI", 18, FontStyle.Bold),
            .ForeColor = _ironRed,
            .AutoSize = False,
            .Width = 760,
            .Height = 35,
            .TextAlign = ContentAlignment.MiddleCenter
        }
        lblAmount.Location = New Point(20, 70)
        
        AddHandler btnTest.Click, Sub()
            _isLiveMode = False
            btnTest.BackColor = Color.FromArgb(0, 200, 0)
            btnLive.BackColor = Color.FromArgb(80, 80, 80)
            LoadPaymentConfiguration()
        End Sub
        
        AddHandler btnLive.Click, Sub()
            _isLiveMode = True
            btnLive.BackColor = Color.FromArgb(220, 20, 20)
            btnTest.BackColor = Color.FromArgb(80, 80, 80)
            LoadPaymentConfiguration()
        End Sub
        
        pnlHeader.Controls.AddRange({lblTitle, lblAmount, btnTest, btnLive})
        
        ' Tender buttons panel
        Dim pnlButtons As New Panel With {
            .Location = New Point(100, 120),
            .Size = New Size(600, 250),
            .BackColor = Color.Transparent
        }
        
        Dim buttonWidth As Integer = 180
        Dim buttonHeight As Integer = 250
        Dim gap As Integer = 30
        
        ' Helper function to create tender button
        Dim CreateTenderButton = Function(text As String, icon As String, bgColor As Color, bgColorDark As Color, xPos As Integer, clickHandler As Action) As Button
            Dim btn As New Button With {
                .Size = New Size(buttonWidth, buttonHeight),
                .Location = New Point(xPos, 0),
                .BackColor = bgColor,
                .ForeColor = Color.White,
                .FlatStyle = FlatStyle.Flat,
                .Cursor = Cursors.Hand,
                .Font = New Font("Segoe UI", 20, FontStyle.Bold)
            }
            btn.FlatAppearance.BorderSize = 2
            btn.FlatAppearance.BorderColor = Color.White
            
            ' Icon
            Dim lblIcon As New Label With {
                .Text = icon,
                .Font = New Font("Segoe UI", 60),
                .ForeColor = Color.White,
                .BackColor = Color.Transparent,
                .AutoSize = True,
                .TextAlign = ContentAlignment.MiddleCenter
            }
            lblIcon.Location = New Point((buttonWidth - lblIcon.PreferredWidth) \ 2, 40)
            btn.Controls.Add(lblIcon)
            
            ' Text
            Dim lblText As New Label With {
                .Text = text,
                .Font = New Font("Segoe UI", 18, FontStyle.Bold),
                .ForeColor = Color.White,
                .BackColor = Color.Transparent,
                .AutoSize = True,
                .TextAlign = ContentAlignment.MiddleCenter
            }
            lblText.Location = New Point((buttonWidth - lblText.PreferredWidth) \ 2, 160)
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
        
        ' Create tender buttons
        Dim btnCash = CreateTenderButton("CASH", "💵", _tenderCash, _tenderCashDark, 0, Sub() ProcessCashRefund())
        Dim btnCard = CreateTenderButton("CARD", "💳", _tenderCard, _tenderCardDark, buttonWidth + gap, Sub() ProcessCardRefund())
        Dim btnEFT = CreateTenderButton("EFT", "🏦", _tenderEFT, _tenderEFTDark, (buttonWidth + gap) * 2, Sub() ProcessEFTRefund())
        
        pnlButtons.Controls.AddRange({btnCash, btnCard, btnEFT})
        
        ' Cancel button
        Dim pnlBottom As New Panel With {.Dock = DockStyle.Bottom, .Height = 70, .BackColor = Color.Transparent, .Padding = New Padding(100, 5, 100, 5)}
        Dim btnCancel As New Button With {
            .Text = "✖ CANCEL",
            .Dock = DockStyle.Fill,
            .BackColor = _ironRed,
            .ForeColor = Color.White,
            .Font = New Font("Segoe UI", 18, FontStyle.Bold),
            .FlatStyle = FlatStyle.Flat,
            .Cursor = Cursors.Hand
        }
        btnCancel.FlatAppearance.BorderSize = 2
        btnCancel.FlatAppearance.BorderColor = Color.White
        AddHandler btnCancel.Click, Sub() Me.DialogResult = DialogResult.Cancel
        AddHandler btnCancel.MouseEnter, Sub()
            btnCancel.BackColor = _ironRedDark
            btnCancel.FlatAppearance.BorderColor = _ironGold
            btnCancel.FlatAppearance.BorderSize = 4
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
    End Sub
    
    Private Sub ProcessCashRefund()
        _tenderMethod = "CASH"
        
        ' Show confirmation
        Dim result = MessageBox.Show($"Confirm CASH refund of R {_totalRefund:N2}?{vbCrLf}{vbCrLf}Open cash drawer and count cash to customer.", "Cash Refund", MessageBoxButtons.YesNo, MessageBoxIcon.Question)
        
        If result = DialogResult.Yes Then
            Try
                ' Update Cash on Hand ledger
                Using conn As New SqlConnection(_connectionString)
                    conn.Open()
                    Using transaction = conn.BeginTransaction()
                        Try
                            ' CREDIT Cash (reducing cash on hand for refund)
                            Dim cashLedgerID = GetLedgerID(conn, transaction, "Cash")
                            Dim refNumber = $"REF-{DateTime.Now:yyyyMMddHHmmss}"
                            InsertJournalEntry(conn, transaction, DateTime.Now, "Refund Journal", refNumber, cashLedgerID, 0, _totalRefund, $"Cash refund {_returnInvoiceNumber}")
                            
                            ' DEBIT Sales Returns
                            Dim salesReturnsLedgerID = GetLedgerID(conn, transaction, "Sales Returns")
                            InsertJournalEntry(conn, transaction, DateTime.Now, "Refund Journal", refNumber, salesReturnsLedgerID, _totalRefund, 0, $"Sales return {_returnInvoiceNumber}")
                            
                            transaction.Commit()
                        Catch ex As Exception
                            transaction.Rollback()
                            Throw New Exception($"Ledger update failed: {ex.Message}", ex)
                        End Try
                    End Using
                End Using
                
                ' Open cash drawer
                OpenCashDrawer()
                
                ' Show message while printing
                MessageBox.Show("Printing customer and cashier receipts...", "Printing", MessageBoxButtons.OK, MessageBoxIcon.Information)
                
                ' Print duplicate receipts automatically (Customer + Cashier copy)
                PrintDuplicateReceipts()
                
                ' Confirm completion
                MessageBox.Show("Cash refund complete. Receipts printed.", "Complete", MessageBoxButtons.OK, MessageBoxIcon.Information)
                
                ' Close tender form - return to summary
                Me.DialogResult = DialogResult.OK
                Me.Close()
            Catch ex As Exception
                MessageBox.Show($"Error processing cash refund: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
            End Try
        End If
    End Sub
    
    Private Sub ProcessCardRefund()
        _tenderMethod = "CARD"
        
        Try
            ' Show processing dialog
            Dim processingResult As DialogResult = MessageBox.Show(
                "Processing card refund through terminal..." & vbCrLf & vbCrLf &
                "Amount: R" & _totalRefund.ToString("N2") & vbCrLf &
                "Please wait...",
                "Card Refund Processing",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information)

            If processingResult <> DialogResult.OK Then
                Return
            End If

            Debug.WriteLine("[REFUND] Processing card refund through terminal")
            Debug.WriteLine($"[REFUND] CURRENT MODE: {_isLiveMode} (True=LIVE, False=TEST)")
            Debug.WriteLine($"[REFUND] Amount: R{_totalRefund:N2}")

            ' Build refund request
            Dim productItems As New List(Of Object)
            Dim itemId As Integer = 1

            For Each row As DataRow In _returnItems.Rows
                productItems.Add(New With {
                    .itemId = itemId,
                    .category = 255,
                    .amount = CInt(CDec(row("LineTotal")) * 100),
                    .description = row("ProductName").ToString().Substring(0, Math.Min(20, row("ProductName").ToString().Length)),
                    .quantity = CInt(row("Quantity")),
                    .unitPrice = CInt(CDec(row("UnitPrice")) * 100)
                })
                itemId += 1
            Next

            Dim refundRequest As Object
            If _isLiveMode Then
                refundRequest = New With {
                    .requestType = "Refund",
                    .reconIndicator = Now.ToString("HHmmss").Substring(0, Math.Min(7, Now.ToString("HHmmss").Length)),
                    .supervisor = New String() {"R"}, ' Refund supervisor code
                    .posIdentifier = 1,
                    .posVersion = "1.0.0",
                    .siteId = "RT08",
                    .totalAmount = _totalRefund, ' Amount in decimal format (e.g., 185.00)
                    .productItems = productItems
                }
            Else
                refundRequest = New With {
                    .requestType = "Refund",
                    .reconIndicator = Now.ToString("HHmmss").Substring(0, Math.Min(7, Now.ToString("HHmmss").Length)),
                    .supervisor = New String() {}, ' Test allows unattended
                    .posIdentifier = 10,
                    .posVersion = "1.0.0",
                    .siteId = "UT02",
                    .totalAmount = _totalRefund, ' Amount in decimal format (e.g., 185.00)
                    .productItems = productItems
                }
            End If

            Dim jsonRequest As String = SimpleJsonSerialize(refundRequest)
            Dim apiUrl As String = If(_isLiveMode, "https://miniposfnb.co.za:49410/api", "https://test.figment.co.za:49410/api")

            Debug.WriteLine($"[REFUND] {_isLiveMode.ToString().ToUpper()} MODE - Sending to: {apiUrl}")
            Debug.WriteLine($"[REFUND] JSON: {jsonRequest}")

            Dim request As HttpWebRequest = WebRequest.Create(apiUrl)
            request.Method = "POST"
            request.ContentType = "application/json"
            request.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) Overn-Delights-POS/1.0.0.37"
            request.Timeout = 30000

            ' Handle authentication
            Dim accessToken As String = ""
            If _isLiveMode Then
                ' OAuth2 for LIVE
                Dim tokenUrl As String = "https://miniposfnb.co.za:49410/oauth2/token"
                Dim tokenRequest As HttpWebRequest = WebRequest.Create(tokenUrl)
                tokenRequest.Method = "POST"
                tokenRequest.ContentType = "application/x-www-form-urlencoded"
                tokenRequest.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) Overn-Delights-POS/1.0.0.37"
                tokenRequest.Timeout = 30000

                Dim tokenData = $"grant_type=client_credentials&client_id={_paypointClientId}&client_secret={_paypointClientSecret}"
                Dim tokenBytes = Encoding.UTF8.GetBytes(tokenData)
                tokenRequest.ContentLength = tokenBytes.Length

                Using tokenStream = tokenRequest.GetRequestStream()
                    tokenStream.Write(tokenBytes, 0, tokenBytes.Length)
                End Using

                Using tokenResponse = tokenRequest.GetResponse()
                    Using tokenStream = tokenResponse.GetResponseStream()
                        Using tokenReader = New StreamReader(tokenStream)
                            Dim tokenJson = tokenReader.ReadToEnd()
                            accessToken = ExtractJsonValue(tokenJson, "access_token")
                        End Using
                    End Using
                End Using
            Else
                accessToken = _paypointApiKey
            End If

            ' Send refund request
            Dim dataBytes = Encoding.UTF8.GetBytes(jsonRequest)
            request.ContentLength = dataBytes.Length

            If _isLiveMode Then
                request.Headers.Add("Authorization", "Bearer " & accessToken)
            Else
                request.Headers.Add("X-API-Key", accessToken)
            End If

            Using requestStream = request.GetRequestStream()
                requestStream.Write(dataBytes, 0, dataBytes.Length)
            End Using

            Using response = request.GetResponse()
                Using responseStream = response.GetResponseStream()
                    Using reader = New StreamReader(responseStream)
                        Dim responseJson = reader.ReadToEnd()
                        Debug.WriteLine($"[REFUND] API Response: {responseJson}")

                        Dim isSuccess As Boolean = responseJson.Contains("""success"":true") OrElse responseJson.Contains("""success"": True")

                        If isSuccess Then
                            _cardApprovalCode = ExtractJsonValue(responseJson, "approvalCode")
                            _transactionId = ExtractJsonValue(responseJson, "transactionId")

                            MessageBox.Show(
                                "✅ REFUND APPROVED" & vbCrLf & vbCrLf &
                                "Amount: R" & _totalRefund.ToString("N2") & vbCrLf &
                                "Auth Code: " & _cardApprovalCode & vbCrLf &
                                "Transaction ID: " & _transactionId,
                                "Refund Successful",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Information)

                            CompleteRefund()
                        Else
                            Dim errorMessage As String = "Refund declined"
                            If responseJson.Contains("""error"":") Then
                                errorMessage = ExtractJsonValue(responseJson, "error")
                            End If

                            MessageBox.Show(
                                "❌ REFUND FAILED" & vbCrLf & vbCrLf &
                                "Error: " & errorMessage,
                                "Refund Error",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Error)
                        End If
                    End Using
                End Using
            End Using

        Catch ex As Exception
            Debug.WriteLine($"[REFUND] Error: {ex.Message}")
            MessageBox.Show(
                "Refund processing error: " & ex.Message & vbCrLf & vbCrLf &
                "Please process refund manually on terminal.",
                "System Error",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error)
        End Try
    End Sub
    
    Private Function SimpleJsonSerialize(obj As Object) As String
        Dim json As New StringBuilder()
        json.Append("{")
        
        Dim properties = obj.GetType().GetProperties()
        For i As Integer = 0 To properties.Length - 1
            Dim prop = properties(i)
            If prop.GetIndexParameters().Length > 0 Then Continue For
            
            If i > 0 Then json.Append(",")
            
            Dim value = prop.GetValue(obj, Nothing)
            json.Append($"""{prop.Name}"":")
            
            If TypeOf value Is String Then
                json.Append($"""{value}""")
            ElseIf TypeOf value Is Decimal Then
                json.Append(CDec(value).ToString("F2"))
            ElseIf TypeOf value Is Integer Then
                json.Append(CInt(value).ToString())
            ElseIf TypeOf value Is Boolean Then
                json.Append(CBool(value).ToString().ToLower())
            ElseIf TypeOf value Is String() Then
                json.Append(SimpleJsonSerializeArray(value))
            ElseIf TypeOf value Is IEnumerable Then
                json.Append(SimpleJsonSerializeArray(value))
            Else
                json.Append($"""{value}""")
            End If
        Next
        
        json.Append("}")
        Return json.ToString()
    End Function
    
    Private Function SimpleJsonSerializeArray(arr As IEnumerable) As String
        Dim json As New StringBuilder()
        json.Append("[")
        
        Dim first As Boolean = True
        For Each item In arr
            If Not first Then json.Append(",")
            first = False
            
            If TypeOf item Is String Then
                json.Append($"""{item}""")
            ElseIf item.GetType().IsClass AndAlso Not TypeOf item Is String Then
                json.Append(SimpleJsonSerialize(item))
            Else
                json.Append(item.ToString())
            End If
        Next
        
        json.Append("]")
        Return json.ToString()
    End Function
    
    Private Function ExtractJsonValue(json As String, key As String) As String
        Try
            Dim pattern As String = $"""{key}""\s*:\s*""([^""]*)"""
            Dim match = System.Text.RegularExpressions.Regex.Match(json, pattern)
            If match.Success Then
                Return match.Groups(1).Value
            End If
        Catch
        End Try
        Return ""
    End Function
    
    Private Sub ProcessEFTRefund()
        _tenderMethod = "EFT"
        
        ' Show EFT refund instructions
        MessageBox.Show($"Process EFT refund of R {_totalRefund:N2}{vbCrLf}{vbCrLf}Customer will receive refund via EFT within 3-5 business days.{vbCrLf}{vbCrLf}Ensure customer details are correct.", "EFT Refund", MessageBoxButtons.OK, MessageBoxIcon.Information)
        
        ' Show receipt and complete
        CompleteRefund()
    End Sub
    
    Private Sub CompleteRefund()
        ' Show return receipt (for Card/EFT - allows manual print)
        Using receiptForm As New ReturnReceiptForm(_returnNumber, _returnItems, _totalRefund, _branchID, _cashierName, _customerName, _customerSurname, _customerCell, _returnReason)
            receiptForm.ShowDialog()
        End Using
        
        Me.DialogResult = DialogResult.OK
        Me.Close()
    End Sub
    
    Private Sub PrintDuplicateReceipts()
        Try
            Dim printDoc As New Printing.PrintDocument()
            printDoc.DefaultPageSettings.PaperSize = New Printing.PaperSize("80mm", 315, 1200)
            
            ' Print handler
            Dim printHandler As Printing.PrintPageEventHandler = Sub(sender, e)
                Dim fontBold As New Font("Courier New", 8, FontStyle.Bold)
                Dim fontLarge As New Font("Courier New", 10, FontStyle.Bold)
                Dim leftMargin As Integer = 10
                Dim yPos As Integer = 10
                
                ' Header
                e.Graphics.DrawString("OVEN DELIGHTS", fontLarge, Brushes.Black, leftMargin, yPos)
                yPos += 18
                e.Graphics.DrawString("RETURN RECEIPT", fontLarge, Brushes.Black, leftMargin, yPos)
                yPos += 18
                e.Graphics.DrawString("======================================", fontBold, Brushes.Black, leftMargin, yPos)
                yPos += 15
                
                ' Return details
                e.Graphics.DrawString($"Return #: {_returnNumber}", fontBold, Brushes.Black, leftMargin, yPos)
                yPos += 14
                e.Graphics.DrawString($"Date: {DateTime.Now:dd/MM/yyyy HH:mm}", fontBold, Brushes.Black, leftMargin, yPos)
                yPos += 14
                e.Graphics.DrawString($"Cashier: {_cashierName}", fontBold, Brushes.Black, leftMargin, yPos)
                yPos += 14
                e.Graphics.DrawString($"Tender: CASH", fontBold, Brushes.Black, leftMargin, yPos)
                yPos += 14
                e.Graphics.DrawString("======================================", fontBold, Brushes.Black, leftMargin, yPos)
                yPos += 15
                
                ' Customer Details
                If Not String.IsNullOrWhiteSpace(_customerName) Then
                    e.Graphics.DrawString("CUSTOMER DETAILS:", fontBold, Brushes.Black, leftMargin, yPos)
                    yPos += 14
                    e.Graphics.DrawString($"Name: {_customerName} {_customerSurname}", fontBold, Brushes.Black, leftMargin, yPos)
                    yPos += 14
                    e.Graphics.DrawString($"Cell: {_customerCell}", fontBold, Brushes.Black, leftMargin, yPos)
                    yPos += 14
                    e.Graphics.DrawString("======================================", fontBold, Brushes.Black, leftMargin, yPos)
                    yPos += 15
                End If
                
                ' Return Reason
                If Not String.IsNullOrWhiteSpace(_returnReason) Then
                    e.Graphics.DrawString("REASON FOR RETURN:", fontBold, Brushes.Black, leftMargin, yPos)
                    yPos += 14
                    e.Graphics.DrawString(_returnReason, fontBold, Brushes.Black, leftMargin, yPos)
                    yPos += 14
                    e.Graphics.DrawString("======================================", fontBold, Brushes.Black, leftMargin, yPos)
                    yPos += 15
                End If
                
                ' Items
                e.Graphics.DrawString("RETURNED ITEMS:", fontBold, Brushes.Black, leftMargin, yPos)
                yPos += 14
                For Each row As DataRow In _returnItems.Rows
                    Dim itemName As String = row("ProductName").ToString()
                    Dim qty As Decimal = CDec(row("Quantity"))
                    Dim price As Decimal = CDec(row("UnitPrice"))
                    Dim total As Decimal = CDec(row("LineTotal"))
                    
                    e.Graphics.DrawString($"{qty:0.00} x {itemName}", fontBold, Brushes.Black, leftMargin, yPos)
                    yPos += 14
                    e.Graphics.DrawString($"    @ R{price:N2} = R{total:N2}", fontBold, Brushes.Black, leftMargin, yPos)
                    yPos += 14
                Next
                
                yPos += 5
                e.Graphics.DrawString("======================================", fontBold, Brushes.Black, leftMargin, yPos)
                yPos += 15
                
                ' Total
                e.Graphics.DrawString($"TOTAL REFUND:         R {_totalRefund:N2}", fontLarge, Brushes.Black, leftMargin, yPos)
                yPos += 20
                
                e.Graphics.DrawString("======================================", fontBold, Brushes.Black, leftMargin, yPos)
                yPos += 15
                e.Graphics.DrawString("Thank you", fontBold, Brushes.Black, leftMargin, yPos)
            End Sub
            
            ' Print CUSTOMER COPY
            AddHandler printDoc.PrintPage, printHandler
            printDoc.Print()
            
            ' Wait briefly between prints
            System.Threading.Thread.Sleep(500)
            
            ' Print CASHIER COPY
            Dim printDoc2 As New Printing.PrintDocument()
            printDoc2.DefaultPageSettings.PaperSize = New Printing.PaperSize("80mm", 315, 1200)
            AddHandler printDoc2.PrintPage, printHandler
            printDoc2.Print()
            
        Catch ex As Exception
            MessageBox.Show($"Print error: {ex.Message}", "Print Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub
    
    Private Sub OpenCashDrawer()
        Try
            ' ESC/POS command to open cash drawer
            ' Drawer Kick: ESC p m t1 t2
            ' ESC = Chr(27), p = Chr(112), m = Chr(0), t1 = Chr(50), t2 = Chr(250)
            Dim drawerCommand As String = Chr(27) & Chr(112) & Chr(0) & Chr(50) & Chr(250)
            
            ' Send to default printer
            Dim printDoc As New Printing.PrintDocument()
            Dim printerName = printDoc.PrinterSettings.PrinterName
            
            ' Create raw printer helper
            Dim rawPrinterHelper As New RawPrinterHelper()
            rawPrinterHelper.SendStringToPrinter(printerName, drawerCommand)
            
        Catch ex As Exception
            ' Silently fail - drawer may not be connected
            Debug.WriteLine($"Cash drawer error: {ex.Message}")
        End Try
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
        Dim sql = "INSERT INTO GeneralJournal (TransactionDate, JournalType, Reference, LedgerID, Debit, Credit, BranchID, CreatedBy, CreatedDate) " &
                  "VALUES (@Date, @Type, @Ref, @LedgerID, @Debit, @Credit, @BranchID, @CreatedBy, GETDATE())"
        
        Using cmd As New SqlCommand(sql, conn, transaction)
            cmd.Parameters.AddWithValue("@Date", journalDate)
            cmd.Parameters.AddWithValue("@Type", journalType)
            cmd.Parameters.AddWithValue("@Ref", reference)
            cmd.Parameters.AddWithValue("@LedgerID", ledgerID)
            cmd.Parameters.AddWithValue("@Debit", debit)
            cmd.Parameters.AddWithValue("@Credit", credit)
            cmd.Parameters.AddWithValue("@BranchID", _branchID)
            cmd.Parameters.AddWithValue("@CreatedBy", _cashierName)
            cmd.ExecuteNonQuery()
        End Using
    End Sub
End Class

' Helper class for raw printer commands
Public Class RawPrinterHelper
    <Runtime.InteropServices.DllImport("winspool.drv", CharSet:=Runtime.InteropServices.CharSet.Auto, SetLastError:=True)>
    Private Shared Function OpenPrinter(pPrinterName As String, ByRef phPrinter As IntPtr, pDefault As IntPtr) As Boolean
    End Function

    <Runtime.InteropServices.DllImport("winspool.drv", SetLastError:=True)>
    Private Shared Function ClosePrinter(hPrinter As IntPtr) As Boolean
    End Function

    <Runtime.InteropServices.DllImport("winspool.drv", SetLastError:=True)>
    Private Shared Function StartDocPrinter(hPrinter As IntPtr, level As Integer, ByRef pDocInfo As DOCINFO) As Boolean
    End Function

    <Runtime.InteropServices.DllImport("winspool.drv", SetLastError:=True)>
    Private Shared Function EndDocPrinter(hPrinter As IntPtr) As Boolean
    End Function

    <Runtime.InteropServices.DllImport("winspool.drv", SetLastError:=True)>
    Private Shared Function StartPagePrinter(hPrinter As IntPtr) As Boolean
    End Function

    <Runtime.InteropServices.DllImport("winspool.drv", SetLastError:=True)>
    Private Shared Function EndPagePrinter(hPrinter As IntPtr) As Boolean
    End Function

    <Runtime.InteropServices.DllImport("winspool.drv", SetLastError:=True)>
    Private Shared Function WritePrinter(hPrinter As IntPtr, pBytes As IntPtr, dwCount As Integer, ByRef dwWritten As Integer) As Boolean
    End Function

    <Runtime.InteropServices.StructLayout(Runtime.InteropServices.LayoutKind.Sequential, CharSet:=Runtime.InteropServices.CharSet.Auto)>
    Private Structure DOCINFO
        <Runtime.InteropServices.MarshalAs(Runtime.InteropServices.UnmanagedType.LPWStr)>
        Public pDocName As String
        <Runtime.InteropServices.MarshalAs(Runtime.InteropServices.UnmanagedType.LPWStr)>
        Public pOutputFile As String
        <Runtime.InteropServices.MarshalAs(Runtime.InteropServices.UnmanagedType.LPWStr)>
        Public pDataType As String
    End Structure

    Public Function SendStringToPrinter(printerName As String, data As String) As Boolean
        Dim hPrinter As IntPtr = IntPtr.Zero
        Dim di As New DOCINFO()
        Dim dwWritten As Integer = 0
        Dim success As Boolean = False

        di.pDocName = "Cash Drawer Command"
        di.pDataType = "RAW"

        If OpenPrinter(printerName, hPrinter, IntPtr.Zero) Then
            If StartDocPrinter(hPrinter, 1, di) Then
                If StartPagePrinter(hPrinter) Then
                    Dim bytes = System.Text.Encoding.Default.GetBytes(data)
                    Dim pBytes = Runtime.InteropServices.Marshal.AllocCoTaskMem(bytes.Length)
                    Runtime.InteropServices.Marshal.Copy(bytes, 0, pBytes, bytes.Length)
                    success = WritePrinter(hPrinter, pBytes, bytes.Length, dwWritten)
                    Runtime.InteropServices.Marshal.FreeCoTaskMem(pBytes)
                    EndPagePrinter(hPrinter)
                End If
                EndDocPrinter(hPrinter)
            End If
            ClosePrinter(hPrinter)
        End If

        Return success
    End Function
End Class
