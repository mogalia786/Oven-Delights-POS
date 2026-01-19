Imports System.Configuration
Imports System.Data.SqlClient
Imports System.Drawing
Imports System.Windows.Forms

Public Class UserDefinedOrderDialog
    Inherits Form

    Private _connectionString As String
    Private _branchID As Integer
    Private _branchName As String

    ' UI Controls
    Private txtCellNumber As TextBox
    Private txtCustomerName As TextBox
    Private txtCustomerSurname As TextBox
    Private txtCakeColour As TextBox
    Private cmbSpecialRequest As ComboBox
    Private txtCakeImage As TextBox
    Private dtpCollectionDate As DateTimePicker
    Private txtCollectionDay As TextBox
    Private dtpCollectionTime As DateTimePicker
    Private btnSave As Button
    Private btnCancel As Button

    ' Public properties to return data
    Public Property CustomerCellNumber As String
    Public Property CustomerName As String
    Public Property CustomerSurname As String
    Public Property CakeColour As String
    Public Property SpecialRequest As String
    Public Property CakeImage As String
    Public Property CollectionDate As Date
    Public Property CollectionTime As TimeSpan
    Public Property CollectionDay As String

    Public Sub New(branchID As Integer, branchName As String)
        MyBase.New()
        _branchID = branchID
        _branchName = branchName
        _connectionString = ConfigurationManager.ConnectionStrings("OvenDelightsERPConnectionString").ConnectionString

        InitializeComponent()
        LoadSpecialRequests()
    End Sub

    Private Sub InitializeComponent()
        Me.Text = "User Defined Order - Header Fields"
        Me.Size = New Size(600, 650)
        Me.StartPosition = FormStartPosition.CenterScreen
        Me.FormBorderStyle = FormBorderStyle.FixedDialog
        Me.MaximizeBox = False
        Me.MinimizeBox = False
        Me.BackColor = Color.White

        Dim yPos As Integer = 20

        ' Header Label
        Dim lblHeader As New Label With {
            .Text = "USER DEFINED ORDER - HEADER FIELDS",
            .Font = New Font("Segoe UI", 16, FontStyle.Bold),
            .ForeColor = ColorTranslator.FromHtml("#E67E22"),
            .Location = New Point(20, yPos),
            .Size = New Size(550, 35),
            .TextAlign = ContentAlignment.MiddleCenter
        }
        Me.Controls.Add(lblHeader)
        yPos += 50

        ' Customer Cell Number
        Dim lblCellNumber As New Label With {
            .Text = "Customer Cell Number:",
            .Font = New Font("Segoe UI", 10, FontStyle.Bold),
            .Location = New Point(20, yPos),
            .Size = New Size(200, 25)
        }
        txtCellNumber = New TextBox With {
            .Font = New Font("Segoe UI", 10),
            .Location = New Point(230, yPos),
            .Size = New Size(330, 25)
        }
        AddHandler txtCellNumber.Leave, AddressOf TxtCellNumber_Leave
        Me.Controls.AddRange({lblCellNumber, txtCellNumber})
        yPos += 40

        ' Customer Name
        Dim lblCustomerName As New Label With {
            .Text = "Customer Name:",
            .Font = New Font("Segoe UI", 10, FontStyle.Bold),
            .Location = New Point(20, yPos),
            .Size = New Size(200, 25)
        }
        txtCustomerName = New TextBox With {
            .Font = New Font("Segoe UI", 10),
            .Location = New Point(230, yPos),
            .Size = New Size(330, 25)
        }
        Me.Controls.AddRange({lblCustomerName, txtCustomerName})
        yPos += 40

        ' Customer Surname
        Dim lblCustomerSurname As New Label With {
            .Text = "Customer Surname:",
            .Font = New Font("Segoe UI", 10, FontStyle.Bold),
            .Location = New Point(20, yPos),
            .Size = New Size(200, 25)
        }
        txtCustomerSurname = New TextBox With {
            .Font = New Font("Segoe UI", 10),
            .Location = New Point(230, yPos),
            .Size = New Size(330, 25)
        }
        Me.Controls.AddRange({lblCustomerSurname, txtCustomerSurname})
        yPos += 40

        ' Cake Colour
        Dim lblCakeColour As New Label With {
            .Text = "Cake Colour (Optional):",
            .Font = New Font("Segoe UI", 10, FontStyle.Bold),
            .Location = New Point(20, yPos),
            .Size = New Size(200, 25)
        }
        txtCakeColour = New TextBox With {
            .Font = New Font("Segoe UI", 10),
            .Location = New Point(230, yPos),
            .Size = New Size(330, 25)
        }
        Me.Controls.AddRange({lblCakeColour, txtCakeColour})
        yPos += 40

        ' Special Request
        Dim lblSpecialRequest As New Label With {
            .Text = "Special Request (Optional):",
            .Font = New Font("Segoe UI", 10, FontStyle.Bold),
            .Location = New Point(20, yPos),
            .Size = New Size(200, 25)
        }
        cmbSpecialRequest = New ComboBox With {
            .Font = New Font("Segoe UI", 10),
            .Location = New Point(230, yPos),
            .Size = New Size(330, 25),
            .DropDownStyle = ComboBoxStyle.DropDown
        }
        cmbSpecialRequest.Items.AddRange(New String() {
            "Double vanilla", "Double choc", "Eggless", "Figure only", "Figure on base",
            "Blackforest", "Red velvet", "Milkybar", "Bar one", "Ferrero",
            "Carrot cake", "Heart shape", "Bible", "Tiered", "Mould",
            "Doll cake", "Soccer field", "1mx 500"
        })
        Me.Controls.AddRange({lblSpecialRequest, cmbSpecialRequest})
        yPos += 40

        ' Cake Image
        Dim lblCakeImage As New Label With {
            .Text = "Cake Picture:",
            .Font = New Font("Segoe UI", 10, FontStyle.Bold),
            .Location = New Point(20, yPos),
            .Size = New Size(200, 25)
        }
        txtCakeImage = New TextBox With {
            .Font = New Font("Segoe UI", 10),
            .Location = New Point(230, yPos),
            .Size = New Size(330, 25)
        }
        Me.Controls.AddRange({lblCakeImage, txtCakeImage})
        yPos += 40

        ' Collection Date
        Dim lblCollectionDate As New Label With {
            .Text = "Collection Date:",
            .Font = New Font("Segoe UI", 10, FontStyle.Bold),
            .Location = New Point(20, yPos),
            .Size = New Size(200, 25)
        }
        dtpCollectionDate = New DateTimePicker With {
            .Font = New Font("Segoe UI", 10),
            .Location = New Point(230, yPos),
            .Size = New Size(330, 25),
            .Format = DateTimePickerFormat.Short,
            .MinDate = DateTime.Today
        }
        AddHandler dtpCollectionDate.ValueChanged, AddressOf DtpCollectionDate_ValueChanged
        Me.Controls.AddRange({lblCollectionDate, dtpCollectionDate})
        yPos += 40

        ' Collection Day (Auto-populated)
        Dim lblCollectionDay As New Label With {
            .Text = "Collection Day:",
            .Font = New Font("Segoe UI", 10, FontStyle.Bold),
            .Location = New Point(20, yPos),
            .Size = New Size(200, 25)
        }
        txtCollectionDay = New TextBox With {
            .Font = New Font("Segoe UI", 10),
            .Location = New Point(230, yPos),
            .Size = New Size(330, 25),
            .ReadOnly = True,
            .BackColor = Color.LightGray,
            .Text = DateTime.Today.DayOfWeek.ToString()
        }
        Me.Controls.AddRange({lblCollectionDay, txtCollectionDay})
        yPos += 40

        ' Collection Time
        Dim lblCollectionTime As New Label With {
            .Text = "Collection Time:",
            .Font = New Font("Segoe UI", 10, FontStyle.Bold),
            .Location = New Point(20, yPos),
            .Size = New Size(200, 25)
        }
        dtpCollectionTime = New DateTimePicker With {
            .Font = New Font("Segoe UI", 10),
            .Location = New Point(230, yPos),
            .Size = New Size(330, 25),
            .Format = DateTimePickerFormat.Time,
            .ShowUpDown = True
        }
        Me.Controls.AddRange({lblCollectionTime, dtpCollectionTime})
        yPos += 50

        ' Buttons
        btnSave = New Button With {
            .Text = "Save",
            .Font = New Font("Segoe UI", 12, FontStyle.Bold),
            .Size = New Size(150, 45),
            .Location = New Point(150, yPos),
            .BackColor = ColorTranslator.FromHtml("#27AE60"),
            .ForeColor = Color.White,
            .FlatStyle = FlatStyle.Flat,
            .Cursor = Cursors.Hand
        }
        btnSave.FlatAppearance.BorderSize = 0
        AddHandler btnSave.Click, AddressOf BtnSave_Click

        btnCancel = New Button With {
            .Text = "Cancel",
            .Font = New Font("Segoe UI", 12, FontStyle.Bold),
            .Size = New Size(150, 45),
            .Location = New Point(320, yPos),
            .BackColor = ColorTranslator.FromHtml("#E74C3C"),
            .ForeColor = Color.White,
            .FlatStyle = FlatStyle.Flat,
            .Cursor = Cursors.Hand
        }
        btnCancel.FlatAppearance.BorderSize = 0
        AddHandler btnCancel.Click, AddressOf BtnCancel_Click

        Me.Controls.AddRange({btnSave, btnCancel})
    End Sub

    Private Sub LoadSpecialRequests()
        ' Load common special requests (same as cake orders)
        cmbSpecialRequest.Items.AddRange(New String() {
            "Happy Birthday",
            "Congratulations",
            "Happy Anniversary",
            "Get Well Soon",
            "Thank You",
            "Graduation",
            "Baby Shower",
            "Wedding",
            "Engagement",
            "Retirement",
            "Farewell",
            "Welcome",
            "Custom Message"
        })
    End Sub

    Private Sub TxtCellNumber_Leave(sender As Object, e As EventArgs)
        ' Lookup customer by cell number
        Dim cellNumber = txtCellNumber.Text.Trim()
        If String.IsNullOrEmpty(cellNumber) Then Return

        Try
            Using conn As New SqlConnection(_connectionString)
                conn.Open()
                Dim sql = "SELECT TOP 1 FirstName AS CustomerName, Surname AS CustomerSurname FROM POS_Customers WHERE CellNumber = @CellNumber"
                Using cmd As New SqlCommand(sql, conn)
                    cmd.Parameters.AddWithValue("@CellNumber", cellNumber)
                    Using reader = cmd.ExecuteReader()
                        If reader.Read() Then
                            ' Customer found - populate fields
                            Dim firstName = If(IsDBNull(reader("CustomerName")), "", reader("CustomerName").ToString())
                            Dim surname = If(IsDBNull(reader("CustomerSurname")), "", reader("CustomerSurname").ToString())
                            
                            txtCustomerName.Text = firstName
                            txtCustomerSurname.Text = surname
                            txtCustomerName.BackColor = Color.LightGreen
                            txtCustomerSurname.BackColor = Color.LightGreen
                            
                            ' Debug: Show what was found
                            If String.IsNullOrEmpty(surname) Then
                                MessageBox.Show($"Customer found: {firstName}{vbCrLf}Note: No surname on file", "Customer Lookup", MessageBoxButtons.OK, MessageBoxIcon.Information)
                            End If
                        Else
                            ' Customer not found - clear fields and allow manual entry
                            txtCustomerName.Text = ""
                            txtCustomerSurname.Text = ""
                            txtCustomerName.BackColor = Color.White
                            txtCustomerSurname.BackColor = Color.White
                            txtCustomerName.Focus()
                        End If
                    End Using
                End Using
            End Using
        Catch ex As Exception
            MessageBox.Show($"Error looking up customer: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    Private Sub DtpCollectionDate_ValueChanged(sender As Object, e As EventArgs)
        ' Auto-populate collection day when date changes
        txtCollectionDay.Text = dtpCollectionDate.Value.DayOfWeek.ToString()
    End Sub

    Private Sub BtnSave_Click(sender As Object, e As EventArgs)
        ' Validate required fields
        If String.IsNullOrWhiteSpace(txtCellNumber.Text) Then
            MessageBox.Show("Customer Cell Number is required.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            txtCellNumber.Focus()
            Return
        End If

        If String.IsNullOrWhiteSpace(txtCustomerName.Text) Then
            MessageBox.Show("Customer Name is required.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            txtCustomerName.Focus()
            Return
        End If

        If dtpCollectionDate.Value.Date < DateTime.Today Then
            MessageBox.Show("Collection Date cannot be in the past.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            dtpCollectionDate.Focus()
            Return
        End If

        ' Save to properties
        CustomerCellNumber = txtCellNumber.Text.Trim()
        CustomerName = txtCustomerName.Text.Trim()
        CustomerSurname = txtCustomerSurname.Text.Trim()
        CakeColour = txtCakeColour.Text.Trim()
        SpecialRequest = cmbSpecialRequest.Text.Trim()
        CakeImage = txtCakeImage.Text.Trim()
        CollectionDate = dtpCollectionDate.Value.Date
        CollectionTime = dtpCollectionTime.Value.TimeOfDay
        CollectionDay = txtCollectionDay.Text

        ' Add customer to database if not exists
        Try
            AddCustomerIfNotExists()
        Catch ex As Exception
            MessageBox.Show($"Error saving customer: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning)
        End Try

        Me.DialogResult = DialogResult.OK
        Me.Close()
    End Sub

    Private Sub AddCustomerIfNotExists()
        Using conn As New SqlConnection(_connectionString)
            conn.Open()
            
            ' Check if customer exists
            Dim checkSql = "SELECT COUNT(*) FROM POS_Customers WHERE CellNumber = @CellNumber"
            Using cmdCheck As New SqlCommand(checkSql, conn)
                cmdCheck.Parameters.AddWithValue("@CellNumber", CustomerCellNumber)
                Dim count = CInt(cmdCheck.ExecuteScalar())
                
                If count = 0 Then
                    ' Customer doesn't exist - add them
                    Dim insertSql = "INSERT INTO POS_Customers (CellNumber, FirstName, Surname, LastOrderDate, TotalOrders, IsActive) VALUES (@CellNumber, @CustomerName, @CustomerSurname, GETDATE(), 1, 1)"
                    Using cmdInsert As New SqlCommand(insertSql, conn)
                        cmdInsert.Parameters.AddWithValue("@CellNumber", CustomerCellNumber)
                        cmdInsert.Parameters.AddWithValue("@CustomerName", CustomerName)
                        cmdInsert.Parameters.AddWithValue("@CustomerSurname", If(String.IsNullOrEmpty(CustomerSurname), DBNull.Value, CustomerSurname))
                        cmdInsert.ExecuteNonQuery()
                    End Using
                Else
                    ' Customer exists - update surname if provided and different
                    If Not String.IsNullOrWhiteSpace(CustomerSurname) Then
                        Dim updateSql = "UPDATE POS_Customers SET Surname = @CustomerSurname, FirstName = @CustomerName, LastOrderDate = GETDATE() WHERE CellNumber = @CellNumber"
                        Using cmdUpdate As New SqlCommand(updateSql, conn)
                            cmdUpdate.Parameters.AddWithValue("@CellNumber", CustomerCellNumber)
                            cmdUpdate.Parameters.AddWithValue("@CustomerName", CustomerName)
                            cmdUpdate.Parameters.AddWithValue("@CustomerSurname", CustomerSurname)
                            cmdUpdate.ExecuteNonQuery()
                        End Using
                    End If
                End If
            End Using
        End Using
    End Sub

    Private Sub BtnCancel_Click(sender As Object, e As EventArgs)
        Me.DialogResult = DialogResult.Cancel
        Me.Close()
    End Sub
End Class
