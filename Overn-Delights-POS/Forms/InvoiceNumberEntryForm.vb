Imports System.Windows.Forms
Imports System.Drawing

Public Class InvoiceNumberEntryForm
    Inherits Form

    Private txtInvoiceNumber As TextBox
    Private lblDisplay As Label
    Private pnlKeypad As Panel
    Private btnOK As Button
    Private btnCancel As Button

    Public Property InvoiceDigits As String = ""

    Public Sub New()
        InitializeComponent()
    End Sub

    Private Sub InitializeComponent()
        Me.Text = "Enter Invoice Number"
        Me.Size = New Size(400, 600)
        Me.StartPosition = FormStartPosition.CenterScreen
        Me.FormBorderStyle = FormBorderStyle.FixedDialog
        Me.MaximizeBox = False
        Me.MinimizeBox = False
        Me.BackColor = Color.White

        ' Header
        Dim pnlHeader As New Panel With {
            .Dock = DockStyle.Top,
            .Height = 80,
            .BackColor = ColorTranslator.FromHtml("#2C3E50")
        }

        Dim lblHeader As New Label With {
            .Text = "📄 INVOICE NUMBER",
            .Font = New Font("Segoe UI", 20, FontStyle.Bold),
            .ForeColor = Color.White,
            .TextAlign = ContentAlignment.MiddleCenter,
            .Dock = DockStyle.Fill
        }
        pnlHeader.Controls.Add(lblHeader)

        ' Display panel
        Dim pnlDisplay As New Panel With {
            .Dock = DockStyle.Top,
            .Height = 100,
            .BackColor = Color.White,
            .Padding = New Padding(20)
        }

        lblDisplay = New Label With {
            .Text = "Enter digits only",
            .Font = New Font("Segoe UI", 24, FontStyle.Bold),
            .ForeColor = ColorTranslator.FromHtml("#2C3E50"),
            .TextAlign = ContentAlignment.MiddleCenter,
            .Dock = DockStyle.Fill,
            .BorderStyle = BorderStyle.FixedSingle
        }
        pnlDisplay.Controls.Add(lblDisplay)

        ' Keypad
        pnlKeypad = New Panel With {
            .Dock = DockStyle.Fill,
            .BackColor = Color.White,
            .Padding = New Padding(20)
        }

        CreateKeypad()

        ' Button panel
        Dim pnlButtons As New Panel With {
            .Dock = DockStyle.Bottom,
            .Height = 80,
            .BackColor = Color.White,
            .Padding = New Padding(20, 10, 20, 10)
        }

        btnCancel = New Button With {
            .Text = "CANCEL",
            .Font = New Font("Segoe UI", 14, FontStyle.Bold),
            .Size = New Size(150, 60),
            .BackColor = ColorTranslator.FromHtml("#E74C3C"),
            .ForeColor = Color.White,
            .FlatStyle = FlatStyle.Flat,
            .Cursor = Cursors.Hand,
            .Location = New Point(20, 10)
        }
        btnCancel.FlatAppearance.BorderSize = 0
        AddHandler btnCancel.Click, Sub()
                                        Me.DialogResult = DialogResult.Cancel
                                        Me.Close()
                                    End Sub

        btnOK = New Button With {
            .Text = "OK",
            .Font = New Font("Segoe UI", 14, FontStyle.Bold),
            .Size = New Size(150, 60),
            .BackColor = ColorTranslator.FromHtml("#27AE60"),
            .ForeColor = Color.White,
            .FlatStyle = FlatStyle.Flat,
            .Cursor = Cursors.Hand,
            .Location = New Point(210, 10),
            .Enabled = False
        }
        btnOK.FlatAppearance.BorderSize = 0
        AddHandler btnOK.Click, Sub()
                                    Me.DialogResult = DialogResult.OK
                                    Me.Close()
                                End Sub

        pnlButtons.Controls.AddRange({btnCancel, btnOK})

        Me.Controls.AddRange({pnlKeypad, pnlDisplay, pnlButtons, pnlHeader})
    End Sub

    Private Sub CreateKeypad()
        Dim buttonSize As New Size(100, 70)
        Dim spacing As Integer = 10
        Dim startX As Integer = 30
        Dim startY As Integer = 20

        ' Numbers 1-9
        For i As Integer = 1 To 9
            Dim row As Integer = (i - 1) \ 3
            Dim col As Integer = (i - 1) Mod 3
            Dim number As Integer = i

            Dim btn As New Button With {
                .Text = number.ToString(),
                .Font = New Font("Segoe UI", 24, FontStyle.Bold),
                .Size = buttonSize,
                .Location = New Point(startX + col * (buttonSize.Width + spacing), startY + row * (buttonSize.Height + spacing)),
                .BackColor = ColorTranslator.FromHtml("#3498DB"),
                .ForeColor = Color.White,
                .FlatStyle = FlatStyle.Flat,
                .Cursor = Cursors.Hand
            }
            btn.FlatAppearance.BorderSize = 0
            AddHandler btn.Click, Sub() AddDigit(number.ToString())
            pnlKeypad.Controls.Add(btn)
        Next

        ' Zero button (bottom center)
        Dim btnZero As New Button With {
            .Text = "0",
            .Font = New Font("Segoe UI", 24, FontStyle.Bold),
            .Size = buttonSize,
            .Location = New Point(startX + (buttonSize.Width + spacing), startY + 3 * (buttonSize.Height + spacing)),
            .BackColor = ColorTranslator.FromHtml("#3498DB"),
            .ForeColor = Color.White,
            .FlatStyle = FlatStyle.Flat,
            .Cursor = Cursors.Hand
        }
        btnZero.FlatAppearance.BorderSize = 0
        AddHandler btnZero.Click, Sub() AddDigit("0")
        pnlKeypad.Controls.Add(btnZero)

        ' Clear button (bottom left)
        Dim btnClear As New Button With {
            .Text = "CLEAR",
            .Font = New Font("Segoe UI", 14, FontStyle.Bold),
            .Size = buttonSize,
            .Location = New Point(startX, startY + 3 * (buttonSize.Height + spacing)),
            .BackColor = ColorTranslator.FromHtml("#E67E22"),
            .ForeColor = Color.White,
            .FlatStyle = FlatStyle.Flat,
            .Cursor = Cursors.Hand
        }
        btnClear.FlatAppearance.BorderSize = 0
        AddHandler btnClear.Click, Sub()
                                       InvoiceDigits = ""
                                       lblDisplay.Text = "Enter digits only"
                                       btnOK.Enabled = False
                                   End Sub
        pnlKeypad.Controls.Add(btnClear)

        ' Backspace button (bottom right)
        Dim btnBack As New Button With {
            .Text = "⌫",
            .Font = New Font("Segoe UI", 24, FontStyle.Bold),
            .Size = buttonSize,
            .Location = New Point(startX + 2 * (buttonSize.Width + spacing), startY + 3 * (buttonSize.Height + spacing)),
            .BackColor = ColorTranslator.FromHtml("#E74C3C"),
            .ForeColor = Color.White,
            .FlatStyle = FlatStyle.Flat,
            .Cursor = Cursors.Hand
        }
        btnBack.FlatAppearance.BorderSize = 0
        AddHandler btnBack.Click, Sub()
                                      If InvoiceDigits.Length > 0 Then
                                          InvoiceDigits = InvoiceDigits.Substring(0, InvoiceDigits.Length - 1)
                                          If InvoiceDigits.Length > 0 Then
                                              lblDisplay.Text = InvoiceDigits
                                          Else
                                              lblDisplay.Text = "Enter digits only"
                                              btnOK.Enabled = False
                                          End If
                                      End If
                                  End Sub
        pnlKeypad.Controls.Add(btnBack)
    End Sub

    Private Sub AddDigit(digit As String)
        InvoiceDigits &= digit
        lblDisplay.Text = InvoiceDigits
        btnOK.Enabled = True
    End Sub
End Class
