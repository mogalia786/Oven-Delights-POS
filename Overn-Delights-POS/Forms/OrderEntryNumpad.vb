Imports System.Windows.Forms
Imports System.Drawing

Public Class OrderEntryNumpad
    Inherits Panel
    
    Private _targetTextBox As TextBox
    Private _darkBlue As Color = ColorTranslator.FromHtml("#2C3E50")
    Private _lightBlue As Color = ColorTranslator.FromHtml("#3498DB")
    Private _orange As Color = ColorTranslator.FromHtml("#E67E22")
    Private _red As Color = ColorTranslator.FromHtml("#E74C3C")
    
    Public Sub New(targetTextBox As TextBox)
        _targetTextBox = targetTextBox
        InitializeNumpad()
    End Sub
    
    Private Sub InitializeNumpad()
        Me.Size = New Size(400, 450)
        Me.BackColor = _darkBlue
        Me.Visible = False
        Me.BorderStyle = BorderStyle.FixedSingle
        
        ' Header
        Dim pnlHeader As New Panel With {
            .Dock = DockStyle.Top,
            .Height = 40,
            .BackColor = _orange
        }
        
        Dim lblTitle As New Label With {
            .Text = "🔢 NUMPAD",
            .Font = New Font("Segoe UI", 14, FontStyle.Bold),
            .ForeColor = Color.White,
            .Location = New Point(10, 8),
            .AutoSize = True
        }
        
        Dim btnClose As New Button With {
            .Text = "✖",
            .Size = New Size(35, 35),
            .Location = New Point(360, 2),
            .BackColor = _red,
            .ForeColor = Color.White,
            .Font = New Font("Segoe UI", 14, FontStyle.Bold),
            .FlatStyle = FlatStyle.Flat,
            .Cursor = Cursors.Hand
        }
        btnClose.FlatAppearance.BorderSize = 0
        AddHandler btnClose.Click, Sub() Me.Visible = False
        
        pnlHeader.Controls.AddRange({lblTitle, btnClose})
        
        ' Numpad layout with - character
        Dim keys(,) As String = {
            {"7", "8", "9"},
            {"4", "5", "6"},
            {"1", "2", "3"},
            {"-", "0", "⌫"}
        }
        
        Dim pnlKeys As New Panel With {
            .Location = New Point(50, 60),
            .Size = New Size(300, 360),
            .BackColor = _darkBlue
        }
        
        For row = 0 To 3
            For col = 0 To 2
                Dim key = keys(row, col)
                Dim btn As New Button With {
                    .Text = key,
                    .Size = New Size(90, 80),
                    .Location = New Point(col * 100, row * 90),
                    .BackColor = If(key = "⌫", _red, Color.White),
                    .ForeColor = If(key = "⌫", Color.White, _darkBlue),
                    .Font = New Font("Segoe UI", 24, FontStyle.Bold),
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
                    Else
                        _targetTextBox.Text &= clickedBtn.Text
                    End If
                    _targetTextBox.Focus()
                End Sub
                
                pnlKeys.Controls.Add(btn)
            Next
        Next
        
        Me.Controls.AddRange({pnlHeader, pnlKeys})
    End Sub
    
    Public Sub ShowNumpad()
        Me.Visible = True
        Me.BringToFront()
    End Sub
    
    Public Sub HideNumpad()
        Me.Visible = False
    End Sub
End Class
