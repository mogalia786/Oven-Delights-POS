Imports System.Configuration
Imports System.Drawing
Imports System.Windows.Forms

Public Class LoginForm
    Private _primaryColor As Color
    Private _accentColor As Color
    Private btnTillSetup As Button

    Public Property CashierID As Integer
    Public Property CashierName As String
    Public Property BranchID As Integer
    Public Property TillPointID As Integer
    Public Property TillNumber As String

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

        ' Till Point Setup button - only show if not configured
        btnTillSetup = New Button With {
            .Text = "⚙ Setup Till Point",
            .Font = New Font("Segoe UI", 11, FontStyle.Bold),
            .Size = New Size(200, 45),
            .Location = New Point(75, 460),
            .BackColor = ColorTranslator.FromHtml("#9B59B6"),
            .ForeColor = Color.White,
            .FlatStyle = FlatStyle.Flat,
            .Cursor = Cursors.Hand,
            .Visible = Not IsTillConfigured()
        }
        btnTillSetup.FlatAppearance.BorderSize = 0
        AddHandler btnTillSetup.Click, AddressOf SetupTillPoint

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
        mainPanel.Controls.AddRange({titlePanel, lblUsername, txtUsername, lblPassword, txtPassword, btnLogin, btnTillSetup, btnExit})
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
                                ' Check role permissions for POS - Teller or Super Administrator
                                Dim roleName = If(reader("RoleName") IsNot DBNull.Value, reader("RoleName").ToString().Trim(), "")

                                ' Debug: Show role name
                                Debug.WriteLine($"User Role: '{roleName}'")

                                If roleName.Equals("Teller", StringComparison.OrdinalIgnoreCase) OrElse
                                   roleName.Equals("Super Administrator", StringComparison.OrdinalIgnoreCase) Then
                                    ' Store user data before closing reader
                                    Dim userID = CInt(reader("UserID"))
                                    Dim userFullName = reader("Username").ToString()
                                    Dim userBranchID = If(IsDBNull(reader("BranchID")), 1, CInt(reader("BranchID")))
                                    reader.Close()

                                    ' Check if Till Point is configured
                                    Me.TillPointID = GetTillPointID()

                                    If Me.TillPointID = 0 Then
                                        MessageBox.Show("Till Point not configured!" & vbCrLf & vbCrLf & "Please click 'Setup Till Point' button to configure this terminal.", "Till Point Required", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                                        Return
                                    End If
                                    
                                    ' CHECK IF TILLS ARE LOCKED BY ERP FINALIZE
                                    Try
                                        Dim dayEndService As New DayEndService()
                                        If dayEndService.IsTillLocked(Me.TillPointID) Then
                                            MessageBox.Show("🔒 ALL TILLS LOCKED 🔒" & vbCrLf & vbCrLf &
                                                          "Day-end has been finalized by Finance." & vbCrLf & vbCrLf &
                                                          "Contact Administrator to reset day-end in ERP:" & vbCrLf &
                                                          "Administration > Reset Day End",
                                                          "Tills Locked", MessageBoxButtons.OK, MessageBoxIcon.Stop)
                                            Return
                                        End If
                                    Catch ex As Exception
                                        MessageBox.Show($"Till lock check failed: {ex.Message}" & vbCrLf & "Please contact support.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
                                        Return
                                    End Try
                                    
                                    ' DAY END CONTROL: Check if previous day is complete for all tills
                                    Try
                                        Dim dayEndService As New DayEndService()
                                        Dim incompleteTills As New List(Of String)
                                        
                                        If Not dayEndService.CheckPreviousDayComplete(incompleteTills) Then
                                            ' Previous day not complete - BLOCK ALL USERS
                                            Dim msg = "❌ LOGIN BLOCKED ❌" & vbCrLf & vbCrLf &
                                                     "Day-end not completed for previous day." & vbCrLf & vbCrLf &
                                                     "Incomplete Tills:" & vbCrLf &
                                                     String.Join(vbCrLf, incompleteTills.Select(Function(t) "  • " & t)) & vbCrLf & vbCrLf &
                                                     "⚠️ SECURITY ALERT ⚠️" & vbCrLf &
                                                     "All tills must complete day-end before next day." & vbCrLf & vbCrLf &
                                                     "Administrator must reset in ERP System:" & vbCrLf &
                                                     "Administration > Reset Day End"
                                            
                                            MessageBox.Show(msg, "Login Blocked - Day End Incomplete", MessageBoxButtons.OK, MessageBoxIcon.Stop)
                                            Return
                                        End If
                                        
                                        ' Check if THIS till already completed day-end for TODAY
                                        If dayEndService.IsTodayDayEndComplete(Me.TillPointID) Then
                                            MessageBox.Show("Day-end already completed for this till today." & vbCrLf & "You cannot log in again today.", "Day End Complete", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                                            Return
                                        End If
                                        
                                        ' Initialize today's day-end record
                                        dayEndService.InitializeTodayDayEnd(Me.TillPointID, userID, userFullName)
                                        
                                    Catch ex As Exception
                                        MessageBox.Show($"Day-end check failed: {ex.Message}" & vbCrLf & "Please contact support.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
                                        Return
                                    End Try

                                    ' Load product cache for this branch
                                    Try
                                        Dim loadingMsg = "Loading products into cache..."
                                        Me.Cursor = Cursors.WaitCursor
                                        
                                        ' Show loading message
                                        Dim lblLoading As New Label With {
                                            .Text = loadingMsg,
                                            .ForeColor = Color.White,
                                            .Font = New Font("Segoe UI", 10, FontStyle.Bold),
                                            .AutoSize = True,
                                            .Location = New Point(20, Me.Height - 60)
                                        }
                                        Me.Controls.Add(lblLoading)
                                        lblLoading.BringToFront()
                                        Application.DoEvents()
                                        
                                        ' Load cache
                                        ProductCacheService.Instance.LoadCache(userBranchID)
                                        
                                        ' Remove loading message
                                        Me.Controls.Remove(lblLoading)
                                        Me.Cursor = Cursors.Default
                                        
                                        Debug.WriteLine($"Cache loaded: {ProductCacheService.Instance.ProductCount} products")
                                    Catch ex As Exception
                                        Me.Cursor = Cursors.Default
                                        MessageBox.Show($"Warning: Failed to load product cache: {ex.Message}" & vbCrLf & "POS may run slower.", "Cache Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                                    End Try
                                    
                                    ' Super Administrator: Show branch selection dialog
                                    If roleName = "Super Administrator" Then

                                    Else
                                        ' Regular Teller: Use assigned branch
                                        Me.CashierID = userID
                                        Me.CashierName = userFullName
                                        Me.BranchID = userBranchID
                                        Me.DialogResult = DialogResult.OK
                                        Me.Close()
                                    End If
                                Else
                                    MessageBox.Show("Insufficient permissions for POS access." & vbCrLf & "Only users with 'Teller' or 'Super Administrator' role can access the POS system.",
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

    Private Function IsTillConfigured() As Boolean
        Try
            Using conn As New SqlClient.SqlConnection(ConfigurationManager.ConnectionStrings("OvenDelightsERPConnectionString").ConnectionString)
                conn.Open()
                Dim sql = "SELECT COUNT(*) FROM TillPoints WHERE MachineName = @MachineName AND IsActive = 1"
                Using cmd As New SqlClient.SqlCommand(sql, conn)
                    cmd.Parameters.AddWithValue("@MachineName", Environment.MachineName)
                    Return CInt(cmd.ExecuteScalar()) > 0
                End Using
            End Using
        Catch
            Return False
        End Try
    End Function

    Private Sub SetupTillPoint()
        ' Request supervisor username
        Dim supervisorUsername = InputBox("Enter Retail Supervisor Username:", "Authorization Required")

        If String.IsNullOrWhiteSpace(supervisorUsername) Then
            Return
        End If

        ' Request supervisor password using secure form
        Using pwdForm As New PasswordInputForm("Enter Retail Supervisor Password:", "Authorization Required")
            If pwdForm.ShowDialog() <> DialogResult.OK Then
                Return
            End If
            Dim supervisorPassword = pwdForm.Password

            If String.IsNullOrWhiteSpace(supervisorPassword) Then
                Return
            End If

            ' Validate supervisor credentials
            Try
                Using conn As New SqlClient.SqlConnection(ConfigurationManager.ConnectionStrings("OvenDelightsERPConnectionString").ConnectionString)
                    conn.Open()

                    ' Validate credentials
                    Dim checkUserSql = "SELECT COUNT(*) FROM Users u INNER JOIN Roles r ON u.RoleID = r.RoleID WHERE u.Username = @Username AND u.Password = @Password AND r.RoleName = 'Retail Supervisor' AND u.IsActive = 1"
                    Using cmdCheck As New SqlClient.SqlCommand(checkUserSql, conn)
                        cmdCheck.Parameters.AddWithValue("@Username", supervisorUsername)
                        cmdCheck.Parameters.AddWithValue("@Password", supervisorPassword)

                        If CInt(cmdCheck.ExecuteScalar()) = 0 Then
                            MessageBox.Show("Invalid Retail Supervisor credentials!", "Authorization Failed", MessageBoxButtons.OK, MessageBoxIcon.Error)
                            Return
                        End If
                    End Using

                    ' Show Till Point setup form
                    Dim branchID = 1 ' Default branch, or get from config
                    Using tillSetupForm As New TillPointSetupForm(ConfigurationManager.ConnectionStrings("OvenDelightsERPConnectionString").ConnectionString, branchID)
                        If tillSetupForm.ShowDialog() = DialogResult.OK Then
                            MessageBox.Show($"Till Point '{tillSetupForm.TillNumber}' has been configured!" & vbCrLf & vbCrLf & "You can now login.", "Setup Complete", MessageBoxButtons.OK, MessageBoxIcon.Information)
                            ' Hide the setup button now that till is configured
                            btnTillSetup.Visible = False
                        End If
                    End Using
                End Using
            Catch ex As Exception
                MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
            End Try
        End Using
    End Sub

    Private Function GetTillPointID() As Integer
        Try
            Using conn As New SqlClient.SqlConnection(ConfigurationManager.ConnectionStrings("OvenDelightsERPConnectionString").ConnectionString)
                conn.Open()

                ' Try to find till point by machine name
                Dim sql = "SELECT TOP 1 TillPointID, TillNumber FROM TillPoints WHERE MachineName = @MachineName AND IsActive = 1"
                Using cmd As New SqlClient.SqlCommand(sql, conn)
                    cmd.Parameters.AddWithValue("@MachineName", Environment.MachineName)

                    Using reader = cmd.ExecuteReader()
                        If reader.Read() Then
                            TillNumber = reader("TillNumber").ToString()
                            Return CInt(reader("TillPointID"))
                        End If
                    End Using
                End Using
            End Using
        Catch
        End Try
        Return 0
    End Function
End Class
