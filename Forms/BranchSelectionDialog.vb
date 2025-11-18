Imports System.Configuration
Imports System.Data.SqlClient
Imports System.Drawing
Imports System.Windows.Forms

Public Class BranchSelectionDialog
    Inherits Form

    Private _connectionString As String
    Private _selectedBranchID As Integer = 0
    Private _selectedBranchName As String = ""
    
    Private cmbBranch As ComboBox
    Private btnConfirm As Button
    Private btnCancel As Button
    
    ' Modern color palette
    Private _darkBlue As Color = ColorTranslator.FromHtml("#2C3E50")
    Private _lightBlue As Color = ColorTranslator.FromHtml("#3498DB")
    Private _green As Color = ColorTranslator.FromHtml("#27AE60")
    Private _red As Color = ColorTranslator.FromHtml("#E74C3C")
    Private _lightGray As Color = ColorTranslator.FromHtml("#ECF0F1")
    Private _yellow As Color = ColorTranslator.FromHtml("#F39C12")
    
    Public ReadOnly Property SelectedBranchID As Integer
        Get
            Return _selectedBranchID
        End Get
    End Property
    
    Public ReadOnly Property SelectedBranchName As String
        Get
            Return _selectedBranchName
        End Get
    End Property
    
    Public Sub New()
        MyBase.New()
        _connectionString = ConfigurationManager.ConnectionStrings("OvenDelightsERPConnectionString").ConnectionString
        InitializeComponent()
        LoadBranches()
    End Sub
    
    Private Sub InitializeComponent()
        Me.Text = "Branch Selection"
        Me.Size = New Size(600, 450)
        Me.StartPosition = FormStartPosition.CenterScreen
        Me.FormBorderStyle = FormBorderStyle.None
        Me.BackColor = Color.White
        
        ' Header Panel
        Dim pnlHeader As New Panel With {
            .Dock = DockStyle.Top,
            .Height = 100,
            .BackColor = _darkBlue
        }
        
        Dim lblIcon As New Label With {
            .Text = "🏢",
            .Font = New Font("Segoe UI", 48),
            .ForeColor = _yellow,
            .Location = New Point(20, 15),
            .AutoSize = True
        }
        
        Dim lblTitle As New Label With {
            .Text = "SELECT BRANCH",
            .Font = New Font("Segoe UI", 24, FontStyle.Bold),
            .ForeColor = Color.White,
            .Location = New Point(120, 20),
            .AutoSize = True
        }
        
        Dim lblSubtitle As New Label With {
            .Text = "Super Administrator Access",
            .Font = New Font("Segoe UI", 12, FontStyle.Italic),
            .ForeColor = _lightGray,
            .Location = New Point(120, 60),
            .AutoSize = True
        }
        
        pnlHeader.Controls.AddRange({lblIcon, lblTitle, lblSubtitle})
        
        ' Content Panel
        Dim pnlContent As New Panel With {
            .Location = New Point(0, 100),
            .Size = New Size(600, 250),
            .BackColor = Color.White
        }
        
        ' Info Label
        Dim lblInfo As New Label With {
            .Text = "As a Super Administrator, you have access to all branches." & vbCrLf & 
                    "Please select which branch you would like to view:",
            .Font = New Font("Segoe UI", 11),
            .ForeColor = _darkBlue,
            .Location = New Point(40, 30),
            .Size = New Size(520, 60),
            .TextAlign = ContentAlignment.MiddleLeft
        }
        
        ' Branch Label
        Dim lblBranch As New Label With {
            .Text = "Branch:",
            .Font = New Font("Segoe UI", 14, FontStyle.Bold),
            .ForeColor = _darkBlue,
            .Location = New Point(40, 110),
            .AutoSize = True
        }
        
        ' Branch ComboBox
        cmbBranch = New ComboBox With {
            .Font = New Font("Segoe UI", 14),
            .Location = New Point(40, 145),
            .Size = New Size(520, 40),
            .DropDownStyle = ComboBoxStyle.DropDownList,
            .BackColor = Color.White,
            .ForeColor = _darkBlue
        }
        
        pnlContent.Controls.AddRange({lblInfo, lblBranch, cmbBranch})
        
        ' Button Panel
        Dim pnlButtons As New Panel With {
            .Dock = DockStyle.Bottom,
            .Height = 100,
            .BackColor = _lightGray
        }
        
        ' Confirm Button
        btnConfirm = New Button With {
            .Text = "✓ CONFIRM SELECTION",
            .Font = New Font("Segoe UI", 14, FontStyle.Bold),
            .Size = New Size(250, 60),
            .Location = New Point(80, 20),
            .BackColor = _green,
            .ForeColor = Color.White,
            .FlatStyle = FlatStyle.Flat,
            .Cursor = Cursors.Hand,
            .Enabled = False
        }
        btnConfirm.FlatAppearance.BorderSize = 0
        AddHandler btnConfirm.Click, AddressOf BtnConfirm_Click
        
        ' Cancel Button
        btnCancel = New Button With {
            .Text = "✖ CANCEL",
            .Font = New Font("Segoe UI", 14, FontStyle.Bold),
            .Size = New Size(150, 60),
            .Location = New Point(350, 20),
            .BackColor = _red,
            .ForeColor = Color.White,
            .FlatStyle = FlatStyle.Flat,
            .Cursor = Cursors.Hand
        }
        btnCancel.FlatAppearance.BorderSize = 0
        AddHandler btnCancel.Click, Sub()
                                        Me.DialogResult = DialogResult.Cancel
                                        Me.Close()
                                    End Sub
        
        pnlButtons.Controls.AddRange({btnConfirm, btnCancel})
        
        ' Enable confirm button when branch is selected
        AddHandler cmbBranch.SelectedIndexChanged, Sub()
                                                       btnConfirm.Enabled = (cmbBranch.SelectedIndex >= 0)
                                                   End Sub
        
        Me.Controls.AddRange({pnlHeader, pnlContent, pnlButtons})
    End Sub
    
    Private Sub LoadBranches()
        Try
            Dim dt As New DataTable()
            
            Using conn As New SqlConnection(_connectionString)
                conn.Open()
                
                Dim sql = "
                    SELECT 
                        BranchID,
                        BranchCode,
                        BranchName,
                        BranchAddress,
                        BranchPhone
                    FROM Branches
                    WHERE IsActive = 1
                    ORDER BY BranchName"
                
                Using cmd As New SqlCommand(sql, conn)
                    Using adapter As New SqlDataAdapter(cmd)
                        adapter.Fill(dt)
                    End Using
                End Using
            End Using
            
            If dt.Rows.Count = 0 Then
                MessageBox.Show("No active branches found in the system.", "No Branches", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                Me.DialogResult = DialogResult.Cancel
                Me.Close()
                Return
            End If
            
            ' Populate combo box with formatted branch names
            cmbBranch.Items.Clear()
            For Each row As DataRow In dt.Rows
                Dim branchCode = row("BranchCode").ToString()
                Dim branchName = row("BranchName").ToString()
                Dim branchAddress = If(IsDBNull(row("BranchAddress")), "", row("BranchAddress").ToString())
                
                ' Create display text: "OD200 - Avondale (123 Main St)"
                Dim displayText = $"{branchCode} - {branchName}"
                If Not String.IsNullOrWhiteSpace(branchAddress) Then
                    displayText &= $" ({branchAddress})"
                End If
                
                ' Store BranchID in Tag
                cmbBranch.Items.Add(New BranchItem With {
                    .BranchID = CInt(row("BranchID")),
                    .DisplayText = displayText,
                    .BranchName = branchName
                })
            Next
            
            ' Auto-select first branch
            If cmbBranch.Items.Count > 0 Then
                cmbBranch.SelectedIndex = 0
            End If
            
        Catch ex As Exception
            MessageBox.Show($"Error loading branches: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
            Me.DialogResult = DialogResult.Cancel
            Me.Close()
        End Try
    End Sub
    
    Private Sub BtnConfirm_Click(sender As Object, e As EventArgs)
        If cmbBranch.SelectedIndex < 0 Then
            MessageBox.Show("Please select a branch.", "Selection Required", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            Return
        End If
        
        Dim selectedItem = CType(cmbBranch.SelectedItem, BranchItem)
        _selectedBranchID = selectedItem.BranchID
        _selectedBranchName = selectedItem.BranchName
        
        Me.DialogResult = DialogResult.OK
        Me.Close()
    End Sub
    
    ' Helper class to store branch data in ComboBox
    Private Class BranchItem
        Public Property BranchID As Integer
        Public Property DisplayText As String
        Public Property BranchName As String
        
        Public Overrides Function ToString() As String
            Return DisplayText
        End Function
    End Class
End Class
