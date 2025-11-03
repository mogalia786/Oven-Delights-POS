Imports System.Drawing
Imports System.Windows.Forms

Public Class OnScreenKeyboard
    Inherits Panel
    
    Private _textBox As TextBox
    Private _isVisible As Boolean = False
    
    Private _btnHide As Button
    Private _keys As New List(Of Button)
    Private _scaleFactor As Single = 1.0F
    
    ' Modern color palette
    Private _keyColor As Color = Color.White
    Private _keyPressedColor As Color = ColorTranslator.FromHtml("#3498DB")
    Private _specialKeyColor As Color = ColorTranslator.FromHtml("#ECF0F1")
    Private _deleteKeyColor As Color = ColorTranslator.FromHtml("#E74C3C")
    Private _spaceKeyColor As Color = ColorTranslator.FromHtml("#95A5A6")
    Private _panelColor As Color = ColorTranslator.FromHtml("#2C3E50")
    
    Public Shadows Event TextChanged(sender As Object, text As String)
    
    Public Sub New(linkedTextBox As TextBox)
        MyBase.New()
        _textBox = linkedTextBox
        InitializeKeyboard()
    End Sub
    
    Private Sub InitializeKeyboard()
        ' Calculate initial scale factor based on screen width
        Dim screenWidth = Screen.PrimaryScreen.Bounds.Width
        _scaleFactor = CSng(screenWidth) / 1920.0F
        If _scaleFactor < 0.6F Then _scaleFactor = 0.6F
        If _scaleFactor > 1.5F Then _scaleFactor = 1.5F
        
        Me.Height = CInt(300 * _scaleFactor)
        Me.Dock = DockStyle.Bottom
        Me.BackColor = _panelColor
        Me.Visible = False
        
        ' Hide/Show button (down arrow) - scaled
        _btnHide = New Button With {
            .Text = "▼ HIDE KEYBOARD",
            .Size = New Size(CInt(200 * _scaleFactor), CInt(35 * _scaleFactor)),
            .Location = New Point(CInt(10 * _scaleFactor), CInt(10 * _scaleFactor)),
            .BackColor = ColorTranslator.FromHtml("#34495E"),
            .ForeColor = Color.White,
            .Font = New Font("Segoe UI", CSng(10 * _scaleFactor), FontStyle.Bold),
            .FlatStyle = FlatStyle.Flat,
            .Cursor = Cursors.Hand
        }
        _btnHide.FlatAppearance.BorderSize = 0
        AddHandler _btnHide.Click, Sub() HideKeyboard()
        Me.Controls.Add(_btnHide)
        
        ' QWERTY Layout
        Dim row1 = {"Q", "W", "E", "R", "T", "Y", "U", "I", "O", "P"}
        Dim row2 = {"A", "S", "D", "F", "G", "H", "J", "K", "L"}
        Dim row3 = {"Z", "X", "C", "V", "B", "N", "M"}
        
        ' Scale key dimensions based on screen size
        Dim keyWidth = CInt(80 * _scaleFactor)
        Dim keyHeight = CInt(60 * _scaleFactor)
        Dim spacing = CInt(5 * _scaleFactor)
        Dim startY = CInt(55 * _scaleFactor)
        
        ' Row 1
        Dim x = CInt(10 * _scaleFactor)
        For Each key In row1
            CreateKey(key, x, startY, keyWidth, keyHeight)
            x += keyWidth + spacing
        Next
        
        ' Row 2 (slightly offset)
        x = CInt(40 * _scaleFactor)
        For Each key In row2
            CreateKey(key, x, startY + keyHeight + spacing, keyWidth, keyHeight)
            x += keyWidth + spacing
        Next
        
        ' Row 3 (more offset)
        x = CInt(70 * _scaleFactor)
        For Each key In row3
            CreateKey(key, x, startY + (keyHeight + spacing) * 2, keyWidth, keyHeight)
            x += keyWidth + spacing
        Next
        
        ' Special keys on row 3
        ' Delete button
        Dim btnDelete As New Button With {
            .Text = "⌫ DELETE",
            .Size = New Size(CInt(160 * _scaleFactor), keyHeight),
            .Location = New Point(x + CInt(10 * _scaleFactor), startY + (keyHeight + spacing) * 2),
            .BackColor = _deleteKeyColor,
            .ForeColor = Color.White,
            .Font = New Font("Segoe UI", CSng(11 * _scaleFactor), FontStyle.Bold),
            .FlatStyle = FlatStyle.Flat,
            .Cursor = Cursors.Hand
        }
        btnDelete.FlatAppearance.BorderSize = 0
        AddHandler btnDelete.Click, Sub() DeleteChar()
        AddHandler btnDelete.MouseDown, Sub() btnDelete.BackColor = Color.FromArgb(200, 50, 50)
        AddHandler btnDelete.MouseUp, Sub() btnDelete.BackColor = _deleteKeyColor
        Me.Controls.Add(btnDelete)
        
        ' Space bar (bottom row)
        Dim btnSpace As New Button With {
            .Text = "SPACE",
            .Size = New Size(CInt(400 * _scaleFactor), keyHeight),
            .Location = New Point(CInt(150 * _scaleFactor), startY + (keyHeight + spacing) * 3),
            .BackColor = _spaceKeyColor,
            .ForeColor = Color.White,
            .Font = New Font("Segoe UI", CSng(11 * _scaleFactor), FontStyle.Bold),
            .FlatStyle = FlatStyle.Flat,
            .Cursor = Cursors.Hand
        }
        btnSpace.FlatAppearance.BorderSize = 0
        AddHandler btnSpace.Click, Sub() AddChar(" ")
        AddHandler btnSpace.MouseDown, Sub() btnSpace.BackColor = Color.FromArgb(120, 130, 140)
        AddHandler btnSpace.MouseUp, Sub() btnSpace.BackColor = _spaceKeyColor
        Me.Controls.Add(btnSpace)
        
        ' Clear button
        Dim btnClear As New Button With {
            .Text = "✖ CLEAR",
            .Size = New Size(CInt(120 * _scaleFactor), keyHeight),
            .Location = New Point(CInt(560 * _scaleFactor), startY + (keyHeight + spacing) * 3),
            .BackColor = ColorTranslator.FromHtml("#E67E22"),
            .ForeColor = Color.White,
            .Font = New Font("Segoe UI", CSng(11 * _scaleFactor), FontStyle.Bold),
            .FlatStyle = FlatStyle.Flat,
            .Cursor = Cursors.Hand
        }
        btnClear.FlatAppearance.BorderSize = 0
        AddHandler btnClear.Click, Sub() ClearText()
        AddHandler btnClear.MouseDown, Sub() btnClear.BackColor = Color.FromArgb(200, 100, 30)
        AddHandler btnClear.MouseUp, Sub() btnClear.BackColor = ColorTranslator.FromHtml("#E67E22")
        Me.Controls.Add(btnClear)
    End Sub
    
    Private Sub CreateKey(letter As String, x As Integer, y As Integer, width As Integer, height As Integer)
        Dim btn As New Button With {
            .Text = letter,
            .Size = New Size(width, height),
            .Location = New Point(x, y),
            .BackColor = _keyColor,
            .ForeColor = Color.Black,
            .Font = New Font("Segoe UI", CSng(14 * _scaleFactor), FontStyle.Bold),
            .FlatStyle = FlatStyle.Flat,
            .Cursor = Cursors.Hand,
            .Tag = letter
        }
        btn.FlatAppearance.BorderSize = 1
        btn.FlatAppearance.BorderColor = Color.LightGray
        
        AddHandler btn.Click, Sub() AddChar(letter)
        AddHandler btn.MouseDown, Sub() 
            btn.BackColor = _keyPressedColor
            btn.ForeColor = Color.White
        End Sub
        AddHandler btn.MouseUp, Sub() 
            btn.BackColor = _keyColor
            btn.ForeColor = Color.Black
        End Sub
        
        _keys.Add(btn)
        Me.Controls.Add(btn)
    End Sub
    
    Private Sub AddChar(character As String)
        If _textBox IsNot Nothing Then
            _textBox.Text &= character
            _textBox.SelectionStart = _textBox.Text.Length
            ' Raise custom event immediately for instant filtering
            RaiseEvent TextChanged(Me, _textBox.Text)
        End If
    End Sub
    
    Private Sub DeleteChar()
        If _textBox IsNot Nothing AndAlso _textBox.Text.Length > 0 Then
            _textBox.Text = _textBox.Text.Substring(0, _textBox.Text.Length - 1)
            _textBox.SelectionStart = _textBox.Text.Length
            ' Raise custom event immediately for instant filtering
            RaiseEvent TextChanged(Me, _textBox.Text)
        End If
    End Sub
    
    Private Sub ClearText()
        If _textBox IsNot Nothing Then
            _textBox.Clear()
            ' Raise custom event immediately for instant filtering
            RaiseEvent TextChanged(Me, "")
        End If
    End Sub
    
    Public Sub ShowKeyboard()
        If _isVisible Then Return
        
        ' Textbox is already editable for physical keyboard - don't change it
        
        Me.Visible = True
        Me.BringToFront()
        _isVisible = True
        
        ' Focus the textbox so user can also type with physical keyboard
        If _textBox IsNot Nothing Then
            _textBox.Focus()
            
            ' Add keyboard event handler for visual feedback
            AddHandler _textBox.KeyDown, AddressOf OnPhysicalKeyPress
        End If
    End Sub
    
    Public Sub HideKeyboard()
        If Not _isVisible Then Return
        
        ' Keep textbox editable for physical keyboard - don't make it readonly
        
        ' Remove keyboard event handler
        If _textBox IsNot Nothing Then
            RemoveHandler _textBox.KeyDown, AddressOf OnPhysicalKeyPress
        End If
        
        Me.Visible = False
        Me.SendToBack()
        _isVisible = False
    End Sub
    
    Private Sub OnPhysicalKeyPress(sender As Object, e As KeyEventArgs)
        ' Show visual feedback on virtual keyboard when physical key is pressed
        Dim keyChar = ChrW(e.KeyCode).ToString().ToUpper()
        
        ' Find the matching button
        For Each btn As Button In _keys
            If btn.Tag IsNot Nothing AndAlso btn.Tag.ToString().ToUpper() = keyChar Then
                ' Flash the button
                FlashButton(btn)
                Exit For
            End If
        Next
    End Sub
    
    Private Async Sub FlashButton(btn As Button)
        ' Highlight the button briefly
        Dim originalColor = btn.BackColor
        Dim originalForeColor = btn.ForeColor
        
        btn.BackColor = _keyPressedColor
        btn.ForeColor = Color.White
        
        ' Wait 150ms
        Await Task.Delay(150)
        
        ' Restore original colors
        btn.BackColor = originalColor
        btn.ForeColor = originalForeColor
    End Sub
    
    Public ReadOnly Property IsKeyboardVisible As Boolean
        Get
            Return _isVisible
        End Get
    End Property
    
    Public Sub UpdateKeyboardSize(scaleFactor As Single)
        ' Update scale factor and rebuild keyboard
        _scaleFactor = scaleFactor
        If _scaleFactor < 0.6F Then _scaleFactor = 0.6F
        If _scaleFactor > 1.5F Then _scaleFactor = 1.5F
        
        ' Update keyboard height
        Me.Height = CInt(300 * _scaleFactor)
        
        ' Rebuild keyboard with new scale
        Me.Controls.Clear()
        _keys.Clear()
        InitializeKeyboard()
    End Sub
End Class
