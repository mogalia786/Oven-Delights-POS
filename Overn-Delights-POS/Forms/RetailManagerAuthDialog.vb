Imports System.Data.SqlClient
Imports System.Configuration
Imports System.Windows.Forms

Public Class RetailManagerAuthDialog
    Inherits Form
    
    Private _connectionString As String
    Private _authenticatedUserID As Integer = 0
    Private _authenticatedUsername As String = ""
    Private txtUsername As TextBox
    Private txtPassword As TextBox

    Public ReadOnly Property AuthenticatedUserID As Integer
        Get
            Return _authenticatedUserID
        End Get
    End Property

    Public ReadOnly Property AuthenticatedUsername As String
        Get
            Return _authenticatedUsername
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
        Me.Text = "Retail Manager Authorization"
        Me.Size = New Size(450, 320)
        Me.FormBorderStyle = FormBorderStyle.FixedDialog
        Me.StartPosition = FormStartPosition.CenterParent
        Me.MaximizeBox = False
        Me.MinimizeBox = False
        Me.BackColor = Color.White

        Dim pnlHeader As New Panel With {
            .Dock = DockStyle.Top,
            .Height = 60,
            .BackColor = ColorTranslator.FromHtml("#D2691E")
        }

        Dim lblTitle As New Label With {
            .Text = "🔐 Manager Authorization Required",
            .Font = New Font("Segoe UI", 14, FontStyle.Bold),
            .ForeColor = Color.White,
            .Dock = DockStyle.Fill,
            .TextAlign = ContentAlignment.MiddleCenter
        }
        pnlHeader.Controls.Add(lblTitle)

        Dim lblInstruction As New Label With {
            .Text = "Please enter Retail Manager credentials to edit cake orders",
            .Font = New Font("Segoe UI", 9),
            .ForeColor = Color.FromArgb(100, 100, 100),
            .Location = New Point(30, 75),
            .Size = New Size(390, 20)
        }

        Dim lblUsername As New Label With {
            .Text = "Username:",
            .Font = New Font("Segoe UI", 10),
            .Location = New Point(30, 90),
            .AutoSize = True
        }

        txtUsername = New TextBox With {
            .Name = "txtUsername",
            .Font = New Font("Segoe UI", 11),
            .Location = New Point(30, 115),
            .Size = New Size(390, 30),
            .BackColor = Color.FromArgb(250, 250, 250)
        }

        Dim lblPassword As New Label With {
            .Text = "Password:",
            .Font = New Font("Segoe UI", 10),
            .Location = New Point(30, 155),
            .AutoSize = True
        }

        txtPassword = New TextBox With {
            .Name = "txtPassword",
            .Font = New Font("Segoe UI", 11),
            .Location = New Point(30, 180),
            .Size = New Size(390, 30),
            .UseSystemPasswordChar = True,
            .BackColor = Color.FromArgb(250, 250, 250)
        }

        Dim btnCancel As New Button With {
            .Text = "Cancel",
            .Font = New Font("Segoe UI", 10),
            .Size = New Size(120, 40),
            .Location = New Point(180, 230),
            .BackColor = Color.FromArgb(200, 200, 200),
            .ForeColor = Color.White,
            .FlatStyle = FlatStyle.Flat,
            .DialogResult = DialogResult.Cancel
        }
        btnCancel.FlatAppearance.BorderSize = 0

        Dim btnLogin As New Button With {
            .Text = "Authorize",
            .Font = New Font("Segoe UI", 10, FontStyle.Bold),
            .Size = New Size(120, 40),
            .Location = New Point(310, 230),
            .BackColor = ColorTranslator.FromHtml("#27AE60"),
            .ForeColor = Color.White,
            .FlatStyle = FlatStyle.Flat
        }
        btnLogin.FlatAppearance.BorderSize = 0
        AddHandler btnLogin.Click, AddressOf BtnLogin_Click

        Me.Controls.AddRange({pnlHeader, lblInstruction, lblUsername, txtUsername, lblPassword, txtPassword, btnCancel, btnLogin})
        Me.AcceptButton = btnLogin
        Me.CancelButton = btnCancel
    End Sub

    Private Sub BtnLogin_Click(sender As Object, e As EventArgs)
        Dim username As String = txtUsername.Text.Trim()
        Dim password As String = txtPassword.Text

        If String.IsNullOrEmpty(username) OrElse String.IsNullOrEmpty(password) Then
            MessageBox.Show("Please enter both username and password.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            Return
        End If

        If AuthenticateRetailManager(username, password) Then
            Me.DialogResult = DialogResult.OK
            Me.Close()
        Else
            MessageBox.Show("Invalid credentials or insufficient permissions." & Environment.NewLine & Environment.NewLine & "Only Retail Managers can edit cake orders.", "Authorization Failed", MessageBoxButtons.OK, MessageBoxIcon.Error)
            txtPassword.Clear()
            txtPassword.Focus()
        End If
    End Sub

    Private Function AuthenticateRetailManager(username As String, password As String) As Boolean
        Try
            Using conn As New SqlConnection(_connectionString)
                conn.Open()

                ' First check if user exists
                Dim checkUserSql = "SELECT UserID, Username, RoleID, IsActive FROM Users WHERE LOWER(Username) = LOWER(@Username)"
                Using checkCmd As New SqlCommand(checkUserSql, conn)
                    checkCmd.Parameters.AddWithValue("@Username", username)
                    Using checkReader = checkCmd.ExecuteReader()
                        If Not checkReader.Read() Then
                            MessageBox.Show($"User '{username}' not found in database.", "Debug Info", MessageBoxButtons.OK, MessageBoxIcon.Information)
                            Return False
                        End If
                    End Using
                End Using

                ' Now authenticate with case-insensitive username
                Dim sql = "
                    SELECT u.UserID, u.Username, r.RoleName
                    FROM Users u
                    INNER JOIN Roles r ON u.RoleID = r.RoleID
                    WHERE LOWER(u.Username) = LOWER(@Username)
                      AND u.Password = @Password 
                      AND u.IsActive = 1
                      AND r.RoleName = 'Retail Supervisor'"

                Using cmd As New SqlCommand(sql, conn)
                    cmd.Parameters.AddWithValue("@Username", username)
                    cmd.Parameters.AddWithValue("@Password", password)

                    Using reader = cmd.ExecuteReader()
                        If reader.Read() Then
                            _authenticatedUserID = reader.GetInt32(0)
                            _authenticatedUsername = reader.GetString(1)
                            Return True
                        Else
                            MessageBox.Show("Authentication failed. Check password or role permissions.", "Debug Info", MessageBoxButtons.OK, MessageBoxIcon.Information)
                        End If
                    End Using
                End Using
            End Using

            Return False

        Catch ex As Exception
            MessageBox.Show($"Authentication error: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
            Return False
        End Try
    End Function
End Class
