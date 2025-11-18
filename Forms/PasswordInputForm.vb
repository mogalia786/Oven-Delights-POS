Imports System.Windows.Forms
Imports System.Drawing

Public Class PasswordInputForm
    Inherits Form
    
    Private txtPassword As TextBox
    Private btnOK As Button
    Private btnCancel As Button
    
    Public Property Password As String = ""
    
    Public Sub New(prompt As String, title As String)
        InitializeComponent(prompt, title)
    End Sub
    
    Private Sub InitializeComponent(prompt As String, title As String)
        Me.Text = title
        Me.Size = New Size(400, 180)
        Me.StartPosition = FormStartPosition.CenterScreen
        Me.FormBorderStyle = FormBorderStyle.FixedDialog
        Me.MaximizeBox = False
        Me.MinimizeBox = False
        Me.BackColor = Color.White
        
        ' Prompt label
        Dim lblPrompt As New Label With {
            .Text = prompt,
            .Font = New Font("Segoe UI", 11),
            .Location = New Point(20, 20),
            .Size = New Size(360, 30),
            .ForeColor = ColorTranslator.FromHtml("#2C3E50")
        }
        
        ' Password textbox
        txtPassword = New TextBox With {
            .Font = New Font("Segoe UI", 12),
            .Location = New Point(20, 60),
            .Size = New Size(340, 30),
            .UseSystemPasswordChar = True
        }
        
        ' OK button
        btnOK = New Button With {
            .Text = "OK",
            .Font = New Font("Segoe UI", 10, FontStyle.Bold),
            .Size = New Size(100, 35),
            .Location = New Point(160, 105),
            .BackColor = ColorTranslator.FromHtml("#27AE60"),
            .ForeColor = Color.White,
            .FlatStyle = FlatStyle.Flat,
            .Cursor = Cursors.Hand
        }
        btnOK.FlatAppearance.BorderSize = 0
        AddHandler btnOK.Click, AddressOf BtnOK_Click
        
        ' Cancel button
        btnCancel = New Button With {
            .Text = "Cancel",
            .Font = New Font("Segoe UI", 10, FontStyle.Bold),
            .Size = New Size(100, 35),
            .Location = New Point(270, 105),
            .BackColor = ColorTranslator.FromHtml("#E74C3C"),
            .ForeColor = Color.White,
            .FlatStyle = FlatStyle.Flat,
            .Cursor = Cursors.Hand
        }
        btnCancel.FlatAppearance.BorderSize = 0
        AddHandler btnCancel.Click, AddressOf BtnCancel_Click
        
        Me.Controls.AddRange({lblPrompt, txtPassword, btnOK, btnCancel})
        Me.AcceptButton = btnOK
        Me.CancelButton = btnCancel
        
        AddHandler Me.Shown, Sub() txtPassword.Focus()
    End Sub
    
    Private Sub BtnOK_Click(sender As Object, e As EventArgs)
        Password = txtPassword.Text
        Me.DialogResult = DialogResult.OK
        Me.Close()
    End Sub
    
    Private Sub BtnCancel_Click(sender As Object, e As EventArgs)
        Me.DialogResult = DialogResult.Cancel
        Me.Close()
    End Sub
End Class
