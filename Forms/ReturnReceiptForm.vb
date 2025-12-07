Imports System.Drawing
Imports System.Windows.Forms

' Shared ReturnItem class for both forms
Public Class ReturnItem
    Public Property ProductID As Integer
    Public Property ItemCode As String
    Public Property ProductName As String
    Public Property QtyReturned As Decimal
    Public Property UnitPrice As Decimal
    Public Property LineTotal As Decimal
End Class

Public Class ReturnReceiptForm
    Inherits Form

    Private _returnNumber As String
    Private _returnDate As DateTime
    Private _customerName As String
    Private _invoiceNumber As String
    Private _returnItems As List(Of ReturnItem)
    Private _totalReturn As Decimal
    Private _totalTax As Decimal
    Private _reason As String

    Public Sub New(returnNumber As String, returnDate As DateTime, customerName As String, invoiceNumber As String, returnItems As List(Of ReturnItem), totalReturn As Decimal, totalTax As Decimal, reason As String)
        MyBase.New()
        _returnNumber = returnNumber
        _returnDate = returnDate
        _customerName = customerName
        _invoiceNumber = invoiceNumber
        _returnItems = returnItems
        _totalReturn = totalReturn
        _totalTax = totalTax
        _reason = reason
        InitializeComponent()
        PopulateReceipt()
    End Sub

    Private Sub InitializeComponent()
        Me.Text = "Return Receipt"
        Me.Size = New Size(500, 700)
        Me.StartPosition = FormStartPosition.CenterScreen
        Me.BackColor = Color.White
        Me.FormBorderStyle = FormBorderStyle.FixedDialog
        Me.MaximizeBox = False
        Me.MinimizeBox = False
    End Sub

    Private Sub PopulateReceipt()
        Dim pnlMain As New Panel With {
            .Dock = DockStyle.Fill,
            .AutoScroll = True,
            .Padding = New Padding(20)
        }

        Dim yPos As Integer = 10

        ' Company Header
        Dim lblCompany As New Label With {
            .Text = "OVEN DELIGHTS",
            .Font = New Font("Segoe UI", 20, FontStyle.Bold),
            .ForeColor = ColorTranslator.FromHtml("#C84B31"),
            .AutoSize = True,
            .Location = New Point(20, yPos)
        }
        pnlMain.Controls.Add(lblCompany)
        yPos += 40

        Dim lblTitle As New Label With {
            .Text = "RETURN RECEIPT",
            .Font = New Font("Segoe UI", 16, FontStyle.Bold),
            .ForeColor = Color.Black,
            .AutoSize = True,
            .Location = New Point(20, yPos)
        }
        pnlMain.Controls.Add(lblTitle)
        yPos += 40

        ' Return Details
        AddLabel(pnlMain, $"Return Number: {_returnNumber}", yPos, True)
        yPos += 25
        AddLabel(pnlMain, $"Date: {_returnDate:dd/MM/yyyy HH:mm}", yPos)
        yPos += 25
        AddLabel(pnlMain, $"Original Invoice: {_invoiceNumber}", yPos)
        yPos += 25
        AddLabel(pnlMain, $"Customer: {_customerName}", yPos)
        yPos += 25
        AddLabel(pnlMain, $"Reason: {_reason}", yPos)
        yPos += 35

        ' Separator
        Dim line1 As New Panel With {
            .Height = 2,
            .Width = 440,
            .BackColor = Color.Black,
            .Location = New Point(20, yPos)
        }
        pnlMain.Controls.Add(line1)
        yPos += 15

        ' Items Header
        AddLabel(pnlMain, "RETURNED ITEMS", yPos, True)
        yPos += 30

        ' Column Headers
        Dim lblItemHeader As New Label With {
            .Text = "Item",
            .Font = New Font("Segoe UI", 10, FontStyle.Bold),
            .Location = New Point(20, yPos),
            .Width = 200
        }
        pnlMain.Controls.Add(lblItemHeader)

        Dim lblQtyHeader As New Label With {
            .Text = "Qty",
            .Font = New Font("Segoe UI", 10, FontStyle.Bold),
            .Location = New Point(230, yPos),
            .Width = 50,
            .TextAlign = ContentAlignment.MiddleRight
        }
        pnlMain.Controls.Add(lblQtyHeader)

        Dim lblPriceHeader As New Label With {
            .Text = "Price",
            .Font = New Font("Segoe UI", 10, FontStyle.Bold),
            .Location = New Point(290, yPos),
            .Width = 70,
            .TextAlign = ContentAlignment.MiddleRight
        }
        pnlMain.Controls.Add(lblPriceHeader)

        Dim lblTotalHeader As New Label With {
            .Text = "Total",
            .Font = New Font("Segoe UI", 10, FontStyle.Bold),
            .Location = New Point(370, yPos),
            .Width = 90,
            .TextAlign = ContentAlignment.MiddleRight
        }
        pnlMain.Controls.Add(lblTotalHeader)
        yPos += 30

        ' Items
        For Each item In _returnItems
            Dim lblItem As New Label With {
                .Text = item.ProductName,
                .Font = New Font("Segoe UI", 9),
                .Location = New Point(20, yPos),
                .Width = 200
            }
            pnlMain.Controls.Add(lblItem)

            Dim lblQty As New Label With {
                .Text = item.QtyReturned.ToString("N0"),
                .Font = New Font("Segoe UI", 9),
                .Location = New Point(230, yPos),
                .Width = 50,
                .TextAlign = ContentAlignment.MiddleRight
            }
            pnlMain.Controls.Add(lblQty)

            Dim lblPrice As New Label With {
                .Text = $"R {item.UnitPrice:N2}",
                .Font = New Font("Segoe UI", 9),
                .Location = New Point(290, yPos),
                .Width = 70,
                .TextAlign = ContentAlignment.MiddleRight
            }
            pnlMain.Controls.Add(lblPrice)

            Dim lblTotal As New Label With {
                .Text = $"R {item.LineTotal:N2}",
                .Font = New Font("Segoe UI", 9),
                .Location = New Point(370, yPos),
                .Width = 90,
                .TextAlign = ContentAlignment.MiddleRight
            }
            pnlMain.Controls.Add(lblTotal)

            yPos += 25
        Next

        yPos += 10

        ' Separator
        Dim line2 As New Panel With {
            .Height = 2,
            .Width = 440,
            .BackColor = Color.Black,
            .Location = New Point(20, yPos)
        }
        pnlMain.Controls.Add(line2)
        yPos += 15

        ' Totals
        Dim subtotal = _totalReturn - _totalTax
        AddTotalLine(pnlMain, "Subtotal:", $"R {subtotal:N2}", yPos)
        yPos += 25
        AddTotalLine(pnlMain, "VAT (15%):", $"R {_totalTax:N2}", yPos)
        yPos += 30
        AddTotalLine(pnlMain, "REFUND AMOUNT:", $"R {_totalReturn:N2}", yPos, True, 12)
        yPos += 40

        ' Footer
        Dim lblFooter As New Label With {
            .Text = "Please retain this receipt for your records." & vbCrLf & "Refund will be processed to original payment method.",
            .Font = New Font("Segoe UI", 9, FontStyle.Italic),
            .ForeColor = Color.Gray,
            .Location = New Point(20, yPos),
            .Width = 440,
            .Height = 50,
            .TextAlign = ContentAlignment.TopCenter
        }
        pnlMain.Controls.Add(lblFooter)
        yPos += 60

        ' Buttons
        Dim pnlButtons As New Panel With {
            .Dock = DockStyle.Bottom,
            .Height = 70,
            .BackColor = ColorTranslator.FromHtml("#ECF0F1")
        }

        Dim btnPrint As New Button With {
            .Text = "PRINT",
            .Font = New Font("Segoe UI", 12, FontStyle.Bold),
            .Size = New Size(150, 45),
            .Location = New Point(80, 12),
            .BackColor = ColorTranslator.FromHtml("#27AE60"),
            .ForeColor = Color.White,
            .FlatStyle = FlatStyle.Flat,
            .Cursor = Cursors.Hand
        }
        btnPrint.FlatAppearance.BorderSize = 0
        AddHandler btnPrint.Click, Sub() PrintReceipt()
        pnlButtons.Controls.Add(btnPrint)

        Dim btnClose As New Button With {
            .Text = "CLOSE",
            .Font = New Font("Segoe UI", 12, FontStyle.Bold),
            .Size = New Size(150, 45),
            .Location = New Point(250, 12),
            .BackColor = ColorTranslator.FromHtml("#95A5A6"),
            .ForeColor = Color.White,
            .FlatStyle = FlatStyle.Flat,
            .Cursor = Cursors.Hand
        }
        btnClose.FlatAppearance.BorderSize = 0
        AddHandler btnClose.Click, Sub() Me.Close()
        pnlButtons.Controls.Add(btnClose)

        Me.Controls.Add(pnlMain)
        Me.Controls.Add(pnlButtons)
    End Sub

    Private Sub AddLabel(panel As Panel, text As String, yPos As Integer, Optional bold As Boolean = False)
        Dim lbl As New Label With {
            .Text = text,
            .Font = New Font("Segoe UI", 10, If(bold, FontStyle.Bold, FontStyle.Regular)),
            .AutoSize = True,
            .Location = New Point(20, yPos)
        }
        panel.Controls.Add(lbl)
    End Sub

    Private Sub AddTotalLine(panel As Panel, label As String, value As String, yPos As Integer, Optional bold As Boolean = False, Optional fontSize As Integer = 10)
        Dim lblLabel As New Label With {
            .Text = label,
            .Font = New Font("Segoe UI", fontSize, If(bold, FontStyle.Bold, FontStyle.Regular)),
            .Location = New Point(270, yPos),
            .Width = 100,
            .TextAlign = ContentAlignment.MiddleRight
        }
        panel.Controls.Add(lblLabel)

        Dim lblValue As New Label With {
            .Text = value,
            .Font = New Font("Segoe UI", fontSize, If(bold, FontStyle.Bold, FontStyle.Regular)),
            .Location = New Point(370, yPos),
            .Width = 90,
            .TextAlign = ContentAlignment.MiddleRight
        }
        panel.Controls.Add(lblValue)
    End Sub

    Private Sub PrintReceipt()
        Try
            Dim printDoc As New Printing.PrintDocument()
            printDoc.DefaultPageSettings.PaperSize = New Printing.PaperSize("80mm", 315, 1200) ' 80mm thermal
            
            AddHandler printDoc.PrintPage, Sub(sender, e)
                Dim font As New Font("Courier New", 8)
                Dim fontBold As New Font("Courier New", 8, FontStyle.Bold)
                Dim leftMargin As Integer = 10
                Dim yPos As Integer = 10
                
                ' Header
                e.Graphics.DrawString("OVEN DELIGHTS", fontBold, Brushes.Black, leftMargin, yPos)
                yPos += 15
                e.Graphics.DrawString("RETURN RECEIPT", fontBold, Brushes.Black, leftMargin, yPos)
                yPos += 15
                e.Graphics.DrawString("======================================", font, Brushes.Black, leftMargin, yPos)
                yPos += 15
                
                ' Return details
                e.Graphics.DrawString($"Return #: {_returnNumber}", font, Brushes.Black, leftMargin, yPos)
                yPos += 14
                e.Graphics.DrawString($"Date: {_returnDate:dd/MM/yyyy HH:mm}", font, Brushes.Black, leftMargin, yPos)
                yPos += 14
                e.Graphics.DrawString($"Original Invoice: {_invoiceNumber}", font, Brushes.Black, leftMargin, yPos)
                yPos += 14
                e.Graphics.DrawString($"Customer: {_customerName}", font, Brushes.Black, leftMargin, yPos)
                yPos += 14
                e.Graphics.DrawString("======================================", font, Brushes.Black, leftMargin, yPos)
                yPos += 15
                
                ' Return reason
                e.Graphics.DrawString("REASON FOR RETURN:", fontBold, Brushes.Black, leftMargin, yPos)
                yPos += 14
                e.Graphics.DrawString(_reason, font, Brushes.Black, leftMargin, yPos)
                yPos += 14
                e.Graphics.DrawString("======================================", font, Brushes.Black, leftMargin, yPos)
                yPos += 15
                
                ' Items
                e.Graphics.DrawString("RETURNED ITEMS:", fontBold, Brushes.Black, leftMargin, yPos)
                yPos += 14
                For Each item In _returnItems
                    e.Graphics.DrawString($"{item.QtyReturned:0.00} x {item.ProductName}", font, Brushes.Black, leftMargin, yPos)
                    yPos += 14
                    e.Graphics.DrawString($"    @ R{item.UnitPrice:N2} = R{item.LineTotal:N2}", font, Brushes.Black, leftMargin, yPos)
                    yPos += 14
                Next
                
                yPos += 5
                e.Graphics.DrawString("======================================", font, Brushes.Black, leftMargin, yPos)
                yPos += 15
                
                ' Totals
                Dim subtotal = Math.Round(_totalReturn / 1.15D, 2)
                Dim vat = Math.Round(_totalReturn - subtotal, 2)
                
                e.Graphics.DrawString($"Subtotal (excl VAT):  R {subtotal:N2}", font, Brushes.Black, leftMargin, yPos)
                yPos += 14
                e.Graphics.DrawString($"VAT (15%):            R {vat:N2}", font, Brushes.Black, leftMargin, yPos)
                yPos += 14
                e.Graphics.DrawString($"TOTAL REFUND:         R {_totalReturn:N2}", fontBold, Brushes.Black, leftMargin, yPos)
                yPos += 20
                
                e.Graphics.DrawString("======================================", font, Brushes.Black, leftMargin, yPos)
                yPos += 15
                e.Graphics.DrawString("Thank you", font, Brushes.Black, leftMargin, yPos)
            End Sub
            
            ' Show preview first
            Dim previewDialog As New PrintPreviewDialog()
            previewDialog.Document = printDoc
            previewDialog.Width = 400
            previewDialog.Height = 600
            
            If previewDialog.ShowDialog() = DialogResult.OK Then
                ' Print to default printer
                printDoc.Print()
            End If
            
        Catch ex As Exception
            MessageBox.Show($"Print error: {ex.Message}", "Print Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub
End Class
