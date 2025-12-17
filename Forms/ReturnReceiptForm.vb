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
    Private _returnItems As DataTable
    Private _totalRefund As Decimal
    Private _branchID As Integer
    Private _cashierName As String
    Private _barcodeImage As Bitmap

    Public Sub New(returnNumber As String, returnItems As DataTable, totalRefund As Decimal, branchID As Integer, cashierName As String)
        MyBase.New()
        _returnNumber = returnNumber
        _returnItems = returnItems
        _totalRefund = totalRefund
        _branchID = branchID
        _cashierName = cashierName
        
        ' Generate barcode
        Dim barcodeGen As New BarcodeGenerator()
        _barcodeImage = barcodeGen.GenerateBarcode(returnNumber)
        
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
        Dim panel As New Panel With {
            .Dock = DockStyle.Fill,
            .AutoScroll = True,
            .BackColor = Color.White
        }
        
        Dim yPos As Integer = 20
        
        ' Title
        Dim lblTitle As New Label With {
            .Text = "RETURN RECEIPT",
            .Font = New Font("Arial", 16, FontStyle.Bold),
            .Location = New Point(150, yPos),
            .AutoSize = True
        }
        panel.Controls.Add(lblTitle)
        yPos += 40
        
        ' Return Number with Barcode
        Dim lblReturnNum As New Label With {
            .Text = $"Return #: {_returnNumber}",
            .Font = New Font("Arial", 12, FontStyle.Bold),
            .Location = New Point(20, yPos),
            .AutoSize = True
        }
        panel.Controls.Add(lblReturnNum)
        yPos += 30
        
        ' Barcode Image
        If _barcodeImage IsNot Nothing Then
            Dim picBarcode As New PictureBox With {
                .Image = _barcodeImage,
                .SizeMode = PictureBoxSizeMode.AutoSize,
                .Location = New Point(150, yPos)
            }
            panel.Controls.Add(picBarcode)
            yPos += _barcodeImage.Height + 20
        End If
        
        ' Date and Cashier
        Dim lblDate As New Label With {
            .Text = $"Date: {DateTime.Now:yyyy-MM-dd HH:mm}",
            .Font = New Font("Arial", 10),
            .Location = New Point(20, yPos),
            .AutoSize = True
        }
        panel.Controls.Add(lblDate)
        yPos += 25
        
        Dim lblCashier As New Label With {
            .Text = $"Cashier: {_cashierName}",
            .Font = New Font("Arial", 10),
            .Location = New Point(20, yPos),
            .AutoSize = True
        }
        panel.Controls.Add(lblCashier)
        yPos += 40
        
        ' Items Header
        Dim lblItemsHeader As New Label With {
            .Text = "RETURNED ITEMS",
            .Font = New Font("Arial", 12, FontStyle.Bold),
            .Location = New Point(20, yPos),
            .AutoSize = True
        }
        panel.Controls.Add(lblItemsHeader)
        yPos += 30
        
        ' Column Headers
        Dim lblHeaderItem As New Label With {
            .Text = "Item",
            .Font = New Font("Arial", 9, FontStyle.Bold),
            .Location = New Point(20, yPos),
            .Size = New Size(200, 20)
        }
        panel.Controls.Add(lblHeaderItem)
        
        Dim lblHeaderQty As New Label With {
            .Text = "Qty",
            .Font = New Font("Arial", 9, FontStyle.Bold),
            .Location = New Point(230, yPos),
            .Size = New Size(50, 20)
        }
        panel.Controls.Add(lblHeaderQty)
        
        Dim lblHeaderPrice As New Label With {
            .Text = "Price",
            .Font = New Font("Arial", 9, FontStyle.Bold),
            .Location = New Point(290, yPos),
            .Size = New Size(80, 20),
            .TextAlign = ContentAlignment.MiddleRight
        }
        panel.Controls.Add(lblHeaderPrice)
        
        Dim lblHeaderTotal As New Label With {
            .Text = "Total",
            .Font = New Font("Arial", 9, FontStyle.Bold),
            .Location = New Point(380, yPos),
            .Size = New Size(80, 20),
            .TextAlign = ContentAlignment.MiddleRight
        }
        panel.Controls.Add(lblHeaderTotal)
        yPos += 25
        
        ' Line separator
        Dim separator1 As New Panel With {
            .BackColor = Color.Black,
            .Location = New Point(20, yPos),
            .Size = New Size(440, 1)
        }
        panel.Controls.Add(separator1)
        yPos += 10
        
        ' Return Items
        For Each row As DataRow In _returnItems.Rows
            Dim itemName As String = row("ProductName").ToString()
            Dim qty As Decimal = CDec(row("Quantity"))
            Dim price As Decimal = CDec(row("UnitPrice"))
            Dim total As Decimal = CDec(row("LineTotal"))
            
            Dim lblItem As New Label With {
                .Text = itemName,
                .Font = New Font("Arial", 9),
                .Location = New Point(20, yPos),
                .Size = New Size(200, 20)
            }
            panel.Controls.Add(lblItem)
            
            Dim lblQty As New Label With {
                .Text = qty.ToString("0.##"),
                .Font = New Font("Arial", 9),
                .Location = New Point(230, yPos),
                .Size = New Size(50, 20)
            }
            panel.Controls.Add(lblQty)
            
            Dim lblPrice As New Label With {
                .Text = $"R {price:N2}",
                .Font = New Font("Arial", 9),
                .Location = New Point(290, yPos),
                .Size = New Size(80, 20),
                .TextAlign = ContentAlignment.MiddleRight
            }
            panel.Controls.Add(lblPrice)
            
            Dim lblTotal As New Label With {
                .Text = $"R {total:N2}",
                .Font = New Font("Arial", 9),
                .Location = New Point(380, yPos),
                .Size = New Size(80, 20),
                .TextAlign = ContentAlignment.MiddleRight
            }
            panel.Controls.Add(lblTotal)
            yPos += 25
        Next
        
        ' Line separator
        Dim separator2 As New Panel With {
            .BackColor = Color.Black,
            .Location = New Point(20, yPos),
            .Size = New Size(440, 1)
        }
        panel.Controls.Add(separator2)
        yPos += 15
        
        ' Total Refund
        Dim lblTotalRefund As New Label With {
            .Text = "TOTAL REFUND:",
            .Font = New Font("Arial", 14, FontStyle.Bold),
            .Location = New Point(20, yPos),
            .Size = New Size(250, 30)
        }
        panel.Controls.Add(lblTotalRefund)
        
        Dim lblRefundAmount As New Label With {
            .Text = $"R {_totalRefund:N2}",
            .Font = New Font("Arial", 14, FontStyle.Bold),
            .Location = New Point(280, yPos),
            .Size = New Size(180, 30),
            .TextAlign = ContentAlignment.MiddleRight,
            .ForeColor = Color.Red
        }
        panel.Controls.Add(lblRefundAmount)
        yPos += 50
        
        ' Buttons
        Dim btnPanel As New Panel With {
            .Location = New Point(100, yPos),
            .Size = New Size(300, 50)
        }
        
        Dim btnPrint As New Button With {
            .Text = "Print",
            .Size = New Size(120, 40),
            .Location = New Point(0, 0),
            .Font = New Font("Arial", 10, FontStyle.Bold),
            .BackColor = Color.FromArgb(52, 152, 219),
            .ForeColor = Color.White,
            .FlatStyle = FlatStyle.Flat
        }
        AddHandler btnPrint.Click, AddressOf PrintReceipt
        btnPanel.Controls.Add(btnPrint)
        
        Dim btnClose As New Button With {
            .Text = "Close",
            .Size = New Size(120, 40),
            .Location = New Point(140, 0),
            .Font = New Font("Arial", 10, FontStyle.Bold),
            .BackColor = Color.FromArgb(149, 165, 166),
            .ForeColor = Color.White,
            .FlatStyle = FlatStyle.Flat
        }
        AddHandler btnClose.Click, Sub() Me.Close()
        btnPanel.Controls.Add(btnClose)
        
        panel.Controls.Add(btnPanel)
        
        Me.Controls.Add(panel)
    End Sub

    Private Sub PrintReceipt()
        Try
            Dim printDoc As New Printing.PrintDocument()
            printDoc.DefaultPageSettings.PaperSize = New Printing.PaperSize("80mm", 315, 1200)
            
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
                e.Graphics.DrawString($"Date: {DateTime.Now:dd/MM/yyyy HH:mm}", font, Brushes.Black, leftMargin, yPos)
                yPos += 14
                e.Graphics.DrawString($"Cashier: {_cashierName}", font, Brushes.Black, leftMargin, yPos)
                yPos += 14
                e.Graphics.DrawString("======================================", font, Brushes.Black, leftMargin, yPos)
                yPos += 15
                
                ' Items
                e.Graphics.DrawString("RETURNED ITEMS:", fontBold, Brushes.Black, leftMargin, yPos)
                yPos += 14
                For Each row As DataRow In _returnItems.Rows
                    Dim itemName As String = row("ProductName").ToString()
                    Dim qty As Decimal = CDec(row("Quantity"))
                    Dim price As Decimal = CDec(row("UnitPrice"))
                    Dim total As Decimal = CDec(row("LineTotal"))
                    
                    e.Graphics.DrawString($"{qty:0.00} x {itemName}", font, Brushes.Black, leftMargin, yPos)
                    yPos += 14
                    e.Graphics.DrawString($"    @ R{price:N2} = R{total:N2}", font, Brushes.Black, leftMargin, yPos)
                    yPos += 14
                Next
                
                yPos += 5
                e.Graphics.DrawString("======================================", font, Brushes.Black, leftMargin, yPos)
                yPos += 15
                
                ' Total
                e.Graphics.DrawString($"TOTAL REFUND:         R {_totalRefund:N2}", fontBold, Brushes.Black, leftMargin, yPos)
                yPos += 20
                
                e.Graphics.DrawString("======================================", font, Brushes.Black, leftMargin, yPos)
                yPos += 15
                e.Graphics.DrawString("Thank you", font, Brushes.Black, leftMargin, yPos)
            End Sub
            
            printDoc.Print()
            
        Catch ex As Exception
            MessageBox.Show($"Print error: {ex.Message}", "Print Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub
End Class
