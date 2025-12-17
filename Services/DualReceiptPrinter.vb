Imports System.Data
Imports System.Data.SqlClient
Imports System.Drawing
Imports System.Drawing.Printing

Public Class DualReceiptPrinter
    Private ReadOnly _connectionString As String
    Private ReadOnly _branchID As Integer

    Public Sub New(connectionString As String, branchID As Integer)
        _connectionString = connectionString
        _branchID = branchID
    End Sub

    ''' <summary>
    ''' Print receipt to both thermal slip printer (default) and continuous network printer
    ''' </summary>
    Public Sub PrintDualReceipt(receiptData As Dictionary(Of String, Object), cartItems As DataTable)
        ' 1. Print to default thermal slip printer (80mm) - ALWAYS try this first
        Try
            PrintToThermalPrinter(receiptData, cartItems)
            System.Windows.Forms.MessageBox.Show("Successfully printed to Thermal Printer", "DEBUG: Thermal Print", System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Information)
        Catch ex As Exception
            System.Windows.Forms.MessageBox.Show($"Thermal printer error: {ex.Message}", "Thermal Printer Error", System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Warning)
        End Try

        ' 2. Print to continuous network printer with XY positioning - independent of thermal
        Try
            PrintToContinuousPrinter(receiptData, cartItems)
            System.Windows.Forms.MessageBox.Show("Successfully printed to Continuous Printer", "DEBUG: Continuous Print", System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Information)
        Catch ex As Exception
            System.Windows.Forms.MessageBox.Show($"Continuous printer error: {ex.Message}", "Continuous Printer Error", System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Warning)
        End Try
    End Sub

    ''' <summary>
    ''' Print to thermal slip printer (80mm Epson)
    ''' </summary>
    Public Sub PrintToThermalPrinter(receiptData As Dictionary(Of String, Object), cartItems As DataTable)
        Try
            Dim printDoc As New PrintDocument()
            
            ' Use Windows default printer - don't set PrinterName to use system default
            ' This allows the user to set their preferred receipt printer as default in Windows
            ' Don't force paper size - let printer use its default settings
            
            Dim changeAmount As Decimal = If(receiptData.ContainsKey("ChangeAmount"), CDec(receiptData("ChangeAmount")), 0D)
            Dim paymentMethod As String = If(receiptData.ContainsKey("PaymentMethod"), receiptData("PaymentMethod").ToString(), "CASH")
            Dim cashAmount As Decimal = If(receiptData.ContainsKey("CashAmount"), CDec(receiptData("CashAmount")), 0D)
            Dim cardAmount As Decimal = If(receiptData.ContainsKey("CardAmount"), CDec(receiptData("CardAmount")), 0D)
            Dim subtotal As Decimal = If(receiptData.ContainsKey("Subtotal"), CDec(receiptData("Subtotal")), 0D)
            Dim taxAmount As Decimal = If(receiptData.ContainsKey("TaxAmount"), CDec(receiptData("TaxAmount")), 0D)
            Dim totalAmount As Decimal = If(receiptData.ContainsKey("TotalAmount"), CDec(receiptData("TotalAmount")), 0D)
            
            AddHandler printDoc.PrintPage, Sub(sender, e)
                Dim font As New Font("Courier New", 8)
                Dim fontBold As New Font("Courier New", 8, FontStyle.Bold)
                Dim fontLarge As New Font("Courier New", 11, FontStyle.Bold)
                Dim yPos As Single = 5
                Dim leftMargin As Single = 5
                
                ' Store header - centered
                Dim headerText = "OVEN DELIGHTS"
                Dim headerSize = e.Graphics.MeasureString(headerText, fontLarge)
                e.Graphics.DrawString(headerText, fontLarge, Brushes.Black, (302 - headerSize.Width) / 2, yPos)
                yPos += 22
                
                ' Branch info
                e.Graphics.DrawString(receiptData("BranchName").ToString(), font, Brushes.Black, leftMargin, yPos)
                yPos += 15
                
                ' Date and time
                e.Graphics.DrawString(CType(receiptData("SaleDateTime"), DateTime).ToString("dd/MM/yyyy HH:mm:ss"), font, Brushes.Black, leftMargin, yPos)
                yPos += 15
                
                ' Invoice number
                e.Graphics.DrawString($"Invoice: {receiptData("InvoiceNumber")}", font, Brushes.Black, leftMargin, yPos)
                yPos += 15
                
                ' Till and Cashier
                e.Graphics.DrawString($"Till: {receiptData("TillNumber")}", font, Brushes.Black, leftMargin, yPos)
                yPos += 15
                e.Graphics.DrawString($"Cashier: {receiptData("CashierName")}", font, Brushes.Black, leftMargin, yPos)
                yPos += 18
                
                ' Separator
                e.Graphics.DrawString("======================================", font, Brushes.Black, leftMargin, yPos)
                yPos += 15
                
                ' Column headers
                e.Graphics.DrawString("Item              Qty  Price  Total", fontBold, Brushes.Black, leftMargin, yPos)
                yPos += 15
                
                ' Line items
                For Each row As DataRow In cartItems.Rows
                    Dim itemName = row("Product").ToString()
                    If itemName.Length > 17 Then itemName = itemName.Substring(0, 14) & "..."
                    
                    Dim qty = CDec(row("Qty"))
                    Dim price = CDec(row("Price"))
                    Dim lineTotal = CDec(row("Total"))
                    
                    Dim line = String.Format("{0,-17} {1,3} {2,5:N2} {3,6:N2}", itemName, qty, price, lineTotal)
                    e.Graphics.DrawString(line, font, Brushes.Black, leftMargin, yPos)
                    yPos += 14
                Next
                
                ' Separator
                e.Graphics.DrawString("======================================", font, Brushes.Black, leftMargin, yPos)
                yPos += 15
                
                ' Totals
                e.Graphics.DrawString($"Subtotal:                 R {subtotal:N2}", font, Brushes.Black, leftMargin, yPos)
                yPos += 14
                e.Graphics.DrawString($"Tax (15%):                R {taxAmount:N2}", font, Brushes.Black, leftMargin, yPos)
                yPos += 14
                e.Graphics.DrawString($"TOTAL:                    R {totalAmount:N2}", fontBold, Brushes.Black, leftMargin, yPos)
                yPos += 18
                
                ' Payment info
                e.Graphics.DrawString($"Payment: {paymentMethod}", font, Brushes.Black, leftMargin, yPos)
                yPos += 14
                
                If paymentMethod = "CASH" OrElse paymentMethod = "SPLIT" Then
                    e.Graphics.DrawString($"Cash Tendered:            R {cashAmount:N2}", font, Brushes.Black, leftMargin, yPos)
                    yPos += 14
                    If changeAmount > 0 Then
                        e.Graphics.DrawString($"CHANGE:                   R {changeAmount:N2}", fontBold, Brushes.Black, leftMargin, yPos)
                        yPos += 14
                    End If
                End If
                
                If paymentMethod = "SPLIT" Then
                    e.Graphics.DrawString($"Card Amount:              R {cardAmount:N2}", font, Brushes.Black, leftMargin, yPos)
                    yPos += 14
                End If
                
                yPos += 10
                e.Graphics.DrawString("======================================", font, Brushes.Black, leftMargin, yPos)
                yPos += 15
                
                ' Barcode for returns (7-digit format, research-based settings)
                Try
                    Dim invoiceNum = receiptData("InvoiceNumber").ToString()
                    Dim barcodeImage = BarcodeGenerator.GenerateCode39Barcode(invoiceNum, 180, 60)
                    e.Graphics.DrawImage(barcodeImage, CInt((302 - 180) / 2), CInt(yPos))
                    yPos += 65
                    
                    barcodeImage.Dispose()
                Catch ex As Exception
                    Dim invoiceNum = receiptData("InvoiceNumber").ToString()
                    Dim invNumFont As New Font("Arial", 20, FontStyle.Bold)
                    Dim invNumSize = e.Graphics.MeasureString(invoiceNum, invNumFont)
                    e.Graphics.DrawString(invoiceNum, invNumFont, Brushes.Black, (302 - invNumSize.Width) / 2, yPos)
                    yPos += 28
                End Try
                
                ' Footer - centered
                Dim footer0 = "SCAN BARCODE FOR RETURNS"
                Dim footer0Size = e.Graphics.MeasureString(footer0, fontBold)
                e.Graphics.DrawString(footer0, fontBold, Brushes.Black, (302 - footer0Size.Width) / 2, yPos)
                yPos += 14
                
                Dim footer1 = "Thank you for your purchase!"
                Dim footer1Size = e.Graphics.MeasureString(footer1, font)
                e.Graphics.DrawString(footer1, font, Brushes.Black, (302 - footer1Size.Width) / 2, yPos)
                yPos += 14
                
                Dim footer2 = "Please come again!"
                Dim footer2Size = e.Graphics.MeasureString(footer2, font)
                e.Graphics.DrawString(footer2, font, Brushes.Black, (302 - footer2Size.Width) / 2, yPos)
            End Sub
            
            ' Print to default thermal printer
            printDoc.Print()
            
        Catch ex As Exception
            Console.WriteLine($"Thermal printer error: {ex.Message}")
        End Try
    End Sub

    ''' <summary>
    ''' Print to continuous network printer with XY field positioning from database
    ''' </summary>
    Public Sub PrintToContinuousPrinter(receiptData As Dictionary(Of String, Object), cartItems As DataTable)
        Try
            ' Get printer config from database
            Dim printerConfig = GetContinuousPrinterConfig()
            If printerConfig Is Nothing Then
                Console.WriteLine("No continuous printer configured for this branch")
                Return
            End If

            ' Get field positions from database
            Dim fieldPositions = GetFieldPositions()
            If fieldPositions.Rows.Count = 0 Then
                Console.WriteLine("No field positions configured for continuous printer")
                Return
            End If

            Dim printDoc As New PrintDocument()
            
            ' Set printer name from database
            Dim printerName = printerConfig("PrinterName").ToString()
            
            ' Validate printer exists before setting it
            If Not String.IsNullOrEmpty(printerName) Then
                Dim printerExists = False
                For Each printer As String In PrinterSettings.InstalledPrinters
                    If printer.Equals(printerName, StringComparison.OrdinalIgnoreCase) Then
                        printerExists = True
                        Exit For
                    End If
                Next
                
                If printerExists Then
                    printDoc.PrinterSettings.PrinterName = printerName
                Else
                    Throw New Exception($"Cannot find Printer ({printerName})")
                End If
            Else
                Throw New Exception("No printer name configured in database")
            End If
            
            ' Set paper size for continuous printer from database (default: 220mm x 215mm)
            Dim paperWidth As Integer = If(printerConfig("PaperWidth") IsNot DBNull.Value, CInt(printerConfig("PaperWidth")), 220)
            Dim paperHeight As Integer = If(printerConfig("PaperHeight") IsNot DBNull.Value, CInt(printerConfig("PaperHeight")), 215)
            printDoc.DefaultPageSettings.PaperSize = New PaperSize("Continuous", MillimetersToHundredthsOfInch(paperWidth), MillimetersToHundredthsOfInch(paperHeight))
            
            AddHandler printDoc.PrintPage, Sub(sender, e)
                ' Print each field at its configured XY position
                For Each fieldRow As DataRow In fieldPositions.Rows
                    Dim fieldName = fieldRow("FieldName").ToString()
                    Dim xPos = CInt(fieldRow("XPosition")) ' Already in pixels from ReceiptTemplateDesigner
                    Dim yPos = CInt(fieldRow("YPosition")) ' Already in pixels from ReceiptTemplateDesigner
                    Dim fontSize = CInt(fieldRow("FontSize"))
                    Dim isBold = CBool(fieldRow("IsBold"))
                    
                    Dim fontStyle As FontStyle = If(isBold, FontStyle.Bold, FontStyle.Regular)
                    Dim font As New Font("Arial", fontSize, fontStyle) ' Use Arial as default font
                    
                    Dim text = GetFieldValue(fieldName, receiptData, cartItems)
                    
                    If Not String.IsNullOrEmpty(text) Then
                        e.Graphics.DrawString(text, font, Brushes.Black, xPos, yPos)
                    End If
                Next
                
                ' Print line items dynamically
                PrintLineItems(e.Graphics, fieldPositions, cartItems)
            End Sub
            
            ' Print to continuous printer
            printDoc.Print()
            
        Catch ex As Exception
            Console.WriteLine($"Continuous printer error: {ex.Message}")
        End Try
    End Sub

    ''' <summary>
    ''' Get continuous printer configuration from database (uses existing PrinterConfig table)
    ''' </summary>
    Private Function GetContinuousPrinterConfig() As DataRow
        Using conn As New SqlConnection(_connectionString)
            conn.Open()
            Dim sql = "SELECT PrinterName, PrinterIPAddress, PaperWidth, PaperHeight FROM PrinterConfig WHERE BranchID = @BranchID"
            Using cmd As New SqlCommand(sql, conn)
                cmd.Parameters.AddWithValue("@BranchID", _branchID)
                Using adapter As New SqlDataAdapter(cmd)
                    Dim dt As New DataTable()
                    adapter.Fill(dt)
                    If dt.Rows.Count > 0 Then
                        Return dt.Rows(0)
                    End If
                End Using
            End Using
        End Using
        Return Nothing
    End Function

    ''' <summary>
    ''' Get field positions from database (uses existing ReceiptTemplateConfig table)
    ''' </summary>
    Private Function GetFieldPositions() As DataTable
        Using conn As New SqlConnection(_connectionString)
            conn.Open()
            Dim sql = "SELECT FieldName, XPosition, YPosition, FontSize, IsBold FROM ReceiptTemplateConfig WHERE BranchID = @BranchID AND IsEnabled = 1 ORDER BY YPosition, XPosition"
            Using cmd As New SqlCommand(sql, conn)
                cmd.Parameters.AddWithValue("@BranchID", _branchID)
                Using adapter As New SqlDataAdapter(cmd)
                    Dim dt As New DataTable()
                    adapter.Fill(dt)
                    Return dt
                End Using
            End Using
        End Using
    End Function

    ''' <summary>
    ''' Get field value from receipt data
    ''' </summary>
    Private Function GetFieldValue(fieldName As String, receiptData As Dictionary(Of String, Object), cartItems As DataTable) As String
        Select Case fieldName
            Case "StoreName"
                Return "OVEN DELIGHTS"
            Case "BranchName"
                Return receiptData("BranchName").ToString()
            Case "InvoiceNumber"
                Return $"Invoice: {receiptData("InvoiceNumber")}"
            Case "Date"
                Return $"Date: {CType(receiptData("SaleDateTime"), DateTime):dd/MM/yyyy}"
            Case "Time"
                Return $"Time: {CType(receiptData("SaleDateTime"), DateTime):HH:mm:ss}"
            Case "TillNumber"
                Return $"Till: {receiptData("TillNumber")}"
            Case "CashierName"
                Return $"Cashier: {receiptData("CashierName")}"
            Case "ItemsHeader"
                Return "Item                      Qty    Price      Total"
            Case "Subtotal"
                Return $"Subtotal: R {CDec(receiptData("Subtotal")):N2}"
            Case "Tax"
                Return $"Tax (15%): R {CDec(receiptData("TaxAmount")):N2}"
            Case "Total"
                Return $"TOTAL: R {CDec(receiptData("TotalAmount")):N2}"
            Case "PaymentMethod"
                Return $"Payment: {receiptData("PaymentMethod")}"
            Case "CashTendered"
                If receiptData.ContainsKey("CashAmount") Then
                    Return $"Cash: R {CDec(receiptData("CashAmount")):N2}"
                End If
            Case "Change"
                If receiptData.ContainsKey("ChangeAmount") Then
                    Dim change = CDec(receiptData("ChangeAmount"))
                    If change > 0 Then
                        Return $"CHANGE: R {change:N2}"
                    End If
                End If
            Case "ThankYou"
                Return "Thank you for your purchase!"
            Case Else
                Return ""
        End Select
        Return ""
    End Function

    ''' <summary>
    ''' Print line items dynamically on continuous printer
    ''' </summary>
    Private Sub PrintLineItems(graphics As Graphics, fieldPositions As DataTable, cartItems As DataTable)
        ' Find ItemLine1 position (first line item position from ReceiptTemplateDesigner)
        Dim startRow = fieldPositions.Select("FieldName = 'ItemLine1'")
        If startRow.Length = 0 Then Return
        
        Dim xPos = CInt(startRow(0)("XPosition")) ' Already in pixels
        Dim yPos = CInt(startRow(0)("YPosition")) ' Already in pixels
        Dim fontSize = CInt(startRow(0)("FontSize"))
        Dim font As New Font("Arial", fontSize)
        
        Dim lineHeight = 15 ' pixels between lines
        
        For Each row As DataRow In cartItems.Rows
            Dim itemName = row("Product").ToString()
            Dim qty = CDec(row("Qty"))
            Dim price = CDec(row("Price"))
            Dim lineTotal = CDec(row("Total"))
            
            Dim line = String.Format("{0,-30} {1,10:N2} {2,15:N2} {3,15:N2}", itemName, qty, price, lineTotal)
            graphics.DrawString(line, font, Brushes.Black, xPos, yPos)
            yPos += lineHeight
        Next
    End Sub

    ''' <summary>
    ''' Convert millimeters to hundredths of an inch (for PrintDocument)
    ''' </summary>
    Private Function MillimetersToHundredthsOfInch(mm As Integer) As Integer
        Return CInt(mm * 3.937) ' 1mm = 0.03937 inches = 3.937 hundredths of inch
    End Function
End Class
