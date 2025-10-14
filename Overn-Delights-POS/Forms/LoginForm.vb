Imports System.Configuration
Imports System.Drawing
Imports System.Windows.Forms

Public Class LoginForm
    Private _primaryColor As Color
    Private _accentColor As Color

    Public Property CashierID As Integer
    Public Property CashierName As String
    Public Property BranchID As Integer

    Public Sub New()
        InitializeComponent()
        LoadColors()
        SetupUI()
    End Sub

    Private Sub LoadColors()
        Dim primaryHex = If(ConfigurationManager.AppSettings("PrimaryColor"), "#D2691E")
        Dim accentHex = If(ConfigurationManager.AppSettings("AccentColor"), "#FFD700")
        _primaryColor = ColorTranslator.FromHtml(primaryHex)
        _accentColor = ColorTranslator.FromHtml(accentHex)
    End Sub

    Private Sub SetupUI()
        ' Form settings
        Me.FormBorderStyle = FormBorderStyle.None
        Me.WindowState = FormWindowState.Maximized
        Me.BackColor = Color.FromArgb(245, 245, 245)
        Me.StartPosition = FormStartPosition.CenterScreen

        ' Main panel
        Dim mainPanel As New Panel With {
            .Size = New Size(500, 700),
            .BackColor = Color.White,
            .Location = New Point((Me.Width - 500) \ 2, (Me.Height - 700) \ 2)
        }
        mainPanel.Anchor = AnchorStyles.None

        ' Add shadow effect
        AddHandler mainPanel.Paint, Sub(s, e)
                                        e.Graphics.SmoothingMode = Drawing2D.SmoothingMode.AntiAlias
                                        Dim rect = mainPanel.ClientRectangle
                                        rect.Inflate(-2, -2)
                                        Using pen As New Pen(Color.FromArgb(200, 200, 200), 2)
                                            e.Graphics.DrawRectangle(pen, rect)
                                        End Using
                                    End Sub

        ' Logo/Title area
        Dim titlePanel As New Panel With {
            .Size = New Size(500, 150),
            .BackColor = _primaryColor,
            .Dock = DockStyle.Top
        }

        Dim lblTitle As New Label With {
            .Text = If(ConfigurationManager.AppSettings("CompanyName"), "Oven Delights"),
            .Font = New Font("Segoe UI", 48, FontStyle.Bold),
            .ForeColor = Color.White,
            .TextAlign = ContentAlignment.MiddleCenter,
            .Dock = DockStyle.Fill
        }
        titlePanel.Controls.Add(lblTitle)

        Dim lblSubtitle As New Label With {
            .Text = "Point of Sale System",
            .Font = New Font("Segoe UI", 14),
            .ForeColor = Color.FromArgb(240, 240, 240),
            .TextAlign = ContentAlignment.TopCenter,
            .Dock = DockStyle.Bottom,
            .Height = 40
        }
        titlePanel.Controls.Add(lblSubtitle)

        ' Username field
        Dim lblUsername As New Label With {
            .Text = "Username",
            .Font = New Font("Segoe UI", 14, FontStyle.Bold),
            .ForeColor = Color.FromArgb(60, 60, 60),
            .Location = New Point(75, 180),
            .Size = New Size(150, 30)
        }

        Dim txtUsername As New TextBox With {
            .Name = "txtUsername",
            .Font = New Font("Segoe UI", 16),
            .Location = New Point(75, 215),
            .Size = New Size(350, 40),
            .BackColor = Color.FromArgb(250, 250, 250),
            .BorderStyle = BorderStyle.FixedSingle
        }

        ' Password field
        Dim lblPassword As New Label With {
            .Text = "Password",
            .Font = New Font("Segoe UI", 14, FontStyle.Bold),
            .ForeColor = Color.FromArgb(60, 60, 60),
            .Location = New Point(75, 275),
            .Size = New Size(150, 30)
        }

        Dim txtPassword As New TextBox With {
            .Name = "txtPassword",
            .Font = New Font("Segoe UI", 16),
            .Location = New Point(75, 310),
            .Size = New Size(350, 40),
            .BackColor = Color.FromArgb(250, 250, 250),
            .BorderStyle = BorderStyle.FixedSingle,
            .UseSystemPasswordChar = True
        }

        ' Login button
        Dim btnLogin As New Button With {
            .Text = "Login",
            .Font = New Font("Segoe UI", 18, FontStyle.Bold),
            .Size = New Size(350, 60),
            .Location = New Point(75, 380),
            .BackColor = _primaryColor,
            .ForeColor = Color.White,
            .FlatStyle = FlatStyle.Flat,
            .Cursor = Cursors.Hand
        }
        btnLogin.FlatAppearance.BorderSize = 0
        AddHandler btnLogin.Click, Sub() ValidateLogin(txtUsername.Text, txtPassword.Text)
        
        ' Handle Enter key
        AddHandler txtPassword.KeyDown, Sub(s, e)
                                            If e.KeyCode = Keys.Enter Then
                                                ValidateLogin(txtUsername.Text, txtPassword.Text)
                                            End If
                                        End Sub

        ' Exit button
        Dim btnExit As New Button With {
            .Text = "Exit",
            .Font = New Font("Segoe UI", 12),
            .Size = New Size(120, 45),
            .Location = New Point(190, 630),
            .BackColor = Color.FromArgb(108, 117, 125),
            .ForeColor = Color.White,
            .FlatStyle = FlatStyle.Flat,
            .Cursor = Cursors.Hand
        }
        btnExit.FlatAppearance.BorderSize = 0
        AddHandler btnExit.Click, Sub() Application.Exit()

        ' Add all controls
        mainPanel.Controls.AddRange({titlePanel, lblUsername, txtUsername, lblPassword, txtPassword, btnLogin, btnExit})
        Me.Controls.Add(mainPanel)
        
        ' Set focus to username
        txtUsername.Select()
    End Sub

    Private Sub ValidateLogin(username As String, password As String)
        ' Validate input
        If String.IsNullOrWhiteSpace(username) OrElse String.IsNullOrWhiteSpace(password) Then
            MessageBox.Show("Please enter username and password", "Login Required", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            Return
        End If
        
        ' Validate against ERP Users table (same logic as ERP LoginForm)
        Try
            Using conn As New SqlClient.SqlConnection(ConfigurationManager.ConnectionStrings("OvenDelightsERPConnectionString").ConnectionString)
                conn.Open()
                
                Dim sql As String = "
                    SELECT u.UserID, u.Username, u.Password, u.BranchID, u.IsActive, r.RoleName
                    FROM Users u
                    LEFT JOIN Roles r ON u.RoleID = r.RoleID
                    WHERE u.Username = @Username"
                
                Using cmd As New SqlClient.SqlCommand(sql, conn)
                    cmd.Parameters.AddWithValue("@Username", username)
                    
                    Using reader = cmd.ExecuteReader()
                        If reader.Read() Then
                            ' Check if active
                            If Not CBool(reader("IsActive")) Then
                                MessageBox.Show("Account is inactive. Please contact your manager.", 
                                              "Account Inactive", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                                Return
                            End If
                            
                            ' Get stored password
                            Dim storedPassword = If(reader("Password") IsNot DBNull.Value, reader("Password").ToString(), String.Empty)
                            
                            ' Verify password (plain text comparison like ERP)
                            If password = storedPassword Then
                                ' Check role permissions for POS - only Teller role needed
                                Dim roleName = If(reader("RoleName") IsNot DBNull.Value, reader("RoleName").ToString(), "")
                                If roleName = "Teller" Then
                                    ' Valid login
                                    Me.CashierID = CInt(reader("UserID"))
                                    Me.CashierName = reader("Username").ToString()
                                    Me.BranchID = If(IsDBNull(reader("BranchID")), 1, CInt(reader("BranchID")))
                                    Me.DialogResult = DialogResult.OK
                                    Me.Close()
                                Else
                                    MessageBox.Show("Insufficient permissions for POS access." & vbCrLf & "Only users with 'Teller' role can access the POS system.", 
                                                  "Access Denied", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                                End If
                            Else
                                ' Invalid password
                                MessageBox.Show("Invalid username or password.", 
                                              "Login Failed", MessageBoxButtons.OK, MessageBoxIcon.Error)
                            End If
                        Else
                            ' User not found
                            MessageBox.Show("Invalid username or password.", 
                                          "Login Failed", MessageBoxButtons.OK, MessageBoxIcon.Error)
                        End If
                    End Using
                End Using
            End Using
            
        Catch ex As Exception
            MessageBox.Show($"Login error: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub
End Class
