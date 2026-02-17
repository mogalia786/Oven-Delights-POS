Imports System.Configuration
Imports System.Drawing
Imports System.Windows.Forms

Public Class IdleScreen
    Inherits UserControl

    Private _primaryColor As Color
    Private _accentColor As Color
    Private _timer As Timer
    Private _currentSlide As Integer = 0
    Private _slides As New List(Of String)

    Public Sub New()
        InitializeComponent()
        LoadColors()
        SetupUI()
        StartSlideshow()
    End Sub

    Private Sub LoadColors()
        Dim primaryHex = If(ConfigurationManager.AppSettings("PrimaryColor"), "#D2691E")
        Dim accentHex = If(ConfigurationManager.AppSettings("AccentColor"), "#FFD700")
        _primaryColor = ColorTranslator.FromHtml(primaryHex)
        _accentColor = ColorTranslator.FromHtml(accentHex)
    End Sub

    Private Sub SetupUI()
        Me.Dock = DockStyle.Fill
        Me.BackColor = _primaryColor

        ' Welcome message
        Dim lblWelcome As New Label With {
            .Text = "Welcome to",
            .Font = New Font("Segoe UI", 36, FontStyle.Regular),
            .ForeColor = Color.White,
            .TextAlign = ContentAlignment.MiddleCenter,
            .Dock = DockStyle.Top,
            .Height = 100,
            .Padding = New Padding(0, 30, 0, 0)
        }

        ' Company name
        Dim lblCompany As New Label With {
            .Text = If(ConfigurationManager.AppSettings("CompanyName"), "Oven Delights"),
            .Font = New Font("Segoe UI", 72, FontStyle.Bold),
            .ForeColor = _accentColor,
            .TextAlign = ContentAlignment.MiddleCenter,
            .Dock = DockStyle.Top,
            .Height = 150
        }

        ' Tagline
        Dim lblTagline As New Label With {
            .Text = "Freshly Baked Daily",
            .Font = New Font("Segoe UI", 28, FontStyle.Italic),
            .ForeColor = Color.FromArgb(240, 240, 240),
            .TextAlign = ContentAlignment.MiddleCenter,
            .Dock = DockStyle.Top,
            .Height = 80
        }

        ' Promotional message panel
        Dim promoPanel As New Panel With {
            .Size = New Size(800, 300),
            .BackColor = Color.FromArgb(30, Color.White),
            .Location = New Point((Me.Width - 800) \ 2, 400)
        }
        promoPanel.Anchor = AnchorStyles.None

        Dim lblPromo As New Label With {
            .Name = "lblPromo",
            .Font = New Font("Segoe UI", 32, FontStyle.Bold),
            .ForeColor = Color.White,
            .TextAlign = ContentAlignment.MiddleCenter,
            .Dock = DockStyle.Fill,
            .Padding = New Padding(20)
        }
        promoPanel.Controls.Add(lblPromo)

        ' Footer message
        Dim lblFooter As New Label With {
            .Text = "Please wait, a cashier will assist you shortly",
            .Font = New Font("Segoe UI", 18),
            .ForeColor = Color.FromArgb(220, 220, 220),
            .TextAlign = ContentAlignment.MiddleCenter,
            .Dock = DockStyle.Bottom,
            .Height = 60,
            .Padding = New Padding(0, 0, 0, 20)
        }

        ' Add promotional messages
        _slides.AddRange({
            "🍞 Fresh Bread Daily!",
            "🥐 Try Our New Croissants",
            "🎂 Custom Cakes Available",
            "☕ Coffee & Pastries",
            "🍪 Fresh Cookies Daily"
        })

        Me.Controls.AddRange({lblWelcome, lblCompany, lblTagline, promoPanel, lblFooter})
    End Sub

    Private Sub StartSlideshow()
        _timer = New Timer With {.Interval = 3000}
        AddHandler _timer.Tick, AddressOf Timer_Tick
        _timer.Start()
        UpdateSlide()
    End Sub

    Private Sub Timer_Tick(sender As Object, e As EventArgs)
        _currentSlide = (_currentSlide + 1) Mod _slides.Count
        UpdateSlide()
    End Sub

    Private Sub UpdateSlide()
        Dim lblPromo = TryCast(Me.Controls.Find("lblPromo", True).FirstOrDefault(), Label)
        If lblPromo IsNot Nothing Then
            lblPromo.Text = _slides(_currentSlide)
        End If
    End Sub

    Private Sub CleanupTimer()
        If _timer IsNot Nothing Then
            _timer.Stop()
            _timer.Dispose()
            _timer = Nothing
        End If
    End Sub
End Class
