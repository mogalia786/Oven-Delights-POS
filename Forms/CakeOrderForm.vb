Imports System.Windows.Forms
Imports System.Drawing
Imports System.Data.SqlClient
Imports System.Configuration

Public Class CakeOrderForm
    Inherits Form

    Private _branchID As Integer
    Private _tillPointID As Integer
    Private _cashierID As Integer
    Private _cashierName As String
    Private _connectionString As String
    
    Private _questions As New DataTable()
    Private _selectedAnswers As New Dictionary(Of Integer, SelectedAnswer)
    Private _selectedAccessories As New List(Of SelectedItem)
    Private _selectedToppings As New List(Of SelectedItem)
    
    Private _totalAmount As Decimal = 0
    Private _depositAmount As Decimal = 0
    Private _balanceAmount As Decimal = 0
    
    Private txtCustomerName As TextBox
    Private txtCustomerPhone As TextBox
    Private txtCustomerEmail As TextBox
    Private txtCustomerAddress As TextBox
    Private dtpPickupDate As DateTimePicker
    Private txtPickupTime As TextBox
    Private flpQuestions As FlowLayoutPanel
    Private lblTotal As Label
    Private txtDeposit As TextBox
    Private lblBalance As Label
    Private _keyboard As OrderEntryKeyboard
    Private _numpad As OrderEntryNumpad
    Private _activeTextBox As TextBox
    Private cboSpecialRequests As ComboBox
    Private txtSpecialRequests As TextBox
    Private btnAddRequest As Button
    
    Private _darkBlue As Color = ColorTranslator.FromHtml("#2C3E50")
    Private _lightBlue As Color = ColorTranslator.FromHtml("#3498DB")
    Private _green As Color = ColorTranslator.FromHtml("#27AE60")
    Private _orange As Color = ColorTranslator.FromHtml("#E67E22")
    Private _red As Color = ColorTranslator.FromHtml("#E74C3C")
    Private _lightGray As Color = ColorTranslator.FromHtml("#ECF0F1")
    Private _darkGray As Color = ColorTranslator.FromHtml("#7F8C8D")
    
    Public Class SelectedAnswer
        Public Property QuestionID As Integer
        Public Property QuestionText As String
        Public Property AnswerText As String
        Public Property AnswerPrice As Decimal
    End Class
    
    Public Class SelectedItem
        Public Property ItemID As Integer
        Public Property ItemName As String
        Public Property Quantity As Integer
        Public Property UnitPrice As Decimal
        Public Property TotalPrice As Decimal
    End Class
    
    Public Sub New(branchID As Integer, tillPointID As Integer, cashierID As Integer, cashierName As String)
        _branchID = branchID
        _tillPointID = tillPointID
        _cashierID = cashierID
        _cashierName = cashierName
        _connectionString = ConfigurationManager.ConnectionStrings("OvenDelightsERPConnectionString").ConnectionString
        
        InitializeComponent()
        LoadQuestions()
    End Sub
    
    Private Sub InitializeComponent()
        Me.Text = "🎂 Cake Order"
        Me.Size = New Size(1200, 900)
        Me.StartPosition = FormStartPosition.CenterScreen
        Me.BackColor = Color.White
        Me.FormBorderStyle = FormBorderStyle.FixedDialog
        Me.MaximizeBox = False
        
        ' Header
        Dim pnlHeader As New Panel With {
            .Dock = DockStyle.Top,
            .Height = 80,
            .BackColor = _orange
        }
        
        Dim lblHeader As New Label With {
            .Text = "🎂 CUSTOM CAKE ORDER",
            .Font = New Font("Segoe UI", 24, FontStyle.Bold),
            .ForeColor = Color.White,
            .TextAlign = ContentAlignment.MiddleCenter,
            .Dock = DockStyle.Fill
        }
        pnlHeader.Controls.Add(lblHeader)
        
        ' Main scroll panel
        Dim pnlMain As New Panel With {
            .Dock = DockStyle.Fill,
            .AutoScroll = True,
            .Padding = New Padding(20)
        }
        
        Dim yPos As Integer = 10
        
        ' Customer Details Section
        Dim lblCustomerSection As New Label With {
            .Text = "CUSTOMER DETAILS",
            .Font = New Font("Segoe UI", 14, FontStyle.Bold),
            .Location = New Point(20, yPos),
            .AutoSize = True
        }
        pnlMain.Controls.Add(lblCustomerSection)
        yPos += 40
        
        ' Customer Name
        Dim lblName As New Label With {
            .Text = "Customer Name *",
            .Font = New Font("Segoe UI", 11),
            .Location = New Point(20, yPos),
            .AutoSize = True
        }
        txtCustomerName = New TextBox With {
            .Font = New Font("Segoe UI", 12),
            .Location = New Point(200, yPos - 3),
            .Width = 400
        }
        pnlMain.Controls.AddRange({lblName, txtCustomerName})
        yPos += 40
        
        ' Customer Phone
        Dim lblPhone As New Label With {
            .Text = "Phone Number *",
            .Font = New Font("Segoe UI", 11),
            .Location = New Point(20, yPos),
            .AutoSize = True
        }
        txtCustomerPhone = New TextBox With {
            .Font = New Font("Segoe UI", 12),
            .Location = New Point(200, yPos - 3),
            .Width = 400
        }
        pnlMain.Controls.AddRange({lblPhone, txtCustomerPhone})
        yPos += 40
        
        ' Customer Email
        Dim lblEmail As New Label With {
            .Text = "Email",
            .Font = New Font("Segoe UI", 11),
            .Location = New Point(20, yPos),
            .AutoSize = True
        }
        txtCustomerEmail = New TextBox With {
            .Font = New Font("Segoe UI", 12),
            .Location = New Point(200, yPos - 3),
            .Width = 400
        }
        pnlMain.Controls.AddRange({lblEmail, txtCustomerEmail})
        yPos += 40
        
        ' Customer Address
        Dim lblAddress As New Label With {
            .Text = "Address",
            .Font = New Font("Segoe UI", 11),
            .Location = New Point(20, yPos),
            .AutoSize = True
        }
        txtCustomerAddress = New TextBox With {
            .Font = New Font("Segoe UI", 12),
            .Location = New Point(200, yPos - 3),
            .Width = 400,
            .Multiline = True,
            .Height = 60
        }
        pnlMain.Controls.AddRange({lblAddress, txtCustomerAddress})
        yPos += 80
        
        ' Keyboard/Numpad buttons
        Dim btnShowKeyboard As New Button With {
            .Text = "⌨️ Show Keyboard",
            .Font = New Font("Segoe UI", 11, FontStyle.Bold),
            .Size = New Size(180, 40),
            .Location = New Point(200, yPos),
            .BackColor = _green,
            .ForeColor = Color.White,
            .FlatStyle = FlatStyle.Flat,
            .Cursor = Cursors.Hand
        }
        btnShowKeyboard.FlatAppearance.BorderSize = 0
        AddHandler btnShowKeyboard.Click, Sub()
            If _numpad IsNot Nothing Then _numpad.HideNumpad()
            If _keyboard IsNot Nothing Then _keyboard.ShowKeyboard()
        End Sub
        
        Dim btnShowNumpad As New Button With {
            .Text = "🔢 Show Numpad",
            .Font = New Font("Segoe UI", 11, FontStyle.Bold),
            .Size = New Size(180, 40),
            .Location = New Point(400, yPos),
            .BackColor = _orange,
            .ForeColor = Color.White,
            .FlatStyle = FlatStyle.Flat,
            .Cursor = Cursors.Hand
        }
        btnShowNumpad.FlatAppearance.BorderSize = 0
        AddHandler btnShowNumpad.Click, Sub()
            If _keyboard IsNot Nothing Then _keyboard.HideKeyboard()
            If _numpad IsNot Nothing Then _numpad.ShowNumpad()
        End Sub
        
        pnlMain.Controls.AddRange({btnShowKeyboard, btnShowNumpad})
        yPos += 60
        
        ' Pickup Date & Time
        Dim lblPickup As New Label With {
            .Text = "Pickup Date *",
            .Font = New Font("Segoe UI", 11),
            .Location = New Point(20, yPos),
            .AutoSize = True
        }
        dtpPickupDate = New DateTimePicker With {
            .Font = New Font("Segoe UI", 12),
            .Location = New Point(200, yPos - 3),
            .Width = 200,
            .Format = DateTimePickerFormat.Short,
            .MinDate = DateTime.Today
        }
        
        Dim lblTime As New Label With {
            .Text = "Time *",
            .Font = New Font("Segoe UI", 11),
            .Location = New Point(420, yPos),
            .AutoSize = True
        }
        txtPickupTime = New TextBox With {
            .Font = New Font("Segoe UI", 12),
            .Location = New Point(480, yPos - 3),
            .Width = 120,
            .Text = "12:00"
        }
        pnlMain.Controls.AddRange({lblPickup, dtpPickupDate, lblTime, txtPickupTime})
        yPos += 60
        
        ' Cake Specifications Section
        Dim lblCakeSection As New Label With {
            .Text = "CAKE SPECIFICATIONS",
            .Font = New Font("Segoe UI", 14, FontStyle.Bold),
            .Location = New Point(20, yPos),
            .AutoSize = True
        }
        pnlMain.Controls.Add(lblCakeSection)
        yPos += 40
        
        ' Questions FlowLayoutPanel
        flpQuestions = New FlowLayoutPanel With {
            .Location = New Point(20, yPos),
            .Size = New Size(1120, 300),
            .FlowDirection = FlowDirection.TopDown,
            .WrapContents = False,
            .AutoScroll = True,
            .BorderStyle = BorderStyle.FixedSingle,
            .BackColor = _lightGray
        }
        pnlMain.Controls.Add(flpQuestions)
        yPos += 320
        
        ' Special Requests Section
        Dim lblSpecialSection As New Label With {
            .Text = "SPECIAL REQUESTS / CAKE OPTIONS",
            .Font = New Font("Segoe UI", 12, FontStyle.Bold),
            .Location = New Point(20, yPos),
            .AutoSize = True,
            .ForeColor = _orange
        }
        pnlMain.Controls.Add(lblSpecialSection)
        yPos += 35
        
        ' ComboBox with predefined cake options (editable)
        cboSpecialRequests = New ComboBox With {
            .Font = New Font("Segoe UI", 11),
            .Location = New Point(20, yPos),
            .Width = 400,
            .DropDownStyle = ComboBoxStyle.DropDown
        }
        
        ' Add cake options to ComboBox
        cboSpecialRequests.Items.AddRange(New String() {
            "Double vanilla",
            "Double choc",
            "Eggless",
            "Figure only",
            "Figure on base",
            "Blackforest",
            "Red velvet",
            "Milkybar",
            "Bar one",
            "Ferrero",
            "Carrot cake",
            "Heart shape",
            "Bible",
            "Tiered",
            "Mould",
            "Doll cake",
            "Soccer field",
            "1mx 500"
        })
        
        ' Add button
        btnAddRequest = New Button With {
            .Text = "➕ ADD",
            .Font = New Font("Segoe UI", 11, FontStyle.Bold),
            .Size = New Size(100, 30),
            .Location = New Point(430, yPos),
            .BackColor = _green,
            .ForeColor = Color.White,
            .FlatStyle = FlatStyle.Flat,
            .Cursor = Cursors.Hand
        }
        btnAddRequest.FlatAppearance.BorderSize = 0
        AddHandler btnAddRequest.Click, AddressOf AddSpecialRequest
        
        pnlMain.Controls.AddRange({cboSpecialRequests, btnAddRequest})
        yPos += 40
        
        ' TextBox to display added special requests
        Dim lblRequestsDisplay As New Label With {
            .Text = "Added Special Requests:",
            .Font = New Font("Segoe UI", 10),
            .Location = New Point(20, yPos),
            .AutoSize = True
        }
        pnlMain.Controls.Add(lblRequestsDisplay)
        yPos += 25
        
        txtSpecialRequests = New TextBox With {
            .Font = New Font("Segoe UI", 10),
            .Location = New Point(20, yPos),
            .Width = 1120,
            .Height = 80,
            .Multiline = True,
            .ScrollBars = ScrollBars.Vertical,
            .BackColor = Color.LightYellow,
            .ReadOnly = False
        }
        pnlMain.Controls.Add(txtSpecialRequests)
        yPos += 100
        
        ' Bottom panel with totals
        Dim pnlBottom As New Panel With {
            .Dock = DockStyle.Bottom,
            .Height = 180,
            .BackColor = Color.White,
            .Padding = New Padding(20)
        }
        
        ' Total
        Dim lblTotalLabel As New Label With {
            .Text = "TOTAL AMOUNT:",
            .Font = New Font("Segoe UI", 16, FontStyle.Bold),
            .Location = New Point(20, 20),
            .AutoSize = True
        }
        lblTotal = New Label With {
            .Text = "R 0.00",
            .Font = New Font("Segoe UI", 20, FontStyle.Bold),
            .ForeColor = _green,
            .Location = New Point(250, 15),
            .AutoSize = True
        }
        
        ' Deposit
        Dim lblDepositLabel As New Label With {
            .Text = "Deposit Amount:",
            .Font = New Font("Segoe UI", 12),
            .Location = New Point(20, 65),
            .AutoSize = True
        }
        txtDeposit = New TextBox With {
            .Font = New Font("Segoe UI", 14),
            .Location = New Point(250, 60),
            .Width = 150,
            .Text = "0.00"
        }
        AddHandler txtDeposit.TextChanged, AddressOf CalculateBalance
        
        ' Balance
        Dim lblBalanceLabel As New Label With {
            .Text = "Balance Due:",
            .Font = New Font("Segoe UI", 12, FontStyle.Bold),
            .Location = New Point(20, 105),
            .AutoSize = True
        }
        lblBalance = New Label With {
            .Text = "R 0.00",
            .Font = New Font("Segoe UI", 16, FontStyle.Bold),
            .ForeColor = _red,
            .Location = New Point(250, 100),
            .AutoSize = True
        }
        
        ' Buttons
        Dim btnCalculate As New Button With {
            .Text = "📊 CALCULATE QUOTATION",
            .Font = New Font("Segoe UI", 12, FontStyle.Bold),
            .Size = New Size(250, 50),
            .Location = New Point(650, 20),
            .BackColor = _lightBlue,
            .ForeColor = Color.White,
            .FlatStyle = FlatStyle.Flat,
            .Cursor = Cursors.Hand
        }
        btnCalculate.FlatAppearance.BorderSize = 0
        AddHandler btnCalculate.Click, AddressOf CalculateQuotation
        
        Dim btnAccept As New Button With {
            .Text = "✓ ACCEPT & CREATE ORDER",
            .Font = New Font("Segoe UI", 12, FontStyle.Bold),
            .Size = New Size(250, 50),
            .Location = New Point(920, 20),
            .BackColor = _green,
            .ForeColor = Color.White,
            .FlatStyle = FlatStyle.Flat,
            .Cursor = Cursors.Hand
        }
        btnAccept.FlatAppearance.BorderSize = 0
        AddHandler btnAccept.Click, AddressOf AcceptOrder
        
        Dim btnCancel As New Button With {
            .Text = "✗ CANCEL",
            .Font = New Font("Segoe UI", 12, FontStyle.Bold),
            .Size = New Size(150, 50),
            .Location = New Point(920, 80),
            .BackColor = _darkGray,
            .ForeColor = Color.White,
            .FlatStyle = FlatStyle.Flat,
            .Cursor = Cursors.Hand
        }
        btnCancel.FlatAppearance.BorderSize = 0
        AddHandler btnCancel.Click, Sub() Me.Close()
        
        pnlBottom.Controls.AddRange({lblTotalLabel, lblTotal, lblDepositLabel, txtDeposit, 
                                     lblBalanceLabel, lblBalance, btnCalculate, btnAccept, btnCancel})
        
        ' Initialize keyboard and numpad (start with first textbox as target)
        _activeTextBox = txtCustomerName
        _keyboard = New OrderEntryKeyboard(_activeTextBox) With {
            .Location = New Point(50, 400),
            .Visible = False
        }
        
        _numpad = New OrderEntryNumpad(_activeTextBox) With {
            .Location = New Point(300, 400),
            .Visible = False
        }
        
        ' Add focus handlers to all textboxes to track active field
        AddHandler txtCustomerName.GotFocus, Sub() UpdateKeyboardTarget(txtCustomerName)
        AddHandler txtCustomerPhone.GotFocus, Sub() UpdateKeyboardTarget(txtCustomerPhone)
        AddHandler txtCustomerEmail.GotFocus, Sub() UpdateKeyboardTarget(txtCustomerEmail)
        AddHandler txtCustomerAddress.GotFocus, Sub() UpdateKeyboardTarget(txtCustomerAddress)
        AddHandler txtPickupTime.GotFocus, Sub() UpdateKeyboardTarget(txtPickupTime)
        AddHandler txtDeposit.GotFocus, Sub() UpdateKeyboardTarget(txtDeposit)
        
        Me.Controls.AddRange({pnlMain, pnlBottom, pnlHeader, _keyboard, _numpad})
    End Sub
    
    Private Sub AddSpecialRequest(sender As Object, e As EventArgs)
        Try
            Dim requestText = cboSpecialRequests.Text.Trim()
            
            If String.IsNullOrWhiteSpace(requestText) Then
                MessageBox.Show("Please enter or select a special request.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                Return
            End If
            
            ' Add to special requests textbox
            If String.IsNullOrWhiteSpace(txtSpecialRequests.Text) Then
                txtSpecialRequests.Text = requestText
            Else
                txtSpecialRequests.Text &= vbCrLf & requestText
            End If
            
            ' Clear combobox for next entry
            cboSpecialRequests.Text = ""
            cboSpecialRequests.Focus()
            
        Catch ex As Exception
            MessageBox.Show($"Error adding special request: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub
    
    Private Sub UpdateKeyboardTarget(targetTextBox As TextBox)
        _activeTextBox = targetTextBox
        ' Update keyboard and numpad targets
        If _keyboard IsNot Nothing Then
            Me.Controls.Remove(_keyboard)
            _keyboard = New OrderEntryKeyboard(targetTextBox) With {
                .Location = New Point(50, 400),
                .Visible = _keyboard.Visible
            }
            Me.Controls.Add(_keyboard)
            _keyboard.BringToFront()
        End If
        
        If _numpad IsNot Nothing Then
            Me.Controls.Remove(_numpad)
            _numpad = New OrderEntryNumpad(targetTextBox) With {
                .Location = New Point(300, 400),
                .Visible = _numpad.Visible
            }
            Me.Controls.Add(_numpad)
            _numpad.BringToFront()
        End If
    End Sub
    
    Private Sub LoadQuestions()
        Try
            Using conn As New SqlConnection(_connectionString)
                conn.Open()
                
                Dim sql = "
                    SELECT QuestionID, QuestionText, QuestionType, DisplayOrder
                    FROM CakeOrder_Questions
                    WHERE BranchID = @BranchID AND IsActive = 1
                    ORDER BY DisplayOrder"
                
                Using cmd As New SqlCommand(sql, conn)
                    cmd.Parameters.AddWithValue("@BranchID", _branchID)
                    Using adapter As New SqlDataAdapter(cmd)
                        adapter.Fill(_questions)
                    End Using
                End Using
            End Using
            
            DisplayQuestions()
            
        Catch ex As Exception
            MessageBox.Show($"Error loading questions: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub
    
    Private Sub DisplayQuestions()
        flpQuestions.Controls.Clear()
        
        For Each row As DataRow In _questions.Rows
            Dim questionID = CInt(row("QuestionID"))
            Dim questionText = row("QuestionText").ToString()
            Dim questionType = row("QuestionType").ToString()
            
            CreateQuestionPanel(questionID, questionText, questionType)
        Next
    End Sub
    
    Private Sub CreateQuestionPanel(questionID As Integer, questionText As String, questionType As String)
        Dim pnlQuestion As New Panel With {
            .Width = 1080,
            .AutoSize = True,
            .Padding = New Padding(10),
            .BackColor = Color.White,
            .Margin = New Padding(5)
        }
        
        Dim lblQuestion As New Label With {
            .Text = questionText,
            .Font = New Font("Segoe UI", 12, FontStyle.Bold),
            .Location = New Point(10, 10),
            .AutoSize = True,
            .ForeColor = _darkBlue
        }
        pnlQuestion.Controls.Add(lblQuestion)
        
        Dim yPos = 40
        
        ' Load options for this question
        Using conn As New SqlConnection(_connectionString)
            conn.Open()
            
            Dim sql = "
                SELECT OptionID, OptionText, Price, DisplayOrder
                FROM CakeOrder_QuestionOptions
                WHERE QuestionID = @QuestionID AND IsActive = 1
                ORDER BY DisplayOrder"
            
            Using cmd As New SqlCommand(sql, conn)
                cmd.Parameters.AddWithValue("@QuestionID", questionID)
                Using reader = cmd.ExecuteReader()
                    
                    Select Case questionType
                        Case "SingleChoice"
                            Dim rbGroup As New GroupBox With {
                                .Location = New Point(20, yPos),
                                .Width = 1040,
                                .Height = 150,
                                .Text = ""
                            }
                            
                            Dim rbYPos = 20
                            While reader.Read()
                                Dim optionID = CInt(reader("OptionID"))
                                Dim optionText = reader("OptionText").ToString()
                                Dim price = CDec(reader("Price"))
                                
                                Dim rb As New RadioButton With {
                                    .Text = $"{optionText} - {price:C2}",
                                    .Font = New Font("Segoe UI", 11),
                                    .Location = New Point(10, rbYPos),
                                    .AutoSize = True,
                                    .Tag = New With {questionID, questionText, optionText, price}
                                }
                                
                                AddHandler rb.CheckedChanged, Sub(s, e)
                                    If rb.Checked Then
                                        _selectedAnswers(questionID) = New SelectedAnswer With {
                                            .QuestionID = questionID,
                                            .QuestionText = questionText,
                                            .AnswerText = optionText,
                                            .AnswerPrice = price
                                        }
                                    End If
                                End Sub
                                
                                rbGroup.Controls.Add(rb)
                                rbYPos += 30
                            End While
                            
                            pnlQuestion.Controls.Add(rbGroup)
                            pnlQuestion.Height = yPos + rbGroup.Height + 20
                            
                        Case "Text"
                            Dim txtAnswer As New TextBox With {
                                .Font = New Font("Segoe UI", 11),
                                .Location = New Point(20, yPos),
                                .Width = 600,
                                .Multiline = True,
                                .Height = 60
                            }
                            
                            AddHandler txtAnswer.TextChanged, Sub(s, e)
                                Dim wordingPrice As Decimal = 0
                                If txtAnswer.Text.Length > 0 AndAlso txtAnswer.Text.Length <= 20 Then
                                    wordingPrice = 50
                                ElseIf txtAnswer.Text.Length > 20 Then
                                    wordingPrice = 100
                                End If
                                
                                _selectedAnswers(questionID) = New SelectedAnswer With {
                                    .QuestionID = questionID,
                                    .QuestionText = questionText,
                                    .AnswerText = txtAnswer.Text,
                                    .AnswerPrice = wordingPrice
                                }
                            End Sub
                            
                            pnlQuestion.Controls.Add(txtAnswer)
                            pnlQuestion.Height = yPos + 80
                            
                        Case "MultiChoice"
                            ' For Accessories and Toppings
                            If questionText.Contains("Accessories") Then
                                Dim btnSelectAccessories As New Button With {
                                    .Text = "Select Accessories",
                                    .Font = New Font("Segoe UI", 11, FontStyle.Bold),
                                    .Size = New Size(200, 40),
                                    .Location = New Point(20, yPos),
                                    .BackColor = _lightBlue,
                                    .ForeColor = Color.White,
                                    .FlatStyle = FlatStyle.Flat,
                                    .Cursor = Cursors.Hand
                                }
                                btnSelectAccessories.FlatAppearance.BorderSize = 0
                                AddHandler btnSelectAccessories.Click, Sub() SelectAccessories()
                                pnlQuestion.Controls.Add(btnSelectAccessories)
                                
                            ElseIf questionText.Contains("Toppings") Then
                                Dim btnSelectToppings As New Button With {
                                    .Text = "Select Toppings",
                                    .Font = New Font("Segoe UI", 11, FontStyle.Bold),
                                    .Size = New Size(200, 40),
                                    .Location = New Point(20, yPos),
                                    .BackColor = _lightBlue,
                                    .ForeColor = Color.White,
                                    .FlatStyle = FlatStyle.Flat,
                                    .Cursor = Cursors.Hand
                                }
                                btnSelectToppings.FlatAppearance.BorderSize = 0
                                AddHandler btnSelectToppings.Click, Sub() SelectToppings()
                                pnlQuestion.Controls.Add(btnSelectToppings)
                            End If
                            
                            pnlQuestion.Height = yPos + 60
                    End Select
                End Using
            End Using
        End Using
        
        flpQuestions.Controls.Add(pnlQuestion)
    End Sub
    
    Private Sub SelectAccessories()
        MessageBox.Show("Accessories selection coming soon!", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information)
        ' TODO: Open accessories selection form
    End Sub
    
    Private Sub SelectToppings()
        MessageBox.Show("Toppings selection coming soon!", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information)
        ' TODO: Open toppings selection form
    End Sub
    
    Private Sub CalculateQuotation(sender As Object, e As EventArgs)
        ' Calculate total from all selected answers
        _totalAmount = 0
        
        For Each answer In _selectedAnswers.Values
            _totalAmount += answer.AnswerPrice
        Next
        
        For Each accessory In _selectedAccessories
            _totalAmount += accessory.TotalPrice
        Next
        
        For Each topping In _selectedToppings
            _totalAmount += topping.TotalPrice
        Next
        
        lblTotal.Text = _totalAmount.ToString("C2")
        CalculateBalance(Nothing, Nothing)
        
        MessageBox.Show($"Quotation Total: {_totalAmount:C2}", "Quotation", MessageBoxButtons.OK, MessageBoxIcon.Information)
    End Sub
    
    Private Sub CalculateBalance(sender As Object, e As EventArgs)
        Decimal.TryParse(txtDeposit.Text, _depositAmount)
        _balanceAmount = _totalAmount - _depositAmount
        lblBalance.Text = _balanceAmount.ToString("C2")
    End Sub
    
    Private Sub AcceptOrder(sender As Object, e As EventArgs)
        ' Validate and save order
        If Not ValidateOrder() Then Return
        
        If MessageBox.Show($"Accept this order?{vbCrLf}Total: {_totalAmount:C2}{vbCrLf}Deposit: {_depositAmount:C2}{vbCrLf}Balance: {_balanceAmount:C2}", 
                          "Confirm Order", MessageBoxButtons.YesNo, MessageBoxIcon.Question) = DialogResult.Yes Then
            SaveOrder()
        End If
    End Sub
    
    Private Function ValidateOrder() As Boolean
        If String.IsNullOrWhiteSpace(txtCustomerName.Text) Then
            MessageBox.Show("Customer name is required!", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            txtCustomerName.Focus()
            Return False
        End If
        
        If String.IsNullOrWhiteSpace(txtCustomerPhone.Text) Then
            MessageBox.Show("Customer phone is required!", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            txtCustomerPhone.Focus()
            Return False
        End If
        
        If _totalAmount <= 0 Then
            MessageBox.Show("Please calculate quotation first!", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            Return False
        End If
        
        Return True
    End Function
    
    Private Sub SaveOrder()
        Try
            Using conn As New SqlConnection(_connectionString)
                conn.Open()
                Using transaction = conn.BeginTransaction()
                    Try
                        ' Get branch prefix and generate order number
                        Dim branchPrefix As String = GetBranchPrefix(conn, transaction)
                        Dim orderNumber As String = GenerateOrderNumber(conn, transaction, branchPrefix)
                        
                        ' Build manufacturing instructions from answers
                        Dim manufacturingInstructions As New System.Text.StringBuilder()
                        manufacturingInstructions.AppendLine("=== CUSTOM CAKE ORDER ===")
                        manufacturingInstructions.AppendLine($"Order: {orderNumber}")
                        manufacturingInstructions.AppendLine($"Customer: {txtCustomerName.Text.Trim()}")
                        manufacturingInstructions.AppendLine($"Phone: {txtCustomerPhone.Text.Trim()}")
                        manufacturingInstructions.AppendLine($"Pickup: {dtpPickupDate.Value:dd MMM yyyy} at {txtPickupTime.Text.Trim()}")
                        manufacturingInstructions.AppendLine("")
                        manufacturingInstructions.AppendLine("SPECIFICATIONS:")
                        For Each answer In _selectedAnswers.Values
                            manufacturingInstructions.AppendLine($"{answer.QuestionText}: {answer.AnswerText}")
                        Next
                        If _selectedAccessories.Count > 0 Then
                            manufacturingInstructions.AppendLine("")
                            manufacturingInstructions.AppendLine("ACCESSORIES:")
                            For Each acc As SelectedItem In _selectedAccessories
                                manufacturingInstructions.AppendLine($"  {acc.Quantity}x {acc.ItemName}")
                            Next
                        End If
                        If _selectedToppings.Count > 0 Then
                            manufacturingInstructions.AppendLine("")
                            manufacturingInstructions.AppendLine("TOPPINGS:")
                            For Each top As SelectedItem In _selectedToppings
                                manufacturingInstructions.AppendLine($"  {top.Quantity}x {top.ItemName}")
                            Next
                        End If
                        
                        ' Add special requests if any
                        If Not String.IsNullOrWhiteSpace(txtSpecialRequests.Text) Then
                            manufacturingInstructions.AppendLine()
                            manufacturingInstructions.AppendLine("SPECIAL REQUESTS:")
                            manufacturingInstructions.AppendLine(txtSpecialRequests.Text)
                        End If
                        
                        ' Insert main order into POS_CustomOrders
                        Dim sqlOrder = "
                            INSERT INTO POS_CustomOrders (
                                OrderNumber, BranchID, OrderType,
                                CustomerName, CustomerSurname, CustomerPhone,
                                OrderDate, ReadyDate, ReadyTime,
                                TotalAmount, DepositPaid, BalanceDue,
                                OrderStatus, CreatedBy, ManufacturingInstructions, BranchName
                            ) VALUES (
                                @OrderNumber, @BranchID, 'Cake',
                                @CustomerName, '', @CustomerPhone,
                                GETDATE(), @ReadyDate, @ReadyTime,
                                @TotalAmount, @DepositPaid, @BalanceDue,
                                'New', @CreatedBy, @ManufacturingInstructions, @BranchName
                            )
                            SELECT SCOPE_IDENTITY()"
                        
                        Dim orderID As Integer
                        Dim branchName As String = GetBranchName(conn, transaction)
                        Dim readyTime As TimeSpan = TimeSpan.Parse(txtPickupTime.Text.Trim())
                        
                        Using cmd As New SqlCommand(sqlOrder, conn, transaction)
                            cmd.Parameters.AddWithValue("@OrderNumber", orderNumber)
                            cmd.Parameters.AddWithValue("@BranchID", _branchID)
                            cmd.Parameters.AddWithValue("@CustomerName", txtCustomerName.Text.Trim())
                            cmd.Parameters.AddWithValue("@CustomerPhone", txtCustomerPhone.Text.Trim())
                            cmd.Parameters.AddWithValue("@ReadyDate", dtpPickupDate.Value.Date)
                            cmd.Parameters.AddWithValue("@ReadyTime", readyTime)
                            cmd.Parameters.AddWithValue("@TotalAmount", _totalAmount)
                            cmd.Parameters.AddWithValue("@DepositPaid", _depositAmount)
                            cmd.Parameters.AddWithValue("@BalanceDue", _balanceAmount)
                            cmd.Parameters.AddWithValue("@CreatedBy", _cashierName)
                            cmd.Parameters.AddWithValue("@ManufacturingInstructions", manufacturingInstructions.ToString())
                            cmd.Parameters.AddWithValue("@BranchName", branchName)
                            
                            orderID = Convert.ToInt32(cmd.ExecuteScalar())
                        End Using
                        
                        ' Insert order items into POS_CustomOrderItems
                        Dim sqlItem = "
                            INSERT INTO POS_CustomOrderItems (
                                OrderID, ProductID, ProductName, Quantity, UnitPrice, LineTotal
                            ) VALUES (
                                @OrderID, 0, @ProductName, @Quantity, @UnitPrice, @LineTotal
                            )"
                        
                        ' Add main cake as item
                        Using cmd As New SqlCommand(sqlItem, conn, transaction)
                            cmd.Parameters.AddWithValue("@OrderID", orderID)
                            cmd.Parameters.AddWithValue("@ProductName", "Custom Cake Order")
                            cmd.Parameters.AddWithValue("@Quantity", 1)
                            Dim cakeTotal = _selectedAnswers.Values.Sum(Function(a) a.AnswerPrice)
                            cmd.Parameters.AddWithValue("@UnitPrice", cakeTotal)
                            cmd.Parameters.AddWithValue("@LineTotal", cakeTotal)
                            cmd.ExecuteNonQuery()
                        End Using
                        
                        ' Add accessories as items
                        For Each accessory In _selectedAccessories
                            Using cmd As New SqlCommand(sqlItem, conn, transaction)
                                cmd.Parameters.AddWithValue("@OrderID", orderID)
                                cmd.Parameters.AddWithValue("@ProductName", accessory.ItemName)
                                cmd.Parameters.AddWithValue("@Quantity", accessory.Quantity)
                                cmd.Parameters.AddWithValue("@UnitPrice", accessory.UnitPrice)
                                cmd.Parameters.AddWithValue("@LineTotal", accessory.TotalPrice)
                                cmd.ExecuteNonQuery()
                            End Using
                        Next
                        
                        ' Add toppings as items
                        For Each topping In _selectedToppings
                            Using cmd As New SqlCommand(sqlItem, conn, transaction)
                                cmd.Parameters.AddWithValue("@OrderID", orderID)
                                cmd.Parameters.AddWithValue("@ProductName", topping.ItemName)
                                cmd.Parameters.AddWithValue("@Quantity", topping.Quantity)
                                cmd.Parameters.AddWithValue("@UnitPrice", topping.UnitPrice)
                                cmd.Parameters.AddWithValue("@LineTotal", topping.TotalPrice)
                                cmd.ExecuteNonQuery()
                            End Using
                        Next
                        
                        ' Record deposit payment in Demo_Sales if > 0
                        If _depositAmount > 0 Then
                            Dim sqlDeposit = "
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
                                cmd.Parameters.AddWithValue("@Subtotal", _depositAmount / 1.15D)
                                cmd.Parameters.AddWithValue("@TaxAmount", _depositAmount - (_depositAmount / 1.15D))
                                cmd.Parameters.AddWithValue("@TotalAmount", _depositAmount)
                                cmd.Parameters.AddWithValue("@ReferenceNumber", orderNumber)
                                cmd.ExecuteNonQuery()
                            End Using
                        End If
                        
                        transaction.Commit()
                        
                        ' Process deposit payment if > 0
                        If _depositAmount > 0 Then
                            ProcessDepositPayment(orderID, orderNumber, branchName)
                        Else
                            ' No deposit - just show confirmation
                            ShowOrderConfirmation(orderNumber, orderID, branchName, Nothing)
                            MessageBox.Show($"Order created successfully!{vbCrLf}Order Number: {orderNumber}{vbCrLf}Sent to Manufacturing automatically.", 
                                          "Success", MessageBoxButtons.OK, MessageBoxIcon.Information)
                        End If
                        
                        Me.DialogResult = DialogResult.OK
                        Me.Close()
                        
                    Catch ex As Exception
                        transaction.Rollback()
                        Throw
                    End Try
                End Using
            End Using
            
        Catch ex As Exception
            MessageBox.Show($"Error saving order: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub
    
    Private Sub ProcessDepositPayment(orderID As Integer, orderNumber As String, branchName As String)
        Try
            ' Open payment tender form for deposit
            Using paymentForm As New PaymentTenderForm(_depositAmount, _branchID, _tillPointID, _cashierID, _cashierName)
                If paymentForm.ShowDialog() = DialogResult.OK Then
                    ' Get payment details
                    Dim paymentMethod = paymentForm.PaymentMethod
                    Dim cashAmount = paymentForm.CashAmount
                    Dim cardAmount = paymentForm.CardAmount
                    Dim changeAmount = paymentForm.ChangeAmount
                    
                    ' Show receipt with payment details
                    Dim paymentDetails As New Dictionary(Of String, Object) From {
                        {"PaymentMethod", paymentMethod},
                        {"CashAmount", cashAmount},
                        {"CardAmount", cardAmount},
                        {"ChangeAmount", changeAmount}
                    }
                    
                    ShowOrderConfirmation(orderNumber, orderID, branchName, paymentDetails)
                    
                    MessageBox.Show($"Deposit payment processed!{vbCrLf}Order Number: {orderNumber}{vbCrLf}Sent to Manufacturing automatically.", 
                                  "Success", MessageBoxButtons.OK, MessageBoxIcon.Information)
                Else
                    MessageBox.Show("Payment cancelled. Order saved but no deposit recorded.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                End If
            End Using
            
        Catch ex As Exception
            MessageBox.Show($"Error processing payment: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub
    
    Private Sub ShowOrderConfirmation(orderNumber As String, orderID As Integer, branchName As String, paymentDetails As Dictionary(Of String, Object))
        ' Create order confirmation/invoice display
        Dim confirmForm As New Form With {
            .Text = "Order Confirmation",
            .Size = New Size(600, 800),
            .StartPosition = FormStartPosition.CenterScreen,
            .BackColor = Color.White
        }
        
        Dim txtReceipt As New TextBox With {
            .Multiline = True,
            .ScrollBars = ScrollBars.Vertical,
            .Dock = DockStyle.Fill,
            .Font = New Font("Courier New", 10),
            .ReadOnly = True
        }
        
        Dim receipt As New System.Text.StringBuilder()
        receipt.AppendLine("================================")
        receipt.AppendLine("     CAKE ORDER CONFIRMATION")
        receipt.AppendLine("================================")
        receipt.AppendLine()
        receipt.AppendLine($"Order #: {orderNumber}")
        receipt.AppendLine($"Date: {DateTime.Now:dd MM yyyy HH:mm}")
        receipt.AppendLine($"Cashier: {_cashierName}")
        receipt.AppendLine()
        receipt.AppendLine("CUSTOMER DETAILS:")
        receipt.AppendLine($"Name: {txtCustomerName.Text}")
        receipt.AppendLine($"Phone: {txtCustomerPhone.Text}")
        If Not String.IsNullOrWhiteSpace(txtCustomerEmail.Text) Then
            receipt.AppendLine($"Email: {txtCustomerEmail.Text}")
        End If
        receipt.AppendLine()
        receipt.AppendLine($"PICKUP: {dtpPickupDate.Value:dd MM yyyy} at {txtPickupTime.Text}")
        receipt.AppendLine()
        receipt.AppendLine("*** PICKUP LOCATION ***")
        receipt.AppendLine($"Branch: {branchName}")
        receipt.AppendLine("================================")
        receipt.AppendLine()
        receipt.AppendLine("================================")
        receipt.AppendLine("CAKE SPECIFICATIONS:")
        receipt.AppendLine("================================")
        
        ' Line item - Custom Cake Order
        receipt.AppendLine("ITEM:")
        receipt.AppendLine($"  Custom Cake Order")
        receipt.AppendLine()
        
        For Each answer In _selectedAnswers.Values
            receipt.AppendLine($"  {answer.QuestionText}")
            receipt.AppendLine($"    {answer.AnswerText} - {answer.AnswerPrice:C2}")
        Next
        
        ' Add special requests to receipt
        If Not String.IsNullOrWhiteSpace(txtSpecialRequests.Text) Then
            receipt.AppendLine()
            receipt.AppendLine("SPECIAL REQUESTS:")
            Dim requests() As String = txtSpecialRequests.Text.Split(New String() {vbCrLf, vbLf}, StringSplitOptions.RemoveEmptyEntries)
            For Each request In requests
                receipt.AppendLine($"  - {request}")
            Next
        End If
        
        receipt.AppendLine()
        receipt.AppendLine("================================")
        receipt.AppendLine($"TOTAL AMOUNT:     {_totalAmount:C2}")
        receipt.AppendLine()
        
        ' Payment details if provided
        If paymentDetails IsNot Nothing Then
            Dim paymentMethod = paymentDetails("PaymentMethod").ToString()
            Dim cashAmount = CDec(paymentDetails("CashAmount"))
            Dim cardAmount = CDec(paymentDetails("CardAmount"))
            Dim changeAmount = CDec(paymentDetails("ChangeAmount"))
            
            receipt.AppendLine("PAYMENT DETAILS:")
            receipt.AppendLine($"Payment Method: {paymentMethod}")
            
            If paymentMethod = "Cash" Then
                receipt.AppendLine($"Cash Tendered:  {cashAmount:C2}")
                receipt.AppendLine($"Change:         {changeAmount:C2}")
            ElseIf paymentMethod = "Card" Then
                receipt.AppendLine($"Card Amount:    {cardAmount:C2}")
            ElseIf paymentMethod = "Split" Then
                receipt.AppendLine($"Cash Amount:    {cashAmount:C2}")
                receipt.AppendLine($"Card Amount:    {cardAmount:C2}")
                If changeAmount > 0 Then
                    receipt.AppendLine($"Change:         {changeAmount:C2}")
                End If
            End If
            receipt.AppendLine()
        End If
        
        receipt.AppendLine($"DEPOSIT PAID:     {_depositAmount:C2}")
        receipt.AppendLine($"BALANCE DUE:      {_balanceAmount:C2}")
        receipt.AppendLine()
        receipt.AppendLine("================================")
        receipt.AppendLine()
        receipt.AppendLine($"** PICKUP DATE **")
        receipt.AppendLine($"{dtpPickupDate.Value:dddd, dd MMMM yyyy}")
        receipt.AppendLine($"Time: {txtPickupTime.Text}")
        receipt.AppendLine()
        receipt.AppendLine("================================")
        receipt.AppendLine()
        receipt.AppendLine("Thank you for your order!")
        receipt.AppendLine("Order sent to Manufacturing.")
        receipt.AppendLine()
        receipt.AppendLine("Please bring this receipt")
        receipt.AppendLine("when collecting your order.")
        receipt.AppendLine()
        
        txtReceipt.Text = receipt.ToString()
        
        Dim btnClose As New Button With {
            .Text = "CLOSE",
            .Dock = DockStyle.Bottom,
            .Height = 50,
            .Font = New Font("Segoe UI", 12, FontStyle.Bold),
            .BackColor = _darkGray,
            .ForeColor = Color.White,
            .FlatStyle = FlatStyle.Flat
        }
        btnClose.FlatAppearance.BorderSize = 0
        AddHandler btnClose.Click, Sub() confirmForm.Close()
        
        confirmForm.Controls.AddRange({txtReceipt, btnClose})
        confirmForm.ShowDialog()
    End Sub
    
    Private Function GetBranchPrefix(conn As SqlConnection, transaction As SqlTransaction) As String
        Try
            Dim cmd As New SqlCommand("SELECT BranchCode FROM Branches WHERE BranchID = @bid", conn, transaction)
            cmd.Parameters.AddWithValue("@bid", _branchID)
            Dim result = cmd.ExecuteScalar()
            Return If(result IsNot Nothing, result.ToString(), "BR")
        Catch
            Return "BR"
        End Try
    End Function
    
    Private Function GetBranchName(conn As SqlConnection, transaction As SqlTransaction) As String
        Try
            Dim cmd As New SqlCommand("SELECT BranchName FROM Branches WHERE BranchID = @bid", conn, transaction)
            cmd.Parameters.AddWithValue("@bid", _branchID)
            Dim result = cmd.ExecuteScalar()
            Return If(result IsNot Nothing, result.ToString(), "Branch")
        Catch
            Return "Branch"
        End Try
    End Function
    
    Private Function GenerateOrderNumber(conn As SqlConnection, transaction As SqlTransaction, branchPrefix As String) As String
        ' Generate numeric-only order number: BranchID + 2 + 4-digit sequence
        ' Example: Branch 6, sequence 1 -> "620001"
        ' Transaction type codes: 1=Sale, 4=Return, 2=Order
        ' Shorter format for optimal barcode scanning (6 digits total)
        
        Dim branchID As Integer = 0
        Try
            Dim sqlBranch = "SELECT BranchID FROM Branches WHERE BranchPrefix = @prefix"
            Using cmdBranch As New SqlCommand(sqlBranch, conn, transaction)
                cmdBranch.Parameters.AddWithValue("@prefix", branchPrefix)
                Dim result = cmdBranch.ExecuteScalar()
                If result IsNot Nothing Then
                    branchID = CInt(result)
                End If
            End Using
        Catch
            branchID = 1 ' Default to 1 if lookup fails
        End Try
        
        Dim pattern As String = $"{branchID}2%"
        Dim sql As String = "
            SELECT ISNULL(MAX(CAST(RIGHT(OrderNumber, 4) AS INT)), 0) + 1 
            FROM POS_CustomOrders WITH (TABLOCKX)
            WHERE OrderNumber LIKE @pattern AND LEN(OrderNumber) = 6"
        
        Using cmd As New SqlCommand(sql, conn, transaction)
            cmd.Parameters.AddWithValue("@pattern", pattern)
            Dim nextNumber As Integer = Convert.ToInt32(cmd.ExecuteScalar())
            Return $"{branchID}2{nextNumber.ToString().PadLeft(4, "0"c)}"
        End Using
    End Function
End Class
