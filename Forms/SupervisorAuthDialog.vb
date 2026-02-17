Imports System.Configuration
Imports System.Data.SqlClient
Imports System.Drawing
Imports System.Windows.Forms

Public Class SupervisorAuthDialog
    Inherits Form

    Private _connectionString As String
    Private _authenticatedUserID As Integer = 0
    Private _authenticatedUsername As String = ""

    ' UI Controls
    Private txtUsername As TextBox
    Private txtPassword As TextBox
    Private btnLogin As Button
    Private btnCancel As Button

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

    Public Sub New(connectionString As String)
        MyBase.New()
        _connectionString = connectionString
        InitializeComponent()
    End Sub

    Private Sub InitializeComponent()
        Me.Text = "Supervisor Authentication Required"
        Me.Size = New Size(450, 280)
        Me.StartPosition = FormStartPosition.CenterParent
        Me.FormBorderStyle = FormBorderStyle.FixedDialog
        Me.MaximizeBox = False
        Me.MinimizeBox = False
        Me.BackColor = Color.White

        Dim yPos As Integer = 20

        ' Header
        Dim lblHeader As New Label With {
            .Text = "SUPERVISOR AUTHENTICATION",
            .Font = New Font("Segoe UI", 14, FontStyle.Bold),
            .ForeColor = ColorTranslator.FromHtml("#E67E22"),
            .Location = New Point(20, yPos),
            .Size = New Size(400, 30),
            .TextAlign = ContentAlignment.MiddleCenter
        }
        Me.Controls.Add(lblHeader)
        yPos += 50

        ' Info label
        Dim lblInfo As New Label With {
            .Text = "Please enter supervisor credentials to manage item priorities",
            .Font = New Font("Segoe UI", 9),
            .Location = New Point(20, yPos),
            .Size = New Size(400, 20),
            .TextAlign = ContentAlignment.MiddleCenter
        }
        Me.Controls.Add(lblInfo)
        yPos += 40

        ' Username
        Dim lblUsername As New Label With {
            .Text = "Username:",
            .Font = New Font("Segoe UI", 10, FontStyle.Bold),
            .Location = New Point(50, yPos),
            .Size = New Size(100, 25)
        }
        txtUsername = New TextBox With {
            .Font = New Font("Segoe UI", 10),
            .Location = New Point(160, yPos),
            .Size = New Size(230, 25)
        }
        Me.Controls.AddRange({lblUsername, txtUsername})
        yPos += 40

        ' Password
        Dim lblPassword As New Label With {
            .Text = "Password:",
            .Font = New Font("Segoe UI", 10, FontStyle.Bold),
            .Location = New Point(50, yPos),
            .Size = New Size(100, 25)
        }
        txtPassword = New TextBox With {
            .Font = New Font("Segoe UI", 10),
            .Location = New Point(160, yPos),
            .Size = New Size(230, 25),
            .PasswordChar = "●"c,
            .UseSystemPasswordChar = False
        }
        Me.Controls.AddRange({lblPassword, txtPassword})
        yPos += 50

        ' Buttons
        btnLogin = New Button With {
            .Text = "Login",
            .Font = New Font("Segoe UI", 11, FontStyle.Bold),
            .Size = New Size(120, 40),
            .Location = New Point(110, yPos),
            .BackColor = ColorTranslator.FromHtml("#27AE60"),
            .ForeColor = Color.White,
            .FlatStyle = FlatStyle.Flat,
            .Cursor = Cursors.Hand
        }
        btnLogin.FlatAppearance.BorderSize = 0
        AddHandler btnLogin.Click, AddressOf BtnLogin_Click

        btnCancel = New Button With {
            .Text = "Cancel",
            .Font = New Font("Segoe UI", 11, FontStyle.Bold),
            .Size = New Size(120, 40),
            .Location = New Point(240, yPos),
            .BackColor = ColorTranslator.FromHtml("#E74C3C"),
            .ForeColor = Color.White,
            .FlatStyle = FlatStyle.Flat,
            .Cursor = Cursors.Hand
        }
        btnCancel.FlatAppearance.BorderSize = 0
        AddHandler btnCancel.Click, AddressOf BtnCancel_Click

        Me.Controls.AddRange({btnLogin, btnCancel})

        ' Set accept/cancel buttons
        Me.AcceptButton = btnLogin
        Me.CancelButton = btnCancel
    End Sub

    Private Sub BtnLogin_Click(sender As Object, e As EventArgs)
        If String.IsNullOrWhiteSpace(txtUsername.Text) Then
            MessageBox.Show("Please enter username", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            txtUsername.Focus()
            Return
        End If

        If String.IsNullOrWhiteSpace(txtPassword.Text) Then
            MessageBox.Show("Please enter password", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            txtPassword.Focus()
            Return
        End If

        If AuthenticateSupervisor() Then
            Me.DialogResult = DialogResult.OK
            Me.Close()
        Else
            MessageBox.Show("Invalid username or password, or user is not a supervisor", "Authentication Failed", MessageBoxButtons.OK, MessageBoxIcon.Error)
            txtPassword.Clear()
            txtUsername.Focus()
        End If
    End Sub

    Private Function AuthenticateSupervisor() As Boolean
        Try
            Using conn As New SqlConnection(_connectionString)
                conn.Open()

                ' Check if user exists, password matches, and has supervisor role
                Dim sql = "
                    SELECT u.UserID, u.Username 
                    FROM Users u
                    LEFT JOIN Roles r ON u.RoleID = r.RoleID
                    WHERE u.Username = @Username 
                    AND u.Password = @Password 
                    AND r.RoleName IN ('Supervisor', 'Admin', 'Manager', 'Retail Supervisor') 
                    AND u.IsActive = 1"
                Using cmd As New SqlCommand(sql, conn)
                    cmd.Parameters.AddWithValue("@Username", txtUsername.Text.Trim())
                    cmd.Parameters.AddWithValue("@Password", txtPassword.Text)

                    Using reader = cmd.ExecuteReader()
                        If reader.Read() Then
                            _authenticatedUserID = CInt(reader("UserID"))
                            _authenticatedUsername = reader("Username").ToString()
                            Return True
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

    Private Sub BtnCancel_Click(sender As Object, e As EventArgs)
        Me.DialogResult = DialogResult.Cancel
        Me.Close()
    End Sub
End Class
