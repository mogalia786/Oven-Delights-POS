Imports System.Data
Imports System.Drawing
Imports System.Drawing.Printing
Imports System.Windows.Forms

Public Class BoxItemsPrinter
    Private _boxBarcode As String
    Private _boxItems As DataTable
    Private _cashierName As String

    Public Sub New(boxBarcode As String, boxItems As DataTable, cashierName As String)
        _boxBarcode = boxBarcode
        _boxItems = boxItems
        _cashierName = cashierName
    End Sub

    Public Sub PrintBoxSlip()
        Try
            Dim printDoc As New PrintDocument()
            
            AddHandler printDoc.PrintPage, Sub(sender, e)
                ' ALL FONTS BOLD
                Dim fontBold As New Font("Courier New", 8, FontStyle.Bold)
                Dim fontLarge As New Font("Courier New", 11, FontStyle.Bold)
                Dim fontBarcode As New Font("Free 3 of 9", 24, FontStyle.Regular)
                Dim yPos As Single = 5
                Dim leftMargin As Single = 5
                
                ' Header
                Dim headerText = "BOX OF ITEMS"
                Dim headerSize = e.Graphics.MeasureString(headerText, fontLarge)
                e.Graphics.DrawString(headerText, fontLarge, Brushes.Black, (302 - headerSize.Width) / 2, yPos)
                yPos += 22
                
                e.Graphics.DrawString("======================================", fontBold, Brushes.Black, leftMargin, yPos)
                yPos += 15
                
                ' Box details
                e.Graphics.DrawString($"Box Barcode: {_boxBarcode}", fontBold, Brushes.Black, leftMargin, yPos)
                yPos += 15
                e.Graphics.DrawString($"Created: {DateTime.Now:dd/MM/yyyy HH:mm:ss}", fontBold, Brushes.Black, leftMargin, yPos)
                yPos += 15
                e.Graphics.DrawString($"Created By: {_cashierName}", fontBold, Brushes.Black, leftMargin, yPos)
                yPos += 18
                
                e.Graphics.DrawString("======================================", fontBold, Brushes.Black, leftMargin, yPos)
                yPos += 15
                
                ' Items header
                e.Graphics.DrawString("ITEMS IN BOX:", fontBold, Brushes.Black, leftMargin, yPos)
                yPos += 15
                e.Graphics.DrawString("--------------------------------------", fontBold, Brushes.Black, leftMargin, yPos)
                yPos += 15
                
                ' Column headers
                e.Graphics.DrawString("QTY  ITEM", fontBold, Brushes.Black, leftMargin, yPos)
                e.Graphics.DrawString("PRICE", fontBold, Brushes.Black, 240, yPos)
                yPos += 15
                e.Graphics.DrawString("--------------------------------------", fontBold, Brushes.Black, leftMargin, yPos)
                yPos += 15
                
                ' Items
                Dim total As Decimal = 0
                For Each row As DataRow In _boxItems.Rows
                    Dim qty = CDec(row("Quantity"))
                    Dim productName = row("ProductName").ToString()
                    Dim price = CDec(row("Price"))
                    Dim itemTotal = CDec(row("Total"))
                    total += itemTotal
                    
                    ' Quantity and product name
                    e.Graphics.DrawString($"{qty:N0}x  {productName}", fontBold, Brushes.Black, leftMargin, yPos)
                    yPos += 15
                    
                    ' Price on right
                    e.Graphics.DrawString($"{itemTotal:C2}", fontBold, Brushes.Black, 240, yPos)
                    yPos += 15
                Next
                
                e.Graphics.DrawString("--------------------------------------", fontBold, Brushes.Black, leftMargin, yPos)
                yPos += 15
                
                ' Total
                e.Graphics.DrawString("TOTAL:", fontBold, Brushes.Black, leftMargin, yPos)
                e.Graphics.DrawString($"{total:C2}", fontBold, Brushes.Black, 240, yPos)
                yPos += 18
                
                e.Graphics.DrawString("======================================", fontBold, Brushes.Black, leftMargin, yPos)
                yPos += 15
                
                ' Barcode section
                Dim barcodeText = "SCAN THIS BARCODE AT TILL:"
                Dim barcodeSize = e.Graphics.MeasureString(barcodeText, fontBold)
                e.Graphics.DrawString(barcodeText, fontBold, Brushes.Black, (302 - barcodeSize.Width) / 2, yPos)
                yPos += 18
                
                ' Barcode (centered)
                Dim barcodeDisplay = $"*{_boxBarcode}*"
                Dim barcodeDisplaySize = e.Graphics.MeasureString(barcodeDisplay, fontBarcode)
                e.Graphics.DrawString(barcodeDisplay, fontBarcode, Brushes.Black, (302 - barcodeDisplaySize.Width) / 2, yPos)
                yPos += 35
                
                ' Barcode text below
                Dim barcodeTextSize = e.Graphics.MeasureString(_boxBarcode, fontBold)
                e.Graphics.DrawString(_boxBarcode, fontBold, Brushes.Black, (302 - barcodeTextSize.Width) / 2, yPos)
                yPos += 18
                
                e.Graphics.DrawString("======================================", fontBold, Brushes.Black, leftMargin, yPos)
                yPos += 15
                
                ' Instructions
                e.Graphics.DrawString("HAND THIS SLIP TO CASHIER", fontBold, Brushes.Black, leftMargin, yPos)
                yPos += 15
                e.Graphics.DrawString("Customer will receive sales receipt", fontBold, Brushes.Black, leftMargin, yPos)
                yPos += 15
            End Sub
            
            printDoc.Print()
            
        Catch ex As Exception
            MessageBox.Show($"Error printing box slip: {ex.Message}", "Print Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    Public Sub PrintBarcodeSticker()
        Try
            Dim printDoc As New PrintDocument()
            
            AddHandler printDoc.PrintPage, Sub(sender, e)
                ' Barcode font
                Dim fontBarcode As New Font("Free 3 of 9", 36, FontStyle.Regular)
                Dim fontBold As New Font("Courier New", 10, FontStyle.Bold)
                Dim yPos As Single = 20
                Dim leftMargin As Single = 5
                
                ' Title
                Dim titleText = "BOX BARCODE"
                Dim titleSize = e.Graphics.MeasureString(titleText, fontBold)
                e.Graphics.DrawString(titleText, fontBold, Brushes.Black, (302 - titleSize.Width) / 2, yPos)
                yPos += 25
                
                ' Barcode (centered and large)
                Dim barcodeDisplay = $"*{_boxBarcode}*"
                Dim barcodeDisplaySize = e.Graphics.MeasureString(barcodeDisplay, fontBarcode)
                e.Graphics.DrawString(barcodeDisplay, fontBarcode, Brushes.Black, (302 - barcodeDisplaySize.Width) / 2, yPos)
                yPos += 50
                
                ' Barcode text below (centered)
                Dim barcodeTextSize = e.Graphics.MeasureString(_boxBarcode, fontBold)
                e.Graphics.DrawString(_boxBarcode, fontBold, Brushes.Black, (302 - barcodeTextSize.Width) / 2, yPos)
                yPos += 25
                
                ' Instructions
                Dim instructText = "SEAL BOX WITH THIS STICKER"
                Dim instructSize = e.Graphics.MeasureString(instructText, fontBold)
                e.Graphics.DrawString(instructText, fontBold, Brushes.Black, (302 - instructSize.Width) / 2, yPos)
            End Sub
            
            printDoc.Print()
            
        Catch ex As Exception
            MessageBox.Show($"Error printing barcode sticker: {ex.Message}", "Print Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub
End Class
