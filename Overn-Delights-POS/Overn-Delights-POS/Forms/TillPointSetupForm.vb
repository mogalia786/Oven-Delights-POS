Imports System.Windows.Forms
Imports System.Drawing
Imports System.Data.SqlClient

Public Class TillPointSetupForm
    Inherits Form
    
    Private _connectionString As String
    Private _branchID As Integer
    Private _branchPrefix As String
    Private txtTillNumber As TextBox
    Private btnSave As Button
    Private btnCancel As Button
    
    Public Property TillPointID As Integer = 0
    Public Property TillNumber As String = ""
    
    Public Sub New(connectionString As String, branchID As Integer)
        _connectionString = connectionString
        _branchID = branchID
        _branchPrefix = GetBranchPrefix()
        InitializeComponent()
    End Sub
    
    Private Sub InitializeComponent()
        Me.Text = "Till Point Setup"
        Me.Size = New Size(500, 300)
        Me.StartPosition = FormStartPosition.CenterScreen
        Me.FormBorderStyle = FormBorderStyle.FixedDialog
        Me.MaximizeBox = False
        Me.MinimizeBox = False
        Me.BackColor = ColorTranslator.FromHtml("#ECF0F1")
        
        ' Header
        Dim pnlHeader As New Panel With {
            .Dock = DockStyle.Top,
            .Height = 80,
            .BackColor = ColorTranslator.FromHtml("#3498DB")
        }
        
        Dim lblTitle As New Label With {
            .Text = "TILL POINT SETUP",
            .Font = New Font("Segoe UI", 24, FontStyle.Bold),
            .ForeColor = Color.White,
            .AutoSize = True,
            .Location = New Point(100, 20)
        }
        pnlHeader.Controls.Add(lblTitle)
        
        ' Instructions
        Dim lblInstructions As New Label With {
            .Text = "Enter Till Number (will be prefixed with " & _branchPrefix & "):" & vbCrLf & 
                    "(e.g., TILL-01, TILL-02, COUNTER-A, etc.)",
            .Font = New Font("Segoe UI", 11),
            .ForeColor = ColorTranslator.FromHtml("#34495E"),
            .Location = New Point(50, 100),
            .Size = New Size(400, 50)
        }
        
        ' Till Number input
        Dim lblTillNumber As New Label With {
            .Text = "Till Number:",
            .Font = New Font("Segoe UI", 12, FontStyle.Bold),
            .ForeColor = ColorTranslator.FromHtml("#2C3E50"),
            .Location = New Point(50, 170),
            .AutoSize = True
        }
        
        txtTillNumber = New TextBox With {
            .Font = New Font("Segoe UI", 14),
            .Location = New Point(180, 165),
            .Size = New Size(250, 35),
            .MaxLength = 50
        }
        
        ' Buttons
        btnSave = New Button With {
            .Text = "SAVE",
            .Font = New Font("Segoe UI", 12, FontStyle.Bold),
            .Size = New Size(120, 45),
            .Location = New Point(150, 220),
            .BackColor = ColorTranslator.FromHtml("#27AE60"),
            .ForeColor = Color.White,
            .FlatStyle = FlatStyle.Flat,
            .Cursor = Cursors.Hand
        }
        btnSave.FlatAppearance.BorderSize = 0
        AddHandler btnSave.Click, AddressOf BtnSave_Click
        
        btnCancel = New Button With {
            .Text = "CANCEL",
            .Font = New Font("Segoe UI", 12, FontStyle.Bold),
            .Size = New Size(120, 45),
            .Location = New Point(280, 220),
            .BackColor = ColorTranslator.FromHtml("#E74C3C"),
            .ForeColor = Color.White,
            .FlatStyle = FlatStyle.Flat,
            .Cursor = Cursors.Hand
        }
        btnCancel.FlatAppearance.BorderSize = 0
        AddHandler btnCancel.Click, AddressOf BtnCancel_Click
        
        Me.Controls.AddRange({pnlHeader, lblInstructions, lblTillNumber, txtTillNumber, btnSave, btnCancel})
        Me.AcceptButton = btnSave
    End Sub
    
    Private Sub BtnSave_Click(sender As Object, e As EventArgs)
        If String.IsNullOrWhiteSpace(txtTillNumber.Text) Then
            MessageBox.Show("Please enter a Till Number!", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            txtTillNumber.Focus()
            Return
        End If
        
        Try
            ' Create full till number with branch prefix
            Dim fullTillNumber = $"{_branchPrefix}-{txtTillNumber.Text.Trim().ToUpper()}"
            
            Using conn As New SqlConnection(_connectionString)
                conn.Open()
                
                ' Check if till number already exists
                Dim checkSql = "SELECT COUNT(*) FROM TillPoints WHERE TillNumber = @TillNumber AND BranchID = @BranchID"
                Using cmdCheck As New SqlCommand(checkSql, conn)
                    cmdCheck.Parameters.AddWithValue("@TillNumber", fullTillNumber)
                    cmdCheck.Parameters.AddWithValue("@BranchID", _branchID)
                    
                    If CInt(cmdCheck.ExecuteScalar()) > 0 Then
                        MessageBox.Show("This Till Number already exists! Please choose a different number.", "Duplicate Till Number", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                        txtTillNumber.SelectAll()
                        txtTillNumber.Focus()
                        Return
                    End If
                End Using
                
                ' Insert new till point
                Dim insertSql = "INSERT INTO TillPoints (TillNumber, BranchID, MachineName, IsActive) 
                                VALUES (@TillNumber, @BranchID, @MachineName, 1);
                                SELECT CAST(SCOPE_IDENTITY() AS INT)"
                
                Using cmdInsert As New SqlCommand(insertSql, conn)
                    cmdInsert.Parameters.AddWithValue("@TillNumber", fullTillNumber)
                    cmdInsert.Parameters.AddWithValue("@BranchID", _branchID)
                    cmdInsert.Parameters.AddWithValue("@MachineName", Environment.MachineName)
                    
                    TillPointID = CInt(cmdInsert.ExecuteScalar())
                    TillNumber = fullTillNumber
                End Using
            End Using
            
            MessageBox.Show($"Till Point '{TillNumber}' configured successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information)
            Me.DialogResult = DialogResult.OK
            Me.Close()
        Catch ex As Exception
            MessageBox.Show($"Error saving Till Point: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub
    
    Private Function GetBranchPrefix() As String
        Try
            Using conn As New SqlConnection(_connectionString)
                conn.Open()
                Dim sql = "SELECT BranchCode FROM Branches WHERE BranchID = @BranchID"
                Using cmd As New SqlCommand(sql, conn)
                    cmd.Parameters.AddWithValue("@BranchID", _branchID)
                    Dim result = cmd.ExecuteScalar()
                    Return If(result IsNot Nothing, result.ToString(), "POS")
                End Using
            End Using
        Catch
            Return "POS"
        End Try
    End Function
    
    Private Sub BtnCancel_Click(sender As Object, e As EventArgs)
        Me.DialogResult = DialogResult.Cancel
        Me.Close()
    End Sub
End Class
