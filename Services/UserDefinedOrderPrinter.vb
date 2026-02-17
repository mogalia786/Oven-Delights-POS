Imports System.Configuration
Imports System.Data
Imports System.Data.SqlClient
Imports System.Drawing
Imports System.Drawing.Printing

Public Class UserDefinedOrderPrinter
    Private _connectionString As String
    Private _branchID As Integer
    Private _branchName As String
    Private _branchAddress As String
    Private _branchPhone As String
    Private _vatRegistrationNumber As String

    Public Sub New(connectionString As String, branchID As Integer)
        _connectionString = connectionString
        _branchID = branchID
        LoadBranchDetails()
    End Sub

    Private Sub LoadBranchDetails()
        Try
            Using conn As New SqlConnection(_connectionString)
                conn.Open()
                Dim sql = "SELECT BranchName, Address, Phone, RegistrationNum FROM Branches WHERE BranchID = @BranchID"
                Using cmd As New SqlCommand(sql, conn)
                    cmd.Parameters.AddWithValue("@BranchID", _branchID)
                    Using reader = cmd.ExecuteReader()
                        If reader.Read() Then
                            _branchName = If(IsDBNull(reader("BranchName")), "Oven Delights", reader("BranchName").ToString())
                            _branchAddress = If(IsDBNull(reader("Address")), "", reader("Address").ToString())
                            _branchPhone = If(IsDBNull(reader("Phone")), "", reader("Phone").ToString())
                            _vatRegistrationNumber = If(IsDBNull(reader("RegistrationNum")), "", reader("RegistrationNum").ToString())
                        End If
                    End Using
                End Using
            End Using
        Catch ex As Exception
            _branchName = "Oven Delights"
            _branchAddress = ""
            _branchPhone = ""
            _vatRegistrationNumber = ""
        End Try
    End Sub

    Public Sub PrintCreationSlip(orderNumber As String, orderData As UserDefinedOrderData, cartItems As DataTable, totalAmount As Decimal, paymentMethod As String, cashAmount As Decimal, cardAmount As Decimal, cashierName As String)
        ' Print twice: Customer Copy + Business Copy
        PrintSlip(orderNumber, orderData, cartItems, totalAmount, paymentMethod, cashAmount, cardAmount, cashierName, "CUSTOMER COPY")
        PrintSlip(orderNumber, orderData, cartItems, totalAmount, paymentMethod, cashAmount, cardAmount, cashierName, "BUSINESS COPY")
    End Sub

    Private Sub PrintSlip(orderNumber As String, orderData As UserDefinedOrderData, cartItems As DataTable, totalAmount As Decimal, paymentMethod As String, cashAmount As Decimal, cardAmount As Decimal, cashierName As String, copyType As String)
        Try
            Dim printDoc As New PrintDocument()
            
            AddHandler printDoc.PrintPage, Sub(sender, e)
                ' ALL FONTS BOLD FOR BETTER VISIBILITY
                Dim fontBold As New Font("Courier New", 8, FontStyle.Bold)
                Dim fontLarge As New Font("Courier New", 11, FontStyle.Bold)
                Dim yPos As Single = 5
                Dim leftMargin As Single = 5
                
                ' Store header - centered
                Dim headerText = "OVEN DELIGHTS"
                Dim headerSize = e.Graphics.MeasureString(headerText, fontLarge)
                e.Graphics.DrawString(headerText, fontLarge, Brushes.Black, (302 - headerSize.Width) / 2, yPos)
                yPos += 22
                
                ' Branch info - PICKUP LOCATION
                Dim pickupText = "*** PICKUP LOCATION ***"
                Dim pickupSize = e.Graphics.MeasureString(pickupText, fontLarge)
                e.Graphics.DrawString(pickupText, fontLarge, Brushes.Black, (302 - pickupSize.Width) / 2, yPos)
                yPos += 22
                
                e.Graphics.DrawString(_branchName, fontBold, Brushes.Black, leftMargin, yPos)
                yPos += 15
                
                If Not String.IsNullOrEmpty(_branchAddress) Then
                    ' Split address into multiple lines if needed
                    Dim addressLines = _branchAddress.Split(New String() {", ", vbCrLf, vbLf}, StringSplitOptions.RemoveEmptyEntries)
                    For Each line In addressLines
                        e.Graphics.DrawString(line.Trim(), fontBold, Brushes.Black, leftMargin, yPos)
                        yPos += 15
                    Next
                Else
                    e.Graphics.DrawString("Address not available", fontBold, Brushes.Black, leftMargin, yPos)
                    yPos += 15
                End If
                
                If Not String.IsNullOrEmpty(_branchPhone) Then
                    e.Graphics.DrawString($"Tel: {_branchPhone}", fontBold, Brushes.Black, leftMargin, yPos)
                    yPos += 15
                Else
                    e.Graphics.DrawString("Phone not available", fontBold, Brushes.Black, leftMargin, yPos)
                    yPos += 15
                End If
                
                If Not String.IsNullOrEmpty(_vatRegistrationNumber) Then
                    e.Graphics.DrawString($"VAT Reg: {_vatRegistrationNumber}", fontBold, Brushes.Black, leftMargin, yPos)
                    yPos += 15
                Else
                    e.Graphics.DrawString("VAT Reg not available", fontBold, Brushes.Black, leftMargin, yPos)
                    yPos += 15
                End If
                
                Dim collectText = "*** COLLECT FROM THIS BRANCH ***"
                Dim collectSize = e.Graphics.MeasureString(collectText, fontLarge)
                e.Graphics.DrawString(collectText, fontLarge, Brushes.Black, (302 - collectSize.Width) / 2, yPos)
                yPos += 22
                
                e.Graphics.DrawString("======================================", fontBold, Brushes.Black, leftMargin, yPos)
                yPos += 15
                
                ' Copy type
                Dim copyText = copyType
                Dim copySize = e.Graphics.MeasureString(copyText, fontLarge)
                e.Graphics.DrawString(copyText, fontLarge, Brushes.Black, (302 - copySize.Width) / 2, yPos)
                yPos += 22
                
                ' Title
                Dim titleText = "USER DEFINED ORDER"
                Dim titleSize = e.Graphics.MeasureString(titleText, fontLarge)
                e.Graphics.DrawString(titleText, fontLarge, Brushes.Black, (302 - titleSize.Width) / 2, yPos)
                yPos += 22
                
                e.Graphics.DrawString("======================================", fontBold, Brushes.Black, leftMargin, yPos)
                yPos += 15
                
                ' Order details
                e.Graphics.DrawString($"Order #: {orderNumber}", fontBold, Brushes.Black, leftMargin, yPos)
                yPos += 15
                e.Graphics.DrawString($"Ordered Date: {DateTime.Now:dd/MM/yyyy}", fontBold, Brushes.Black, leftMargin, yPos)
                yPos += 15
                e.Graphics.DrawString($"Ordered Time: {DateTime.Now:HH:mm:ss}", fontBold, Brushes.Black, leftMargin, yPos)
                yPos += 15
                e.Graphics.DrawString($"Cashier: {cashierName}", fontBold, Brushes.Black, leftMargin, yPos)
                yPos += 18
                
                ' Customer info
                e.Graphics.DrawString("CUSTOMER:", fontBold, Brushes.Black, leftMargin, yPos)
                yPos += 15
                Dim customerFullName = $"{orderData.CustomerName} {orderData.CustomerSurname}".Trim()
                e.Graphics.DrawString(customerFullName, fontBold, Brushes.Black, leftMargin, yPos)
                yPos += 15
                e.Graphics.DrawString($"Phone: {orderData.CustomerCellNumber}", fontBold, Brushes.Black, leftMargin, yPos)
                yPos += 18
                
                ' Collection details
                e.Graphics.DrawString("COLLECTION DETAILS:", fontBold, Brushes.Black, leftMargin, yPos)
                yPos += 15
                e.Graphics.DrawString($"Date: {orderData.CollectionDate:dd/MM/yyyy}", fontBold, Brushes.Black, leftMargin, yPos)
                yPos += 15
                e.Graphics.DrawString($"Time: {orderData.CollectionTime:hh\:mm}", fontBold, Brushes.Black, leftMargin, yPos)
                yPos += 15
                e.Graphics.DrawString($"Day: {orderData.CollectionDay}", fontBold, Brushes.Black, leftMargin, yPos)
                yPos += 18
                
                ' Order details
                e.Graphics.DrawString("ORDER DETAILS:", fontBold, Brushes.Black, leftMargin, yPos)
                yPos += 15
                
                If Not String.IsNullOrEmpty(orderData.CakeColour) Then
                    e.Graphics.DrawString($"Cake Colour: {orderData.CakeColour}", fontBold, Brushes.Black, leftMargin, yPos)
                    yPos += 15
                End If
                
                If Not String.IsNullOrEmpty(orderData.CakeImage) Then
                    e.Graphics.DrawString($"Cake Picture: {orderData.CakeImage}", fontBold, Brushes.Black, leftMargin, yPos)
                    yPos += 15
                End If
                
                If Not String.IsNullOrEmpty(orderData.SpecialRequest) Then
                    e.Graphics.DrawString($"Special Request: {orderData.SpecialRequest}", fontBold, Brushes.Black, leftMargin, yPos)
                    yPos += 15
                End If
                
                yPos += 5
                e.Graphics.DrawString("======================================", fontBold, Brushes.Black, leftMargin, yPos)
                yPos += 15
                
                ' Items header
                e.Graphics.DrawString("ITEMS:", fontBold, Brushes.Black, leftMargin, yPos)
                yPos += 15
                e.Graphics.DrawString("Item              Qty  Price  Total", fontBold, Brushes.Black, leftMargin, yPos)
                yPos += 15
                
                ' Line items
                Dim subtotal As Decimal = 0
                For Each row As DataRow In cartItems.Rows
                    Dim itemName = row("Product").ToString()
                    If itemName.Length > 17 Then itemName = itemName.Substring(0, 14) & "..."
                    
                    Dim qty = CDec(row("Qty"))
                    Dim price = CDec(row("Price"))
                    Dim lineTotal = CDec(row("Total"))
                    subtotal += lineTotal
                    
                    Dim line = String.Format("{0,-17} {1,3} {2,5:N2} {3,6:N2}", itemName, qty, price, lineTotal)
                    e.Graphics.DrawString(line, fontBold, Brushes.Black, leftMargin, yPos)
                    yPos += 14
                Next
                
                ' Separator
                e.Graphics.DrawString("--------------------------------------", fontBold, Brushes.Black, leftMargin, yPos)
                yPos += 15
                
                ' Totals
                Dim taxAmount = totalAmount - subtotal
                e.Graphics.DrawString($"Subtotal (excl VAT):      R {subtotal:N2}", fontBold, Brushes.Black, leftMargin, yPos)
                yPos += 14
                e.Graphics.DrawString($"VAT (15%):                R {taxAmount:N2}", fontBold, Brushes.Black, leftMargin, yPos)
                yPos += 14
                e.Graphics.DrawString($"TOTAL:                    R {totalAmount:N2}", fontBold, Brushes.Black, leftMargin, yPos)
                yPos += 18
                
                ' Payment info
                e.Graphics.DrawString($"Payment: {paymentMethod}", fontBold, Brushes.Black, leftMargin, yPos)
                yPos += 14
                
                If paymentMethod = "CASH" Then
                    e.Graphics.DrawString($"Amount Paid:              R {cashAmount:N2}", fontBold, Brushes.Black, leftMargin, yPos)
                    yPos += 14
                ElseIf paymentMethod = "CARD" Then
                    e.Graphics.DrawString($"Amount Paid:              R {cardAmount:N2}", fontBold, Brushes.Black, leftMargin, yPos)
                    yPos += 14
                ElseIf paymentMethod = "SPLIT" Then
                    e.Graphics.DrawString($"Cash Amount:              R {cashAmount:N2}", fontBold, Brushes.Black, leftMargin, yPos)
                    yPos += 14
                    e.Graphics.DrawString($"Card Amount:              R {cardAmount:N2}", fontBold, Brushes.Black, leftMargin, yPos)
                    yPos += 14
                End If
                
                e.Graphics.DrawString("PAID IN FULL", fontBold, Brushes.Black, leftMargin, yPos)
                yPos += 18
                
                e.Graphics.DrawString("======================================", fontBold, Brushes.Black, leftMargin, yPos)
                yPos += 15
                
                ' Barcode
                Try
                    Dim barcodeImage = BarcodeGenerator.GenerateCode39Barcode(orderNumber, 180, 60)
                    e.Graphics.DrawImage(barcodeImage, CInt((302 - 180) / 2), CInt(yPos))
                    yPos += 65
                    barcodeImage.Dispose()
                Catch ex As Exception
                    Dim invNumFont As New Font("Arial", 20, FontStyle.Bold)
                    Dim invNumSize = e.Graphics.MeasureString(orderNumber, invNumFont)
                    e.Graphics.DrawString(orderNumber, invNumFont, Brushes.Black, (302 - invNumSize.Width) / 2, yPos)
                    yPos += 28
                End Try
                
                e.Graphics.DrawString("======================================", fontBold, Brushes.Black, leftMargin, yPos)
                yPos += 15
                
                ' Footer
                Dim footer1 = "SCAN BARCODE FOR COLLECTION"
                Dim footer1Size = e.Graphics.MeasureString(footer1, fontBold)
                e.Graphics.DrawString(footer1, fontBold, Brushes.Black, (302 - footer1Size.Width) / 2, yPos)
                yPos += 14
                
                Dim footer2 = "Thank you!"
                Dim footer2Size = e.Graphics.MeasureString(footer2, fontBold)
                e.Graphics.DrawString(footer2, fontBold, Brushes.Black, (302 - footer2Size.Width) / 2, yPos)
            End Sub
            
            printDoc.Print()
            
        Catch ex As Exception
            Console.WriteLine($"Print error: {ex.Message}")
        End Try
    End Sub

    Public Sub PrintPickupSlip(orderNumber As String, orderData As UserDefinedOrderData, cartItems As DataTable, totalAmount As Decimal, cashierName As String)
        ' Print twice: Customer Copy + Business Copy
        PrintPickup(orderNumber, orderData, cartItems, totalAmount, cashierName, "CUSTOMER COPY")
        PrintPickup(orderNumber, orderData, cartItems, totalAmount, cashierName, "BUSINESS COPY")
    End Sub

    Private Sub PrintPickup(orderNumber As String, orderData As UserDefinedOrderData, cartItems As DataTable, totalAmount As Decimal, cashierName As String, copyType As String)
        Try
            Dim printDoc As New PrintDocument()
            
            AddHandler printDoc.PrintPage, Sub(sender, e)
                ' ALL FONTS BOLD
                Dim fontBold As New Font("Courier New", 8, FontStyle.Bold)
                Dim fontLarge As New Font("Courier New", 11, FontStyle.Bold)
                Dim yPos As Single = 5
                Dim leftMargin As Single = 5
                
                ' Store header
                Dim headerText = "OVEN DELIGHTS"
                Dim headerSize = e.Graphics.MeasureString(headerText, fontLarge)
                e.Graphics.DrawString(headerText, fontLarge, Brushes.Black, (302 - headerSize.Width) / 2, yPos)
                yPos += 22
                
                ' Branch info - PICKUP LOCATION
                Dim pickupText = "*** PICKUP LOCATION ***"
                Dim pickupSize = e.Graphics.MeasureString(pickupText, fontLarge)
                e.Graphics.DrawString(pickupText, fontLarge, Brushes.Black, (302 - pickupSize.Width) / 2, yPos)
                yPos += 22
                
                e.Graphics.DrawString(_branchName, fontBold, Brushes.Black, leftMargin, yPos)
                yPos += 15
                
                If Not String.IsNullOrEmpty(_branchAddress) Then
                    ' Split address into multiple lines if needed
                    Dim addressLines = _branchAddress.Split(New String() {", ", vbCrLf, vbLf}, StringSplitOptions.RemoveEmptyEntries)
                    For Each line In addressLines
                        e.Graphics.DrawString(line.Trim(), fontBold, Brushes.Black, leftMargin, yPos)
                        yPos += 15
                    Next
                Else
                    e.Graphics.DrawString("Address not available", fontBold, Brushes.Black, leftMargin, yPos)
                    yPos += 15
                End If
                
                If Not String.IsNullOrEmpty(_branchPhone) Then
                    e.Graphics.DrawString($"Tel: {_branchPhone}", fontBold, Brushes.Black, leftMargin, yPos)
                    yPos += 15
                Else
                    e.Graphics.DrawString("Phone not available", fontBold, Brushes.Black, leftMargin, yPos)
                    yPos += 15
                End If
                
                If Not String.IsNullOrEmpty(_vatRegistrationNumber) Then
                    e.Graphics.DrawString($"VAT Reg: {_vatRegistrationNumber}", fontBold, Brushes.Black, leftMargin, yPos)
                    yPos += 15
                Else
                    e.Graphics.DrawString("VAT Reg not available", fontBold, Brushes.Black, leftMargin, yPos)
                    yPos += 15
                End If
                
                Dim collectText = "*** COLLECTED FROM THIS BRANCH ***"
                Dim collectSize = e.Graphics.MeasureString(collectText, fontLarge)
                e.Graphics.DrawString(collectText, fontLarge, Brushes.Black, (302 - collectSize.Width) / 2, yPos)
                yPos += 22
                
                e.Graphics.DrawString("======================================", fontBold, Brushes.Black, leftMargin, yPos)
                yPos += 15
                
                ' Copy type
                Dim copyText = copyType
                Dim copySize = e.Graphics.MeasureString(copyText, fontLarge)
                e.Graphics.DrawString(copyText, fontLarge, Brushes.Black, (302 - copySize.Width) / 2, yPos)
                yPos += 22
                
                ' Title
                Dim titleText = "USER DEFINED ORDER - PICKED UP"
                Dim titleSize = e.Graphics.MeasureString(titleText, fontLarge)
                e.Graphics.DrawString(titleText, fontLarge, Brushes.Black, (302 - titleSize.Width) / 2, yPos)
                yPos += 22
                
                e.Graphics.DrawString("======================================", fontBold, Brushes.Black, leftMargin, yPos)
                yPos += 15
                
                ' Order details
                e.Graphics.DrawString($"Order #: {orderNumber}", fontBold, Brushes.Black, leftMargin, yPos)
                yPos += 15
                e.Graphics.DrawString($"Ordered Date: {DateTime.Now:dd/MM/yyyy}", fontBold, Brushes.Black, leftMargin, yPos)
                yPos += 15
                e.Graphics.DrawString($"Ordered Time: {DateTime.Now:HH:mm:ss}", fontBold, Brushes.Black, leftMargin, yPos)
                yPos += 15
                e.Graphics.DrawString($"Collected By: {cashierName}", fontBold, Brushes.Black, leftMargin, yPos)
                yPos += 18
                
                ' Customer info
                e.Graphics.DrawString("CUSTOMER:", fontBold, Brushes.Black, leftMargin, yPos)
                yPos += 15
                Dim customerFullName = $"{orderData.CustomerName} {orderData.CustomerSurname}".Trim()
                e.Graphics.DrawString(customerFullName, fontBold, Brushes.Black, leftMargin, yPos)
                yPos += 15
                e.Graphics.DrawString($"Phone: {orderData.CustomerCellNumber}", fontBold, Brushes.Black, leftMargin, yPos)
                yPos += 18
                
                ' Original order info
                e.Graphics.DrawString($"ORIGINAL ORDER DATE: {DateTime.Now:dd/MM/yyyy}", fontBold, Brushes.Black, leftMargin, yPos)
                yPos += 15
                e.Graphics.DrawString($"COLLECTION DATE: {orderData.CollectionDate:dd/MM/yyyy}", fontBold, Brushes.Black, leftMargin, yPos)
                yPos += 15
                e.Graphics.DrawString($"COLLECTION TIME: {orderData.CollectionTime:hh\:mm}", fontBold, Brushes.Black, leftMargin, yPos)
                yPos += 18
                
                ' Order details
                e.Graphics.DrawString("ORDER DETAILS:", fontBold, Brushes.Black, leftMargin, yPos)
                yPos += 15
                
                If Not String.IsNullOrEmpty(orderData.CakeColour) Then
                    e.Graphics.DrawString($"Cake Colour: {orderData.CakeColour}", fontBold, Brushes.Black, leftMargin, yPos)
                    yPos += 15
                End If
                
                If Not String.IsNullOrEmpty(orderData.CakeImage) Then
                    e.Graphics.DrawString($"Cake Picture: {orderData.CakeImage}", fontBold, Brushes.Black, leftMargin, yPos)
                    yPos += 15
                End If
                
                If Not String.IsNullOrEmpty(orderData.SpecialRequest) Then
                    e.Graphics.DrawString($"Special Request: {orderData.SpecialRequest}", fontBold, Brushes.Black, leftMargin, yPos)
                    yPos += 15
                End If
                
                yPos += 5
                e.Graphics.DrawString("======================================", fontBold, Brushes.Black, leftMargin, yPos)
                yPos += 15
                
                ' Items
                e.Graphics.DrawString("ITEMS:", fontBold, Brushes.Black, leftMargin, yPos)
                yPos += 15
                e.Graphics.DrawString("Item              Qty  Price  Total", fontBold, Brushes.Black, leftMargin, yPos)
                yPos += 15
                
                For Each row As DataRow In cartItems.Rows
                    Dim itemName = row("Product").ToString()
                    If itemName.Length > 17 Then itemName = itemName.Substring(0, 14) & "..."
                    
                    Dim qty = CDec(row("Qty"))
                    Dim price = CDec(row("Price"))
                    Dim lineTotal = CDec(row("Total"))
                    
                    Dim line = String.Format("{0,-17} {1,3} {2,5:N2} {3,6:N2}", itemName, qty, price, lineTotal)
                    e.Graphics.DrawString(line, fontBold, Brushes.Black, leftMargin, yPos)
                    yPos += 14
                Next
                
                e.Graphics.DrawString("--------------------------------------", fontBold, Brushes.Black, leftMargin, yPos)
                yPos += 15
                
                e.Graphics.DrawString($"TOTAL PAID:               R {totalAmount:N2}", fontBold, Brushes.Black, leftMargin, yPos)
                yPos += 18
                
                e.Graphics.DrawString("======================================", fontBold, Brushes.Black, leftMargin, yPos)
                yPos += 15
                
                ' Barcode
                Try
                    Dim barcodeImage = BarcodeGenerator.GenerateCode39Barcode(orderNumber, 180, 60)
                    e.Graphics.DrawImage(barcodeImage, CInt((302 - 180) / 2), CInt(yPos))
                    yPos += 65
                    barcodeImage.Dispose()
                Catch ex As Exception
                    Dim invNumFont As New Font("Arial", 20, FontStyle.Bold)
                    Dim invNumSize = e.Graphics.MeasureString(orderNumber, invNumFont)
                    e.Graphics.DrawString(orderNumber, invNumFont, Brushes.Black, (302 - invNumSize.Width) / 2, yPos)
                    yPos += 28
                End Try
                
                e.Graphics.DrawString("======================================", fontBold, Brushes.Black, leftMargin, yPos)
                yPos += 15
                
                ' Footer
                Dim footer = "Thank you for your business!"
                Dim footerSize = e.Graphics.MeasureString(footer, fontBold)
                e.Graphics.DrawString(footer, fontBold, Brushes.Black, (302 - footerSize.Width) / 2, yPos)
            End Sub
            
            printDoc.Print()
            
        Catch ex As Exception
            Console.WriteLine($"Print error: {ex.Message}")
        End Try
    End Sub
End Class
