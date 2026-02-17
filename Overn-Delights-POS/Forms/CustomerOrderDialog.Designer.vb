<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()>
Partial Class CustomerOrderDialog
    Inherits System.Windows.Forms.Form

    <System.Diagnostics.DebuggerNonUserCode()>
    Protected Overrides Sub Dispose(ByVal disposing As Boolean)
        Try
            If disposing AndAlso components IsNot Nothing Then
                components.Dispose()
            End If
        Finally
            MyBase.Dispose(disposing)
        End Try
    End Sub

    Private components As System.ComponentModel.IContainer

    Friend WithEvents pnlTop As Panel
    Friend WithEvents lblTitle As Label
    Friend WithEvents grpCustomer As GroupBox
    Friend WithEvents txtCellNumber As TextBox
    Friend WithEvents lblCellNumber As Label
    Friend WithEvents txtCustomerName As TextBox
    Friend WithEvents lblName As Label
    Friend WithEvents txtCustomerSurname As TextBox
    Friend WithEvents lblSurname As Label
    Friend WithEvents txtCustomerEmail As TextBox
    Friend WithEvents lblEmail As Label
    Friend WithEvents grpOrderDetails As GroupBox
    Friend WithEvents txtSpecialInstructions As TextBox
    Friend WithEvents lblSpecialInstructions As Label
    Friend WithEvents txtNotes As TextBox
    Friend WithEvents lblNotes As Label
    Friend WithEvents txtColour As TextBox
    Friend WithEvents lblColour As Label
    Friend WithEvents txtPicture As TextBox
    Friend WithEvents lblPicture As Label
    Friend WithEvents txtCollectionDay As TextBox
    Friend WithEvents lblCollectionDay As Label
    Friend WithEvents dtpReadyTime As DateTimePicker
    Friend WithEvents lblReadyTime As Label
    Friend WithEvents dtpReadyDate As DateTimePicker
    Friend WithEvents lblReadyDate As Label
    Friend WithEvents grpItems As GroupBox
    Friend WithEvents dgvItems As DataGridView
    Friend WithEvents grpPayment As GroupBox
    Friend WithEvents lblBalanceDue As Label
    Friend WithEvents txtDepositAmount As TextBox
    Friend WithEvents lblDepositAmount As Label
    Friend WithEvents lblTotal As Label
    Friend WithEvents lblTax As Label
    Friend WithEvents lblSubtotal As Label
    Friend WithEvents btnAmendTotal As Button
    Friend WithEvents pnlBottom As Panel
    Friend WithEvents btnCancel As Button
    Friend WithEvents btnCreateOrder As Button

    <System.Diagnostics.DebuggerStepThrough()>
    Private Sub InitializeComponent()
        Me.pnlTop = New System.Windows.Forms.Panel()
        Me.lblTitle = New System.Windows.Forms.Label()
        Me.grpCustomer = New System.Windows.Forms.GroupBox()
        Me.txtCellNumber = New System.Windows.Forms.TextBox()
        Me.lblCellNumber = New System.Windows.Forms.Label()
        Me.txtCustomerName = New System.Windows.Forms.TextBox()
        Me.lblName = New System.Windows.Forms.Label()
        Me.txtCustomerSurname = New System.Windows.Forms.TextBox()
        Me.lblSurname = New System.Windows.Forms.Label()
        Me.txtCustomerEmail = New System.Windows.Forms.TextBox()
        Me.lblEmail = New System.Windows.Forms.Label()
        Me.grpOrderDetails = New System.Windows.Forms.GroupBox()
        Me.txtSpecialInstructions = New System.Windows.Forms.TextBox()
        Me.lblSpecialInstructions = New System.Windows.Forms.Label()
        Me.txtNotes = New System.Windows.Forms.TextBox()
        Me.lblNotes = New System.Windows.Forms.Label()
        Me.txtColour = New System.Windows.Forms.TextBox()
        Me.lblColour = New System.Windows.Forms.Label()
        Me.txtPicture = New System.Windows.Forms.TextBox()
        Me.lblPicture = New System.Windows.Forms.Label()
        Me.txtCollectionDay = New System.Windows.Forms.TextBox()
        Me.lblCollectionDay = New System.Windows.Forms.Label()
        Me.dtpReadyTime = New System.Windows.Forms.DateTimePicker()
        Me.lblReadyTime = New System.Windows.Forms.Label()
        Me.dtpReadyDate = New System.Windows.Forms.DateTimePicker()
        Me.lblReadyDate = New System.Windows.Forms.Label()
        Me.grpItems = New System.Windows.Forms.GroupBox()
        Me.dgvItems = New System.Windows.Forms.DataGridView()
        Me.grpPayment = New System.Windows.Forms.GroupBox()
        Me.lblBalanceDue = New System.Windows.Forms.Label()
        Me.txtDepositAmount = New System.Windows.Forms.TextBox()
        Me.lblDepositAmount = New System.Windows.Forms.Label()
        Me.lblTotal = New System.Windows.Forms.Label()
        Me.lblTax = New System.Windows.Forms.Label()
        Me.lblSubtotal = New System.Windows.Forms.Label()
        Me.btnAmendTotal = New System.Windows.Forms.Button()
        Me.pnlBottom = New System.Windows.Forms.Panel()
        Me.btnCancel = New System.Windows.Forms.Button()
        Me.btnCreateOrder = New System.Windows.Forms.Button()
        Me.pnlTop.SuspendLayout()
        Me.grpCustomer.SuspendLayout()
        Me.grpOrderDetails.SuspendLayout()
        Me.grpItems.SuspendLayout()
        CType(Me.dgvItems, System.ComponentModel.ISupportInitialize).BeginInit()
        Me.grpPayment.SuspendLayout()
        Me.pnlBottom.SuspendLayout()
        Me.SuspendLayout()
        '
        'pnlTop
        '
        Me.pnlTop.BackColor = System.Drawing.Color.FromArgb(CType(CType(0, Byte), Integer), CType(CType(122, Byte), Integer), CType(CType(204, Byte), Integer))
        Me.pnlTop.Controls.Add(Me.lblTitle)
        Me.pnlTop.Dock = System.Windows.Forms.DockStyle.Top
        Me.pnlTop.Location = New System.Drawing.Point(0, 0)
        Me.pnlTop.Name = "pnlTop"
        Me.pnlTop.Size = New System.Drawing.Size(1200, 60)
        Me.pnlTop.TabIndex = 0
        '
        'lblTitle
        '
        Me.lblTitle.AutoSize = True
        Me.lblTitle.Font = New System.Drawing.Font("Segoe UI", 18.0!, System.Drawing.FontStyle.Bold)
        Me.lblTitle.ForeColor = System.Drawing.Color.White
        Me.lblTitle.Location = New System.Drawing.Point(12, 15)
        Me.lblTitle.Name = "lblTitle"
        Me.lblTitle.Size = New System.Drawing.Size(250, 32)
        Me.lblTitle.TabIndex = 0
        Me.lblTitle.Text = "Create Order"
        '
        'grpCustomer
        '
        Me.grpCustomer.Controls.Add(Me.txtCustomerEmail)
        Me.grpCustomer.Controls.Add(Me.lblEmail)
        Me.grpCustomer.Controls.Add(Me.txtCustomerSurname)
        Me.grpCustomer.Controls.Add(Me.lblSurname)
        Me.grpCustomer.Controls.Add(Me.txtCustomerName)
        Me.grpCustomer.Controls.Add(Me.lblName)
        Me.grpCustomer.Controls.Add(Me.txtCellNumber)
        Me.grpCustomer.Controls.Add(Me.lblCellNumber)
        Me.grpCustomer.Font = New System.Drawing.Font("Segoe UI", 10.0!, System.Drawing.FontStyle.Bold)
        Me.grpCustomer.Location = New System.Drawing.Point(12, 70)
        Me.grpCustomer.Name = "grpCustomer"
        Me.grpCustomer.Size = New System.Drawing.Size(480, 220)
        Me.grpCustomer.TabIndex = 1
        Me.grpCustomer.TabStop = False
        Me.grpCustomer.Text = "Customer Details"
        '
        'txtCellNumber (FIRST FIELD)
        '
        Me.txtCellNumber.Font = New System.Drawing.Font("Segoe UI", 10.0!)
        Me.txtCellNumber.Location = New System.Drawing.Point(120, 30)
        Me.txtCellNumber.Name = "txtCellNumber"
        Me.txtCellNumber.Size = New System.Drawing.Size(340, 25)
        Me.txtCellNumber.TabIndex = 1
        '
        'lblCellNumber
        '
        Me.lblCellNumber.AutoSize = True
        Me.lblCellNumber.Font = New System.Drawing.Font("Segoe UI", 10.0!)
        Me.lblCellNumber.Location = New System.Drawing.Point(15, 33)
        Me.lblCellNumber.Name = "lblCellNumber"
        Me.lblCellNumber.Size = New System.Drawing.Size(87, 19)
        Me.lblCellNumber.TabIndex = 0
        Me.lblCellNumber.Text = "Cell Number:"
        '
        'txtCustomerName
        '
        Me.txtCustomerName.Font = New System.Drawing.Font("Segoe UI", 10.0!)
        Me.txtCustomerName.Location = New System.Drawing.Point(120, 70)
        Me.txtCustomerName.Name = "txtCustomerName"
        Me.txtCustomerName.Size = New System.Drawing.Size(340, 25)
        Me.txtCustomerName.TabIndex = 2
        '
        'lblName
        '
        Me.lblName.AutoSize = True
        Me.lblName.Font = New System.Drawing.Font("Segoe UI", 10.0!)
        Me.lblName.Location = New System.Drawing.Point(15, 73)
        Me.lblName.Name = "lblName"
        Me.lblName.Size = New System.Drawing.Size(48, 19)
        Me.lblName.TabIndex = 1
        Me.lblName.Text = "Name:"
        '
        'txtCustomerSurname
        '
        Me.txtCustomerSurname.Font = New System.Drawing.Font("Segoe UI", 10.0!)
        Me.txtCustomerSurname.Location = New System.Drawing.Point(120, 110)
        Me.txtCustomerSurname.Name = "txtCustomerSurname"
        Me.txtCustomerSurname.Size = New System.Drawing.Size(340, 25)
        Me.txtCustomerSurname.TabIndex = 3
        '
        'lblSurname
        '
        Me.lblSurname.AutoSize = True
        Me.lblSurname.Font = New System.Drawing.Font("Segoe UI", 10.0!)
        Me.lblSurname.Location = New System.Drawing.Point(15, 113)
        Me.lblSurname.Name = "lblSurname"
        Me.lblSurname.Size = New System.Drawing.Size(67, 19)
        Me.lblSurname.TabIndex = 2
        Me.lblSurname.Text = "Surname:"
        '
        'txtCustomerEmail
        '
        Me.txtCustomerEmail.Font = New System.Drawing.Font("Segoe UI", 10.0!)
        Me.txtCustomerEmail.Location = New System.Drawing.Point(120, 170)
        Me.txtCustomerEmail.Name = "txtCustomerEmail"
        Me.txtCustomerEmail.Size = New System.Drawing.Size(340, 25)
        Me.txtCustomerEmail.TabIndex = 4
        '
        'lblEmail
        '
        Me.lblEmail.AutoSize = True
        Me.lblEmail.Font = New System.Drawing.Font("Segoe UI", 10.0!)
        Me.lblEmail.Location = New System.Drawing.Point(15, 173)
        Me.lblEmail.Name = "lblEmail"
        Me.lblEmail.Size = New System.Drawing.Size(99, 19)
        Me.lblEmail.TabIndex = 3
        Me.lblEmail.Text = "Email (optional):"
        '
        'grpOrderDetails
        '
        Me.grpOrderDetails.Controls.Add(Me.txtSpecialInstructions)
        Me.grpOrderDetails.Controls.Add(Me.lblSpecialInstructions)
        Me.grpOrderDetails.Controls.Add(Me.txtNotes)
        Me.grpOrderDetails.Controls.Add(Me.lblNotes)
        Me.grpOrderDetails.Controls.Add(Me.txtColour)
        Me.grpOrderDetails.Controls.Add(Me.lblColour)
        Me.grpOrderDetails.Controls.Add(Me.txtPicture)
        Me.grpOrderDetails.Controls.Add(Me.lblPicture)
        Me.grpOrderDetails.Controls.Add(Me.txtCollectionDay)
        Me.grpOrderDetails.Controls.Add(Me.lblCollectionDay)
        Me.grpOrderDetails.Controls.Add(Me.dtpReadyTime)
        Me.grpOrderDetails.Controls.Add(Me.lblReadyTime)
        Me.grpOrderDetails.Controls.Add(Me.dtpReadyDate)
        Me.grpOrderDetails.Controls.Add(Me.lblReadyDate)
        Me.grpOrderDetails.Font = New System.Drawing.Font("Segoe UI", 10.0!, System.Drawing.FontStyle.Bold)
        Me.grpOrderDetails.Location = New System.Drawing.Point(508, 70)
        Me.grpOrderDetails.Name = "grpOrderDetails"
        Me.grpOrderDetails.Size = New System.Drawing.Size(680, 220)
        Me.grpOrderDetails.TabIndex = 2
        Me.grpOrderDetails.TabStop = False
        Me.grpOrderDetails.Text = "Order Details"
        '
        'txtCollectionDay (Auto-filled from date)
        '
        Me.txtCollectionDay.Font = New System.Drawing.Font("Segoe UI", 10.0!, System.Drawing.FontStyle.Bold)
        Me.txtCollectionDay.Location = New System.Drawing.Point(550, 30)
        Me.txtCollectionDay.Name = "txtCollectionDay"
        Me.txtCollectionDay.ReadOnly = True
        Me.txtCollectionDay.Size = New System.Drawing.Size(110, 25)
        Me.txtCollectionDay.TabIndex = 4
        Me.txtCollectionDay.BackColor = System.Drawing.Color.LightYellow
        '
        'lblCollectionDay
        '
        Me.lblCollectionDay.AutoSize = True
        Me.lblCollectionDay.Font = New System.Drawing.Font("Segoe UI", 10.0!)
        Me.lblCollectionDay.Location = New System.Drawing.Point(485, 33)
        Me.lblCollectionDay.Name = "lblCollectionDay"
        Me.lblCollectionDay.Size = New System.Drawing.Size(35, 19)
        Me.lblCollectionDay.TabIndex = 3
        Me.lblCollectionDay.Text = "Day:"
        '
        'txtSpecialInstructions
        '
        Me.txtSpecialInstructions.Font = New System.Drawing.Font("Segoe UI", 10.0!)
        Me.txtSpecialInstructions.Location = New System.Drawing.Point(15, 90)
        Me.txtSpecialInstructions.Multiline = True
        Me.txtSpecialInstructions.Name = "txtSpecialInstructions"
        Me.txtSpecialInstructions.Size = New System.Drawing.Size(640, 80)
        Me.txtSpecialInstructions.TabIndex = 6
        '
        'lblSpecialInstructions
        '
        Me.lblSpecialInstructions.AutoSize = True
        Me.lblSpecialInstructions.Font = New System.Drawing.Font("Segoe UI", 10.0!)
        Me.lblSpecialInstructions.Location = New System.Drawing.Point(15, 68)
        Me.lblSpecialInstructions.Name = "lblSpecialInstructions"
        Me.lblSpecialInstructions.Size = New System.Drawing.Size(132, 19)
        Me.lblSpecialInstructions.TabIndex = 5
        Me.lblSpecialInstructions.Text = "Special Instructions:"
        '
        'txtNotes
        '
        Me.txtNotes.Font = New System.Drawing.Font("Segoe UI", 10.0!)
        Me.txtNotes.Location = New System.Drawing.Point(15, 200)
        Me.txtNotes.Multiline = True
        Me.txtNotes.Name = "txtNotes"
        Me.txtNotes.Size = New System.Drawing.Size(640, 60)
        Me.txtNotes.TabIndex = 8
        '
        'lblNotes
        '
        Me.lblNotes.AutoSize = True
        Me.lblNotes.Font = New System.Drawing.Font("Segoe UI", 10.0!)
        Me.lblNotes.Location = New System.Drawing.Point(15, 178)
        Me.lblNotes.Name = "lblNotes"
        Me.lblNotes.Size = New System.Drawing.Size(47, 19)
        Me.lblNotes.TabIndex = 7
        Me.lblNotes.Text = "Notes:"
        '
        'txtColour
        '
        Me.txtColour.Font = New System.Drawing.Font("Segoe UI", 10.0!)
        Me.txtColour.Location = New System.Drawing.Point(80, 270)
        Me.txtColour.Name = "txtColour"
        Me.txtColour.Size = New System.Drawing.Size(250, 25)
        Me.txtColour.TabIndex = 8
        '
        'lblColour
        '
        Me.lblColour.AutoSize = True
        Me.lblColour.Font = New System.Drawing.Font("Segoe UI", 10.0!)
        Me.lblColour.Location = New System.Drawing.Point(15, 198)
        Me.lblColour.Name = "lblColour"
        Me.lblColour.Size = New System.Drawing.Size(55, 19)
        Me.lblColour.TabIndex = 7
        Me.lblColour.Text = "Colour:"
        '
        'txtPicture
        '
        Me.txtPicture.Font = New System.Drawing.Font("Segoe UI", 10.0!)
        Me.txtPicture.Location = New System.Drawing.Point(420, 195)
        Me.txtPicture.Name = "txtPicture"
        Me.txtPicture.Size = New System.Drawing.Size(235, 25)
        Me.txtPicture.TabIndex = 10
        '
        'lblPicture
        '
        Me.lblPicture.AutoSize = True
        Me.lblPicture.Font = New System.Drawing.Font("Segoe UI", 10.0!)
        Me.lblPicture.Location = New System.Drawing.Point(355, 198)
        Me.lblPicture.Name = "lblPicture"
        Me.lblPicture.Size = New System.Drawing.Size(55, 19)
        Me.lblPicture.TabIndex = 9
        Me.lblPicture.Text = "Picture:"
        '
        'dtpReadyTime
        '
        Me.dtpReadyTime.Font = New System.Drawing.Font("Segoe UI", 10.0!)
        Me.dtpReadyTime.Format = System.Windows.Forms.DateTimePickerFormat.Time
        Me.dtpReadyTime.Location = New System.Drawing.Point(320, 30)
        Me.dtpReadyTime.Name = "dtpReadyTime"
        Me.dtpReadyTime.ShowUpDown = True
        Me.dtpReadyTime.Size = New System.Drawing.Size(145, 25)
        Me.dtpReadyTime.TabIndex = 3
        '
        'lblReadyTime
        '
        Me.lblReadyTime.AutoSize = True
        Me.lblReadyTime.Font = New System.Drawing.Font("Segoe UI", 10.0!)
        Me.lblReadyTime.Location = New System.Drawing.Point(270, 33)
        Me.lblReadyTime.Name = "lblReadyTime"
        Me.lblReadyTime.Size = New System.Drawing.Size(42, 19)
        Me.lblReadyTime.TabIndex = 2
        Me.lblReadyTime.Text = "Time:"
        '
        'dtpReadyDate
        '
        Me.dtpReadyDate.Font = New System.Drawing.Font("Segoe UI", 10.0!)
        Me.dtpReadyDate.Format = System.Windows.Forms.DateTimePickerFormat.[Short]
        Me.dtpReadyDate.Location = New System.Drawing.Point(100, 30)
        Me.dtpReadyDate.Name = "dtpReadyDate"
        Me.dtpReadyDate.Size = New System.Drawing.Size(150, 25)
        Me.dtpReadyDate.TabIndex = 1
        '
        'lblReadyDate
        '
        Me.lblReadyDate.AutoSize = True
        Me.lblReadyDate.Font = New System.Drawing.Font("Segoe UI", 10.0!)
        Me.lblReadyDate.Location = New System.Drawing.Point(15, 33)
        Me.lblReadyDate.Name = "lblReadyDate"
        Me.lblReadyDate.Size = New System.Drawing.Size(79, 19)
        Me.lblReadyDate.TabIndex = 0
        Me.lblReadyDate.Text = "Ready Date:"
        '
        'grpItems
        '
        Me.grpItems.Controls.Add(Me.dgvItems)
        Me.grpItems.Font = New System.Drawing.Font("Segoe UI", 10.0!, System.Drawing.FontStyle.Bold)
        Me.grpItems.Location = New System.Drawing.Point(12, 300)
        Me.grpItems.Name = "grpItems"
        Me.grpItems.Size = New System.Drawing.Size(680, 350)
        Me.grpItems.TabIndex = 3
        Me.grpItems.TabStop = False
        Me.grpItems.Text = "Order Items"
        '
        'dgvItems
        '
        Me.dgvItems.AllowUserToAddRows = False
        Me.dgvItems.AllowUserToDeleteRows = False
        Me.dgvItems.BackgroundColor = System.Drawing.Color.White
        Me.dgvItems.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize
        Me.dgvItems.Dock = System.Windows.Forms.DockStyle.Fill
        Me.dgvItems.Location = New System.Drawing.Point(3, 22)
        Me.dgvItems.Name = "dgvItems"
        Me.dgvItems.ReadOnly = True
        Me.dgvItems.Size = New System.Drawing.Size(674, 255)
        Me.dgvItems.TabIndex = 0
        '
        'grpPayment
        '
        Me.grpPayment.Controls.Add(Me.lblBalanceDue)
        Me.grpPayment.Controls.Add(Me.txtDepositAmount)
        Me.grpPayment.Controls.Add(Me.lblDepositAmount)
        Me.grpPayment.Controls.Add(Me.btnAmendTotal)
        Me.grpPayment.Controls.Add(Me.lblTotal)
        Me.grpPayment.Controls.Add(Me.lblTax)
        Me.grpPayment.Controls.Add(Me.lblSubtotal)
        Me.grpPayment.Font = New System.Drawing.Font("Segoe UI", 10.0!, System.Drawing.FontStyle.Bold)
        Me.grpPayment.Location = New System.Drawing.Point(708, 300)
        Me.grpPayment.Name = "grpPayment"
        Me.grpPayment.Size = New System.Drawing.Size(480, 350)
        Me.grpPayment.TabIndex = 4
        Me.grpPayment.TabStop = False
        Me.grpPayment.Text = "Payment"
        '
        'lblBalanceDue
        '
        Me.lblBalanceDue.AutoSize = True
        Me.lblBalanceDue.Font = New System.Drawing.Font("Segoe UI", 12.0!, System.Drawing.FontStyle.Bold)
        Me.lblBalanceDue.ForeColor = System.Drawing.Color.Red
        Me.lblBalanceDue.Location = New System.Drawing.Point(15, 200)
        Me.lblBalanceDue.Name = "lblBalanceDue"
        Me.lblBalanceDue.Size = New System.Drawing.Size(145, 21)
        Me.lblBalanceDue.TabIndex = 5
        Me.lblBalanceDue.Text = "Balance Due: R0.00"
        '
        'txtDepositAmount
        '
        Me.txtDepositAmount.Font = New System.Drawing.Font("Segoe UI", 14.0!, System.Drawing.FontStyle.Bold)
        Me.txtDepositAmount.Location = New System.Drawing.Point(15, 150)
        Me.txtDepositAmount.Name = "txtDepositAmount"
        Me.txtDepositAmount.Size = New System.Drawing.Size(250, 32)
        Me.txtDepositAmount.TabIndex = 4
        Me.txtDepositAmount.Text = "0.00"
        Me.txtDepositAmount.TextAlign = System.Windows.Forms.HorizontalAlignment.Right
        '
        'lblDepositAmount
        '
        Me.lblDepositAmount.AutoSize = True
        Me.lblDepositAmount.Font = New System.Drawing.Font("Segoe UI", 10.0!)
        Me.lblDepositAmount.Location = New System.Drawing.Point(15, 125)
        Me.lblDepositAmount.Name = "lblDepositAmount"
        Me.lblDepositAmount.Size = New System.Drawing.Size(115, 19)
        Me.lblDepositAmount.TabIndex = 3
        Me.lblDepositAmount.Text = "Deposit Amount:"
        '
        'lblTotal
        '
        Me.lblTotal.AutoSize = True
        Me.lblTotal.Font = New System.Drawing.Font("Segoe UI", 14.0!, System.Drawing.FontStyle.Bold)
        Me.lblTotal.ForeColor = System.Drawing.Color.Green
        Me.lblTotal.Location = New System.Drawing.Point(15, 80)
        Me.lblTotal.Name = "lblTotal"
        Me.lblTotal.Size = New System.Drawing.Size(123, 25)
        Me.lblTotal.TabIndex = 2
        Me.lblTotal.Text = "Total: R0.00"
        '
        'btnAmendTotal
        '
        Me.btnAmendTotal.BackColor = System.Drawing.Color.FromArgb(CType(CType(243, Byte), Integer), CType(CType(156, Byte), Integer), CType(CType(18, Byte), Integer))
        Me.btnAmendTotal.FlatStyle = System.Windows.Forms.FlatStyle.Flat
        Me.btnAmendTotal.Font = New System.Drawing.Font("Segoe UI", 11.0!, System.Drawing.FontStyle.Bold)
        Me.btnAmendTotal.ForeColor = System.Drawing.Color.White
        Me.btnAmendTotal.Location = New System.Drawing.Point(280, 75)
        Me.btnAmendTotal.Name = "btnAmendTotal"
        Me.btnAmendTotal.Size = New System.Drawing.Size(180, 35)
        Me.btnAmendTotal.TabIndex = 6
        Me.btnAmendTotal.Text = "🔢 AMEND TOTAL"
        Me.btnAmendTotal.UseVisualStyleBackColor = False
        '
        'lblTax
        '
        Me.lblTax.AutoSize = True
        Me.lblTax.Font = New System.Drawing.Font("Segoe UI", 10.0!)
        Me.lblTax.Location = New System.Drawing.Point(15, 55)
        Me.lblTax.Name = "lblTax"
        Me.lblTax.Size = New System.Drawing.Size(106, 19)
        Me.lblTax.TabIndex = 1
        Me.lblTax.Text = "VAT (15%): R0.00"
        '
        'lblSubtotal
        '
        Me.lblSubtotal.AutoSize = True
        Me.lblSubtotal.Font = New System.Drawing.Font("Segoe UI", 10.0!)
        Me.lblSubtotal.Location = New System.Drawing.Point(15, 30)
        Me.lblSubtotal.Name = "lblSubtotal"
        Me.lblSubtotal.Size = New System.Drawing.Size(96, 19)
        Me.lblSubtotal.TabIndex = 0
        Me.lblSubtotal.Text = "Subtotal: R0.00"
        '
        'pnlBottom
        '
        Me.pnlBottom.BackColor = System.Drawing.Color.WhiteSmoke
        Me.pnlBottom.Controls.Add(Me.btnCancel)
        Me.pnlBottom.Controls.Add(Me.btnCreateOrder)
        Me.pnlBottom.Dock = System.Windows.Forms.DockStyle.Bottom
        Me.pnlBottom.Location = New System.Drawing.Point(0, 660)
        Me.pnlBottom.Name = "pnlBottom"
        Me.pnlBottom.Size = New System.Drawing.Size(1200, 70)
        Me.pnlBottom.TabIndex = 5
        '
        'btnCancel
        '
        Me.btnCancel.Anchor = CType((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.btnCancel.BackColor = System.Drawing.Color.Gray
        Me.btnCancel.FlatStyle = System.Windows.Forms.FlatStyle.Flat
        Me.btnCancel.Font = New System.Drawing.Font("Segoe UI", 12.0!, System.Drawing.FontStyle.Bold)
        Me.btnCancel.ForeColor = System.Drawing.Color.White
        Me.btnCancel.Location = New System.Drawing.Point(870, 15)
        Me.btnCancel.Name = "btnCancel"
        Me.btnCancel.Size = New System.Drawing.Size(120, 45)
        Me.btnCancel.TabIndex = 1
        Me.btnCancel.Text = "Cancel"
        Me.btnCancel.UseVisualStyleBackColor = False
        '
        'btnCreateOrder
        '
        Me.btnCreateOrder.Anchor = CType((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.btnCreateOrder.BackColor = System.Drawing.Color.FromArgb(CType(CType(0, Byte), Integer), CType(CType(122, Byte), Integer), CType(CType(204, Byte), Integer))
        Me.btnCreateOrder.FlatStyle = System.Windows.Forms.FlatStyle.Flat
        Me.btnCreateOrder.Font = New System.Drawing.Font("Segoe UI", 12.0!, System.Drawing.FontStyle.Bold)
        Me.btnCreateOrder.ForeColor = System.Drawing.Color.White
        Me.btnCreateOrder.Location = New System.Drawing.Point(720, 15)
        Me.btnCreateOrder.Name = "btnCreateOrder"
        Me.btnCreateOrder.Size = New System.Drawing.Size(140, 45)
        Me.btnCreateOrder.TabIndex = 0
        Me.btnCreateOrder.Text = "Create Order"
        Me.btnCreateOrder.UseVisualStyleBackColor = False
        '
        'CustomerOrderDialog
        '
        Me.AutoScaleDimensions = New System.Drawing.SizeF(7.0!, 15.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.ClientSize = New System.Drawing.Size(1200, 730)
        Me.Controls.Add(Me.pnlBottom)
        Me.Controls.Add(Me.grpPayment)
        Me.Controls.Add(Me.grpItems)
        Me.Controls.Add(Me.grpOrderDetails)
        Me.Controls.Add(Me.grpCustomer)
        Me.Controls.Add(Me.pnlTop)
        Me.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog
        Me.MaximizeBox = False
        Me.MinimizeBox = False
        Me.Name = "CustomerOrderDialog"
        Me.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen
        Me.Text = "Create Order"
        Me.pnlTop.ResumeLayout(False)
        Me.pnlTop.PerformLayout()
        Me.grpCustomer.ResumeLayout(False)
        Me.grpCustomer.PerformLayout()
        Me.grpOrderDetails.ResumeLayout(False)
        Me.grpOrderDetails.PerformLayout()
        Me.grpItems.ResumeLayout(False)
        CType(Me.dgvItems, System.ComponentModel.ISupportInitialize).EndInit()
        Me.grpPayment.ResumeLayout(False)
        Me.grpPayment.PerformLayout()
        Me.pnlBottom.ResumeLayout(False)
        Me.ResumeLayout(False)

    End Sub
End Class
