Imports System.Data.SqlClient
Imports System.Configuration
Imports System.Windows.Forms

Public Class OrderLookupDialog
    Inherits Form
    
    Private _connectionString As String
    Private _accountNumber As String = ""
    Private _pickupDate As Date = Date.Today
    Private txtAccountNumber As TextBox
    Private dtpPickupDate As DateTimePicker
    Private lblAccountNumber As Label
    Private lblPickupDate As Label

    Public ReadOnly Property AccountNumber As String
        Get
            Return _accountNumber
        End Get
    End Property

    Public ReadOnly Property PickupDate As Date
        Get
            Return _pickupDate
        End Get
    End Property

    Public Sub New()
        Try
            Dim connStr = ConfigurationManager.ConnectionStrings("OvenDelightsPOSConnectionString")
            If connStr Is Nothing Then
                Throw New Exception("Connection string 'OvenDelightsPOSConnectionString' not found in configuration file.")
            End If
            _connectionString = connStr.ConnectionString
            InitializeComponent()
        Catch ex As Exception
            MessageBox.Show($"Configuration Error: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
            Throw
        End Try
    End Sub

    Private Sub InitializeComponent()
        Me.Text = "Find Cake Order"
        Me.Size = New Size(500, 360)
        Me.FormBorderStyle = FormBorderStyle.FixedDialog
        Me.StartPosition = FormStartPosition.CenterParent
        Me.MaximizeBox = False
        Me.MinimizeBox = False
        Me.BackColor = Color.White

        Dim pnlHeader As New Panel With {
            .Dock = DockStyle.Top,
            .Height = 70,
            .BackColor = ColorTranslator.FromHtml("#D2691E")
        }

        Dim lblTitle As New Label With {
            .Text = "🔍 Find Cake Order to Edit",
            .Font = New Font("Segoe UI", 16, FontStyle.Bold),
            .ForeColor = Color.White,
            .Dock = DockStyle.Fill,
            .TextAlign = ContentAlignment.MiddleCenter
        }
        pnlHeader.Controls.Add(lblTitle)

        Dim lblInstruction As New Label With {
            .Text = "Enter customer details to locate their cake order",
            .Font = New Font("Segoe UI", 9),
            .ForeColor = Color.FromArgb(100, 100, 100),
            .Location = New Point(30, 85),
            .Size = New Size(440, 20)
        }

        lblAccountNumber = New Label With {
            .Text = "Customer Account Number (Cellphone):",
            .Font = New Font("Segoe UI", 10),
            .Location = New Point(30, 115),
            .AutoSize = True
        }

        txtAccountNumber = New TextBox With {
            .Name = "txtAccountNumber",
            .Font = New Font("Segoe UI", 11),
            .Location = New Point(30, 140),
            .Size = New Size(440, 30),
            .BackColor = Color.FromArgb(250, 250, 250),
            .MaxLength = 15
        }
        AddHandler txtAccountNumber.KeyPress, AddressOf TxtAccountNumber_KeyPress

        lblPickupDate = New Label With {
            .Text = "Pickup Date:",
            .Font = New Font("Segoe UI", 10),
            .Location = New Point(30, 185),
            .AutoSize = True
        }

        dtpPickupDate = New DateTimePicker With {
            .Name = "dtpPickupDate",
            .Font = New Font("Segoe UI", 11),
            .Location = New Point(30, 210),
            .Size = New Size(440, 30),
            .Format = DateTimePickerFormat.Long,
            .Value = Date.Today
        }

        Dim btnCancel As New Button With {
            .Text = "Cancel",
            .Font = New Font("Segoe UI", 10),
            .Size = New Size(140, 45),
            .Location = New Point(180, 265),
            .BackColor = Color.FromArgb(200, 200, 200),
            .ForeColor = Color.White,
            .FlatStyle = FlatStyle.Flat,
            .DialogResult = DialogResult.Cancel
        }
        btnCancel.FlatAppearance.BorderSize = 0

        Dim btnSearch As New Button With {
            .Text = "Search Orders",
            .Font = New Font("Segoe UI", 10, FontStyle.Bold),
            .Size = New Size(140, 45),
            .Location = New Point(330, 265),
            .BackColor = ColorTranslator.FromHtml("#27AE60"),
            .ForeColor = Color.White,
            .FlatStyle = FlatStyle.Flat
        }
        btnSearch.FlatAppearance.BorderSize = 0
        AddHandler btnSearch.Click, AddressOf BtnSearch_Click

        Me.Controls.AddRange({pnlHeader, lblInstruction, lblAccountNumber, txtAccountNumber, lblPickupDate, dtpPickupDate, btnCancel, btnSearch})
        Me.AcceptButton = btnSearch
        Me.CancelButton = btnCancel
    End Sub

    Private Sub TxtAccountNumber_KeyPress(sender As Object, e As KeyPressEventArgs)
        If Not Char.IsDigit(e.KeyChar) AndAlso Not Char.IsControl(e.KeyChar) Then
            e.Handled = True
        End If
    End Sub

    Private Sub BtnSearch_Click(sender As Object, e As EventArgs)
        Dim accountNumber As String = txtAccountNumber.Text.Trim()
        Dim pickupDate As Date = dtpPickupDate.Value.Date

        If String.IsNullOrEmpty(accountNumber) Then
            MessageBox.Show("Please enter the customer's account number (cellphone number).", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            txtAccountNumber.Focus()
            Return
        End If

        If accountNumber.Length < 10 Then
            MessageBox.Show("Please enter a valid cellphone number (at least 10 digits).", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            txtAccountNumber.Focus()
            Return
        End If

        If Not OrdersExist(accountNumber, pickupDate) Then
            MessageBox.Show($"No cake orders found for account number {accountNumber} on {pickupDate:dd MMM yyyy}." & Environment.NewLine & Environment.NewLine & "Please verify the account number and pickup date.", "No Orders Found", MessageBoxButtons.OK, MessageBoxIcon.Information)
            Return
        End If

        _accountNumber = accountNumber
        _pickupDate = pickupDate
        Me.DialogResult = DialogResult.OK
        Me.Close()
    End Sub

    Private Function OrdersExist(accountNumber As String, pickupDate As Date) As Boolean
        Try
            Using conn As New SqlConnection(_connectionString)
                conn.Open()

                Dim sql = "SELECT COUNT(*) FROM POS_CustomOrders WHERE AccountNumber = @AccountNumber AND CAST(ReadyDate AS DATE) = @ReadyDate"

                Using cmd As New SqlCommand(sql, conn)
                    cmd.Parameters.AddWithValue("@AccountNumber", accountNumber)
                    cmd.Parameters.AddWithValue("@ReadyDate", pickupDate)

                    Dim count As Integer = CInt(cmd.ExecuteScalar())
                    Return count > 0
                End Using
            End Using

        Catch ex As Exception
            MessageBox.Show($"Error checking orders: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
            Return False
        End Try
    End Function
End Class
