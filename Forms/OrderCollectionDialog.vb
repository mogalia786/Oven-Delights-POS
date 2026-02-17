Imports System.Windows.Forms
Imports System.Drawing

Public Class OrderCollectionDialog
    Inherits Form
    
    Private txtOrderNumber As TextBox
    Private btnCollect As Button
    Private btnCancel As Button
    Private _keyboard As OrderEntryKeyboard
    Private _numpad As OrderEntryNumpad
    
    Private _darkBlue As Color = ColorTranslator.FromHtml("#2C3E50")
    Private _lightBlue As Color = ColorTranslator.FromHtml("#3498DB")
    Private _green As Color = ColorTranslator.FromHtml("#27AE60")
    Private _orange As Color = ColorTranslator.FromHtml("#E67E22")
    Private _lightGray As Color = ColorTranslator.FromHtml("#ECF0F1")
    
    Public Property OrderNumber As String = ""
    
    Public Sub New()
        InitializeComponent()
    End Sub
    
    Private Sub InitializeComponent()
        Me.Text = "Order Collection"
        Me.Size = New Size(900, 850)
        Me.StartPosition = FormStartPosition.CenterScreen
        Me.FormBorderStyle = FormBorderStyle.None
        Me.BackColor = Color.White
        
        ' Header panel
        Dim pnlHeader As New Panel With {
            .Dock = DockStyle.Top,
            .Height = 80,
            .BackColor = _lightBlue
        }
        
        Dim lblHeader As New Label With {
            .Text = "📦 ORDER COLLECTION",
            .Font = New Font("Segoe UI", 24, FontStyle.Bold),
            .ForeColor = Color.White,
            .TextAlign = ContentAlignment.MiddleCenter,
            .Dock = DockStyle.Fill
        }
        pnlHeader.Controls.Add(lblHeader)
        
        ' Content panel
        Dim pnlContent As New Panel With {
            .Location = New Point(50, 120),
            .Size = New Size(500, 230),
            .BackColor = Color.White
        }
        
        ' Instructions
        Dim lblInstructions As New Label With {
            .Text = "Enter the complete order number:",
            .Font = New Font("Segoe UI", 14, FontStyle.Bold),
            .Location = New Point(0, 0),
            .Size = New Size(500, 30),
            .ForeColor = _darkBlue
        }
        pnlContent.Controls.Add(lblInstructions)
        
        ' Examples
        Dim lblExamples As New Label With {
            .Text = "Cake Order: O-JHB-CCAKE-000001" & vbCrLf & "General Order: O-JHB-000001",
            .Font = New Font("Segoe UI", 11),
            .Location = New Point(0, 35),
            .Size = New Size(500, 50),
            .ForeColor = _orange
        }
        pnlContent.Controls.Add(lblExamples)
        
        ' Order number input
        txtOrderNumber = New TextBox With {
            .Font = New Font("Segoe UI", 18, FontStyle.Bold),
            .Location = New Point(0, 95),
            .Size = New Size(500, 40),
            .CharacterCasing = CharacterCasing.Upper,
            .BorderStyle = BorderStyle.FixedSingle
        }
        pnlContent.Controls.Add(txtOrderNumber)
        
        ' Keyboard/Numpad helper buttons
        Dim pnlHelpers As New Panel With {
            .Location = New Point(0, 150),
            .Size = New Size(500, 60),
            .BackColor = Color.White
        }
        
        Dim btnKeyboard As New Button With {
            .Text = "⌨️ Keyboard",
            .Font = New Font("Segoe UI", 12, FontStyle.Bold),
            .Size = New Size(240, 50),
            .Location = New Point(0, 0),
            .BackColor = _green,
            .ForeColor = Color.White,
            .FlatStyle = FlatStyle.Flat,
            .Cursor = Cursors.Hand
        }
        btnKeyboard.FlatAppearance.BorderSize = 0
        AddHandler btnKeyboard.Click, Sub()
            If _numpad IsNot Nothing Then _numpad.HideNumpad()
            _keyboard.ShowKeyboard()
        End Sub
        pnlHelpers.Controls.Add(btnKeyboard)
        
        Dim btnNumpad As New Button With {
            .Text = "🔢 Numpad",
            .Font = New Font("Segoe UI", 12, FontStyle.Bold),
            .Size = New Size(240, 50),
            .Location = New Point(260, 0),
            .BackColor = _orange,
            .ForeColor = Color.White,
            .FlatStyle = FlatStyle.Flat,
            .Cursor = Cursors.Hand
        }
        btnNumpad.FlatAppearance.BorderSize = 0
        AddHandler btnNumpad.Click, Sub()
            If _keyboard IsNot Nothing Then _keyboard.HideKeyboard()
            _numpad.ShowNumpad()
        End Sub
        pnlHelpers.Controls.Add(btnNumpad)
        
        pnlContent.Controls.Add(pnlHelpers)
        
        ' Create keyboard and numpad
        _keyboard = New OrderEntryKeyboard(txtOrderNumber) With {
            .Location = New Point(50, 280),
            .Visible = False
        }
        Me.Controls.Add(_keyboard)
        
        _numpad = New OrderEntryNumpad(txtOrderNumber) With {
            .Location = New Point(250, 280),
            .Visible = False
        }
        Me.Controls.Add(_numpad)
        
        ' Button panel
        Dim pnlButtons As New Panel With {
            .Location = New Point(200, 770),
            .Size = New Size(500, 60),
            .BackColor = Color.White
        }
        
        btnCollect = New Button With {
            .Text = "✓ COLLECT ORDER",
            .Font = New Font("Segoe UI", 14, FontStyle.Bold),
            .Size = New Size(240, 60),
            .Location = New Point(0, 0),
            .BackColor = _green,
            .ForeColor = Color.White,
            .FlatStyle = FlatStyle.Flat,
            .Cursor = Cursors.Hand
        }
        btnCollect.FlatAppearance.BorderSize = 0
        AddHandler btnCollect.Click, AddressOf btnCollect_Click
        pnlButtons.Controls.Add(btnCollect)
        
        btnCancel = New Button With {
            .Text = "✗ CANCEL",
            .Font = New Font("Segoe UI", 14, FontStyle.Bold),
            .Size = New Size(240, 60),
            .Location = New Point(260, 0),
            .BackColor = _lightGray,
            .ForeColor = _darkBlue,
            .FlatStyle = FlatStyle.Flat,
            .Cursor = Cursors.Hand
        }
        btnCancel.FlatAppearance.BorderSize = 0
        AddHandler btnCancel.Click, AddressOf btnCancel_Click
        pnlButtons.Controls.Add(btnCancel)
        
        Me.Controls.AddRange({pnlHeader, pnlContent, pnlButtons})
        
        ' Focus on textbox
        txtOrderNumber.Select()
    End Sub
    
    Private Sub btnCollect_Click(sender As Object, e As EventArgs)
        OrderNumber = txtOrderNumber.Text.Trim().ToUpper()
        
        If String.IsNullOrWhiteSpace(OrderNumber) Then
            MessageBox.Show("Please enter an order number", "Required", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            Return
        End If
        
        If Not OrderNumber.StartsWith("O-") Then
            MessageBox.Show("Invalid order number format!" & vbCrLf & vbCrLf & "Order number must start with O-", "Invalid Format", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            Return
        End If
        
        Me.DialogResult = DialogResult.OK
        Me.Close()
    End Sub
    
    Private Sub btnCancel_Click(sender As Object, e As EventArgs)
        Me.DialogResult = DialogResult.Cancel
        Me.Close()
    End Sub
End Class
