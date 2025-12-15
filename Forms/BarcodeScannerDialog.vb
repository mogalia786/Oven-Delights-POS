Imports System.Drawing
Imports System.Windows.Forms

Public Class BarcodeScannerDialog
    Inherits Form
    
    Private txtBarcode As TextBox
    Private lblInstruction As Label
    Private btnCancel As Button
    
    Public Property ScannedBarcode As String = ""
    
    Public Sub New(title As String, instruction As String)
        InitializeComponent(title, instruction)
    End Sub
    
    Private Sub InitializeComponent(title As String, instruction As String)
        Me.Text = title
        Me.Size = New Size(500, 250)
        Me.StartPosition = FormStartPosition.CenterScreen
        Me.FormBorderStyle = FormBorderStyle.FixedDialog
        Me.MaximizeBox = False
        Me.MinimizeBox = False
        Me.BackColor = ColorTranslator.FromHtml("#0a0e27")
        
        ' Instruction label
        lblInstruction = New Label With {
            .Text = instruction,
            .Font = New Font("Segoe UI", 14, FontStyle.Bold),
            .ForeColor = ColorTranslator.FromHtml("#FFD700"),
            .Location = New Point(20, 20),
            .Size = New Size(460, 60),
            .TextAlign = ContentAlignment.MiddleCenter
        }
        Me.Controls.Add(lblInstruction)
        
        ' Barcode textbox
        txtBarcode = New TextBox With {
            .Font = New Font("Courier New", 18, FontStyle.Bold),
            .Location = New Point(50, 100),
            .Size = New Size(400, 40),
            .TextAlign = HorizontalAlignment.Center,
            .BackColor = Color.White,
            .ForeColor = Color.Black
        }
        AddHandler txtBarcode.KeyDown, AddressOf txtBarcode_KeyDown
        Me.Controls.Add(txtBarcode)
        
        ' Cancel button
        btnCancel = New Button With {
            .Text = "CANCEL (ESC)",
            .Font = New Font("Segoe UI", 11, FontStyle.Bold),
            .Location = New Point(175, 160),
            .Size = New Size(150, 45),
            .BackColor = ColorTranslator.FromHtml("#C1272D"),
            .ForeColor = Color.White,
            .FlatStyle = FlatStyle.Flat,
            .Cursor = Cursors.Hand
        }
        btnCancel.FlatAppearance.BorderSize = 0
        AddHandler btnCancel.Click, Sub() Me.DialogResult = DialogResult.Cancel
        Me.Controls.Add(btnCancel)
        
        Me.AcceptButton = Nothing ' Enter will be handled in KeyDown
        Me.CancelButton = btnCancel
    End Sub
    
    Private Sub txtBarcode_KeyDown(sender As Object, e As KeyEventArgs)
        If e.KeyCode = Keys.Enter Then
            e.SuppressKeyPress = True
            Dim scannedValue = txtBarcode.Text.Trim()
            
            If String.IsNullOrWhiteSpace(scannedValue) Then
                MessageBox.Show("Please scan a barcode first!", "No Barcode", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                Return
            End If
            
            ' Scanner returns 13-digit padded barcode, remove leading zeros to get order number
            ' Example: Scanner reads "0000006000009" -> Return "6000009"
            ScannedBarcode = scannedValue.TrimStart("0"c)
            
            ' If all zeros were trimmed, keep at least one zero
            If String.IsNullOrEmpty(ScannedBarcode) Then
                ScannedBarcode = "0"
            End If
            
            Me.DialogResult = DialogResult.OK
            Me.Close()
        ElseIf e.KeyCode = Keys.Escape Then
            e.SuppressKeyPress = True
            Me.DialogResult = DialogResult.Cancel
            Me.Close()
        End If
    End Sub
    
    Protected Overrides Sub OnShown(e As EventArgs)
        MyBase.OnShown(e)
        txtBarcode.Focus()
    End Sub
End Class
