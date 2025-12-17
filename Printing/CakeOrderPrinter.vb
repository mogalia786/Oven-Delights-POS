Imports System.Drawing
Imports System.Drawing.Printing
Imports System.Configuration
Imports System.Data.SqlClient

Public Class CakeOrderPrinter
    Private _printDocument As PrintDocument
    Private _orderData As CakeOrderPrintData
    Private _currentPage As Integer = 0
    Private _fieldPositions As New Dictionary(Of String, FieldPosition)
    Private _connectionString As String
    
    ' Field position configuration (loaded from ERP ReceiptTemplateConfig)
    Private Class FieldPosition
        Public Property XPos As Integer
        Public Property YPos As Integer
        Public Property FontSize As Integer
        Public Property IsBold As Boolean
        Public Property IsEnabled As Boolean
    End Class
    
    Public Class CakeOrderPrintData
        ' Company/Branch Info
        Public Property BranchID As Integer
        Public Property BranchName As String
        Public Property BranchAddress As String
        Public Property BranchTelephone As String
        Public Property BranchEmail As String
        Public Property VATNumber As String
        
        ' Cake Details (Top Right)
        Public Property CakeColor As String
        Public Property CakePicture As String
        Public Property CollectionDate As Date
        Public Property CollectionDay As String
        Public Property CollectionTime As String
        
        ' Order Info
        Public Property CollectionPoint As String
        Public Property OrderNumber As String
        Public Property OrderDate As Date
        Public Property OrderTakenBy As String
        
        ' Customer Info
        Public Property CustomerName As String
        Public Property CustomerPhone As String
        Public Property AccountNumber As String
        
        ' Items
        Public Property Items As New List(Of OrderItem)
        
        ' Special Requests
        Public Property SpecialRequests As String
        
        ' Totals
        Public Property InvoiceTotal As Decimal
        Public Property DepositPaid As Decimal
        Public Property BalanceOwing As Decimal
        
        Public Class OrderItem
            Public Property Description As String
            Public Property Quantity As Integer
            Public Property UnitPrice As Decimal
            Public Property TotalPrice As Decimal
        End Class
    End Class
    
    Public Sub New(orderData As CakeOrderPrintData)
        _orderData = orderData
        _connectionString = ConfigurationManager.ConnectionStrings("OvenDelightsERPConnectionString").ConnectionString
        LoadFieldPositions(orderData.BranchID)
        _printDocument = New PrintDocument()
        AddHandler _printDocument.PrintPage, AddressOf PrintPage
    End Sub
    
    Private Sub LoadFieldPositions(branchID As Integer)
        Try
            Using conn As New SqlConnection(_connectionString)
                conn.Open()
                
                Dim sql = "SELECT FieldName, XPosition, YPosition, FontSize, IsBold, IsEnabled FROM ReceiptTemplateConfig WHERE BranchID = @BranchID"
                Using cmd As New SqlCommand(sql, conn)
                    cmd.Parameters.AddWithValue("@BranchID", branchID)
                    Using reader = cmd.ExecuteReader()
                        While reader.Read()
                            Dim fieldName = reader("FieldName").ToString()
                            _fieldPositions(fieldName) = New FieldPosition With {
                                .XPos = Convert.ToInt32(reader("XPosition")),
                                .YPos = Convert.ToInt32(reader("YPosition")),
                                .FontSize = Convert.ToInt32(reader("FontSize")),
                                .IsBold = Convert.ToBoolean(reader("IsBold")),
                                .IsEnabled = Convert.ToBoolean(reader("IsEnabled"))
                            }
                        End While
                    End Using
                End Using
            End Using
        Catch ex As Exception
            ' If no config found, use defaults (will be set in PrintPage)
            System.Diagnostics.Debug.WriteLine($"Could not load field positions: {ex.Message}")
        End Try
    End Sub
    
    Public Sub Print(Optional printerName As String = Nothing)
        Try
            If Not String.IsNullOrEmpty(printerName) Then
                _printDocument.PrinterSettings.PrinterName = printerName
            End If
            
            ' Set page settings for continuous form
            _printDocument.DefaultPageSettings.PaperSize = New PaperSize("Custom", 827, 1169) ' A4 in 1/100th inch
            _printDocument.DefaultPageSettings.Margins = New Margins(0, 0, 0, 0)
            
            _printDocument.Print()
        Catch ex As Exception
            Throw New Exception($"Print error: {ex.Message}", ex)
        End Try
    End Sub
    
    Public Function ShowPrintPreview() As DialogResult
        Try
            Using previewDialog As New PrintPreviewDialog()
                previewDialog.Document = _printDocument
                previewDialog.Width = 800
                previewDialog.Height = 1000
                previewDialog.StartPosition = FormStartPosition.CenterScreen
                Return previewDialog.ShowDialog()
            End Using
        Catch ex As Exception
            Throw New Exception($"Print preview error: {ex.Message}", ex)
        End Try
    End Function
    
    Private Sub PrintPage(sender As Object, e As PrintPageEventArgs)
        Dim g As Graphics = e.Graphics
        
        ' ===== HEADER SECTION =====
        ' Use configured positions from ERP or defaults
        DrawField(g, "CompanyName", _orderData.BranchName, 10, 10, 12, True)
        DrawField(g, "CompanyTagline", "YOUR TRUSTED FAMILY BAKERY", 10, 30, 8, False)
        DrawField(g, "CoRegNo", $"Co Reg No: {_orderData.VATNumber}", 10, 50, 7, False)
        DrawField(g, "VATNumber", $"VAT Number: {_orderData.VATNumber}", 10, 65, 7, False)
        DrawField(g, "Address", _orderData.BranchAddress, 10, 95, 7, False)
        DrawField(g, "Phone", $"Tel: {_orderData.BranchTelephone}", 10, 125, 7, False)
        DrawField(g, "Email", $"Email: {_orderData.BranchEmail}", 10, 140, 7, False)
        
        ' Cake Details (Top Right)
        DrawField(g, "CakeColour", $"Cake Colour: {_orderData.CakeColor}", 450, 50, 8, False)
        DrawField(g, "CakePicture", $"Cake Picture: {_orderData.CakePicture}", 450, 65, 8, False)
        DrawField(g, "CollectionDate", $"Collection Date: {_orderData.CollectionDate:dd/MM/yyyy}", 450, 80, 8, False)
        DrawField(g, "CollectionDay", $"Collection Day: {_orderData.CollectionDay}", 450, 95, 8, False)
        DrawField(g, "CollectionTime", $"Collection Time: {_orderData.CollectionTime}", 450, 110, 8, False)
        
        ' ===== BODY SECTION =====
        ' Customer Account Info
        If Not String.IsNullOrEmpty(_orderData.AccountNumber) Then
            DrawField(g, "AccountRef", "PLEASE USE ACCOUNT NO AS REFERENCE", 10, 160, 7, True)
            DrawField(g, "AccountNo", $"ACCOUNT NO: {_orderData.AccountNumber}", 10, 185, 8, False)
            DrawField(g, "CustomerName", $"NAME: {_orderData.CustomerName}", 10, 200, 8, False)
            DrawField(g, "CellNumber", $"CELL NUMBER: {_orderData.CustomerPhone}", 10, 230, 8, False)
        End If
        
        ' Special Request
        If Not String.IsNullOrEmpty(_orderData.SpecialRequests) Then
            DrawField(g, "SpecialRequest", $"Special Request: {_orderData.SpecialRequests}", 10, 250, 8, True)
        End If
        
        ' Order Header
        DrawField(g, "OrderHeader", "Collection Point    Order Number         Date           Order Taken By", 10, 290, 8, True)
        DrawField(g, "OrderDetails", $"{_orderData.CollectionPoint}      {_orderData.OrderNumber}    {_orderData.OrderDate:dd/MM/yyyy}      {_orderData.OrderTakenBy}", 10, 305, 8, False)
        
        ' ===== ITEMS GRID =====
        DrawField(g, "ItemHeader", "Item Description              Qty Required    Unit Price (R)    Total Price (R)", 10, 330, 8, True)
        
        Dim itemYPos = 345
        For Each item In _orderData.Items
            Dim itemLine = $"{item.Description.PadRight(30)} {item.Quantity.ToString().PadLeft(4)} {item.UnitPrice.ToString("F2").PadLeft(15)} {item.TotalPrice.ToString("F2").PadLeft(18)}"
            DrawField(g, "ItemLine1", itemLine, 10, itemYPos, 8, False)
            itemYPos += 15
        Next
        
        ' ===== FOOTER SECTION =====
        DrawField(g, "Terms", "All same day orders and cancellations will attract a R30.00 service charge", 10, 650, 7, False)
        DrawField(g, "Terms2", "All changes to size, cream and date - R20.00 service charge", 10, 665, 7, False)
        
        ' Totals (Right side)
        DrawField(g, "InvoiceTotal", $"Invoice Total        {_orderData.InvoiceTotal:F2}", 450, 650, 9, True)
        DrawField(g, "DepositPaid", $"Deposit paid         {_orderData.DepositPaid:F2}", 450, 670, 9, False)
        DrawField(g, "BalanceOwing", $"Balance Owing        {_orderData.BalanceOwing:F2}", 450, 690, 9, True)
        
        e.HasMorePages = False
    End Sub
    
    Private Sub DrawField(g As Graphics, fieldName As String, text As String, defaultX As Integer, defaultY As Integer, defaultFontSize As Integer, defaultBold As Boolean)
        ' Get configured position or use defaults
        Dim xPos = defaultX
        Dim yPos = defaultY
        Dim fontSize = defaultFontSize
        Dim isBold = defaultBold
        Dim isEnabled = True
        
        If _fieldPositions.ContainsKey(fieldName) Then
            Dim config = _fieldPositions(fieldName)
            xPos = config.XPos
            yPos = config.YPos
            fontSize = config.FontSize
            isBold = config.IsBold
            isEnabled = config.IsEnabled
        End If
        
        ' Only draw if enabled
        If Not isEnabled Then Return
        
        Dim font As New Font("Courier New", fontSize, If(isBold, FontStyle.Bold, FontStyle.Regular))
        g.DrawString(text, font, Brushes.Black, xPos, yPos)
    End Sub
    
    Private Function MmToPixels(mm As Single) As Single
        ' Convert millimeters to pixels at 96 DPI
        Return mm * 96.0F / 25.4F
    End Function
    
    Public Shared Function GetBranchInfo(branchID As Integer) As (name As String, address As String, tel As String, email As String, vat As String)
        Try
            Dim connectionString = ConfigurationManager.ConnectionStrings("OvenDelightsERPConnectionString").ConnectionString
            Using conn As New SqlClient.SqlConnection(connectionString)
                conn.Open()
                Dim sql = "SELECT BranchName, Address, PhoneNumber, Email, VATNumber FROM Branches WHERE BranchID = @BranchID"
                Using cmd As New SqlClient.SqlCommand(sql, conn)
                    cmd.Parameters.AddWithValue("@BranchID", branchID)
                    Using reader = cmd.ExecuteReader()
                        If reader.Read() Then
                            Return (
                                If(IsDBNull(reader("BranchName")), "", reader("BranchName").ToString()),
                                If(IsDBNull(reader("Address")), "", reader("Address").ToString()),
                                If(IsDBNull(reader("PhoneNumber")), "", reader("PhoneNumber").ToString()),
                                If(IsDBNull(reader("Email")), "", reader("Email").ToString()),
                                If(IsDBNull(reader("VATNumber")), "", reader("VATNumber").ToString())
                            )
                        End If
                    End Using
                End Using
            End Using
        Catch ex As Exception
            ' Return defaults if error
        End Try
        
        Return ("Oven Delights", "Shop No.1, Ayesha Centre, Chatsworth", "0314019942", "info@ovendelights.co.za", "4150166793")
    End Function
End Class
