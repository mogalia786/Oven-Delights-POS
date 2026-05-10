' Decompiled with JetBrains decompiler
' Type: Overn_Delights_POS.POSMainForm_REDESIGN
' Assembly: Overn-Delights-POS, Version=1.0.0.37, Culture=neutral, PublicKeyToken=null
' MVID: D972AA95-4F3D-4B5C-88A0-46AFBD999782
' Assembly location: C:\Development Apps\Cascades projects\Overn-Delights-POS\Overn-Delights-POS\bin\Release\Overn-Delights-POS.exe
' XML documentation location: C:\Development Apps\Cascades projects\Overn-Delights-POS\Overn-Delights-POS\bin\Release\Overn-Delights-POS.xml

Imports Microsoft.VisualBasic
Imports Microsoft.VisualBasic.CompilerServices
Imports System
Imports System.Collections
Imports System.Collections.Generic
Imports System.Configuration
Imports System.Data
Imports System.Data.SqlClient
Imports System.Drawing
Imports System.Drawing.Printing
Imports System.IO
Imports System.Linq
Imports System.Runtime.CompilerServices
Imports System.Text
Imports System.Threading
Imports System.Threading.Tasks
Imports System.Windows.Forms

Namespace Overn_Delights_POS

Public Class POSMainForm_REDESIGN_EXACT
    Inherits Form
    
    Private _cashierID As Integer
    Private _cashierName As String
    Private _branchID As Integer
    Private _tillPointID As Integer
    Private _connectionString As String
    Private _cartItems As DataTable
    Private _allProducts As DataTable
    Private pnlTop As Panel
    Private pnlCategories As FlowLayoutPanel
    Private pnlProducts As Panel
    Private pnlCart As Panel
    Private pnlShortcuts As Panel
    Private pnlNumpad As Panel
    Private pnlSearchBar As Panel
    Private flpProducts As FlowLayoutPanel
    Private dgvCart As DataGridView
    Private lblTotal As Label
    Private lblSubtotal As Label
    Private lblTax As Label
    Private txtSearch As TextBox
    Private txtSearchByName As TextBox
    Private txtBarcodeScanner As TextBox
    Private btnRefresh As Button
    Private btnModifyQty As Button
    Private btnPriceOverride As Button
    Private _onScreenKeyboard As OnScreenKeyboard
    Private _currentView As String
    Private _currentCategoryId As Integer
    Private _currentCategoryName As String
    Private _currentSubCategoryId As Integer
    Private _currentSubCategoryName As String
    Private _categoryService As CategoryNavigationService
    Private lblBreadcrumb As Label
    Private _searchTimer As System.Windows.Forms.Timer
    Private _pendingSearchText As String
    Private _idleTimer As System.Windows.Forms.Timer
    Private _idleOverlay As Panel
    Private _messageTimer As System.Windows.Forms.Timer
    Private _currentMessageIndex As Integer
    Private _lblRotatingMessage As Label
    Private Const IDLE_TIMEOUT_MS As Integer = 300000
    Private _tileWidth As Integer
    Private _tileHeight As Integer
    Private _tilesPerRow As Integer
    Private _ironRed As Color
    Private _ironRedDark As Color
    Private _ironGold As Color
    Private _ironGoldDark As Color
    Private _ironDark As Color
    Private _ironBlue As Color
    Private _ironBlueDark As Color
    Private _ironDarkBlue As Color
    Private _ironSilver As Color
    Private _ironGlow As Color
    Private _darkBlue As Color
    Private _lightBlue As Color
    Private _green As Color
    Private _orange As Color
    Private _red As Color
    Private _purple As Color
    Private _yellow As Color
    Private _lightGray As Color
    Private _darkGray As Color
    Private _screenWidth As Integer
    Private _screenHeight As Integer
    Private _scaleFactor As Single
    Private _baseWidth As Integer
    Private _baseHeight As Integer
    Private _priceOverrides As Dictionary(Of Integer, PriceOverride)
    Private _isOrderMode As Boolean
    Private _btnAddOrderInfo As Button
    Private _isUserDefinedMode As Boolean
    Private _userDefinedOrderData As UserDefinedOrderData
    Private _btnCompleteUserDefined As Button
    Private _isOrderCollectionMode As Boolean
    Private _collectionOrderID As Integer
    Private _collectionOrderNumber As String
    Private _collectionDepositPaid As Decimal
    Private _collectionTotalAmount As Decimal
    Private _collectionCustomerName As String

    Public Sub New(cashierID As Integer, cashierName As String, branchID As Integer, tillPointID As Integer)
        _cartItems = New DataTable()
        _allProducts = New DataTable()
        _currentView = "categories"
        _currentCategoryId = 0
        _currentCategoryName = ""
        _currentSubCategoryId = 0
        _currentSubCategoryName = ""
        _categoryService = New CategoryNavigationService()
        _pendingSearchText = ""
        _currentMessageIndex = 0
        _tileWidth = 100
        _tileHeight = 70
        _tilesPerRow = 6
        _ironRed = ColorTranslator.FromHtml("#C1272D")
        _ironRedDark = ColorTranslator.FromHtml("#8B0000")
        _ironGold = ColorTranslator.FromHtml("#FFD700")
        _ironGoldDark = ColorTranslator.FromHtml("#DAA520")
        _ironDark = ColorTranslator.FromHtml("#0a0e27")
        _ironBlue = ColorTranslator.FromHtml("#00D4FF")
        _ironBlueDark = ColorTranslator.FromHtml("#0099CC")
        _ironDarkBlue = ColorTranslator.FromHtml("#1a1f3a")
        _ironSilver = ColorTranslator.FromHtml("#C0C0C0")
        _ironGlow = ColorTranslator.FromHtml("#00F5FF")
        _darkBlue = ColorTranslator.FromHtml("#2C3E50")
        _lightBlue = ColorTranslator.FromHtml("#3498DB")
        _green = ColorTranslator.FromHtml("#27AE60")
        _orange = ColorTranslator.FromHtml("#E67E22")
        _red = ColorTranslator.FromHtml("#E74C3C")
        _purple = ColorTranslator.FromHtml("#9B59B6")
        _yellow = ColorTranslator.FromHtml("#F39C12")
        _lightGray = ColorTranslator.FromHtml("#ECF0F1")
        _darkGray = ColorTranslator.FromHtml("#7F8C8D")
        _scaleFactor = 1.0F
        _baseWidth = 1920
        _baseHeight = 1080
        _priceOverrides = New Dictionary(Of Integer, PriceOverride)()
        _isOrderMode = False
        _isUserDefinedMode = False
        _isOrderCollectionMode = False
        _collectionOrderID = 0
        _collectionOrderNumber = ""
        _collectionDepositPaid = 0D
        _collectionTotalAmount = 0D
        _collectionCustomerName = ""
        _cashierID = cashierID
        _cashierName = cashierName
        _branchID = branchID
        _tillPointID = tillPointID
        _connectionString = ConfigurationManager.ConnectionStrings("OvenDelightsERPConnectionString").ConnectionString
        Me.KeyPreview = True
        SetupModernUI()
        InitializeScreenScaling()
        InitializeCart()
        SetupIdleScreen()
        InitializeSearchTimer()
        InitializeIdleTimer()
        ' ISSUE: reference to a compiler-generated method
        AddHandler Me.Resize, AddressOf Me._Lambda__69_0
        AddHandler Me.Shown, AddressOf Me.OnFormShown
    End Sub

    Private Sub OnFormShown(sender As Object, e As EventArgs)
        Try
            Me.WindowState = FormWindowState.Maximized
            InitializeScreenScaling()
            HandleFormResize()
            If txtBarcodeScanner Is Nothing Then
                Return
            End If
            txtBarcodeScanner.Focus()
        Catch ex As Exception
            ProjectData.SetProjectError(ex)
            MessageBox.Show($"Screen detection error: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Hand)
            ProjectData.ClearProjectError()
        End Try
    End Sub

    Private Sub InitializeSearchTimer()
        _searchTimer = New System.Windows.Forms.Timer()
        _searchTimer.Interval = 300
        ' ISSUE: reference to a compiler-generated method
        AddHandler _searchTimer.Tick, AddressOf Me._Lambda__71_0
    End Sub

    Private Sub LoadCategories()
        ' Check if panel is initialized
        If pnlCategories Is Nothing Then
            ShowCategories()
            ResetIdleTimer()
            Return
        End If

        ' Simple clear like decompiled version
        pnlCategories.Controls.Clear()

        ' Add "All Products" button - EXACT MATCH WITH DECOMPILED
        Dim btnAll As New Button With {
            .Text = "📦 All Products",
            .Size = New Size(190, 60),
            .BackColor = _darkBlue,
            .ForeColor = Color.White,
            .Font = New Font("Segoe UI", 11, FontStyle.Bold),
            .FlatStyle = FlatStyle.Flat,
            .Cursor = Cursors.Hand,
            .TextAlign = ContentAlignment.MiddleLeft,
            .Padding = New Padding(10, 0, 0, 0),
            .Margin = New Padding(0, 0, 0, 10)
        }
        btnAll.FlatAppearance.BorderSize = 0
        AddHandler btnAll.Click, Sub() LoadProducts()
        AddHandler btnAll.MouseEnter, Sub() btnAll.BackColor = _lightBlue
        AddHandler btnAll.MouseLeave, Sub() btnAll.BackColor = _darkBlue
        pnlCategories.Controls.Add(btnAll)

        Try
            ' Use exact SQL from decompiled version
            Dim sql = "SELECT CategoryID, CategoryCode, CategoryName FROM ProductCategories WHERE IsActive = 1 AND CategoryName NOT IN ('BUITERCREAM CAKE', 'FRESHCREAM CAKE') ORDER BY CategoryName"
            
            Using conn As New SqlConnection(_connectionString)
                conn.Open()
                Using cmd As New SqlCommand(sql, conn)
                    Using reader = cmd.ExecuteReader()
                        Dim num As Integer = 0
                        Dim colors() As Color = {_lightBlue, _green, _orange, _purple, _red, _yellow}
                        
                        While reader.Read()
                            Dim categoryCode = reader.GetString(1)
                            Dim categoryName = reader.GetString(2)
                            Dim categoryIcon = GetCategoryIcon(categoryCode)
                            Dim btnColor = colors(num Mod colors.Length)

                            Dim btn As New Button With {
                                .Text = $"{categoryIcon} {categoryName}",
                                .Size = New Size(190, 60),
                                .BackColor = btnColor,
                                .ForeColor = Color.White,
                                .Font = New Font("Segoe UI", 11, FontStyle.Bold),
                                .FlatStyle = FlatStyle.Flat,
                                .Cursor = Cursors.Hand,
                                .TextAlign = ContentAlignment.MiddleLeft,
                                .Padding = New Padding(10, 0, 0, 0),
                                .Tag = categoryName,
                                .Margin = New Padding(0, 0, 0, 5)
                            }
                            btn.FlatAppearance.BorderSize = 0

                            AddHandler btn.Click, Sub(s, ev)
                                                      LoadProducts(categoryName)
                                                  End Sub
                            AddHandler btn.MouseEnter, Sub(s, ev)
                                                           btn.BackColor = ControlPaint.Light(btnColor, 0.2)
                                                       End Sub
                            AddHandler btn.MouseLeave, Sub(s, ev)
                                                           btn.BackColor = btnColor
                                                       End Sub

                            pnlCategories.Controls.Add(btn)
                            num += 1
                        End While
                    End Using
                End Using
            End Using
        Catch ex As Exception
            MessageBox.Show($"Error loading categories: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    Private Function GetCategoryIcon(categoryCode As String) As String
        Select Case categoryCode.ToUpper()
            Case "BREAD", "BRD" : Return "🍞"
            Case "CAKE", "CKE" : Return "🎂"
            Case "PASTRY", "PST" : Return "🥐"
            Case "COOKIE", "COK" : Return "🍪"
            Case "CUPCAKE", "CPC" : Return "🧁"
            Case "DONUT", "DNT" : Return "🍩"
            Case "PIE" : Return "🥧"
            Case "BEVERAGE", "BEV", "BEVERAGES" : Return "☕"
            Case "MANUFACTURED", "GOODS" : Return "🏭"
            Case "PACKAGING" : Return "📦"
            Case "RAW", "MATERIALS" : Return "🌾"
            Case Else : Return "📦"
        End Select
    End Function

    Private Sub LoadProducts(Optional category As String = Nothing)
        ' TODO: Implement LoadProducts method from decompiled version
    End Sub

    Private Sub ShowCategories()
        ' TODO: Implement ShowCategories method from decompiled version
    End Sub

    Private Sub SetupModernUI()
        ' TODO: Implement SetupModernUI method from decompiled version
    End Sub

    Private Sub InitializeScreenScaling()
        ' TODO: Implement InitializeScreenScaling method from decompiled version
    End Sub

    Private Sub InitializeCart()
        ' TODO: Implement InitializeCart method from decompiled version
    End Sub

    Private Sub SetupIdleScreen()
        ' TODO: Implement SetupIdleScreen method from decompiled version
    End Sub

    Private Sub InitializeIdleTimer()
        ' TODO: Implement InitializeIdleTimer method from decompiled version
    End Sub

    Private Sub HandleFormResize()
        ' TODO: Implement HandleFormResize method from decompiled version
    End Sub

    Private Sub _Lambda__69_0(sender As Object, e As EventArgs)
        ' TODO: Implement resize handler from decompiled version
    End Sub

    Private Sub _Lambda__71_0(sender As Object, e As EventArgs)
        ' TODO: Implement search timer handler from decompiled version
    End Sub

    Private Sub ResetIdleTimer()
        ' TODO: Implement ResetIdleTimer method from decompiled version
    End Sub

End Class

Public Class PriceOverride
    Public Property OriginalPrice As Decimal
    Public Property NewPrice As Decimal
    Public Property Reason As String
    Public Property CashierName As String
    Public Property Timestamp As DateTime
End Class

Public Class UserDefinedOrderData
    Public Property CustomerName As String
    Public Property CustomerPhone As String
    Public Property PickupDate As DateTime
    Public Property SpecialInstructions As String
End Class

Public Class OnScreenKeyboard
    ' TODO: Implement OnScreenKeyboard class
End Class
