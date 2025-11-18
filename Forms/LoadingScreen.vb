Imports System.Windows.Forms
Imports System.Drawing

Public Class LoadingScreen
    Inherits Form
    
    Private WithEvents progressBar As ProgressBar
    Private lblStatus As Label
    Private lblTitle As Label
    
    Public Sub New()
        InitializeComponent()
    End Sub
    
    Private Sub InitializeComponent()
        Me.FormBorderStyle = FormBorderStyle.None
        Me.StartPosition = FormStartPosition.CenterScreen
        Me.Size = New Size(600, 300)
        Me.BackColor = ColorTranslator.FromHtml("#2C3E50")
        
        ' Title
        lblTitle = New Label With {
            .Text = "OVEN DELIGHTS POS",
            .Font = New Font("Segoe UI", 32, FontStyle.Bold),
            .ForeColor = Color.White,
            .AutoSize = True,
            .Location = New Point(100, 50)
        }
        
        ' Status label
        lblStatus = New Label With {
            .Text = "Initializing...",
            .Font = New Font("Segoe UI", 14),
            .ForeColor = ColorTranslator.FromHtml("#ECF0F1"),
            .AutoSize = True,
            .Location = New Point(220, 130)
        }
        
        ' Progress bar
        progressBar = New ProgressBar With {
            .Location = New Point(100, 170),
            .Size = New Size(400, 30),
            .Style = ProgressBarStyle.Marquee,
            .MarqueeAnimationSpeed = 30
        }
        
        ' Version label
        Dim lblVersion As New Label With {
            .Text = "Version 1.0",
            .Font = New Font("Segoe UI", 9),
            .ForeColor = ColorTranslator.FromHtml("#95A5A6"),
            .AutoSize = True,
            .Location = New Point(260, 240)
        }
        
        Me.Controls.AddRange({lblTitle, lblStatus, progressBar, lblVersion})
    End Sub
    
    Public Sub UpdateStatus(status As String)
        If Me.InvokeRequired Then
            Me.Invoke(Sub() lblStatus.Text = status)
        Else
            lblStatus.Text = status
        End If
        Application.DoEvents()
    End Sub
    
    Public Sub SetProgress(value As Integer)
        If Me.InvokeRequired Then
            Me.Invoke(Sub()
                progressBar.Style = ProgressBarStyle.Continuous
                progressBar.Value = Math.Min(value, 100)
            End Sub)
        Else
            progressBar.Style = ProgressBarStyle.Continuous
            progressBar.Value = Math.Min(value, 100)
        End If
        Application.DoEvents()
    End Sub
End Class
