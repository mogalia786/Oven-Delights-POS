Imports System.Windows.Forms

Public Class PriceOverrideDialog
    Inherits Form

    Private txtPrice As TextBox
    Private lblProduct As Label
    Private lblOriginalPrice As Label
    Private btnOK As Button
    Private btnCancel As Button
    
    Private _productName As String
    Private _originalPrice As Decimal
    Private _newPrice As Decimal

    Public ReadOnly Property NewPrice As Decimal
        Get
            Return _newPrice
        End Get
    End Property

    Public Sub New(productName As String, originalPrice As Decimal)
        _productName = productName
        _originalPrice = originalPrice
        InitializeComponent()
    End Sub

    Private Sub InitializeComponent()
        Me.Text = "Override Price"
        Me.Size = New Size(340, 470)
        Me.FormBorderStyle = FormBorderStyle.FixedDialog
        Me.StartPosition = FormStartPosition.CenterParent
        Me.MaximizeBox = False
        Me.MinimizeBox = False
        Me.BackColor = Color.White

        Dim yPos = 10

        ' Title
        Dim lblTitle As New Label With {
            .Text = "Override Price",
            .Font = New Font("Segoe UI", 11, FontStyle.Bold),
            .Location = New Point(10, yPos),
            .Size = New Size(320, 22),
            .TextAlign = ContentAlignment.MiddleCenter
        }
        Me.Controls.Add(lblTitle)
        yPos += 25

        ' Product name
        lblProduct = New Label With {
            .Text = "Product: " & _productName,
            .Font = New Font("Segoe UI", 8, FontStyle.Regular),
            .Location = New Point(10, yPos),
            .Size = New Size(320, 18),
            .TextAlign = ContentAlignment.MiddleLeft
        }
        Me.Controls.Add(lblProduct)
        yPos += 20

        ' Original price
        lblOriginalPrice = New Label With {
            .Text = "Original: R " & _originalPrice.ToString("N2"),
            .Font = New Font("Segoe UI", 8, FontStyle.Regular),
            .Location = New Point(10, yPos),
            .Size = New Size(320, 18),
            .TextAlign = ContentAlignment.MiddleLeft,
            .ForeColor = Color.Gray
        }
        Me.Controls.Add(lblOriginalPrice)
        yPos += 22

        ' Separator
        Dim separator2 As New Panel With {
            .Location = New Point(10, yPos),
            .Size = New Size(320, 1),
            .BackColor = Color.LightGray
        }
        Me.Controls.Add(separator2)
        yPos += 8

        ' Price input with R prefix
        Dim lblR As New Label With {
            .Text = "R",
            .Font = New Font("Segoe UI", 12, FontStyle.Bold),
            .Location = New Point(60, yPos),
            .Size = New Size(20, 30),
            .TextAlign = ContentAlignment.MiddleLeft
        }
        Me.Controls.Add(lblR)

        txtPrice = New TextBox With {
            .Font = New Font("Segoe UI", 12, FontStyle.Regular),
            .Location = New Point(80, yPos),
            .Size = New Size(180, 30),
            .TextAlign = HorizontalAlignment.Right,
            .Text = "0.00",
            .ReadOnly = True,
            .BackColor = Color.White
        }
        Me.Controls.Add(txtPrice)
        yPos += 38

        ' Separator
        Dim separator3 As New Panel With {
            .Location = New Point(10, yPos),
            .Size = New Size(320, 1),
            .BackColor = Color.LightGray
        }
        Me.Controls.Add(separator3)
        yPos += 8

        ' Numeric keypad
        Dim buttonSize = 52
        Dim buttonSpacing = 6
        Dim startX = 42

        ' Row 1: 7, 8, 9, Backspace
        AddNumberButton("7", startX, yPos, buttonSize)
        AddNumberButton("8", startX + buttonSize + buttonSpacing, yPos, buttonSize)
        AddNumberButton("9", startX + (buttonSize + buttonSpacing) * 2, yPos, buttonSize)
        AddSpecialButton("←", startX + (buttonSize + buttonSpacing) * 3, yPos, buttonSize, AddressOf BackspaceClick)
        yPos += buttonSize + buttonSpacing

        ' Row 2: 4, 5, 6, Decimal
        AddNumberButton("4", startX, yPos, buttonSize)
        AddNumberButton("5", startX + buttonSize + buttonSpacing, yPos, buttonSize)
        AddNumberButton("6", startX + (buttonSize + buttonSpacing) * 2, yPos, buttonSize)
        AddSpecialButton(".", startX + (buttonSize + buttonSpacing) * 3, yPos, buttonSize, AddressOf DecimalClick)
        yPos += buttonSize + buttonSpacing

        ' Row 3: 1, 2, 3, Clear
        AddNumberButton("1", startX, yPos, buttonSize)
        AddNumberButton("2", startX + buttonSize + buttonSpacing, yPos, buttonSize)
        AddNumberButton("3", startX + (buttonSize + buttonSpacing) * 2, yPos, buttonSize)
        AddSpecialButton("C", startX + (buttonSize + buttonSpacing) * 3, yPos, buttonSize, AddressOf ClearClick)
        yPos += buttonSize + buttonSpacing

        ' Row 4: 0, ., (empty), (empty)
        AddNumberButton("0", startX, yPos, buttonSize)
        AddSpecialButton(".", startX + buttonSize + buttonSpacing, yPos, buttonSize, AddressOf DecimalClick)
        yPos += buttonSize + buttonSpacing + 8

        ' Separator
        Dim separator4 As New Panel With {
            .Location = New Point(10, yPos),
            .Size = New Size(320, 1),
            .BackColor = Color.LightGray
        }
        Me.Controls.Add(separator4)
        yPos += 8

        ' OK and Cancel buttons
        btnOK = New Button With {
            .Text = "OK",
            .Font = New Font("Segoe UI", 9, FontStyle.Bold),
            .Location = New Point(55, yPos),
            .Size = New Size(95, 35),
            .BackColor = Color.FromArgb(46, 204, 113),
            .ForeColor = Color.White,
            .FlatStyle = FlatStyle.Flat,
            .Cursor = Cursors.Hand,
            .DialogResult = DialogResult.OK
        }
        btnOK.FlatAppearance.BorderSize = 0
        AddHandler btnOK.Click, AddressOf OKClick
        Me.Controls.Add(btnOK)

        btnCancel = New Button With {
            .Text = "Cancel",
            .Font = New Font("Segoe UI", 9, FontStyle.Bold),
            .Location = New Point(165, yPos),
            .Size = New Size(95, 35),
            .BackColor = Color.FromArgb(231, 76, 60),
            .ForeColor = Color.White,
            .FlatStyle = FlatStyle.Flat,
            .Cursor = Cursors.Hand,
            .DialogResult = DialogResult.Cancel
        }
        btnCancel.FlatAppearance.BorderSize = 0
        Me.Controls.Add(btnCancel)
    End Sub

    Private Sub AddNumberButton(text As String, x As Integer, y As Integer, width As Integer)
        Dim btn As New Button With {
            .Text = text,
            .Font = New Font("Segoe UI", 12, FontStyle.Bold),
            .Location = New Point(x, y),
            .Size = New Size(width, 52),
            .BackColor = Color.FromArgb(248, 249, 250),
            .ForeColor = Color.Black,
            .FlatStyle = FlatStyle.Flat,
            .Cursor = Cursors.Hand,
            .Tag = text
        }
        btn.FlatAppearance.BorderColor = Color.LightGray
        btn.FlatAppearance.BorderSize = 1
        AddHandler btn.Click, AddressOf NumberClick
        Me.Controls.Add(btn)
    End Sub

    Private Sub AddSpecialButton(text As String, x As Integer, y As Integer, width As Integer, handler As EventHandler)
        Dim btn As New Button With {
            .Text = text,
            .Font = New Font("Segoe UI", 11, FontStyle.Bold),
            .Location = New Point(x, y),
            .Size = New Size(width, 52),
            .BackColor = Color.FromArgb(108, 117, 125),
            .ForeColor = Color.White,
            .FlatStyle = FlatStyle.Flat,
            .Cursor = Cursors.Hand
        }
        btn.FlatAppearance.BorderSize = 0
        AddHandler btn.Click, handler
        Me.Controls.Add(btn)
    End Sub

    Private Sub NumberClick(sender As Object, e As EventArgs)
        Dim btn = DirectCast(sender, Button)
        Dim digit = btn.Tag.ToString()
        
        If txtPrice.Text = "0.00" Or txtPrice.Text = "0" Then
            txtPrice.Text = digit
        Else
            ' Check if we already have 4 decimal places
            If txtPrice.Text.Contains(".") Then
                Dim parts = txtPrice.Text.Split("."c)
                If parts.Length > 1 AndAlso parts(1).Length >= 4 Then
                    Return ' Don't add more than 4 decimal places
                End If
            End If
            txtPrice.Text &= digit
        End If
    End Sub

    Private Sub DecimalClick(sender As Object, e As EventArgs)
        If Not txtPrice.Text.Contains(".") Then
            If String.IsNullOrEmpty(txtPrice.Text) Or txtPrice.Text = "0" Then
                txtPrice.Text = "0."
            Else
                txtPrice.Text &= "."
            End If
        End If
    End Sub

    Private Sub BackspaceClick(sender As Object, e As EventArgs)
        If txtPrice.Text.Length > 0 Then
            txtPrice.Text = txtPrice.Text.Substring(0, txtPrice.Text.Length - 1)
            If String.IsNullOrEmpty(txtPrice.Text) Then
                txtPrice.Text = "0"
            End If
        End If
    End Sub

    Private Sub ClearClick(sender As Object, e As EventArgs)
        txtPrice.Text = "0"
    End Sub

    Private Sub OKClick(sender As Object, e As EventArgs)
        If Decimal.TryParse(txtPrice.Text, _newPrice) Then
            If _newPrice <= 0 Then
                MessageBox.Show("Price must be greater than zero.", "Invalid Price", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                Return
            End If
            
            If _newPrice > _originalPrice * 2 Then
                Dim result = MessageBox.Show($"New price (R {_newPrice:N4}) is more than double the original price (R {_originalPrice:N2}).{vbCrLf}{vbCrLf}Are you sure this is correct?", 
                                           "Confirm High Price", MessageBoxButtons.YesNo, MessageBoxIcon.Warning)
                If result = DialogResult.No Then
                    Return
                End If
            End If
            
            Me.DialogResult = DialogResult.OK
            Me.Close()
        Else
            MessageBox.Show("Please enter a valid price.", "Invalid Input", MessageBoxButtons.OK, MessageBoxIcon.Warning)
        End If
    End Sub

    Private Sub CancelClick(sender As Object, e As EventArgs)
        Me.DialogResult = DialogResult.Cancel
        Me.Close()
    End Sub
End Class
