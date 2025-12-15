Imports System.Drawing
Imports System.Drawing.Imaging

Public Class BarcodeGenerator
    ''' <summary>
    ''' Generates EAN-13 barcode for order numbers (same format as product barcodes)
    ''' Example: "6000009" -> Padded to "0000006000009" -> EAN-13 barcode
    ''' </summary>
    Public Shared Function GenerateCode39Barcode(text As String, width As Integer, height As Integer) As Bitmap
        Try
            ' Pad order number to 13 digits for EAN-13 (same as product barcodes)
            Dim barcode13 As String = text.PadLeft(13, "0"c)
            
            ' EAN-13 L-codes (left side, odd parity)
            Dim lCodes As String() = {
                "0001101", "0011001", "0010011", "0111101", "0100011",
                "0110001", "0101111", "0111011", "0110111", "0001011"
            }
            
            ' EAN-13 R-codes (right side)
            Dim rCodes As String() = {
                "1110010", "1100110", "1101100", "1000010", "1011100",
                "1001110", "1010000", "1000100", "1001000", "1110100"
            }
            
            ' Start, middle, end guards
            Dim startGuard As String = "101"
            Dim middleGuard As String = "01010"
            Dim endGuard As String = "101"
            
            ' Build barcode pattern
            Dim pattern As New System.Text.StringBuilder()
            pattern.Append(startGuard)
            
            ' Left side (6 digits)
            For i As Integer = 1 To 6
                Dim digit As Integer = Integer.Parse(barcode13(i).ToString())
                pattern.Append(lCodes(digit))
            Next
            
            pattern.Append(middleGuard)
            
            ' Right side (6 digits)
            For i As Integer = 7 To 12
                Dim digit As Integer = Integer.Parse(barcode13(i).ToString())
                pattern.Append(rCodes(digit))
            Next
            
            pattern.Append(endGuard)
            
            ' Draw barcode
            Dim bmp As New Bitmap(width, height)
            Using g As Graphics = Graphics.FromImage(bmp)
                g.Clear(Color.White)
                g.SmoothingMode = Drawing2D.SmoothingMode.None
                
                ' Calculate bar width
                Dim totalBars As Integer = pattern.Length
                Dim barWidth As Single = CSng((width - 20) / totalBars)
                Dim x As Single = 10
                Dim barHeight As Single = height - 25
                
                ' Draw bars
                For Each bit As Char In pattern.ToString()
                    If bit = "1"c Then
                        g.FillRectangle(Brushes.Black, x, 5, barWidth, barHeight)
                    End If
                    x += barWidth
                Next
                
                ' Draw human-readable order number below barcode
                Dim textFont As New Font("Arial", 10, FontStyle.Bold)
                Dim textSize = g.MeasureString(text, textFont)
                g.DrawString(text, textFont, Brushes.Black, (width - textSize.Width) / 2, height - 18)
            End Using
            
            Return bmp
        Catch ex As Exception
            ' Return blank white image if generation fails
            Dim bmp As New Bitmap(width, height)
            Using g As Graphics = Graphics.FromImage(bmp)
                g.Clear(Color.White)
                Dim errorFont As New Font("Arial", 8, FontStyle.Regular)
                g.DrawString($"Error: {text}", errorFont, Brushes.Black, 10, height / 2)
            End Using
            Return bmp
        End Try
    End Function
End Class
