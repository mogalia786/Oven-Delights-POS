Imports System.Windows.Forms
Imports System.Drawing
Imports System.Drawing.Printing
Imports System.Data.SqlClient
Imports System.Configuration

Public Class CakeOrderFormNew
    Inherits Form

    Private _branchID As Integer
    Private _tillPointID As Integer
    Private _cashierID As Integer
    Private _cashierName As String
    Private _connectionString As String
    Private _branchName As String
    Private _branchAddress As String
    Private _branchPhone As String
    
    ' Form controls matching pre-printed template
    Private txtCakeColor As TextBox
    Private txtCakePicture As TextBox
    Private dtpCollectionDate As DateTimePicker
    Private lblCollectionDay As Label
    Private dtpCollectionTime As DateTimePicker
    
    Private txtAccountNumber As TextBox
    Private txtCustomerName As TextBox
    Private txtCustomerPhone As TextBox
    
    Private lblCollectionPoint As Label
    Private lblOrderNumber As Label
    Private lblOrderDate As Label
    Private lblOrderTakenBy As Label
    
    Private dgvItems As DataGridView
    Private cboProductSearch As ComboBox
    Private nudQuantity As NumericUpDown
    Private btnAddItem As Button
    Private btnRemoveItem As Button
    
    Private cboSpecialRequests As ComboBox
    Private txtSpecialRequests As TextBox
    Private btnAddRequest As Button
    Private txtNotes As TextBox
    
    Private lblInvoiceTotal As Label
    Private txtDeposit As TextBox
    Private lblBalance As Label
    
    Private btnPrintPreview As Button
    Private btnAcceptOrder As Button
    Private btnCancel As Button
    
    Private _orderItems As New List(Of OrderItem)
    Private _totalAmount As Decimal = 0
    Private _depositAmount As Decimal = 0
    Private _balanceAmount As Decimal = 0
    
    ' Edit mode fields
    Private _isEditMode As Boolean = False
    Private _editOrderID As Integer = 0
    Private _editOrderNumber As String = ""
    Private _originalDepositPaid As Decimal = 0
    Private _originalTotalAmount As Decimal = 0
    Private _orderEditedDate As DateTime = DateTime.Now
    
    ' Cancel mode fields
    Private _isCancelMode As Boolean = False
    Private _cancellationFeeProductID As Integer = 0
    Private _cancellationFeeAmount As Decimal = 0
    
    ' Accounting service
    Private _accountingService As AccountingService
    
    ' Customer info for accounting
    Private _customerID As Integer = 0
    Private _customerAccountNumber As String = ""
    
    Public Class OrderItem
        Public Property ProductID As Integer
        Public Property Description As String
        Public Property Quantity As Integer
        Public Property UnitPrice As Decimal
        Public Property TotalPrice As Decimal
    End Class
    
    Public Sub New(branchID As Integer, tillPointID As Integer, cashierID As Integer, cashierName As String, branchName As String, branchAddress As String, branchPhone As String, Optional cartItems As DataTable = Nothing, Optional editOrderID As Integer = 0, Optional isCancelMode As Boolean = False)
        _branchID = branchID
        _tillPointID = tillPointID
        _cashierID = cashierID
        _cashierName = cashierName
        _branchName = branchName
        _branchAddress = branchAddress
        _branchPhone = branchPhone
        _connectionString = ConfigurationManager.ConnectionStrings("OvenDelightsERPConnectionString").ConnectionString
        
        ' Initialize accounting service
        _accountingService = New AccountingService()
        
        ' Check if this is edit or cancel mode
        If editOrderID > 0 Then
            If isCancelMode Then
                _isCancelMode = True
                _editOrderID = editOrderID
            Else
                _isEditMode = True
                _editOrderID = editOrderID
            End If
        End If
        
        InitializeComponent()
        LoadProducts()
        
        ' Load existing order for editing or cancelling
        If _isEditMode OrElse _isCancelMode Then
            LoadExistingOrder(_editOrderID)
            
            ' If cancel mode, load cancellation fees and setup UI
            If _isCancelMode Then
                SetupCancelMode()
            End If
        ElseIf cartItems IsNot Nothing AndAlso cartItems.Rows.Count > 0 Then
            ' Pre-populate with cart items if provided
            LoadCartItems(cartItems)
        End If
    End Sub
    
    Private Sub LoadCartItems(cartItems As DataTable)
        Try
            ' Convert cart items to order items
            For Each row As DataRow In cartItems.Rows
                Dim item As New OrderItem With {
                    .ProductID = If(IsDBNull(row("ProductID")), 0, CInt(row("ProductID"))),
                    .Description = row("Product").ToString(),
                    .Quantity = CInt(row("Qty")),
                    .UnitPrice = CDec(row("Price")),
                    .TotalPrice = CDec(row("Total"))
                }
                _orderItems.Add(item)
            Next
            
            ' Refresh grid and calculate totals
            RefreshItemsGrid()
            CalculateTotals()
            
        Catch ex As Exception
            MessageBox.Show($"Error loading cart items: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub
    
    Private Sub LoadExistingOrder(orderID As Integer)
        Try
            Using conn As New SqlConnection(_connectionString)
                conn.Open()
                
                ' Load order header
                Dim sqlOrder = "
                    SELECT 
                        OrderNumber, AccountNumber, CustomerName, CustomerPhone,
                        CakeColor, CakePicture, ReadyDate, ReadyTime,
                        SpecialInstructions, Notes, TotalAmount, DepositPaid
                    FROM POS_CustomOrders
                    WHERE OrderID = @OrderID"
                
                Using cmd As New SqlCommand(sqlOrder, conn)
                    cmd.Parameters.AddWithValue("@OrderID", orderID)
                    
                    Using reader = cmd.ExecuteReader()
                        If reader.Read() Then
                            _editOrderNumber = reader("OrderNumber").ToString()
                            _originalDepositPaid = CDec(reader("DepositPaid"))
                            _originalTotalAmount = CDec(reader("TotalAmount"))
                            _customerAccountNumber = If(IsDBNull(reader("AccountNumber")), "", reader("AccountNumber").ToString())
                            
                            ' Populate form fields (will be done after InitializeComponent)
                            ' Store values temporarily
                            Dim orderData As New Dictionary(Of String, Object) From {
                                {"AccountNumber", _customerAccountNumber},
                                {"CustomerName", reader("CustomerName").ToString()},
                                {"CustomerPhone", reader("CustomerPhone").ToString()},
                                {"CakeColor", reader("CakeColor").ToString()},
                                {"CakePicture", reader("CakePicture").ToString()},
                                {"PickupDate", CDate(reader("ReadyDate"))},
                                {"PickupTime", reader("ReadyTime").ToString()},
                                {"SpecialRequests", If(IsDBNull(reader("SpecialInstructions")), "", reader("SpecialInstructions").ToString())},
                                {"Notes", If(IsDBNull(reader("Notes")), "", reader("Notes").ToString())},
                                {"DepositPaid", CDec(reader("DepositPaid"))}
                            }
                            
                            ' Populate controls after they're created
                            AddHandler Me.Load, Sub(s, e)
                                txtAccountNumber.Text = orderData("AccountNumber").ToString()
                                txtCustomerName.Text = orderData("CustomerName").ToString()
                                txtCustomerPhone.Text = orderData("CustomerPhone").ToString()
                                txtCakeColor.Text = orderData("CakeColor").ToString()
                                txtCakePicture.Text = orderData("CakePicture").ToString()
                                dtpCollectionDate.Value = CDate(orderData("PickupDate"))
                                
                                Dim pickupTime = DateTime.Parse(orderData("PickupTime").ToString())
                                dtpCollectionTime.Value = pickupTime
                                
                                txtSpecialRequests.Text = orderData("SpecialRequests").ToString()
                                txtNotes.Text = orderData("Notes").ToString()
                                txtDeposit.Text = CDec(orderData("DepositPaid")).ToString("F2")
                                txtDeposit.ReadOnly = True
                                txtDeposit.BackColor = Color.LightGray
                                
                                ' Update order number label to show existing order number
                                lblOrderNumber.Text = _editOrderNumber
                                lblOrderNumber.ForeColor = Color.Red
                                lblOrderNumber.Font = New Font("Segoe UI", 9, FontStyle.Bold)
                                
                                ' Update title to show edit mode
                                Me.Text = $"EDIT Cake Order #{_editOrderNumber} - {_branchName}"
                            End Sub
                        End If
                    End Using
                End Using
                
                ' Load order items
                Dim sqlItems = "
                    SELECT ProductID, ProductName, Quantity, UnitPrice, LineTotal
                    FROM POS_CustomOrderItems
                    WHERE OrderID = @OrderID"
                
                Using cmd As New SqlCommand(sqlItems, conn)
                    cmd.Parameters.AddWithValue("@OrderID", orderID)
                    
                    Using reader = cmd.ExecuteReader()
                        While reader.Read()
                            Dim item As New OrderItem With {
                                .ProductID = CInt(reader("ProductID")),
                                .Description = reader("ProductName").ToString(),
                                .Quantity = CInt(reader("Quantity")),
                                .UnitPrice = CDec(reader("UnitPrice")),
                                .TotalPrice = CDec(reader("LineTotal"))
                            }
                            _orderItems.Add(item)
                        End While
                    End Using
                End Using
                
                ' Refresh grid and calculate totals
                RefreshItemsGrid()
                CalculateTotals()
            End Using
            
        Catch ex As Exception
            MessageBox.Show($"Error loading order for editing: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub
    
    
    Private Sub InitializeComponent()
        Me.Text = "Cake Order - " & _branchName
        Me.WindowState = FormWindowState.Maximized
        Me.StartPosition = FormStartPosition.CenterScreen
        Me.BackColor = Color.White
        Me.Font = New Font("Segoe UI", 10)
        Me.AutoScroll = True
        Me.MinimumSize = New Size(800, 600)
        Me.FormBorderStyle = FormBorderStyle.None
        Me.MaximizeBox = True
        
        Dim yPos As Integer = 20
        
        ' ===== HEADER SECTION =====
        ' Company Logo/Name (Top Left)
        Dim lblCompany As New Label With {
            .Text = "🍰 Oven Delights",
            .Font = New Font("Segoe UI", 20, FontStyle.Bold),
            .ForeColor = ColorTranslator.FromHtml("#E67E22"),
            .Location = New Point(20, yPos),
            .AutoSize = True
        }
        Me.Controls.Add(lblCompany)
        
        Dim lblTagline As New Label With {
            .Text = "YOUR TRUSTED FAMILY BAKERY",
            .Font = New Font("Segoe UI", 9, FontStyle.Italic),
            .ForeColor = Color.Gray,
            .Location = New Point(20, yPos + 35),
            .AutoSize = True
        }
        Me.Controls.Add(lblTagline)
        
        ' Branch Details (Below tagline)
        Dim lblBranchName As New Label With {
            .Text = _branchName,
            .Font = New Font("Segoe UI", 11, FontStyle.Bold),
            .ForeColor = Color.Black,
            .Location = New Point(20, yPos + 55),
            .AutoSize = True
        }
        Me.Controls.Add(lblBranchName)
        
        If Not String.IsNullOrWhiteSpace(_branchAddress) Then
            Dim lblBranchAddress As New Label With {
                .Text = _branchAddress,
                .Font = New Font("Segoe UI", 9),
                .ForeColor = Color.DarkGray,
                .Location = New Point(20, yPos + 75),
                .AutoSize = True,
                .MaximumSize = New Size(400, 0)
            }
            Me.Controls.Add(lblBranchAddress)
        End If
        
        If Not String.IsNullOrWhiteSpace(_branchPhone) Then
            Dim lblBranchPhone As New Label With {
                .Text = "Tel: " & _branchPhone,
                .Font = New Font("Segoe UI", 9),
                .ForeColor = Color.DarkGray,
                .Location = New Point(20, yPos + 95),
                .AutoSize = True
            }
            Me.Controls.Add(lblBranchPhone)
        End If
        
        ' Cake Details (Top Right)
        Dim xRight As Integer = 550
        
        Dim lblCakeColorLabel As New Label With {
            .Text = "Cake Colour:",
            .Font = New Font("Segoe UI", 10, FontStyle.Bold),
            .Location = New Point(xRight, yPos),
            .Width = 120
        }
        txtCakeColor = New TextBox With {
            .Font = New Font("Segoe UI", 10),
            .Location = New Point(xRight + 125, yPos - 3),
            .Width = 280,
            .BackColor = Color.LightYellow
        }
        Me.Controls.AddRange({lblCakeColorLabel, txtCakeColor})
        yPos += 30
        
        Dim lblCakePictureLabel As New Label With {
            .Text = "Cake Picture:",
            .Font = New Font("Segoe UI", 10, FontStyle.Bold),
            .Location = New Point(xRight, yPos),
            .Width = 120
        }
        txtCakePicture = New TextBox With {
            .Font = New Font("Segoe UI", 10),
            .Location = New Point(xRight + 125, yPos - 3),
            .Width = 280,
            .BackColor = Color.LightYellow
        }
        Me.Controls.AddRange({lblCakePictureLabel, txtCakePicture})
        yPos += 30
        
        Dim lblCollectionDateLabel As New Label With {
            .Text = "Collection Date:",
            .Font = New Font("Segoe UI", 10, FontStyle.Bold),
            .Location = New Point(xRight, yPos),
            .Width = 120
        }
        dtpCollectionDate = New DateTimePicker With {
            .Font = New Font("Segoe UI", 10),
            .Location = New Point(xRight + 125, yPos - 3),
            .Width = 150,
            .Format = DateTimePickerFormat.Short,
            .MinDate = DateTime.Today
        }
        AddHandler dtpCollectionDate.ValueChanged, AddressOf UpdateCollectionDay
        Me.Controls.AddRange({lblCollectionDateLabel, dtpCollectionDate})
        yPos += 30
        
        Dim lblCollectionDayLabel As New Label With {
            .Text = "Collection Day:",
            .Font = New Font("Segoe UI", 10, FontStyle.Bold),
            .Location = New Point(xRight, yPos),
            .Width = 120
        }
        lblCollectionDay = New Label With {
            .Font = New Font("Segoe UI", 10),
            .Location = New Point(xRight + 125, yPos),
            .Width = 150,
            .Text = DateTime.Today.DayOfWeek.ToString()
        }
        Me.Controls.AddRange({lblCollectionDayLabel, lblCollectionDay})
        yPos += 30
        
        Dim lblCollectionTimeLabel As New Label With {
            .Text = "Collection Time:",
            .Font = New Font("Segoe UI", 10, FontStyle.Bold),
            .Location = New Point(xRight, yPos),
            .Width = 120
        }
        dtpCollectionTime = New DateTimePicker With {
            .Font = New Font("Segoe UI", 10),
            .Location = New Point(xRight + 125, yPos - 3),
            .Width = 100,
            .Format = DateTimePickerFormat.Time,
            .ShowUpDown = True,
            .Value = DateTime.Today.AddHours(12)
        }
        Me.Controls.AddRange({lblCollectionTimeLabel, dtpCollectionTime})
        
        yPos = 180
        
        ' ===== CUSTOMER/ACCOUNT SECTION =====
        Dim pnlAccount As New Panel With {
            .Location = New Point(20, yPos),
            .Size = New Size(950, 100),
            .BorderStyle = BorderStyle.FixedSingle,
            .BackColor = Color.FromArgb(255, 250, 240)
        }
        
        Dim lblAccountTitle As New Label With {
            .Text = "PLEASE USE ACCOUNT NO AS REFERENCE",
            .Font = New Font("Segoe UI", 10, FontStyle.Bold),
            .ForeColor = Color.Red,
            .Location = New Point(10, 10),
            .AutoSize = True
        }
        pnlAccount.Controls.Add(lblAccountTitle)
        
        Dim lblAccountNo As New Label With {
            .Text = "ACCOUNT NO:",
            .Font = New Font("Segoe UI", 9, FontStyle.Bold),
            .Location = New Point(10, 35),
            .Width = 100
        }
        txtAccountNumber = New TextBox With {
            .Font = New Font("Segoe UI", 9),
            .Location = New Point(115, 33),
            .Width = 150
        }
        AddHandler txtAccountNumber.Leave, AddressOf txtAccountNumber_Leave
        pnlAccount.Controls.AddRange({lblAccountNo, txtAccountNumber})
        
        Dim lblName As New Label With {
            .Text = "NAME:",
            .Font = New Font("Segoe UI", 9, FontStyle.Bold),
            .Location = New Point(10, 60),
            .Width = 100
        }
        txtCustomerName = New TextBox With {
            .Font = New Font("Segoe UI", 9),
            .Location = New Point(115, 58),
            .Width = 250
        }
        pnlAccount.Controls.AddRange({lblName, txtCustomerName})
        
        Dim lblPhone As New Label With {
            .Text = "CELL NUMBER:",
            .Font = New Font("Segoe UI", 9, FontStyle.Bold),
            .Location = New Point(400, 60),
            .Width = 100
        }
        txtCustomerPhone = New TextBox With {
            .Font = New Font("Segoe UI", 9),
            .Location = New Point(505, 58),
            .Width = 150
        }
        AddHandler txtCustomerPhone.Leave, AddressOf txtCustomerPhone_Leave
        pnlAccount.Controls.AddRange({lblPhone, txtCustomerPhone})
        
        Me.Controls.Add(pnlAccount)
        yPos += 110
        
        ' ===== ORDER INFO SECTION =====
        Dim pnlOrderInfo As New Panel With {
            .Location = New Point(20, yPos),
            .Size = New Size(950, 60),
            .BorderStyle = BorderStyle.FixedSingle
        }
        
        Dim lblCollectionPointLabel As New Label With {
            .Text = "Collection Point",
            .Font = New Font("Segoe UI", 9, FontStyle.Bold),
            .Location = New Point(10, 5),
            .AutoSize = True
        }
        lblCollectionPoint = New Label With {
            .Text = _branchName,
            .Font = New Font("Segoe UI", 9),
            .Location = New Point(10, 25),
            .Width = 200
        }
        pnlOrderInfo.Controls.AddRange({lblCollectionPointLabel, lblCollectionPoint})
        
        Dim lblOrderNumberLabel As New Label With {
            .Text = "Order Number",
            .Font = New Font("Segoe UI", 9, FontStyle.Bold),
            .Location = New Point(250, 5),
            .AutoSize = True
        }
        lblOrderNumber = New Label With {
            .Text = "[Will be generated]",
            .Font = New Font("Segoe UI", 9),
            .Location = New Point(250, 25),
            .Width = 200
        }
        pnlOrderInfo.Controls.AddRange({lblOrderNumberLabel, lblOrderNumber})
        
        Dim lblDateLabel As New Label With {
            .Text = "Date",
            .Font = New Font("Segoe UI", 9, FontStyle.Bold),
            .Location = New Point(500, 5),
            .AutoSize = True
        }
        lblOrderDate = New Label With {
            .Text = DateTime.Now.ToString("dd/MM/yyyy"),
            .Font = New Font("Segoe UI", 9),
            .Location = New Point(500, 25),
            .Width = 150
        }
        pnlOrderInfo.Controls.AddRange({lblDateLabel, lblOrderDate})
        
        Dim lblTakenByLabel As New Label With {
            .Text = "Order Taken By:",
            .Font = New Font("Segoe UI", 9, FontStyle.Bold),
            .Location = New Point(680, 5),
            .AutoSize = True
        }
        lblOrderTakenBy = New Label With {
            .Text = _cashierName,
            .Font = New Font("Segoe UI", 9),
            .Location = New Point(680, 25),
            .Width = 200
        }
        pnlOrderInfo.Controls.AddRange({lblTakenByLabel, lblOrderTakenBy})
        
        Me.Controls.Add(pnlOrderInfo)
        yPos += 70
        
        ' ===== ITEMS GRID =====
        Dim lblItemsTitle As New Label With {
            .Text = "ORDER ITEMS",
            .Font = New Font("Segoe UI", 11, FontStyle.Bold),
            .Location = New Point(20, yPos),
            .AutoSize = True
        }
        Me.Controls.Add(lblItemsTitle)
        yPos += 30
        
        ' Product selection
        Dim lblProduct As New Label With {
            .Text = "Select Product:",
            .Font = New Font("Segoe UI", 9),
            .Location = New Point(20, yPos),
            .Width = 100
        }
        cboProductSearch = New ComboBox With {
            .Font = New Font("Segoe UI", 9),
            .Location = New Point(125, yPos - 3),
            .Width = 350,
            .DropDownStyle = ComboBoxStyle.DropDown,
            .AutoCompleteMode = AutoCompleteMode.SuggestAppend,
            .AutoCompleteSource = AutoCompleteSource.ListItems
        }
        
        Dim lblQty As New Label With {
            .Text = "Qty:",
            .Font = New Font("Segoe UI", 9),
            .Location = New Point(490, yPos),
            .Width = 30
        }
        nudQuantity = New NumericUpDown With {
            .Font = New Font("Segoe UI", 9),
            .Location = New Point(525, yPos - 3),
            .Width = 60,
            .Minimum = 1,
            .Maximum = 999,
            .Value = 1
        }
        
        btnAddItem = New Button With {
            .Text = "➕ ADD ITEM",
            .Font = New Font("Segoe UI", 9, FontStyle.Bold),
            .Location = New Point(600, yPos - 5),
            .Size = New Size(120, 28),
            .BackColor = ColorTranslator.FromHtml("#27AE60"),
            .ForeColor = Color.White,
            .FlatStyle = FlatStyle.Flat
        }
        btnAddItem.FlatAppearance.BorderSize = 0
        AddHandler btnAddItem.Click, AddressOf AddItemToOrder
        
        btnRemoveItem = New Button With {
            .Text = "✖ REMOVE",
            .Font = New Font("Segoe UI", 9, FontStyle.Bold),
            .Location = New Point(730, yPos - 5),
            .Size = New Size(100, 28),
            .BackColor = ColorTranslator.FromHtml("#E74C3C"),
            .ForeColor = Color.White,
            .FlatStyle = FlatStyle.Flat
        }
        btnRemoveItem.FlatAppearance.BorderSize = 0
        AddHandler btnRemoveItem.Click, AddressOf RemoveSelectedItem
        
        Me.Controls.AddRange({lblProduct, cboProductSearch, lblQty, nudQuantity, btnAddItem, btnRemoveItem})
        yPos += 35
        
        ' DataGridView for items
        dgvItems = New DataGridView With {
            .Location = New Point(20, yPos),
            .Size = New Size(950, 180),
            .AllowUserToAddRows = False,
            .AllowUserToDeleteRows = False,
            .ReadOnly = True,
            .SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            .MultiSelect = False,
            .BackgroundColor = Color.White,
            .BorderStyle = BorderStyle.FixedSingle,
            .ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing,
            .ColumnHeadersHeight = 35,
            .RowTemplate = New DataGridViewRow With {.Height = 30}
        }
        
        dgvItems.Columns.Add(New DataGridViewTextBoxColumn With {
            .Name = "ProductID",
            .HeaderText = "ID",
            .Visible = False
        })
        dgvItems.Columns.Add(New DataGridViewTextBoxColumn With {
            .Name = "Description",
            .HeaderText = "Item Description",
            .Width = 500
        })
        dgvItems.Columns.Add(New DataGridViewTextBoxColumn With {
            .Name = "Quantity",
            .HeaderText = "Qty Required",
            .Width = 100,
            .DefaultCellStyle = New DataGridViewCellStyle With {.Alignment = DataGridViewContentAlignment.MiddleCenter}
        })
        dgvItems.Columns.Add(New DataGridViewTextBoxColumn With {
            .Name = "UnitPrice",
            .HeaderText = "Unit Price (Incl)",
            .Width = 150,
            .DefaultCellStyle = New DataGridViewCellStyle With {.Format = "F2", .Alignment = DataGridViewContentAlignment.MiddleRight}
        })
        dgvItems.Columns.Add(New DataGridViewTextBoxColumn With {
            .Name = "TotalPrice",
            .HeaderText = "Total Price",
            .Width = 150,
            .DefaultCellStyle = New DataGridViewCellStyle With {.Format = "F2", .Alignment = DataGridViewContentAlignment.MiddleRight}
        })
        
        dgvItems.ColumnHeadersDefaultCellStyle.Font = New Font("Segoe UI", 9, FontStyle.Bold)
        dgvItems.ColumnHeadersDefaultCellStyle.BackColor = ColorTranslator.FromHtml("#34495E")
        dgvItems.ColumnHeadersDefaultCellStyle.ForeColor = Color.White
        
        Me.Controls.Add(dgvItems)
        yPos += 190
        
        ' ===== SPECIAL REQUESTS SECTION =====
        Dim lblSpecialTitle As New Label With {
            .Text = "SPECIAL REQUESTS / CAKE OPTIONS",
            .Font = New Font("Segoe UI", 10, FontStyle.Bold),
            .ForeColor = ColorTranslator.FromHtml("#E67E22"),
            .Location = New Point(20, yPos),
            .AutoSize = True
        }
        Me.Controls.Add(lblSpecialTitle)
        yPos += 25
        
        cboSpecialRequests = New ComboBox With {
            .Font = New Font("Segoe UI", 9),
            .Location = New Point(20, yPos),
            .Width = 400,
            .DropDownStyle = ComboBoxStyle.DropDown
        }
        cboSpecialRequests.Items.AddRange(New String() {
            "Double vanilla", "Double choc", "Eggless", "Figure only", "Figure on base",
            "Blackforest", "Red velvet", "Milkybar", "Bar one", "Ferrero",
            "Carrot cake", "Heart shape", "Bible", "Tiered", "Mould",
            "Doll cake", "Soccer field", "1mx 500"
        })
        
        btnAddRequest = New Button With {
            .Text = "➕ ADD",
            .Font = New Font("Segoe UI", 9, FontStyle.Bold),
            .Location = New Point(430, yPos - 2),
            .Size = New Size(80, 26),
            .BackColor = ColorTranslator.FromHtml("#27AE60"),
            .ForeColor = Color.White,
            .FlatStyle = FlatStyle.Flat
        }
        btnAddRequest.FlatAppearance.BorderSize = 0
        AddHandler btnAddRequest.Click, AddressOf AddSpecialRequest
        
        Me.Controls.AddRange({cboSpecialRequests, btnAddRequest})
        yPos += 30
        
        txtSpecialRequests = New TextBox With {
            .Font = New Font("Segoe UI", 9),
            .Location = New Point(20, yPos),
            .Size = New Size(950, 60),
            .Multiline = True,
            .ScrollBars = ScrollBars.Vertical,
            .BackColor = Color.LightYellow,
            .BorderStyle = BorderStyle.FixedSingle
        }
        Me.Controls.Add(txtSpecialRequests)
        yPos += 70
        
        ' ===== NOTES SECTION =====
        Dim lblNotesTitle As New Label With {
            .Text = "NOTES",
            .Font = New Font("Segoe UI", 10, FontStyle.Bold),
            .ForeColor = ColorTranslator.FromHtml("#E67E22"),
            .Location = New Point(20, yPos),
            .AutoSize = True
        }
        Me.Controls.Add(lblNotesTitle)
        yPos += 25
        
        txtNotes = New TextBox With {
            .Font = New Font("Segoe UI", 9),
            .Location = New Point(20, yPos),
            .Size = New Size(950, 50),
            .Multiline = True,
            .ScrollBars = ScrollBars.Vertical,
            .BackColor = Color.White,
            .BorderStyle = BorderStyle.FixedSingle
        }
        Me.Controls.Add(txtNotes)
        yPos += 60
        
        ' ===== FOOTER SECTION =====
        Dim pnlFooter As New Panel With {
            .Location = New Point(20, yPos),
            .Size = New Size(950, 80),
            .BorderStyle = BorderStyle.FixedSingle,
            .BackColor = Color.FromArgb(245, 245, 245)
        }
        
        ' Service charges (Left)
        Dim lblServiceCharges As New Label With {
            .Text = "All same day orders and cancellations will attract a" & vbCrLf &
                   "R30.00 service charge" & vbCrLf &
                   "All changes to size, cream and date - R20.00 service charge",
            .Font = New Font("Segoe UI", 8),
            .Location = New Point(10, 10),
            .Size = New Size(400, 60)
        }
        pnlFooter.Controls.Add(lblServiceCharges)
        
        ' Totals (Right)
        Dim xTotals As Integer = 550
        
        Dim lblInvoiceTotalLabel As New Label With {
            .Text = "Invoice Total:",
            .Font = New Font("Segoe UI", 10, FontStyle.Bold),
            .Location = New Point(xTotals, 10),
            .Width = 150
        }
        lblInvoiceTotal = New Label With {
            .Text = "R 0.00",
            .Font = New Font("Segoe UI", 11, FontStyle.Bold),
            .ForeColor = ColorTranslator.FromHtml("#27AE60"),
            .Location = New Point(xTotals + 160, 10),
            .Width = 150,
            .TextAlign = ContentAlignment.MiddleRight
        }
        pnlFooter.Controls.AddRange({lblInvoiceTotalLabel, lblInvoiceTotal})
        
        Dim lblDepositLabel As New Label With {
            .Text = "Deposit Paid:",
            .Font = New Font("Segoe UI", 10, FontStyle.Bold),
            .Location = New Point(xTotals, 35),
            .Width = 150
        }
        txtDeposit = New TextBox With {
            .Font = New Font("Segoe UI", 11, FontStyle.Bold),
            .Location = New Point(xTotals + 160, 35),
            .Width = 150,
            .Text = "0.00",
            .TextAlign = HorizontalAlignment.Right
        }
        AddHandler txtDeposit.TextChanged, AddressOf CalculateBalance
        pnlFooter.Controls.AddRange({lblDepositLabel, txtDeposit})
        
        Dim lblBalanceLabel As New Label With {
            .Text = "Balance Owing:",
            .Font = New Font("Segoe UI", 10, FontStyle.Bold),
            .Location = New Point(xTotals, 60),
            .Width = 150
        }
        lblBalance = New Label With {
            .Text = "R 0.00",
            .Font = New Font("Segoe UI", 11, FontStyle.Bold),
            .ForeColor = ColorTranslator.FromHtml("#E74C3C"),
            .Location = New Point(xTotals + 160, 60),
            .Width = 150,
            .TextAlign = ContentAlignment.MiddleRight
        }
        pnlFooter.Controls.AddRange({lblBalanceLabel, lblBalance})
        
        Me.Controls.Add(pnlFooter)
        
        ' ===== ACTION BUTTONS =====
        yPos += 90
        
        btnPrintPreview = New Button With {
            .Text = "🖨️ PRINT PREVIEW",
            .Font = New Font("Segoe UI", 11, FontStyle.Bold),
            .Location = New Point(20, yPos),
            .Size = New Size(200, 45),
            .BackColor = ColorTranslator.FromHtml("#3498DB"),
            .ForeColor = Color.White,
            .FlatStyle = FlatStyle.Flat
        }
        btnPrintPreview.FlatAppearance.BorderSize = 0
        AddHandler btnPrintPreview.Click, AddressOf PrintPreview
        
        btnAcceptOrder = New Button With {
            .Text = "✓ ACCEPT & SAVE ORDER",
            .Font = New Font("Segoe UI", 11, FontStyle.Bold),
            .Location = New Point(240, yPos),
            .Size = New Size(250, 45),
            .BackColor = ColorTranslator.FromHtml("#27AE60"),
            .ForeColor = Color.White,
            .FlatStyle = FlatStyle.Flat
        }
        btnAcceptOrder.FlatAppearance.BorderSize = 0
        AddHandler btnAcceptOrder.Click, AddressOf AcceptOrder
        
        btnCancel = New Button With {
            .Text = "✗ CANCEL",
            .Font = New Font("Segoe UI", 11, FontStyle.Bold),
            .Location = New Point(510, yPos),
            .Size = New Size(150, 45),
            .BackColor = ColorTranslator.FromHtml("#95A5A6"),
            .ForeColor = Color.White,
            .FlatStyle = FlatStyle.Flat
        }
        btnCancel.FlatAppearance.BorderSize = 0
        AddHandler btnCancel.Click, Sub() Me.Close()
        
        Me.Controls.AddRange({btnPrintPreview, btnAcceptOrder, btnCancel})
        
        Me.WindowState = FormWindowState.Maximized
    End Sub
    
    Private Sub LoadProducts()
        Try
            Using conn As New SqlConnection(_connectionString)
                conn.Open()
                Dim sql = "SELECT DISTINCT p.ProductID, p.Name, ISNULL(pr.SellingPrice, 0) AS Price 
                          FROM Demo_Retail_Product p
                          LEFT JOIN Demo_Retail_Price pr ON p.ProductID = pr.ProductID AND pr.BranchID = @BranchID
                          WHERE p.IsActive = 1 
                          AND (p.ProductType = 'External' OR p.ProductType = 'Internal')
                          AND p.Category NOT LIKE '%ingredient%'
                          AND p.Category NOT LIKE '%consumable%'
                          AND p.Category NOT LIKE '%pack%'
                          ORDER BY p.Name"
                
                Using cmd As New SqlCommand(sql, conn)
                    cmd.Parameters.AddWithValue("@BranchID", _branchID)
                    Using reader = cmd.ExecuteReader()
                        cboProductSearch.Items.Clear()
                        While reader.Read()
                            Dim item As New ProductItem With {
                                .ProductID = CInt(reader("ProductID")),
                                .Name = reader("Name").ToString(),
                                .Price = CDec(reader("Price"))
                            }
                            cboProductSearch.Items.Add(item)
                        End While
                    End Using
                End Using
            End Using
        Catch ex As Exception
            MessageBox.Show($"Error loading products: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub
    
    Private Sub txtAccountNumber_Leave(sender As Object, e As EventArgs)
        If Not String.IsNullOrWhiteSpace(txtAccountNumber.Text) Then
            LookupCustomer(txtAccountNumber.Text.Trim())
        End If
    End Sub
    
    Private Sub txtCustomerPhone_Leave(sender As Object, e As EventArgs)
        If Not String.IsNullOrWhiteSpace(txtCustomerPhone.Text) Then
            LookupCustomer(txtCustomerPhone.Text.Trim())
        End If
    End Sub
    
    Private Sub LookupCustomer(cellNumber As String)
        Try
            ' Only lookup if cell number is valid (at least 10 digits)
            If cellNumber.Length < 10 Then
                Return
            End If
            
            Using conn As New SqlConnection(_connectionString)
                conn.Open()
                Dim sql = "SELECT FirstName, Surname FROM POS_Customers WHERE CellNumber = @CellNumber"
                Using cmd As New SqlCommand(sql, conn)
                    cmd.Parameters.AddWithValue("@CellNumber", cellNumber)
                    Using reader = cmd.ExecuteReader()
                        If reader.Read() Then
                            ' Customer found - auto-populate with visual feedback
                            Dim firstName = reader("FirstName").ToString()
                            Dim surname = reader("Surname").ToString()
                            txtCustomerName.Text = $"{firstName} {surname}"
                            txtAccountNumber.Text = cellNumber
                            
                            ' Visual feedback - flash the field green briefly
                            txtCustomerName.BackColor = Color.LightGreen
                            txtAccountNumber.BackColor = Color.LightGreen
                            
                            ' Reset color after 1 second
                            Dim timer As New Timer()
                            timer.Interval = 1000
                            AddHandler timer.Tick, Sub()
                                txtCustomerName.BackColor = Color.White
                                txtAccountNumber.BackColor = Color.White
                                timer.Stop()
                                timer.Dispose()
                            End Sub
                            timer.Start()
                        End If
                    End Using
                End Using
            End Using
        Catch ex As Exception
            ' Log error for debugging
            System.Diagnostics.Debug.WriteLine($"Customer lookup error: {ex.Message}")
            MessageBox.Show($"Customer lookup error: {ex.Message}", "Debug", MessageBoxButtons.OK, MessageBoxIcon.Information)
        End Try
    End Sub
    
    Private Class ProductItem
        Public Property ProductID As Integer
        Public Property Name As String
        Public Property Price As Decimal
        
        Public Overrides Function ToString() As String
            Return $"{Name} - R{Price:F2}"
        End Function
    End Class
    
    Private Sub AddItemToOrder(sender As Object, e As EventArgs)
        Try
            If cboProductSearch.SelectedItem Is Nothing Then
                MessageBox.Show("Please select a product.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                Return
            End If
            
            Dim product = CType(cboProductSearch.SelectedItem, ProductItem)
            Dim qty = CInt(nudQuantity.Value)
            Dim total = product.Price * qty
            
            Dim item As New OrderItem With {
                .ProductID = product.ProductID,
                .Description = product.Name,
                .Quantity = qty,
                .UnitPrice = product.Price,
                .TotalPrice = total
            }
            
            _orderItems.Add(item)
            RefreshItemsGrid()
            CalculateTotals()
            
            cboProductSearch.SelectedIndex = -1
            nudQuantity.Value = 1
            
        Catch ex As Exception
            MessageBox.Show($"Error adding item: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub
    
    Private Sub RemoveSelectedItem(sender As Object, e As EventArgs)
        Try
            If dgvItems.SelectedRows.Count = 0 Then
                MessageBox.Show("Please select an item to remove.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                Return
            End If
            
            Dim index = dgvItems.SelectedRows(0).Index
            _orderItems.RemoveAt(index)
            RefreshItemsGrid()
            CalculateTotals()
            
        Catch ex As Exception
            MessageBox.Show($"Error removing item: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub
    
    Private Sub RefreshItemsGrid()
        dgvItems.Rows.Clear()
        For Each item In _orderItems
            dgvItems.Rows.Add(item.ProductID, item.Description, item.Quantity, item.UnitPrice, item.TotalPrice)
        Next
    End Sub
    
    Private Sub CalculateTotals()
        _totalAmount = _orderItems.Sum(Function(i) i.TotalPrice)
        lblInvoiceTotal.Text = $"R {_totalAmount:F2}"
        CalculateBalance(Nothing, Nothing)
    End Sub
    
    Private Sub CalculateBalance(sender As Object, e As EventArgs)
        Decimal.TryParse(txtDeposit.Text, _depositAmount)
        _balanceAmount = _totalAmount - _depositAmount
        lblBalance.Text = $"R {_balanceAmount:F2}"
    End Sub
    
    Private Sub AddSpecialRequest(sender As Object, e As EventArgs)
        Try
            Dim requestText = cboSpecialRequests.Text.Trim()
            If String.IsNullOrWhiteSpace(requestText) Then
                MessageBox.Show("Please enter or select a special request.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                Return
            End If
            
            If String.IsNullOrWhiteSpace(txtSpecialRequests.Text) Then
                txtSpecialRequests.Text = requestText
            Else
                txtSpecialRequests.Text &= vbCrLf & requestText
            End If
            
            cboSpecialRequests.Text = ""
            cboSpecialRequests.Focus()
        Catch ex As Exception
            MessageBox.Show($"Error adding special request: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub
    
    Private Sub UpdateCollectionDay(sender As Object, e As EventArgs)
        lblCollectionDay.Text = dtpCollectionDate.Value.DayOfWeek.ToString()
    End Sub
    
    Private Sub PrintPreview(sender As Object, e As EventArgs)
        Try
            If _orderItems.Count = 0 Then
                MessageBox.Show("Please add at least one item to preview.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                Return
            End If
            
            ' Calculate totals for preview
            CalculateTotals()
            
            ' Build preview data
            Dim printData = BuildPrintData("[PREVIEW]", 0)
            Dim printer As New CakeOrderPrinter(printData)
            printer.ShowPrintPreview()
            
        Catch ex As Exception
            MessageBox.Show($"Error showing print preview: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub
    
    Private Function GetConfiguredPrinter() As String
        Try
            Using conn As New SqlConnection(_connectionString)
                conn.Open()
                Dim sql = "SELECT PrinterName FROM PrinterConfig WHERE BranchID = @BranchID"
                Using cmd As New SqlCommand(sql, conn)
                    cmd.Parameters.AddWithValue("@BranchID", _branchID)
                    Dim result = cmd.ExecuteScalar()
                    If result IsNot Nothing AndAlso Not IsDBNull(result) Then
                        Return result.ToString()
                    End If
                End Using
            End Using
        Catch ex As Exception
            System.Diagnostics.Debug.WriteLine($"Error getting configured printer: {ex.Message}")
        End Try
        
        ' Return default printer if no config found
        Dim defaultPrinter As New PrinterSettings()
        Return defaultPrinter.PrinterName
    End Function
    
    Private Sub SaveCustomerToDatabase()
        ' Save new customer to POS_Customers table
        Try
            Using conn As New SqlConnection(_connectionString)
                conn.Open()
                Dim sql = "IF NOT EXISTS (SELECT 1 FROM POS_Customers WHERE CellNumber = @CellNumber) " &
                         "INSERT INTO POS_Customers (CellNumber, FirstName, Surname, LastOrderDate, TotalOrders, IsActive) " &
                         "VALUES (@CellNumber, @FirstName, @Surname, GETDATE(), 1, 1) " &
                         "ELSE " &
                         "UPDATE POS_Customers SET LastOrderDate = GETDATE(), TotalOrders = TotalOrders + 1 WHERE CellNumber = @CellNumber"
                Using cmd As New SqlCommand(sql, conn)
                    ' Split name into first and surname
                    Dim fullName = txtCustomerName.Text.Trim()
                    Dim nameParts = fullName.Split(" "c)
                    Dim firstName = If(nameParts.Length > 0, nameParts(0), fullName)
                    Dim surname = If(nameParts.Length > 1, String.Join(" ", nameParts.Skip(1)), "")
                    
                    cmd.Parameters.AddWithValue("@CellNumber", txtCustomerPhone.Text.Trim())
                    cmd.Parameters.AddWithValue("@FirstName", firstName)
                    cmd.Parameters.AddWithValue("@Surname", surname)
                    cmd.ExecuteNonQuery()
                End Using
            End Using
        Catch ex As Exception
            ' Silently fail - not critical
            System.Diagnostics.Debug.WriteLine($"Error saving customer: {ex.Message}")
        End Try
    End Sub
    
    Private Sub PrintTillSlip(orderNumber As String, orderID As Integer, Optional paymentMethod As String = "Cash", Optional cardMaskedPan As String = "", Optional cardType As String = "", Optional cardApprovalCode As String = "")
        Try
            ' Print twice: Customer Copy + Merchant Copy
            PrintSingleTillSlip(orderNumber, orderID, paymentMethod, cardMaskedPan, cardType, cardApprovalCode, "CUSTOMER COPY")
            PrintSingleTillSlip(orderNumber, orderID, paymentMethod, cardMaskedPan, cardType, cardApprovalCode, "MERCHANT COPY")
        Catch ex As Exception
            Throw New Exception($"Till slip print error: {ex.Message}", ex)
        End Try
    End Sub
    
    Private Sub PrintSingleTillSlip(orderNumber As String, orderID As Integer, paymentMethod As String, cardMaskedPan As String, cardType As String, cardApprovalCode As String, copyType As String)
        Try
            ' Create standard POS till slip - ALL BOLD FONTS
            Dim printDoc As New PrintDocument()
            
            AddHandler printDoc.PrintPage, Sub(sender As Object, e As PrintPageEventArgs)
                Dim g = e.Graphics
                Dim fontBold As New Font("Courier New", 8, FontStyle.Bold)
                Dim fontLarge As New Font("Courier New", 11, FontStyle.Bold)
                Dim yPos As Single = 5
                Dim leftMargin As Single = 5
                
                ' Store header - centered
                Dim headerText = "OVEN DELIGHTS"
                Dim headerSize = g.MeasureString(headerText, fontLarge)
                g.DrawString(headerText, fontLarge, Brushes.Black, (302 - headerSize.Width) / 2, yPos)
                yPos += 22
                
                ' Branch info with full address
                g.DrawString(_branchName, fontBold, Brushes.Black, leftMargin, yPos)
                yPos += 14
                
                ' VAT Number
                g.DrawString("Vat Number        4150166793", fontBold, Brushes.Black, leftMargin, yPos)
                yPos += 14
                
                ' VAT Registration
                g.DrawString("Vat Registration  CK 99/65000/23", fontBold, Brushes.Black, leftMargin, yPos)
                yPos += 14
                
                ' Telephone
                g.DrawString("Telephone         0314019942", fontBold, Brushes.Black, leftMargin, yPos)
                yPos += 18
                
                ' ORDER EDITED NOTICE (BIG BOLD)
                If _isEditMode AndAlso _orderEditedDate <> Nothing Then
                    Dim editFont As New Font("Courier New", 10, FontStyle.Bold)
                    Dim editText As String = $"*** ORDER EDITED ON ***"
                    Dim editText2 As String = $"{_orderEditedDate:dd/MM/yyyy HH:mm}"
                    Dim editSize = g.MeasureString(editText, editFont)
                    Dim editSize2 = g.MeasureString(editText2, editFont)
                    g.DrawString(editText, editFont, Brushes.Black, (302 - editSize.Width) / 2, yPos)
                    yPos += 14
                    g.DrawString(editText2, editFont, Brushes.Black, (302 - editSize2.Width) / 2, yPos)
                    yPos += 18
                End If
                
                ' Copy type - centered
                Dim copySize = g.MeasureString(copyType, fontBold)
                g.DrawString(copyType, fontBold, Brushes.Black, (302 - copySize.Width) / 2, yPos)
                yPos += 15
                
                ' Date and time
                g.DrawString(DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss"), fontBold, Brushes.Black, leftMargin, yPos)
                yPos += 15
                
                ' Order number
                g.DrawString($"Order #: {orderNumber}", fontBold, Brushes.Black, leftMargin, yPos)
                yPos += 15
                
                ' Account number with reference message
                If Not String.IsNullOrWhiteSpace(txtAccountNumber.Text) Then
                    g.DrawString($"Account #: {txtAccountNumber.Text.Trim()}", fontBold, Brushes.Black, leftMargin, yPos)
                    yPos += 14
                    g.DrawString("Please use account number as reference", fontBold, Brushes.Black, leftMargin, yPos)
                    yPos += 18
                Else
                    yPos += 3
                End If
                
                ' Cashier
                g.DrawString($"Cashier: {_cashierName}", fontBold, Brushes.Black, leftMargin, yPos)
                yPos += 18
                
                ' Separator
                g.DrawString("======================================", fontBold, Brushes.Black, leftMargin, yPos)
                yPos += 15
                
                ' Order header - centered
                Dim orderText = "CAKE ORDER RECEIPT"
                Dim orderSize = g.MeasureString(orderText, fontBold)
                g.DrawString(orderText, fontBold, Brushes.Black, (302 - orderSize.Width) / 2, yPos)
                yPos += 18
                
                ' Separator
                g.DrawString("======================================", fontBold, Brushes.Black, leftMargin, yPos)
                yPos += 15
                
                ' Customer details
                g.DrawString("CUSTOMER:", fontBold, Brushes.Black, leftMargin, yPos)
                yPos += 14
                g.DrawString(txtCustomerName.Text.Trim(), fontBold, Brushes.Black, leftMargin, yPos)
                yPos += 14
                g.DrawString($"Phone: {txtCustomerPhone.Text.Trim()}", fontBold, Brushes.Black, leftMargin, yPos)
                yPos += 18
                
                ' Collection details
                g.DrawString("READY FOR COLLECTION:", fontBold, Brushes.Black, leftMargin, yPos)
                yPos += 14
                g.DrawString($"Date: {dtpCollectionDate.Value:dd/MM/yyyy}", fontBold, Brushes.Black, leftMargin, yPos)
                yPos += 14
                g.DrawString($"Time: {dtpCollectionTime.Value:HH:mm}", fontBold, Brushes.Black, leftMargin, yPos)
                yPos += 14
                g.DrawString($"*** {lblCollectionDay.Text.ToUpper()} ***", fontBold, Brushes.Black, leftMargin, yPos)
                yPos += 18
                
                ' Cake details
                If Not String.IsNullOrWhiteSpace(txtCakeColor.Text) Then
                    g.DrawString("CAKE COLOR:", fontBold, Brushes.Black, leftMargin, yPos)
                    yPos += 14
                    g.DrawString(txtCakeColor.Text.Trim(), fontBold, Brushes.Black, leftMargin, yPos)
                    yPos += 18
                End If
                
                If Not String.IsNullOrWhiteSpace(txtCakePicture.Text) Then
                    g.DrawString("CAKE IMAGE:", fontBold, Brushes.Black, leftMargin, yPos)
                    yPos += 14
                    g.DrawString(txtCakePicture.Text.Trim(), fontBold, Brushes.Black, leftMargin, yPos)
                    yPos += 18
                End If
                
                ' Special instructions
                If Not String.IsNullOrWhiteSpace(txtSpecialRequests.Text) Then
                    g.DrawString("SPECIAL INSTRUCTIONS:", fontBold, Brushes.Black, leftMargin, yPos)
                    yPos += 14
                    g.DrawString(txtSpecialRequests.Text.Trim(), fontBold, Brushes.Black, leftMargin, yPos)
                    yPos += 18
                End If
                
                ' Notes
                If Not String.IsNullOrWhiteSpace(txtNotes.Text) Then
                    g.DrawString("NOTES:", fontBold, Brushes.Black, leftMargin, yPos)
                    yPos += 14
                    g.DrawString(txtNotes.Text.Trim(), fontBold, Brushes.Black, leftMargin, yPos)
                    yPos += 18
                End If
                
                ' Separator
                g.DrawString("======================================", fontBold, Brushes.Black, leftMargin, yPos)
                yPos += 15
                
                ' Items
                For Each item In _orderItems
                    g.DrawString($"{item.Quantity:0.00} x {item.Description}", fontBold, Brushes.Black, leftMargin, yPos)
                    yPos += 14
                    g.DrawString($"    @ R{item.UnitPrice:N2} = R{item.TotalPrice:N2}", fontBold, Brushes.Black, leftMargin, yPos)
                    yPos += 14
                Next
                
                yPos += 5
                g.DrawString("--------------------------------------", fontBold, Brushes.Black, leftMargin, yPos)
                yPos += 15
                
                ' Calculate VAT breakdown (prices are VAT-inclusive)
                Dim subtotalExclVAT = Math.Round(_totalAmount / 1.15D, 2)
                Dim vatAmount = Math.Round(_totalAmount - subtotalExclVAT, 2)
                
                ' Right-aligned figures
                g.DrawString("Subtotal (excl VAT):", fontBold, Brushes.Black, leftMargin, yPos)
                g.DrawString($"R {subtotalExclVAT:N2}", fontBold, Brushes.Black, 220, yPos)
                yPos += 14
                g.DrawString("VAT (15%):", fontBold, Brushes.Black, leftMargin, yPos)
                g.DrawString($"R {vatAmount:N2}", fontBold, Brushes.Black, 220, yPos)
                yPos += 14
                g.DrawString("Total Amount:", fontBold, Brushes.Black, leftMargin, yPos)
                g.DrawString($"R {_totalAmount:N2}", fontBold, Brushes.Black, 220, yPos)
                yPos += 14
                g.DrawString("Deposit Paid:", fontBold, Brushes.Black, leftMargin, yPos)
                g.DrawString($"R {_depositAmount:N2}", fontBold, Brushes.Black, 220, yPos)
                yPos += 14
                g.DrawString($"Payment: {paymentMethod}", fontBold, Brushes.Black, leftMargin, yPos)
                yPos += 14
                
                ' Show card details if payment was by card
                If (paymentMethod = "CARD" OrElse paymentMethod = "SPLIT") AndAlso Not String.IsNullOrEmpty(cardMaskedPan) Then
                    g.DrawString($"Card: {cardMaskedPan}", fontBold, Brushes.Black, leftMargin, yPos)
                    yPos += 14
                    If Not String.IsNullOrEmpty(cardType) Then
                        g.DrawString($"Card Type: {cardType}", fontBold, Brushes.Black, leftMargin, yPos)
                        yPos += 14
                    End If
                    If Not String.IsNullOrEmpty(cardApprovalCode) Then
                        g.DrawString($"Approval: {cardApprovalCode}", fontBold, Brushes.Black, leftMargin, yPos)
                        yPos += 14
                    End If
                End If
                
                g.DrawString("Balance Due:", fontBold, Brushes.Black, leftMargin, yPos)
                g.DrawString($"R {_balanceAmount:N2}", fontBold, Brushes.Black, 220, yPos)
                yPos += 18
                
                g.DrawString("======================================", fontBold, Brushes.Black, leftMargin, yPos)
                yPos += 15
                
                ' Barcode for order collection
                Try
                    Dim barcodeImage = BarcodeGenerator.GenerateCode39Barcode(orderNumber, 180, 60)
                    g.DrawImage(barcodeImage, CInt((302 - 180) / 2), CInt(yPos))
                    yPos += 65
                    barcodeImage.Dispose()
                Catch ex As Exception
                    Dim orderNumFont As New Font("Arial", 20, FontStyle.Bold)
                    Dim orderNumSize = g.MeasureString(orderNumber, orderNumFont)
                    g.DrawString(orderNumber, orderNumFont, Brushes.Black, (302 - orderNumSize.Width) / 2, yPos)
                    yPos += 28
                End Try
                
                ' Footer - centered
                Dim footer1 = "SCAN BARCODE TO COLLECT"
                Dim footer1Size = g.MeasureString(footer1, fontBold)
                g.DrawString(footer1, fontBold, Brushes.Black, (302 - footer1Size.Width) / 2, yPos)
                yPos += 14
                
                Dim footer2 = "PLEASE BRING THIS RECEIPT"
                Dim footer2Size = g.MeasureString(footer2, fontBold)
                g.DrawString(footer2, fontBold, Brushes.Black, (302 - footer2Size.Width) / 2, yPos)
                
                e.HasMorePages = False
            End Sub
            
            ' Set to use default receipt printer (usually 80mm thermal)
            printDoc.DefaultPageSettings.PaperSize = New PaperSize("Receipt", 315, 1000) ' 80mm width
            printDoc.Print()
            
        Catch ex As Exception
            Throw New Exception($"Till slip print error: {ex.Message}", ex)
        End Try
    End Sub
    
    Private Sub PrintManufacturerSummary(orderNumber As String)
        Try
            Dim printDoc As New PrintDocument()
            
            AddHandler printDoc.PrintPage, Sub(sender As Object, e As PrintPageEventArgs)
                Dim g = e.Graphics
                Dim fontBold As New Font("Courier New", 8, FontStyle.Bold)
                Dim fontLarge As New Font("Courier New", 11, FontStyle.Bold)
                Dim yPos As Single = 5
                Dim leftMargin As Single = 5
                
                ' Header
                Dim headerText = "MANUFACTURER SUMMARY"
                Dim headerSize = g.MeasureString(headerText, fontLarge)
                g.DrawString(headerText, fontLarge, Brushes.Black, (302 - headerSize.Width) / 2, yPos)
                yPos += 22
                
                g.DrawString("======================================", fontBold, Brushes.Black, leftMargin, yPos)
                yPos += 15
                
                ' ORDER EDITED NOTICE (BIG BOLD)
                If _isEditMode AndAlso _orderEditedDate <> Nothing Then
                    Dim editFont As New Font("Courier New", 10, FontStyle.Bold)
                    Dim editText As String = $"*** ORDER EDITED ON ***"
                    Dim editText2 As String = $"{_orderEditedDate:dd/MM/yyyy HH:mm}"
                    Dim editSize = g.MeasureString(editText, editFont)
                    Dim editSize2 = g.MeasureString(editText2, editFont)
                    g.DrawString(editText, editFont, Brushes.Black, (302 - editSize.Width) / 2, yPos)
                    yPos += 14
                    g.DrawString(editText2, editFont, Brushes.Black, (302 - editSize2.Width) / 2, yPos)
                    yPos += 18
                End If
                
                ' Order number
                g.DrawString($"Order #: {orderNumber}", fontBold, Brushes.Black, leftMargin, yPos)
                yPos += 18
                
                ' Customer name
                g.DrawString("CUSTOMER:", fontBold, Brushes.Black, leftMargin, yPos)
                yPos += 14
                g.DrawString(txtCustomerName.Text.Trim(), fontBold, Brushes.Black, leftMargin, yPos)
                yPos += 18
                
                ' DUE DATE AND TIME - PROMINENT
                g.DrawString("*** DUE DATE & TIME ***", fontLarge, Brushes.Black, leftMargin, yPos)
                yPos += 18
                g.DrawString($"Date: {dtpCollectionDate.Value:dd/MM/yyyy}", fontBold, Brushes.Black, leftMargin, yPos)
                yPos += 14
                g.DrawString($"Time: {dtpCollectionTime.Value:HH:mm}", fontBold, Brushes.Black, leftMargin, yPos)
                yPos += 14
                g.DrawString($"Day: {lblCollectionDay.Text.ToUpper()}", fontBold, Brushes.Black, leftMargin, yPos)
                yPos += 18
                
                ' Cake details
                If Not String.IsNullOrWhiteSpace(txtCakeColor.Text) Then
                    g.DrawString("CAKE COLOR:", fontBold, Brushes.Black, leftMargin, yPos)
                    yPos += 14
                    g.DrawString(txtCakeColor.Text.Trim(), fontBold, Brushes.Black, leftMargin, yPos)
                    yPos += 18
                End If
                
                If Not String.IsNullOrWhiteSpace(txtCakePicture.Text) Then
                    g.DrawString("CAKE IMAGE:", fontBold, Brushes.Black, leftMargin, yPos)
                    yPos += 14
                    g.DrawString(txtCakePicture.Text.Trim(), fontBold, Brushes.Black, leftMargin, yPos)
                    yPos += 18
                End If
                
                ' Special instructions
                If Not String.IsNullOrWhiteSpace(txtSpecialRequests.Text) Then
                    g.DrawString("SPECIAL INSTRUCTIONS:", fontBold, Brushes.Black, leftMargin, yPos)
                    yPos += 14
                    g.DrawString(txtSpecialRequests.Text.Trim(), fontBold, Brushes.Black, leftMargin, yPos)
                    yPos += 18
                End If
                
                g.DrawString("======================================", fontBold, Brushes.Black, leftMargin, yPos)
                yPos += 15
                
                ' Items - NO PRICES
                g.DrawString("ITEMS TO PREPARE:", fontBold, Brushes.Black, leftMargin, yPos)
                yPos += 15
                
                For Each item In _orderItems
                    g.DrawString($"{item.Quantity:0.00} x {item.Description}", fontBold, Brushes.Black, leftMargin, yPos)
                    yPos += 14
                Next
                
                yPos += 5
                g.DrawString("======================================", fontBold, Brushes.Black, leftMargin, yPos)
                yPos += 15
                
                ' Footer
                Dim footer = "FOR MANUFACTURER USE ONLY"
                Dim footerSize = g.MeasureString(footer, fontBold)
                g.DrawString(footer, fontBold, Brushes.Black, (302 - footerSize.Width) / 2, yPos)
                
                e.HasMorePages = False
            End Sub
            
            printDoc.DefaultPageSettings.PaperSize = New PaperSize("Receipt", 315, 1000)
            printDoc.Print()
            
        Catch ex As Exception
            Throw New Exception($"Manufacturer summary print error: {ex.Message}", ex)
        End Try
    End Sub
    
    Private Sub AcceptOrder(sender As Object, e As EventArgs)
        Try
            ' CANCEL MODE: Process cancellation instead
            If _isCancelMode Then
                CancelOrder()
                Return
            End If
            
            If Not ValidateOrder() Then Return
            
            If MessageBox.Show($"Accept this order?{vbCrLf}Total: R{_totalAmount:F2}{vbCrLf}Deposit: R{_depositAmount:F2}{vbCrLf}Balance: R{_balanceAmount:F2}",
                              "Confirm Order", MessageBoxButtons.YesNo, MessageBoxIcon.Question) = DialogResult.Yes Then
                
                ' EDIT MODE: Skip payment - deposit already paid on original order
                If _isEditMode Then
                    ' Just save the updated order and print receipts
                    SaveOrder("CASH", "", "", "")
                Else
                    ' NEW ORDER: Open payment tender form for deposit collection
                    ' Note: This is a DEPOSIT payment, not a sale transaction
                    Using paymentForm As New PaymentTenderForm(_depositAmount, _branchID, _tillPointID, _cashierID, _cashierName)
                        If paymentForm.ShowDialog() = DialogResult.OK Then
                            ' Payment successful - get payment details from form
                            Dim paymentMethod = paymentForm.PaymentMethod
                            Dim cardMaskedPan = paymentForm.CardMaskedPan
                            Dim cardType = paymentForm.CardType
                            Dim cardApprovalCode = paymentForm.CardApprovalCode
                            
                            ' Save order (deposit recorded in POS_CustomOrders, NOT as sale)
                            SaveOrder(paymentMethod, cardMaskedPan, cardType, cardApprovalCode)
                        Else
                            MessageBox.Show("Payment cancelled. Order not saved.", "Cancelled", MessageBoxButtons.OK, MessageBoxIcon.Information)
                        End If
                    End Using
                End If
            End If
            
        Catch ex As Exception
            MessageBox.Show($"Error accepting order: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub
    
    Private Function ValidateOrder() As Boolean
        If String.IsNullOrWhiteSpace(txtCustomerName.Text) Then
            MessageBox.Show("Please enter customer name.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            txtCustomerName.Focus()
            Return False
        End If
        
        If String.IsNullOrWhiteSpace(txtCustomerPhone.Text) Then
            MessageBox.Show("Please enter customer phone number.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            txtCustomerPhone.Focus()
            Return False
        End If
        
        If _orderItems.Count = 0 Then
            MessageBox.Show("Please add at least one item to the order.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            Return False
        End If
        
        If String.IsNullOrWhiteSpace(txtCakeColor.Text) Then
            MessageBox.Show("Please enter cake color.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            txtCakeColor.Focus()
            Return False
        End If
        
        Return True
    End Function
    
    Private Sub SaveOrder(paymentMethod As String, Optional cardMaskedPan As String = "", Optional cardType As String = "", Optional cardApprovalCode As String = "")
        Try
            Using conn As New SqlConnection(_connectionString)
                conn.Open()
                
                Using transaction = conn.BeginTransaction()
                    Try
                        Dim orderNumber As String
                        Dim orderID As Integer
                        
                        If _isEditMode Then
                            ' EDIT MODE: Use existing order number and ID
                            orderNumber = _editOrderNumber
                            orderID = _editOrderID
                            _orderEditedDate = DateTime.Now
                            
                            ' Update existing order
                            Dim sqlUpdate = "
                                UPDATE POS_CustomOrders SET
                                    CustomerName = @CustomerName,
                                    CustomerSurname = @CustomerSurname,
                                    CustomerPhone = @CustomerPhone,
                                    AccountNumber = @AccountNumber,
                                    CakeColor = @CakeColor,
                                    CakePicture = @CakePicture,
                                    ReadyDate = @ReadyDate,
                                    ReadyTime = @ReadyTime,
                                    SpecialInstructions = @SpecialInstructions,
                                    Notes = @Notes,
                                    TotalAmount = @TotalAmount,
                                    BalanceDue = @Balance,
                                    LastEditedDate = @EditedDate,
                                    LastEditedBy = @EditedBy
                                WHERE OrderID = @OrderID"
                            
                            Using cmd As New SqlCommand(sqlUpdate, conn, transaction)
                                Dim fullName = txtCustomerName.Text.Trim()
                                Dim nameParts = fullName.Split(" "c)
                                Dim firstName = If(nameParts.Length > 0, nameParts(0), fullName)
                                Dim surname = If(nameParts.Length > 1, String.Join(" ", nameParts.Skip(1)), "")
                                
                                cmd.Parameters.AddWithValue("@OrderID", orderID)
                                cmd.Parameters.AddWithValue("@CustomerName", firstName)
                                cmd.Parameters.AddWithValue("@CustomerSurname", surname)
                                cmd.Parameters.AddWithValue("@CustomerPhone", txtCustomerPhone.Text.Trim())
                                cmd.Parameters.AddWithValue("@AccountNumber", If(String.IsNullOrWhiteSpace(txtAccountNumber.Text), DBNull.Value, txtAccountNumber.Text.Trim()))
                                cmd.Parameters.AddWithValue("@CakeColor", txtCakeColor.Text.Trim())
                                cmd.Parameters.AddWithValue("@CakePicture", If(String.IsNullOrWhiteSpace(txtCakePicture.Text), "see whats app", txtCakePicture.Text.Trim()))
                                cmd.Parameters.AddWithValue("@ReadyDate", dtpCollectionDate.Value.Date)
                                cmd.Parameters.AddWithValue("@ReadyTime", dtpCollectionTime.Value.TimeOfDay)
                                cmd.Parameters.AddWithValue("@SpecialInstructions", If(String.IsNullOrWhiteSpace(txtSpecialRequests.Text), DBNull.Value, txtSpecialRequests.Text.Trim()))
                                cmd.Parameters.AddWithValue("@Notes", If(String.IsNullOrWhiteSpace(txtNotes.Text), DBNull.Value, txtNotes.Text.Trim()))
                                cmd.Parameters.AddWithValue("@TotalAmount", _totalAmount)
                                cmd.Parameters.AddWithValue("@Balance", _balanceAmount)
                                cmd.Parameters.AddWithValue("@EditedDate", _orderEditedDate)
                                cmd.Parameters.AddWithValue("@EditedBy", _cashierName)
                                cmd.ExecuteNonQuery()
                            End Using
                            
                            ' Delete existing order items
                            Dim sqlDeleteItems = "DELETE FROM POS_CustomOrderItems WHERE OrderID = @OrderID"
                            Using cmd As New SqlCommand(sqlDeleteItems, conn, transaction)
                                cmd.Parameters.AddWithValue("@OrderID", orderID)
                                cmd.ExecuteNonQuery()
                            End Using
                            
                        Else
                            ' NEW ORDER MODE: Generate order number and insert new order
                            orderNumber = GenerateOrderNumber(conn, transaction)
                        
                            ' Insert order header (deposit recorded here, NOT as a sale transaction)
                            Dim sqlOrder = "
                                INSERT INTO POS_CustomOrders (
                                    OrderNumber, BranchID, BranchName, OrderType,
                                    CustomerName, CustomerSurname, CustomerPhone, AccountNumber,
                                    OrderDate, ReadyDate, ReadyTime, CollectionDay, CollectionPoint,
                                    CakeColor, CakePicture, SpecialInstructions, Notes,
                                    TotalAmount, DepositPaid, BalanceDue,
                                    OrderStatus, CreatedBy, ManufacturingInstructions
                                ) VALUES (
                                    @OrderNumber, @BranchID, @BranchName, 'Cake',
                                    @CustomerName, @CustomerSurname, @CustomerPhone, @AccountNumber,
                                    GETDATE(), @ReadyDate, @ReadyTime, @CollectionDay, @CollectionPoint,
                                    @CakeColor, @CakePicture, @SpecialInstructions, @Notes,
                                    @TotalAmount, @DepositPaid, @BalanceDue,
                                    'New', @CreatedBy, @DepositPaymentMethod
                                )
                                SELECT SCOPE_IDENTITY()"
                            
                            Using cmd As New SqlCommand(sqlOrder, conn, transaction)
                                Dim fullName = txtCustomerName.Text.Trim()
                                Dim nameParts = fullName.Split(" "c)
                                Dim firstName = If(nameParts.Length > 0, nameParts(0), fullName)
                                Dim surname = If(nameParts.Length > 1, String.Join(" ", nameParts.Skip(1)), "")
                                
                                cmd.Parameters.AddWithValue("@OrderNumber", orderNumber)
                                cmd.Parameters.AddWithValue("@BranchID", _branchID)
                                cmd.Parameters.AddWithValue("@BranchName", _branchName)
                                cmd.Parameters.AddWithValue("@CustomerName", firstName)
                                cmd.Parameters.AddWithValue("@CustomerSurname", surname)
                                cmd.Parameters.AddWithValue("@CustomerPhone", txtCustomerPhone.Text.Trim())
                                cmd.Parameters.AddWithValue("@AccountNumber", If(String.IsNullOrWhiteSpace(txtAccountNumber.Text), DBNull.Value, txtAccountNumber.Text.Trim()))
                                cmd.Parameters.AddWithValue("@ReadyDate", dtpCollectionDate.Value.Date)
                                cmd.Parameters.AddWithValue("@ReadyTime", dtpCollectionTime.Value.TimeOfDay)
                                cmd.Parameters.AddWithValue("@CollectionDay", lblCollectionDay.Text)
                                cmd.Parameters.AddWithValue("@CollectionPoint", _branchName)
                                cmd.Parameters.AddWithValue("@CakeColor", txtCakeColor.Text.Trim())
                                cmd.Parameters.AddWithValue("@CakePicture", If(String.IsNullOrWhiteSpace(txtCakePicture.Text), "see whats app", txtCakePicture.Text.Trim()))
                                cmd.Parameters.AddWithValue("@SpecialInstructions", If(String.IsNullOrWhiteSpace(txtSpecialRequests.Text), DBNull.Value, txtSpecialRequests.Text.Trim()))
                                cmd.Parameters.AddWithValue("@Notes", If(String.IsNullOrWhiteSpace(txtNotes.Text), DBNull.Value, txtNotes.Text.Trim()))
                                cmd.Parameters.AddWithValue("@TotalAmount", _totalAmount)
                                cmd.Parameters.AddWithValue("@DepositPaid", _depositAmount)
                                cmd.Parameters.AddWithValue("@BalanceDue", _balanceAmount)
                                cmd.Parameters.AddWithValue("@CreatedBy", _cashierName)
                                cmd.Parameters.AddWithValue("@DepositPaymentMethod", $"Deposit paid via {paymentMethod}")
                                
                                orderID = Convert.ToInt32(cmd.ExecuteScalar())
                            End Using
                        End If
                        
                        ' EDIT MODE: Delete existing order items first to prevent duplicates
                        If _isEditMode Then
                            Dim sqlDeleteItems = "DELETE FROM POS_CustomOrderItems WHERE OrderID = @OrderID"
                            Using cmd As New SqlCommand(sqlDeleteItems, conn, transaction)
                                cmd.Parameters.AddWithValue("@OrderID", orderID)
                                cmd.ExecuteNonQuery()
                            End Using
                        End If
                        
                        ' Insert order items (for both new and edit modes)
                        Dim sqlItem = "
                            INSERT INTO POS_CustomOrderItems (
                                OrderID, ProductID, ProductName, Quantity, UnitPrice, LineTotal
                            ) VALUES (
                                @OrderID, @ProductID, @ProductName, @Quantity, @UnitPrice, @LineTotal
                            )"
                        
                        For Each item In _orderItems
                            Using cmd As New SqlCommand(sqlItem, conn, transaction)
                                cmd.Parameters.AddWithValue("@OrderID", orderID)
                                cmd.Parameters.AddWithValue("@ProductID", item.ProductID)
                                cmd.Parameters.AddWithValue("@ProductName", item.Description)
                                cmd.Parameters.AddWithValue("@Quantity", item.Quantity)
                                cmd.Parameters.AddWithValue("@UnitPrice", item.UnitPrice)
                                cmd.Parameters.AddWithValue("@LineTotal", item.TotalPrice)
                                cmd.ExecuteNonQuery()
                            End Using
                        Next
                        
                        ' Save customer to database
                        SaveCustomerToDatabase()
                        
                        transaction.Commit()
                        
                        ' POST ACCOUNTING ENTRIES (outside transaction to avoid deadlocks)
                        Try
                            If _isEditMode Then
                                ' Post order edit accounting - adjust customer receivable
                                Dim customerName = txtCustomerName.Text.Trim()
                                _accountingService.PostOrderEdit(orderNumber, orderID, 0, customerName, 
                                                                _customerAccountNumber, _originalTotalAmount, 
                                                                _totalAmount, _branchID, _cashierName)
                            Else
                                ' Post order deposit accounting - new order
                                Dim customerName = txtCustomerName.Text.Trim()
                                Dim accountNumber = If(String.IsNullOrWhiteSpace(txtAccountNumber.Text), 
                                                      txtCustomerPhone.Text.Trim(), txtAccountNumber.Text.Trim())
                                _accountingService.PostOrderDeposit(orderNumber, orderID, 0, customerName,
                                                                   accountNumber, _totalAmount, _depositAmount,
                                                                   paymentMethod, _branchID, _cashierName)
                            End If
                        Catch accEx As Exception
                            MessageBox.Show($"Order saved but accounting entry failed: {accEx.Message}", 
                                          "Accounting Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                        End Try
                        
                        ' Print to both printers AFTER payment is tendered
                        Try
                            ' 1. Print till slip on default slip printer (customer + merchant copies)
                            PrintTillSlip(orderNumber, orderID, paymentMethod, cardMaskedPan, cardType, cardApprovalCode)
                            
                            ' 2. Print manufacturer summary slip (no prices, just items and due date/time)
                            PrintManufacturerSummary(orderNumber)
                            
                            ' 3. Print cake order form on configured continuous printer
                            Dim configuredPrinter = GetConfiguredPrinter()
                            Dim cakeOrderData = BuildPrintData(orderNumber, orderID)
                            Dim cakeOrderPrinter As New CakeOrderPrinter(cakeOrderData)
                            cakeOrderPrinter.Print(configuredPrinter)
                            
                        Catch printEx As Exception
                            MessageBox.Show($"Order saved but printing failed: {printEx.Message}", "Print Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                        End Try
                        
                        Dim successMessage As String
                        If _isEditMode Then
                            successMessage = $"Order EDITED successfully!{vbCrLf}Order Number: {orderNumber}{vbCrLf}Edited on: {_orderEditedDate:dd MMM yyyy HH:mm}{vbCrLf}{vbCrLf}New Total: R{_totalAmount:F2}{vbCrLf}Deposit paid: R{_originalDepositPaid:F2}{vbCrLf}Balance due: R{_balanceAmount:F2}{vbCrLf}{vbCrLf}Printed to:{vbCrLf}- Till slip (receipt printer){vbCrLf}- Manufacturer summary{vbCrLf}- Cake order form (continuous printer)"
                        Else
                            successMessage = $"Order created successfully!{vbCrLf}Order Number: {orderNumber}{vbCrLf}{vbCrLf}Deposit paid: R{_depositAmount:F2}{vbCrLf}Balance due: R{_balanceAmount:F2}{vbCrLf}{vbCrLf}Printed to:{vbCrLf}- Till slip (receipt printer){vbCrLf}- Manufacturer summary{vbCrLf}- Cake order form (continuous printer)"
                        End If
                        
                        MessageBox.Show(successMessage, "Success", MessageBoxButtons.OK, MessageBoxIcon.Information)
                        
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
    
    Private Function GenerateOrderNumber(conn As SqlConnection, transaction As SqlTransaction) As String
        ' Generate numeric-only order number: BranchID + 4 + 4-digit sequence
        ' Example: Branch 6, sequence 1 -> "640001"
        ' Transaction type code: 4=Order
        ' Format: BranchID (1 digit) + Type (1 digit) + Sequence (4 digits) = 6 digits total
        ' Numeric only for better barcode scanning with Free 3 of 9 font
        
        Try
            Dim sql = "SELECT ISNULL(MAX(CAST(RIGHT(OrderNumber, 4) AS INT)), 0) + 1 
                      FROM POS_CustomOrders WITH (TABLOCKX)
                      WHERE OrderNumber LIKE @pattern AND LEN(OrderNumber) = 6"
            
            Dim pattern = $"{_branchID}4%"
            
            Using cmd As New SqlCommand(sql, conn, transaction)
                cmd.Parameters.AddWithValue("@pattern", pattern)
                Dim nextNumber = Convert.ToInt32(cmd.ExecuteScalar())
                
                ' Format: BranchID + 4 (Order) + Sequence (4 digits) = 6 digits total
                ' Example: Branch 6, order 1 -> "640001"
                Return $"{_branchID}4{nextNumber.ToString().PadLeft(4, "0"c)}"
            End Using
        Catch ex As Exception
            ' Fallback to timestamp-based number if error
            Return $"{_branchID}4{DateTime.Now:HHmmss}"
        End Try
    End Function
    
    Private Function BuildPrintData(Optional orderNumber As String = "[PREVIEW]", Optional orderID As Integer = 0) As CakeOrderPrinter.CakeOrderPrintData
        Dim branchInfo = CakeOrderPrinter.GetBranchInfo(_branchID)
        
        Dim printData As New CakeOrderPrinter.CakeOrderPrintData With {
            .BranchID = _branchID,
            .BranchName = branchInfo.name,
            .BranchAddress = branchInfo.address,
            .BranchTelephone = branchInfo.tel,
            .BranchEmail = branchInfo.email,
            .VATNumber = branchInfo.vat,
            .CakeColor = txtCakeColor.Text.Trim(),
            .CakePicture = If(String.IsNullOrWhiteSpace(txtCakePicture.Text), "see whats app", txtCakePicture.Text.Trim()),
            .CollectionDate = dtpCollectionDate.Value,
            .CollectionDay = lblCollectionDay.Text,
            .CollectionTime = dtpCollectionTime.Value.ToString("HH:mm"),
            .CollectionPoint = _branchName,
            .OrderNumber = orderNumber,
            .OrderDate = DateTime.Now,
            .OrderTakenBy = _cashierName,
            .CustomerName = txtCustomerName.Text.Trim(),
            .CustomerPhone = txtCustomerPhone.Text.Trim(),
            .AccountNumber = txtAccountNumber.Text.Trim(),
            .SpecialRequests = txtSpecialRequests.Text.Trim(),
            .Notes = If(_isEditMode, $"{txtNotes.Text.Trim()}{vbCrLf}*** ORDER CHANGED ON {_orderEditedDate:dd/MM/yyyy HH:mm} ***", txtNotes.Text.Trim()),
            .InvoiceTotal = _totalAmount,
            .DepositPaid = If(_isEditMode, _originalDepositPaid, _depositAmount),
            .BalanceOwing = _balanceAmount,
            .IsEditedOrder = _isEditMode,
            .EditedDate = If(_isEditMode, _orderEditedDate, Nothing)
        }
        
        For Each item In _orderItems
            printData.Items.Add(New CakeOrderPrinter.CakeOrderPrintData.OrderItem With {
                .Description = item.Description,
                .Quantity = item.Quantity,
                .UnitPrice = item.UnitPrice,
                .TotalPrice = item.TotalPrice
            })
        Next
        
        Return printData
    End Function
    
    Private Sub SetupCancelMode()
        Try
            ' Change form title and button text
            Me.Text = "Cancel Cake Order"
            btnAcceptOrder.Text = "Cancel Order"
            btnAcceptOrder.BackColor = Color.FromArgb(192, 0, 0) ' Red
            
            ' Keep existing items visible so customer can see what they're canceling
            ' They will add cancellation fee as a separate item
            
            ' ENABLE product dropdown to add cancellation fee
            cboProductSearch.Enabled = True
            btnAddItem.Enabled = True
            btnRemoveItem.Enabled = True
            
            ' Change label to guide user
            Dim lblProductLabel = Me.Controls.Find("Label1", True).FirstOrDefault()
            If lblProductLabel IsNot Nothing Then
                DirectCast(lblProductLabel, Label).Text = "Add Cancellation Fee:"
                DirectCast(lblProductLabel, Label).ForeColor = Color.Red
            End If
            
            ' Make customer details READ-ONLY
            txtCustomerName.ReadOnly = True
            txtCustomerPhone.ReadOnly = True
            txtAccountNumber.ReadOnly = True
            dtpCollectionDate.Enabled = False
            dtpCollectionTime.Enabled = False
            txtCakeColor.ReadOnly = True
            txtCakePicture.ReadOnly = True
            cboSpecialRequests.Enabled = False
            btnAddRequest.Enabled = False
            txtNotes.ReadOnly = True
            
            ' Make deposit READ-ONLY (not hidden)
            txtDeposit.ReadOnly = True
            txtDeposit.BackColor = Color.FromArgb(245, 245, 245) ' Light grey to show read-only
            
        Catch ex As Exception
            MessageBox.Show($"Error setting up cancel mode: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub
    
    
    Private Sub CancelOrder()
        Try
            ' Get ONLY the cancellation fee item from the grid (ignore original order items)
            Dim cancellationFeeAmount As Decimal = 0
            For Each item In _orderItems
                If item.Description.ToUpper().Contains("CANCELLATION") OrElse item.Description.ToUpper().Contains("CANCEL") Then
                    cancellationFeeAmount = item.TotalPrice
                    Exit For
                End If
            Next
            
            If cancellationFeeAmount = 0 Then
                MessageBox.Show("Please add a cancellation fee item.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                Return
            End If
            
            ' Calculate balance: Deposit - Cancellation Fee
            ' Positive = we owe customer (refund)
            ' Negative = customer owes us (payment)
            Dim balanceAmount As Decimal = _depositAmount - cancellationFeeAmount
            
            Dim confirmMsg = $"Cancel this order?{vbCrLf}{vbCrLf}" &
                           $"Order Number: {_editOrderNumber}{vbCrLf}" &
                           $"Deposit Paid: R{_depositAmount:F2}{vbCrLf}" &
                           $"Cancellation Fee: R{cancellationFeeAmount:F2}{vbCrLf}"
            
            If balanceAmount >= 0 Then
                confirmMsg &= $"Refund to Customer: R{balanceAmount:F2}"
            Else
                confirmMsg &= $"Customer Pays: R{Math.Abs(balanceAmount):F2}"
            End If
            
            If MessageBox.Show(confirmMsg, "Confirm Cancellation", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) <> DialogResult.Yes Then
                Return
            End If
            
            ' Open tender dialog based on balance
            Dim paymentMethod As String = ""
            Dim cardMaskedPan As String = ""
            Dim cardType As String = ""
            Dim cardApprovalCode As String = ""
            
            If balanceAmount < 0 Then
                ' Customer pays us the difference
                Using paymentForm As New PaymentTenderForm(Math.Abs(balanceAmount), _branchID, _tillPointID, _cashierID, _cashierName)
                    If paymentForm.ShowDialog() <> DialogResult.OK Then
                        MessageBox.Show("Payment cancelled. Order not cancelled.", "Cancelled", MessageBoxButtons.OK, MessageBoxIcon.Information)
                        Return
                    End If
                    paymentMethod = paymentForm.PaymentMethod
                    cardMaskedPan = paymentForm.CardMaskedPan
                    cardType = paymentForm.CardType
                    cardApprovalCode = paymentForm.CardApprovalCode
                End Using
            ElseIf balanceAmount > 0 Then
                ' We refund customer
                Using refundForm As New RefundTenderDialog(balanceAmount, "CASH", False)
                    If refundForm.ShowDialog() <> DialogResult.OK Then
                        MessageBox.Show("Refund cancelled. Order not cancelled.", "Cancelled", MessageBoxButtons.OK, MessageBoxIcon.Information)
                        Return
                    End If
                    paymentMethod = refundForm.RefundMethod
                End Using
            Else
                ' No payment/refund needed
                paymentMethod = "NONE"
            End If
            
            ' Process cancellation in database
            ProcessCancellation(paymentMethod, cardMaskedPan, cardType, cardApprovalCode, cancellationFeeAmount, balanceAmount)
            
        Catch ex As Exception
            MessageBox.Show($"Error cancelling order: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub
    
    Private Sub ProcessCancellation(paymentMethod As String, cardMaskedPan As String, cardType As String, cardApprovalCode As String, cancellationFeeAmount As Decimal, balanceAmount As Decimal)
        Try
            Using conn As New SqlConnection(_connectionString)
                conn.Open()
                Using transaction = conn.BeginTransaction()
                    Try
                        ' 1. Update order status to Cancelled
                        Dim sqlUpdate = "UPDATE POS_CustomOrders 
                                        SET OrderStatus = 'Cancelled',
                                            ModifiedDate = GETDATE()
                                        WHERE OrderID = @OrderID"
                        Using cmd As New SqlCommand(sqlUpdate, conn, transaction)
                            cmd.Parameters.AddWithValue("@OrderID", _editOrderID)
                            cmd.ExecuteNonQuery()
                        End Using
                        
                        ' 2. Record net cash flow (customer pays us or we refund them)
                        ' Only record actual money movement, not the gross cancellation fee
                        If balanceAmount <> 0 Then
                            Dim invoice = GenerateInvoiceNumber()
                            Dim saleType As String
                            Dim amount As Decimal
                            
                            If balanceAmount < 0 Then
                                ' Customer pays us (cancellation fee > deposit)
                                saleType = "CancellationFee"
                                amount = Math.Abs(balanceAmount)
                            Else
                                ' We refund customer (deposit > cancellation fee)
                                saleType = "CancellationRefund"
                                amount = -balanceAmount
                            End If
                            
                            Dim sql = "INSERT INTO Demo_Sales 
                                      (InvoiceNumber, BranchID, TotalAmount, PaymentMethod, 
                                       SaleType, SaleDate, CashierID, CustomerName)
                                      VALUES 
                                      (@invoice, @branchId, @amount, @paymentMethod, 
                                       @saleType, GETDATE(), @cashierId, @customerName)"
                            Using cmd As New SqlCommand(sql, conn, transaction)
                                cmd.Parameters.AddWithValue("@invoice", invoice)
                                cmd.Parameters.AddWithValue("@branchId", _branchID)
                                cmd.Parameters.AddWithValue("@amount", amount)
                                cmd.Parameters.AddWithValue("@paymentMethod", paymentMethod)
                                cmd.Parameters.AddWithValue("@saleType", saleType)
                                cmd.Parameters.AddWithValue("@cashierId", _cashierID)
                                cmd.Parameters.AddWithValue("@customerName", txtCustomerName.Text.Trim())
                                cmd.ExecuteNonQuery()
                            End Using
                        End If
                        
                        transaction.Commit()
                        
                        ' POST ACCOUNTING ENTRIES (outside transaction to avoid deadlocks)
                        Try
                            Dim customerName = txtCustomerName.Text.Trim()
                            Dim accountNumber = If(String.IsNullOrWhiteSpace(txtAccountNumber.Text), 
                                                  txtCustomerPhone.Text.Trim(), txtAccountNumber.Text.Trim())
                            _accountingService.PostOrderCancellation(_editOrderNumber, _editOrderID, 0, 
                                                                    customerName, accountNumber, 
                                                                    _depositAmount, cancellationFeeAmount,
                                                                    paymentMethod, _branchID, _cashierName)
                        Catch accEx As Exception
                            MessageBox.Show($"Order cancelled but accounting entry failed: {accEx.Message}", 
                                          "Accounting Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                        End Try
                        
                        ' Print cancellation slip
                        PrintCancellationSlip(paymentMethod, cardMaskedPan, cardType, cardApprovalCode, cancellationFeeAmount, balanceAmount)
                        
                        Dim successMsg = $"Order cancelled successfully!{vbCrLf}{vbCrLf}" &
                                       $"Order Number: {_editOrderNumber}{vbCrLf}" &
                                       $"Deposit: R{_depositAmount:F2}{vbCrLf}" &
                                       $"Cancellation Fee: R{cancellationFeeAmount:F2}{vbCrLf}"
                        
                        If balanceAmount >= 0 Then
                            successMsg &= $"Refunded: R{balanceAmount:F2}"
                        Else
                            successMsg &= $"Customer Paid: R{Math.Abs(balanceAmount):F2}"
                        End If
                        
                        MessageBox.Show(successMsg, "Success", MessageBoxButtons.OK, MessageBoxIcon.Information)
                        
                        Me.DialogResult = DialogResult.OK
                        Me.Close()
                        
                    Catch ex As Exception
                        transaction.Rollback()
                        Throw
                    End Try
                End Using
            End Using
            
        Catch ex As Exception
            Throw New Exception($"Error processing cancellation: {ex.Message}", ex)
        End Try
    End Sub
    
    Private Function GenerateInvoiceNumber() As String
        Return $"INV{DateTime.Now:yyyyMMddHHmmss}"
    End Function
    
    Private Sub PrintCancellationSlip(paymentMethod As String, cardMaskedPan As String, cardType As String, cardApprovalCode As String, cancellationFeeAmount As Decimal, balanceAmount As Decimal)
        ' Print cancellation receipt showing deposit, fee, and balance
        ' Implementation similar to PrintTillSlip but for cancellation
        Try
            Dim printDoc As New PrintDocument()
            
            AddHandler printDoc.PrintPage, Sub(sender, e)
                Dim g = e.Graphics
                Dim fontBold As New Font("Courier New", 9, FontStyle.Bold)
                Dim fontRegular As New Font("Courier New", 8, FontStyle.Regular)
                Dim yPos = 10
                Dim leftMargin = 5
                
                ' Header
                g.DrawString("ORDER CANCELLED", fontBold, Brushes.Black, leftMargin, yPos)
                yPos += 20
                g.DrawString($"Order #: {_editOrderNumber}", fontRegular, Brushes.Black, leftMargin, yPos)
                yPos += 15
                g.DrawString($"Customer: {txtCustomerName.Text.Trim()}", fontRegular, Brushes.Black, leftMargin, yPos)
                yPos += 15
                g.DrawString($"Phone: {txtCustomerPhone.Text.Trim()}", fontRegular, Brushes.Black, leftMargin, yPos)
                yPos += 20
                
                g.DrawString("======================================", fontBold, Brushes.Black, leftMargin, yPos)
                yPos += 15
                
                ' Financial details
                g.DrawString($"Deposit Paid:        R{_depositAmount,10:F2}", fontRegular, Brushes.Black, leftMargin, yPos)
                yPos += 15
                g.DrawString($"Cancellation Fee:    R{cancellationFeeAmount,10:F2}", fontRegular, Brushes.Black, leftMargin, yPos)
                yPos += 15
                
                g.DrawString("--------------------------------------", fontRegular, Brushes.Black, leftMargin, yPos)
                yPos += 15
                
                If balanceAmount >= 0 Then
                    g.DrawString($"Amount Refunded:     R{balanceAmount,10:F2}", fontBold, Brushes.Black, leftMargin, yPos)
                    yPos += 15
                    g.DrawString($"Refund Method: {paymentMethod}", fontRegular, Brushes.Black, leftMargin, yPos)
                Else
                    g.DrawString($"Amount Paid:         R{Math.Abs(balanceAmount),10:F2}", fontBold, Brushes.Black, leftMargin, yPos)
                    yPos += 15
                    g.DrawString($"Payment Method: {paymentMethod}", fontRegular, Brushes.Black, leftMargin, yPos)
                End If
                
                yPos += 20
                g.DrawString("======================================", fontBold, Brushes.Black, leftMargin, yPos)
                yPos += 15
                
                g.DrawString($"Date: {DateTime.Now:dd/MM/yyyy HH:mm}", fontRegular, Brushes.Black, leftMargin, yPos)
                yPos += 15
                g.DrawString($"Cashier: {_cashierName}", fontRegular, Brushes.Black, leftMargin, yPos)
                
                e.HasMorePages = False
            End Sub
            
            printDoc.DefaultPageSettings.PaperSize = New PaperSize("Receipt", 315, 600)
            printDoc.Print()
            
        Catch ex As Exception
            ' Don't fail cancellation if print fails
            MessageBox.Show($"Cancellation successful but printing failed: {ex.Message}", "Print Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning)
        End Try
    End Sub
End Class
