Imports System.Drawing
Imports System.Windows.Forms

''' <summary>
''' Universal responsive scaling helper for all forms and dialogs
''' Ensures consistent touch-friendly sizing across all screen sizes
''' </summary>
Public Class ResponsiveHelper
    Private Shared _scaleFactor As Single = 1.0F
    Private Shared _screenWidth As Integer
    Private Shared _screenHeight As Integer
    Private Const BASE_WIDTH As Integer = 1920
    Private Const BASE_HEIGHT As Integer = 1080
    Private Const MIN_SCALE As Single = 0.6F
    Private Const MAX_SCALE As Single = 2.0F
    
    ''' <summary>
    ''' Initialize screen scaling - call this once at app startup
    ''' </summary>
    Public Shared Sub Initialize()
        _screenWidth = Screen.PrimaryScreen.Bounds.Width
        _screenHeight = Screen.PrimaryScreen.Bounds.Height
        
        Dim widthScale As Single = CSng(_screenWidth) / CSng(BASE_WIDTH)
        Dim heightScale As Single = CSng(_screenHeight) / CSng(BASE_HEIGHT)
        
        _scaleFactor = Math.Min(widthScale, heightScale)
        
        ' Enforce min/max limits
        If _scaleFactor < MIN_SCALE Then _scaleFactor = MIN_SCALE
        If _scaleFactor > MAX_SCALE Then _scaleFactor = MAX_SCALE
        
        Debug.WriteLine($"[RESPONSIVE HELPER] Screen: {_screenWidth}x{_screenHeight}, Scale: {_scaleFactor:F2}")
    End Sub
    
    ''' <summary>
    ''' Get current scale factor
    ''' </summary>
    Public Shared ReadOnly Property ScaleFactor As Single
        Get
            If _scaleFactor = 0 Then Initialize()
            Return _scaleFactor
        End Get
    End Property
    
    ''' <summary>
    ''' Scale a size value (width or height in pixels)
    ''' </summary>
    Public Shared Function ScaleSize(baseSize As Integer) As Integer
        If _scaleFactor = 0 Then Initialize()
        Return CInt(baseSize * _scaleFactor)
    End Function
    
    ''' <summary>
    ''' Scale a font size
    ''' </summary>
    Public Shared Function ScaleFont(baseFontSize As Single) As Single
        If _scaleFactor = 0 Then Initialize()
        Return baseFontSize * _scaleFactor
    End Function
    
    ''' <summary>
    ''' Scale a Size structure
    ''' </summary>
    Public Shared Function ScaleSize(baseSize As Size) As Size
        Return New Size(ScaleSize(baseSize.Width), ScaleSize(baseSize.Height))
    End Function
    
    ''' <summary>
    ''' Scale a Point structure
    ''' </summary>
    Public Shared Function ScalePoint(basePoint As Point) As Point
        Return New Point(ScaleSize(basePoint.X), ScaleSize(basePoint.Y))
    End Function
    
    ''' <summary>
    ''' Ensure minimum touch target size (40x40 pixels)
    ''' </summary>
    Public Shared Function EnsureTouchTarget(size As Size) As Size
        Const MIN_TOUCH_SIZE As Integer = 40
        Return New Size(
            Math.Max(size.Width, MIN_TOUCH_SIZE),
            Math.Max(size.Height, MIN_TOUCH_SIZE)
        )
    End Function
    
    ''' <summary>
    ''' Create a scaled Font
    ''' </summary>
    Public Shared Function CreateScaledFont(fontFamily As String, baseSize As Single, Optional style As FontStyle = FontStyle.Regular) As Font
        Return New Font(fontFamily, ScaleFont(baseSize), style)
    End Function
    
    ''' <summary>
    ''' Scale a form/dialog to fit screen
    ''' </summary>
    Public Shared Sub ScaleForm(form As Form, baseWidth As Integer, baseHeight As Integer)
        If _scaleFactor = 0 Then Initialize()
        
        Dim scaledWidth = ScaleSize(baseWidth)
        Dim scaledHeight = ScaleSize(baseHeight)
        
        ' Don't exceed screen bounds
        If scaledWidth > _screenWidth * 0.95 Then scaledWidth = CInt(_screenWidth * 0.95)
        If scaledHeight > _screenHeight * 0.95 Then scaledHeight = CInt(_screenHeight * 0.95)
        
        form.Size = New Size(scaledWidth, scaledHeight)
        form.StartPosition = FormStartPosition.CenterScreen
    End Sub
    
    ''' <summary>
    ''' Create a scaled button with touch-friendly size
    ''' </summary>
    Public Shared Function CreateScaledButton(text As String, baseWidth As Integer, baseHeight As Integer, baseX As Integer, baseY As Integer, backColor As Color, fontSize As Single) As Button
        Dim scaledSize = EnsureTouchTarget(New Size(ScaleSize(baseWidth), ScaleSize(baseHeight)))
        
        Return New Button With {
            .Text = text,
            .Size = scaledSize,
            .Location = ScalePoint(New Point(baseX, baseY)),
            .BackColor = backColor,
            .ForeColor = Color.White,
            .Font = CreateScaledFont("Segoe UI", fontSize, FontStyle.Bold),
            .FlatStyle = FlatStyle.Flat,
            .Cursor = Cursors.Hand
        }
    End Function
    
    ''' <summary>
    ''' Get scaled spacing value
    ''' </summary>
    Public Shared Function ScaleSpacing(baseSpacing As Integer) As Integer
        Return Math.Max(5, ScaleSize(baseSpacing)) ' Minimum 5px spacing
    End Function
    
    ''' <summary>
    ''' Get screen dimensions
    ''' </summary>
    Public Shared ReadOnly Property ScreenWidth As Integer
        Get
            If _screenWidth = 0 Then Initialize()
            Return _screenWidth
        End Get
    End Property
    
    Public Shared ReadOnly Property ScreenHeight As Integer
        Get
            If _screenHeight = 0 Then Initialize()
            Return _screenHeight
        End Get
    End Property
End Class
