Imports System.Windows.Forms
Imports System.Drawing

Public Class OrderEntryKeyboard
    Inherits Panel
    
    Private _targetTextBox As TextBox
    Private _darkBlue As Color = ColorTranslator.FromHtml("#2C3E50")
    Private _lightBlue As Color = ColorTranslator.FromHtml("#3498DB")
    Private _green As Color = ColorTranslator.FromHtml("#27AE60")
    Private _red As Color = ColorTranslator.FromHtml("#E74C3C")
    
    Public Sub New(targetTextBox As TextBox)
        _targetTextBox = targetTextBox
        InitializeKeyboard()
    End Sub
    
    Private Sub InitializeKeyboard()
        Me.Size = New Size(800, 350)
        Me.BackColor = _darkBlue
        Me.Visible = False
        Me.BorderStyle = BorderStyle.FixedSingle
        
        ' Header
        Dim pnlHeader As New Panel With {
            .Dock = DockStyle.Top,
            .Height = 40,
            .BackColor = _lightBlue
        }
        
        Dim lblTitle As New Label With {
            .Text = "⌨️ ON-SCREEN KEYBOARD",
            .Font = New Font("Segoe UI", 14, FontStyle.Bold),
            .ForeColor = Color.White,
            .Location = New Point(10, 8),
            .AutoSize = True
        }
        
        Dim btnClose As New Button With {
            .Text = "✖",
            .Size = New Size(35, 35),
            .Location = New Point(760, 2),
            .BackColor = _red,
            .ForeColor = Color.White,
            .Font = New Font("Segoe UI", 14, FontStyle.Bold),
            .FlatStyle = FlatStyle.Flat,
            .Cursor = Cursors.Hand
        }
        btnClose.FlatAppearance.BorderSize = 0
        AddHandler btnClose.Click, Sub() Me.Visible = False
        
        pnlHeader.Controls.AddRange({lblTitle, btnClose})
        
        ' Keyboard layout with - character, TAB and RETURN
        Dim keys As String()() = {
            New String() {"1", "2", "3", "4", "5", "6", "7", "8", "9", "0", "-"},
            New String() {"Q", "W", "E", "R", "T", "Y", "U", "I", "O", "P"},
            New String() {"A", "S", "D", "F", "G", "H", "J", "K", "L"},
            New String() {"Z", "X", "C", "V", "B", "N", "M", "⌫"},
            New String() {"TAB", "CLEAR", "RETURN"}
        }
        
        Dim pnlKeys As New Panel With {
            .Location = New Point(10, 50),
            .Size = New Size(780, 290),
            .BackColor = _darkBlue
        }
        
        Dim yPos = 0
        For Each row In keys
            Dim xPos = 0
            For Each key In row
                Dim btnWidth As Integer
                If key = "TAB" OrElse key = "RETURN" Then
                    btnWidth = 240
                ElseIf key = "⌫" OrElse key = "CLEAR" Then
                    btnWidth = 100
                Else
                    btnWidth = 65
                End If
                
                Dim isSpecial = (key = "⌫" OrElse key = "CLEAR" OrElse key = "TAB" OrElse key = "RETURN")
                Dim btn As New Button With {
                    .Text = key,
                    .Size = New Size(btnWidth, 50),
                    .Location = New Point(xPos, yPos),
                    .BackColor = If(key = "⌫" OrElse key = "CLEAR", _red, If(key = "TAB" OrElse key = "RETURN", _green, Color.White)),
                    .ForeColor = If(isSpecial, Color.White, _darkBlue),
                    .Font = New Font("Segoe UI", If(key = "TAB" OrElse key = "RETURN", 14, 16), FontStyle.Bold),
                    .FlatStyle = FlatStyle.Flat,
                    .Cursor = Cursors.Hand
                }
                btn.FlatAppearance.BorderSize = 1
                btn.FlatAppearance.BorderColor = _darkBlue
                
                AddHandler btn.Click, Sub(s, e)
                    Dim clickedBtn = CType(s, Button)
                    If clickedBtn.Text = "⌫" Then
                        If _targetTextBox.Text.Length > 0 Then
                            _targetTextBox.Text = _targetTextBox.Text.Substring(0, _targetTextBox.Text.Length - 1)
                        End If
                    ElseIf clickedBtn.Text = "CLEAR" Then
                        _targetTextBox.Clear()
                    ElseIf clickedBtn.Text = "TAB" Then
                        ' Send TAB key to move to next control
                        SendKeys.Send("{TAB}")
                    ElseIf clickedBtn.Text = "RETURN" Then
                        ' Send ENTER key
                        SendKeys.Send("{ENTER}")
                    Else
                        _targetTextBox.Text &= clickedBtn.Text
                    End If
                    _targetTextBox.Focus()
                End Sub
                
                pnlKeys.Controls.Add(btn)
                xPos += btnWidth + 5
            Next
            yPos += 55
        Next
        
        Me.Controls.AddRange({pnlHeader, pnlKeys})
    End Sub
    
    Public Sub ShowKeyboard()
        Me.Visible = True
        Me.BringToFront()
    End Sub
    
    Public Sub HideKeyboard()
        Me.Visible = False
    End Sub
End Class
