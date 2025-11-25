Imports System.Drawing.Printing
Imports System.Configuration
Imports System.Data.SqlClient

Public Class POSReceiptPrinter
    Private _connectionString As String
    Private _printDocument As PrintDocument
    Private _receiptData As Dictionary(Of String, Object)
    Private _currentLine As Integer = 0
    Private _lineHeight As Integer = 20
    Private _leftMargin As Integer = 10
    Private _templateFields As DataTable
    Private _templateSettings As Dictionary(Of String, Object)

    Public Sub New()
        _connectionString = ConfigurationManager.ConnectionStrings("OvenDelightsERPConnectionString").ConnectionString
        _printDocument = New PrintDocument()
        AddHandler _printDocument.PrintPage, AddressOf PrintReceipt
    End Sub

    Public Function PrintSaleReceipt(branchID As Integer, invoiceNumber As String, items As DataTable, 
                                     total As Decimal, paymentMethod As String, cashierName As String) As Boolean
        Try
            ' Load receipt template from database
            If Not LoadReceiptTemplate(branchID, "SALE") Then
                ' Fall back to hardcoded template if database template not found
                _templateFields = Nothing
            End If
            
            ' Get printer configuration
            Dim printerName As String = GetPrinterName(branchID)
            If String.IsNullOrEmpty(printerName) Then
                MessageBox.Show("No printer configured for this branch", "Printer Error", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                Return False
            End If

            ' Prepare receipt data
            _receiptData = New Dictionary(Of String, Object) From {
                {"Type", "SALE"},
                {"InvoiceNumber", invoiceNumber},
                {"Items", items},
                {"Total", total},
                {"PaymentMethod", paymentMethod},
                {"Cashier", cashierName},
                {"Date", DateTime.Now},
                {"BranchID", branchID}
            }

            ' Set printer
            _printDocument.PrinterSettings.PrinterName = printerName
            
            ' Print
            _currentLine = 0
            _printDocument.Print()
            Return True

        Catch ex As Exception
            MessageBox.Show($"Print error: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
            Return False
        End Try
    End Function

    Public Function PrintOrderReceipt(orderNumber As String, customerName As String, customerSurname As String, customerPhone As String, readyDate As DateTime, readyTime As TimeSpan, collectionDay As String, specialInstructions As String, depositPaid As Decimal, totalAmount As Decimal, colour As String, picture As String, items As DataTable, branchID As Integer, cashierName As String) As Boolean
        Try
            ' Load receipt template from database
            If Not LoadReceiptTemplate(branchID, "ORDER") Then
                ' Fall back to hardcoded template if database template not found
                _templateFields = Nothing
            End If
            
            ' Get printer configuration
            Dim printerName As String = GetPrinterName(branchID)
            If String.IsNullOrEmpty(printerName) Then
                MessageBox.Show("No printer configured for this branch", "Printer Error", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                Return False
            End If

            ' Prepare receipt data
            _receiptData = New Dictionary(Of String, Object) From {
                {"Type", "ORDER"},
                {"OrderNumber", orderNumber},
                {"CustomerName", customerName},
                {"CustomerSurname", customerSurname},
                {"CustomerPhone", customerPhone},
                {"ReadyDate", readyDate},
                {"ReadyTime", readyTime},
                {"CollectionDay", collectionDay},
                {"SpecialInstructions", specialInstructions},
                {"Colour", colour},
                {"Picture", picture},
                {"Items", items},
                {"DepositPaid", depositPaid},
                {"TotalAmount", totalAmount},
                {"Cashier", cashierName},
                {"Date", DateTime.Now},
                {"BranchID", branchID}
            }

            ' Print to slip printer (customer copy)
            _printDocument.PrinterSettings.PrinterName = printerName
            _currentLine = 0
            _printDocument.Print()
            
            ' Print to network printer (kitchen/production copy)
            Dim networkPrinter As String = GetNetworkPrinterIP(branchID)
            If Not String.IsNullOrEmpty(networkPrinter) Then
                _printDocument.PrinterSettings.PrinterName = networkPrinter
                _currentLine = 0
                _printDocument.Print()
            End If
            
            Return True

        Catch ex As Exception
            MessageBox.Show($"Print error: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
            Return False
        End Try
    End Function

    Public Function PrintCustomOrderReceipt(branchID As Integer, orderNumber As String, orderData As Dictionary(Of String, Object)) As Boolean
        Try
            ' Load receipt template from database
            If Not LoadReceiptTemplate(branchID, "ORDER") Then
                ' Fall back to hardcoded template if database template not found
                _templateFields = Nothing
            End If
            
            ' Get printer configuration
            Dim printerName As String = GetPrinterName(branchID)
            If String.IsNullOrEmpty(printerName) Then
                MessageBox.Show("No printer configured for this branch", "Printer Error", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                Return False
            End If

            ' Prepare receipt data
            _receiptData = New Dictionary(Of String, Object) From {
                {"Type", "ORDER"},
                {"OrderNumber", orderNumber},
                {"OrderData", orderData},
                {"Date", DateTime.Now},
                {"BranchID", branchID}
            }

            ' Set printer
            _printDocument.PrinterSettings.PrinterName = printerName
            
            ' Print
            _currentLine = 0
            _printDocument.Print()
            Return True

        Catch ex As Exception
            MessageBox.Show($"Print error: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
            Return False
        End Try
    End Function

    Private Function GetPrinterName(branchID As Integer) As String
        Try
            Using conn As New SqlConnection(_connectionString)
                conn.Open()
                Dim cmd As New SqlCommand("SELECT TOP 1 PrinterName FROM PrinterConfig WHERE BranchID = @BranchID", conn)
                cmd.Parameters.AddWithValue("@BranchID", branchID)
                Dim result = cmd.ExecuteScalar()
                Return If(result IsNot Nothing, result.ToString(), "")
            End Using
        Catch ex As Exception
            Return ""
        End Try
    End Function
    
    Private Function GetNetworkPrinterIP(branchID As Integer) As String
        Try
            Using conn As New SqlConnection(_connectionString)
                conn.Open()
                Dim cmd As New SqlCommand("SELECT TOP 1 PrinterIPAddress FROM PrinterConfig WHERE BranchID = @BranchID AND IsNetworkPrinter = 1", conn)
                cmd.Parameters.AddWithValue("@BranchID", branchID)
                Dim result = cmd.ExecuteScalar()
                Return If(result IsNot Nothing, result.ToString(), "")
            End Using
        Catch ex As Exception
            Return ""
        End Try
    End Function

    Private Function LoadReceiptTemplate(branchID As Integer, templateType As String) As Boolean
        Try
            Using conn As New SqlConnection(_connectionString)
                conn.Open()
                
                ' Load template fields from existing ReceiptTemplateConfig table (from ERP Designer)
                Dim cmdFields As New SqlCommand("
                    SELECT 
                        FieldName,
                        XPosition,
                        YPosition,
                        FontSize,
                        IsBold,
                        IsEnabled
                    FROM ReceiptTemplateConfig
                    WHERE BranchID = @BranchID AND IsEnabled = 1
                    ORDER BY YPosition", conn)
                cmdFields.Parameters.AddWithValue("@BranchID", branchID)
                
                _templateFields = New DataTable()
                Using adapter As New SqlDataAdapter(cmdFields)
                    adapter.Fill(_templateFields)
                End Using
                
                If _templateFields.Rows.Count > 0 Then
                    ' Use first field's position as base margin
                    _leftMargin = 10 ' Default left margin
                    _lineHeight = 16 ' Default line height
                    Return True
                Else
                    Return False ' No template found
                End If
            End Using
        Catch ex As Exception
            Return False
        End Try
    End Function

    Private Function GetBranchInfo(branchID As Integer) As Dictionary(Of String, String)
        Try
            Using conn As New SqlConnection(_connectionString)
                conn.Open()
                Dim cmd As New SqlCommand("SELECT BranchName, BranchAddress, BranchPhone, BranchEmail, RegistrationNumber FROM Branches WHERE BranchID = @BranchID", conn)
                cmd.Parameters.AddWithValue("@BranchID", branchID)
                Using reader = cmd.ExecuteReader()
                    If reader.Read() Then
                        Return New Dictionary(Of String, String) From {
                            {"Name", If(reader("BranchName"), "").ToString()},
                            {"Address", If(reader("BranchAddress"), "").ToString()},
                            {"Phone", If(reader("BranchPhone"), "").ToString()},
                            {"Email", If(reader("BranchEmail"), "").ToString()},
                            {"RegNo", If(reader("RegistrationNumber"), "").ToString()}
                        }
                    End If
                End Using
            End Using
        Catch ex As Exception
        End Try
        Return New Dictionary(Of String, String)
    End Function

    Private Sub PrintReceipt(sender As Object, e As PrintPageEventArgs)
        Dim font As New Font("Courier New", 9, FontStyle.Regular)
        Dim boldFont As New Font("Courier New", 10, FontStyle.Bold)
        Dim titleFont As New Font("Courier New", 12, FontStyle.Bold)
        
        Dim y As Integer = 10
        Dim branchID As Integer = CInt(_receiptData("BranchID"))
        Dim branchInfo = GetBranchInfo(branchID)

        ' Header - Branch Info
        e.Graphics.DrawString("OVEN DELIGHTS", titleFont, Brushes.Black, _leftMargin, y)
        y += 25
        e.Graphics.DrawString("YOUR TRUSTED FAMILY BAKERY", font, Brushes.Black, _leftMargin, y)
        y += 20
        
        If branchInfo.Count > 0 Then
            e.Graphics.DrawString(branchInfo("Name"), boldFont, Brushes.Black, _leftMargin, y)
            y += 18
            e.Graphics.DrawString(branchInfo("Address"), font, Brushes.Black, _leftMargin, y)
            y += 16
            e.Graphics.DrawString($"Tel: {branchInfo("Phone")}", font, Brushes.Black, _leftMargin, y)
            y += 16
            e.Graphics.DrawString($"Email: {branchInfo("Email")}", font, Brushes.Black, _leftMargin, y)
            y += 16
            If Not String.IsNullOrEmpty(branchInfo("RegNo")) Then
                e.Graphics.DrawString($"Co Reg No: {branchInfo("RegNo")}", font, Brushes.Black, _leftMargin, y)
                y += 16
            End If
        End If

        ' Separator
        e.Graphics.DrawString(New String("-"c, 48), font, Brushes.Black, _leftMargin, y)
        y += 20

        If _receiptData("Type").ToString() = "SALE" Then
            ' SALE RECEIPT
            Dim invoiceNo As String = _receiptData("InvoiceNumber").ToString()
            Dim saleDate As DateTime = CDate(_receiptData("Date"))
            Dim cashier As String = _receiptData("Cashier").ToString()
            
            e.Graphics.DrawString($"SALE RECEIPT", boldFont, Brushes.Black, _leftMargin, y)
            y += 20
            e.Graphics.DrawString($"Invoice: {invoiceNo}", font, Brushes.Black, _leftMargin, y)
            y += 16
            e.Graphics.DrawString($"Date: {saleDate:yyyy/MM/dd HH:mm}", font, Brushes.Black, _leftMargin, y)
            y += 16
            e.Graphics.DrawString($"Cashier: {cashier}", font, Brushes.Black, _leftMargin, y)
            y += 20

            ' Items header
            e.Graphics.DrawString("Item                    Qty  Price   Total", font, Brushes.Black, _leftMargin, y)
            y += 16
            e.Graphics.DrawString(New String("-"c, 48), font, Brushes.Black, _leftMargin, y)
            y += 18

            ' Items
            Dim items As DataTable = CType(_receiptData("Items"), DataTable)
            For Each row As DataRow In items.Rows
                Dim itemName As String = row("Product").ToString()
                If itemName.Length > 20 Then itemName = itemName.Substring(0, 20)
                
                Dim qty As String = CDec(row("Qty")).ToString("0.00")
                Dim price As String = CDec(row("Price")).ToString("0.00")
                Dim lineTotal As String = CDec(row("Total")).ToString("0.00")
                
                Dim line As String = $"{itemName,-20} {qty,4} {price,6} {lineTotal,7}"
                e.Graphics.DrawString(line, font, Brushes.Black, _leftMargin, y)
                y += 16
            Next

            ' Total
            y += 10
            e.Graphics.DrawString(New String("-"c, 48), font, Brushes.Black, _leftMargin, y)
            y += 18
            Dim total As Decimal = CDec(_receiptData("Total"))
            e.Graphics.DrawString($"TOTAL:                          R {total:N2}", boldFont, Brushes.Black, _leftMargin, y)
            y += 20
            e.Graphics.DrawString($"Payment: {_receiptData("PaymentMethod")}", font, Brushes.Black, _leftMargin, y)
            y += 20

        Else
            ' CUSTOM ORDER RECEIPT
            Dim orderNo As String = _receiptData("OrderNumber").ToString()
            Dim orderDate As DateTime = CDate(_receiptData("Date"))
            
            e.Graphics.DrawString($"CUSTOM ORDER RECEIPT", boldFont, Brushes.Black, _leftMargin, y)
            y += 20
            e.Graphics.DrawString($"Order No: {orderNo}", font, Brushes.Black, _leftMargin, y)
            y += 16
            e.Graphics.DrawString($"Date: {orderDate:yyyy/MM/dd HH:mm}", font, Brushes.Black, _leftMargin, y)
            y += 16
            e.Graphics.DrawString($"Cashier: {_receiptData("Cashier")}", font, Brushes.Black, _leftMargin, y)
            y += 20

            ' Customer Info
            e.Graphics.DrawString("CUSTOMER DETAILS:", boldFont, Brushes.Black, _leftMargin, y)
            y += 16
            e.Graphics.DrawString($"Name: {_receiptData("CustomerName")} {_receiptData("CustomerSurname")}", font, Brushes.Black, _leftMargin, y)
            y += 16
            e.Graphics.DrawString($"Phone: {_receiptData("CustomerPhone")}", font, Brushes.Black, _leftMargin, y)
            y += 20

            ' Collection Details
            e.Graphics.DrawString("READY FOR COLLECTION:", boldFont, Brushes.Black, _leftMargin, y)
            y += 16
            Dim readyDate As DateTime = CDate(_receiptData("ReadyDate"))
            Dim readyTime As TimeSpan = CType(_receiptData("ReadyTime"), TimeSpan)
            e.Graphics.DrawString($"Date: {readyDate:dd/MM/yyyy}", font, Brushes.Black, _leftMargin, y)
            y += 16
            e.Graphics.DrawString($"Time: {readyTime:hh\:mm}", font, Brushes.Black, _leftMargin, y)
            y += 16
            e.Graphics.DrawString($"*** {_receiptData("CollectionDay").ToString().ToUpper()} ***", boldFont, Brushes.Black, _leftMargin, y)
            y += 20

            ' Colour and Picture
            If _receiptData.ContainsKey("Colour") AndAlso Not String.IsNullOrWhiteSpace(_receiptData("Colour").ToString()) Then
                e.Graphics.DrawString($"Colour: {_receiptData("Colour")}", font, Brushes.Black, _leftMargin, y)
                y += 16
            End If
            If _receiptData.ContainsKey("Picture") AndAlso Not String.IsNullOrWhiteSpace(_receiptData("Picture").ToString()) Then
                e.Graphics.DrawString($"Picture/Design: {_receiptData("Picture")}", font, Brushes.Black, _leftMargin, y)
                y += 16
            End If
            If (_receiptData.ContainsKey("Colour") AndAlso Not String.IsNullOrWhiteSpace(_receiptData("Colour").ToString())) OrElse
               (_receiptData.ContainsKey("Picture") AndAlso Not String.IsNullOrWhiteSpace(_receiptData("Picture").ToString())) Then
                y += 4 ' Extra space after colour/picture
            End If

            ' Special Instructions
            If _receiptData.ContainsKey("SpecialInstructions") AndAlso Not String.IsNullOrWhiteSpace(_receiptData("SpecialInstructions").ToString()) Then
                e.Graphics.DrawString("SPECIAL INSTRUCTIONS:", boldFont, Brushes.Black, _leftMargin, y)
                y += 16
                e.Graphics.DrawString(_receiptData("SpecialInstructions").ToString(), font, Brushes.Black, _leftMargin, y)
                y += 20
            End If

            ' Order Items
            e.Graphics.DrawString(New String("-"c, 48), font, Brushes.Black, _leftMargin, y)
            y += 18
            e.Graphics.DrawString("ORDER ITEMS:", boldFont, Brushes.Black, _leftMargin, y)
            y += 16
            e.Graphics.DrawString(New String("-"c, 48), font, Brushes.Black, _leftMargin, y)
            y += 18
            
            Dim items As DataTable = CType(_receiptData("Items"), DataTable)
            For Each row As DataRow In items.Rows
                Dim itemName As String = row("Product").ToString()
                If itemName.Length > 20 Then itemName = itemName.Substring(0, 20)
                
                Dim qty As String = CDec(row("Qty")).ToString("0.00")
                Dim price As String = CDec(row("Price")).ToString("0.00")
                Dim lineTotal As String = CDec(row("Total")).ToString("0.00")
                
                Dim line As String = $"{itemName,-20} {qty,4} {price,6} {lineTotal,7}"
                e.Graphics.DrawString(line, font, Brushes.Black, _leftMargin, y)
                y += 16
            Next

            ' Totals
            y += 10
            e.Graphics.DrawString(New String("-"c, 48), font, Brushes.Black, _leftMargin, y)
            y += 18
            Dim totalAmount As Decimal = CDec(_receiptData("TotalAmount"))
            Dim depositPaid As Decimal = CDec(_receiptData("DepositPaid"))
            Dim balanceOwing As Decimal = totalAmount - depositPaid
            
            e.Graphics.DrawString($"Total Amount:               R {totalAmount:N2}", boldFont, Brushes.Black, _leftMargin, y)
            y += 16
            e.Graphics.DrawString($"Deposit Paid:               R {depositPaid:N2}", font, Brushes.Black, _leftMargin, y)
            y += 16
            e.Graphics.DrawString($"Balance Due:                R {balanceOwing:N2}", boldFont, Brushes.Black, _leftMargin, y)
            y += 20
            
            ' Important notice
            e.Graphics.DrawString(New String("-"c, 48), font, Brushes.Black, _leftMargin, y)
            y += 18
            e.Graphics.DrawString("PLEASE BRING THIS RECEIPT WHEN", boldFont, Brushes.Black, _leftMargin, y)
            y += 16
            e.Graphics.DrawString("COLLECTING YOUR ORDER", boldFont, Brushes.Black, _leftMargin, y)
            y += 20
        End If

        ' Footer
        e.Graphics.DrawString(New String("-"c, 48), font, Brushes.Black, _leftMargin, y)
        y += 18
        e.Graphics.DrawString("Thank you for your business!", font, Brushes.Black, _leftMargin, y)
        y += 16
        e.Graphics.DrawString("Please visit us again!", font, Brushes.Black, _leftMargin, y)

        e.HasMorePages = False
    End Sub
End Class
